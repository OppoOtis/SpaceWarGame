using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Mathematics;

namespace Astar.MultiThreaded
{
    [BurstCompile]
    public struct StaticNode
    {
        readonly public static StaticNode empty = new StaticNode(0, false);
        public int movementPenalty;
        [MarshalAs(UnmanagedType.U1)]
        public bool walkable;

        public StaticNode(int movementPenalty, bool walkable)
        {
            this.movementPenalty = movementPenalty;
            this.walkable = walkable;
        }
    }
}