using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Enemy))]
public class EnemyRewardDrop : MonoBehaviour
{
    [SerializeField] private ItemDefinition reward;
    [SerializeField] private ShooterItemPickup pickupPrefab;

    [SerializeField] private Vector3 spawnOffset =
        new Vector3(0f, 0.6f, 0f);
    [SerializeField, Min(0f)] private float dropDelay;
    [SerializeField] private UnityEvent beforeDrop;

    private Enemy enemy;
    private bool dropped;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    private void OnEnable()
    {
        enemy.Spawned += ResetDrop;
        enemy.Killed += DropReward;

        ResetDrop();
    }

    private void OnDisable()
    {
        enemy.Spawned -= ResetDrop;
        enemy.Killed -= DropReward;StopAllCoroutines();
    }

    private void ResetDrop()
    {
        dropped = false;
    }

    private void DropReward(EnemyKillContext context)
    {
        if (dropped)
            return;
        dropped = true;
        beforeDrop?.Invoke();

        if (reward == null || pickupPrefab == null)
        {
            Debug.LogError("请给门配置 Reward 和 Pickup Prefab。", this);
            return;
        }

        Vector3 position = context.Position + spawnOffset;

        if (dropDelay > 0f)
            StartCoroutine(SpawnAfterDelay(position));
        else
            SpawnPickup(position);
        
    }

    private IEnumerator SpawnAfterDelay(Vector3 position)
    {
        yield return new WaitForSeconds(dropDelay);
        SpawnPickup(position);
    }

    private void SpawnPickup(Vector3 position)
    {
        ShooterItemPickup pickup = Instantiate(
            pickupPrefab, position, Quaternion.identity);

        pickup.Initialize(reward);
    }
}