
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class ShooterHealthHUD : MonoBehaviour
{
    [Header("Player")]
    [SerializeField]
    private ShooterPlayerHealth player;

    [Header("血条成长")]
    [SerializeField, Min(1f)]
    private float pixelsPerHP = 12f;

    [SerializeField, Min(0.01f)]
    private float greenGrowDuration = 0.35f;

    [SerializeField, Min(0.01f)]
    private float redCoverDuration = 0.8f;

    [SerializeField, Min(0.01f)]
    private float normalChangeDuration = 0.15f;

    private VisualElement root;
    private VisualElement healthBarBG;
    private VisualElement growthFill;
    private VisualElement healthFill;
    private Label healthText;

    private enum Phase
    {
        Idle,
        GrowGreen,
        CoverRed,
        ChangeRed
    }

    private Phase phase = Phase.Idle;
    private float elapsed;

    // 当前画面上实际显示的宽度
    private float displayedBarWidth;
    private float displayedRedWidth;

    // 本次动画的目标宽度
    private float targetBarWidth;
    private float targetRedWidth;

    // 动画起点
    private float startBarWidth;
    private float startRedWidth;

    private int lastHP;
    private int lastMaxHP;

    private bool started;

    private void OnEnable()
    {
        UIDocument document = GetComponent<UIDocument>();

        root = document.rootVisualElement.Q<VisualElement>("HUDRoot");

        if (root == null)
        {
            Debug.LogError("找不到 HUDRoot", this);
            return;
        }

        healthBarBG = root.Q<VisualElement>("HealthBarBG");
        growthFill = root.Q<VisualElement>("GrowthFill");
        healthFill = root.Q<VisualElement>("HealthFill");
        healthText = root.Q<Label>("HealthText");

        if (healthBarBG == null ||
            growthFill == null ||
            healthFill == null ||
            healthText == null)
        {
            Debug.LogError("ShooterHUD 缺少血条元素", this);
            return;
        }

        // 监听窗口尺寸变化
        root.RegisterCallback<GeometryChangedEvent>(
            OnRootGeometryChanged
        );

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

        if (player == null || healthBarBG == null)
        {
            Debug.LogError("找不到玩家血量组件或血条 UI", this);
            return;
        }

        // 防止重复订阅
        player.HealthChanged -= OnHealthChanged;
        player.HealthChanged += OnHealthChanged;

        InitializeBar(player.CurrentHealth, player.MaxHealth);
    }

    private void OnDisable()
    {
        if (player != null)
            player.HealthChanged -= OnHealthChanged;

        if (root != null)
        {
            root.UnregisterCallback<GeometryChangedEvent>(
                OnRootGeometryChanged
            );
        }
    }

    // 首次显示：直接设置长度，不播放成长动画
    private void InitializeBar(int current, int max)
    {
        lastHP = current;
        lastMaxHP = max;

        targetBarWidth = Mathf.Max(1, max) * pixelsPerHP;
        targetRedWidth =
            Mathf.Clamp(current, 0, max) * pixelsPerHP;

        displayedBarWidth = targetBarWidth;
        displayedRedWidth = targetRedWidth;

        growthFill.style.width = 0f;

        phase = Phase.Idle;
        elapsed = 0f;

        healthText.text = $"HP {current} / {max}";

        ApplyVisuals();
    }

    // 由 ShooterPlayerHealth.HealthChanged 触发
    private void OnHealthChanged(int current, int max)
    {
        bool maxIncreased = max > lastMaxHP;
        bool hpIncreased = current > lastHP;

        lastHP = current;
        lastMaxHP = max;

        targetBarWidth = Mathf.Max(1, max) * pixelsPerHP;
        targetRedWidth =
            Mathf.Clamp(current, 0, max) * pixelsPerHP;

        healthText.text = $"HP {current} / {max}";

        elapsed = 0f;

        if (maxIncreased && hpIncreased)
        {
            // 击杀成长：先绿后红
            startBarWidth = displayedBarWidth;
            startRedWidth = displayedRedWidth;

            phase = Phase.GrowGreen;
        }
        else
        {
            // 普通受伤 / 治疗 / 仅增加上限
            startRedWidth = displayedRedWidth;
            displayedBarWidth = targetBarWidth;

            growthFill.style.width = 0f;
            phase = Phase.ChangeRed;
        }

        ApplyVisuals();
    }

    private void Update()
    {
        if (phase == Phase.Idle)
            return;

        elapsed += Time.unscaledDeltaTime;

        switch (phase)
        {
            case Phase.GrowGreen:
                UpdateGreenGrowth();
                break;

            case Phase.CoverRed:
                UpdateRedCover();
                break;

            case Phase.ChangeRed:
                UpdateNormalChange();
                break;
        }

        ApplyVisuals();
    }

    private void UpdateGreenGrowth()
    {
        float t = Mathf.Clamp01(
            elapsed / greenGrowDuration
        );

        float smooth = Mathf.SmoothStep(0f, 1f, t);

        // 1. 整条血条增长
        displayedBarWidth = Mathf.Lerp(
            startBarWidth,
            targetBarWidth,
            smooth
        );

        // 2. 绿色从原有的红色末端长出来
        float greenEnd = Mathf.Lerp(
            startRedWidth,
            targetRedWidth,
            smooth
        );

        growthFill.style.left = startRedWidth;
        growthFill.style.width =
            Mathf.Max(0f, greenEnd - startRedWidth);

        // 3. 红色保持不动
        displayedRedWidth = startRedWidth;

        if (t >= 1f)
        {
            phase = Phase.CoverRed;
            elapsed = 0f;
        }
    }

    private void UpdateRedCover()
    {
        float t = Mathf.Clamp01(
            elapsed / redCoverDuration
        );

        float smooth = Mathf.SmoothStep(0f, 1f, t);

        // 绿色维持完全展开的状态
        growthFill.style.left = startRedWidth;
        growthFill.style.width =
            Mathf.Max(0f, targetRedWidth - startRedWidth);

        // 红色逐渐覆盖绿色
        displayedRedWidth = Mathf.Lerp(
            startRedWidth,
            targetRedWidth,
            smooth
        );

        if (t >= 1f)
        {
            displayedRedWidth = targetRedWidth;
            growthFill.style.width = 0f;
            phase = Phase.Idle;
        }
    }

    private void UpdateNormalChange()
    {
        float t = Mathf.Clamp01(
            elapsed / normalChangeDuration
        );

        float smooth = Mathf.SmoothStep(0f, 1f, t);

        displayedRedWidth = Mathf.Lerp(
            startRedWidth,
            targetRedWidth,
            smooth
        );

        if (t >= 1f)
        {
            displayedRedWidth = targetRedWidth;
            phase = Phase.Idle;
        }
    }

    private void ApplyVisuals()
    {
        if (healthBarBG == null)
            return;

        // 整条血条的宽度：随 MaxHealth 不断变长
        healthBarBG.style.width = displayedBarWidth;

        // 红色实际生命值
        healthFill.style.width = Mathf.Clamp(
            displayedRedWidth,
            0f,
            displayedBarWidth
        );

        CenterBar();
    }

    private void OnRootGeometryChanged(
        GeometryChangedEvent evt)
    {
        CenterBar();
    }

    private void CenterBar()
    {
        if (root == null || healthBarBG == null)
            return;

        float screenWidth = root.contentRect.width;

        if (screenWidth <= 0f || float.IsNaN(screenWidth))
            return;

        // 始终以屏幕中心为基准，向两侧增长
        healthBarBG.style.left =
            (screenWidth - displayedBarWidth) * 0.5f;

        // 生命值数字始终位于屏幕下方中央
        healthText.style.left =
            (screenWidth - 240f) * 0.5f;
    }
}
