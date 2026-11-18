
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Enemy))]
public class EnemyApproachMovement : MonoBehaviour
{
    [SerializeField] private float speed = 1.5f;
    [SerializeField] private float stopZ = 9f;
    [SerializeField] private UnityEvent onArrived = new UnityEvent();

    private Enemy enemy;
    private bool arrived;

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
    }

    private void ResetForSpawn()
    {
        arrived = false;
    }

    private void Update()
    {
        if (!enemy.IsAlive || arrived)
            return;

        Vector3 position = transform.position;

        if (position.z > stopZ)
        {
            position.z = Mathf.MoveTowards(
                position.z,
                stopZ,
                speed * Time.deltaTime
            );

            transform.position = position;
        }

        if (position.z <= stopZ + 0.01f)
        {
            arrived = true;
            onArrived.Invoke();
        }
    }
}
