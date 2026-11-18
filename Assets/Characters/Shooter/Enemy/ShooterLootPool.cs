using System.Collections.Generic;
using UnityEngine;

public static class ShooterLootPool
{
    private const int CurseIndex = (int)ItemRarity.Curse;
    private const int RarityCount = CurseIndex + 1;

    // 顺序与 ItemRarity 一致。无可抽取道具的品质不参与本次抽签。
    private static readonly int[,] RarityWeights =
    {
        { 92, 8, 0, 0, 0 },    // 第 1–4 波
        { 73, 18, 5, 0, 4 },  // 第 5–9 波
        { 57, 23, 12, 2, 6 }, // 第 10–14 波
        { 43, 25, 18, 5, 9 }  // 第 15–20 波
    };

    public static List<ItemDefinition> Roll(ShooterInventory inventory, int wave, int count)
    {
        var result = new List<ItemDefinition>();
        if (count <= 0 || inventory == null ||
            inventory.Database == null || inventory.Core == null)
            return result;

        int band = GetWaveBand(wave);
        var buckets = new List<ItemDefinition>[RarityCount];
        var seenIds = new HashSet<int>();
        ItemDatabase database = inventory.Database;

        foreach (ItemDefinition item in database.GetAllItems())
        {
            if (item == null || !item.IsAvailableIn(ItemAvailability.Shooter) ||
                !(item.dropWeight > 0f) || float.IsInfinity(item.dropWeight))
                continue;

            // 兼容后来把 ItemKind 设成 Curse、品质未改的资产。
            int rarityIndex = (int)GetRarity(item);
            if (rarityIndex < 0 || rarityIndex >= RarityCount ||
                RarityWeights[band, rarityIndex] == 0 ||
                database.GetItem(item.id) != item ||
                !inventory.Core.CanAdd(item.id) || !seenIds.Add(item.id))
                continue;

            buckets[rarityIndex] ??= new List<ItemDefinition>();
            buckets[rarityIndex].Add(item);
        }

        bool canRollCurse = count > 1;
        while (result.Count < count)
        {
            int rarityIndex = RollRarity(buckets, band, canRollCurse);
            if (rarityIndex < 0)
                break;

            List<ItemDefinition> bucket = buckets[rarityIndex];
            int index = RollItemIndex(bucket);
            result.Add(bucket[index]);
            bucket.RemoveAt(index);

            if (rarityIndex == CurseIndex)
                canRollCurse = false;
        }

        return result;
    }

    public static ItemRarity GetRarity(ItemDefinition item)
    {
        return item.kind == ItemKind.Curse ? ItemRarity.Curse : item.rarity;
    }

    public static string GetRarityName(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Rare => "稀有",
            ItemRarity.Epic => "史诗",
            ItemRarity.Legendary => "传奇",
            ItemRarity.Curse => "诅咒",
            _ => "普通"
        };
    }

    public static Color GetRarityColor(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Rare => new Color32(109, 205, 255, 255),
            ItemRarity.Epic => new Color32(211, 161, 255, 255),
            ItemRarity.Legendary => new Color32(255, 199, 93, 255),
            ItemRarity.Curse => new Color32(255, 126, 158, 255),
            _ => Color.white
        };
    }

    private static int GetWaveBand(int wave)
    {
        if (wave < 5) return 0;
        if (wave < 10) return 1;
        if (wave < 15) return 2;
        return 3;
    }

    private static int RollRarity(List<ItemDefinition>[] buckets, int band, bool canRollCurse)
    {
        int positiveWeight = 0;
        for (int i = 0; i < CurseIndex; i++)
        {
            if (buckets[i] != null && buckets[i].Count > 0)
                positiveWeight += RarityWeights[band, i];
        }

        // 正常奖励已抽完时，结束门排；不要用诅咒填满剩余位置。
        if (positiveWeight == 0)
            return -1;

        int curseWeight = 0;
        if (canRollCurse && buckets[CurseIndex] != null && buckets[CurseIndex].Count > 0)
            curseWeight = RarityWeights[band, CurseIndex];

        int ticket = Random.Range(0, positiveWeight + curseWeight);
        if (ticket >= positiveWeight)
            return CurseIndex;

        for (int i = 0; i < CurseIndex; i++)
        {
            if (buckets[i] == null || buckets[i].Count == 0)
                continue;

            ticket -= RarityWeights[band, i];
            if (ticket < 0)
                return i;
        }

        return -1;
    }

    private static int RollItemIndex(List<ItemDefinition> bucket)
    {
        double totalWeight = 0;
        foreach (ItemDefinition item in bucket)
            totalWeight += item.dropWeight;

        double ticket = Random.value * totalWeight;
        for (int i = 0; i < bucket.Count; i++)
        {
            ticket -= bucket[i].dropWeight;
            if (ticket < 0)
                return i;
        }

        // Random.value 包含 1。
        return bucket.Count - 1;
    }
}
