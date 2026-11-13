
using UnityEngine;

[RequireComponent(typeof(Enemy))]
public class EnemyApproachMovement : MonoBehaviour
{
    [SerializeField] private float speed = 1.5f;
    [SerializeField] private float stopZ = 9f;

    private Enemy enemy;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    private void Update()
    {
        if (!enemy.IsAlive)
            return;

        Vector3 position = transform.position;

        if (position.z <= stopZ)
            return;

        position.z = Mathf.MoveTowards(
            position.z,
            stopZ,
            speed * Time.deltaTime
        );

        transform.position = position;
    }
}
