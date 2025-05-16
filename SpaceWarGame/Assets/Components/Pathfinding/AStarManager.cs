using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Astar.MultiThreaded
{
    public class AStarManager : MonoBehaviour
    {
        public static AStarManager Instance { get; private set; }
        [Header("Grid Parameters")] 
        [SerializeField] public float nodeSize = 0.5f;
        
        [SerializeField] private LayerMask unwalkableMask;
        [SerializeField] private LayerMask walkableMask;
        [SerializeField] private TerrainType[] walkableRegions;
        [SerializeField] private int obstacleProximityPenalty = 15;
        [SerializeField] private int amountBlurPasses = 1;

#if UNITY_EDITOR
        [Header("Debug")]
        [SerializeField] public bool alwaysDrawDebug = true;

        [SerializeField] private Mesh debugMesh;
        [SerializeField] private Material debugPathfindingMat;
        [SerializeField, Range(0f, 1f)] private float amountDebugPathfindingNodes;
        [SerializeField] public bool drawDebugGrid = true;
        [SerializeField] private Material debugGridMat;
#endif

        private Transform[] boxes;
        private GridManager gridManager;
        private RequestManager requestManager;

        void Start()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            //Multiply it by twenty seven so it becomes the same value before multiplying after grid blurring
            obstacleProximityPenalty *= 27; 
            boxes = new Transform[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
            {
                boxes[i] = transform.GetChild(i);
            }

            gridManager = new GridManager(unwalkableMask, walkableRegions, obstacleProximityPenalty, amountBlurPasses, boxes, nodeSize);
            requestManager = new RequestManager();
        }

        private void Update()
        {
            gridManager.CheckComplete();
            requestManager.CheckComplete();
#if UNITY_EDITOR
            if(drawDebugGrid)
			{
                requestManager.DrawDebugNodes(debugMesh, debugPathfindingMat, amountDebugPathfindingNodes);
                gridManager.DrawDebugGrid(debugMesh, debugGridMat);
			}
#endif
        }

        public void Pathfind(Vector3 startPos, Vector3 endPos, Action<float3[], bool> callBack)
        {
            PathfindJob pathfindJob = new PathfindJob(gridManager.boxes.AsReadOnly(), gridManager.nodes.AsReadOnly(), gridManager.staticNodes.AsReadOnly(), 
                startPos, endPos, nodeSize, gridManager.maxSize);
            gridManager.gridHandle = pathfindJob.Schedule(gridManager.gridHandle);
            
            requestManager.Enqueue(gridManager.gridHandle, pathfindJob, callBack);
        }

        public void UpdateGrid(Vector3 min, Vector3 max)
        {
            gridManager.UpdateGrid(min, max);
        }
        
        public void UpdateGridFast(Vector3 min, Vector3 max, bool walkable)
        {
            gridManager.UpdateGridFast(min, max, walkable);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (alwaysDrawDebug)
            {
                DrawGizmos();
                return;
            }
            
            bool selectedAnObject = false;
            foreach (var o in Selection.objects)
            {
                if (o is GameObject selectedObject)
                {
                    if (selectedObject == gameObject || selectedObject.transform.parent == transform)
                    {
                        selectedAnObject = true;
                        break;
                    }
                }
            }
            if (selectedAnObject)
            {
                DrawGizmos();
            }
        }

        private void DrawGizmos()
        {
            List<Object> selectedObjects = Selection.objects.ToList();
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform boxTransform = transform.GetChild(i);
                if (selectedObjects.Contains(boxTransform.gameObject))
                {
                    DrawBoxes(boxTransform, Color.blue);
                    continue;
                }
                
                DrawBoxes(boxTransform, Color.red);
            }

            void DrawBoxes(Transform boxTransform, Color color)
            {
                float3 pos = math.round(boxTransform.position / nodeSize) * nodeSize;
                float3 scale = math.round(boxTransform.lossyScale / (nodeSize * 2)) * (nodeSize * 2);

                Gizmos.color = color;
                Gizmos.DrawWireCube(pos, scale + new float3(nodeSize, nodeSize, nodeSize));
                color.a = 0.1f;
                Gizmos.color = color;
                Gizmos.DrawCube(pos, scale + new float3(nodeSize, nodeSize, nodeSize));
            }
        }
#endif

        private void OnDisable()
        {
            requestManager?.Dispose();
            gridManager?.Dispose();
        }
    }

    [Serializable]
    public class TerrainType
    {
        public LayerMask terrainMask;
        public int terrainPenalty;
    }
}