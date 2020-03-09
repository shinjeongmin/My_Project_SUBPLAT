using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Controller2D : MonoBehaviour // 플레이어는 별도의 Raycast를 사용함
{
    [SerializeField] const float skinWidth = .015f; // 
    [SerializeField] int horizontalRayCount = 4;
    [SerializeField] int verticalRayCount = 4;
    float horizontalRaySpacing; // 수평 레이캐스트 간격
    float verticalRaySpacing; // 수직 레이캐스트 간격
    bool crouchingOld = false; // 웅크리기 상태였는가

    [SerializeField] float maxClimbAngle = 70f; // 최대로 오를 수 있는 경사로의 각도
    [SerializeField] float maxDescendAngle = 70f; // 최대로 내려갈 수 있는 경사로의 각도
    [SerializeField] float minSlopeAngle = 10f; // 경사로로 인식하는 최소 각도

    [SerializeField] LayerMask WhatIsGround; // 땅으로 인식하는 Layer
    [SerializeField] Transform CeilingCheck; // 플레이어가 천장에 있는지 확인하는 기준 위치
    const float CeilingRadius = .2f;

    [SerializeField] BoxCollider2D mainCollider; // 아래쪽의 box collider
    [SerializeField] BoxCollider2D crouchDisableCollider; // 위쪽의 box collider

    RaycastOrigins raycastOrigins;
    public CollisionInfo collisions;

    void Start()
    {
        CalculateRaySpacing(false); //서있는 상태 시작
        collisions.faceDir = 1; //시작시 오른쪽을 향함.
    }

    public void Move(Vector3 velocity, bool crouch) // 플레이어가 움직이는 중에 충돌을 감지한다.
    {
        if (Physics2D.OverlapCircle(CeilingCheck.position, CeilingRadius, WhatIsGround) && collisions.below)
        // OverlapCircle의 범위안에 layermask가 감지되면서, 바닥과 충돌한 상태일 때. 즉 천장과 맞닿아 있을때
        {
            crouch = true;
            collisions.crouchLock = true; // 웅크리기 상태를 풀 수 없도록.
        }
        else collisions.crouchLock = false;

        if (crouchingOld != crouch) //웅크리고 있던 상태가 아니었을 때, 천장과 맞닿아 웅크리기 고정이 되면, 또는 둘 다 반대일 경우 (웅크리기 상태였는데 천장과 맞닿지 않음)
        {
            CalculateRaySpacing(crouch); // 현재 상태로 레이캐스트 간격을 수정
            crouchingOld = crouch; // 현재상태로 Old변수를 업데이트
        }

        UpdateRaycastOrigins(crouch); // 레이캐스트의 시작부분을 수정
        collisions.Reset(); // 충돌상태 초기화
        collisions.velocityOld = velocity; // 현재속도로 Old 변수를 수정.
        DetectSlope(); // 경사를 탐지하는 함수

        if (velocity.y < 0)
            DescendScope(ref velocity);

        if (velocity.x != 0)
            collisions.faceDir = Mathf.FloorToInt(Mathf.Sign(velocity.x));
        HorizontalCollisions(ref velocity);

        if (velocity.y != 0)
            VerticalCollisions(ref velocity);

        transform.Translate(velocity);
    }

    void HorizontalCollisions(ref Vector3 velocity) //수평방향 충돌감지
    {
        float directionX = collisions.faceDir; // x방향 변수
        float rayLength = Mathf.Abs(velocity.x) + skinWidth; // x방향 속력에 표면두께 더한만큼 raycast의 길이를 정함.

        if (Mathf.Abs(velocity.x) < skinWidth) // x방향 속력이  표면두께보다 수치가 작으면 raylength가 너무 짧아지므로 재수정.
            rayLength = 2 * skinWidth;

        for (int i = 0; i < horizontalRayCount; i++) // raycast들의 갯수만큼 반복
        {
            Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
            rayOrigin += Vector2.up * (horizontalRaySpacing * i);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, WhatIsGround);

            Debug.DrawRay(rayOrigin, Vector2.right * directionX * rayLength, Color.red);

            if (hit)
            {
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up); //경사각도 계산. 수직항력과 수직방향 벡터의 각도로 알 수 있음.
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
                    ClimbSlope(ref velocity, slopeAngle); //경사를 올라갈때 속력 조정함수
                    velocity.x += distanceToSlopeStart * directionX;
                }

                if (!collisions.climbingSlope || slopeAngle > maxClimbAngle) //경사에 닿아있지 않거나 오를수 있는 각도를 초과한 경우
                {
                    velocity.x = Mathf.Min(Mathf.Abs(velocity.x), (hit.distance - skinWidth)) * directionX; // x방향 속력과 raycast의 길이에서 표면두께를 뺀 값중 작은값에 방향을 곱하여 속력으로 지정.
                    rayLength = Mathf.Min(Mathf.Abs(velocity.x) + skinWidth, hit.distance); // 둘 중 작은값으로 raycast의 길이를 지정.

                    if (collisions.climbingSlope) //각도를 초과했지만 경사에 닿아있는 경우에 y방향 속력
                    {
                        velocity.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(velocity.x); // 탄젠트 각도와 x방향 속도에 따라서 y방향 속도를 지정.
                    }

                    collisions.left = directionX == -1;
                    collisions.right = directionX == 1;
                }
            }
        }
    }

    void VerticalCollisions(ref Vector3 velocity) //수직방향 충돌감지
    {
        float directionY = Mathf.Sign(velocity.y); //방향변수
        float rayLength = Mathf.Abs(velocity.y) + skinWidth; // raycast 길이

        for (int i = 0; i < verticalRayCount; i++) // raycast 갯수만큼 반복.
        {
            Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft; // raycast의 시작위치 지정. 방향에 따라 좌측하단 또는 좌측상단을 선택.
            rayOrigin += Vector2.right * (verticalRaySpacing * i + velocity.x); // 시작 위치를 수직 raycast의 간격만큼 떨어져서 배치
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, WhatIsGround); // raycast 생성

            Debug.DrawRay(rayOrigin, Vector2.up * directionY * rayLength, Color.red); // 빨간색으로 수직 raycast를 표시

            if (hit) // raycast에 감지되면
            {
                velocity.y = Mathf.Min(Mathf.Abs(velocity.y), (hit.distance - skinWidth)) * directionY; // y 최소속력
                rayLength = Mathf.Min(Mathf.Abs(velocity.y) + skinWidth, hit.distance); // raycast 최소길이
                collisions.verticalTag = hit.collider.tag; // raycast가 감지한 태그를 수직으로 닿는 태그로 설정 

                if (collisions.climbingSlope) // 경사를 오르는 중이라면
                {
                    velocity.x = velocity.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(velocity.x); // x속력을 지정.
                }

                collisions.below = directionY == -1; // 아래로 움직이는지 여부
                collisions.above = directionY == 1; // 위로 움직이는지 여부
            }
        }

        if (collisions.climbingSlope) // 경사를 오르는 중이라면
        {
            float directionX = collisions.faceDir; // 충돌한 방향을 x방향변수에 담음
            rayLength = Mathf.Abs(velocity.x + skinWidth); // raycast 길이설정
            Vector2 rayOrigin = ((directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight)
                + Vector2.up * velocity.y; // raycast의 시작점 설정. 

            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, WhatIsGround); // raycast 생성

            if (hit) // raycast가 인식하면
            {
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up); // 경사각도 계산
                if (slopeAngle != collisions.slopeAngle) // 경사각이 현재 미끄러지는 각도와 다르면
                {
                    velocity.x = (hit.distance - skinWidth) * directionX;
                    collisions.slopeAngle = slopeAngle;
                }
            }
        }
    }

    void ClimbSlope(ref Vector3 velocity, float slopeAngle) // 경사를 올라갈때 속력을 조정하는 함수
    {
        float moveDistance = Mathf.Abs(velocity.x); // 움직이는 속도
        float climbVelocityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance; // y방향 이동속도

        if (velocity.y <= climbVelocityY) // y방향 속력이 y방향으로 올라가는 속도보다 작을 때
        {
            velocity.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(velocity.x); // x방향속력과 y방향 속력을 재정의 (최소 속력이 되는듯)
            velocity.y = climbVelocityY;

            collisions.below = true;
            collisions.climbingSlope = true;
            collisions.slopeAngle = slopeAngle;
        }
    }

    void DescendScope(ref Vector3 velocity) // 경사를 내려갈때 속력을 조정하는 함수
    {
        float directionX = collisions.faceDir;
        Vector2 rayOrigin = (directionX == 1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
        // 오른쪽을 보고 있다면 좌측하단을, 왼쪽을 보고 있다면 우측하단을 시작점으로 설정
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, -Vector2.up, Mathf.Infinity, WhatIsGround); // 시작점에서 아래방향으로 raycast발사

        if (hit)
        {
            float slopeAngle = Vector2.Angle(hit.normal, Vector2.up); // 경사각도 저장 변수
            if (slopeAngle != 0 && slopeAngle <= maxDescendAngle) //경사각도가 0이 아니고 최대 이동가능 경사보다 작을 때
            {
                if (Mathf.Sign(hit.normal.x) == directionX) // raycast의 부호가 X축 방향과 같으면 (?)
                {
                    if (hit.distance - skinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(velocity.x))
                    // raycast의 거리에서 표면두께를 뺀 값()이 속력의 절댓값 * 경사각의 탄젠트값(대략 삼각형의 높이) 보다 작거나 같으면 (?)
                    {
                        float moveDistance = Mathf.Abs(velocity.x); // 이동거리 변수에 x축속력 절댓값 대입. (?)
                        float descendVelocityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance; // 하향속력. 계산 (?)
                        velocity.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(velocity.x); // 계산시 머야(?) 레퍼런스이므로 대입시 전달됨.
                        velocity.y -= descendVelocityY;

                        collisions.slopeAngle = slopeAngle; // 경사각도를 전달
                        collisions.descendingSlope = true;
                        collisions.below = true;
                    }
                }
            }
        }
    }

    void DetectSlope() //경사탐지 함수
    {
        float directionX = collisions.faceDir; // 현재 향하는 방향을 X축 방향으로 설정.
        Vector2 rayOrigin = transform.position; // 현재 위치를 레이캐스트의 시작점으로 설정.
        RaycastHit2D centerHit = Physics2D.Raycast(rayOrigin, -Vector2.up, Mathf.Infinity, WhatIsGround); //중앙위치에서 아래 방향으로 레이캐스트 발사
        RaycastHit2D rightHit = Physics2D.Raycast(raycastOrigins.bottomRight, -Vector2.up, Mathf.Infinity, WhatIsGround); //우측하단에서 아래 방향으로 레이캐스트 발사
        RaycastHit2D leftHit = Physics2D.Raycast(raycastOrigins.bottomLeft, -Vector2.up, Mathf.Infinity, WhatIsGround); //좌측 하단에서 아래 방향으로 레이캐스트 발사

        if (centerHit) // center레이캐스트가 감지되었을 때
        {
            collisions.slopeDir = (leftHit.distance < rightHit.distance) ? 1 : -1; // 경사방향 설정. 레이캐스트 길이차이를 이용해 우측하단향 경사를 1 좌측하단향 경사를 -1로 지정
            collisions.slopeAngleCenter = Vector2.Angle(centerHit.normal, Vector2.up); // 경사각도 설정. 센터raycast에서의 벡터각도와 수직 벡터와의 사이각으로 각도를 계산
            collisions.onSlope = (collisions.slopeAngleCenter > minSlopeAngle && centerHit.collider.tag == "Slope") ? true : false;
            // 경사 오브젝트 위에 위치하는지 여부 설정. 경사인지 최소각도보다 경사각이 크고, Slope 태그와 맞닿아 있을 때만 true값을 설정.
        }
    }

    void UpdateRaycastOrigins(bool crouch)
    {
        // 콜라이더로 경계설정단계
        Bounds lowerBounds = mainCollider.bounds;
        Bounds upperBounds = crouchDisableCollider.bounds;
        lowerBounds.Expand(skinWidth * -2);
        upperBounds.Expand(skinWidth * -2);

        // 레이캐스트의 좌측 하단과 우측하단 시작점 위치 설정.
        raycastOrigins.bottomLeft = new Vector2(lowerBounds.min.x, lowerBounds.min.y);
        raycastOrigins.bottomRight = new Vector2(lowerBounds.max.x, lowerBounds.min.y);

        if (crouch) //웅크린 경우와 서있는 경우를 구별하여 좌측 상단과 우측 상단의 시작점 위치 설정.
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

    void CalculateRaySpacing(bool crouch) // 레이캐스트의 간격을 지정하는 함수. crouch는 웅크리면 간격이 변하므로 false 전달
    {// Bounds는 경계 (콜라이더 같은)를 의미. 
        Bounds lowerBounds = mainCollider.bounds; // 아래쪽 콜라이더
        Bounds upperBounds = crouchDisableCollider.bounds; // 위쪽 콜라이더
        lowerBounds.Expand(skinWidth * -2); // 표면두께 * -2 만큼 사이즈 변경 ??
        upperBounds.Expand(skinWidth * -2);

        //레이캐스트의 갯수는 2개에서 정수 최댓값 사이의 값으로 고정.
        horizontalRayCount = Mathf.Clamp(horizontalRayCount, 2, int.MaxValue);
        verticalRayCount = Mathf.Clamp(verticalRayCount, 2, int.MaxValue);

        if (crouch) //웅크린 상태일 경우
        {
            horizontalRaySpacing = lowerBounds.size.y / (horizontalRayCount - 1); // 아래쪽 콜라이더 높이에서 (갯수 - 1 : {간격의 갯수는 경계선 - 1}) 만큼 나눈길이로 간격
            verticalRaySpacing = lowerBounds.size.x / (verticalRayCount - 1); // 마찬가지
        }
        else
        {
            horizontalRaySpacing = (upperBounds.size.y + lowerBounds.size.y) / (horizontalRayCount - 1); // 서있는 상태라면 콜라이더는 아래 + 위쪽 합산.
            verticalRaySpacing = lowerBounds.size.x / (verticalRayCount - 1); // 마찬가지
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
