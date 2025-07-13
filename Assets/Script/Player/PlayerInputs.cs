using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering;

public class PlayerInputs : MonoBehaviour
{
    public static PlayerInputs Instance { get; private set; }
    private PlayerControl controls;
    [SerializeField] GameObject character_object;
    public Vector2 moveInput { get; private set; }
    public Vector2 rawMoveInput { get; private set; } // 添加原始输入值
    public enum  InstructionKeys{ UP, DOWN, LEFT, RIGHT, ATTACK, SKILL };
    Dictionary<float, InstructionKeys> Instructions;

    private string KeyIconPrefab = "Assets/Resoruces/Prefabs/KeyIcons.prefab";
    GameObject KeyIcon;
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
    TestCanvas canvas;
    void Awake()
    {
        controls = new PlayerControl();
        controls.Player.Moving.performed += OnMoving;
        controls.Player.Moving.canceled += OnMoving;
        controls.Player.Jump.performed += OnJumpPerformed;
        controls.Player.Jump.canceled += OnJumpCanceled;
        controls.Player.BasicAttack.performed += OnAttack;
        controls.Player.SpecialSkill.performed += OnSpecialSkill;
        controls.Player.Skill.performed += OnSkill;
        Application.targetFrameRate = 60;
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
        DetectInputDevice();
        // 监听设备变化
        InputSystem.onDeviceChange += OnDeviceChange;

        KeyIcon = Addressables.LoadAsset<GameObject>(KeyIconPrefab).WaitForCompletion();
        GameObject canvas_obj = GameObject.FindGameObjectWithTag("Canvas");
        if (canvas_obj != null)
        {
            canvas = canvas_obj.GetComponent<TestCanvas>();
        }
    }

     
    void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        // 设备变化时立即检测当前设备
        DetectInputDevice();
    }

    void OnEnable() => controls.Enable();
    void OnDisable() => controls.Disable();
    
    private Queue<TimedInstruction> _instructionCache = new Queue<TimedInstruction>(7);
    public Coroutine InstructionCacheRefreshCoroutine;
    
    public void OnAttack(InputAction.CallbackContext context)
    {
        AddInstructionToCanvas(InstructionKeys.ATTACK);
        _instructionCache.Enqueue(new TimedInstruction(InstructionKeys.ATTACK));
    }
   
    public bool specialSkill { get; private set; }
    private Coroutine specialSkillCoroutine;
    public void OnSpecialSkill(InputAction.CallbackContext context)
    {
        if (specialSkillCoroutine != null)
        {
            StopCoroutine(specialSkillCoroutine);
        }
        specialSkillCoroutine = StartCoroutine(SpecialSkillCoroutine());
    }
    private IEnumerator SpecialSkillCoroutine()
    {
        specialSkill = true;
        int frame_wait = 0;
        while (frame_wait < 5)
        {
            frame_wait += 1;
            yield return new WaitForEndOfFrame();
        }

        specialSkill = false;
    }
    
    public bool skill {  get; private set; }
    private Coroutine skillCoroutine;
    public void OnSkill(InputAction.CallbackContext context)
    {
        _instructionCache.Enqueue(new TimedInstruction(InstructionKeys.SKILL));
        if (skillCoroutine != null)
        {
            StopCoroutine(skillCoroutine);
        }
        skillCoroutine = StartCoroutine(SkillCoroutine());
    }

    private IEnumerator SkillCoroutine()
    {
        skill = true;
        int frame_wait = 0;
        while (frame_wait < 5)
        {
            frame_wait += 1;
            yield return new WaitForEndOfFrame();
        }
        skill = false;        
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
        while (_instructionCache.Count > 0 && _instructionCache.Peek().IsExpired)
        {
            _instructionCache.Dequeue();
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

    void AddInstructionToCanvas(InstructionKeys instruction)
    {
        if (canvas != null)
        {
            GameObject instructionNode = Instantiate(KeyIcon);
            
            instructionNode.GetComponent<KeyIcons>().key_text.text = instruction.ToString();
            canvas.AddInstructionNode(instructionNode);
        }
    }
    public class TimedInstruction
    {
        public InstructionKeys Key { get; }
        public int CreationFrame { get; }
    
        public TimedInstruction(InstructionKeys key)
        {
            Key = key;
            CreationFrame = Time.frameCount;
        }
    
        public bool IsExpired => Time.frameCount - CreationFrame >= 15;
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
