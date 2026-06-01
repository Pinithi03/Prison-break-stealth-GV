using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Enhanced Guard Controller — Student 4, GV Module (SE3032)
///
/// Translates mathematical path arrays (from IS BFS/UCS) into smooth
/// 3D character movement, rotations, and animations.
///
/// HOW IT WORKS:
///   1. Guard has a list of patrol target node IDs (set in Inspector)
///   2. On startup, calls BFS_Search.FindPath() to find path to first target
///   3. Walks each node in the returned path using NavMeshAgent
///   4. At each waypoint, performs a look-around sweep
///   5. When patrol node is reached, requests next BFS path to next patrol node
///   6. Repeats indefinitely
///
/// Falls back to NavMesh-only patrol if no graph/BFS is assigned.
/// </summary>
public class GuardController : MonoBehaviour
{
    // ── Inspector References ──────────────────────────────────────────────
    [Header("IS Module Integration")]
    [Tooltip("Assign the GraphReal script from your scene")]
    public GraphReal  graph;
    [Tooltip("Assign the BFS_Search script from your scene")]
    public BFS_Search bfsSearch;
    [Tooltip("Node IDs this guard patrols between (must match GraphNode IDs in scene)")]
    public int[] patrolNodeIds = new int[] { 0, 2, 4, 6 };

    [Header("Agent")]
    public NavMeshAgent agent;
    public Animator     anim;

    [Header("Movement")]
    public float patrolSpeed     = 2.5f;
    public float suspiciousSpeed = 1.2f;
    public float arrivalDistance = 0.7f;

    [Header("Waypoint Sweep")]
    public float sweepAngle    = 75f;
    public float sweepSpeed    = 45f;
    public float sweepPauseTime = 0.5f;

    // ── Runtime state ─────────────────────────────────────────────────────
    private int           patrolTargetIndex = 0;   // index into patrolNodeIds
    private List<int>     currentISPath     = new List<int>(); // node ID path from BFS
    private int           pathStepIndex     = 0;   // step along currentISPath
    private bool          isSweeping        = false;
    private bool          isSuspicious      = false;
    private bool          usingISPath       = false; // true = IS path, false = fallback

    // Fallback hardcoded patrol (used if no graph/BFS assigned)
    private readonly List<Vector3> fallbackPath = new List<Vector3>
    {
        new Vector3(-13f, 0f, -8f),
        new Vector3( -9f, 0f, -8f),
        new Vector3( -9f, 0f, -4f),
        new Vector3(-13f, 0f, -4f),
    };
    private int fallbackIndex = 0;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (anim   == null) anim = GetComponentInChildren<Animator>();

        agent.speed = patrolSpeed;

        if (graph != null && bfsSearch != null && patrolNodeIds.Length >= 2)
        {
            usingISPath = true;
            bfsSearch.graph = graph;
            Debug.Log("[Guard1] IS path integration ACTIVE — using BFS navigation");
            RequestNextISPath();
        }
        else
        {
            usingISPath = false;
            Debug.LogWarning("[Guard1] No graph/BFS assigned — falling back to NavMesh patrol");
            if (agent.isOnNavMesh) agent.SetDestination(fallbackPath[0]);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (!agent.isOnNavMesh || isSweeping) return;

        agent.speed = isSuspicious ? suspiciousSpeed : patrolSpeed;

        if (usingISPath)
            UpdateISPatrol();
        else
            UpdateFallbackPatrol();

        UpdateAnimation();
    }

    // ── IS Path patrol ────────────────────────────────────────────────────
    void UpdateISPatrol()
    {
        if (currentISPath.Count == 0) return;
        if (agent.pathPending) return;

        if (agent.remainingDistance < arrivalDistance)
        {
            pathStepIndex++;

            if (pathStepIndex >= currentISPath.Count)
            {
                // Reached end of current IS path → sweep then request next
                StartCoroutine(SweepThenAdvance());
            }
            else
            {
                // Move to next node in IS path
                MoveToNode(currentISPath[pathStepIndex]);
            }
        }
    }

