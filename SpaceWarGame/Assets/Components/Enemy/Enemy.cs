using UnityEngine;

public abstract class Enemy : MonoBehaviour
{
    public int maxHealth = 100;
    
    [HideInInspector] public int health;
    
    public virtual void StartState()
    {
        health = maxHealth;
    }
    
    public virtual void UpdateState(){}
}
