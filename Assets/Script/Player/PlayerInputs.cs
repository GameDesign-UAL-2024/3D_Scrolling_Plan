using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Script.Player
{
    public class PlayerInputs : MonoBehaviour
    {
        public static PlayerInputs Instance { get; private set; }
        private PlayerControl controls;
        
        public Vector2 moveInput { get; private set; }
        public Vector2 rawMoveInput { get; private set; }
        public enum InstructionKeys { UP, DOWN, LEFT, RIGHT, UPL, UPR, DOWNL, DOWNR, ATTACK, SKILL };
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

        public static event UnityAction<InputDeviceType> OnInputDeviceChanged;
        TestCanvas canvas;
    
        private bool inputLocked = false;
        private float inputLockThreshold = 0.1f;
        private Vector2 pendingRawInput = Vector2.zero;
        
        [SerializeField] GameObject character_object;
        private PlayerObject character_component;
        private bool locked;
        private int lastDirection;
        
        void Awake()
        {
            controls = new PlayerControl();
            controls.Player.Moving.performed += OnMoving;
            controls.Player.Moving.canceled += OnMovingCancel;
            controls.Player.Jump.performed += OnJumpPerformed;
            controls.Player.Jump.canceled += OnJumpCanceled;
            controls.Player.BasicAttack.performed += OnAttack;
            controls.Player.SpecialSkill.performed += OnSpecialSkill;
            controls.Player.Skill.performed += OnSkill;
            controls.Player.Commands.performed += OnCommandPerformed;
            controls.Player.Commands.canceled += OnCommandCancel;
            Application.targetFrameRate = 60;
            
            if (Instance != null) Destroy(this);
            else Instance = this;
        }

        void Start()
        {
            DetectInputDevice();
            InputSystem.onDeviceChange += OnDeviceChange;
            
            if (character_object != null)
            {
                character_component = character_object.GetComponent<PlayerObject>();
            }
            
            KeyIcon = Addressables.LoadAsset<GameObject>(KeyIconPrefab).WaitForCompletion();
            GameObject canvas_obj = GameObject.FindGameObjectWithTag("Canvas");
            if (canvas_obj != null)
            {
                canvas = canvas_obj.GetComponent<TestCanvas>();
            }
        }
    
        void Update()
        {
            DetectInputDevice();
    
            if (currentInputDevice == InputDeviceType.KeyboardMouse)
            {
                moveInput = Vector2.Lerp(moveInput, rawMoveInput, 0.1f);
                moveInput = new Vector2(FixFloat(moveInput.x), FixFloat(moveInput.y));
            }
            else
            {
                moveInput = rawMoveInput;
            }

            if (inputLocked && moveInput.magnitude <= 0.01f)
            {
                inputLocked = false;
                if (pendingRawInput.magnitude > 0.01f)
                {
                    rawMoveInput = pendingRawInput;
                    moveInput = pendingRawInput;
                    pendingRawInput = Vector2.zero;
                }
            }

            CleanExpiredInstructions();

            if (character_component)
            {
                lastDirection = character_component.GetLastDirection();
                locked = character_component.GetLocked();
            }
        }

        void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            DetectInputDevice();
        }

        void OnEnable() => controls.Enable();
        void OnDisable() => controls.Disable();
    
        public Queue<TimedInstruction> instructionCache = new Queue<TimedInstruction>(7);
        public Coroutine InstructionCacheRefreshCoroutine;
        public static event UnityAction OnInstructionCacheRefresh;
        
        public string[] CheckInstructions(InstructionSets[] instructions)
        {
            string[] results = new string[instructions.Length];
    
            if (instructionCache.Count == 0) return results;
    
            TimedInstruction[] cacheArray = instructionCache.ToArray();
            int cacheCount = cacheArray.Length;
    
            InstructionKeys[] keyArray = new InstructionKeys[cacheCount];
            for (int i = 0; i < cacheCount; i++)
            {
                keyArray[i] = cacheArray[i].Key;
            }
    
            var sortedInstructions = instructions
                .Select((ins, index) => new { Instruction = ins, Index = index })
                .OrderBy(x => x.Instruction.sequence.Length)
                .ToArray();
    
            foreach (var item in sortedInstructions)
            {
                InstructionSets ins = item.Instruction;
                int originalIndex = item.Index;
        
                if (ins.sequence.Length > cacheCount || ins.sequence.Length == 0)
                    continue;
        
                if (FindSequence(keyArray, ins.sequence))
                {
                    results[originalIndex] = ins.description;
                    print(results[0]);
                }
            }

            return results;
        }
        
        private bool FindSequence(InstructionKeys[] haystack, InstructionKeys[] needle)
        {
            if (needle.Length == 0) return true;
            if (needle.Length > haystack.Length) return false;
    
            int haystackLen = haystack.Length;
            int needleLen = needle.Length;
    
            for (int i = 0; i <= haystackLen - needleLen; i++)
            {
                bool match = true;
                for (int j = 0; j < needleLen; j++)
                {
                    if (haystack[i + j] != needle[j])
                    {
                        match = false;
                        break;
                    }
                }
        
                if (match) return true;
            }
    
            return false;
        }
        
        public void OnAttack(InputAction.CallbackContext context)
        {
            if (locked)
            {
                AddInstructionToCanvas(InstructionKeys.ATTACK);
                instructionCache.Enqueue(new TimedInstruction(InstructionKeys.ATTACK));
                OnInstructionCacheRefresh?.Invoke();
            }
        }
   
        public bool specialSkill { get; private set; }
        private Coroutine specialSkillCoroutine;
        public void OnSpecialSkill(InputAction.CallbackContext context)
        {
            if (specialSkillCoroutine != null) StopCoroutine(specialSkillCoroutine);
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
            if (skillCoroutine != null) StopCoroutine(skillCoroutine);
            skillCoroutine = StartCoroutine(SkillCoroutine());
            
            if (locked)
            {
                AddInstructionToCanvas(InstructionKeys.SKILL);
                instructionCache.Enqueue(new TimedInstruction(InstructionKeys.SKILL));
                OnInstructionCacheRefresh?.Invoke();
            }
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
            Vector2 newRawInput = context.ReadValue<Vector2>();
        
            if (inputLocked)
            {
                pendingRawInput = newRawInput;
                return;
            }
        
            rawMoveInput = newRawInput;
            moveInput = newRawInput;
        }

        public void OnMovingCancel(InputAction.CallbackContext context)
        {
            if (moveInput.magnitude <= inputLockThreshold) inputLocked = true;
            rawMoveInput = Vector2.zero;
            pendingRawInput = Vector2.zero;
        }

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            OnJumpPressed?.Invoke();
        }

        private void OnJumpCanceled(InputAction.CallbackContext context)
        {
            OnJumpReleased?.Invoke();
        }

        [Header("摇杆输入设置")]
        [SerializeField] private float joystickDeadZone = 0.5f;
        [SerializeField] private float commandCooldown = 0.2f;
        private float lastCommandTime = 0f;
        private bool wasInDeadZone = true;
        private int? lastDirectionZone = null;
        
        private void OnCommandPerformed(InputAction.CallbackContext context)
        {
            if (!locked) return;

            Vector2 input = context.ReadValue<Vector2>();
            InstructionKeys? instruction = null;
    
            if (currentInputDevice == InputDeviceType.Gamepad)
            {
                bool isInDeadZone = input.magnitude < joystickDeadZone;
                int? currentDirectionZone = null;
        
                if (!isInDeadZone)
                {
                    currentDirectionZone = GetDirectionZone(input);
                }
                
                // 修复1：确保区域变化时重置状态
                if (wasInDeadZone && !isInDeadZone)
                {
                    lastDirectionZone = null;
                }
                
                // 修复2：正确处理区域变化
                if (currentDirectionZone.HasValue)
                {
                    bool directionChanged = lastDirectionZone != currentDirectionZone;
                    bool cooldownExpired = Time.time - lastCommandTime >= commandCooldown;
                    
                    if ((wasInDeadZone && !isInDeadZone) || 
                        directionChanged || 
                        (cooldownExpired && !directionChanged))
                    {
                        instruction = GetInstructionFromZone(currentDirectionZone.Value);
                        if (instruction.HasValue)
                        {
                            lastDirectionZone = currentDirectionZone;
                            lastCommandTime = Time.time;
                        }
                    }
                }
                else
                {
                    lastDirectionZone = null;
                }
        
                wasInDeadZone = isInDeadZone;
            }
            else
            {
                if (input.x != 0 && input.y != 0)
                {
                    if (input.y > 0)
                    {
                        instruction = input.x > 0 ? InstructionKeys.UPR : InstructionKeys.UPL;
                    }
                    else
                    {
                        instruction = input.x > 0 ? InstructionKeys.DOWNR : InstructionKeys.DOWNL;
                    }
                }
                else if (input.x != 0)
                {
                    instruction = input.x > 0 ? InstructionKeys.RIGHT : InstructionKeys.LEFT;
                }
                else if (input.y != 0)
                {
                    instruction = input.y > 0 ? InstructionKeys.UP : InstructionKeys.DOWN;
                }
            }
            
            if (instruction.HasValue)
            {
                instructionCache.Enqueue(new TimedInstruction(instruction.Value));
                AddInstructionToCanvas(instruction.Value);
                OnInstructionCacheRefresh?.Invoke();
            }
        }
        
        private int GetDirectionZone(Vector2 input)
        {
            // 修复3：确保处理零向量情况
            if (input.magnitude < 0.01f) return -1;
            
            float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360;
    
            int zone = Mathf.FloorToInt((angle + 22.5f) / 45f) % 8;
            return zone;
        }

        private InstructionKeys? GetInstructionFromZone(int zone)
        {
            switch (zone)
            {
                case 0: return InstructionKeys.RIGHT;
                case 1: return InstructionKeys.UPR;
                case 2: return InstructionKeys.UP;
                case 3: return InstructionKeys.UPL;
                case 4: return InstructionKeys.LEFT;
                case 5: return InstructionKeys.DOWNL;
                case 6: return InstructionKeys.DOWN;
                case 7: return InstructionKeys.DOWNR;
                default: return null;
            }
        }
        
        public void ResetCommandState()
        {
            lastDirectionZone = null;
            wasInDeadZone = true;
        }

        public void OnCommandCancel(InputAction.CallbackContext context)
        {
            if (moveInput.magnitude <= inputLockThreshold) inputLocked = true;
            rawMoveInput = Vector2.zero;
            pendingRawInput = Vector2.zero;
            
            if (currentInputDevice == InputDeviceType.Gamepad) ResetCommandState();
        }
        
        private void CleanExpiredInstructions()
        {
            if (instructionCache.Count == 0) return;
    
            int currentFrame = Time.frameCount;
    
            while (instructionCache.Count > 0)
            {
                var instruction = instructionCache.Peek();
                if (currentFrame - instruction.CreationFrame >= 20)
                {
                    instructionCache.Dequeue();
                }
                else
                {
                    break;
                }
            }
        }
        
        private float lastDeviceCheckTime = 0f;
        private const float DEVICE_CHECK_INTERVAL = 0.1f;
        
        void DetectInputDevice()
        {
            if (Time.time - lastDeviceCheckTime < DEVICE_CHECK_INTERVAL) return;
            lastDeviceCheckTime = Time.time;
    
            if (Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame)
            {
                if (IsGamepadActive() && currentInputDevice != InputDeviceType.Gamepad)
                {
                    SetInputDevice(InputDeviceType.Gamepad);
                }
            }

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
            if (float.IsNaN(value) || float.IsInfinity(value)) return 0f;
            return (float)System.Math.Round(value, 4);
        }
        
        void SetInputDevice(InputDeviceType deviceType)
        {
            if (currentInputDevice != deviceType)
            {
                currentInputDevice = deviceType;
                OnInputDeviceChanged?.Invoke(deviceType);
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

        public bool ChangeCharacter(GameObject character)
        {
            if (character.TryGetComponent<PlayerObject>(out PlayerObject _character_comp))
            {
                character_object = character;
                character_component = _character_comp;
                return true;
            }
            return false;
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
    
            public bool IsExpired => Time.frameCount - CreationFrame >= 20;
        }
    }

    [Serializable]
    public class InstructionSets
    {
        [Tooltip("指令序列(最多7个)")]
        public PlayerInputs.InstructionKeys[] sequence = new PlayerInputs.InstructionKeys[0];

        [Tooltip("允许的最大输入间隔时间（秒）")]
        [Range(0.1f, 2f)]
        public float maxInterval = 0.5f;

        [Tooltip("指令描述（可选）")]
        public string description = "New Command";
    }
}