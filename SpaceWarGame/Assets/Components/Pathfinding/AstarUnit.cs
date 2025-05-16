using System;
using Unity.Mathematics;
using UnityEngine;
using VInspector;
using Random = UnityEngine.Random;

namespace Astar.MultiThreaded
{
    public class AstarUnit : MonoBehaviour
    {
        [SerializeField] private Transform target;

        private float3[] path;
        private Color color;

        private void Awake()
        {
            color = new Color(Random.Range(0.1f, 1), Random.Range(0.1f, 1), Random.Range(0.1f, 1), 1);
        }

        [Button]
        private void Pathfind()
        {
            AStarManager.Instance.Pathfind(transform.position, target.position, FinishedPathfinding);
        }

        private void Update()
        {
            if (Time.frameCount == 10)
            {
                AStarManager.Instance.Pathfind(transform.position, target.position, FinishedPathfinding);
            }
        }

        private void FinishedPathfinding(float3[] path, bool success)
        {
            this.path = path;
        }

        private void OnDrawGizmos()
        {
            if (path == null)
                return;
            if(path.Length == 0)
                return;
            
            Gizmos.color = color;
            for (var i = 0; i < path.Length - 1; i++)
            {
                var pos = path[i];
                Gizmos.color = Color.white;
                Gizmos.DrawLine(path[i], path[i + 1]);

                Gizmos.color = i == 0 ? Color.green : color;
                Gizmos.DrawCube(pos, new Vector3(0.1f, 0.1f, 0.1f));
            }
            Gizmos.color = Color.red;
            Gizmos.DrawCube(path[^1], new Vector3(0.1f, 0.1f, 0.1f));

        }
    }
}