using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Enemy))]
public class EnemyRewardDrop : MonoBehaviour
{
    [SerializeField] private ItemDefinition reward;
    [SerializeField] private ShooterItemPickup pickupPrefab;
    [SerializeField] private TMP_Text previewLabel;

    [SerializeField] private Vector3 spawnOffset =
        new Vector3(0f, 0.6f, 0f);
    [SerializeField, Min(0f)] private float dropDelay;
    [SerializeField] private UnityEvent beforeDrop;

    private Enemy enemy;
    private bool dropped;
    private bool isCarrier;

    public void ConfigureCarrier(EnemyRewardDrop template, ItemDefinition item)
    {
        isCarrier = true;
        pickupPrefab = template.pickupPrefab;
        spawnOffset = template.spawnOffset;

        if (previewLabel == null && template.previewLabel != null)
        {
            Collider hitbox = GetComponent<Collider>();
            Vector3 position = transform.position;
            position.y = hitbox != null ? hitbox.bounds.max.y + 0.25f : position.y + 2.8f;
            position.z = hitbox != null ? hitbox.bounds.min.z - 0.1f : position.z - 0.7f;
            // 先按世界尺寸复制，再挂到怪物身上，兼容不同模型比例和朝向。
            previewLabel = Instantiate(template.previewLabel, position, Quaternion.identity);
            previewLabel.transform.SetParent(transform, true);
        }

        SetReward(item);
    }

    public void SetReward(ItemDefinition item)
    {
        reward = item;
        UpdatePreview();
    }

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
        enemy.Killed -= DropReward;
        StopAllCoroutines();
    }

    private void ResetDrop()
    {
        dropped = false;
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (previewLabel == null)
            return;

        previewLabel.gameObject.SetActive(reward != null);
        if (reward == null)
            return;

        ItemRarity rarity = ShooterLootPool.GetRarity(reward);
        string rarityName = ShooterLootPool.GetRarityName(rarity);
        previewLabel.color = ShooterLootPool.GetRarityColor(rarity);
        string title = $"【{rarityName}】{reward.itemName}";
        if (isCarrier)
            title = $"奖励怪\n{title}";
        previewLabel.text = rarity == ItemRarity.Curse
            ? $"{title}\n{reward.description}"
            : title;
    }

    private void DropReward(EnemyKillContext context)
    {
        if (dropped)
            return;
        dropped = true;
        if (previewLabel != null)
            previewLabel.gameObject.SetActive(false);
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
