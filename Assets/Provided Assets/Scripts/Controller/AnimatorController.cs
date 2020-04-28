using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorController : MonoBehaviour
{
    Animator animator;
    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        TakeInput();
    }

    void TakeInput()
    {
        if(Input.GetKey(KeyCode.UpArrow))
        {
            animator.SetInteger("isMoving", 1);
        }
        else if(Input.GetKey(KeyCode.DownArrow))
        {
            animator.SetInteger("isMoving", -1);
        }
        else
        {
            animator.SetInteger("isMoving", 0);
        }
    }
}
