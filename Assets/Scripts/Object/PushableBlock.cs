using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PushableBlock : RaycastCollider
{
    // 평탄한 땅에서만 미는 것을 가정함
    [HideInInspector] public Vector3 velocity; // 박스의 속력
    public CollisionInfo collisions;
    public LayerMask WhatIsPlayer;

    float playerCrouchingSpeed;
    float velocityXSmoothing;
    bool playerOnLeft = false;
    bool playerOnRight = false;

    [HideInInspector] float accelerationTimeGrounded = .3f; // 땅에서의 좌우 가속
    [HideInInspector] float accelerationTimeAirborne = .5f; // 공중에서의 좌우 가속

    public override void Start()
    {
        base.Start();
        playerCrouchingSpeed = player.GetComponent<PlayerMovement>().walkingSpeed * player.GetComponent<PlayerMovement>().crouchSpeed;

        accelerationTimeGrounded = player.GetComponent<PlayerMovement>().accelerationTimeGrounded;
        accelerationTimeAirborne = player.GetComponent<PlayerMovement>().accelerationTimeAirborne;
    }

    void Update()
    {
        float inputXDir = Input.GetAxisRaw("Horizontal");

        if (velocity.y < -fallingSpeedMax) // 일정 속도 이상 낙하하지 않음
            velocity.y = -fallingSpeedMax;

        if (collisions.above || collisions.below) // raycast에서의 중력 축적 방지
            velocity.y = 0;

        if (playerOnLeft || playerOnRight)
        {
            if (player.GetComponent<PlayerMovement>().pushing)
                velocity.x = player.GetComponent<PlayerMovement>().velocity.x;
            else 
                velocity.x = 0;
        }
        else
        {
            velocity.x = 0;
        }

        velocity.y += gravity * Time.deltaTime;
        Move(velocity * Time.deltaTime);
    }

    void Move(Vector3 velocity)
    {
        UpdateRaycastOrigins();
        playerOnLeft = false;
        playerOnRight = false;
        collisions.Reset();

        HorizontalCollisionsPlayer(ref velocity);
        if (velocity.x != 0)
        {
            collisions.faceDir = Mathf.FloorToInt(Mathf.Sign(velocity.x));
            HorizontalCollisionsMoving(ref velocity);
        }
        if (velocity.y != 0)
            VerticalCollisions(ref velocity);

        transform.Translate(velocity);
    }

    void HorizontalCollisionsMoving(ref Vector3 velocity)
    {
        float directionX = collisions.faceDir;
        float rayLength = Mathf.Abs(velocity.x) + skinWidth;

        if (Mathf.Abs(velocity.x) < skinWidth)
            rayLength = 2 * skinWidth;

        for (int i = 0; i < horizontalRayCount; i++)
        {
            Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
            rayOrigin += Vector2.up * (horizontalRaySpacing * i);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, WhatIsGround);

            Debug.DrawRay(rayOrigin, Vector2.right * rayLength, Color.blue);
            Debug.DrawRay(rayOrigin, Vector2.right * rayLength * -1, Color.blue);

            if (hit)
            {
                velocity.x = Mathf.Min(Mathf.Abs(velocity.x), (hit.distance - skinWidth)) * directionX;
                rayLength = Mathf.Min(Mathf.Abs(velocity.x) + skinWidth, hit.distance);

                collisions.left = directionX == -1;
                collisions.right = directionX == 1;
            }
        }
    }

    void HorizontalCollisionsPlayer(ref Vector3 velocity)
    {
        float directionX = collisions.faceDir;
        float rayLength = 5 * skinWidth;

        for (int i = 0; i < horizontalRayCount; i++)
        {
            Vector2 rayOriginLeft = raycastOrigins.bottomLeft;
            Vector2 rayOriginRight = raycastOrigins.bottomRight;
            rayOriginLeft += Vector2.up * (horizontalRaySpacing * i);
            rayOriginRight += Vector2.up * (horizontalRaySpacing * i);
            RaycastHit2D hitLeft = Physics2D.Raycast(rayOriginLeft, Vector2.right * -1, rayLength, WhatIsPlayer);
            RaycastHit2D hitRight = Physics2D.Raycast(rayOriginRight, Vector2.right, rayLength, WhatIsPlayer);

            Debug.DrawRay(rayOriginRight, Vector2.right * rayLength, Color.yellow);
            Debug.DrawRay(rayOriginLeft, Vector2.right * rayLength * -1, Color.yellow);

            if (hitLeft || hitRight)
            {
                playerOnLeft = hitLeft;
                playerOnRight = hitRight;
                if (playerOnLeft)
                    collisions.horizontalTag = hitLeft.collider.tag;
                else
                    collisions.horizontalTag = hitRight.collider.tag;
            }
        }
    }

    void VerticalCollisions(ref Vector3 velocity)
    {
        float directionY = Mathf.Sign(velocity.y);
        float rayLength = Mathf.Abs(velocity.y) + skinWidth;

        for (int i = 0; i < verticalRayCount; i++)
        {
            Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
            rayOrigin += Vector2.right * (verticalRaySpacing * i + velocity.x);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, WhatIsGround);

            Debug.DrawRay(rayOrigin, Vector2.up * directionY * rayLength, Color.yellow);

            if (hit)
            {
                velocity.y = Mathf.Min(Mathf.Abs(velocity.y), (hit.distance - skinWidth)) * directionY;
                rayLength = Mathf.Min(Mathf.Abs(velocity.y) + skinWidth, hit.distance);

                collisions.below = directionY == -1;
                collisions.above = directionY == 1;
            }
        }
    }

    public struct CollisionInfo
    {
        public bool above, below;
        public bool left, right;
        public int faceDir;

        public string horizontalTag;

        public void Reset()
        {
            above = below = false;
            left = right = false;
        }
    }
}
