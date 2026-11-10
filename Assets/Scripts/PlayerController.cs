
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Animator animator;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 12f;
    [SerializeField] private float gravity = -20f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 14f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;

    [Header("Attack")]
    [SerializeField] private float attackCooldown = 0.5f;

    private CharacterController controller;
    private Vector2 moveInput;
    private float verticalVelocity;

    private Vector3 dashDirection;
    private float dashTimeRemaining;
    private float nextDashTime;
    private float nextAttackTime;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    // Input System 通过 Send Messages 调用
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnAttack(InputValue value)
    {
        if (!value.isPressed || Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + attackCooldown;

        if (animator != null)
            animator.SetTrigger("Attack");
    }

    public void OnDash(InputValue value)
    {
        if (!value.isPressed || Time.time < nextDashTime)
            return;

        if (dashTimeRemaining > 0f)
            return;

        Vector3 direction = GetMoveDirection();

        // 不按移动键时，沿角色当前朝向冲刺
        if (direction.sqrMagnitude < 0.001f)
            direction = transform.forward;

        dashDirection = Vector3.ProjectOnPlane(
            direction, Vector3.up
        ).normalized;

        dashTimeRemaining = dashDuration;
        nextDashTime = Time.time + dashCooldown;
    }

    private void Update()
    {
        Vector3 moveDirection = GetMoveDirection();

        bool isDashing = dashTimeRemaining > 0f;

        Vector3 direction = isDashing
            ? dashDirection
            : moveDirection;

        float speed = isDashing
            ? dashSpeed
            : moveSpeed;

        if (isDashing)
            dashTimeRemaining -= Time.deltaTime;

        // 1. 角色朝移动方向转身
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        // 2. 重力
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;

        // 3. 水平移动 + 竖直速度
        Vector3 velocity =
            direction * speed +
            Vector3.up * verticalVelocity;

        controller.Move(velocity * Time.deltaTime);

        // 4. 驱动动画
        if (animator != null)
        {
            float animationSpeed =
                direction.sqrMagnitude > 0.001f ? 1f : 0f;

            animator.SetFloat(
                "Speed",
                animationSpeed,
                0.1f,
                Time.deltaTime
            );
        }
    }

    private Vector3 GetMoveDirection()
    {
        // W → 世界 +Z
        // S → 世界 -Z
        // A → 世界 -X
        // D → 世界 +X

        Vector3 direction = new Vector3(
            moveInput.x,
            0f,
            moveInput.y
        );

        // 防止斜向移动速度超过普通速度
        return Vector3.ClampMagnitude(direction, 1f);
    }


}
