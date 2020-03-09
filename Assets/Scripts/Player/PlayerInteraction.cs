using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

/*
Script_Player_Interaction.cs

플레이어가 상호작용 할 수 있는 물체에 대한 작동 여부, 수정 소지 여부, 소집품 소지 여부를 판단하며
상호작용 가능한 물체에 대한 우선순위를 거리순으로 결정함.
*/

public class PlayerInteraction : MonoBehaviour
{
    // TODO: 수정을 가지고 있을 때 애니메이션 교체
    public SpriteRenderer haveKeyRender; // 플레이어가 수정을 가지고 있을 때 표시하는 Sprite
    public float m_awarenessSize; // 플레이어가 상호작용 할 수 있는 범위

    [HideInInspector] public bool haveKey = false; // 키를 가지고 있는지 확인
    Controller2D controller; // 플레이어의 Controller2D
    GameObject[] keygen_arr; // 모든 수정 GameObj가 담긴 array
    GameObject[] keyhole_arr; // 모든 수정을 이용하는 GameObj가 담긴 array
    GameObject[] usable_arr; // 모든 작동 가능한 GameObj가 담긴 array

    const int COLLECTABLES_COUNT = 5; // 수집품의 개수
    public bool[] collectables = new bool[COLLECTABLES_COUNT]; // 수집품 수집 여부가 정리된 array

    // 로그용 수집품 수집 여부
    bool[] old_collectables = new bool[COLLECTABLES_COUNT];

    void Start()
    {
        controller = GetComponent<Controller2D>();
        keygen_arr = GameObject.FindGameObjectsWithTag("KeyGen"); // Tag가 KeyGen인 모든 GameObject의 array 구성
        keyhole_arr = GameObject.FindGameObjectsWithTag("KeyHole"); // Tag가 keyhole인 모든 GameObject의 array 구성
        usable_arr = GameObject.FindGameObjectsWithTag("Usable"); // Tag가 Usable인 모든 GameObject의 array 구성
    }

