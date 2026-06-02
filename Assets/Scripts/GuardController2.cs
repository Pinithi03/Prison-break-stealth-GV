using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Enhanced Guard 2 Controller — Student 4, GV Module (SE3062)
/// Uses UCS_Search for cost-optimal pathfinding (demonstrates both algorithms).
/// Same IS integration pattern as GuardController but uses UCS instead of BFS.
/// </summary>
public class GuardController2 : MonoBehaviour
{
    // ── Inspector References ──────────────────────────────────────────────
    [Header("IS Module Integration (UCS)")]
    [Tooltip("Assign the GraphReal script from your scene")]
    public GraphReal  graph;
    [Tooltip("Assign the UCS_Search script — Guard 2 uses UCS for optimal cost paths")]
    public UCS_Search ucsSearch;
    [Tooltip("Node IDs this guard patrols between")]
    public int[] patrolNodeIds = new int[] { 1, 3, 5, 7 };

    [Header("Agent")]
    public NavMeshAgent agent;
    public Animator     anim;

    [Header("Movement")]
    public float patrolSpeed     = 2.5f;
    public float suspiciousSpeed = 1.2f;
    public float arrivalDistance = 0.7f;

    [Header("Waypoint Sweep")]
    public float sweepAngle     = 75f;
    public float sweepSpeed     = 45f;
    public float sweepPauseTime = 0.5f;

    // ── Runtime state ─────────────────────────────────────────────────────
    private int       patrolTargetIndex = 0;
    private List<int> currentISPath     = new List<int>();
    private int       pathStepIndex     = 0;
    private bool      isSweeping        = false;
    private bool      isSuspicious      = false;
    private bool      usingISPath       = false;

    // ── Stats exposed for PathOverlay ─────────────────────────────────────
    [HideInInspector] public List<int> lastCalculatedPath = new List<int>();
    [HideInInspector] public int       lastPathLength     = 0;
    [HideInInspector] public float     lastPathCost       = 0f;
    [HideInInspector] public int       lastNodesExplored  = 0;

    // Fallback patrol
    private readonly List<Vector3> fallbackPath = new List<Vector3>
    {
        new Vector3(-6f, 0f,  3f),
        new Vector3(-8f, 0f,  8f),
        new Vector3(-6f, 0f,  8f),
        new Vector3(-4f, 0f,  3f),
    };
    private int fallbackIndex = 0;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (anim   == null) anim = GetComponentInChildren<Animator>();

        agent.speed = patrolSpeed;

        if (graph != null && ucsSearch != null && patrolNodeIds.Length >= 2)
        {
            usingISPath = true;
            ucsSearch.graph = graph;
            graph.OnEdgeChanged += HandleEdgeChanged;
            Debug.Log("[Guard2] IS path integration ACTIVE — using UCS navigation");
            RequestNextISPath();
        }
        else
        {
            usingISPath = false;
            Debug.LogWarning("[Guard2] No graph/UCS assigned — falling back to NavMesh patrol");
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

    // ── UCS Path patrol ───────────────────────────────────────────────────
    void UpdateISPatrol()
    {
        if (currentISPath.Count == 0 || agent.pathPending) return;

        if (agent.remainingDistance < arrivalDistance)
        {
            pathStepIndex++;
            if (pathStepIndex >= currentISPath.Count)
                StartCoroutine(SweepThenAdvance());
            else
                MoveToNode(currentISPath[pathStepIndex]);
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

        agent.isStopped = false;
        isSweeping = false;

        patrolTargetIndex = (patrolTargetIndex + 1) % patrolNodeIds.Length;
        RequestNextISPath();
    }

    void RequestNextISPath()
    {
        int currentNode = GetNearestNodeId(transform.position);
        int targetNode  = patrolNodeIds[patrolTargetIndex];

        if (currentNode == targetNode)
        {
            patrolTargetIndex = (patrolTargetIndex + 1) % patrolNodeIds.Length;
            targetNode = patrolNodeIds[patrolTargetIndex];
        }

        // Guard 2 uses UCS for cost-optimal paths
        List<int> path   = ucsSearch.FindPath(currentNode, targetNode);
        currentISPath    = path;
        lastCalculatedPath = new List<int>(path);
        lastPathLength   = path.Count;
        lastNodesExplored = ucsSearch.lastVisited.Count;
        lastPathCost     = ucsSearch.lastCostSoFar.ContainsKey(targetNode)
                           ? ucsSearch.lastCostSoFar[targetNode] : 0f;
        pathStepIndex    = 0;

        Debug.Log($"[Guard2] UCS path requested: Node {currentNode} → Node {targetNode}" +
                  $" | Steps: {path.Count}");

        if (currentISPath.Count > 0) MoveToNode(currentISPath[0]);
    }

    void MoveToNode(int nodeId)
    {
        if (!graph.nodePositions.ContainsKey(nodeId)) return;
        agent.SetDestination(graph.nodePositions[nodeId]);
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

    // ── Fallback ──────────────────────────────────────────────────────────
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

    public void SetSuspicious(bool suspicious) => isSuspicious = suspicious;

    /// <summary>Returns how many IS path nodes remain in current patrol segment.</summary>
    public int GetCurrentPathLength() => currentISPath.Count;

    /// <summary>Returns the node ID the guard is currently navigating towards.</summary>
    public int GetCurrentTargetNodeId()
    {
        if (currentISPath.Count == 0 || pathStepIndex >= currentISPath.Count) return -1;
        return currentISPath[pathStepIndex];
    }

    void OnDestroy()
    {
        if (graph != null)
        {
            graph.OnEdgeChanged -= HandleEdgeChanged;
        }
    }

    private void HandleEdgeChanged(int fromId, int toId, bool isBlocked)
    {
        if (usingISPath && !isSweeping && agent != null && agent.isOnNavMesh)
        {
            Debug.Log($"[Guard2] Path changed due to edge {fromId} <-> {toId} change. Recalculating path!");
            RequestNextISPath();
        }
    }
}
