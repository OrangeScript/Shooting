
using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private EnemyPool enemyPool;

    [Header("生成范围")]
    [SerializeField] private float spawnZ = 20f;
    [SerializeField] private float spawnY = 0f;
    [SerializeField] private float halfWidth = 4f;

    [Header("生成规则")]
    [SerializeField] private float spawnInterval = 1f;
    [SerializeField] private int enemiesPerWave = 2;
    [SerializeField] private int maxActiveEnemies = 25;
    [SerializeField] private bool autoStart = true;

    public bool CanSpawn => isActiveAndEnabled && enemyPool != null &&
                            enemyPool.ActiveCount < maxActiveEnemies;
    public Enemy Prefab => enemyPool != null ? enemyPool.Prefab : null;

    private IEnumerator Start()
    {
        if (!autoStart)
            yield break;

        var interval = new WaitForSeconds(Mathf.Max(0.1f, spawnInterval));

        while (isActiveAndEnabled)
        {
            SpawnBatch();
            yield return interval;
        }
    }

    public void SpawnBatch() => SpawnBatch(default);

    public void SpawnBatch(EnemyStats stats)
    {
        if (!CanSpawn)
            return;

        for (int i = 0; i < enemiesPerWave; i++)
        {
            if (enemyPool.ActiveCount >= maxActiveEnemies)
                break;

            float x = Random.Range(-halfWidth, halfWidth);
            var position = new Vector3(x, spawnY, spawnZ);

            enemyPool.Spawn(position, Quaternion.Euler(0f, 180f, 0f), stats: stats);
        }
    }

    public void SpawnTeamBatch(EnemyStats stats, int teamSize, int activeCount,
        float centerX, float teamHalfWidth, float extraDepth,
        System.Action<Vector3, Quaternion, EnemyStats> spawn)
    {
        int count = Mathf.Clamp(maxActiveEnemies * teamSize - activeCount,
            0, enemiesPerWave * teamSize);
        for (int i = 0; i < count; i++)
        {
            float laneWidth = 2f * teamHalfWidth / count;
            float x = centerX - teamHalfWidth + (i + Random.value) * laneWidth;
            float z = spawnZ + Random.Range(0f, extraDepth);
            spawn(new Vector3(x, spawnY, z), Quaternion.Euler(0f, 180f, 0f), stats);
        }
    }
}
