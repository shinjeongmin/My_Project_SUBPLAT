﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Controller2D))]
public class PlayerMovement : MonoBehaviour
{
    Controller2D controller; // 플레이어의 움직임에 관여하는 또다른 스크립트
    public GameManager gm;
    public Animator animator; // 플레이어의 animator

    public float maxJumpHeight = 4f; // 점프 높이
    [SerializeField] float minJumpHeight = 1f; // 최소 점프 높이
    public float timeToJumpApex = .4f; // 점프했을 때 최고점에 도달하는데 걸리는 시간
    float maxJumpVelocity; // 가장 높게 점프할 때 받는 힘
    float minJumpVelocity; // 가장 낮게 점프할 때 받는 힘
    float doubleJumpVelocity; // 이단 점프할 때 받는 힘
    float gravity; // 플레이어에 작용하는 중력

    int jumpcount; // 점프 횟수
    bool facingRight = true; // 플레이어가 향하고 있는 방향
    bool crouch = false; // 플레이어가 앉아 있는가?
    bool slide = false; // 플레이어가 슬라이딩하는 중인가?
    bool wallJumping = false; // 플레이어가 벽점프 가능한 상태인가?
    public bool pushing = false; // 플레이어가 뭔가를 밀고 있는 상태인가?
    float moveSpeed; // 플레이어의 이동 속도
    public float walkingSpeed = 3f; // 플레이어가 걷는 속도
    public float runningSpeed = 8f; // 플레이어가 달리는 속도
    public float slidingSpeed = 50f; // 플레이어가 경사로에서 슬라이딩하는 속도
    public float fallingSpeedMax = 20f; // 플레이어의 최대 낙하 속도
    [Range(0, 1)] public float crouchSpeed = .36f; // 앉았을 때 이동 속도 비율

    bool wallSliding; // 플레이어가 벽에 붙어있는가?
    int wallDirX; // 플레이어가 붙어있는 벽의 방향
    [SerializeField] float wallSlideSpeedMax = 3f; // 벽에서 미끄러질 때 갖는 최대 낙하 속도
    [SerializeField] float wallStickTime = .5f; // 플레이어가 벽에 무조건 붙어있는 최대 시간
    float timeToWallUnstick; // 플레이어가 벽에 붙은 시간

    [SerializeField] Vector2 wallJumpWeak; // 가까이 벽점프할 때의 힘
    [SerializeField] Vector2 wallJumpStrong; // 멀리 벽점프할 때의 힘

    /*[HideInInspector]*/ public Vector3 velocity; // 플레이어의 속력
    public float accelerationTimeGrounded = .3f; // 땅에서의 좌우 가속
    public float accelerationTimeAirborne = .5f; // 공중에서의 좌우 가속
    public float accelerationTimeSlope = 5f; // 경사로에서 좌우 가속
    float velocityXSmoothing; // smoothdamp에 쓰이는 변수

