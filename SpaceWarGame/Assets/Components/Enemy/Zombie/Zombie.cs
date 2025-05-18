using System;
using Astar.MultiThreaded;
using Unity.Mathematics;
using UnityEngine;

public class Zombie : Enemy
{
    public override void StartState(Transform player)
    {
        base.StartState(player);
    }
    public override void UpdateState()
    {
        base.UpdateState();
        if (Vector3.Distance(transform.position, preferredPosition) > 0.1f)
        {
            GoTo(preferredPosition);
        }
    }
}
