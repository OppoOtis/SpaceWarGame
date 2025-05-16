using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;

namespace Astar.MultiThreaded
{
    [NativeContainerSupportsDeallocateOnJobCompletion]
    [BurstCompile]
    public struct PathfindJob : IJob
    {
        public NativeList<float3> waypoints;
        public NativeArray<bool> pathSuccess;
        public NativeList<int3> closedSet;
        public NativeHashMap<int3, float> gCostNodes;
        
#if UNITY_EDITOR
        public NativeList<NodeDebug> debugNodes;
#endif
        
        private int maxSize;
        private float3 startPos;
        private float3 targetPos;
        private float nodeSize;

        [DeallocateOnJobCompletion]
        private NativeArray<Node> neighbourNodes;
        private NativeArray<Box>.ReadOnly boxes; 
        private NativeArray<StaticNode>.ReadOnly staticNodes; 
        private NativeArray<Node>.ReadOnly nodesReadOnly;
        
        public PathfindJob(NativeArray<Box>.ReadOnly boxes, NativeArray<Node>.ReadOnly nodes, NativeArray<StaticNode>.ReadOnly staticNodes, float3 startPos, float3 targetPos, float nodeSize, int maxSize) : this()
        {
            this.boxes = boxes;
            this.staticNodes = staticNodes;
            this.startPos = startPos;
            this.targetPos = targetPos;
            this.nodeSize = nodeSize;
            this.maxSize = maxSize;
            nodesReadOnly = nodes;
            waypoints = new NativeList<float3>(AllocatorManager.TempJob);
            neighbourNodes = new NativeArray<Node>(27, Allocator.TempJob);
            pathSuccess = new NativeArray<bool>(1, Allocator.TempJob);
            closedSet = new NativeList<int3>(Allocator.TempJob);
            gCostNodes = new NativeHashMap<int3, float>(10, Allocator.TempJob);
            
#if UNITY_EDITOR
            debugNodes = new NativeList<NodeDebug>(AllocatorManager.TempJob);
#endif
        }
        static readonly ProfilerMarker pm1 = new ProfilerMarker("allocating nodes");
        static readonly ProfilerMarker pm2 = new ProfilerMarker("allocating binaryHeap");
        static readonly ProfilerMarker pm3 = new ProfilerMarker("filling nodes");
        static readonly ProfilerMarker pm4 = new ProfilerMarker("allocating new memory");

