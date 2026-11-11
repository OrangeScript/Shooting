using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAppearance : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Transform weaponSocket;
    [SerializeField] private Transform headSocket;
    [SerializeField] private Transform backSocket;
    [Tooltip("角色已有的武器子物体，或要挂载到 Weapon Socket 的武器预制体。")]
    [SerializeField] private GameObject defaultWeaponVisual;

    private CharacterController controller;
    private GameObject defaultWeaponInstance;
    private Vector3 baseVisualScale;
    private float baseHeight;
    private float baseRadius;
    private float baseStepOffset;
    private Vector3 baseCenter;

    private readonly Dictionary<EquipmentSlot, GameObject>
        spawned = new();

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        baseVisualScale = visualRoot != null
            ? visualRoot.localScale
            : Vector3.one;

        if (controller != null)
        {
            baseHeight = controller.height;
            baseRadius = controller.radius;
            baseCenter = controller.center;
            baseStepOffset = controller.stepOffset;
        }

        InitializeDefaultWeapon();
    }

    private void InitializeDefaultWeapon()
    {
        if (defaultWeaponVisual == null)
            return;

        // 已挂在角色身上的模型可以直接切换显示；资源需要先实例化。
        if (defaultWeaponVisual.transform.IsChildOf(transform))
        {
            defaultWeaponInstance = defaultWeaponVisual;
        }
        else
        {
            if (weaponSocket == null)
            {
                Debug.LogError("默认武器缺少 Weapon Socket，无法挂载。", this);
                return;
            }

            defaultWeaponInstance = Instantiate(
                defaultWeaponVisual, weaponSocket, false
            );
            defaultWeaponInstance.transform.localPosition = Vector3.zero;
            defaultWeaponInstance.transform.localRotation = Quaternion.identity;
        }

        defaultWeaponInstance.SetActive(true);
    }

    public void Apply(
        IReadOnlyList<ItemDefinition> items,
        float sizeMultiplier)
    {
        // 清理原有装备模型
        foreach (var model in spawned.Values)
        {
            if (model == null) continue;
            model.SetActive(false);
            Destroy(model);
        }

        spawned.Clear();

        if (defaultWeaponInstance != null)
            defaultWeaponInstance.SetActive(true);

        // 同槽位后获得的道具优先显示
        Dictionary<EquipmentSlot, ItemDefinition>
            selected = new();

        foreach (var item in items)
        {
            if (item == null ||
                item.slot == EquipmentSlot.None ||
                item.equipPrefab == null)
                continue;

            selected[item.slot] = item;
        }

        foreach (var pair in selected)
        {
            Transform socket = GetSocket(pair.Key);
            if (socket == null) continue;

            GameObject model = Instantiate(
                pair.Value.equipPrefab,
                socket
            );

            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            spawned[pair.Key] = model;

            if (pair.Key == EquipmentSlot.Weapon &&
                defaultWeaponInstance != null)
            {
                defaultWeaponInstance.SetActive(false);
            }
        }

        // 视觉模型变大或变小
        if (visualRoot != null)
        {
            visualRoot.localScale =
                baseVisualScale * sizeMultiplier;
        }

        // 同步修改角色本地碰撞胶囊大小
        if (controller != null)
        {
            controller.height = baseHeight * sizeMultiplier;
            controller.radius = baseRadius * sizeMultiplier;
            controller.center = baseCenter * sizeMultiplier;

            controller.stepOffset = Mathf.Min(
                baseStepOffset * sizeMultiplier,
                controller.height * 0.4f
            );
        }
    }

    private Transform GetSocket(EquipmentSlot slot)
    {
        return slot switch
        {
            EquipmentSlot.Weapon => weaponSocket,
            EquipmentSlot.Head => headSocket,
            EquipmentSlot.Back => backSocket,
            _ => null
        };
    }

}
