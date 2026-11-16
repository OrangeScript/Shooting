
using System;
using System.Collections.Generic;

public sealed class InventoryCore
{
    private readonly ItemDatabase database;
    private readonly List<int> items = new();

    public IReadOnlyList<int> Items => items;

    public event Action Changed;

    public InventoryCore(ItemDatabase database)
    {
        this.database = database;
    }

    public int GetStacks(int itemId)
    {
        int result = 0;

        foreach (int id in items)
        {
            if (id == itemId)
                result++;
        }

        return result;
    }

    public bool CanAdd(int itemId)
    {
        if (database == null)
            return false;

        ItemDefinition definition =
            database.GetItem(itemId);

        if (definition == null)
            return false;

        return GetStacks(itemId) < definition.maxStacks;
    }

    public bool TryAdd(int itemId)
    {
        if (!CanAdd(itemId))
            return false;

        items.Add(itemId);
        Changed?.Invoke();

        return true;
    }

    public bool TryRemove(int itemId)
    {
        if (!items.Remove(itemId))
            return false;

        Changed?.Invoke();
        return true;
    }

    public List<int> Capture()
    {
        return new List<int>(items);
    }

    public void Restore(IEnumerable<int> savedItems)
    {
        // 先复制快照，避免传入 Items 自身时出错
        List<int> snapshot = savedItems != null
            ? new List<int>(savedItems)
            : new List<int>();

        items.Clear();

        foreach (int id in snapshot)
        {
            if (CanAdd(id))
                items.Add(id);
        }

        // 恢复完成之后只触发一次
        Changed?.Invoke();
    }
}
