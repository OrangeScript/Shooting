using TMPro;
using UnityEngine;

[RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
public class ShooterItemPickup : MonoBehaviour
{
    [SerializeField] private ItemDefinition item;
    [SerializeField] private TMP_Text itemLabel;
    [SerializeField] private Transform itemModelPoint;

    [SerializeField, Min(0.1f)] private float moveSpeed = 3f;
    [SerializeField, Min(1f)] private float keepSeconds = 8f;

    private Rigidbody body;
    private ShooterPlayerHealth player;
    private bool arrived;
    private bool collected;

    // 门 Instantiate 后立即调用，Start 会读取这一次掉落的道具。
    public void Initialize(ItemDefinition reward)
    {
        item = reward;
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        if (item == null)
        {
            Debug.LogError("拾取物没有配置 Item。", this);
            enabled = false;
            return;
        }

        player = FindObjectOfType<ShooterPlayerHealth>();

        if (itemLabel != null)
        {
            ItemRarity rarity = ShooterLootPool.GetRarity(item);
            itemLabel.color = ShooterLootPool.GetRarityColor(rarity);
            itemLabel.text = $"【{ShooterLootPool.GetRarityName(rarity)}】{item.itemName}\n{item.description}";
        }

        if (itemModelPoint != null && item.pickupVisualPrefab != null)
        {
            GameObject model = Instantiate(
                item.pickupVisualPrefab, itemModelPoint, false);

            model.transform.localPosition = Vector3.zero;
        }
    }

    private void FixedUpdate()
    {
        if (arrived || collected || player == null || player.IsDead)
            return;

        Vector3 position = body.position;
        float targetZ = player.transform.position.z;

        position.z = Mathf.MoveTowards(
            position.z, targetZ, moveSpeed * Time.fixedDeltaTime);

        body.MovePosition(position);

        if (position.z == targetZ)
        {
            arrived = true;
            Destroy(gameObject, keepSeconds);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActiveAndEnabled || collected || item == null)
            return;

        ShooterInventory inventory =
            other.GetComponentInParent<ShooterInventory>();

        if (inventory == null)
            return;

        ShooterPlayerHealth health =
            inventory.GetComponent<ShooterPlayerHealth>();

        if (health != null && health.IsDead)
            return;

        collected = true;

        if (!inventory.TryAdd(item.id))
        {
            collected = false;
            return;
        }

        Debug.Log($"获得道具：{item.itemName}");
        Destroy(gameObject);
    }
}
