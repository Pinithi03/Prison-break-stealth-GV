using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Central navigation graph for Prison Break: Silent Escape.
/// Extracted from Unity scene GraphNode objects with NavMesh-snapped positions.
///
/// Student 2 — IS integration points:
///   BlockEdge(fromId, toId)   — sever an edge (door closed / barricade placed)
///   UnblockEdge(fromId, toId) — restore an edge (door opened / barricade removed)
///   OnEdgeChanged              — event fired after every block/unblock (guards subscribe here
///                                to trigger A* recalculation without polling every frame)
/// </summary>
public class GraphReal : MonoBehaviour
{
    // Node ID -> world/NavMesh position
    public Dictionary<int, Vector3> nodePositions = new Dictionary<int, Vector3>();

    // Node ID -> connected neighbour node IDs
    public Dictionary<int, List<int>> adjacencyList = new Dictionary<int, List<int>>();

    // Node ID -> neighbour ID -> movement cost
    public Dictionary<int, Dictionary<int, float>> weightedAdjacencyList =
        new Dictionary<int, Dictionary<int, float>>();

    private GraphNode[] graphNodes;

    // ── Student 2 IS: OnEdgeChanged event ─────────────────────────────────────
    // Fires whenever an edge is blocked or restored.
    // Parameters: (int fromId, int toId, bool isBlocked)
    //   isBlocked = true  → edge was just severed   (door closed / barricade placed)
    //   isBlocked = false → edge was just restored  (door opened / barricade removed)
    // Guards subscribe to this event so A* recalculates exactly once per state change,
    // NOT every frame — preventing infinite recalculation loops.
    public event System.Action<int, int, bool> OnEdgeChanged;

    // Re-entrancy guard: prevents BlockEdge/UnblockEdge from firing OnEdgeChanged
    // while a subscriber is already mid-recalculation.
    private bool _isProcessingEdgeChange = false;

    [Header("Debug Settings")]
    public bool showFallbackWarnings = false;

    void Awake()
    {
        BuildGraphFromSceneNodes();
        PrintGraph();
    }

    public void BuildGraphFromSceneNodes()
    {
        nodePositions.Clear();
        adjacencyList.Clear();
        weightedAdjacencyList.Clear();

        graphNodes = FindObjectsOfType<GraphNode>();

        // Step 1: Read all GraphNode objects from Unity scene
        foreach (GraphNode node in graphNodes)
        {
            if (nodePositions.ContainsKey(node.nodeId))
            {
                Debug.LogError("Duplicate node ID found: " + node.nodeId + " on " + node.name);
                continue;
            }

            Vector3 finalPosition = GetBestNodePosition(node.transform.position);

            nodePositions.Add(node.nodeId, finalPosition);
            adjacencyList.Add(node.nodeId, new List<int>());
            weightedAdjacencyList.Add(node.nodeId, new Dictionary<int, float>());
        }

        // Step 2: Build edges from connectedNodes list
        foreach (GraphNode node in graphNodes)
        {
            foreach (GraphNode connectedNode in node.connectedNodes)
            {
                if (connectedNode == null)
                {
                    Debug.LogWarning("Missing connected node in: " + node.nodeName);
                    continue;
                }

                AddEdge(node, connectedNode);
            }
        }

        Debug.Log("Real prison graph extracted from scene nodes with NavMesh support");
    }

    private Vector3 GetBestNodePosition(Vector3 originalPosition)
    {
        NavMeshHit hit;

        // Try exact node area first
        if (NavMesh.SamplePosition(originalPosition, out hit, 1f, NavMesh.AllAreas))
        {
            return hit.position;
        }

        // If Y level is wrong, search from above using same X and Z
        Vector3 positionAbove = new Vector3(
            originalPosition.x,
            originalPosition.y + 10f,
            originalPosition.z
        );

        if (NavMesh.SamplePosition(positionAbove, out hit, 20f, NavMesh.AllAreas))
        {
            return hit.position;
        }

        // Fallback: use scene object position
        if (showFallbackWarnings)
        {
            Debug.LogWarning("NavMesh not found near node position. Using scene position: " + originalPosition);
        }

        return originalPosition;
    }

