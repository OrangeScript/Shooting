
using System;
using Unity.Netcode;
using Unity.Netcode.Components;
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
    [Header("Jump")]
    [SerializeField] private float jumpHeight = 1.8f;

    private bool jumpRequested;
    private NetworkAnimator networkAnimator;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 14f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;

    [Header("Attack")]
    [SerializeField] private float attackCooldown = 0.5f;
    PlayerCombat combat;

    private CharacterController controller;
    private Vector2 moveInput;
    private Vector2 touchMoveInput;
    private PlayerInput playerInput;
    private float verticalVelocity;

    private Vector3 dashDirection;
    private float dashTimeRemaining;
    private float nextDashTime;
    private float nextAttackTime;

    private PlayerInventory inventory;
    private PlayerHealth health;
    private Vector3 forcedMoveDirection;
    private float forcedMoveDistance;
    private float forcedMoveDuration;
    private float forcedMoveElapsed;
    private bool forcedMoveIsKnockback;
    private float serverKnockbackUntil;

    private bool HasForcedMovement => forcedMoveDistance > 0f && forcedMoveElapsed < forcedMoveDuration;
    public bool IsKnockedBack => (IsServer && Time.time < serverKnockbackUntil) ||
        (IsOwner && forcedMoveIsKnockback && HasForcedMovement);

    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
        health = GetComponent<PlayerHealth>();
        controller = GetComponent<CharacterController>();
        combat = GetComponent<PlayerCombat>();
        playerInput = GetComponent<PlayerInput>();
        playerInput.enabled = false;
        networkAnimator = GetComponent<NetworkAnimator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }
    public void OnJump(InputValue value)
    {
        if (value.isPressed)
            RequestJump();
    }
    public void OnKick(InputValue inputValue)
    {
        if (inputValue.isPressed && !GameHUDController.IsPointerOverTouchControls())
            RequestKick();
    }

    public void SetTouchMoveInput(Vector2 input)
    {
        if (IsSpawned && IsOwner)
            touchMoveInput = Vector2.ClampMagnitude(input, 1f);
    }

    private bool CanUseAction => IsSpawned && IsOwner && !IsKnockedBack &&
        (health == null || !health.IsDead);

    public void RequestJump()
    {
        if (CanUseAction)
            jumpRequested = true;
    }

    public void RequestKick()
    {
        if (CanUseAction && networkAnimator != null)
            networkAnimator.SetTrigger("Kick");
    }

    public void RequestAttack()
    {
        if (CanUseAction)
            combat?.TryAttack();
    }

    public override void OnNetworkSpawn()
    {
        // 只有角色拥有者启用输入
        playerInput.enabled = IsOwner;

        moveInput = Vector2.zero;
        touchMoveInput = Vector2.zero;
        verticalVelocity = 0f;
        dashTimeRemaining = 0f;
        ResetCombatMovement();
    }

    public override void OnNetworkDespawn()
    {
        playerInput.enabled = false;
        moveInput = Vector2.zero;
        touchMoveInput = Vector2.zero;
        dashTimeRemaining = 0f;
        ResetCombatMovement();
    }
    public void OnMove(InputValue value)
    {
        if (!IsSpawned || !IsOwner) return;

        moveInput = value.Get<Vector2>();
    }


    public void OnSAttack(InputValue inputValue)
    {
        if (!IsSpawned || !IsOwner) return;
        if (!inputValue.isPressed) return;

        combat?.TrySAttack();
    }

    public void OnAttack(InputValue value)
    {
        if (value.isPressed && !GameHUDController.IsPointerOverTouchControls())
            RequestAttack();
    }

    public void OnDash(InputValue value)
    {
        if (!IsSpawned || !IsOwner) return;
        if (!value.isPressed) return;
        if (HasForcedMovement) return;

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

    private void ResetCombatMovement()
    {
        forcedMoveDistance = 0f;
        forcedMoveDuration = 0f;
        forcedMoveElapsed = 0f;
        forcedMoveDirection = Vector3.zero;
        forcedMoveIsKnockback = false;
        serverKnockbackUntil = 0f;
        jumpRequested = false;
    }

    internal void BeginAttackLunge(Vector3 direction, float distance, float duration)
    {
        if (!IsSpawned || !IsOwner || IsKnockedBack || (health != null && health.IsDead))
            return;

        BeginCombatMovement(direction, distance, duration, false);
    }

    internal void CancelAttackLunge()
    {
        if (!forcedMoveIsKnockback)
            forcedMoveDistance = 0f;
    }

    // 服务器决定击退，目标 owner 执行 Move，位置再由已有 NetworkTransform 同步。
    public void ServerApplyKnockback(Vector3 direction, float distance, float duration)
    {
        if (!IsSpawned || !IsServer || distance <= 0f || (health != null && health.IsDead))
            return;

        direction = Vector3.ProjectOnPlane(direction, Vector3.up);
        if (direction.sqrMagnitude < 0.001f)
            return;

        duration = Mathf.Max(0.01f, duration);
        serverKnockbackUntil = Time.time + duration;
        var ownerRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } }
        };
        ApplyKnockbackClientRpc(direction.normalized, distance, duration, ownerRpcParams);
    }

    [ClientRpc]
    private void ApplyKnockbackClientRpc(Vector3 direction, float distance, float duration,
        ClientRpcParams clientRpcParams = default)
    {
        if (!IsSpawned || !IsOwner || (health != null && health.IsDead))
            return;

        BeginCombatMovement(direction, distance, duration, true);
        jumpRequested = false;
    }

    private void BeginCombatMovement(Vector3 direction, float distance, float duration, bool knockback)
    {
        direction = Vector3.ProjectOnPlane(direction, Vector3.up);
        if (direction.sqrMagnitude < 0.001f || distance <= 0f)
            return;

        forcedMoveDirection = direction.normalized;
        forcedMoveDistance = distance;
        forcedMoveDuration = Mathf.Max(0.01f, duration);
        forcedMoveElapsed = 0f;
        forcedMoveIsKnockback = knockback;
        dashTimeRemaining = 0f;
    }

    
