using UnityEngine;
using System.Collections;

/*
 파일명 : ScriptPhysicsCarController

차의 물리를 다루는 스크립트입니다. 차의 이동에 관한 함수와 여러 장아물이나 지형과의 충돌에 대한 함수들을 담고 있습니다. 
이동에 관련한 부분은 수정을 자제해주시길 바랍니다.
*/

public class PhysicsCarController : KeyHoleCheck 
{
    /* public변수 선언 */
    public float speedF;                //앞바퀴 속도 float변수
    public float speedB;                //뒷바퀴 속도 float변수

    public float torqueF;               //앞바퀴 돌림힘 float변수
    public float torqueB;               //뒷바퀴 돌림힘 float 변수

    public bool TractionFront = true;   //앞바퀴 정지마찰력 bool변수
    public bool TractionBack = true;    //뒷바퀴 정지마찰력 bool변수

    public float carRotationSpeed;      //차의 회전 속도 float변수

    public WheelJoint2D frontwheel;     //앞바퀴 WheelJoint2D 컴포넌트 변수
    public WheelJoint2D backwheel;      //뒷바퀴 WheelJoint2D 컴포넌트 변수

    public LayerMask whatIsGround;      // 땅 (자동차가 충돌하면 멈추는) 레이어

    /* private변수 선언 */
    JointMotor2D motorFront;            //앞바퀴 JointMotor2D 변수
    JointMotor2D motorBack;             //뒷바퀴 JointMotor2D 변수
    Rigidbody2D rigid;                  //Rigidbody2D 컴포넌트 변수

    /* Awake() 함수 정의 */
    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
    }

    /*FixedUpdate() 함수 정의*/
    void FixedUpdate()
    {
        //지형과 충돌시 정지
        Vector2 frontVec = new Vector2(rigid.position.x + 1.3f, rigid.position.y);
        Debug.DrawRay(frontVec, Vector3.right, new Color(0, 1, 0));
        RaycastHit2D rayHit = Physics2D.Raycast(frontVec, Vector3.right, 1, whatIsGround);

        if (rayHit.collider != null)
        {
            frontwheel.useMotor = false;
            speedF = 0;
            torqueF = 0;
        }
    }
    
    /*유틸리티 함수 정의*/

    //차와 장애물 충돌 함수
    void OnCollisionEnter2D(Collision2D arg_collision)
    {
        //속도가 있는 상대에서 장애물과 충돌하면 파괴
        if (arg_collision.gameObject.tag == "Breakable" && motorFront.motorSpeed != 0f)
            arg_collision.gameObject.GetComponent<BreakableBlock>().ExplodeThisGameObject();
    }

    /*공용 함수 정의*/

    //차의 이동 함수
    override public void WhenActive()
    {
        isActive = true;
        rigid.bodyType = RigidbodyType2D.Dynamic;
        Move();
    }

    public void Move()
    {
        if (TractionFront)
        {
            motorFront.motorSpeed = speedF * -1;
            motorFront.maxMotorTorque = torqueF;
            frontwheel.motor = motorFront;
        }
        if (TractionBack)
        {
            motorBack.motorSpeed = speedF * -1;
            motorBack.maxMotorTorque = torqueF;
            backwheel.motor = motorBack;
        }
    }
}