using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;

public class SampleCutSceneTrigger : MonoBehaviour
{
    bool scenePlayed = false;
    public GameObject player;
    PlayableDirector playableDirector;

    void Start()
    {
        playableDirector = GetComponent<PlayableDirector>();
    }

    void Update()
    {
        if (transform.GetChild(0).gameObject.activeSelf) // cutSceneEnd는 언제나 첫 자식 Object로 할 것
        {
            player.SetActive(true);
        }
    }

    void OnTriggerEnter2D(Collider2D collide)
    {
        if (collide.tag == "Player" && !scenePlayed)
        {
            playableDirector.Play(); 
            scenePlayed = true;
            player.SetActive(false);
        }
    }
}
