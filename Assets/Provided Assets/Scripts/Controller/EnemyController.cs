using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : AIController
{

    [Header("Customized Settings")]
    public Collider playerCollider;
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
        PrintLog("Update");
        if (CheckPlayerInBounds() && CheckPlayerInVision())
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
        agent.speed = 0;
        agent.isStopped = true;

        // TODO: Attacking animation
        // TODO: Attacking mechanism
    }

    void Chase()
    {
        agent.isStopped = false;
        agent.SetDestination(playerCollider.transform.position);
        agent.speed = chaseSpeed;

        // TODO: Chasing animations
    }

    void PrintLog(string log)
    {
        Debug.Log("EnemyController: " + log);
    }
}
