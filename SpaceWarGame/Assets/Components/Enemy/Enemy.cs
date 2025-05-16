using UnityEngine;

public abstract class Enemy : MonoBehaviour
{
    public int maxHealth = 100;
    
    [HideInInspector] public int health;
    protected Transform playerTransform;
    
    public virtual void StartState(Transform player)
    {
        playerTransform = player;
        health = maxHealth;
    }
    
    public virtual void UpdateState(){}
}