        public void Execute()
        {
            pm4.Begin();
            pm1.Begin();
            NativeArray<Node> nodes = new NativeArray<Node>(nodesReadOnly.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            pm1.End();
            pm3.Begin();
            NativeArray<Node>.Copy(nodesReadOnly, nodes);
            pm3.End();
            pm2.Begin();
            NativeBinaryHeap openSet = new NativeBinaryHeap(maxSize, Allocator.Temp, boxes, nodes);
            pm2.End();
            pm4.End();

            float3 startPosWS = startPos;
            float3 targetPosWS = targetPos;

            startPos.x = math.round(startPos.x / nodeSize);
            startPos.y = math.round(startPos.y / nodeSize);
            startPos.z = math.round(startPos.z / nodeSize);
            targetPos.x = math.round(targetPos.x / nodeSize);
            targetPos.y = math.round(targetPos.y / nodeSize);
            targetPos.z = math.round(targetPos.z / nodeSize);
            
            GetNodeFromWorldPoint((int3)startPos, out int3 startIndex, out int startBoxIndex);
            if (startBoxIndex == -1)
            {
                openSet.Dispose();
                nodes.Dispose();
                return;
            }
            if (!boxes[startBoxIndex].GetStaticNode(staticNodes, startIndex).walkable)
            {
                GetClosestWalkableNode((int3)startPos, targetPos, out startIndex, out startBoxIndex);
            }

            GetNodeFromWorldPoint((int3)targetPos, out int3 targetIndex, out int targetBoxIndex);
            if (targetBoxIndex == -1)
            {
                openSet.Dispose();
                nodes.Dispose();
                return;
            }
            if (!boxes[targetBoxIndex].GetStaticNode(staticNodes, targetIndex).walkable)
            {
                GetClosestWalkableNode((int3)targetPos, startPos, out targetIndex, out targetBoxIndex);
            }
            
            if (startBoxIndex == -1 || targetBoxIndex == -1)
            {
                openSet.Dispose();
                nodes.Dispose();
                return;
            }

            Node startNode = boxes[startBoxIndex].GetNode(nodes, startIndex);
            Node targetNode = boxes[targetBoxIndex].GetNode(nodes, targetIndex);
            StaticNode staticStartNode = boxes[startBoxIndex].GetStaticNode(staticNodes, startIndex);
            StaticNode staticTargetNode = boxes[targetBoxIndex].GetStaticNode(staticNodes, targetIndex);
            GridIndexToWorldIndex(targetIndex, boxes[targetBoxIndex].minPosition, out int3 targetWorldIndex);
            if (staticStartNode.walkable && staticTargetNode.walkable)
            {
                startNode.hCost = GetDistance(startIndex, targetIndex);
                openSet.Add(startNode);
                int counter = 0;

                while (openSet.currentItemCount > 0 && counter < maxSize)
                {
                    counter++;
                    Node currentNode = openSet.RemoveFirst();
                    currentNode.locked = true;
                    closedSet.Add(currentNode.gridIndex);
                    boxes[currentNode.gridBoxIndex].UpdateNode(nodes, currentNode);

#if UNITY_EDITOR
                    GridIndexToWorldPos(currentNode.gridIndex, boxes[currentNode.gridBoxIndex].minPositionWS, out float3 currentWorldPos);
                    debugNodes.Add(new NodeDebug(currentWorldPos, counter));
#endif

                    bool shouldEnd = EquateInt3(currentNode.gridIndex, targetNode.gridIndex) && currentNode.gridBoxIndex == targetNode.gridBoxIndex;
                    if (shouldEnd)
                    {
                        RetracePath(startNode, boxes[targetNode.gridBoxIndex].GetNode(nodes, targetNode.gridIndex.x, targetNode.gridIndex.y, targetNode.gridIndex.z), nodes);
                        if (waypoints.Length == 0)
                        {
                            waypoints.Add(startPosWS);
                        }
                        waypoints[0] = targetPosWS;
                        waypoints[^1] = startPosWS;
                        pathSuccess[0] = true;
                        break;
                    }

                    int walkableBitMask;
                    int amountNeighbours;
                    int currentNodeNeighbourIndex;
                    int[] currentNodeWalkableMask;
                    if (boxes[currentNode.gridBoxIndex].scale.y == 0)
                    {
                        GetNeighbours2D(currentNode.gridIndex, currentNode.gridBoxIndex, out walkableBitMask, ref neighbourNodes, nodes);
                        amountNeighbours = 9;
                        currentNodeNeighbourIndex = 4;
                        currentNodeWalkableMask = nodeWalkableMask2D;
                    }
                    else
                    {
                        GetNeighbours(currentNode.gridIndex, currentNode.gridBoxIndex, out walkableBitMask, ref neighbourNodes, nodes);
                        amountNeighbours = 27;
                        currentNodeNeighbourIndex = 13;
                        currentNodeWalkableMask = nodeWalkableMask;
                    }
                    GridIndexToWorldIndex(currentNode.gridIndex, boxes[currentNode.gridBoxIndex].minPosition, out int3 currentWorldIndex);

                    for (int i = 0; i < amountNeighbours; i++)
                    {
                        if (i == currentNodeNeighbourIndex)
                        {
                            continue;
                        }
                        
                        Node neighbour = neighbourNodes[i];
                        int mask = walkableBitMask & currentNodeWalkableMask[i];
                        bool isInClosedSet = false;
                        for (int j = 0; j < closedSet.Length; j++)
                        {
                            if (EquateInt3(neighbour.gridIndex, closedSet[j]))
                                isInClosedSet = true;
                        }
                        if (neighbour.gridBoxIndex == -1 || mask != currentNodeWalkableMask[i] || neighbour.locked || isInClosedSet)
                        {
                            continue;
                        }
                        
                        GridIndexToWorldIndex(neighbour.gridIndex, boxes[neighbour.gridBoxIndex].minPosition, out int3 neighbourWorldIndex);
                        float neighbourDistance = GetNeighbourDistance(currentWorldIndex, neighbourWorldIndex);
                        int movementPenalty = boxes[neighbour.gridBoxIndex].GetStaticNode(staticNodes, neighbour.gridIndex).movementPenalty;
                        float tentativeGCost = currentNode.gCost + neighbourDistance + movementPenalty;
                        bool containsOpenSet = openSet.Contains(neighbour);

                        if (tentativeGCost < neighbour.gCost || !containsOpenSet)
                        {
                            neighbour.gCost = tentativeGCost;
                            if(!gCostNodes.ContainsKey(neighbour.gridIndex))
                                gCostNodes.Add(neighbour.gridIndex, neighbour.gCost);
                            
                            neighbour.hCost = GetDistance(targetWorldIndex, neighbourWorldIndex);
                            neighbour.gridParentIndex = currentNode.gridIndex;
                            neighbour.gridParentBoxIndex = currentNode.gridBoxIndex;
                            boxes[neighbour.gridBoxIndex].UpdateNode(nodes, neighbour);

                            if (containsOpenSet)
                            {
                                // Debug.Log($"updating item {neighbour.gridIndex}");
                                openSet.UpdateItem(neighbour);
                                continue;
                            }
                            openSet.Add(neighbour);
                        }
                    }
                }
            }
            openSet.Dispose();
            nodes.Dispose();
        }
        
        private bool EquateInt3(int3 a, int3 b)
        {
            return a.x == b.x && a.y == b.y && a.z == b.z;
        }
        
        private void RetracePath(Node startNode, Node endNode, NativeArray<Node> nodes)
        {
            Node currentNode = endNode;

            int3 directionOld = new int3(0, 0, 0);

            int counter = 0;
            while (!EquateInt3(startNode.gridIndex, currentNode.gridIndex) && counter < 1000)
            {
                counter++;
                
                int3 directionNew =  (int3)new float3(
                    currentNode.gridParentIndex.x - currentNode.gridIndex.x, 
                    currentNode.gridParentIndex.y - currentNode.gridIndex.y, 
                    currentNode.gridParentIndex.z - currentNode.gridIndex.z);
                
                GridIndexToWorldPos(currentNode.gridIndex, boxes[currentNode.gridBoxIndex].minPositionWS, out float3 endWorldPos);
                waypoints.Add(endWorldPos);
                
                if ((directionNew.x != directionOld.x) || directionNew.z != directionOld.z)
                {
                    // GridIndexToWorldPos(currentNode.gridIndex, boxes[currentNode.gridBoxIndex].minPositionWS, out float3 endWorldPos);
                    // waypoints.Add(endWorldPos);
                    // GridIndexToWorldPos(currentNode.gridIndex, boxes[currentNode.gridBoxIndex].minPositionWS, out endWorldPos);
                    // waypoints.Add(endWorldPos);
                }

                currentNode = boxes[currentNode.gridParentBoxIndex].GetNode(nodes, currentNode.gridParentIndex);
                directionOld = directionNew;
            }
        }

        private void GetNodeFromWorldPoint(int3 worldPosition, out int3 outIndex, out int boxIndex)
        {
            outIndex = new int3(-1, -1, -1);
            boxIndex = -1;

            for (int i = 0; i < boxes.Length; i++)
            {
                if (IsPointInsideCubeWS(worldPosition, i))
                {
                    Box box = boxes[i];
                    WorldPosToGridIndex(worldPosition, box.minPosition, out outIndex);
                    boxIndex = i;
                    return;
                }
            }
        }

        private void GetClosestWalkableNode(int3 worldPosition, float3 closestPosition, out int3 outIndex, out int boxIndex)
        {
            outIndex = new int3(-1, -1, -1);
            boxIndex = -1;
        
            for (int i = 0; i < boxes.Length; i++)
            {
                float closestDist = float.MaxValue;

                for (int x = -1; x <= 1; x++)
                for (int y = -1; y <= 1; y++)
                for (int z = -1; z <= 1; z++)
                {
                    int3 newIndex = new int3(x, y, z);
                    int3 neighbourIndex = worldPosition + newIndex;
                    int3 neighbourPos = worldPosition + newIndex;
                    if (IsPointInsideCubeWS(neighbourPos, i))
                    {
                        Box box = boxes[i];
                        
                        neighbourIndex -= (int3)(box.position / nodeSize);
                        neighbourIndex = new int3((int)(neighbourIndex.x + box.scale.x / 2 / nodeSize),
                            (int)(neighbourIndex.y + box.scale.y / 2 / nodeSize),
                            (int)(neighbourIndex.z + box.scale.z / 2 / nodeSize));
                        
                        StaticNode neighbourStaticNode = box.GetStaticNode(staticNodes, neighbourIndex.x, neighbourIndex.y, neighbourIndex.z);

                        float dist = math.distance(neighbourIndex, closestPosition);
                        if (neighbourStaticNode.walkable && dist < closestDist)
                        {
                            closestDist = dist;
                            outIndex = neighbourIndex;
                            boxIndex = i;
                        }
                    }
                }
            }
        }
        
        /*
         *  25 26 27
         *  22 23 24  top
         *  19 20 21
         *
         *  16 17 18 
         *  13 14 15  middle
         *  10 11 12
         *
         *  7  8  9
         *  4  5  6  bottom 
         *  1  2  3
         */
        // 0  0  0  0  0  0  0  0  0  0  0  0  0  0  0  0  0  0  0  0  0  0  0  0 0 0 0 0 0 0 0 0
        // 32 31 30 29 28 27 26 25 24 23 22 21 20 19 18 17 16 15 14 13 12 11 10 9 8 7 6 5 4 3 2 1
        private readonly static int[] nodeWalkableMask = { 
            5659   ,1042  ,19510, 
            4120   ,0     ,16432,           //bottom
            102616 ,65680 ,213424,
            
            5120   ,0     ,17408,
            0      ,0     ,0,               //middle
            69632  ,0     ,81920,
            
            7083520  ,4719616  ,14175232,
            6295552  ,0        ,12599296,   //top
            56725504 ,37814272 ,113459200
        };
        
        private readonly static int[] nodeWalkableMask2D = { 
            10  ,0, 34, 
            0   ,0, 0,           //bottom
            136 ,0, 160,
        };
        
        private void GetNeighbours(int3 nodeIndex, int nodeBoxIndex, out int walkableBitMask, ref NativeArray<Node> neighbourNodes, NativeArray<Node> nodes)
        {
            walkableBitMask = 0;
            for (int y = -1; y <= 1; y += 1)
            for (int z = -1; z <= 1; z += 1)
            for (int x = -1; x <= 1; x += 1)
            {
                int index = (x + 1) + (z + 1) * 3 + (y + 1) * 9;
                CheckNode(nodeIndex, nodeBoxIndex, x, y, z, out int3 outIndex, out int outBoxIndex);

                if (index == 13 || outIndex.x == -1)
                {
                    neighbourNodes[index] = Node.empty;
                    continue;
                }
                
                StaticNode staticNode = boxes[outBoxIndex].GetStaticNode(staticNodes, outIndex);
                if (staticNode.walkable)
                {
                    walkableBitMask |= 1 << index;
                    neighbourNodes[index] = boxes[outBoxIndex].GetNode(nodes, outIndex);
                }
                else
                {
                    neighbourNodes[index] = Node.empty;
                }
            }
        }
        private void GetNeighbours2D(int3 nodeIndex, int nodeBoxIndex, out int walkableBitMask, ref NativeArray<Node> neighbourNodes, NativeArray<Node> nodes)
        {
            walkableBitMask = 0;
            int y = 0;
            for (int z = -1; z <= 1; z += 1)
            for (int x = -1; x <= 1; x += 1)
            {
                int index = (x + 1) + (z + 1) * 3;
                CheckNode(nodeIndex, nodeBoxIndex, x, y, z, out int3 outIndex, out int outBoxIndex);

                if (index == 13 || outIndex.x == -1)
                {
                    neighbourNodes[index] = Node.empty;
                    continue;
                }
                
                StaticNode staticNode = boxes[outBoxIndex].GetStaticNode(staticNodes, outIndex);
                if (staticNode.walkable)
                {
                    walkableBitMask |= 1 << index;
                    Node tempNode = boxes[outBoxIndex].GetNode(nodes, outIndex);
                    if (gCostNodes.ContainsKey(nodeIndex + new int3(x, 0, z)))
                    {
                        tempNode.gCost = gCostNodes[nodeIndex + new int3(x, 0, z)];
                    }
                    neighbourNodes[index] = tempNode;
                }
                else
                {
                    neighbourNodes[index] = Node.empty;
                }
            }
        }

        private void CheckNode(int3 nodeIndex, int nodeBoxIndex, int x, int y, int z, out int3 index, out int boxIndex)
        {
            int checkX = nodeIndex.x + x;
            int checkY = nodeIndex.y + y;
            int checkZ = nodeIndex.z + z;
            int3 neighbourIndex = new int3(checkX, checkY, checkZ);
            GridIndexToWorldIndex(neighbourIndex, boxes[nodeBoxIndex].minPosition, out int3 neighbourWorldIndex);
            index = new int3(-1, -1, -1);
            boxIndex = -1;

            for (var i = 0; i < boxes.Length; i++)
            {
                if (IsPointInsideCubeWS(neighbourWorldIndex, i))
                {
                    Box box = boxes[i];
                    WorldPosToGridIndex(neighbourWorldIndex, box.minPosition, out neighbourIndex);
                    index = neighbourIndex;
                    boxIndex = i;
                    return;
                }
            }
        }
        
        private void GridIndexToWorldIndex(int3 gridIndex, int3 minPosition, out int3 worldIndex)
        {
            worldIndex = new int3(
                gridIndex.x + minPosition.x, 
                gridIndex.y + minPosition.y, 
                gridIndex.z + minPosition.z);
        }
        
        private void GridIndexToWorldPos(int3 gridIndex, float3 minPositionWS, out float3 worldPos)
        {
            worldPos = new float3(
                gridIndex.x, 
                gridIndex.y, 
                gridIndex.z) * nodeSize + minPositionWS;
        }
        
        private void WorldPosToGridIndex(int3 worldIndex, int3 minPosition, out int3 index)
        {
            index = new int3(
                worldIndex.x - minPosition.x, 
                worldIndex.y - minPosition.y, 
                worldIndex.z - minPosition.z);
        }
        
        private bool IsPointInsideCubeWS(int3 point, int boxIndex)
        {
            return point.x >= boxes[boxIndex].minPosition.x && point.x <= boxes[boxIndex].maxPosition.x &&
                   point.y >= boxes[boxIndex].minPosition.y && point.y <= boxes[boxIndex].maxPosition.y &&
                   point.z >= boxes[boxIndex].minPosition.z && point.z <= boxes[boxIndex].maxPosition.z;
        }
        
        private readonly static int[] cachedNeighbourDistances = { 17, 14, 10, 0 };
        private float GetNeighbourDistance(int3 pos1, int3 pos2)
        {
            return math.distance(pos1, pos2);
            // return (int)math.round(math.distance(pos1, pos2));
            int amountMatching = 0;
            amountMatching += pos1.x == pos2.x ? 1 : 0;
            amountMatching += pos1.y == pos2.y ? 1 : 0;
            amountMatching += pos1.z == pos2.z ? 1 : 0;
            return cachedNeighbourDistances[amountMatching];
        }

        private float GetDistance(int3 nodeA, int3 nodeB)
        {
            return math.distance(nodeA, nodeB);
            // return (int)math.round(math.distance(nodeA, nodeB));
            int dstX = math.abs(nodeA.x - nodeB.x);
            int dstY = math.abs(nodeA.y - nodeB.y);
            int dstZ = math.abs(nodeA.z - nodeB.z);
    
            int minimum = math.min(math.min(dstX, dstY), dstZ);
            int maximum = math.max(math.max(dstX, dstY), dstZ);

            int tripleAxis = minimum;
            int doubleAxis = dstX + dstY + dstZ - maximum - 2 * minimum;
            int singleAxis = maximum - doubleAxis - tripleAxis;

            return 10 * singleAxis + 14 * doubleAxis + 17 * tripleAxis;
        }
    }
}