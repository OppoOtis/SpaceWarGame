using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Astar.MultiThreaded
{
    [BurstCompile]
    public class GridManager
    {
        readonly private static int MAX_COLLIDERS = 1;
        
        public NativeArray<Box> boxes;
        public NativeArray<Node> nodes;
        public NativeArray<StaticNode> staticNodes;
        public JobHandle gridHandle;
        
        private LayerMask unwalkableMask;
        
        private int obstacleProximityPenalty;
        private int amountBlurPasses;
        private readonly int[] walkableRegions;

        public int maxSize;
        private float nodeSize;
        private float nodeRadius;
        private float3 nodeScale;
        private bool disposed;
        
        private NativeArray<OverlapBoxCommand> overlapBoxCommands;
        private NativeArray<ColliderHit> results;
        
        static readonly ProfilerMarker pmGrid = new ProfilerMarker("building grid");
        static readonly ProfilerMarker pmUpdateGridFast = new ProfilerMarker("update grid fast");
        
#if UNITY_EDITOR
        private List<Matrix4x4> gridDebug;
#endif

        public GridManager(LayerMask unwalkableMask, TerrainType[] walkableRegions, 
            int obstacleProximityPenalty, int amountBlurPasses, Transform[] serialBoxes, float nodeSize)
        {
            this.unwalkableMask = unwalkableMask;
            this.obstacleProximityPenalty = obstacleProximityPenalty;
            this.amountBlurPasses = amountBlurPasses;
            this.nodeSize = nodeSize;
            
            nodeRadius = nodeSize / 2;
            nodeScale = new float3(nodeSize, nodeSize, nodeSize);

            Box[] tempBoxes = new Box[serialBoxes.Length];
            int counter = 0;
            for (var i = 0; i < serialBoxes.Length; i++)
            {
                float3 pos = math.round(serialBoxes[i].position / nodeSize) * nodeSize;
                float3 scale = math.round(serialBoxes[i].lossyScale / (nodeSize * 2)) * (nodeSize * 2);
                
                int xLength = Mathf.CeilToInt(scale.x / nodeSize) + 1;
                int yLength = Mathf.CeilToInt(scale.y / nodeSize) + 1;
                int zLength = Mathf.CeilToInt(scale.z / nodeSize) + 1;
                float3 minPos = new float3(pos.x - scale.x / 2, pos.y - scale.y / 2, pos.z - scale.z / 2);
                float3 maxPos = new float3(pos.x + scale.x / 2, pos.y + scale.y / 2, pos.z + scale.z / 2);
                tempBoxes[i] = new Box(counter, pos, minPos, maxPos, scale, xLength, yLength, zLength, nodeSize);
                counter += xLength * yLength * zLength;
            }
            
            boxes = new NativeArray<Box>(tempBoxes, Allocator.Persistent);
            foreach (Box box in boxes)
            {
                maxSize += box.xLength * box.yLength * box.zLength;
            }
            nodes = new NativeArray<Node>(maxSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            staticNodes = new NativeArray<StaticNode>(maxSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            this.walkableRegions = new int[32];
            foreach (TerrainType region in walkableRegions)
            {
                for (int i = 0; i < 32; i++)
                {
                    int mask = 1 << i;
                    int result = region.terrainMask.value & mask;
                    if (result > 0)
                    {
                        this.walkableRegions[i] += region.terrainPenalty;
                    }
                }
            }
            
            CreateGridParallelized();
        }
        
        private void CreateGridParallelized()
        {
#if UNITY_EDITOR
            gridDebug = new List<Matrix4x4>();
#endif
            
            disposed = false;
            overlapBoxCommands = new NativeArray<OverlapBoxCommand>(maxSize, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            results = new NativeArray<ColliderHit>(maxSize * MAX_COLLIDERS, Allocator.Persistent,
                NativeArrayOptions.ClearMemory);
            
            // QueryParameters queryParameters = new QueryParameters(unwalkableMask);
            gridHandle = new JobHandle();
            
            for (int i = 0; i < boxes.Length; i++)
            {
                Box box = boxes[i];
                PrepareBoxCommandsJob prepareBoxCommandsJob = new PrepareBoxCommandsJob(nodeSize, box.xLength, 
                    box.zLength, box.minPositionWS, box.startIndex, QueryParameters.Default, overlapBoxCommands);
                gridHandle = prepareBoxCommandsJob.Schedule(box.GridSize, 128, gridHandle);
            }

            gridHandle = OverlapBoxCommand.ScheduleBatch(overlapBoxCommands, results, 32, MAX_COLLIDERS, gridHandle);
        }

        public void CheckComplete()
        {
            if (gridHandle.IsCompleted && !disposed)
            {
                pmGrid.Begin();
                ResolveOverlapBoxCommands();
                pmGrid.End();
            }
        }

        public void UpdateGrid(float3 min, float3 max)
        {
            int count = 0;
            
            for (int i = 0; i < boxes.Length; i++)
            {
                var box = boxes[i];

                if (!BoxCollision(min, max, box.minPositionWS, box.maxPositionWS))
                {
                    continue;
                }
            
                int3 minIndex = WorldPosToGridIndex(min + new float3(nodeRadius, nodeRadius, nodeRadius), i);
                int3 maxIndex = WorldPosToGridIndex(max - new float3(nodeRadius, nodeRadius, nodeRadius), i);
                
                int xDelta = maxIndex.x - minIndex.x + 1;
                int yDelta = maxIndex.y - minIndex.y + 1;
                int zDelta = maxIndex.z - minIndex.z + 1;
                int batchSize = xDelta * yDelta * zDelta;
                
                count += batchSize;
            }
            
            overlapBoxCommands = new NativeArray<OverlapBoxCommand>(count, Allocator.TempJob);
            results = new NativeArray<ColliderHit>(count * MAX_COLLIDERS, Allocator.TempJob);

            count = 0;
            
            for (int i = 0; i < boxes.Length; i++)
            {
                var box = boxes[i];

                if (!BoxCollision(min, max, box.minPositionWS, box.maxPositionWS))
                {
                    continue;
                }
            
                int3 minIndex = WorldPosToGridIndex(min + new float3(nodeRadius, nodeRadius, nodeRadius), i);
                int3 maxIndex = WorldPosToGridIndex(max - new float3(nodeRadius, nodeRadius, nodeRadius), i);
                
                int xDelta = maxIndex.x - minIndex.x + 1;
                int yDelta = maxIndex.y - minIndex.y + 1;
                int zDelta = maxIndex.z - minIndex.z + 1;
                int batchSize = xDelta * yDelta * zDelta;

                float3 offset = GridIndexToWorldPos(minIndex, box.minPositionWS);
                
                PrepareBoxCommandsJob prepareBoxCommandsJob = new PrepareBoxCommandsJob(nodeSize, xDelta, 
                    zDelta, offset, count, QueryParameters.Default, overlapBoxCommands);
                gridHandle = prepareBoxCommandsJob.Schedule(batchSize, 128, gridHandle);
                
                count += batchSize;
            }
            
            gridHandle = OverlapBoxCommand.ScheduleBatch(overlapBoxCommands, results, 32, MAX_COLLIDERS, gridHandle);
            gridHandle.Complete();
            int counter = 0;
            for (int i = 0; i < boxes.Length; i++)
            {
                var box = boxes[i];

                if (!BoxCollision(min, max, box.minPositionWS, box.maxPositionWS))
                {
                    continue;
                }

                int3 minIndex = WorldPosToGridIndex(min + new float3(nodeRadius, nodeRadius, nodeRadius), i);
                int3 maxIndex = WorldPosToGridIndex(max - new float3(nodeRadius, nodeRadius, nodeRadius), i);

                int xDelta = maxIndex.x - minIndex.x + 1;
                int yDelta = maxIndex.y - minIndex.y + 1;
                int zDelta = maxIndex.z - minIndex.z + 1;
                
                for (int y = 0; y < yDelta; y++)
                for (int z = 0; z < zDelta; z++)
                for (int x = 0; x < xDelta; x++)
                {
                    int3 gridIndex = new int3(x, y, z) + minIndex;
                    StaticNode staticNode = GetStaticNodeFromResults(counter, gridIndex, box);
                    box.UpdateStaticNode(staticNodes, gridIndex, staticNode);
                    counter++;
                }
            }

            overlapBoxCommands.Dispose();
            results.Dispose();
#if UNITY_EDITOR
            RecalcDebugGrid();
#endif
        }
        
        public void UpdateGridFast(float3 min, float3 max, bool walkable)
        {
            pmUpdateGridFast.Begin();
            for (int i = 0; i < boxes.Length; i++)
            {
                var box = boxes[i];

                if (!BoxCollision(min, max, box.minPositionWS, box.maxPositionWS))
                {
                    continue;
                }

                int3 minIndex = WorldPosToGridIndex(min + new float3(nodeRadius, nodeRadius, nodeRadius), i);
                int3 maxIndex = WorldPosToGridIndex(max - new float3(nodeRadius, nodeRadius, nodeRadius), i);

                int xDelta = maxIndex.x - minIndex.x + 1;
                int yDelta = maxIndex.y - minIndex.y + 1;
                int zDelta = maxIndex.z - minIndex.z + 1;
                int batchSize = xDelta * yDelta * zDelta;

                GridUpdateFastJob gridUpdateFastJob = new GridUpdateFastJob(box, staticNodes, xDelta, zDelta, minIndex, walkable);
                gridHandle = gridUpdateFastJob.Schedule(batchSize, 32, gridHandle);
            }
            gridHandle.Complete();
            pmUpdateGridFast.End();
#if UNITY_EDITOR
            RecalcDebugGrid();
#endif
            
            // BlurPenaltyMap();
        }

#if UNITY_EDITOR
        private void RecalcDebugGrid()
        {
            gridDebug.Clear();
            for (int i = 0; i < boxes.Length; i++)
            {
                Box box = boxes[i];

                for (int j = 0; j < box.GridSize; j++)
                {
                    int index = j;
                    int3 gridIndex = new int3(index % box.xLength, index / (box.xLength * box.zLength), index / box.xLength % box.zLength);

                    StaticNode staticNode = box.GetStaticNode(staticNodes.AsReadOnly(), gridIndex);
                    if(staticNode.walkable)
                        continue;
                    
                    GridIndexToWorldPos(gridIndex, box.minPositionWS, nodeSize, out float3 worldPos);
                    gridDebug.Add(Get_TRS_Matrix(worldPos, nodeScale));
                }
            }
        }
#endif
        
        
        private void ResolveOverlapBoxCommands()
        {
#if UNITY_EDITOR
            gridDebug.Clear();
#endif
            gridHandle.Complete();

            disposed = true;
            for (int i = 0; i < boxes.Length; i++)
            {
                Box box = boxes[i];
                for (int index = box.startIndex; index < box.GridSize + box.startIndex; index++)
                {
                    int localIndex = index - box.startIndex;
                    int3 gridIndex = new int3(localIndex % box.xLength, localIndex / (box.xLength * box.zLength), localIndex / box.xLength % box.zLength);
                        
                    StaticNode staticNode = GetStaticNodeFromResults(index, gridIndex, box);

                    nodes[index] = new Node(gridIndex, i);
                    staticNodes[index] = staticNode;
                }
            }

            for (int i = 0; i < amountBlurPasses; i++)
            {
                BlurPenaltyMap();
            }
            
            overlapBoxCommands.Dispose();
            results.Dispose();
        }

        private StaticNode GetStaticNodeFromResults(int index, int3 gridIndex, Box box)
        {
            StaticNode staticNode = new StaticNode(0, true);
            for (int f = 0; f < MAX_COLLIDERS; f++)
            {
                ColliderHit colliderHit = results[index * MAX_COLLIDERS + f];
                if (colliderHit.instanceID == 0)
                {
                    continue;
                }
                
                int movementPenalty = 0;
                int unwalkableMaskInt = (1 << colliderHit.collider.gameObject.layer) & unwalkableMask.value;
                bool walkable = unwalkableMaskInt == 0;
                if (!walkable)
                {
                    movementPenalty += obstacleProximityPenalty;
#if UNITY_EDITOR
                    GridIndexToWorldPos(gridIndex, box.minPositionWS, nodeSize, out float3 worldPos);
                    gridDebug.Add(Get_TRS_Matrix(worldPos, new float3(nodeSize, nodeSize, nodeSize)));
#endif
                }

                for (int j = 0; j < 32; j++)
                {
                    int mask = 1 << j;
                    int result = (1 << colliderHit.collider.gameObject.layer) & mask;
                    if (result > 0)
                    {
                        movementPenalty += walkableRegions[j];
                        break;
                    }
                }
                staticNode.movementPenalty = movementPenalty;
                staticNode.walkable = walkable;
            }

            return staticNode;
        }
        
        private void BlurPenaltyMap()
        {
            NativeArray<StaticNode> staticNodesCopy = new NativeArray<StaticNode>(staticNodes, Allocator.TempJob);
            for (int i = 0; i < boxes.Length; i++)
            {
                Box box = boxes[i];

                GridBlurJob gridBlurJob = new GridBlurJob(staticNodes, staticNodesCopy.AsReadOnly(), boxes.AsReadOnly(), box, GridDimension.Y, obstacleProximityPenalty);
                gridHandle = gridBlurJob.Schedule(box.GridSize, 32, gridHandle);
            }
            
            gridHandle.Complete();
            staticNodesCopy.Dispose();
            staticNodesCopy = new NativeArray<StaticNode>(staticNodes, Allocator.TempJob);
            for (int i = 0; i < boxes.Length; i++)
            {
                Box box = boxes[i];
                
                GridBlurJob gridBlurJob = new GridBlurJob(staticNodes, staticNodesCopy.AsReadOnly(), boxes.AsReadOnly(), box, GridDimension.Z, obstacleProximityPenalty);
                gridHandle = gridBlurJob.Schedule(box.GridSize, 32, gridHandle);
            }
            
            gridHandle.Complete();
            staticNodesCopy.Dispose();
            staticNodesCopy = new NativeArray<StaticNode>(staticNodes, Allocator.TempJob);
            for (int i = 0; i < boxes.Length; i++)
            {
                Box box = boxes[i];
                
                GridBlurJob gridBlurJob = new GridBlurJob(staticNodes, staticNodesCopy.AsReadOnly(), boxes.AsReadOnly(), box, GridDimension.X, obstacleProximityPenalty);
                gridHandle = gridBlurJob.Schedule(box.GridSize, 32, gridHandle);
            }
            gridHandle.Complete();
            staticNodesCopy.Dispose();
        }

#if UNITY_EDITOR
        public void DrawDebugGrid(Mesh mesh, Material material)
        {
            if(gridDebug == null)
                return;
            
            Graphics.DrawMeshInstanced(mesh, 0, material, gridDebug);
        }
#endif
        
        private static void GridIndexToWorldPos(int3 gridIndex, float3 minPositionWS, float nodeSize, out float3 worldPos)
        {
            worldPos = new float3(
                gridIndex.x, 
                gridIndex.y, 
                gridIndex.z) * nodeSize + minPositionWS;
        }
        
        private float3 GridIndexToWorldPos(int3 gridIndex, float3 minPositionWS)
        {
            return new float3(
                gridIndex.x * nodeSize + minPositionWS.x, 
                gridIndex.y * nodeSize + minPositionWS.y, 
                gridIndex.z * nodeSize + minPositionWS.z);
        }

        private int3 WorldPosToGridIndex(float3 worldPos, int boxIndex)
        {
            int3 index = new int3(
                (int)math.round((worldPos.x - boxes[boxIndex].minPositionWS.x) / nodeSize),
                (int)math.round((worldPos.y - boxes[boxIndex].minPositionWS.y) / nodeSize),
                (int)math.round((worldPos.z - boxes[boxIndex].minPositionWS.z) / nodeSize));
            index.x = math.clamp(index.x, 0, boxes[boxIndex].xLength - 1);
            index.y = math.clamp(index.y, 0, boxes[boxIndex].yLength - 1);
            index.z = math.clamp(index.z, 0, boxes[boxIndex].zLength - 1);
            return index;
        }
        
        private bool BoxCollision(float3 minPosA, float3 maxPosA, float3 minPosB, float3 maxPosB)
        {
            return minPosA.x <= maxPosB.x && maxPosA.x >= minPosB.x &&
                   minPosA.y <= maxPosB.y && maxPosA.y >= minPosB.y &&
                   minPosA.z <= maxPosB.z && maxPosA.z >= minPosB.z;
        }

        public void Dispose()
        {
            boxes.Dispose();
            nodes.Dispose();
            staticNodes.Dispose();
        }

#if UNITY_EDITOR
        private Matrix4x4 GetTranslationMatrix(Vector3 position)
        {
            return new Matrix4x4(
                new Vector4(1, 0, 0, 0),
                new Vector4(0, 1, 0, 0),
                new Vector4(0, 0, 1, 0),
                new Vector4(position.x, position.y, position.z, 1));
        }

        private Matrix4x4 GetScaleMatrix(Vector3 scale)
        {
            return new Matrix4x4(
                new Vector4(scale.x, 0, 0, 0),
                new Vector4(0, scale.y, 0, 0),
                new Vector4(0, 0, scale.z, 0),
                new Vector4(0, 0, 0, 1));
        }

        private Matrix4x4 Get_TRS_Matrix(Vector3 position, Vector3 scale)
        {
            return GetTranslationMatrix(position) * GetScaleMatrix(scale);
        }
#endif
    }
}



                    
