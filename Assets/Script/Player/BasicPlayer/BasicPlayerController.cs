using System;
using UnityEngine;

namespace Script.Player.BasicPlayer
{
    [RequireComponent(typeof(Animator))]
    public class BasicPlayerController : PlayerObject
    {
        [Header("移动参数")]
        [SerializeField] Rigidbody RB;
        [Header("地面监测")]
        [SerializeField] private float groundCheckDistance = 0.2f;
        [SerializeField] private Vector3 groundCheckOffset = new Vector3(0, -0.1f, 0);
        [SerializeField] private LayerMask groundLayer;
        [Header("斜面")]
        [SerializeField] private float maxSlopeAngle = 45f;
        [SerializeField] private float slopeCheckDistance = 0.5f;
        // 速度参数
        [Header("速度参数")]
        [SerializeField] private float minSpeed = 1.5f;
        [SerializeField] private float midSpeed = 2f;
        [SerializeField] private float maxSpeed = 3.5f;
        // 地面状态属性
        public bool IsGrounded { get; private set; }
        public bool IsOnSlope { get; private set; }
        public Vector3 SlopeDirection { get; private set; }
        bool IsJump;
        [Header("参数")]
        [SerializeField] float target_speed = 3;
        [SerializeField] bool Locking = false;
        float current_speed;
        [SerializeField] float jump_force = 5;
        int jump_count = 1;
        private float lastGroundedTime;
        private float lastJumpPressTime;
        [SerializeField] private float jumpBufferTime = 0.1f;
        [SerializeField] private float maxJumpTime = 0.5f; // 最大跳跃持续时间
        private RaycastHit groundHit;
        private int lastDirection = 1;  // 1=右，-1=左
        private float groundDistance; // 存储地面距离
        private float jumpStartTime; // 跳跃开始时间
        Vector2 input = Vector2.zero;
        private Animator animator;
        [SerializeField] private float inputSmoothTime = 0.1f; // 输入平滑时间
        private CommandConfig instructionConfig;
        private void Awake()
        {
            animator = GetComponent<Animator>();
            PlayerInputs.OnJumpPressed += JumpStart;
            PlayerInputs.OnJumpReleased += JumpEnd;
            PlayerInputs.OnInstructionCacheRefresh += CheckCommands;
        }

        private void Start()
        {
            instructionConfig = GetComponentInChildren<CommandConfig>();
        }
        
        private void FixedUpdate()
        {
            // 1. 读取输入（统一平滑处理）
            float smoothFactor = Mathf.Clamp01(Time.fixedDeltaTime / inputSmoothTime);
            input = Vector2.Lerp (input,PlayerInputs.Instance.moveInput,0.2f);
            input = new Vector2(PlayerInputs.FixFloat(input.x), PlayerInputs.FixFloat(input.y));
            // 2. 计算水平输入方向并翻转朝向
            if (!Locking)
            {
                if (input.x > 0.01f) { SetFacing(1); }
                else if (input.x < -0.01f) { SetFacing(-1); }
            }

            if (IsJump)
            {
                if (Time.time - lastJumpPressTime > maxJumpTime)
                {
                    IsJump = false;
                }
            }
            animator.SetFloat("MoveInputX", Locking ? (lastDirection > 0 ? input.x : input.x * -1) : Mathf.Abs(input.x));
            animator.SetFloat("MoveInputY", input.y);
            animator.SetFloat("VelocityY", PlayerInputs.FixFloat(RB.velocity.y));
            animator.SetBool("Jump", IsJump);
            animator.SetBool("OnGround", IsGrounded);
            PerformGroundCheck();
        }

        void JumpStart()
        {
            bool hasJumpBuffer = Time.time - lastJumpPressTime <= jumpBufferTime;

            if (IsGrounded && !hasJumpBuffer && !IsJump)
            {
                IsJump = true;
                lastJumpPressTime = Time.time;
            }
            else if (!IsGrounded && !hasJumpBuffer && !IsJump && jump_count > 0)
            {
                jump_count -= 1;
                IsJump = true;
                lastJumpPressTime = Time.time;
                animator.Play("JumpStart", 0, 0);
            }
        }
    
        void JumpEnd()
        {
            IsJump = false;
        }

