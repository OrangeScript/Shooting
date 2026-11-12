using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ItemDatabase",
    menuName = "Items/Item Database"
)]
public class ItemDatabase : ScriptableObject
{
    [SerializeField]
    private List<ItemDefinition> items = new();

    private Dictionary<int, ItemDefinition> lookup;

    private void OnEnable()
    {
        lookup = null;
    }

    private void BuildLookup()
    {
        lookup = new Dictionary<int, ItemDefinition>();

        foreach (var item in items)
        {
            if (item == null) continue;

            if (!lookup.TryAdd(item.id, item))
            {
                Debug.LogError(
                    $"重复的道具 ID: {item.id}"
                );
            }
        }
    }

    public ItemDefinition GetItem(int id)
    {
        if (lookup == null)
            BuildLookup();

        lookup.TryGetValue(id, out var item);
        return item;
    }

    public IReadOnlyList<ItemDefinition> GetAllItems()
    {
        return items;
    }
}