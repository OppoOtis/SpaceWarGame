using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public Enemy enemyPrefab;
    private List<Enemy> enemies;

    private void Start()
    {
        enemies = new List<Enemy>();
        SpawnEnemy(Vector3.forward, enemyPrefab);
    }

    private void SpawnEnemy(Vector3 spawnPosition, Enemy enemy)
    {
        Enemy enemyInstance = Instantiate(enemy, spawnPosition, quaternion.identity);
        enemies.Add(enemyInstance);
        enemyInstance.StartState();
    }

    private void Update()
    {
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
}
