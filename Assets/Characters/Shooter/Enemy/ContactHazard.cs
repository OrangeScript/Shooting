using UnityEngine;

[RequireComponent(typeof(Enemy))]
public class ContactHazard : MonoBehaviour
{
    private Enemy enemy;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!enemy.IsAlive)
            return;

        ShooterPlayerHealth player =
            other.GetComponentInParent<ShooterPlayerHealth>();

        if (player == null || player.IsDead)
            return;

        // 复用已有的扣血和回收逻辑，不触发击杀掉落。
        enemy.ReachEnd(player);
    }
}