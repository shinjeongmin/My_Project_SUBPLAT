using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShieldLever : KeyHoleCheck
{
    [SerializeField] GameObject shield;
    SpriteRenderer sp;

    // Start is called before the first frame update
    void Start()
    {
        sp = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override void WhenActive()
    {
        if (!isActive)
        {
            isActive = true;
            shield.SetActive(!shield.activeSelf);
            this.tag = "Usable";
            sp.sprite.name = "crank-down";
        }
        else
        {
            shield.SetActive(!shield.activeSelf);
            sp.sprite.name = "crank-up";
        }
    }
}
