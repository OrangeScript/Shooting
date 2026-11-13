
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

    private IEnumerator Start()
    {
        if (enemyPool == null)
        {
            Debug.LogError("EnemySpawner 没有绑定 EnemyPool！");
            yield break;
        }

        while (true)
        {
            for (int i = 0; i < enemiesPerWave; i++)
            {
                if (enemyPool.ActiveCount >= maxActiveEnemies)
                    break;

                float x = Random.Range(-halfWidth, halfWidth);

                Vector3 position = new Vector3(
                    x,
                    spawnY,
                    spawnZ
                );

                enemyPool.Spawn(position, Quaternion.Euler(0,180,0));
            }

            yield return new WaitForSeconds(
                Mathf.Max(0.1f, spawnInterval)
            );
        }
    }
}
