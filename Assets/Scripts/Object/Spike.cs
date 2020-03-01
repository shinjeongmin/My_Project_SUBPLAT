using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spike : MonoBehaviour
{
    public GameManager gameManager; //게임 매니저의 함수를 쓰기위한 변수

    void OnTriggerEnter2D(Collider2D arg_collider)
    {
        print("collide");
        if (arg_collider.gameObject.tag == "Player")
        {
            gameManager.GameOver();
        }
    }
}
