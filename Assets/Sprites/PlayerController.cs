using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float movementSpeed = 10f;
    public float jumpForce = 16f;
    public float groundCheckRadius = 2f;
    public Transform groundCheck;
    public Transform wallCheck;
    public LayerMask whatIsGround;
    public int amountOfJumps = 1;//可跳跃次数
    public float wallCheckDistance = 1f;
    public float wallSildeSpeed = 1f;
    public float movementForceInAir = 1f;
    [Range(0,1)]
    public float airDragMultiplier = 0.95f;
    [Range(0,1)]
    public float variableJumpHeightMultiplier = 0.5f;


    private float movementInputDirection;
    private bool isFacingRight = true;
    private bool isWalking;

    private bool isGrounded;
    private bool canJump;
    private bool canVariableJump;//修复在掉下过程中可以疯狂按Jump来停滞空中的问题
    private int amountOfJumpsLeft;//剩余跳跃次数

    private bool isTouchingWall;
    private bool isWallSliding;

    private Rigidbody2D _rigidbody;
    private Animator _animator;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
    }
    private void Start()
    {
        amountOfJumpsLeft = amountOfJumps;
    }

    private void Update()
    {
        CheckInput();
        CheckMovementDirection();
        UpdateAnimation();
        CheckIfCanJump();
        CheckIfWallSliding();
    }

    private void FixedUpdate()
    {
        ApplyMovement();
        CheckSurroundings();
    }

    private void CheckIfWallSliding()
    {
        //接触到墙壁、不在地上、在下降
        if(isTouchingWall && !isGrounded && _rigidbody.velocity.y < 0)
        {
            isWallSliding = true;
        }
        else
        {
            isWallSliding = false;
        }
    }

    private void CheckSurroundings()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position,groundCheckRadius,whatIsGround);

        isTouchingWall = Physics2D.Raycast(wallCheck.position, transform.right, wallCheckDistance, whatIsGround);
    }

    private void UpdateAnimation()
    {
        _animator.SetBool("isWalking", isWalking);
        _animator.SetBool("isGrounded", isGrounded);
        _animator.SetFloat("yVelocity",_rigidbody.velocity.y);
        _animator.SetBool("isWallSliding", isWallSliding);
    }

    /// <summary>
    /// 修改角色朝向
    /// </summary>
    private void CheckMovementDirection()
    {
        if(isFacingRight && movementInputDirection < 0)
        {
            CharacherFilp();
        }
        else if(!isFacingRight && movementInputDirection > 0)
        {
            CharacherFilp();
        }

        if (Mathf.Abs(_rigidbody.velocity.x) > 0.005f) isWalking = true;
        else isWalking = false;
    }

    private void CheckInput()
    {
        movementInputDirection = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump"))
        {
            Jump();
        }
        if (Input.GetButtonUp("Jump"))
           JumpVariable();
    }

    private  void CheckIfCanJump()
    {
        if (isGrounded && _rigidbody.velocity.y <= 0)
        {
            amountOfJumpsLeft = amountOfJumps;
            canVariableJump = true;
        }
        
        if (amountOfJumpsLeft <= 0)
            canJump = false;
        else
            canJump = true;
    }


    private void Jump()
    {
        if(canJump)
        {
            _rigidbody.velocity = new Vector2(_rigidbody.velocity.x, jumpForce);
            amountOfJumpsLeft--;
        }
    }

    private void JumpVariable()
    {
        if (canVariableJump)
        {
            _rigidbody.velocity = new Vector2(_rigidbody.velocity.x, _rigidbody.velocity.y * variableJumpHeightMultiplier);
        }
        canVariableJump = false;
    }

    private void ApplyMovement()
    {
        if(isGrounded)
            _rigidbody.velocity = new Vector2(movementInputDirection * movementSpeed,_rigidbody.velocity.y);
        else if(!isGrounded && !isWallSliding && movementInputDirection != 0)
        {
            Vector2 forceToAdd = new Vector2(movementForceInAir * movementInputDirection, 0);
            _rigidbody.AddForce(forceToAdd);

            if(Mathf.Abs(_rigidbody.velocity.x) > movementSpeed)
            {
                _rigidbody.velocity = new Vector2(movementSpeed * movementInputDirection,_rigidbody.velocity.y);
            }
        }
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

    private void CharacherFilp()
    {
        if (!isWallSliding)
        {
            isFacingRight = !isFacingRight;
            transform.Rotate(0f,180f,0f);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(groundCheck.position,groundCheckRadius);
        Gizmos.DrawLine(wallCheck.position, new Vector3(isFacingRight? wallCheck.position.x + wallCheckDistance : wallCheck.position.x - wallCheckDistance, 
            wallCheck.position.y, wallCheck.position.z));
    }
}
