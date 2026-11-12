using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerInventory : NetworkBehaviour
{
    [Header("Item Database")]
    [SerializeField] private ItemDatabase database;

    private NetworkList<int> ownedItems;
    private PlayerAppearance appearance;
    public int BonusDamage { get; private set; }
    public float SpeedMultiplier { get; private set; } = 1f;
    public float SizeMultiplier { get; private set; } = 1f;
    private void Awake()
    {
        ownedItems = new NetworkList<int>();
        appearance = GetComponent<PlayerAppearance>();
    }

    public override void OnNetworkSpawn()
    {
        ownedItems.OnListChanged += OnItemsChanged;
        Rebuild();
    }

    public override void OnNetworkDespawn()
    {
        ownedItems.OnListChanged -= OnItemsChanged;
    }

    public override void OnDestroy()
    {
        ownedItems?.Dispose();
    }

    private ItemDefinition FindItem(int id)
    {
        return database.GetItem(id);
    }

    // 只能由服务器调用
    public bool ServerTryAdd(ItemDefinition item)
    {
        if (!IsServer || !IsSpawned || item == null)
            return false;


        ItemDefinition definition = FindItem(item.id);
        if (definition == null)
            return false;

        int count = 0;

        foreach (int id in ownedItems)
        {
            if (id == item.id)
                count++;
        }

        if (count >= definition.maxStacks)
            return false;
        ownedItems.Add(definition.id);
        return true;
    }

    private void OnItemsChanged(
        NetworkListEvent<int> changeEvent)
    {
        Rebuild();
    }

    private void Rebuild()
    {
        BonusDamage = 0;
        SpeedMultiplier = 1f;
        SizeMultiplier = 1f;

        List<ItemDefinition> equipped = new();

        foreach (int id in ownedItems)
        {
            ItemDefinition item = FindItem(id);
            if (item == null) continue;

            BonusDamage += item.bonusDamage;
            SpeedMultiplier *= item.speedMultiplier;
            SizeMultiplier *= item.sizeMultiplier;

            equipped.Add(item);
        }

        // 防止原型阶段角色变得过大或过小
        SizeMultiplier = Mathf.Clamp(
            SizeMultiplier, 0.6f, 2f
        );

        appearance?.Apply(equipped, SizeMultiplier);

        Debug.Log(
            $"Inventory: Damage +{BonusDamage}, " +
            $"Speed x{SpeedMultiplier:F2}, " +
            $"Size x{SizeMultiplier:F2}"
        );
    }

}
