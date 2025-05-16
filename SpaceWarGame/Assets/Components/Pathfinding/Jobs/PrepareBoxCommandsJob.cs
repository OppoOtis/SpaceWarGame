using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Astar.MultiThreaded
{
    [BurstCompile]
    public struct PrepareBoxCommandsJob : IJobParallelFor
    {
        private float3 nodeScale;
        private float boxSize;
        private int xLength;
        private int zLength;
        private float3 offset;
        private QueryParameters queryParameters;
        private int startIndex;
        [NativeDisableParallelForRestriction] private NativeArray<OverlapBoxCommand> commands;

        public PrepareBoxCommandsJob(float boxSize, int xLength, int zLength, float3 offset, int startIndex, QueryParameters queryParameters, NativeArray<OverlapBoxCommand> commands) : this()
        {
            this.boxSize = boxSize;
            this.xLength = xLength;
            this.zLength = zLength;
            this.offset = offset;
            this.startIndex = startIndex;
            this.commands = commands;
            this.queryParameters = queryParameters;
            nodeScale = new float3(boxSize, boxSize, boxSize) / 2;
        }

        public void Execute(int index)
        {
            int3 gridIndex = new int3(index % xLength, index / (xLength * zLength), index / xLength % zLength);
            float3 pos = (float3)gridIndex * boxSize + offset;
            commands[index + startIndex] = new OverlapBoxCommand(pos, nodeScale, Quaternion.identity, queryParameters);
        }
    }
}