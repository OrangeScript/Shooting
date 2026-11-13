
using UnityEngine;

[RequireComponent(typeof(Enemy))]
public class EnemyForwardMovement : MonoBehaviour
{
    [SerializeField] private float speed = 2f;
    [SerializeField] private float endZ = -1f;

    private Enemy enemy;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    private void Update()
    {
        if (!enemy.IsAlive)
            return;

        transform.position +=
            Vector3.back * speed * Time.deltaTime;

        if (transform.position.z <= endZ)
            enemy.ReachEnd();
    }
}