    IEnumerator SweepThenAdvance()
    {
        isSweeping = true;
        agent.isStopped = true;

        yield return RotateTo(transform.rotation * Quaternion.Euler(0f, -sweepAngle, 0f));
        yield return new WaitForSeconds(sweepPauseTime);
        yield return RotateTo(transform.rotation * Quaternion.Euler(0f,  sweepAngle * 2f, 0f));
        yield return new WaitForSeconds(sweepPauseTime);
        yield return RotateTo(transform.rotation * Quaternion.Euler(0f, -sweepAngle, 0f));
        yield return new WaitForSeconds(sweepPauseTime * 0.5f);

        agent.isStopped = false;
        isSweeping = false;

        // Advance patrol target
        patrolTargetIndex = (patrolTargetIndex + 1) % patrolNodeIds.Length;
        RequestNextISPath();
    }

    void RequestNextISPath()
    {
        // Find which node the guard is currently closest to
        int currentNode = GetNearestNodeId(transform.position);
        int targetNode  = patrolNodeIds[patrolTargetIndex];

        if (currentNode == targetNode)
        {
            patrolTargetIndex = (patrolTargetIndex + 1) % patrolNodeIds.Length;
            targetNode = patrolNodeIds[patrolTargetIndex];
        }

        // Ask BFS for the optimal path
        currentISPath  = bfsSearch.FindPath(currentNode, targetNode);
        pathStepIndex  = 0;

        Debug.Log($"[Guard1] BFS path requested: Node {currentNode} → Node {targetNode}" +
                  $" | Steps: {currentISPath.Count}");

        if (currentISPath.Count > 0)
            MoveToNode(currentISPath[0]);
    }

    void MoveToNode(int nodeId)
    {
        if (!graph.nodePositions.ContainsKey(nodeId)) return;
        Vector3 worldPos = graph.nodePositions[nodeId];
        agent.SetDestination(worldPos);
    }

    int GetNearestNodeId(Vector3 position)
    {
        int   bestId   = -1;
        float bestDist = float.MaxValue;

        foreach (var kvp in graph.nodePositions)
        {
            float d = Vector3.Distance(position, kvp.Value);
            if (d < bestDist) { bestDist = d; bestId = kvp.Key; }
        }
        return bestId;
    }

    // ── Fallback patrol (NavMesh only, no graph) ──────────────────────────
    void UpdateFallbackPatrol()
    {
        if (fallbackPath.Count == 0) return;
        if (agent.remainingDistance < arrivalDistance && !agent.pathPending)
        {
            fallbackIndex = (fallbackIndex + 1) % fallbackPath.Count;
            agent.SetDestination(fallbackPath[fallbackIndex]);
        }
        SmoothRotation();
    }

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator RotateTo(Quaternion target)
    {
        while (Quaternion.Angle(transform.rotation, target) > 1f)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, target, sweepSpeed * Time.deltaTime);
            yield return null;
        }
        transform.rotation = target;
    }

    void SmoothRotation()
    {
        if (agent.velocity.sqrMagnitude > Mathf.Epsilon)
        {
            Quaternion lr = Quaternion.LookRotation(agent.velocity.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, lr, Time.deltaTime * 5f);
        }
    }

    void UpdateAnimation()
    {
        if (anim == null) anim = GetComponentInChildren<Animator>();
        if (anim == null || anim.runtimeAnimatorController == null) return;
        float speed = agent.velocity.magnitude;
        anim.SetFloat("Speed", speed);
        anim.speed = speed > 0.1f ? 1f : 0f;
    }

    /// <summary>Called by GuardDetection when player is spotted.</summary>
    public void SetSuspicious(bool suspicious) => isSuspicious = suspicious;

    /// <summary>Returns how many IS path nodes remain in current patrol segment.</summary>
    public int GetCurrentPathLength() => currentISPath.Count;

    /// <summary>Returns the node ID the guard is currently navigating towards.</summary>
    public int GetCurrentTargetNodeId()
    {
        if (currentISPath.Count == 0 || pathStepIndex >= currentISPath.Count) return -1;
        return currentISPath[pathStepIndex];
    }
}
