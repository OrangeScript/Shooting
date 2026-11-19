using UnityEngine;

[RequireComponent(typeof(Enemy))]
public class EnemyStealth : MonoBehaviour
{
    [SerializeField] private Transform visualRoot;

    [Tooltip("X为最短时间，Y为最长时间")]
    [SerializeField] private Vector2 visibleDuration =
        new Vector2(0.8f, 1.3f);

    [SerializeField] private Vector2 hiddenDuration =
        new Vector2(0.3f, 0.5f);

    private Enemy enemy;
    private Renderer[] renderers = new Renderer[0];
    private bool[] originalStates = new bool[0];

    private bool hidden;
    private bool suspended;
    private float changeAt;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();

        if (visualRoot == null)
        {
            Debug.LogError("EnemyStealth需要绑定Visual Root。", this);
            enabled = false;
            return;
        }

        renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
        originalStates = new bool[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
            originalStates[i] = renderers[i].enabled;
    }

    private void OnEnable()
    {
        enemy.Spawned += ResetForSpawn;
    }

    private void OnDisable()
    {
        if (enemy == null)
            return;

        enemy.Spawned -= ResetForSpawn;
        SetHidden(false);
    }

    private void ResetForSpawn()
    {
        suspended = false;
        SetHidden(false);
        Schedule(visibleDuration);
    }

    private void Update()
    {
        if (!enemy.IsAlive || suspended || Time.time < changeAt)
            return;

        SetHidden(!hidden);

        Schedule(hidden ? hiddenDuration : visibleDuration);
    }

    private void Schedule(Vector2 duration)
    {
        float minimum = Mathf.Max(0.05f, duration.x);
        float maximum = Mathf.Max(minimum, duration.y);

        changeAt = Time.time + Random.Range(minimum, maximum);
    }

    private void SetHidden(bool value)
    {
        hidden = value;
        enemy.SetInvulnerable(value);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].enabled = !value && originalStates[i];
        }

        if (value || !enemy.IsAlive || !enemy.isActiveAndEnabled)
            EnemyHealthUI.Instance?.Unregister(enemy);
        else
            EnemyHealthUI.Instance?.Register(enemy);
    }

    // 显形，并停止本次生命期间的自动隐身。
    public void RevealAndSuspend()
    {
        suspended = true;
        SetHidden(false);
    }
}