private void MoveCharacter()
{
    float dt = Time.deltaTime;

    bool dead = health != null && health.IsDead;
    if (dead)
    {
        ResetCombatMovement();
        dashTimeRemaining = 0f;
    }
    combat?.UpdateAttackMovement(dt);
    bool forcedMovement = HasForcedMovement;
    bool knockedBack = forcedMovement && forcedMoveIsKnockback;

    Vector3 moveDirection = dead ? Vector3.zero : GetMoveDirection();

    bool isDashing = dashTimeRemaining > 0f;

    Vector3 direction = isDashing
        ? dashDirection
        : moveDirection;
    if (forcedMovement)
        direction = forcedMoveDirection;

    float speed = isDashing
        ? dashSpeed
        : moveSpeed;

    speed *= inventory != null
        ? inventory.SpeedMultiplier
        : 1f;

    if (isDashing)
        dashTimeRemaining -= dt;

    // 1. 角色转向
    if (!knockedBack && direction.sqrMagnitude > 0.001f)
    {
        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * dt
        );
    }

    // 2. 根据上一次 Move 判断当前是否站在地面
    bool wasGrounded = controller.isGrounded;

    if (wasGrounded && verticalVelocity < 0f)
    {
        verticalVelocity = -2f;
    }

    // 3. 跳跃
    if (jumpRequested && wasGrounded && !knockedBack && !dead)
    {
        // 根据目标高度计算起跳初速度
        verticalVelocity = Mathf.Sqrt(
            jumpHeight * -2f * gravity
        );

        // 同步起跳动画 Trigger
        if (networkAnimator != null)
        {
            networkAnimator.SetTrigger("Jump");
        }
    }

    // 本帧处理完输入后清除请求
    jumpRequested = false;

    // 4. 重力始终作用
    verticalVelocity += gravity * dt;

    Vector3 horizontalDisplacement = direction * speed * dt;
    if (forcedMovement)
    {
        // 累计 ease-out 进度，末帧只移动余量，不受道具移速倍率影响。
        float previousProgress = forcedMoveElapsed / forcedMoveDuration;
        forcedMoveElapsed = Mathf.Min(forcedMoveDuration, forcedMoveElapsed + dt);
        float progress = forcedMoveElapsed / forcedMoveDuration;
        float distance = forcedMoveDistance *
            (progress * (2f - progress) - previousProgress * (2f - previousProgress));
        horizontalDisplacement = forcedMoveDirection * distance;
    }

    // 5. 只有一次 CharacterController.Move
    CollisionFlags flags =
        controller.Move(horizontalDisplacement + Vector3.up * verticalVelocity * dt);

    // 6. Move 之后获得最新的地面接触状态
    bool groundedNow =
        (flags & CollisionFlags.Below) != 0;

    if (groundedNow && verticalVelocity < 0f)
    {
        verticalVelocity = -2f;
    }

    // 7. 驱动 Animator
    if (animator != null)
    {
        float animationSpeed =
            !forcedMovement && direction.sqrMagnitude > 0.001f
                ? 1f
                : 0f;

        animator.SetFloat(
            "Speed",
            animationSpeed,
            0.1f,
            dt
        );

        animator.SetBool("Grounded", groundedNow);
    }
}


    private Vector3 GetMoveDirection()
    {
        if (touchMoveInput.sqrMagnitude > 0.0001f)
        {
            // 摇杆方向跟随画面，切换场景后重新取得当前相机。
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            Vector3 forward = cameraTransform != null
                ? Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized
                : Vector3.forward;
            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            return Vector3.ClampMagnitude(
                right * touchMoveInput.x + forward * touchMoveInput.y, 1f);
        }

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
