using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public enum WaveState { Waiting, Active, Completed }

    [Header("Enemigos")]
    public EnemyController enemyPrefab;
    public Transform[] spawnPoints;

    [Header("Configuración de oleada")]
    public int enemiesPerWave = 5;
    [Tooltip("Tiempo entre cada spawn individual dentro de la oleada (para no aparecer todos juntos).")]
    public float spawnInterval = 1.5f;
    [Tooltip("Si es true, la oleada arranca sola al iniciar la escena tras 'autoStartDelay'. Si es false, hay que llamar StartWave() manualmente (ej. por zona o evento de progreso).")]
    public bool autoStart = true;
    public float autoStartDelay = 3f;

    public WaveState CurrentState { get; private set; } = WaveState.Waiting;

    private readonly List<Health> aliveEnemies = new List<Health>();

    private void Start()
    {
        if (autoStart)
            Invoke(nameof(StartWave), autoStartDelay);
    }

    public void StartWave()
    {
        if (CurrentState == WaveState.Active) return;
        StartCoroutine(SpawnWaveRoutine());
    }

    private IEnumerator SpawnWaveRoutine()
    {
        CurrentState = WaveState.Active;
        aliveEnemies.Clear();

        for (int i = 0; i < enemiesPerWave; i++)
        {
            SpawnOne();
            yield return new WaitForSeconds(spawnInterval);
        }

        yield return new WaitUntil(() => aliveEnemies.Count == 0);

        CurrentState = WaveState.Completed;
    }

    private void SpawnOne()
    {
        if (enemyPrefab == null || spawnPoints.Length == 0) return;

        Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
        EnemyController enemy = Instantiate(enemyPrefab, point.position, Quaternion.identity);

        Health health = enemy.GetComponent<Health>();
        aliveEnemies.Add(health);
        health.OnDeath += () => aliveEnemies.Remove(health);
    }
}