
using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : NetworkBehaviour
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
    PlayerCombat combat;

    private CharacterController controller;
    private Vector2 moveInput;
    private PlayerInput playerInput;
    private float verticalVelocity;

    private Vector3 dashDirection;
    private float dashTimeRemaining;
    private float nextDashTime;
    private float nextAttackTime;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        combat = GetComponent<PlayerCombat>();
        playerInput = GetComponent<PlayerInput>();
        playerInput.enabled = false;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }
    public override void OnNetworkSpawn()
    {
        // 只有角色拥有者启用输入
        playerInput.enabled = IsOwner;

        moveInput = Vector2.zero;
        verticalVelocity = 0f;
        dashTimeRemaining = 0f;
    }

    public override void OnNetworkDespawn()
    {
        playerInput.enabled = false;
        moveInput = Vector2.zero;
        dashTimeRemaining = 0f;
    }
public void OnMove(InputValue value)
    {
        if (!IsSpawned || !IsOwner) return;

        moveInput = value.Get<Vector2>();
    }

    public void OnAttack(InputValue value)
    {
        if (!IsSpawned || !IsOwner) return;
        if (!value.isPressed) return;

        combat?.TryAttack();
    }

    public void OnDash(InputValue value)
    {
        if (!IsSpawned || !IsOwner) return;
        if (!value.isPressed) return;

        if (Time.time < nextDashTime)
            return;

        if (dashTimeRemaining > 0f)
            return;

        Vector3 direction = GetMoveDirection();

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
        if (!IsSpawned || !IsOwner)
            return;

        MoveCharacter();
    }

    private void MoveCharacter()
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

        // 旋转
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

        // 重力
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;

        // 移动
        Vector3 velocity =
            direction * speed +
            Vector3.up * verticalVelocity;

        controller.Move(velocity * Time.deltaTime);

        // 动画
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
            moveInput.y,
            0f,
            -moveInput.x
        );

        // 防止斜向移动速度超过普通速度
        return Vector3.ClampMagnitude(direction, 1f);
    }


}
