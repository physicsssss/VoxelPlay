using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorController : MonoBehaviour
{
    public Transform weapoinIK;
    public float turnSpeed;
    Animator animator;
    public GameObject cam;
    public float animatorWrightTime = 0.4f;
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
        animator.SetFloat("isMoving", Input.GetAxis("Vertical"));
        if (Input.GetMouseButton(0))
        {
            if (IsInvoking("SetLayerWeight"))
            {
                CancelInvoke("SetLayerWeight");
            }
            animator.SetLayerWeight(1, 1);
            isHittable = true;
        }
        else
        {
            if(!IsInvoking("SetLayerWeight"))
            Invoke("SetLayerWeight", animatorWrightTime);
            isHittable = false;
        }
    }
    void SetLayerWeight()
    {
        animator.SetLayerWeight(1, 0);
    }
    bool isHittable;
    private void OnTriggerStay(Collider other)
    {
        if(other.tag == "Enemy")
        {

            if (isHittable)
            {
                _other = other;
                if(!IsInvoking("onAttack"))
                Invoke("onAttack", 1f);
            }

        }
    }
    Collider _other;
    void onAttack()
    {
        _other.SendMessage("DoDamage", 20f);
        isHittable = false;
    }
}
