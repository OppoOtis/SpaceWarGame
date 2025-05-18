using System;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Astar.MultiThreaded
{
    public class RequestManager
    {
        private Queue<PathfindRequest> pathfindRequestsQueue;
        public RequestManager()
        {
            pathfindRequestsQueue = new Queue<PathfindRequest>();
        }

        public void Enqueue(JobHandle jobHandle, PathfindJob pathfindJob, Action<float3[], bool> callBack)
        {
            PathfindRequest pathfindRequest = new PathfindRequest(jobHandle, pathfindJob, callBack, Time.frameCount);
            pathfindRequestsQueue.Enqueue(pathfindRequest);
        }

        // Jobs use JobTemp memory which shouldnt last more then 4 frames to prevent memory leaks
        // Check if the first enqueued job lasted more then 4 frames if so pause mainthread and wait for the job to complete
        public void CheckComplete()
        {
            if(pathfindRequestsQueue.Count == 0)
                return;
            
            if (pathfindRequestsQueue.Peek().jobHandle.IsCompleted)
            {
                FinishJob();
                CheckComplete();
            }
            else if (pathfindRequestsQueue.Peek().frameCount + 3 <= Time.frameCount)
            {
                Debug.LogWarning($"stopping A* job early pausing mainthread");
                FinishJob();
                CheckComplete();
            }

            void FinishJob()
            {
                PathfindRequest pathfindRequest = pathfindRequestsQueue.Dequeue();
                pathfindRequest.jobHandle.Complete();
#if UNITY_EDITOR
                debugPathfinding = new Matrix4x4[pathfindRequest.pathfindJob.debugNodes.Length];
                for (int i = 0; i < debugPathfinding.Length; i++)
                {
                    debugPathfinding[i] = Get_TRS_Matrix(pathfindRequest.pathfindJob.debugNodes[i].pos, new float3(0.5f, 0.5f, 0.5f));
                }
                pathfindRequest.pathfindJob.debugNodes.Dispose();
#endif
                float3[] path = new float3[pathfindRequest.pathfindJob.waypoints.Length];
                for (int i = 0; i < pathfindRequest.pathfindJob.waypoints.Length; i++)
                {
                    path[i] = pathfindRequest.pathfindJob.waypoints[i];
                }
                
                bool pathSuccess = pathfindRequest.pathfindJob.pathSuccess[0];
                
                pathfindRequest.pathfindJob.waypoints.Dispose();
                pathfindRequest.pathfindJob.pathSuccess.Dispose();
// #if UNITY_EDITOR
//                 pathfindRequest.pathfindJob.debugNodes.Dispose();
// #endif
                // pathfindRequest.pathfindJob.closedSet.Dispose();
                pathfindRequest.pathfindJob.gCostNodes.Dispose();
                pathfindRequest.callBack.Invoke(path, pathSuccess);
            }
        }

        public void Dispose()
        {
            if(pathfindRequestsQueue.Count == 0)
                return;

            PathfindRequest pathfindRequest = pathfindRequestsQueue.Dequeue();
            pathfindRequest.jobHandle.Complete();
            pathfindRequest.pathfindJob.waypoints.Dispose();
            pathfindRequest.pathfindJob.pathSuccess.Dispose();
#if UNITY_EDITOR
            pathfindRequest.pathfindJob.debugNodes.Dispose();
#endif
            pathfindRequest.pathfindJob.gCostNodes.Dispose();
            // pathfindRequest.pathfindJob.closedSet.Dispose();
                
            Dispose();
        }

#if UNITY_EDITOR
        private Matrix4x4[] debugPathfinding;
        public void DrawDebugNodes(Mesh mesh, Material material, float t)
        {
            if (debugPathfinding == null)
            {
                return;
            }
            
            int count = (int)(debugPathfinding.Length * t);
            if(mesh != null)
                Graphics.DrawMeshInstanced(mesh, 0, material, debugPathfinding, count);
        }
        
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

        private Matrix4x4 Get_TRS_Matrix(float3 position,  float3 scale) 
        {
            return GetTranslationMatrix(position) * GetScaleMatrix(scale);
        }
#endif
    }

    struct PathfindRequest
    {
        public JobHandle jobHandle;
        public PathfindJob pathfindJob;
        public Action<float3[], bool> callBack;
        public int frameCount;

        public PathfindRequest(JobHandle jobHandle, PathfindJob pathfindJob, Action<float3[], bool> callBack, int frameCount)
        {
            this.jobHandle = jobHandle;
            this.pathfindJob = pathfindJob;
            this.callBack = callBack;
            this.frameCount = frameCount;
        }
    }
}