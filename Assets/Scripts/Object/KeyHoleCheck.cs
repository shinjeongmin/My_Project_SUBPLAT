using UnityEngine;
using UnityEngine.Animations;

// 수정을 사용하는 Object의 정의에 사용되는 abstract class
public abstract class KeyHoleCheck : MonoBehaviour
{
    /*[HideInInspector]*/ public bool isActive = false; // isActive는 수정을 사용하는 모든 Object가 public bool값으로 가져야 합니다

    // WhenActive는 수정을 이용한 모든 Object가 수정이 끼워졌을 시 호출됩니다
    public abstract void WhenActive();
}
