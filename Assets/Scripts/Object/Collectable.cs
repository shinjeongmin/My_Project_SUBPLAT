using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collectable : MonoBehaviour
{
    public int index; // 수집품의 인덱스 (순서)
    // PlayerInteraction.cs의 COLLECTABLES_COUNT에 대해 COLLECTABLES_COUNT > index >= 0 이여야 함

    // 수집품은 접촉하면 바로 먹어짐
    void OnTriggerEnter2D(Collider2D collider2D)
    {
        if (collider2D.tag == "Player")
        {
            try
            {
                collider2D.gameObject.GetComponent<PlayerInteraction>().collectables[index] = true;
                gameObject.SetActive(false);
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.Message);
            }
        }
    }
}
