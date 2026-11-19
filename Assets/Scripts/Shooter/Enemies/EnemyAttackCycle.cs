using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Enemy))]
public class EnemyAttackCycle : MonoBehaviour
{
    private enum Phase
    {
        Waiting,
        Warning,
        Firing,
        Cooldown
    }

    [SerializeField, Min(0.05f)] private float warningTime = 1f;
    [SerializeField, Min(0.05f)] private float firingTime = 2f;
    [SerializeField, Min(0.05f)] private float cooldownTime = 3f;

    [SerializeField] private UnityEvent onWarning = new UnityEvent();
    [SerializeField] private UnityEvent onFire = new UnityEvent();
    [SerializeField] private UnityEvent onCooldown = new UnityEvent();
    [SerializeField] private UnityEvent onStopped = new UnityEvent();

    private Enemy enemy;
    private Phase phase;
    private float changeAt;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    private void OnEnable()
    {
        enemy.Spawned += ResetForSpawn;
    }

    private void OnDisable()
    {
        enemy.Spawned -= ResetForSpawn;
        StopCycle();
    }

    private void ResetForSpawn()
    {
        phase = Phase.Waiting;
        changeAt = 0f;
    }

    public void BeginCycle()
    {
        if (!isActiveAndEnabled || enemy == null ||
            !enemy.IsAlive || phase != Phase.Waiting)
            return;

        Enter(Phase.Warning, warningTime, onWarning);
    }

    public void StopCycle()
    {
        if (phase == Phase.Waiting)
            return;

        phase = Phase.Waiting;
        onStopped.Invoke();
    }

    private void Update()
    {
        if (!enemy.IsAlive)
        {
            StopCycle();
            return;
        }

        if (phase == Phase.Waiting || Time.time < changeAt)
            return;

        switch (phase)
        {
            case Phase.Warning:
                Enter(Phase.Firing, firingTime, onFire);
                break;

            case Phase.Firing:
                Enter(Phase.Cooldown, enemy.Stats.ScaleCooldown(cooldownTime), onCooldown);
                break;

            case Phase.Cooldown:
                Enter(Phase.Warning, warningTime, onWarning);
                break;
        }
    }

    private void Enter(
        Phase nextPhase,
        float duration,
        UnityEvent callback)
    {
        phase = nextPhase;
        changeAt = Time.time + Mathf.Max(0.05f, duration);
        callback.Invoke();
    }
}