    void Start()
    {
        controller = GetComponent<Controller2D>();

        gravity = -2 * maxJumpHeight / Mathf.Pow(timeToJumpApex, 2f); // s = 1/2 * a * t^2
        maxJumpVelocity = -gravity * timeToJumpApex; // v = at
        minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * minJumpHeight);
        doubleJumpVelocity = maxJumpVelocity / (float)1.5;
    }

    void Update()
    {
        if (velocity.y < -fallingSpeedMax) // 일정 속도 이상 낙하하지 않음
            velocity.y = -fallingSpeedMax;

        if (controller.collisions.below) // 애니메이터 옵션은 나중에 추가할 것
        {
            jumpcount = 0;
            if ((Input.GetButton("Run") && !crouch) || slide)
            {
                moveSpeed = runningSpeed;
            }
            else
            {
                moveSpeed = walkingSpeed;
            }
        }
        else
        {
            if (jumpcount == 0)
                jumpcount = 1;
        }

        wallJumping = (controller.collisions.horizontalTag == "Climbing Wall") ? true : false;

        float inputXDir = Input.GetAxisRaw("Horizontal"); // 입력한 X축 방향
        
        wallDirX = (controller.collisions.left) ? -1 : 1;
        wallSliding = false;

        if (controller.collisions.horizontalTag == "Pushable" && controller.collisions.verticalTag != "Pushable"
            && (controller.collisions.left || controller.collisions.right)) {
            if (Input.GetButtonDown("Use"))
            {
                if (controller.collisions.left && facingRight)
                    Flip();
                else if (controller.collisions.right && !facingRight)
                    Flip();
                pushing = true;
            }
                
        }

        if (pushing)
        {
            if (Input.GetButtonUp("Use"))
            {
                pushing = false;
            }   
        }
            

        if (slide) // 슬라이드중이면 좌우 입력이 슬라이드의 경사에 따라서 결정됨
        {
            // 플레이어의 X방향 속력
            float slopeXDir = controller.collisions.slopeDir;
            float targetVelocityX = slopeXDir * Mathf.Sin(controller.collisions.slopeAngleCenter * Mathf.Deg2Rad) * slidingSpeed;
            // 약간의 가속이 붙도록 smoothdamp를 이용
            velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocityXSmoothing, accelerationTimeSlope);
            // 플레이어의 속도로 애니메이터 설정
            animator.SetFloat("PlayerSpeed", Mathf.Abs(targetVelocityX));
            if ((slide && !controller.collisions.below && Mathf.Abs(velocity.x) <= Mathf.Abs(runningSpeed)) || (Mathf.Sign(inputXDir) != Mathf.Sign(velocity.x)))
                slide = false;
        }
        else
        {
            // 플레이어의 X방향 속력
            float targetVelocityX = (crouch || pushing) ? inputXDir * moveSpeed * crouchSpeed : inputXDir * moveSpeed;
            // 약간의 가속이 붙도록 smoothdamp를 이용
            velocity.x = Mathf.SmoothDamp(velocity.x, controlLimit(ref targetVelocityX), ref velocityXSmoothing,
                (controller.collisions.below) ? accelerationTimeGrounded : accelerationTimeAirborne);
            // 플레이어의 속도로 애니메이터 설정
            animator.SetFloat("PlayerSpeed", Mathf.Abs(targetVelocityX));
        }
        
        // 공중에서 벽에 붙어있고, 떨어지는 때
        if ((controller.collisions.left || controller.collisions.right) && !controller.collisions.below && wallJumping)
        {
            wallSliding = true;
            if (velocity.y < -wallSlideSpeedMax) // 일정 속도 이상 낙하하지 않음
                velocity.y = -wallSlideSpeedMax;

            // 일정 시간동안 벽에 무조건 붙어있도록 함
            if (timeToWallUnstick > 0)
            {
                velocity.x = 0;
                velocityXSmoothing = 0;

                if (inputXDir != wallDirX || inputXDir != 0)
                    timeToWallUnstick -= Time.deltaTime;
                else
                    timeToWallUnstick = wallStickTime;
            }
            else timeToWallUnstick = wallStickTime;
        }

        if (controller.collisions.above || controller.collisions.below) // raycast에서의 중력 축적 방지
            velocity.y = 0;
 
        if ((controller.collisions.below || jumpcount < 2) && !pushing && !wallSliding) // 땅에 있을 때
        {
            if (jumpcount == 0)
                animator.SetBool("IsJumping", false);
            if (Input.GetButtonDown("Jump") && !crouch) // 점프키를 눌렀을 때
            {
                if (jumpcount == 0)
                    velocity.y = maxJumpVelocity;
                else
                    velocity.y = doubleJumpVelocity;

                animator.SetBool("IsJumping", true);
                jumpcount += 1;
            }
        }
        else if (Input.GetButtonDown("Jump") && wallSliding) // 벽점프하는 경우
        {
            slide = false;
            animator.SetBool("IsJumping", true);
            jumpcount = 2;
            if (wallDirX == inputXDir) // 점프하는 방향과 입력한 방향이 반대
            {
                velocity.x = -wallDirX * wallJumpWeak.x;
                velocity.y = wallJumpWeak.y;
                animator.Play("Player_Jump", -1, 0f);
            }
            else // 입력한 방향이 없거나 반대
            {
                inputXDir = -wallDirX;
                velocity.x = -wallDirX * wallJumpStrong.x;
                velocity.y = wallJumpStrong.y;
                animator.Play("Player_Jump", -1, 0f);
            }
        }

        // 점프 키를 땠을 때 (점프키를 누른 시간에 따라 점프 높이를 다르게 함)
        if (Input.GetButtonUp("Jump") && !crouch && !slide && !pushing && jumpcount < 2)
        {
            if (velocity.y > minJumpVelocity)
            {
                velocity.y = minJumpVelocity;
                animator.SetBool("IsJumping", true);
            }
        }

        // 캐릭터의 움직임에 따라 스프라이트의 방향 변경
        if (inputXDir > 0 && !facingRight && !pushing)
            Flip();
        else if (inputXDir < 0 && facingRight && !pushing)
            Flip();

        velocity.y += gravity * Time.deltaTime; // 중력 작용
        controller.Move(velocity * Time.deltaTime, crouch); // 이동중 충돌 확인

        // 앉기
        if (controller.collisions.below && !pushing && !GetComponent<PlayerInteraction>().haveKey) // 땅에 있을 때
        {
            if (!controller.collisions.onSlope)
            {
                if (!Input.GetButton("Crouch") && slide)
                    slide = false;

                if (Input.GetButton("Crouch") && !slide)
                {
                    crouch = true;
                    animator.SetBool("IsCrouch", true);
                }
                else if (!controller.collisions.crouchLock && !Input.GetButton("Crouch"))
                {
                    crouch = false;
                    animator.SetBool("IsCrouch", false);
                }
            }
            else
            {
                crouch = false;
                animator.SetBool("IsCrouch", false);
                if (Input.GetButton("Crouch") && controller.collisions.onSlope)
                    slide = true;
                else
                    slide = false;
            }     
        }
        else
        {
            if (crouch)
            {
                crouch = false;
                animator.SetBool("IsCrouch", false);
            }
        }
    }

    // 공중에서 좌우 이동 속도 제한
    float controlLimit(ref float velocityX)
    {
        if (!controller.collisions.below) // 만약 공중에 있다면
        {
            float airControlAccelerationLimit = 0.3f; // 공중에서의 X방향 속력의 크기 한계
            float targetDeltaVelocityX = velocityX - velocity.x; // X방향 속력의 변화량
            float targetXChange = Mathf.Clamp(targetDeltaVelocityX, -airControlAccelerationLimit, airControlAccelerationLimit);
            velocityX += targetXChange;
        }

        return velocityX;
    }

    void Flip() // 방향 전환
    {
        facingRight = !facingRight;
        Vector3 theScale = transform.localScale;
        theScale.x *= -1;
        transform.localScale = theScale;
    }

    void OnTriggerEnter2D(Collider2D collide)
    {
        if (collide.tag == "Shield" || collide.tag == "Spike")
        {
            gm.GameOver();
        }
        else if (collide.tag == "Goal")
        {
            gm.NextStage();
        }
    }
}

