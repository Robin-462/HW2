using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Tooltip("Frame-rate independent movement")]
    public float MoveRate = 5.0f;

    [Tooltip("Player-relative ortho camera")]
    public GameObject playerCamera;

    [Tooltip("Time to fade sounds end of action, in seconds")]
    public float fadeTime = 0.1f;

    [Header("Jump Physics")]
    public float JumpForce = 12f;    // 起跳初速度（正）
    public float Gravity   = -30f;   // 重力（负）
    public float GroundY   = -3.8f;  // 地面高度，按场景调

    // local reference to GameController EventSystem
    GameObject     GC;
    GameController eventSystem;

    Animator       wizardAnim;
    SpriteRenderer wizardSprite;

    enum ActionType { Cast, Hack, Jump, Die }

    // 状态
    bool dead = false;
    float verticalVelocity = 0f;
    bool  onGround = true;

    // Animator 参数名（保持与你的参数一致）
    readonly int pIsRunning   = Animator.StringToHash("isRunning");
    readonly int pIsIdling    = Animator.StringToHash("isIdling");
    readonly int pIsJumping   = Animator.StringToHash("isJumping");
    readonly int pIsFalling   = Animator.StringToHash("isFalling");
    readonly int pIsDying     = Animator.StringToHash("isDying");
    readonly int pIsAttacking = Animator.StringToHash("isAttacking");
    readonly int pAttackType  = Animator.StringToHash("attackType");

    // 统一设置布尔（one-hot 风格，避免互相打架）
    void SetAnimFlags(bool idle=false, bool run=false, bool jump=false, bool fall=false, bool dying=false, bool attacking=false)
    {
        wizardAnim.SetBool(pIsIdling,    idle);
        wizardAnim.SetBool(pIsRunning,   run);
        wizardAnim.SetBool(pIsJumping,   jump);
        wizardAnim.SetBool(pIsFalling,   fall);
        wizardAnim.SetBool(pIsDying,     dying);
        wizardAnim.SetBool(pIsAttacking, attacking);
    }

    // 根据输入设置动画 flags（不改参数类型）
    private void SetAnimState (float x, float y)
    {
        bool cast=false, hack=false, jump=false, die=false;

        if (!dead)
        {
            cast = eventSystem.getInput(GameController.ControlType.Cast);
            hack = eventSystem.getInput(GameController.ControlType.Hack);
            jump = eventSystem.getInput(GameController.ControlType.Jump);
            die  = eventSystem.getInput(GameController.ControlType.Die);
        }

        // 死亡：立刻锁定到 Dying，其他全关
        if (die)
        {
            SetAnimFlags(dying:true);
            dead = true;
            return;
        }

        // 起跳只在落地时响应一次（物理速度在 FixedUpdate 里改）
        if (jump && onGround)
        {
            verticalVelocity = JumpForce;
            onGround = false;
            SetAnimFlags(jump:true);
            return;
        }

        // 左右移动时的朝向
        if (x > 0f) wizardSprite.flipX = false;
        else if (x < 0f) wizardSprite.flipX = true;

        // 地面上的移动/待机
        if (onGround)
        {
            if (Mathf.Abs(x) > 0f) SetAnimFlags(run:true);
            else                   SetAnimFlags(idle:true);
        }

        // 站立时的攻击（可选）
        if (onGround && (cast || hack))
        {
            wizardAnim.SetInteger(pAttackType, cast ? (int)ActionType.Cast : (int)ActionType.Hack);
            // 给一个“正在攻击”的布尔脉冲（1帧），避免卡住
            StartCoroutine(PulseAttackBool());
        }
    }

    IEnumerator PulseAttackBool()
    {
        wizardAnim.SetBool(pIsAttacking, true);
        yield return null; // 下一帧
        wizardAnim.SetBool(pIsAttacking, false);
    }

    void Start()
    {
        GC = GameObject.FindGameObjectWithTag("GameController");
        eventSystem = GC.GetComponent<GameController>();

        wizardAnim   = GetComponent<Animator>();
        wizardSprite = GetComponent<SpriteRenderer>();

        // 初始置 Idle
        SetAnimFlags(idle:true);
        transform.position = new Vector3(transform.position.x, GroundY, transform.position.z);
        onGround = true;
        verticalVelocity = 0f;
    }

    void Update()
    {
        float x = 0.0f;
        float y = 0.0f;

        if (!dead)
        {
            x = eventSystem.getAxis(GameController.AxisType.X);
            y = eventSystem.getAxis(GameController.AxisType.Y);
        }

        SetAnimState(x, y);

        // 水平移动（和原来一样）
        Vector3 move = new Vector3(x, 0.0f, 0.0f) * MoveRate * Time.deltaTime;
        if (move != Vector3.zero)
        {
            float totalMove = eventSystem.playerMove.x + move.x;
            float clampMove = eventSystem.clamp(totalMove);

            eventSystem.scrollerMove.x = totalMove;
            eventSystem.playerMove.x   = totalMove;
        }
    }

    void FixedUpdate()
    {
        if (dead) return;

        // 竖直运动积分
        verticalVelocity += Gravity * Time.fixedDeltaTime;
        transform.position += new Vector3(0f, verticalVelocity * Time.fixedDeltaTime, 0f);

        // 上升→下降切换：在空中且速度向下时置 Falling
        if (!onGround && verticalVelocity < 0f)
            SetAnimFlags(fall:true);

        // 落地
        if (transform.position.y <= GroundY)
        {
            transform.position = new Vector3(transform.position.x, GroundY, transform.position.z);
            verticalVelocity = 0f;
            onGround = true;

            // 回到 Idle（是否回 Run 由 Update 的水平输入再次决定）
            SetAnimFlags(idle:true);
        }
        else
        {
            onGround = false;
        }
    }
}
