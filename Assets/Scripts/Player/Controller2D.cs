using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Controller2D : MonoBehaviour // 플레이어는 별도의 Raycast를 사용함
{
    [SerializeField] const float skinWidth = .015f;
    [SerializeField] int horizontalRayCount = 4;
    [SerializeField] int verticalRayCount = 4;
    float horizontalRaySpacing;
    float verticalRaySpacing;
    bool crouchingOld = false;

    [SerializeField] float maxClimbAngle = 70f; // 최대로 오를 수 있는 경사로의 각도
    [SerializeField] float maxDescendAngle = 70f; // 최대로 내려갈 수 있는 경사로의 각도
    [SerializeField] float minSlopeAngle = 10f; // 경사로로 인식하는 최소 각도

    [SerializeField] LayerMask WhatIsGround; // 땅으로 인식하는 Layer
    [SerializeField] Transform CeilingCheck; // 플레이어가 천장에 있는지 확인하는 기준 위치
    const float CeilingRadius = .2f;

    [SerializeField] BoxCollider2D mainCollider;
    [SerializeField] BoxCollider2D crouchDisableCollider;

    RaycastOrigins raycastOrigins;
    public CollisionInfo collisions;

    void Start()
    {
        CalculateRaySpacing(false);
        collisions.faceDir = 1;
    }

    public void Move(Vector3 velocity, bool crouch) // 플레이어가 움직이는 중에 충돌을 감지한다.
    {
        if (Physics2D.OverlapCircle(CeilingCheck.position, CeilingRadius, WhatIsGround) && collisions.below)
        {
            crouch = true;
            collisions.crouchLock = true;
        }
        else collisions.crouchLock = false;

        if (crouchingOld != crouch)
        {
            CalculateRaySpacing(crouch);
            crouchingOld = crouch;
        }

        UpdateRaycastOrigins(crouch);
        collisions.Reset();
        collisions.velocityOld = velocity;
        DetectSlope();

        if (velocity.y < 0)
            DescendScope(ref velocity);

        if (velocity.x != 0)
            collisions.faceDir = Mathf.FloorToInt(Mathf.Sign(velocity.x));
        HorizontalCollisions(ref velocity);

        if (velocity.y != 0)
            VerticalCollisions(ref velocity);

        transform.Translate(velocity);
    }

    void HorizontalCollisions(ref Vector3 velocity)
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

            Debug.DrawRay(rayOrigin, Vector2.right * directionX * rayLength, Color.red);

            if (hit)
            {
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                collisions.horizontalTag = hit.collider.tag;

                if (i == 0 && slopeAngle <= maxClimbAngle)
                {
                    if (collisions.descendingSlope)
                    {
                        collisions.descendingSlope = false;
                        velocity = collisions.velocityOld;
                    }

                    float distanceToSlopeStart = 0;
                    if (slopeAngle != collisions.slopeAngleOld)
                    {
                        distanceToSlopeStart = hit.distance - skinWidth;
                        velocity.x -= distanceToSlopeStart * directionX;
                    }
                    ClimbSlope(ref velocity, slopeAngle);
                    velocity.x += distanceToSlopeStart * directionX;
                }

                if (!collisions.climbingSlope || slopeAngle > maxClimbAngle)
                {
                    velocity.x = Mathf.Min(Mathf.Abs(velocity.x), (hit.distance - skinWidth)) * directionX;
                    rayLength = Mathf.Min(Mathf.Abs(velocity.x) + skinWidth, hit.distance);

                    if (collisions.climbingSlope)
                    {
                        velocity.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(velocity.x);
                    }

                    collisions.left = directionX == -1;
                    collisions.right = directionX == 1;
                }
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

            Debug.DrawRay(rayOrigin, Vector2.up * directionY * rayLength, Color.red);

            if (hit)
            {
                velocity.y = Mathf.Min(Mathf.Abs(velocity.y), (hit.distance - skinWidth)) * directionY;
                rayLength = Mathf.Min(Mathf.Abs(velocity.y) + skinWidth, hit.distance);
                collisions.verticalTag = hit.collider.tag;

                if (collisions.climbingSlope)
                {
                    velocity.x = velocity.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(velocity.x);
                }

                collisions.below = directionY == -1;
                collisions.above = directionY == 1;
            }
        }

        if (collisions.climbingSlope)
        {
            float directionX = collisions.faceDir;
            rayLength = Mathf.Abs(velocity.x + skinWidth);
            Vector2 rayOrigin = ((directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight)
                + Vector2.up * velocity.y;

            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, WhatIsGround);

            if (hit)
            {
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                if (slopeAngle != collisions.slopeAngle)
                {
                    velocity.x = (hit.distance - skinWidth) * directionX;
                    collisions.slopeAngle = slopeAngle;
                }
            }
        }
    }

    void ClimbSlope(ref Vector3 velocity, float slopeAngle)
    {
        float moveDistance = Mathf.Abs(velocity.x);
        float climbVelocityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;

        if (velocity.y <= climbVelocityY)
        {
            velocity.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(velocity.x);
            velocity.y = climbVelocityY;

            collisions.below = true;
            collisions.climbingSlope = true;
            collisions.slopeAngle = slopeAngle;
        }
    }

    void DescendScope(ref Vector3 velocity)
    {
        float directionX = collisions.faceDir;
        Vector2 rayOrigin = (directionX == 1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, -Vector2.up, Mathf.Infinity, WhatIsGround);

        if (hit)
        {
            float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            if (slopeAngle != 0 && slopeAngle <= maxDescendAngle)
            {
                if (Mathf.Sign(hit.normal.x) == directionX)
                {
                    if (hit.distance - skinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(velocity.x))
                    {
                        float moveDistance = Mathf.Abs(velocity.x);
                        float descendVelocityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
                        velocity.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(velocity.x);
                        velocity.y -= descendVelocityY;

                        collisions.slopeAngle = slopeAngle;
                        collisions.descendingSlope = true;
                        collisions.below = true;
                    }
                }
            }
        }
    }

    void DetectSlope()
    {
        float directionX = collisions.faceDir;
        Vector2 rayOrigin = transform.position;
        RaycastHit2D centerHit = Physics2D.Raycast(rayOrigin, -Vector2.up, Mathf.Infinity, WhatIsGround);
        RaycastHit2D rightHit = Physics2D.Raycast(raycastOrigins.bottomRight, -Vector2.up, Mathf.Infinity, WhatIsGround);
        RaycastHit2D leftHit = Physics2D.Raycast(raycastOrigins.bottomLeft, -Vector2.up, Mathf.Infinity, WhatIsGround);

        if (centerHit)
        {
            collisions.slopeDir = (leftHit.distance < rightHit.distance) ? 1 : -1;
            collisions.slopeAngleCenter = Vector2.Angle(centerHit.normal, Vector2.up);
            collisions.onSlope = (collisions.slopeAngleCenter > minSlopeAngle && centerHit.collider.tag == "Slope") ? true : false;
        }
    }

    void UpdateRaycastOrigins(bool crouch)
    {
        Bounds lowerBounds = mainCollider.bounds;
        Bounds upperBounds = crouchDisableCollider.bounds;
        lowerBounds.Expand(skinWidth * -2);
        upperBounds.Expand(skinWidth * -2);

        raycastOrigins.bottomLeft = new Vector2(lowerBounds.min.x, lowerBounds.min.y);
        raycastOrigins.bottomRight = new Vector2(lowerBounds.max.x, lowerBounds.min.y);

        if (crouch)
        {
            raycastOrigins.topLeft = new Vector2(lowerBounds.min.x, lowerBounds.max.y);
            raycastOrigins.topRight = new Vector2(lowerBounds.max.x, lowerBounds.max.y);
        }
        else
        {
            raycastOrigins.topLeft = new Vector2(upperBounds.min.x, upperBounds.max.y);
            raycastOrigins.topRight = new Vector2(upperBounds.max.x, upperBounds.max.y);
        }
    }

    void CalculateRaySpacing(bool crouch)
    {
        Bounds lowerBounds = mainCollider.bounds;
        Bounds upperBounds = crouchDisableCollider.bounds;
        lowerBounds.Expand(skinWidth * -2);
        upperBounds.Expand(skinWidth * -2);

        horizontalRayCount = Mathf.Clamp(horizontalRayCount, 2, int.MaxValue);
        verticalRayCount = Mathf.Clamp(verticalRayCount, 2, int.MaxValue);

        if (crouch)
        {
            horizontalRaySpacing = lowerBounds.size.y / (horizontalRayCount - 1);
            verticalRaySpacing = lowerBounds.size.x / (verticalRayCount - 1);
        }
        else
        {
            horizontalRaySpacing = (upperBounds.size.y + lowerBounds.size.y) / (horizontalRayCount - 1);
            verticalRaySpacing = lowerBounds.size.x / (verticalRayCount - 1);
        }
    }

    struct RaycastOrigins
    {
        public Vector2 topLeft, topRight;
        public Vector2 bottomLeft, bottomRight;
    }

    public struct CollisionInfo
    {
        public bool above, below;
        public bool left, right;

        public bool onSlope;
        public bool climbingSlope, descendingSlope;
        public float slopeAngle, slopeAngleOld, slopeAngleCenter;
        public Vector3 velocityOld;

        public int faceDir, slopeDir;
        public bool crouchLock;
        public string horizontalTag, verticalTag;
        public void Reset()
        {
            above = below = false;
            left = right = false;
            climbingSlope = false;
            descendingSlope = false;

            slopeAngleOld = slopeAngle;
            slopeAngle = 0;
        }
    }
}
