using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Animator))]
public class BasicPlayerController : MonoBehaviour
{
    [Header("移动参数")]
    public float moveSpeed = 5f;    // 最大速度
    public float accelTime = 0.2f;  // 起步加速所用时间
    public float decelTime = 0.05f; // 停止减速所用时间

    private float accelRate;        // 加速度 = moveSpeed / accelTime
    private float decelRate;        // 减速度 = moveSpeed / decelTime
    private float currentVelocity;  // 当前速度

    private int lastDirection = 1;  // 1=右，-1=左

    private Rigidbody RB;
    private Animator animator;

    private void Awake()
    {
        RB = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        accelRate = moveSpeed / accelTime;
        decelRate = moveSpeed / decelTime;
    }

    private void FixedUpdate()
    {
        // 1. 读取输入
        Vector2 input = PlayerInputs.Instance.moveInput;

        // 2. 计算水平输入方向并翻转朝向
        float horizontalInput = 0f;
        if (input.x >  0.01f) { horizontalInput =  1f; SetFacing(1); }
        else if (input.x < -0.01f) { horizontalInput = -1f; SetFacing(-1); }

        // 3. 依据斜向判定是否减半速度
        bool isDiagonal = PlayerInputs.isUpLeft || PlayerInputs.isUpRight
                       || PlayerInputs.isDownLeft || PlayerInputs.isDownRight;
        float speedFactor = (horizontalInput != 0f && isDiagonal) ? 0.5f : 1f;
        float targetVelocity = horizontalInput * moveSpeed * speedFactor;

        // 4. 加速或减速
        if (horizontalInput != 0f)
        {
            currentVelocity = Mathf.MoveTowards(
                currentVelocity,
                targetVelocity,
                accelRate * Time.fixedDeltaTime
            );
        }
        else
        {
            currentVelocity = Mathf.MoveTowards(
                currentVelocity,
                0f,
                decelRate * Time.fixedDeltaTime
            );
        }

        // 5. 用物理刚体移动并触发碰撞
        if (Mathf.Abs(currentVelocity) > 0f)
        {
            Vector3 delta = Vector3.right * currentVelocity * Time.fixedDeltaTime;
            RB.MovePosition(RB.position + delta);
        }

        // 6. 动画 Run 开关
        animator.SetBool("Walk",isDiagonal);
        bool isRunning = Mathf.Abs(currentVelocity) > 4f;
        animator.SetBool("Run", isRunning && ! isDiagonal);
    }

    // 翻转物体朝向：Z 轴 scale 变 ±1
    private void SetFacing(int direction)
    {
        lastDirection = direction;
        Vector3 scale = transform.localScale;
        scale.z = Mathf.Abs(scale.z) * direction;
        transform.localScale = scale;
    }
}