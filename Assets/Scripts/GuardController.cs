using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Enhanced Guard Controller with runtime algorithm switching.
/// Supports BFS, UCS, and A* — switchable live via AlgorithmSwitcherUI.
/// Student 4, GV Module (SE3032)
/// </summary>
public class GuardController : MonoBehaviour
{
    // ── Algorithm Selection ───────────────────────────────────────────────
    public enum SearchAlgorithm { BFS, UCS, AStar }

    [Header("IS Module Integration")]
    public GraphReal      graph;
    public BFS_Search     bfsSearch;
    public UCS_Search     ucsSearch;
    public AStar_Search   aStarSearch;
    public SearchAlgorithm activeAlgorithm = SearchAlgorithm.BFS;

    [Tooltip("Node IDs this guard patrols between")]
    public int[] patrolNodeIds = new int[] { 0, 1, 2, 7 };

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

    // ── Stats exposed for UI ──────────────────────────────────────────────
    [HideInInspector] public int   lastPathLength    = 0;
    [HideInInspector] public float lastPathCost      = 0f;
    [HideInInspector] public int   lastNodesExplored = 0;
    [HideInInspector] public List<int> lastCalculatedPath = new List<int>();

    // Fallback
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

        bool hasGraph  = graph != null && patrolNodeIds.Length >= 2;
        bool hasSearch = bfsSearch != null || ucsSearch != null || aStarSearch != null;

        if (hasGraph && hasSearch)
        {
            usingISPath = true;
            SetGraphOnSearchers();
            graph.OnEdgeChanged += HandleEdgeChanged;
            Debug.Log($"[Guard1] IS path integration ACTIVE — Algorithm: {activeAlgorithm}");
            RequestNextISPath();
        }
        else
        {
            usingISPath = false;
            Debug.LogWarning("[Guard1] No graph/search assigned — falling back to NavMesh patrol");
            if (agent.isOnNavMesh) agent.SetDestination(fallbackPath[0]);
        }
    }

    void SetGraphOnSearchers()
    {
        if (bfsSearch   != null) bfsSearch.graph   = graph;
        if (ucsSearch   != null) ucsSearch.graph   = graph;
        if (aStarSearch != null) aStarSearch.graph = graph;
    }

    // ── Runtime algorithm switch ──────────────────────────────────────────
    /// <summary>Switch algorithm live and immediately recalculate path.</summary>
    public void SwitchAlgorithm(SearchAlgorithm newAlgorithm)
    {
        activeAlgorithm = newAlgorithm;
        Debug.Log($"[Guard1] Algorithm switched to: {newAlgorithm}");

        if (usingISPath && !isSweeping)
        {
            // Recalculate path immediately with new algorithm
            StopAllCoroutines();
            isSweeping = false;
            agent.isStopped = false;
            RequestNextISPath();
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

        Quaternion start = transform.rotation;
        yield return RotateTo(start * Quaternion.Euler(0f, -sweepAngle, 0f));
        yield return new WaitForSeconds(sweepPauseTime);
        yield return RotateTo(start * Quaternion.Euler(0f,  sweepAngle, 0f));
        yield return new WaitForSeconds(sweepPauseTime);
        yield return RotateTo(start);

        agent.isStopped = false;
        isSweeping = false;

        patrolTargetIndex = (patrolTargetIndex + 1) % patrolNodeIds.Length;
        RequestNextISPath();
    }

    public void RequestNextISPath()
    {
        int currentNode = GetNearestNodeId(transform.position);
        int targetNode  = patrolNodeIds[patrolTargetIndex];
        if (currentNode == targetNode)
        {
            patrolTargetIndex = (patrolTargetIndex + 1) % patrolNodeIds.Length;
            targetNode = patrolNodeIds[patrolTargetIndex];
        }

        // ── Select algorithm ──────────────────────────────────────────────
        List<int> path = new List<int>();

        switch (activeAlgorithm)
        {
            case SearchAlgorithm.BFS:
                if (bfsSearch != null)
                {
                    path = bfsSearch.FindPath(currentNode, targetNode);
                    lastNodesExplored = bfsSearch.lastVisited.Count;
                    lastPathCost      = 0f; // BFS has no cost metric
                }
                break;

            case SearchAlgorithm.UCS:
                if (ucsSearch != null)
                {
                    path = ucsSearch.FindPath(currentNode, targetNode);
                    lastNodesExplored = ucsSearch.lastVisited.Count;
                    lastPathCost      = ucsSearch.lastCostSoFar.ContainsKey(targetNode)
                                        ? ucsSearch.lastCostSoFar[targetNode] : 0f;
                }
                break;

            case SearchAlgorithm.AStar:
                if (aStarSearch != null)
                {
                    path = aStarSearch.FindPath(currentNode, targetNode);
                    lastNodesExplored = aStarSearch.lastExplored.Count;
                    lastPathCost      = 0f;
                }
                break;
        }

        currentISPath         = path;
        lastCalculatedPath    = new List<int>(path);
        lastPathLength        = path.Count;
        pathStepIndex         = 0;

        Debug.Log($"[Guard1] {activeAlgorithm} path: Node {currentNode}→{targetNode} | " +
                  $"Steps:{path.Count} | Explored:{lastNodesExplored}");

        if (currentISPath.Count > 0) MoveToNode(currentISPath[0]);
    }

    void MoveToNode(int nodeId)
    {
        if (!graph.nodePositions.ContainsKey(nodeId)) return;
        agent.SetDestination(graph.nodePositions[nodeId]);
    }

    int GetNearestNodeId(Vector3 pos)
    {
        int bestId = -1; float bestDist = float.MaxValue;
        foreach (var kvp in graph.nodePositions)
        {
            float d = Vector3.Distance(pos, kvp.Value);
            if (d < bestDist) { bestDist = d; bestId = kvp.Key; }
        }
        return bestId;
    }

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
    public int  GetCurrentPathLength()    => currentISPath.Count;
    public int  GetCurrentTargetNodeId()  =>
        (currentISPath.Count == 0 || pathStepIndex >= currentISPath.Count)
        ? -1 : currentISPath[pathStepIndex];

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
            Debug.Log($"[Guard1] Path changed due to edge {fromId} <-> {toId} change. Recalculating path!");
            RequestNextISPath();
        }
    }
}
