using UnityEngine;
using UnityEngine.SceneManagement;

/*
파일명: gameManager
기본적인 맵과 UI의 관리를 다루는 스크립트입니다. 맵 이동에 관한 함수와 캐릭터의 기본적인 정보에 관해 관리합니다.
또한 기본적인 UI들을 관리합니다.
*/

public class GameManager : MonoBehaviour
{
    /* pubic 변수 선언 */
    public int stageIndex;                      // 현재 있는 스테이지 수

    public GameObject player;
    PlayerMovement pm;   // player의 함수를 쓰기위한 변수

    void Start()
    {
        pm = player.GetComponent<PlayerMovement>();
    }

    void Update()
    {
        if (player.transform.position.y < -10)
        {
            GameOver();
        }
    }

    //플레이어를 다시 원점으로 돌려놓는 함수
    void PlayerReposition()
    {
        pm.transform.position = Vector3.zero;
        pm.velocity = Vector3.zero;
    }

    /* 공용 함수 */
    // 다음 스테이지로 넘어가기 위한 함수
    // 임시, 수정할 것!
    public void NextStage()
    {
        SceneManager.LoadScene("SampleScene2");
    }

    public void GameOver() // 게임 오버, 임시로 넣은 것이므로 나중에 수정할 것
    {
        player.SetActive(false);
        Invoke("PlayerRestart", 1f);
    }

    public void PlayerRestart() 
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}