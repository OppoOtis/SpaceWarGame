using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Astar.MultiThreaded
{
    [BurstCompile]
    [Serializable]
    public struct Box
    {
        readonly public float3 minPositionWS;
        readonly public float3 maxPositionWS;
        readonly public int3 minPosition;
        readonly public int3 maxPosition;
        readonly public float3 position;
        readonly public float3 scale;
        readonly public int startIndex;
        readonly public int xLength;
        readonly public int yLength;
        readonly public int zLength;
        readonly public int YMultiplier => xLength * zLength;
        readonly public int GridSize => xLength * yLength * zLength;
        
        public Box(int startIndex, float3 position, float3 minPositionWS, float3 maxPositionWS, float3 scale, int xLength, int yLength, int zLength, float nodeSize)
        {
            this.startIndex = startIndex;
            this.position = position;
            this.scale = scale;
            this.xLength = xLength;
            this.yLength = yLength;
            this.zLength = zLength;
            this.minPositionWS = minPositionWS;
            this.maxPositionWS = maxPositionWS;
            minPosition = (int3)(minPositionWS / nodeSize);
            maxPosition = (int3)(maxPositionWS / nodeSize);
        }

        public Node GetNode(NativeArray<Node> grid, int x, int y, int z)
        {
            return grid[x + z * xLength + y * YMultiplier + startIndex];
        }
        
        public Node GetNode(NativeArray<Node> grid, int3 index)
        {
            return grid[index.x + index.z * xLength + index.y * YMultiplier + startIndex];
        }
        
        public StaticNode GetStaticNode(NativeArray<StaticNode>.ReadOnly grid, int x, int y, int z)
        {
            return grid[x + z * xLength + y * YMultiplier + startIndex];
        }
        
        public StaticNode GetStaticNode(NativeArray<StaticNode>.ReadOnly grid, int3 index)
        {
            return grid[index.x + index.z * xLength + index.y * YMultiplier + startIndex];
        }

        public void UpdateNode(NativeArray<Node> grid, Node node)
        {
            grid[node.gridIndex.x + node.gridIndex.z * xLength + node.gridIndex.y * YMultiplier + startIndex] = node;
        }
        
        public void UpdateStaticNode(NativeArray<StaticNode> grid, int3 gridIndex, StaticNode node)
        {
            grid[gridIndex.x + gridIndex.z * xLength + gridIndex.y * YMultiplier + startIndex] = node;
        }

        public bool IsPointInsideBox(float3 pos)
        {
            return minPositionWS.x > pos.x && pos.x < maxPositionWS.x &&
                   minPositionWS.y > pos.y && pos.y < maxPositionWS.y &&
                   minPositionWS.z > pos.z && pos.z < maxPositionWS.z;
        }
    }
}