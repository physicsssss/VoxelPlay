using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using VoxelPlay;

public class AIController : MonoBehaviour
{
    public enum Type
    {
        NPC,
        Enemy
    }

    public enum AIStates
    {
        Idle,
        Patrolling,
        Chasing,
        Attacking,
        Talking,
    }
    public int AIdamage;
    [Header("Common Settings")]
    public Type AIType;
    public AIStates currentState;
    public Collider playerCollider;

    [Header("Health")]
    public float maxHealth;
    public float currentHealth;



    [System.Serializable]
    public class PatrollingSettings
    {
        private Vector3 currentTarget;
        
        [HideInInspector] public Vector3 startingPoint;
        public float radius = 100;
        public float stoppingDistance = 10;
        public float speed = 10;
        [Range(0, 2)]
        public int movingSpeed = 1;
        [HideInInspector] public int currentQuadrent = 0;

        public Vector3 CurrentTarget { get => currentTarget; }

        public void Init(Vector3 playerPosition)
        {
            startingPoint = currentTarget = playerPosition;
        }

        public void UpdateDestination(Vector3 playerPosition)
        {
            if (Vector3.Distance(currentTarget, playerPosition) > stoppingDistance)
                return;

            int quadrent = UnityEngine.Random.Range(1, 5);

            if (!playerPosition.Equals(startingPoint) && currentQuadrent == quadrent)
                quadrent = (quadrent + 1) % 4;

            currentQuadrent = quadrent;

            float x = 0, y = playerPosition.y, z = 0;

            switch (quadrent)
            {
                case 0:
                    x = UnityEngine.Random.Range(0, radius);
                    z = UnityEngine.Random.Range(0, radius);
                    break;
                case 1:
                    x = UnityEngine.Random.Range(0, -radius);
                    z = UnityEngine.Random.Range(0, radius);
                    break;
                case 2:
                    x = UnityEngine.Random.Range(0, -radius);
                    z = UnityEngine.Random.Range(0, -radius);
                    break;
                case 3:
                    x = UnityEngine.Random.Range(0, radius);
                    z = UnityEngine.Random.Range(0, -radius);
                    break;
            }

            currentTarget = new Vector3(x, y, z);
        }
    }
    public PatrollingSettings patrollingSettings;

    [HideInInspector] public NavMeshAgent agent;
    [HideInInspector] public Animator animator;

    private void Start()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();
        currentState = AIStates.Patrolling;

        animator = transform.GetComponent<Animator>();

        patrollingSettings.Init(transform.position);

        if(AIType.Equals(Type.NPC))
        {
            patrollingSettings.movingSpeed = UnityEngine.Random.Range(0, 3);
            patrollingSettings.speed = patrollingSettings.movingSpeed * 10;
        }
    }
    private void Update()
    {
        if (currentHealth <= 0)
        {
            Destroy(this.gameObject);
        }
    }
    public virtual void Patrol()
    {
        if (currentState == AIStates.Patrolling && patrollingSettings.movingSpeed > 0 && agent.isActiveAndEnabled)
        {
            PrintLog("Patrolling");
            patrollingSettings.UpdateDestination(transform.position);

            agent.isStopped = false;
            agent.SetDestination(patrollingSettings.CurrentTarget);
            agent.speed = patrollingSettings.speed;

            if(AIType == Type.Enemy)
                animator.SetBool("IsAttacking", false);
            else
                animator.SetBool("IsTalking", false);

            // if(animator.GetInteger("Speed") != patrollingSettings.movingSpeed)
            //   animator.SetInteger("Speed", patrollingSettings.movingSpeed);
            animator.SetFloat("Speed", agent.velocity.magnitude);
        }
        else
        {
            //if(animator.GetInteger("Speed") != patrollingSettings.movingSpeed)
                animator.SetFloat("Speed", agent.velocity.magnitude);
        }
    }
    public void DamagePlayer()
    {
        playerCollider.GetComponent<VoxelPlayPlayer>().DamageToPlayer(AIdamage);
    }
    void PrintLog(string log)
    {
//        Debug.Log("AIController: " + log);
    }

    void PrintLogError(string log)
    {
        Debug.LogError("AIController Error: " + log);
    }
    public void DoDamage(float _dmg)
    {
        Debug.LogError("Damaging");
        currentHealth -= _dmg;
    }
}

