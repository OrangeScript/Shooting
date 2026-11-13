using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(Enemy))]
[RequireComponent(typeof(EnemyMeleeAttack))]
public class DemonMovement : MonoBehaviour
{
    private enum Phase
    {
        Approach,
        Chase
    }

    [Header("移动")]
    [SerializeField] private float approachSpeed = 1.2f;
    [SerializeField] private float chaseSpeed = 3f;

    [Tooltip("停在玩家前方多少个世界单位")]
    [SerializeField] private float frontDistance = 1.8f;

    private Enemy enemy;
    private EnemyMeleeAttack meleeAttack;
    private Transform player;

    private Phase phase;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        meleeAttack = GetComponent<EnemyMeleeAttack>();
    }

    private void OnEnable()
    {
        // 从对象池重新生成时，恢复第一阶段
        phase = Phase.Approach;
        player = null;
    }

    private void Update()
    {
        if (!enemy.IsAlive)
            return;

        if (player == null)
        {
            GamerController controller =
                FindObjectOfType<GamerController>();

            if (controller == null)
                return;

            player = controller.transform;
        }

        // 正在挥拳时停止移动
        if (meleeAttack.IsAttacking)
            return;

        float targetZ = player.position.z + frontDistance;

        switch (phase)
        {
            case Phase.Approach:
                UpdateApproach(targetZ);
                break;

            case Phase.Chase:
                UpdateChase(targetZ);
                break;
        }
    }

    private void UpdateApproach(float targetZ)
    {
        Vector3 position = transform.position;

        position.z = Mathf.MoveTowards(
            position.z,
            targetZ,
            approachSpeed * Time.deltaTime
        );

        transform.position = position;

        if (Mathf.Abs(position.z - targetZ) < 0.02f)
        {
            phase = Phase.Chase;
            Debug.Log("MiniBoss 进入追击阶段");
        }
    }

    private void UpdateChase(float targetZ)
    {
        Vector3 position = transform.position;

        // 横向追逐主角
        position.x = Mathf.MoveTowards(
            position.x,
            player.position.x,
            chaseSpeed * Time.deltaTime
        );

        // 始终尽量保持在玩家正前方
        position.z = Mathf.MoveTowards(
            position.z,
            targetZ,
            approachSpeed * Time.deltaTime
        );

        transform.position = position;

        // 进入近战范围就尝试攻击
        if (meleeAttack.IsInRange(player))
            meleeAttack.TryAttack(player);
    }
}
