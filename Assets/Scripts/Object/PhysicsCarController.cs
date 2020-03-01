using UnityEngine;
using System.Collections;

/*
 ���ϸ� : ScriptPhysicsCarController

���� ������ �ٷ�� ��ũ��Ʈ�Դϴ�. ���� �̵��� ���� �Լ��� ���� ��ƹ��̳� �������� �浹�� ���� �Լ����� ��� �ֽ��ϴ�. 
�̵��� ������ �κ��� ������ �������ֽñ� �ٶ��ϴ�.
*/

public class PhysicsCarController : KeyHoleCheck 
{
    /* public���� ���� */
    public float speedF;                //�չ��� �ӵ� float����
    public float speedB;                //�޹��� �ӵ� float����

    public float torqueF;               //�չ��� ������ float����
    public float torqueB;               //�޹��� ������ float ����

    public bool TractionFront = true;   //�չ��� ���������� bool����
    public bool TractionBack = true;    //�޹��� ���������� bool����

    public float carRotationSpeed;      //���� ȸ�� �ӵ� float����

    public WheelJoint2D frontwheel;     //�չ��� WheelJoint2D ������Ʈ ����
    public WheelJoint2D backwheel;      //�޹��� WheelJoint2D ������Ʈ ����

    public LayerMask whatIsGround;      // �� (�ڵ����� �浹�ϸ� ���ߴ�) ���̾�

    /* private���� ���� */
    JointMotor2D motorFront;            //�չ��� JointMotor2D ����
    JointMotor2D motorBack;             //�޹��� JointMotor2D ����
    Rigidbody2D rigid;                  //Rigidbody2D ������Ʈ ����

    /* Awake() �Լ� ���� */
    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
    }

    /*FixedUpdate() �Լ� ����*/
    void FixedUpdate()
    {
        //������ �浹�� ����
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
    
    /*��ƿ��Ƽ �Լ� ����*/

    //���� ��ֹ� �浹 �Լ�
    void OnCollisionEnter2D(Collision2D arg_collision)
    {
        //�ӵ��� �ִ� ��뿡�� ��ֹ��� �浹�ϸ� �ı�
        if (arg_collision.gameObject.tag == "Breakable" && motorFront.motorSpeed != 0f)
            arg_collision.gameObject.GetComponent<BreakableBlock>().ExplodeThisGameObject();
    }

    /*���� �Լ� ����*/

    //���� �̵� �Լ�
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