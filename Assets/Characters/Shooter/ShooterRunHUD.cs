using UnityEngine;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
[RequireComponent(typeof(UIDocument))]
public sealed class ShooterRunHUD : MonoBehaviour
{
    [SerializeField] private GameDirector director;

    private const string HiddenClass = "run-hidden";
    private Label status;
    private VisualElement bossPanel;
    private Label bossHealth;
    private VisualElement bossHealthFill;
    private VisualElement resultPanel;
    private VisualElement resultCard;
    private Label resultTitle;
    private Label resultDetails;
    private Button restartFresh;
    private Button restartKeepingItems;

    private string statusMessage = "准备开始";
    private bool? victory;
    private Enemy boss;
    private int lastBossHealth = -1;
    private int lastBossMaxHealth = -1;
    private bool bound;

    private void OnEnable()
    {
        // UIDocument 通常先初始化。Start 也会为首次加载补一次绑定。
        Bind();
    }

    private void Start()
    {
        if (!bound && !Bind())
            Debug.LogError("无法绑定本局界面，请检查 ShooterHUD.uxml 和 UIDocument。", this);

        if (director == null)
            Debug.LogError("本局界面缺少 GameDirector 引用。", this);
    }

    private bool Bind()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        if (root == null)
            return false;

        status = root.Q<Label>("RunStatus");
        bossPanel = root.Q<VisualElement>("RunBoss");
        bossHealth = root.Q<Label>("RunBossHealth");
        bossHealthFill = root.Q<VisualElement>("RunBossHealthFill");
        resultPanel = root.Q<VisualElement>("RunResult");
        resultCard = root.Q<VisualElement>("RunResultCard");
        resultTitle = root.Q<Label>("RunResultTitle");
        resultDetails = root.Q<Label>("RunResultDetails");
        restartFresh = root.Q<Button>("RunRestartFresh");
        restartKeepingItems = root.Q<Button>("RunRestartKeepingItems");

        if (status == null || bossPanel == null || bossHealth == null || bossHealthFill == null ||
            resultPanel == null || resultCard == null || resultTitle == null || resultDetails == null ||
            restartFresh == null || restartKeepingItems == null)
            return false;

        root.pickingMode = PickingMode.Ignore;
        restartFresh.clicked += OnRestartFresh;
        restartKeepingItems.clicked += OnRestartKeepingItems;
        bound = true;

        SetStatus(statusMessage);
        SetBoss(boss);
        RefreshResult();
        return true;
    }

    private void OnDisable()
    {
        if (!bound)
            return;

        restartFresh.clicked -= OnRestartFresh;
        restartKeepingItems.clicked -= OnRestartKeepingItems;
        bound = false;
    }

    public void ResetRun()
    {
        victory = null;
        SetStatus("准备开始");
        SetBoss(null);
        RefreshResult();
    }

    public void SetStatus(string message)
    {
        statusMessage = message;
        if (bound)
            status.text = message;
    }

    public void SetBoss(Enemy nextBoss)
    {
        boss = nextBoss;
        lastBossHealth = -1;
        lastBossMaxHealth = -1;
        if (bound)
            RefreshBoss();
    }

    private void LateUpdate()
    {
        if (bound && !bossPanel.ClassListContains(HiddenClass))
            RefreshBoss();
    }

    private void RefreshBoss()
    {
        bool active = boss != null && boss.isActiveAndEnabled && boss.IsAlive;
        bossPanel.EnableInClassList(HiddenClass, !active);
        if (!active)
            return;

        int current = boss.CurrentHealth;
        int max = Mathf.Max(1, boss.MaxHealth);
        if (current == lastBossHealth && max == lastBossMaxHealth)
            return;

        lastBossHealth = current;
        lastBossMaxHealth = max;
        bossHealth.text = $"{current} / {max}";
        bossHealthFill.style.width = Length.Percent(100f * Mathf.Clamp01(current / (float)max));
    }

    public void ShowResult(bool won)
    {
        victory = won;
        SetBoss(null);
        RefreshResult();
    }

    private void RefreshResult()
    {
        if (!bound)
            return;

        resultPanel.EnableInClassList(HiddenClass, !victory.HasValue);
        if (!victory.HasValue)
            return;

        bool won = victory.Value;
        resultCard.EnableInClassList("victory", won);
        resultTitle.text = won ? "通关" : "本局结束";
        resultDetails.text = won ? "选择是否带着本局道具重开。" : "重开后从第 1 波开始。";
        restartKeepingItems.EnableInClassList(HiddenClass, !won);
        restartFresh.text = won ? "从零重开" : "重新开始";
        restartFresh.EnableInClassList("run-result-button-primary", !won);
    }

    private void OnRestartFresh()
    {
        if (director != null)
            director.RestartFresh();
    }

    private void OnRestartKeepingItems()
    {
        if (director != null)
            director.RestartKeepingItems();
    }
}
