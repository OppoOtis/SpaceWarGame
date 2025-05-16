using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockingBoxDebugger : MonoBehaviour
{
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position, transform.lossyScale);
        Gizmos.color = new Color(0, 0, 1, 0.2f);
        Gizmos.DrawCube(transform.position, transform.lossyScale);
    }
}
