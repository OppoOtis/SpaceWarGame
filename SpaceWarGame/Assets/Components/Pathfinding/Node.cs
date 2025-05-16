using System;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Mathematics;

namespace Astar.MultiThreaded
{
    [BurstCompile]
    public struct Node : IComparable<Node>
    {
        readonly public static Node empty = new Node(new int3(-1, -1, -1), -1);
        
        public int3 gridIndex;
        public int3 gridParentIndex;
        public int gridBoxIndex;
        public int gridParentBoxIndex;

        public float gCost;
        public float hCost;
        public float fCost => gCost + hCost; 
        
        public int heapIndex;
        
        [MarshalAs(UnmanagedType.U1)]
        public bool locked;

        public Node(int3 gridIndex, int gridBoxIndex)
        {
            this.gridIndex = gridIndex;
            this.gridBoxIndex = gridBoxIndex;
            gridParentIndex = new int3(0, 0, 0);
            gridParentBoxIndex = 0;
            gCost = 10;
            hCost = 0;
            locked = false;
            heapIndex = 0;
        }
        
        [BurstCompile]
        public int CompareTo(Node other)
        {
            return fCost >= other.fCost ? 0 : 1;
        }
        
        [BurstCompile]
        public bool Equals(Node other)
        {
            return gridIndex.x == other.gridIndex.x && gridIndex.y == other.gridIndex.y && gridIndex.z == other.gridIndex.z;
        }
    }
    
    [BurstCompile]
    public struct NodeDebug
    {
        public float3 pos;
        public int index;

        public NodeDebug(float3 pos, int index)
        {
            this.pos = pos;
            this.index = index;
        }
    }
}