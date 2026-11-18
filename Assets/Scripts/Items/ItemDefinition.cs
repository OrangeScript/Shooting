using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum EquipmentSlot
{
    None,
    Weapon,
    Head,
    Back
}

public enum ItemKind { Standard, Tradeoff, Curse }

public enum ItemRarity { Common = 0, Rare = 1, Epic = 2, Legendary = 3, Curse = 4 }

[System.Flags]
public enum ItemAvailability { Arena = 1, Shooter = 2, Both = Arena | Shooter }

[CreateAssetMenu(
    fileName = "NewItem",
    menuName = "Item Definition"
)]


public class ItemDefinition : ScriptableObject
{
    [Header("Identity")]
    public int id;
    public string itemName;

    [TextArea(2, 5)] public string description;
    public ItemKind kind;
    public ItemRarity rarity = ItemRarity.Common;
    public ItemAvailability availability = ItemAvailability.Both;
    [Tooltip("Game_01 奖励门中，相同品质内的抽取权重。")]
    [Min(0f)] public float dropWeight = 8f;

    [Min(1)]
    public int maxStacks = 1;

    [Header("Stats Per Stack")]
    public int bonusDamage = 0;
    public float speedMultiplier = 1f;
    public float sizeMultiplier = 1f;

    [Header("Appearance")]
    public EquipmentSlot slot = EquipmentSlot.None;
    public GameObject equipPrefab;

    [Header("World Appearance")]
    public GameObject pickupVisualPrefab;

    [Header("Shared Item Effects")]
    public List<ItemEffectDefinition> sharedEffects = new();

    [Header("Game_01 Shooter Effects")]
    public List<ItemEffectDefinition> shooterEffects = new();

    [Header("Scene_02 Arena Effects")]
    public List<ItemEffectDefinition> arenaEffects = new();

    public bool IsAvailableIn(ItemAvailability mode) => (availability & mode) != 0;

}
