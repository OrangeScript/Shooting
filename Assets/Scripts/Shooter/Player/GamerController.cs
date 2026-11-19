using System;
using UnityEngine.InputSystem;
using UnityEngine;

public class GamerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    private CharacterController controller;
    private float horizontalInput;
    private float roadHeight;
    private ShooterPlayerHealth health;
    private ShooterCombatStats combatStats;
    private float laneCenter;
    private float laneHalfWidth = float.PositiveInfinity;
    public event Action<float> MoveInputChanged;

    public float BaseMoveSpeed => moveSpeed;
    public float EffectiveMoveSpeed => moveSpeed *
        (combatStats != null ? combatStats.MoveSpeedMultiplier : 1f);

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        roadHeight = transform.position.y;
        health = GetComponent<ShooterPlayerHealth>();
        combatStats = GetComponent<ShooterCombatStats>();
    }


    // PlayerControls -> Move
    // PlayerInput Behavior = Send Messages
    public void OnMove(InputValue value)
    {

        Vector2 input = value.Get<Vector2>();

        // 只使用水平方向
        if (MoveInputChanged == null)
            SetHorizontalInput(input.x);
        else
            MoveInputChanged.Invoke(input.x);
    }

    public void SetHorizontalInput(float input)
    {
        horizontalInput = float.IsNaN(input) ? 0f : Mathf.Clamp(input, -1f, 1f);
    }

    public void SetLaneBounds(float center, float halfWidth)
    {
        laneCenter = center;
        laneHalfWidth = Mathf.Max(0f, halfWidth);
    }

    private void Update()
    {

        if ((health != null && health.IsDead) || Time.deltaTime <= 0f)
            return;

        // 横移继续处理碰撞。如果角色被抬高，下一帧尝试回到原路面高度。
        float horizontal = horizontalInput * EffectiveMoveSpeed * Time.deltaTime;
        Vector3 position = transform.position;
        horizontal = Mathf.Clamp(horizontal, laneCenter - laneHalfWidth - position.x,
            laneCenter + laneHalfWidth - position.x);
        controller.Move(new Vector3(horizontal, roadHeight - position.y, 0f));
    }
}
