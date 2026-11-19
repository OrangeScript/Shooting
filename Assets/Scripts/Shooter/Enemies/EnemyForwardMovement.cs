
using System;
using UnityEngine;

[RequireComponent(typeof(Enemy))]
public class EnemyForwardMovement : MonoBehaviour
{
    [SerializeField] private float speed = 2f;
    [SerializeField] private float endZ = -1f;
    [Tooltip("普通怪保留开启；门关闭，改为真实接触时扣血。")]
    [SerializeField] private bool damageAtEnd = true;

    private Enemy enemy;
    public Func<Vector3, ShooterPlayerHealth> SelectEndTarget { get; set; }

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    private void Update()
    {
        if (!enemy.IsAlive)
            return;

        transform.position +=
            Vector3.back * enemy.Stats.ScaleMoveSpeed(speed) * Time.deltaTime;

        if (transform.position.z > endZ)
            return;

        if (!damageAtEnd)
        {
            enemy.Despawn();
            return;
        }

        ShooterPlayerHealth player = SelectEndTarget != null
            ? SelectEndTarget(transform.position)
            : FindObjectOfType<ShooterPlayerHealth>();

        if (player != null)
            enemy.ReachEnd(player);
        else
            enemy.Despawn();
    }
}
