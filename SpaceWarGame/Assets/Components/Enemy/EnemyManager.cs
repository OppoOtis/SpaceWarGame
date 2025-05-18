using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public List<Transform> spawnPositions;
    public Enemy enemyPrefab;
    public PlayerController player;
    public float meleePreferredRange = 5;
    public int amountAngles;
    
    private List<Enemy> enemies;

    private void Start()
    {
        enemies = new List<Enemy>();
    }
    
    private void SpawnEnemy(Vector3 spawnPosition, Enemy enemy)
    {
        Enemy enemyInstance = Instantiate(enemy, spawnPosition, quaternion.identity);
        enemies.Add(enemyInstance);
        enemyInstance.StartState(player.transform);
    }

    private void Update()
    {
        if (Time.frameCount == 15)
        {
            foreach (Transform spawnPosition in spawnPositions)
            {
                SpawnEnemy(spawnPosition.position, enemyPrefab);
            }
        }
        
        PreferredMeleePositions(enemies);
        
        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            
            enemy.UpdateState();
            
            if (enemy.health <= 0)
            {
                enemies.RemoveAtSwapBack(i);
                i--;
            }
        }
    }
    
    private void PreferredMeleePositions(List<Enemy> meleeEnemies)
    {
        //Amount Angles cant be larger then 32 because of the bitmask
        // int amountAngles = 12;
        float angleBetweenEnemies = 360f / amountAngles;
        uint bitmaskAnglesTaken = 0;
        for (int i = 0; i < meleeEnemies.Count; i++)
        {
            Enemy enemy = meleeEnemies[i];
            Vector3 direction = enemy.transform.position - player.transform.position;
            float angle = Vector3.SignedAngle(Vector3.forward, direction, Vector3.up);
            if (angle < 0f) angle += 360f;

            int flipped = 1;
            int takenAngle = -1;
            for (int j = 0; j < amountAngles; j = (j * -1) + (1 * flipped))
            {
                flipped = 1 - flipped;
                int closestAngle = (Mathf.RoundToInt(angle / angleBetweenEnemies) + j) % amountAngles;
                if ((bitmaskAnglesTaken & 1 << closestAngle) == 0)
                {
                    bitmaskAnglesTaken |= (uint)1 << closestAngle;
                    takenAngle = closestAngle;
                    break;
                }
            }
            
            if (takenAngle != -1)
            {
                float angleNew = takenAngle * angleBetweenEnemies;
                Vector3 directionNew = new Vector3(Mathf.Sin(angleNew * Mathf.Deg2Rad), 0f, Mathf.Cos(angleNew * Mathf.Deg2Rad));
                enemy.preferredPosition = player.transform.position + directionNew * meleePreferredRange;
            }
        }
    }
}
