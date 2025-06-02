using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInputs : MonoBehaviour, PlayerControl.IBasicPlayerActions
{
    public static PlayerInputs Instance { get; private set; }
    private PlayerControl controls;
    [SerializeField] GameObject character_object;
    public Vector2 moveInput { get; private set; }
    // 用于判断动画的接口
    public static bool isUpLeft, isUpRight, isDownLeft, isDownRight;

    void Awake()
    {
        controls = new PlayerControl();
        controls.BasicPlayer.SetCallbacks(this);
        if (Instance != null)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    void OnEnable()  => controls.Enable();
    void OnDisable() => controls.Disable();

    public void OnMoving(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (context.performed)
            moveInput = context.ReadValue<Vector2>();
        else if (context.canceled)
            moveInput = Vector2.zero;
    }

    void Update()
    {
        // 检测输入方向
        bool left  = moveInput.x < -0.1f;
        bool right = moveInput.x >  0.1f;
        bool up    = moveInput.y >  0.1f;
        bool down  = moveInput.y < -0.1f;

        // 初始化动画接口
        isUpLeft = isUpRight = isDownLeft = isDownRight = false;

        // 只左右移动
        if (left && !right)
        {
            if (up && !down)
            {
                isUpLeft = true;     // 动画调用点
            }
            else if (down && !up)
            {
                isDownLeft = true;
            }
        }
        else if (right && !left)
        {
            if (up && !down)
            {
                isUpRight = true;
            }
            else if (down && !up)
            {
                isDownRight = true;
            }
        }
    }
}
