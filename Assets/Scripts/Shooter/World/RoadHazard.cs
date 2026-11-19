using UnityEngine;

[RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
public sealed class RoadHazard : MonoBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 3f;
    [SerializeField] private float endZ = -3f;
    [SerializeField, Min(1)] private int damage = 2;
    [SerializeField] private bool lethal;

    private Rigidbody body;
    private bool hitPlayer;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        Vector3 next = body.position;
        next.z -= moveSpeed * Time.fixedDeltaTime;

        if (next.z <= endZ)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        body.MovePosition(next);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActiveAndEnabled || hitPlayer)
            return;

        var player = other.GetComponentInParent<ShooterPlayerHealth>();
        if (player == null || player.IsDead)
            return;

        hitPlayer = true;

        if (lethal)
            player.Kill();
        else
            player.TakeDamage(damage);
    }
}