using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelPlay;

public class EnemyController : AIController
{

    [Header("Customized Settings")]
    public float chaseSpeed;

    // For enemy sighting
    [System.Serializable]
    public struct SightingSettings
    {
        public Camera myCam;
        [HideInInspector] public Plane[] planes;
        public float lookDistance;
        public float attackDistance;
    }
    public SightingSettings sightingSettings;

    private void Update()
    {
        if(!playerCollider)
            playerCollider = GameObject.FindGameObjectWithTag("Player").GetComponent<Collider>();

        if (CheckPlayerInBounds())
        {
            PrintLog("Distance from Player -> " + Vector3.Distance(transform.position, playerCollider.transform.position));
            if (AttackPlayer())
            {
                currentState = AIStates.Attacking;
            }
            else
                currentState = AIStates.Chasing;
        }
        else
            currentState = AIStates.Patrolling;

        HandleState();
        if (currentHealth <= 0)
        {
            Destroy(this.gameObject);
        }
    }

    bool AttackPlayer()
    {
        return Vector3.Distance(transform.position, playerCollider.transform.position) <= sightingSettings.attackDistance;
    }

    bool CheckPlayerInBounds()
    {
        sightingSettings.planes = GeometryUtility.CalculateFrustumPlanes(sightingSettings.myCam);
        if (GeometryUtility.TestPlanesAABB(sightingSettings.planes, playerCollider.bounds))
        {
            PrintLog("Player Sighted");
            return true;
        }

        return false;
    }

    bool CheckPlayerInVision()
    {
        RaycastHit hit;
        Debug.DrawRay(sightingSettings.myCam.transform.position, transform.forward * sightingSettings.lookDistance, Color.red);
        if (Physics.Raycast(sightingSettings.myCam.transform.position, transform.forward, out hit, sightingSettings.lookDistance))
        {
            PrintLog("Start Chase");
            return true;
        }

        return false;
    }

    void HandleState()
    {
        switch (currentState)
        {
            case AIStates.Attacking:
                Attack();
                break;
            case AIStates.Chasing:
                Chase();
                break;
            case AIStates.Patrolling:
                Patrol();
                break;
        }
    }

    void Attack()
    {
        PrintLog("Attack");
        agent.speed = 0;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        if (!animator.GetBool("IsAttacking"))
            animator.SetBool("IsAttacking", true);
       // if (animator.GetInteger("Speed") != 0)
            animator.SetFloat("Speed", agent.velocity.magnitude);
        // TODO: Attacking mechanism
    }

    void Chase()
    {
        PrintLog("Chase");
        agent.isStopped = false;
        agent.SetDestination(playerCollider.transform.position);
        agent.speed = chaseSpeed;

        if (animator.GetBool("IsAttacking"))
            animator.SetBool("IsAttacking", false);
       // if (animator.GetInteger("Speed") != 2)
            animator.SetFloat("Speed", agent.velocity.magnitude);

    }

    
    void PrintLog(string log)
    {
        Debug.Log("EnemyController: " + log);
    }
}