    void Update()
    {
        /*
         * 우선 순위
         * 1. 무조건 거리순.
         * 2. 거리가 같으면, 수정 => 수정을 끼우지 않은 Obj => 
         *    수정을 끼운 Obj => 수정이 필요 없는 Obj 순으로 작동함.
         */

        for (int i = 0; i < COLLECTABLES_COUNT; i++)
        {
            if (collectables[i] != old_collectables[i])
            {
                Debug.Log("Collectable " + i + " Set to True");
                old_collectables[i] = collectables[i];
            }
        }

        int closest_key = -1; // 플레이어로부터 가장 가까운 수정의 index  
        float closest_distance_keygen = m_awarenessSize + 1; // 플레이어로부터 가장 가까운 수정과의 거리
        int closest_keyhole = -1; // 플레이어로부터 가장 가까운 keyhole의 index
        float closest_distance_keyhole = m_awarenessSize + 1; // 플레이어로부터 가장 가까운 keyhole와의 거리
        int closest_usable = -1; // 플레이어로부터 가장 가까운 obj의 index
        float closest_distance_use = m_awarenessSize + 1; // 플레이어로부터 가장 가까운 obj와의 거리

        for (int i = 0; i < keygen_arr.Length; ++i)
        {
            Transform keygen_tr = keygen_arr[i].transform; // 현재 선택된 수정의 위치

            // 수정과 플레이어간의 거리가 상호작용 범위보다 작다면
            if (Vector3.Distance(transform.position, keygen_tr.position) <= m_awarenessSize)
            {
                // 선택된 수정이 없을 때
                if (closest_key == -1)
                {
                    closest_key = i;
                    closest_distance_keygen = Vector3.Distance(transform.position, keygen_arr[closest_key].transform.position);
                }
                // 선택된 수정이 있으나, 현재 선택된 수정과 플레이어간의 거리가 더 가까울 때
                else if (closest_distance_keygen > Vector3.Distance(transform.position, keygen_tr.position))
                {
                    closest_key = i;
                }
            }
        }


        for (int i = 0; i < keyhole_arr.Length; ++i)
        {
            Transform keyhole_tr = keyhole_arr[i].transform; // 현재 선택된 수정을 사용하는 Obj의 위치

            // keyhole와 플레이어간의 거리가 상호작용 범위보다 작고, keyhole가 활성화된 상태가 아니라면
            if (Vector3.Distance(transform.position, keyhole_tr.position) <= m_awarenessSize) // isActive라는 변수가 존재해야 함
            {

                // 선택된 keyhole가 없을 때
                if (closest_keyhole == -1)
                {
                    closest_keyhole = i;
                    closest_distance_keyhole = Vector3.Distance(transform.position, keyhole_arr[closest_keyhole].transform.position);
                }
                // 선택된 keyhole가 있으나, 현재 선택된 keyhole와 플레이어간의 거리가 더 가까울 때
                else if (closest_distance_keyhole > Vector3.Distance(transform.position, keyhole_tr.position))
                {
                    closest_keyhole = i;
                }
            }
        }

        for (int i = 0; i < usable_arr.Length; ++i)
        {
            Transform usable_tr = usable_arr[i].transform; // 현재 선택된 수정을 사용하지 않는 Obj의 위치

            // 사용 가능한 오브젝트와 플레이어간의 거리가 상호작용 범위보다 작을 때
            if (Vector3.Distance(transform.position, usable_tr.position) <= m_awarenessSize)
            {
                // 선택된 Obj가 없을 때
                if (closest_usable == -1)
                {
                    closest_usable = i;
                    closest_distance_use = Vector3.Distance(transform.position, usable_arr[closest_keyhole].transform.position);
                }
                // 선택된 keyhole가 있으나, 현재 선택된 keyhole와 플레이어간의 거리가 더 가까울 때
                else if (closest_distance_use > Vector3.Distance(transform.position, usable_tr.position))
                {
                    closest_usable = i;
                }
            }
        }

        if (haveKey)
        {
            // 플레이어 근처에 keyhole가 있고, 'E' 키를 누를 때
            if (Input.GetButtonDown("Use") && closest_keyhole != -1 && closest_distance_keyhole <= closest_distance_use)
            {
                if (!keyhole_arr[closest_keyhole].GetComponent<KeyHoleCheck>().isActive)
                {
                    haveKey = false;
                    haveKeyRender.enabled = false;
                    keyhole_arr[closest_keyhole].GetComponent<KeyHoleCheck>().WhenActive(); // WhenActive라는 public method가 존재해야 함
                }
                else // 수정을 이미 끼운 물체
                    keyhole_arr[closest_keyhole].GetComponent<KeyHoleCheck>().WhenActive(); // WhenActive라는 public method가 존재해야 함
            }
            // 수정 없이도 사용 가능한 물체
            else if (Input.GetButtonDown("Use") && closest_usable != -1 && closest_distance_keyhole > closest_distance_use)
                usable_arr[closest_usable].GetComponent<KeyHoleCheck>().WhenActive();
        }
        else
        {
            // 플레이어 근처에 수정이 있고, 'Use' 키를 누를 때
            if (Input.GetButtonDown("Use") && closest_key != -1 && closest_distance_keygen <= closest_distance_use && closest_distance_keygen <= closest_distance_keyhole)
            {
                if (keygen_arr[closest_key].activeSelf)
                {
                    haveKey = true;
                    haveKeyRender.enabled = true;
                    keygen_arr[closest_key].SetActive(false);
                }
            }
            // 수정을 사용해서 켜져있고, 사용 가능한 물체일 때
            else if (Input.GetButtonDown("Use") && closest_keyhole != -1 && closest_distance_keyhole <= closest_distance_use && closest_distance_keyhole <= closest_distance_keygen)
            {
                if (keyhole_arr[closest_keyhole].GetComponent<KeyHoleCheck>().isActive)
                    keyhole_arr[closest_keyhole].GetComponent<KeyHoleCheck>().WhenActive(); // WhenActive라는 public method가 존재해야 함
            }
            // 수정 없이도 사용 가능한 물체일 때
            else if (Input.GetButtonDown("Use") && closest_usable != -1 && closest_distance_keygen > closest_distance_use && closest_distance_keyhole > closest_distance_use)
                usable_arr[closest_usable].GetComponent<KeyHoleCheck>().WhenActive();
        }
    }
}
