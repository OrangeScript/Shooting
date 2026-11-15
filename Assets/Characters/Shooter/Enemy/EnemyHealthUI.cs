using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class EnemyHealthUI : MonoBehaviour
{
    public static EnemyHealthUI Instance {get; private set;}
    [Header("显示规则")]
    [SerializeField] private int healthThreshold = 50;

    [Header("位置")]
    [SerializeField] private Camera worldCamera;
    [SerializeField] private float worldYOffset = 0.3f;
    [SerializeField] private float labelWidth = 88f;
    [SerializeField] private float labelHeight = 30f;

    private VisualElement layer;

    private class Entry
    {
        public Label label;
        public Collider collider;
        public int lastHealth = -1;
    }

    private readonly Dictionary<Enemy, Entry> entries = new();
    private readonly List<Enemy> pendingRemoval = new();

    private void OnEnable()
    {
        Instance = this;

        UIDocument document = GetComponent<UIDocument>();

        VisualElement root = document.rootVisualElement;

        // 和玩家的 ShooterHealthHUD 共享 UIDocument
        VisualElement hudRoot =
            root.Q<VisualElement>("HUDRoot") ?? root;

        layer = new VisualElement
        {
            name = "EnemyHealthLayer",
            pickingMode = PickingMode.Ignore
        };

        layer.style.position = Position.Absolute;
        layer.style.left = 0;
        layer.style.right = 0;
        layer.style.top = 0;
        layer.style.bottom = 0;
        layer.style.overflow = Overflow.Visible;

        hudRoot.Add(layer);
    }

    private void Start()
    {
        // 防止 UI 管理器启用前已有怪物生成
        foreach (Enemy enemy in FindObjectsOfType<Enemy>())
            Register(enemy);
    }

    private void OnDisable()
    {
        if (Instance == this)
            Instance = null;

        foreach (Entry entry in entries.Values)
            entry.label.RemoveFromHierarchy();

        entries.Clear();
        pendingRemoval.Clear();

        layer?.RemoveFromHierarchy();
        layer = null;
    }

    public void Register(Enemy enemy)
    {
        if (enemy == null ||
            !enemy.IsAlive ||
            enemy.MaxHealth <= healthThreshold ||
            layer == null)
            return;

        if (entries.ContainsKey(enemy))
            return;

        Label label = new Label(enemy.CurrentHealth.ToString());

        label.AddToClassList("enemy-hp-number");
        label.pickingMode = PickingMode.Ignore;
        label.style.width = labelWidth;
        label.style.height = labelHeight;
        label.style.display = DisplayStyle.None;

        layer.Add(label);

        entries.Add(enemy, new Entry
        {
            label = label,
            collider = enemy.GetComponentInChildren<Collider>(),
            lastHealth = enemy.CurrentHealth
        });
    }

    public void Unregister(Enemy enemy)
    {
        if (enemy == null || !entries.TryGetValue(enemy, out Entry entry))
            return;

        entry.label.RemoveFromHierarchy();
        entries.Remove(enemy);
    }

    private void LateUpdate()
    {
        if (layer == null || layer.panel == null)
            return;

        if (worldCamera == null)
            worldCamera = Camera.main;

        if (worldCamera == null)
            return;

        pendingRemoval.Clear();

        foreach (var pair in entries)
        {
            Enemy enemy = pair.Key;
            Entry entry = pair.Value;

            if (enemy == null ||
                !enemy.isActiveAndEnabled ||
                !enemy.IsAlive)
            {
                pendingRemoval.Add(enemy);
                continue;
            }

            // 只有血量变化时才更新文字
            if (entry.lastHealth != enemy.CurrentHealth)
            {
                entry.lastHealth = enemy.CurrentHealth;
                entry.label.text = enemy.CurrentHealth.ToString();
            }

            UpdatePosition(enemy, entry);
        }

        foreach (Enemy enemy in pendingRemoval)
            Unregister(enemy);
    }

    private void UpdatePosition(Enemy enemy, Entry entry)
    {
        Vector3 worldPosition;

        if (enemy.HealthAnchor != null)
        {
            // 设置了定位点，就直接使用它的位置
            worldPosition = enemy.HealthAnchor.position;
        }
        else if (entry.collider != null && entry.collider.enabled)
        {
            // 未设置定位点的怪物，继续使用原来的方式
            Bounds bounds = entry.collider.bounds;

            worldPosition = new Vector3(
                bounds.center.x,
                bounds.max.y + worldYOffset,
                bounds.center.z
            );
        }
        else
        {
            worldPosition =
                enemy.transform.position + Vector3.up * 2f;
        }

        // 世界坐标 -> 屏幕坐标
        Vector3 screenPosition =
            worldCamera.WorldToScreenPoint(worldPosition);

        // 位于摄像机背后或屏幕之外时隐藏
        if (screenPosition.z <= 0f ||
            screenPosition.x < 0f ||
            screenPosition.x > Screen.width ||
            screenPosition.y < 0f ||
            screenPosition.y > Screen.height)
        {
            entry.label.style.display = DisplayStyle.None;
            return;
        }

        // Screen 使用左下角原点，UI Toolkit 使用左上角
        Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(
            layer.panel,
            new Vector2(
                screenPosition.x,
                Screen.height - screenPosition.y
            )
        );

        // Panel 坐标 -> 当前 UI 容器的局部坐标
        Vector2 localPosition =
            layer.WorldToLocal(panelPosition);

        // 居中显示在怪物头顶
        entry.label.style.left =
            localPosition.x - labelWidth * 0.5f;

        entry.label.style.top =
            localPosition.y - labelHeight;

        entry.label.style.display = DisplayStyle.Flex;
    }

}
