using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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
        Talking
    }

    [Header("Common Settings")]
    public Type AIType;
    public AIStates currentState;

    [System.Serializable]
    public struct PatrollingSettings
    {
        [System.Serializable]
        public class WayPoint
        {
            public Transform patrollingPoint;
            public float pointTime;
        }

        public int defaultWayPoint;
        public int currentWayPoint;
        public float updatePointDistance;

        // Walking speed
        public float speed;
        public WayPoint[] wayPoints;

        public Vector3 CurrentTarget
        {
            get => wayPoints[currentWayPoint].patrollingPoint.position;
        }

        public bool WayPointsAvailible
        {
            get => wayPoints.Length > 0;
        }

        public void UpdateDestination(Vector3 myPosition)
        {
            if (Vector3.Distance(myPosition, CurrentTarget) <= updatePointDistance)
                currentWayPoint = (currentWayPoint + 1) % wayPoints.Length;
        }
    }
    public PatrollingSettings patrollingSettings;

    [HideInInspector] public NavMeshAgent agent;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        currentState = AIStates.Patrolling;
    }

    private void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            //transform.localEulerAngles = new Vector3(0, 90, 0);

        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            //transform.localEulerAngles = new Vector3(0, 0, 0);

        }
    }

    public virtual void Patrol()
    {
        PrintLog("WayPointsAvailible -> " + patrollingSettings.WayPointsAvailible);
        if (currentState == AIStates.Patrolling && patrollingSettings.WayPointsAvailible)
        {
            PrintLog("Patrolling");
            agent.isStopped = false;
            agent.SetDestination(patrollingSettings.CurrentTarget);
            agent.speed = patrollingSettings.speed;

            patrollingSettings.UpdateDestination(transform.position);

            // TODO: Patrolling Animation
        }
    }

    void PrintLog(string log)
    {
        Debug.Log("AIController: " + log);
    }
}
