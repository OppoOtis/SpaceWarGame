using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Astar.MultiThreaded
{
    //Unused since cant access the collider component inside jobs
    [BurstCompile]
    public struct ResolveBoxCommandsJob : IJobFor
    {
        private Box box;
        private int boxIndex;
        private int obstacleProximityPenalty;
        private int maxHits;
        
        private NativeArray<ColliderHit>.ReadOnly results;
        [NativeDisableParallelForRestriction] private NativeArray<Node> nodes;
        [NativeDisableParallelForRestriction] private NativeArray<StaticNode> staticNodes;
        
#if UNITY_EDITOR
        [NativeDisableParallelForRestriction] public NativeList<Matrix4x4> gridDebug;
        public float nodeSize;
#endif

        public ResolveBoxCommandsJob(Box box, int boxIndex, int obstacleProximityPenalty, int maxHits, NativeArray<ColliderHit>.ReadOnly results, NativeArray<Node> nodes, NativeArray<StaticNode> staticNodes) : this()
        {
            this.box = box;
            this.boxIndex = boxIndex;
            this.obstacleProximityPenalty = obstacleProximityPenalty;
            this.maxHits = maxHits;
            this.results = results;
            this.nodes = nodes;
            this.staticNodes = staticNodes;
        }

        public void Execute(int index)
        {
            index += box.startIndex;
            int3 gridIndex = new int3((int)(index % box.xLength), (int)(index / (box.xLength * box.zLength)), (int)(index / box.xLength) % box.xLength);
            
            StaticNode staticNode = new StaticNode(0, true);
            for (int i = 0; i < maxHits; i++)
            {
                ColliderHit colliderHit = results[index * maxHits + i];
                if (colliderHit.instanceID == 0)
                {
                    continue;
                }
                
#if UNITY_EDITOR
                GridIndexToWorldPos(gridIndex, box.minPosition, out float3 worldPos);
                gridDebug.Add(Get_TRS_Matrix(worldPos, new float3(nodeSize, nodeSize, nodeSize)));
#endif

                staticNode.movementPenalty = obstacleProximityPenalty;
                staticNode.walkable = false;
                break;
            }
            
            nodes[index] = new Node(gridIndex, boxIndex);
            staticNodes[index] = staticNode;
        }

#if UNITY_EDITOR
        private void GridIndexToWorldPos(int3 gridIndex, int3 minPosition, out float3 worldPos)
        {
            worldPos = new float3(
                gridIndex.x + minPosition.x, 
                gridIndex.y + minPosition.y, 
                gridIndex.z + minPosition.z) * nodeSize;
        }
#endif
        
        
        private Matrix4x4 GetTranslationMatrix(float3 position)
        {
            return new Matrix4x4(
                new Vector4(1, 0, 0, 0),
                new Vector4(0, 1, 0, 0),
                new Vector4(0, 0, 1, 0),
                new Vector4(position.x, position.y, position.z, 1));
        }

        private Matrix4x4 GetScaleMatrix(float3 scale)
        {
            return new Matrix4x4(
                new Vector4(scale.x, 0, 0, 0),
                new Vector4(0, scale.y, 0, 0),
                new Vector4(0, 0, scale.z, 0),
                new Vector4(0, 0, 0, 1));
        }

        private Matrix4x4 Get_TRS_Matrix(float3 position, float3 scale)
        {
            return GetTranslationMatrix(position) * GetScaleMatrix(scale);
        }
    }
}