    private void AddEdge(GraphNode fromNode, GraphNode toNode)
    {
        int fromId = fromNode.nodeId;
        int toId = toNode.nodeId;

        if (!nodePositions.ContainsKey(fromId) || !nodePositions.ContainsKey(toId))
        {
            Debug.LogWarning("Cannot create edge. Node missing: "
                + fromNode.nodeName + " -> " + toNode.nodeName);
            return;
        }

        Vector3 fromPosition = nodePositions[fromId];
        Vector3 toPosition = nodePositions[toId];

        NavMeshPath path = new NavMeshPath();

        bool pathFound = NavMesh.CalculatePath(
            fromPosition,
            toPosition,
            NavMesh.AllAreas,
            path
        );

        float cost;

        // Use NavMesh distance if possible
        if (pathFound && path.status == NavMeshPathStatus.PathComplete && path.corners.Length > 1)
        {
            cost = CalculatePathCost(path);
        }
        else
        {
            // Door/collider may block NavMesh. Use straight-line fallback cost.
            cost = Vector3.Distance(fromPosition, toPosition);

            if (showFallbackWarnings)
            {
                Debug.LogWarning("NavMesh path unavailable. Using straight distance for edge: "
                    + fromNode.nodeName + " -> " + toNode.nodeName);
            }
        }

        if (!adjacencyList[fromId].Contains(toId))
        {
            adjacencyList[fromId].Add(toId);
        }

        weightedAdjacencyList[fromId][toId] = cost;
    }

    private float CalculatePathCost(NavMeshPath path)
    {
        float totalCost = 0f;

        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            totalCost += Vector3.Distance(path.corners[i], path.corners[i + 1]);
        }

        return totalCost;
    }

    public void PrintGraph()
    {
        Debug.Log("========== REAL PRISON GRAPH ==========");

        foreach (var node in adjacencyList)
        {
            string output = "Node " + node.Key + " -> ";

            foreach (int neighbour in node.Value)
            {
                float cost = weightedAdjacencyList[node.Key][neighbour];
                output += neighbour + " (cost: " + cost.ToString("F2") + ") ";
            }

            Debug.Log(output);
        }

        Debug.Log("=======================================");
    }

    // ── Student 2 IS ──────────────────────────────────────────────────────────
    // Call when a door closes or a barricade is placed — severs the edge so
    // the guard AI can no longer route through it.
    public void BlockEdge(int fromId, int toId)
    {
        if (adjacencyList.ContainsKey(fromId))
        {
            adjacencyList[fromId].Remove(toId);
        }

        if (weightedAdjacencyList.ContainsKey(fromId))
        {
            weightedAdjacencyList[fromId].Remove(toId);
        }

        Debug.Log($"[GraphReal] Blocked edge: {fromId} -> {toId}");

        // Fire OnEdgeChanged once — guard AI recalculates path.
        // The re-entrancy guard prevents a subscriber from accidentally
        // calling BlockEdge again mid-handler (infinite loop protection).
        if (!_isProcessingEdgeChange)
        {
            _isProcessingEdgeChange = true;
            OnEdgeChanged?.Invoke(fromId, toId, true);
            _isProcessingEdgeChange = false;
        }
    }

    // ── Student 2 IS ──────────────────────────────────────────────────────────
    // Call when a door opens or a barricade is removed — restores the edge
    // and notifies guard AI to recalculate with the new route available.
    public void UnblockEdge(int fromId, int toId)
    {
        GraphNode fromNode = FindNodeById(fromId);
        GraphNode toNode = FindNodeById(toId);

        if (fromNode == null || toNode == null)
        {
            Debug.LogWarning("[GraphReal] Cannot unblock edge — node not found.");
            return;
        }

        AddEdge(fromNode, toNode);
        Debug.Log($"[GraphReal] Unblocked edge: {fromId} -> {toId}");

        // Fire OnEdgeChanged once — guard AI recalculates path.
        if (!_isProcessingEdgeChange)
        {
            _isProcessingEdgeChange = true;
            OnEdgeChanged?.Invoke(fromId, toId, false);
            _isProcessingEdgeChange = false;
        }
    }

    private GraphNode FindNodeById(int nodeId)
    {
        foreach (GraphNode node in graphNodes)
        {
            if (node.nodeId == nodeId)
            {
                return node;
            }
        }

        return null;
    }

    void OnDrawGizmos()
    {
        if (nodePositions == null || adjacencyList == null)
        {
            return;
        }

        // Yellow spheres = graph nodes
        Gizmos.color = Color.yellow;

        foreach (var node in nodePositions)
        {
            Gizmos.DrawSphere(node.Value, 0.3f);
        }

        // Green lines = graph edges
        Gizmos.color = Color.green;

        foreach (var edge in adjacencyList)
        {
            int fromNode = edge.Key;

            foreach (int toNode in edge.Value)
            {
                if (nodePositions.ContainsKey(fromNode) && nodePositions.ContainsKey(toNode))
                {
                    Gizmos.DrawLine(nodePositions[fromNode], nodePositions[toNode]);
                }
            }
        }
    }
}