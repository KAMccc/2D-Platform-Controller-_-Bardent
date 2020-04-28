using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float movementSpeed = 10f;
    public float jumpForce = 16f;
    public float groundCheckRadius = 2f;
    public float wallCheckDistance = 1f;
    public float wallSildeSpeed = 1f;
    public float movementForceInAir = 1f;
    [Range(0,1)]
    public float airDragMultiplier = 0.95f;
    [Range(0,1)]
    public float variableJumpHeightMultiplier = 0.5f;
    public float wallHopForce = 10f;
    public float wallJumpForce = 10f;
    public float jumpTimerSet = 0.15f;
    public float turnTimerSet = 0.15f;
    public float wallJumpTimerSet = 0.5f;
    public float ledgeClimbTimerSet = 0.1f;
    public float ledgeClimbXOffset1 = 0f;
    public float ledgeClimbYOffset1 = 0f;
    public float ledgeClimbXOffset2 = 0f;
    public float ledgeClimbYOffset2 = 0f;


    public int amountOfJumps = 1;//可跳跃次数

    public Transform groundCheck;
    public Transform wallCheck;
    public Transform ledgeCheck;

    public LayerMask whatIsGround;

    public Vector2 wallHopDirection;//小跳 跳下
    public Vector2 wallJumpDirection;//大跳 跳墙

    

    private float movementInputDirection;
    private float jumpTimer;
    private float turnTimer;
    private float wallJumpTimer;
    private float ledgeClimbTimer;

    private int facingDirection = 1; 
    private int amountOfJumpsLeft;//剩余跳跃次数
    private int lastWallJumpDirection;

    private bool isFacingRight = true;
    private bool isWalking;
    private bool isGrounded;
    private bool canNormalJump;
    private bool canWallJump;
    private bool isTouchingWall;
    private bool isWallSliding;
    private bool isAttemptingToJump;//尝试跳跃标记位
    private bool checkJumpMultiplier;
    private bool canMove;
    private bool canFlip;
    private bool hasWallJumped;
    private bool isTouchingLedge;
    private bool canClimbLedge = false;
    private bool ledgeDetected;

    private Rigidbody2D _rigidbody;
    private Animator _animator;

    private Vector2 ledgePosBot;
    private Vector2 ledgePos1;
    private Vector2 ledgePos2;
    private Vector2 _rigidbodyVelocity;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
    }
    private void Start()
    {
        amountOfJumpsLeft = amountOfJumps;
        //归一化
        wallHopDirection.Normalize();
        wallJumpDirection.Normalize();
    }

    private void Update()
    {
        _rigidbodyVelocity = _rigidbody.velocity;
        CheckInput();
        CheckMovementDirection();
        UpdateAnimation();
        CheckIfCanJump();
        CheckIfWallSliding();
        CheckJump();
        CheckLedgeClimb();
    }

    private void FixedUpdate()
    {
        ApplyMovement();
        CheckSurroundings();
    }

    private void CheckIfWallSliding()
    {
        //接触到墙壁、不在地上、在下降
        if(isTouchingWall && movementInputDirection == facingDirection && _rigidbody.velocity.y < 0 && !canClimbLedge)
        {
            isWallSliding = true;
        }
        else
        {
            isWallSliding = false;
        }
    }

    private void CheckLedgeClimb()
    {
        if(ledgeDetected && !canClimbLedge && movementInputDirection!= 0)
        {
            canClimbLedge = true;

            if (isFacingRight)
            {
                //X：打中墙体的位置向右取整再偏移
                ledgePos1 = new Vector2(Mathf.Floor(ledgePosBot.x + wallCheckDistance) - ledgeClimbXOffset1,
                    Mathf.Floor(ledgePosBot.y) + ledgeClimbYOffset1);
                ledgePos2 = new Vector2(Mathf.Floor(ledgePosBot.x + wallCheckDistance) + ledgeClimbXOffset2,
                    Mathf.Floor(ledgePosBot.y) + ledgeClimbYOffset2);
            }
            else
            {
                ledgePos1 = new Vector2(Mathf.Ceil(ledgePosBot.x - wallCheckDistance) + ledgeClimbXOffset1,
                    Mathf.Floor(ledgePosBot.y) + ledgeClimbYOffset1);
                ledgePos2 = new Vector2(Mathf.Ceil(ledgePosBot.x - wallCheckDistance) - ledgeClimbXOffset2,
                    Mathf.Floor(ledgePosBot.y) + ledgeClimbYOffset2);
            }

            //进入 LedgeClimb 状态： 移除玩家控制权
            canMove = false;
            canFlip = false;

            _animator.SetBool("canClimbLedge",canClimbLedge);
        }

        if (canClimbLedge)
        {
            transform.position = ledgePos1;
        }

        if (ledgeDetected)
            ledgeClimbTimer -= Time.deltaTime;

        if(ledgeClimbTimer <= 0)
        {
            ledgeDetected = false;
        }
    }

    private void CheckSurroundings()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position,groundCheckRadius,whatIsGround);

        isTouchingWall = Physics2D.Raycast(wallCheck.position, transform.right, wallCheckDistance, whatIsGround);
        isTouchingLedge = Physics2D.Raycast(ledgeCheck.position,transform.right,wallCheckDistance,whatIsGround);

        if(isTouchingWall && !isTouchingLedge && !ledgeDetected)
        {
            ledgeDetected = true;
            //存储检测瞬间的点
            ledgePosBot = wallCheck.position;
            ledgeClimbTimer = ledgeClimbTimerSet;
        }
    }


    /// <summary>
    /// 修改角色朝向
    /// </summary>
    private void CheckMovementDirection()
    {
        if(isFacingRight && movementInputDirection < 0)
        {
            CharacherFlip();
        }
        else if(!isFacingRight && movementInputDirection > 0)
        {
            CharacherFlip();
        }

        if (Mathf.Abs(_rigidbody.velocity.x) > 0.005f) isWalking = true;
        else isWalking = false;
    }

    private void CheckInput()
    {
        movementInputDirection = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump"))
        {
            //在地上 || 没贴墙
            if(isGrounded || ( amountOfJumpsLeft > 0 && !isTouchingWall))
            {
                NormalJump();
            }
            //BUG ：amountOfJumpsLeft > 0 && isTouchingWall 也会进入这里，即贴着墙也可以多段跳，与初始设定不符
            else
            {
                //在不可以跳的时候按下了跳跃键，JumpTimer开始记时
                jumpTimer = jumpTimerSet;
                isAttemptingToJump = true;
            }
        }

        //玩家贴墙 && 不在地板上 && 输入与面向相反
        //冻结移动和反转
        if(Input.GetButtonDown("Horizontal") && isTouchingWall)
        {
            if(!isGrounded && movementInputDirection != facingDirection)
            {
                canMove = false;
                canFlip = false;

                turnTimer = turnTimerSet;
            }
        }

        if (turnTimer >= 0)
        {
            turnTimer -= Time.deltaTime;

            if (turnTimer <= 0)
            {
                canMove = true;
                canFlip = true;
            }
        }

        if (checkJumpMultiplier && !Input.GetButton("Jump"))
           JumpMultiplier();
    }

    private  void CheckIfCanJump()
    {
        //由于支持力的存在，且unity没有做自洽
        //所以rigidbody.velocity总是存在一个或+或-的Y轴的瞬时速度
        if (isGrounded && Mathf.Abs(_rigidbody.velocity.y) <= 0.001f)
        {
            amountOfJumpsLeft = amountOfJumps;
            //canVariableJump = true;
        }

        if (isTouchingWall)
        {
            canWallJump = true;
        }
        
        if (amountOfJumpsLeft <= 0)
            canNormalJump = false;
        else
            canNormalJump = true;
    }


    private void CheckJump()
    {
        //开始尝试跳跃，JumpTimer计时器开始
        if(isAttemptingToJump)
        {
            jumpTimer -= Time.deltaTime;
        }

        //尝试跳跃计时器没走完的时候
        if( jumpTimer > 0)
        {
            //WallJump
            // movementInputDirection != facingDirection 输入的跳跃的方向 和 角色当前朝向相反（ 这才是跳墙 ）
            if (!isGrounded && isTouchingWall && movementInputDirection != 0 && movementInputDirection != facingDirection)
            {
                WallJump();
            }
            //计时器没走完就着地
            else if(isGrounded)
            {
                NormalJump();
            }
        }

        if(wallJumpTimer > 0)
        {
            //要跳跃的方向和上一次墙壁的方向相反
            if(hasWallJumped && movementInputDirection == -lastWallJumpDirection)
            {
                //不让他跳
                _rigidbody.velocity = new Vector2(_rigidbody.velocity.x,0f);
                hasWallJumped = false;
            }
            else if(wallJumpTimer <= 0)
            {
                hasWallJumped = false;
            }
            else
            {
                wallJumpTimer -= Time.deltaTime;
            }
        }
    }

    private void NormalJump()
    {
        if (canNormalJump)
        {
            _rigidbody.velocity = new Vector2(_rigidbody.velocity.x, jumpForce);

            //跳跃之后处理
            amountOfJumpsLeft--;
            //跳跃计时器归零
            jumpTimer = 0;
            //尝试跳跃关闭（ 用过了 ）
            isAttemptingToJump = false;
            checkJumpMultiplier = true;
        }
    }

    private void WallJump()
    {
        if (canWallJump)//Wall Jump
        {
            //弹跳前移除Vy,使得所有弹跳的动力一致
            //不然会导致弹跳力不一致
            _rigidbody.velocity = new Vector2(_rigidbody.velocity.x, 0f);
            isWallSliding = false;
            //贴墙后可跳次数恢复
            amountOfJumpsLeft = amountOfJumps;
            amountOfJumpsLeft--;
            Vector2 forceToAdd = new Vector2(wallJumpForce * wallJumpDirection.x * movementInputDirection, wallJumpForce * wallJumpDirection.y);
            _rigidbody.AddForce(forceToAdd, ForceMode2D.Impulse);

            //跳跃计时器归零
            jumpTimer = 0;
            //尝试跳跃关闭（ 用过了 ）
            isAttemptingToJump = false;
            checkJumpMultiplier = true;

            turnTimer = 0;
            canMove = true;
            canFlip = true;

            hasWallJumped = true;
            wallJumpTimer = wallJumpTimerSet;
            lastWallJumpDirection = -facingDirection;//玩家贴着墙壁面朝右，所以上一个墙是在左边
        }
    }


    private void JumpMultiplier()
    {
            _rigidbody.velocity = new Vector2(_rigidbody.velocity.x, _rigidbody.velocity.y * variableJumpHeightMultiplier);
        checkJumpMultiplier = false;
    }

    private void ApplyMovement()
    {
        //地面移动
        if(isGrounded && canMove)
            _rigidbody.velocity = new Vector2(movementInputDirection * movementSpeed,_rigidbody.velocity.y);
        //空中移动
        else if(!isGrounded && !isWallSliding && movementInputDirection != 0)
        {
            Vector2 forceToAdd = new Vector2(movementForceInAir * movementInputDirection, 0);
            _rigidbody.AddForce(forceToAdd);

            if(Mathf.Abs(_rigidbody.velocity.x) > movementSpeed)
            {
                _rigidbody.velocity = new Vector2(movementSpeed * movementInputDirection,_rigidbody.velocity.y);
            }
        }
        //空中不移动，自然减速
        else if(!isGrounded && !isWallSliding && movementInputDirection == 0)
        {
            _rigidbody.velocity = new Vector2(_rigidbody.velocity.x * airDragMultiplier,_rigidbody.velocity.y);
        }



        if (isWallSliding)
        {
            if(_rigidbody.velocity.y < -wallSildeSpeed)
            {
                _rigidbody.velocity = new Vector2(_rigidbody.velocity.x, -wallSildeSpeed);
            }
        }
    }

    private void CharacherFlip()
    {
        if (!isWallSliding && canFlip)
        {
            facingDirection *= -1;
            isFacingRight = !isFacingRight;
            transform.Rotate(0f,180f,0f);
        }
    }

    private void UpdateAnimation()
    {
        _animator.SetBool("isWalking", isWalking);
        _animator.SetBool("isGrounded", isGrounded);
        _animator.SetFloat("yVelocity",_rigidbody.velocity.y);
        _animator.SetBool("isWallSliding", isWallSliding);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(groundCheck.position,groundCheckRadius);
        Gizmos.DrawLine(wallCheck.position, new Vector3(wallCheck.position.x + wallCheckDistance * facingDirection,
            wallCheck.position.y, wallCheck.position.z));
        Gizmos.DrawLine(ledgeCheck.position, new Vector3(ledgeCheck.position.x + wallCheckDistance * facingDirection,
            ledgeCheck.position.y, ledgeCheck.position.z));
    }

    public void FinishLedgeClimb()
    {
        canClimbLedge = false;
        transform.position = ledgePos2;

        //LedgeClimb 播放完毕，交还玩家控制权
        canMove = true;
        canFlip = true;

        ledgeDetected = false;
        _animator.SetBool("canClimbLedge", canClimbLedge);
    }
}