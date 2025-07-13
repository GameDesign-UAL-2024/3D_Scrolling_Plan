using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PlayerInputs : MonoBehaviour
{
    public static PlayerInputs Instance { get; private set; }
    private PlayerControl controls;
    [SerializeField] GameObject character_object;
    public Vector2 moveInput { get; private set; }
    public Vector2 rawMoveInput { get; private set; } // 添加原始输入值
    public enum  InstructionKeys{ UP, DOWN, LEFT, RIGHT, ATTACK, SKILL };
    Dictionary<float, InstructionKeys> Instructions;

    public enum InputDeviceType
    {
        KeyboardMouse,
        Gamepad
    }
    public InputDeviceType currentInputDevice = InputDeviceType.KeyboardMouse;
    public static event UnityAction OnJumpPressed;
    public static event UnityAction OnJumpReleased;

    // 添加设备切换事件
    public static event UnityAction<InputDeviceType> OnInputDeviceChanged;

    void Awake()
    {
        controls = new PlayerControl();
        controls.BasicPlayer.Moving.performed += OnMoving;
        controls.BasicPlayer.Moving.canceled += OnMoving;
        controls.BasicPlayer.Jump.performed += OnJumpPerformed;
        controls.BasicPlayer.Jump.canceled += OnJumpCanceled;

        if (Instance != null)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        // 监听设备变化
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        // 设备变化时立即检测当前设备
        DetectInputDevice();
    }

    void OnEnable() => controls.Enable();
    void OnDisable() => controls.Disable();

    public void OnAttack(InputAction.CallbackContext context)
    {

    }
    public void OnMoving(InputAction.CallbackContext context)
    {
        rawMoveInput = context.ReadValue<Vector2>(); // 存储原始输入
        moveInput = context.ReadValue<Vector2>(); // 初始值设为原始输入
    }

    public void OnMovingCancel(InputAction.CallbackContext context)
    {
        rawMoveInput = Vector2.zero;
        moveInput = Vector2.zero;
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        OnJumpPressed?.Invoke(); // 触发跳跃按下事件
    }

    private void OnJumpCanceled(InputAction.CallbackContext context)
    {
        OnJumpReleased?.Invoke(); // 触发跳跃释放事件
    }

    void Update()
    {
        DetectInputDevice();

        // 添加输入平滑处理
        if (currentInputDevice == InputDeviceType.KeyboardMouse)
        {
            // 键盘鼠标使用平滑过渡
            moveInput = Vector2.Lerp(moveInput, rawMoveInput, 0.1f);
            moveInput = new Vector2(FixFloat(moveInput.x), FixFloat(moveInput.y));
        }
        else
        {
            // 手柄直接使用原始输入（手柄自带摇杆平滑）
            moveInput = rawMoveInput;
        }
    }

    void DetectInputDevice()
    {
        // 检测手柄输入
        if (Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame)
        {
            if (IsGamepadActive() && currentInputDevice != InputDeviceType.Gamepad)
            {
                SetInputDevice(InputDeviceType.Gamepad);
            }
        }

        // 检测键盘鼠标输入
        if ((Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) ||
            (Mouse.current != null && Mouse.current.delta.ReadValue() != Vector2.zero))
        {
            if (currentInputDevice != InputDeviceType.KeyboardMouse)
            {
                SetInputDevice(InputDeviceType.KeyboardMouse);
            }
        }
    }
    public static float FixFloat(float value)
    {
        // 确保是有效数字
        if (float.IsNaN(value) || float.IsInfinity(value))
            return 0f;

        // 四舍五入到4位小数
        return (float)System.Math.Round(value, 4);
    }
    void SetInputDevice(InputDeviceType deviceType)
    {
        if (currentInputDevice != deviceType)
        {
            currentInputDevice = deviceType;
            OnInputDeviceChanged?.Invoke(deviceType);
            Debug.Log($"输入设备切换到: {deviceType}");
        }
    }

    bool IsGamepadActive()
    {
        var gamepad = Gamepad.current;
        return gamepad.leftStick.ReadValue().magnitude > 0.1f ||
               gamepad.rightStick.ReadValue().magnitude > 0.1f ||
               gamepad.buttonSouth.wasPressedThisFrame ||
               gamepad.buttonEast.wasPressedThisFrame ||
               gamepad.buttonWest.wasPressedThisFrame ||
               gamepad.buttonNorth.wasPressedThisFrame;
    }
}

[Serializable]
public class InstructionSets
{

    [Tooltip("指令序列(最多7个)")]
    public PlayerInputs.InstructionKeys[] sequence = new PlayerInputs.InstructionKeys[0]; // 初始化为空数组

    [Tooltip("允许的最大输入间隔时间（秒）")]
    [Range(0.1f, 2f)]
    public float maxInterval = 0.5f;

    [Tooltip("指令描述（可选）")]
    public string description = "New Command";
}
