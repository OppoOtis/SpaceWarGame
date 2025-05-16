using System;
using Astar.MultiThreaded;
using Unity.Mathematics;
using UnityEngine;

public class Zombie : Enemy
{
    private float3[] path;
    public void Update()
    {
        AStarManager.Instance.Pathfind(transform.position, playerTransform.position, FinishedPathfinding);
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
            
        for (var i = 0; i < path.Length - 1; i++)
        {
            var pos = path[i];
            Gizmos.color = Color.white;
            Gizmos.DrawLine(path[i], path[i + 1]);

            Gizmos.color = Color.green;
            Gizmos.DrawCube(pos, new Vector3(0.1f, 0.1f, 0.1f));
        }
        Gizmos.color = Color.red;
        Gizmos.DrawCube(path[^1], new Vector3(0.1f, 0.1f, 0.1f));

    }
}
