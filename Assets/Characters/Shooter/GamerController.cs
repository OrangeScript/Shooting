using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;

public class GamerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    private CharacterController controller;
    private PlayerInput playerInput;

    private float horizontalInput;
    private float verticalVelocity;
    private ShooterPlayerHealth health;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();
        health = GetComponent<ShooterPlayerHealth>();
    }


    // PlayerControls -> Move
    // PlayerInput Behavior = Send Messages
    public void OnMove(InputValue value)
    {

        Vector2 input = value.Get<Vector2>();

        // 只使用水平方向
        horizontalInput = input.x;
    }

    private void Update()
    {

        if (health != null && health.IsDead)
        return;
        // 仅允许 X 轴移动
        Vector3 velocity = new Vector3(
            horizontalInput * moveSpeed,
            0,
            0f
        );

        controller.Move(velocity * Time.deltaTime);
    }
}
