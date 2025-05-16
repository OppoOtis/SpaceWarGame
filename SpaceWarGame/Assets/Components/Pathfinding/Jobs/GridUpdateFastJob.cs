using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Astar.MultiThreaded
{
    [BurstCompile]
    public struct GridUpdateFastJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] private NativeArray<StaticNode> staticNodes;
        
        private Box box;
        private int3 offset;
        private bool walkable;
        private int xLength;
        private int zLength;

        public GridUpdateFastJob(Box box, NativeArray<StaticNode> staticNodes, int xLength, int zLength, int3 offset, bool walkable) : this()
        {
            this.staticNodes = staticNodes;
            this.box = box;
            this.xLength = xLength;
            this.zLength = zLength;
            this.offset = offset;
            this.walkable = walkable;
        }

        public void Execute(int index)
        {
            int3 gridIndex = new int3(index % xLength, index / (xLength * zLength), index / xLength % zLength) + offset;
            StaticNode staticNode = box.GetStaticNode(staticNodes.AsReadOnly(), gridIndex);
            staticNode.walkable = walkable;
            box.UpdateStaticNode(staticNodes, gridIndex, staticNode);
        }
    }
}