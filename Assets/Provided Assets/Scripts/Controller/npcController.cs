using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class npcController : AIController
{
    [System.Serializable]
    public struct ConversationSettings
    {
        public bool canTalk;
        public string[] lines;
        public AudioClip[] voice;

        public bool IsInTalkingRange;
        public bool isTalking;
    }

    [Header("Customized Settings")]
    public ConversationSettings conversationSettings;
    public bool isMovingNPC;
    // Start is called before the first frame update

    // Update is called once per frame
    void Update()
    {
        if (conversationSettings.canTalk && conversationSettings.IsInTalkingRange && !conversationSettings.isTalking)
        {
            currentState = AIStates.Talking;
        }
        else if(patrollingSettings.movingSpeed > 0)
        {
            currentState = AIStates.Patrolling;
        }
        else
            currentState = AIStates.Idle;

        HandleState();
    }

    void HandleState()
    {
        switch (currentState)
        {
            case AIStates.Talking:
                Talk();
                break;
            case AIStates.Patrolling:
                Patrol();
                break;
            case AIStates.Idle:
                Idle();
                break;
        }
    }

    void Talk()
    {
        conversationSettings.isTalking = true;
        if(!animator.GetBool("IsTalking"))
            animator.SetBool("IsTalking", true);

        // TODO: Conversation Mechanism
    }

    void Idle()
    {
        animator.SetBool("IsTalking", false);
        animator.SetFloat("Speed", agent.velocity.magnitude);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            conversationSettings.IsInTalkingRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            conversationSettings.IsInTalkingRange = false;
        }
    }
}
