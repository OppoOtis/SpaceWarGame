using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Astar.MultiThreaded
{
    public enum GridDimension
    {
        X,
        Y,
        Z
    }
    [BurstCompile]
    public struct GridBlurJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] private NativeArray<StaticNode> staticNodes;
        private NativeArray<StaticNode>.ReadOnly staticNodesCopy;
        private NativeArray<Box>.ReadOnly boxes;
        private Box box;
        private GridDimension gridDimension;
        private int obstacleProximityPenalty;

        public GridBlurJob(NativeArray<StaticNode> staticNodes, NativeArray<StaticNode>.ReadOnly staticNodesCopy, NativeArray<Box>.ReadOnly boxes, Box box, GridDimension gridDimension, int obstacleProximityPenalty)
        {
            this.staticNodes = staticNodes;
            this.staticNodesCopy = staticNodesCopy;
            this.boxes = boxes;
            this.box = box;
            this.gridDimension = gridDimension;
            this.obstacleProximityPenalty = obstacleProximityPenalty;
        }

        public void Execute(int index)
        {
            int3 gridIndex = new int3(index % box.xLength, index / (box.xLength * box.zLength), index / box.xLength % box.zLength);

            int averageMomvement = 0;
            StaticNode staticNode = box.GetStaticNode(staticNodesCopy, gridIndex);
            averageMomvement += staticNode.movementPenalty;
            
            int3 neighbourGridIndex;
            int neighbourBoxIndex;
            switch (gridDimension)
            {
                case GridDimension.X :
                    CheckNode(gridIndex, -1, 0, 0, out neighbourGridIndex, out neighbourBoxIndex);
                    if (neighbourBoxIndex != -1)
                    {
                        averageMomvement += boxes[neighbourBoxIndex].GetStaticNode(staticNodesCopy, neighbourGridIndex).movementPenalty;
                    }
                    else
                    {
                        averageMomvement += obstacleProximityPenalty;
                    }

                    CheckNode(gridIndex, 1, 0, 0, out neighbourGridIndex, out neighbourBoxIndex);
                    if (neighbourBoxIndex != -1)
                    {
                        averageMomvement += boxes[neighbourBoxIndex].GetStaticNode(staticNodesCopy, neighbourGridIndex).movementPenalty;
                    }
                    else
                    {
                        averageMomvement += obstacleProximityPenalty;
                    }
                    break;
                case GridDimension.Y :
                    CheckNode(gridIndex, 0, -1, 0, out neighbourGridIndex, out neighbourBoxIndex);
                    if (neighbourBoxIndex != -1)
                    {
                        averageMomvement += boxes[neighbourBoxIndex].GetStaticNode(staticNodesCopy, neighbourGridIndex).movementPenalty;
                    }
                    else
                    {
                        averageMomvement += obstacleProximityPenalty;
                    }
                    CheckNode(gridIndex, 0, 1, 0, out neighbourGridIndex, out neighbourBoxIndex);
                    if (neighbourBoxIndex != -1)
                    {
                        averageMomvement += boxes[neighbourBoxIndex].GetStaticNode(staticNodesCopy, neighbourGridIndex).movementPenalty;
                    }
                    else
                    {
                        averageMomvement += obstacleProximityPenalty;
                    }
                    break;
                case GridDimension.Z :
                    CheckNode(gridIndex, 0, 0, -1, out neighbourGridIndex, out neighbourBoxIndex);
                    if (neighbourBoxIndex != -1)
                    {
                        averageMomvement += boxes[neighbourBoxIndex].GetStaticNode(staticNodesCopy, neighbourGridIndex).movementPenalty;
                    }
                    else
                    {
                        averageMomvement += obstacleProximityPenalty;
                    }
                    CheckNode(gridIndex, 0, 0, 1, out neighbourGridIndex, out neighbourBoxIndex);
                    if (neighbourBoxIndex != -1)
                    {
                        averageMomvement += boxes[neighbourBoxIndex].GetStaticNode(staticNodesCopy, neighbourGridIndex).movementPenalty;
                    }
                    else
                    {
                        averageMomvement += obstacleProximityPenalty;
                    }
                    break;
            }

            averageMomvement /= 3;
            staticNode.movementPenalty = averageMomvement;
            box.UpdateStaticNode(staticNodes, gridIndex, staticNode);
        }
        
        private void CheckNode(int3 nodeIndex, int x, int y, int z, out int3 index, out int boxIndex)
        {
            int checkX = nodeIndex.x + x;
            int checkY = nodeIndex.y + y;
            int checkZ = nodeIndex.z + z;
            int3 neighbourIndex = new int3(checkX, checkY, checkZ);
            GridIndexToWorldIndex(neighbourIndex, box.minPosition, out int3 neighbourWorldIndex);
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
        
        private bool IsPointInsideCubeWS(int3 point, int boxIndex)
        {
            return point.x >= boxes[boxIndex].minPosition.x && point.x <= boxes[boxIndex].maxPosition.x &&
                   point.y >= boxes[boxIndex].minPosition.y && point.y <= boxes[boxIndex].maxPosition.y &&
                   point.z >= boxes[boxIndex].minPosition.z && point.z <= boxes[boxIndex].maxPosition.z;
        }
        
        private void WorldPosToGridIndex(int3 worldIndex, int3 minPosition, out int3 index)
        {
            index = new int3(
                worldIndex.x - minPosition.x, 
                worldIndex.y - minPosition.y, 
                worldIndex.z - minPosition.z);
        }
    }
}