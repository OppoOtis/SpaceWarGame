using Astar.MultiThreaded;
using Unity.Mathematics;
using UnityEngine;

public abstract class Enemy : MonoBehaviour
{
    public int maxHealth = 100;
    public float movementSpeed = 1;
    public LayerMask terrainLayerMask;
    
    [HideInInspector] public Vector3 preferredPosition;
    [HideInInspector] public int health;
    
    protected Transform playerTransform;
    
    private float currentT;
    private bool onPath;
    private float3[] path;
    private int currentI;
    private Vector3 cachedPosition;
    
    public virtual void StartState(Transform player)
    {
        playerTransform = player;
        health = maxHealth;
    }

    public virtual void UpdateState()
    {
        if (onPath)
        {
            float dist;
            dist = currentI > 0 ? 
                math.distance(path[currentI], path[currentI - 1]) : 
                math.distance(cachedPosition, path[0]);
            
            currentT += Time.deltaTime / (dist / movementSpeed);
            transform.position = currentI > 0 ? 
                math.lerp(path[currentI], path[currentI - 1], currentT) :
                math.lerp(cachedPosition, path[0], currentT);
            if (currentT > 1)
            {
                if (currentI <= 1)
                {
                    onPath = false;
                    return;
                }
                currentT = 0;
                currentI--;
            }
        }
    }
    protected void GoTo(Vector3 worldPos)
    {
        if (Physics.Raycast(transform.position, worldPos - transform.position, Vector3.Distance(transform.position, worldPos), terrainLayerMask))
        { 
            AStarManager.Instance.Pathfind(transform.position, worldPos, FinishedPathfinding);
        }
        else
        {
            FinishedPathfinding(new[] { (float3)worldPos, (float3)transform.position}, true);
        }
    }

    private void FinishedPathfinding(float3[] path, bool success)
    {
        if (!success)
        {
            Debug.LogWarning($"{gameObject.name} could not find path");
            return;
        }
        if (path.Length <= 1)
        {
            Debug.LogWarning($"{gameObject.name} found path in place");
            return;
        }
        currentT = 0;
        currentI = path.Length - 1;
        cachedPosition = transform.position;
        this.path = path;
        onPath = true;
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
