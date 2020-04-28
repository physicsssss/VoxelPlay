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
        
            animator.SetInteger("isMoving", (int)Input.GetAxis("Vertical"));
        
    }
}
