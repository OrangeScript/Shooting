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

[CreateAssetMenu(
    fileName = "NewItem",
    menuName = "Item Definition"
)]


public class ItemDefinition : ScriptableObject
{
    [Header("Identity")]
    public int id;
    public string itemName;

    [Min(1)]
    public int maxStacks = 1;

    [Header("Stats Per Stack")]
    public int bonusDamage = 0;
    public float speedMultiplier = 1f;
    public float sizeMultiplier = 1f;

    [Header("Appearance")]
    public EquipmentSlot slot = EquipmentSlot.None;
    public GameObject equipPrefab;
}
