using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GuardController2 : MonoBehaviour
{
    public NavMeshAgent agent;
    public Animator anim;

    public List<Vector3> currentPath = new List<Vector3>();
    private int currentPathIndex = 0;

    public enum GuardState { Patrol, Chase, Search }
    public GuardState currentState = GuardState.Patrol;

    public float rotationSpeed = 5f;

    void Start()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (anim == null) anim = GetComponentInChildren<Animator>();

        // Guard 2 patrol: security room zone near Keycard 3
        currentPath.Add(new Vector3(-6, 0, 3));
        currentPath.Add(new Vector3(-8, 0, 8));
        currentPath.Add(new Vector3(-6, 0, 8));
        currentPath.Add(new Vector3(-4, 0, 3));

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
                break;
            case GuardState.Search:
                break;
        }

        UpdateAnimation();
    }

    void UpdatePatrol()
    {
        if (currentPath == null || currentPath.Count == 0) return;
        if (!agent.isOnNavMesh) return;

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
        if (anim == null)
        {
            anim = GetComponentInChildren<Animator>();
        }

        if (anim != null && anim.runtimeAnimatorController != null)
        {
            float speed = agent.velocity.magnitude;
            anim.SetFloat("Speed", speed);

            if (speed > 0.1f)
                anim.speed = 1f;
            else
                anim.speed = 0f;
        }
    }
}
