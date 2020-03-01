using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreakableBlock : RaycastCollider
{
    [HideInInspector] public Vector3 velocity; // 박스의 속력
    [SerializeField] UnityEngine.Object destructableRef;
    public CollisionInfo collisions;

    public override void Start()
    {
        base.Start();
        gravity = -2 * player.GetComponent<PlayerMovement>().maxJumpHeight / Mathf.Pow(player.GetComponent<PlayerMovement>().timeToJumpApex, 2f);
    }

    void Update()
    {
        if (velocity.y < -fallingSpeedMax) // 일정 속도 이상 낙하하지 않음
            velocity.y = -fallingSpeedMax;

        if (collisions.above || collisions.below) // raycast에서의 중력 축적 방지
            velocity.y = 0;

        velocity.y += gravity * Time.deltaTime;
        Move(velocity * Time.deltaTime);
    }

    void Move(Vector3 velocity)
    {
        UpdateRaycastOrigins();
        collisions.Reset();

        if (velocity.y != 0)
            VerticalCollisions(ref velocity);

        transform.Translate(velocity);
    }

    // 차량이나 활성화된 차폐막에 닿았을 때만 부서지도록 정의
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Shield")
        {
            ExplodeThisGameObject();
        }
    }

    // 적절한 sprite가 없음, 있을 시 제대로 작동할 것
    public void ExplodeThisGameObject()
    {
        print("Boom");
        Destroy(gameObject);
        GameObject destuctable = (GameObject)Instantiate(destructableRef);

        //map the new loaded destructable object to the x and y position of the previously destroyed barrel
        destuctable.transform.position = transform.position;
        Destroy(gameObject);
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

        public void Reset()
        {
            above = below = false;
        }
    }
}