        void OnAnimatorMove()
        {
            if (IsGrounded)
            {
                Vector3 newVelocity = animator.velocity;
                newVelocity.y = RB.velocity.y;
                RB.velocity = new Vector3(PlayerInputs.FixFloat(newVelocity.x), PlayerInputs.FixFloat(newVelocity.y), PlayerInputs.FixFloat(newVelocity.z));
            }
            else
            {
                Vector3 newvelocity = new Vector3(PlayerInputs.FixFloat(input.x * target_speed), PlayerInputs.FixFloat(RB.velocity.y), PlayerInputs.FixFloat(animator.velocity.z));
                RB.velocity = newvelocity;
            }
        
            AdjustAnimationSpeedSafely();
            if (IsJump && RB.velocity.y < jump_force)
            {
                RB.velocity += new Vector3(0, 1f, 0);
            }
        }
// 安全调整动画速度的方法
        private void AdjustAnimationSpeedSafely()
        {
            // 获取当前动画速度的绝对值
            float absCurrentVelocityX = Mathf.Abs(animator.velocity.x);
        
            // 避免除零错误和无效值
            if (absCurrentVelocityX < 0.01f || float.IsNaN(absCurrentVelocityX) || float.IsInfinity(absCurrentVelocityX))
            {
                animator.speed = 1f;
                return;
            }
        
            float targetSpeed = 0f;
            float currentVelocityX = animator.velocity.x;

            if (!Locking)
            {
                // 未锁定状态：只关心输入大小（绝对值）
                float absInputX = Mathf.Abs(input.x);
            
                // 分段计算目标速度（始终正数）
                if (absInputX <= 0.4f) targetSpeed = minSpeed;
                else if (absInputX <= 0.6f) targetSpeed = midSpeed;
                else targetSpeed = maxSpeed;
            
                // 确保目标速度有效
                if (float.IsNaN(targetSpeed) || float.IsInfinity(targetSpeed))
                    targetSpeed = minSpeed;
            
                // 只有当当前速度不足时才调整
                if (absCurrentVelocityX < targetSpeed)
                {
                    // 计算安全的速度比例
                    float speedRatio = Mathf.Clamp(targetSpeed / absCurrentVelocityX, 1f, 3f);
                    animator.speed = Mathf.Clamp(animator.speed * speedRatio, 0.5f, 3f);
                }
                else
                {
                    animator.speed = 1f;
                }
            }
            else
            {
                // 锁定状态：考虑前进/后退
                float adjustedInput = input.x * lastDirection;
            
                if (adjustedInput >= 0)
                {
                    // 正向移动（前进）
                    if (adjustedInput <= 0.4f) targetSpeed = minSpeed;
                    else if (adjustedInput <= 0.6f) targetSpeed = midSpeed;
                    else targetSpeed = maxSpeed;
                }
                // 确保目标速度有效
                if (float.IsNaN(targetSpeed) || float.IsInfinity(targetSpeed))
                    targetSpeed = minSpeed;
            
                // 调整动画速度
                bool shouldAdjust = (adjustedInput >= 0 && currentVelocityX < targetSpeed) ||
                                    (adjustedInput < 0 && currentVelocityX > targetSpeed);
            
                if (shouldAdjust)
                {
                    // 计算安全的速度比例
                    float absTarget = Mathf.Abs(targetSpeed);
                    float speedRatio = Mathf.Clamp(absTarget / absCurrentVelocityX, 1f, 3f);
                    animator.speed = Mathf.Clamp(animator.speed * speedRatio, 1f, 3f);
                }
                else
                {
                    animator.speed = 1f;
                }
            }
        }
        // 翻转物体朝向：Z 轴 scale 变 ±1
        private void SetFacing(int direction)
        {
            lastDirection = direction;
            Vector3 scale = transform.localScale;
            scale.z = Mathf.Abs(scale.z) * direction;
            transform.localScale = scale;
        }

        private void PerformGroundCheck()
        {
            // 重置地面状态
            IsGrounded = false;
            IsOnSlope = false;
        
            // 主射线检测
            Vector3 origin = transform.position + groundCheckOffset;
        
            // 带LayerMask的射线检测
            bool hitGround = Physics.Raycast(origin, Vector3.down, out groundHit, groundCheckDistance, groundLayer);
        
            if (hitGround)
            {
                // 获取地面相对射线发射点的距离
                groundDistance = groundHit.distance;
            
                float slopeAngle = Vector3.Angle(groundHit.normal, Vector3.up);
            
                if (slopeAngle <= maxSlopeAngle)
                {
                    IsGrounded = true;
                    jump_count = 1;
                    lastGroundedTime = Time.time; // 只有在真正接地时才更新时间

                    // 计算斜坡方向
                    if (slopeAngle > 5f)
                    {
                        IsOnSlope = true;
                        SlopeDirection = Vector3.ProjectOnPlane(Vector3.forward, groundHit.normal).normalized;
                    }
                }
            }
            else
            {
                groundDistance = groundCheckDistance; // 如果没有命中，使用最大检测距离
            }
        }
    
        // 调试可视化
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Vector3 origin = transform.position + groundCheckOffset;
        
            // 绘制射线
            Gizmos.DrawLine(origin, origin + Vector3.down * groundCheckDistance);
        
            // 绘制检测终点
            Gizmos.DrawSphere(origin + Vector3.down * groundCheckDistance, 0.1f);
        
            // 如果命中地面，绘制实际命中点
            if (IsGrounded)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(origin + Vector3.down * groundDistance, 0.15f);
            }
        }

        void CheckCommands()
        {
            if (instructionConfig)
            {
                PlayerInputs.Instance.CheckInstructions(instructionConfig.InstructionSet);
            }
        }
        public override int GetLastDirection()
        {
            return lastDirection;
        }

        public override bool GetLocked()
        {
            return Locking;
        }
    }
}