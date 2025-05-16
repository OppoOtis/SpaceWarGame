using Unity.Mathematics;
using UnityEngine;
using VInspector;

namespace Astar.MultiThreaded
{
    public class GridUpdater : MonoBehaviour
    {
        [Header("Referencing the AStarManager is only needed for editor gizmos")]
        [SerializeField] private AStarManager aStarManager;
        [SerializeField] private bool setWalkable;

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!aStarManager)
                return;

            if (aStarManager.alwaysDrawDebug)
            {
                DrawGizmos();
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!aStarManager)
                return;

            if (!aStarManager.alwaysDrawDebug)
            {
                DrawGizmos();
            }
        }

        private void DrawGizmos()
        {
            var (center, scale) = GetCenterScaleOnNodeGrid();
            
            Gizmos.color = new Color(0.5f, 0, 0.8f, 0.3f);
            Gizmos.DrawCube(center, scale);
            Gizmos.color = new Color(0.5f, 0, 0.8f, 1);
            Gizmos.DrawWireCube(center, scale);
        }
#endif


        [Button]
        public void UpdateGrid()
        {
            var (center, scale) = GetCenterScaleOnNodeGrid();
            
            AStarManager.Instance.UpdateGrid(center - scale / 2, center + scale / 2);
        }
        
        [Button]
        public void UpdateGridFast()
        {
            var (center, scale) = GetCenterScaleOnNodeGrid();

            AStarManager.Instance.UpdateGridFast(center - scale / 2, center + scale / 2, setWalkable);
        }

        private (float3 center, float3 scale) GetCenterScaleOnNodeGrid()
        {
            float3 min = transform.position - transform.lossyScale / 2;
            float3 max = transform.position + transform.lossyScale / 2;
            float3 center = math.round((min + max) / 2 / aStarManager.nodeSize) * aStarManager.nodeSize;
            float3 scale = math.round((max - min) / (aStarManager.nodeSize * 1)) * (aStarManager.nodeSize * 2) + aStarManager.nodeSize;
            return (center, scale);
        }
    }
}