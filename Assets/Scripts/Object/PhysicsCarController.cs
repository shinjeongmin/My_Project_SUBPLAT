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

    public bool useCar = false;
    public bool carDirection;

    /* private변수 선언 */
    JointMotor2D motorFront;            //앞바퀴 JointMotor2D 변수
    JointMotor2D motorBack;             //뒷바퀴 JointMotor2D 변수
    Rigidbody2D rigid;                  //Rigidbody2D 컴포넌트 변수

    /* Awake() 함수 정의 */
    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
    }

    /*유틸리티 함수 정의*/

    //차와 장애물 충돌 함수
    void OnCollisionEnter2D(Collision2D arg_collision)
    {
        //속도가 있는 상대에서 장애물과 충돌하면 파괴
        if (arg_collision.gameObject.tag == "Breakable" && speedF > 500)
        {
            if (gameObject.transform.position.x < arg_collision.transform.position.x)
            {
                attack(-1);
            }
            else if (gameObject.transform.position.x > arg_collision.transform.position.x)
            {
                attack(1);
            }

            arg_collision.gameObject.GetComponent<BreakableBlock>().ExplodeThisGameObject();
        }
    }

    //장애물과 충돌처리 함수
    void attack(int dir)
    {
        rigid.AddForce(new Vector2(dir, 0) * 20, ForceMode2D.Impulse);
        speedF = 50;
        torqueF = 100;
    }

    //가속 함수
    void Acceleration()
    {
        speedF += 25;
        torqueF += 160;
    }

    /*공용 함수 정의*/

    //차의 이동 함수
    override public void WhenActive()
    {
        if (!isActive)
            isActive = true;

        // rigid.constraints = RigidbodyConstraints2D.FreezeRotation;
        if (Mathf.Abs(transform.rotation.z) * Mathf.Rad2Deg <= 35 )
        {
            freezeX(false);
            rigid.bodyType = RigidbodyType2D.Dynamic;
            useCar = !useCar;
        }
    }

    //차의 이동 함수
    public void Move(bool direction)
    {
        //모터가 사용중이 아니라면 작동
        if (!frontwheel.useMotor)
        {
            frontwheel.useMotor = true;
            speedF = 15;
            torqueF = 100;
        }

        if (!direction)
        {
            if (TractionFront)
            {
                motorFront.motorSpeed = speedF * 1;
                motorFront.maxMotorTorque = torqueF;
                frontwheel.motor = motorFront;
            }

            if (TractionBack)
            {
                motorBack.motorSpeed = speedF * 1;
                motorBack.maxMotorTorque = torqueF;
                backwheel.motor = motorBack;

            }
        }
        else
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

        //최고 속도제한
        if (speedF < 1000 && torqueF < 6666)
        {
            if (carDirection && direction || !carDirection && !direction)
                Acceleration();
            else
            {
                speedF = 1500;
                torqueF = 1998;
            }
        }
    }

    //차를 정지시키는 함수
    public void stop()
    {
        frontwheel.useMotor = false;
        speedF = 0;
        torqueF = 0;
    }

    public void freezeX(bool freeze)
    {
        // freezeX 말고 더 나은 멈춤방법은 없는가?
        if (freeze)
            rigid.constraints = RigidbodyConstraints2D.FreezePositionX;
        else if (!freeze)
            rigid.constraints = RigidbodyConstraints2D.None;
    }
}