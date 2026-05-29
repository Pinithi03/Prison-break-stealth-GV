using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GuardController : MonoBehaviour
{
    public NavMeshAgent agent;
    public Animator anim;
    
    // Testing path variables
    public List<Vector3> currentPath = new List<Vector3>();
    private int currentPathIndex = 0;
    
    public enum GuardState { Patrol, Chase, Search }
    public GuardState currentState = GuardState.Patrol;

    public float rotationSpeed = 5f;

    void Start()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (anim == null) anim = GetComponent<Animator>();
        
        // Dummy test path (we will replace this with IS array later)
        currentPath.Add(new Vector3(0, 0, 0));
        currentPath.Add(new Vector3(5, 0, 0));
        currentPath.Add(new Vector3(5, 0, 5));
        currentPath.Add(new Vector3(0, 0, 5));
        
        if (currentPath.Count > 0)
        {
            agent.SetDestination(currentPath[0]);
        }
    }

    void Update()
    {
        switch (currentState)
        {
            case GuardState.Patrol:
                UpdatePatrol();
                break;
            case GuardState.Chase:
                // Chase logic later
                break;
            case GuardState.Search:
                // Search logic later
                break;
        }

        UpdateAnimation();
    }

    void UpdatePatrol()
    {
        if (currentPath == null || currentPath.Count == 0) return;
        if (!agent.isOnNavMesh) return; // Prevent error if NavMesh is missing

        // Check if we reached the current waypoint
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            currentPathIndex = (currentPathIndex + 1) % currentPath.Count;
            agent.SetDestination(currentPath[currentPathIndex]);
        }

        SmoothRotation();
    }

    void SmoothRotation()
    {
        if (!agent.isOnNavMesh) return;
        
        if (agent.velocity.sqrMagnitude > Mathf.Epsilon)
        {
            Quaternion lookRotation = Quaternion.LookRotation(agent.velocity.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        }
    }

    void UpdateAnimation()
    {
        if (anim != null && anim.runtimeAnimatorController != null) // Prevent Animator warning
        {
            float speed = agent.velocity.magnitude;
            anim.SetFloat("Speed", speed);
        }
    }
}
