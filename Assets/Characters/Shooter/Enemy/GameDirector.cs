using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameDirector : MonoBehaviour
{
    [System.Serializable]
    private class BossWave
    {
        public int wave;
        public string title;
        public EnemyPool pool;
        [Min(1)] public int health = 30;
    }

    [System.Serializable]
    private class EnemySpawn
    {
        public EnemySpawner spawner;
        [Range(1, 20)] public int firstWave = 1;
        [Min(1)] public int weight = 1;
    }

    private enum State { Playing, Dying, Defeat, Victory }

    [Header("本局")]
    [SerializeField] private ShooterPlayerHealth player;
    [SerializeField] private ShooterInventory inventory;
    [SerializeField] private EnemySpawn[] enemySpawns;
    [SerializeField, Range(1, 20)] private int waveCount = 20;
    [SerializeField, Min(1f)] private float firstWaveSeconds = 20f;
    [SerializeField, Min(1f)] private float lastWaveSeconds = 60f;

    [Header("奖励门")]
    [SerializeField] private Enemy doorPrefab;

    [Header("危险与 Boss")]
    [SerializeField] private RoadHazard spikePrefab;
    [SerializeField] private RoadHazard pitPrefab;
    [SerializeField] private Transform bossSpawnPoint;
    [SerializeField] private BossWave[] bosses;

    [Header("界面")]
    [SerializeField] private ShooterRunHUD hud;

    private State state;
    private bool restarting;
    private Enemy activeBoss;
    private bool bossKilled;

    private static List<int> pendingItems;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPendingItems()
    {
        pendingItems = null;
    }

    private IEnumerator Start()
    {
        if (player == null || inventory == null || inventory.Database == null ||
            enemySpawns == null || enemySpawns.Length == 0 ||
            doorPrefab == null || doorPrefab.GetComponent<EnemyRewardDrop>() == null)
        {
            Debug.LogError("请配置玩家、道具数据库、怪物生成和带掉落组件的 Door。", this);
            yield break;
        }

        Time.timeScale = 1f;
        hud?.ResetRun();

        List<int> carried = pendingItems;
        pendingItems = null;

        if (carried != null)
            inventory.Core.Restore(carried);

        player.Died += OnPlayerDied;

        for (int wave = 1; wave <= waveCount; wave++)
        {
            yield return PlayCombat(wave);
            ClearThreats();

            BossWave boss = FindBoss(wave);
            if (boss != null)
            {
                yield return FightBoss(wave, boss);

                if (state != State.Playing)
                    yield break;
            }

            yield return PlayRewardRows(wave);
        }

        if (state == State.Playing && !player.IsDead)
            ShowResult(true);
    }

    private IEnumerator PlayCombat(int wave)
    {
        float progress = (wave - 1) / 19f;
        float duration = Mathf.Lerp(firstWaveSeconds, lastWaveSeconds, progress);
        float endTime = Time.time + duration;
        float nextEnemy = Time.time;
        float nextHazard = Time.time + 5f;
        float spawnInterval = Mathf.Lerp(1.8f, 0.9f, progress);
        int lastSecond = -1;

        while (Time.time < endTime)
        {
            float now = Time.time;
            int seconds = Mathf.CeilToInt(endTime - now);
            if (seconds != lastSecond)
            {
                lastSecond = seconds;
                SetStatus($"第 {wave}/{waveCount} 波  ·  {seconds}s");
            }

            if (now >= nextEnemy)
            {
                SpawnEnemies(wave);
                nextEnemy = now + spawnInterval;
            }

            if (wave >= 6 && now >= nextHazard && endTime - now > 6f)
            {
                SpawnHazard(wave);
                nextHazard = now + Random.Range(9f, 14f);
            }

            yield return null;
        }
    }

    private void SpawnEnemies(int wave)
    {
        int totalWeight = 0;
        foreach (EnemySpawn entry in enemySpawns)
        {
            if (CanSpawn(entry, wave))
                totalWeight += entry.weight;
        }

        if (totalWeight == 0)
            return;

        int roll = Random.Range(0, totalWeight);
        foreach (EnemySpawn entry in enemySpawns)
        {
            if (!CanSpawn(entry, wave))
                continue;

            roll -= entry.weight;
            if (roll < 0)
            {
                entry.spawner.SpawnBatch();
                return;
            }
        }
    }

    private static bool CanSpawn(EnemySpawn entry, int wave)
    {
        return entry != null && entry.spawner != null && wave >= entry.firstWave &&
               entry.weight > 0 && entry.spawner.CanSpawn;
    }

    private void SpawnHazard(int wave)
    {
        bool usePit = wave >= 11 && pitPrefab != null && Random.value < 0.35f;
        RoadHazard prefab = usePit ? pitPrefab : spikePrefab;

        if (prefab == null)
            return;

        float x = Random.Range(0, 3) switch
        {
            0 => -2.5f,
            1 => 0f,
            _ => 2.5f
        };

        Instantiate(prefab, new Vector3(x, 0f, 12f), Quaternion.identity);
    }

    private BossWave FindBoss(int wave)
    {
        if (bosses == null)
            return null;

        foreach (BossWave entry in bosses)
        {
            if (entry != null && entry.wave == wave)
                return entry;
        }

        return null;
    }

    private IEnumerator FightBoss(int wave, BossWave boss)
    {
        if (bossSpawnPoint == null || boss.pool == null)
        {
            Debug.LogError($"请配置第 {wave} 波的 Boss 池和出生点。", this);
            ShowResult(false);
            yield break;
        }

        bossKilled = false;
        activeBoss = boss.pool.Spawn(
            bossSpawnPoint.position, bossSpawnPoint.rotation, boss.health);
        activeBoss.Killed += OnBossKilled;
        hud?.SetBoss(activeBoss);
        SetStatus($"第 {wave}/{waveCount} 波  ·  {boss.title}");

        yield return new WaitUntil(() => bossKilled ||
            activeBoss == null || !activeBoss.gameObject.activeInHierarchy);

        Enemy defeatedBoss = activeBoss;
        UnbindBoss();
        hud?.SetBoss(null);

        if (!bossKilled)
        {
            Debug.LogError("Boss 未被击杀就离场了，请检查它的移动组件。", this);
            ShowResult(false);
            yield break;
        }

        ClearEnemyProjectiles();

        // 保留尸体，等已有的死亡动画和延迟掉落结束。
        yield return new WaitUntil(() =>
            defeatedBoss == null || !defeatedBoss.gameObject.activeInHierarchy);

        yield return WaitForPickups();
    }

    private void OnBossKilled(EnemyKillContext context)
    {
        bossKilled = true;
    }

    private void UnbindBoss()
    {
        if (activeBoss != null)
            activeBoss.Killed -= OnBossKilled;

        activeBoss = null;
    }

    private IEnumerator PlayRewardRows(int wave)
    {
        int rows = wave >= 16 ? 3 : wave >= 11 ? 2 : 1;
        int doorHealth = 3 + (wave - 1) / 3;

        for (int row = 1; row <= rows; row++)
        {
            List<ItemDefinition> offers =
                ShooterLootPool.Roll(inventory, wave, Random.Range(2, 5));
            int count = offers.Count;
            if (count == 0)
                yield break;

            SetStatus($"第 {wave}/{waveCount} 波  ·  奖励门 {row}/{rows}");

            var slots = new List<float> { -3.75f, -1.25f, 1.25f, 3.75f };
            var doors = new List<Enemy>(count);

            for (int i = 0; i < count; i++)
            {
                int slotIndex = Random.Range(0, slots.Count);
                float x = slots[slotIndex];
                slots.RemoveAt(slotIndex);

                var position = new Vector3(x, 0f, 12f);
                Quaternion rotation = doorPrefab.transform.rotation;
                Enemy door = Instantiate(doorPrefab, position, rotation);

                door.GetComponent<EnemyRewardDrop>().SetReward(offers[i]);
                door.Spawn(null, position, rotation, doorHealth);
                doors.Add(door);
            }

            float timeout = Time.time + 15f;

            // 死亡到掉落之间还有延迟，等门回收后再看地上的奖励。
            yield return new WaitUntil(() => Time.time >= timeout ||
                !doors.Exists(door => door != null && door.gameObject.activeSelf));

            foreach (Enemy door in doors)
            {
                if (door == null)
                    continue;

                door.Despawn();
                Destroy(door.gameObject);
            }

            yield return WaitForPickups();
        }
    }

    private IEnumerator WaitForPickups()
    {
        ShooterItemPickup[] pickups = FindObjectsOfType<ShooterItemPickup>();
        float timeout = Time.time + 15f;

        yield return new WaitUntil(() => Time.time >= timeout ||
            !System.Array.Exists(pickups, pickup => pickup != null));

        foreach (ShooterItemPickup pickup in pickups)
        {
            if (pickup != null)
                Destroy(pickup.gameObject);
        }
    }

    private void ClearThreats()
    {
        foreach (Enemy enemy in FindObjectsOfType<Enemy>())
            enemy.Despawn();

        foreach (RoadHazard hazard in FindObjectsOfType<RoadHazard>())
        {
            hazard.gameObject.SetActive(false);
            Destroy(hazard.gameObject);
        }

        ClearEnemyProjectiles();
    }

    private void ClearEnemyProjectiles()
    {
        foreach (EnemyProjectile projectile in FindObjectsOfType<EnemyProjectile>())
        {
            projectile.gameObject.SetActive(false);
            Destroy(projectile.gameObject);
        }
    }

    private void OnPlayerDied()
    {
        if (state != State.Playing)
            return;

        state = State.Dying;
        StopAllCoroutines();
        UnbindBoss();
        StartCoroutine(FinishDeath());
    }

    private IEnumerator FinishDeath()
    {
        SetStatus("本局结束");
        yield return new WaitForSecondsRealtime(0.8f);
        ClearThreats();
        ShowResult(false);
    }

    private void ShowResult(bool won)
    {
        state = won ? State.Victory : State.Defeat;
        SetStatus(won ? "通关！" : "本局结束");
        hud?.SetBoss(null);
        hud?.ShowResult(won);
        Time.timeScale = 0f;
    }

    public void RestartFresh()
    {
        Restart(false);
    }

    public void RestartKeepingItems()
    {
        if (state == State.Victory)
            Restart(true);
    }

    private void Restart(bool keepItems)
    {
        if (restarting || (state != State.Defeat && state != State.Victory))
            return;

        int sceneIndex = SceneManager.GetActiveScene().buildIndex;
        if (sceneIndex < 0)
        {
            Debug.LogError("请先将 Game_01 加入 Build Settings。", this);
            return;
        }

        restarting = true;
        pendingItems = keepItems ? inventory.Core.Capture() : null;
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneIndex);
    }

    private void SetStatus(string message)
    {
        hud?.SetStatus(message);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        UnbindBoss();

        if (player != null)
            player.Died -= OnPlayerDied;
    }
}
