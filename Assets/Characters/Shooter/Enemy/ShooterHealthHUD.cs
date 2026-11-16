using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class ShooterHealthHUD : MonoBehaviour
{
    private const float HealthPerLayer = 100f;

    [Header("Player")]
    [SerializeField] private ShooterPlayerHealth player;

    [Header("血条成长（长度最多容纳100点）")]
    [SerializeField, Min(1f)] private float pixelsPerHP = 12f;
    [SerializeField, Min(0.01f)] private float greenGrowDuration = 0.35f;
    [Tooltip("当前血量层的颜色覆盖绿色的时间。")]
    [SerializeField, Min(0.01f)] private float redCoverDuration = 0.8f;
    [SerializeField, Min(0.01f)] private float normalChangeDuration = 0.15f;

    [Header("每100点叠一层，RGB逐层变深")]
    [SerializeField] private Color firstLayerColor = new Color32(255, 85, 100, 255);
    [SerializeField, Min(0.01f)] private float layerDarkening = 0.22f;

    private VisualElement root;
    private VisualElement healthBarBG;
    private VisualElement layerBaseFill;
    private VisualElement healthFill;
    private VisualElement growthFill;
    private VisualElement growthWrapFill;
    private Label healthText;

    private enum Phase
    {
        Idle,
        GrowGreen,
        CoverColor,
        ChangeHealth
    }

    private Phase phase;
    private float elapsed;

    // 用血量而不是像素记录动画，跨100点时仍然连续向前。
    private float displayedHealth;
    private float growthHealth;
    private float targetHealth;
    private float startHealth;
    private float startGrowthHealth;

    // 不足100点时保留长度成长；达到100点后只增加颜色层。
    private float displayedCapacity;
    private float targetCapacity;
    private float startCapacity;
    private float displayedBarWidth;

    private int lastHP;
    private int lastMaxHP;
    private bool started;

    private void OnEnable()
    {
        var document = GetComponent<UIDocument>();
        root = document.rootVisualElement.Q<VisualElement>("HUDRoot");
        if (root == null)
        {
            Debug.LogError("找不到 HUDRoot", this);
            return;
        }

        healthBarBG = root.Q<VisualElement>("HealthBarBG");
        layerBaseFill = root.Q<VisualElement>("LayerBaseFill");
        healthFill = root.Q<VisualElement>("HealthFill");
        growthFill = root.Q<VisualElement>("GrowthFill");
        growthWrapFill = root.Q<VisualElement>("GrowthWrapFill");
        healthText = root.Q<Label>("HealthText");

        if (healthBarBG == null || layerBaseFill == null || healthFill == null ||
            growthFill == null || growthWrapFill == null || healthText == null)
        {
            Debug.LogError("ShooterHUD 缺少血条元素", this);
            return;
        }

        root.RegisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
        if (started)
            BindPlayer();
    }

    private void Start()
    {
        started = true;
        BindPlayer();
    }

    private void BindPlayer()
    {
        if (player == null)
            player = FindObjectOfType<ShooterPlayerHealth>();

        if (player == null || healthBarBG == null || layerBaseFill == null ||
            healthFill == null || growthFill == null || growthWrapFill == null || healthText == null)
        {
            Debug.LogError("找不到玩家血量组件或血条 UI", this);
            return;
        }

        player.HealthChanged -= OnHealthChanged;
        player.HealthChanged += OnHealthChanged;
        InitializeBar(player.CurrentHealth, player.MaxHealth);
    }

    private void OnDisable()
    {
        if (player != null)
            player.HealthChanged -= OnHealthChanged;
        if (root != null)
            root.UnregisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
        phase = Phase.Idle;
    }

    private void InitializeBar(int current, int max)
    {
        lastMaxHP = Mathf.Max(1, max);
        lastHP = Mathf.Clamp(current, 0, lastMaxHP);
        displayedHealth = growthHealth = targetHealth = lastHP;
        displayedCapacity = targetCapacity = Mathf.Min(lastMaxHP, HealthPerLayer);
        phase = Phase.Idle;
        elapsed = 0f;
        UpdateHealthText();
        ApplyVisuals();
    }

    private void OnHealthChanged(int current, int max)
    {
        max = Mathf.Max(1, max);
        current = Mathf.Clamp(current, 0, max);
        if (current == lastHP && max == lastMaxHP)
            return;

        int previousHP = lastHP;
        lastHP = current;
        lastMaxHP = max;
        targetHealth = current;
        targetCapacity = Mathf.Min(max, HealthPerLayer);
        UpdateHealthText();

        if (current > previousHP)
        {
            // 受伤动画尚未结束就回血时，先结算先前的伤害，避免从虚高血量继续增长。
            if (phase == Phase.ChangeHealth)
            {
                displayedHealth = Mathf.Min(displayedHealth, previousHP);
                growthHealth = displayedHealth;
            }

            startHealth = displayedHealth;
            startGrowthHealth = Mathf.Clamp(
                Mathf.Max(displayedHealth, growthHealth), displayedHealth, targetHealth);
            growthHealth = startGrowthHealth;
            phase = Phase.GrowGreen;
        }
        else if (current < previousHP)
        {
            // 受伤立即取消旧回血目标；此后只能向最新的血量下降。
            displayedHealth = Mathf.Max(targetHealth, Mathf.Max(displayedHealth, growthHealth));
            growthHealth = displayedHealth;
            startHealth = displayedHealth;
            phase = Phase.ChangeHealth;
        }
        else if (phase != Phase.Idle)
        {
            // 仅增加血量上限，不伪造绿色回血，也不打断正在进行的回血覆盖。
            displayedCapacity = startCapacity = targetCapacity;
            ApplyVisuals();
            return;
        }
        else
        {
            startHealth = displayedHealth;
            growthHealth = displayedHealth;
            phase = Phase.ChangeHealth;
        }

        startCapacity = displayedCapacity;
        elapsed = 0f;
        ApplyVisuals();
    }

    private void Update()
    {
        if (phase != Phase.Idle)
            AdvanceAnimation(Time.unscaledDeltaTime);
    }

    private void AdvanceAnimation(float deltaTime)
    {
        float remaining = Mathf.Max(0f, deltaTime);
        while (phase != Phase.Idle)
        {
            float duration = Mathf.Max(0.01f, phase == Phase.GrowGreen
                ? greenGrowDuration
                : phase == Phase.CoverColor ? redCoverDuration : normalChangeDuration);
            float step = Mathf.Min(remaining, Mathf.Max(0f, duration - elapsed));
            remaining -= step;
            elapsed += step;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

            if (phase == Phase.GrowGreen)
            {
                displayedHealth = startHealth;
                growthHealth = Mathf.Lerp(startGrowthHealth, targetHealth, t);
            }
            else
            {
                displayedHealth = Mathf.Lerp(startHealth, targetHealth, t);
                growthHealth = phase == Phase.CoverColor ? targetHealth : displayedHealth;
            }

            displayedCapacity = Mathf.Lerp(startCapacity, targetCapacity, t);
            if (elapsed < duration)
                break;

            elapsed = 0f;
            displayedCapacity = targetCapacity;
            if (phase == Phase.GrowGreen)
            {
                growthHealth = targetHealth;
                startHealth = displayedHealth;
                startCapacity = displayedCapacity;
                phase = Phase.CoverColor;
            }
            else
            {
                displayedHealth = growthHealth = targetHealth;
                phase = Phase.Idle;
            }

            if (remaining <= 0f)
                break;
        }

        ApplyVisuals();
    }

    private void UpdateHealthText()
    {
        healthText.text = $"HP {lastHP} / {lastMaxHP}";
    }

    private void ApplyVisuals()
    {
        if (healthBarBG == null || layerBaseFill == null || healthFill == null ||
            growthFill == null || growthWrapFill == null)
            return;

        float capacity = Mathf.Clamp(displayedCapacity, 1f, HealthPerLayer);
        displayedBarWidth = capacity * Mathf.Max(1f, pixelsPerHP);
        float screenWidth = root.contentRect.width;
        if (screenWidth > 0f && !float.IsNaN(screenWidth))
            displayedBarWidth = Mathf.Min(displayedBarWidth, Mathf.Max(1f, screenWidth - 32f));
        healthBarBG.style.width = displayedBarWidth;

        // 只需要显示最上面的两个颜色层，不随总血量创建更多 UI 元素。
        int layer = displayedHealth > 0f
            ? Mathf.Max(0, Mathf.CeilToInt(displayedHealth / HealthPerLayer) - 1)
            : 0;
        float layerHealth = Mathf.Max(0f, displayedHealth - layer * HealthPerLayer);
        layerBaseFill.style.backgroundColor = GetLayerColor(Mathf.Max(0, layer - 1));
        SetFillRange(layerBaseFill, 0f, layer > 0 ? capacity : 0f, capacity);
        healthFill.style.backgroundColor = GetLayerColor(layer);
        SetFillRange(healthFill, 0f, layerHealth, capacity);

        RenderGreenGrowth(capacity);
        CenterBar();
    }

    private Color GetLayerColor(int layer)
    {
        float brightness = 1f / (1f + Mathf.Max(0.01f, layerDarkening) * Mathf.Max(0, layer));
        return new Color(
            firstLayerColor.r * brightness,
            firstLayerColor.g * brightness,
            firstLayerColor.b * brightness,
            firstLayerColor.a);
    }

    private void RenderGreenGrowth(float capacity)
    {
        SetFillRange(growthFill, 0f, 0f, capacity);
        SetFillRange(growthWrapFill, 0f, 0f, capacity);
        float amount = Mathf.Max(0f, growthHealth - displayedHealth);
        if (amount <= 0f)
            return;

        if (amount >= HealthPerLayer)
        {
            // 尚未覆盖的回血跨越完整一层时，绿色暂时覆盖整条血条。
            SetFillRange(growthFill, 0f, capacity, capacity);
            return;
        }

        float start = Mathf.Repeat(displayedHealth, HealthPerLayer);
        float firstAmount = Mathf.Min(amount, HealthPerLayer - start);
        SetFillRange(growthFill, start, firstAmount, capacity);
        // 例如95 -> 105，绿色同时出现在旧层末尾和新层开头。
        SetFillRange(growthWrapFill, 0f, amount - firstAmount, capacity);
    }

    private static void SetFillRange(VisualElement fill, float start, float amount, float capacity)
    {
        start = Mathf.Clamp(start, 0f, capacity);
        amount = Mathf.Clamp(amount, 0f, capacity - start);
        fill.style.left = Length.Percent(start / capacity * 100f);
        fill.style.width = Length.Percent(amount / capacity * 100f);
    }

    private void OnRootGeometryChanged(GeometryChangedEvent evt)
    {
        ApplyVisuals();
    }

    private void CenterBar()
    {
        if (root == null || healthBarBG == null || healthText == null)
            return;

        float screenWidth = root.contentRect.width;
        if (screenWidth <= 0f || float.IsNaN(screenWidth))
            return;
        healthBarBG.style.left = (screenWidth - displayedBarWidth) * 0.5f;
        healthText.style.left = (screenWidth - 240f) * 0.5f;
    }
}
