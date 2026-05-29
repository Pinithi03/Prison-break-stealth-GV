using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GraphReal : MonoBehaviour
{
    public Dictionary<int, Vector3> nodePositions = new Dictionary<int, Vector3>();
    public Dictionary<int, List<int>> adjacencyList = new Dictionary<int, List<int>>();
    public Dictionary<int, Dictionary<int, float>> weightedAdjacencyList =
        new Dictionary<int, Dictionary<int, float>>();

    private GraphNode[] graphNodes;

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

        // Try near exact node position first
        if (NavMesh.SamplePosition(originalPosition, out hit, 1f, NavMesh.AllAreas))
        {
            return hit.position;
        }

        // Try from above using same X and Z
        Vector3 positionAbove = new Vector3(originalPosition.x, originalPosition.y + 10f, originalPosition.z);

        if (NavMesh.SamplePosition(positionAbove, out hit, 20f, NavMesh.AllAreas))
        {
            return hit.position;
        }

        // Fallback: keep scene node position so graph still works
        Debug.LogWarning("NavMesh not found near node position. Using scene position: " + originalPosition);
        return originalPosition;
    }

    private void AddEdge(GraphNode fromNode, GraphNode toNode)
    {
        int fromId = fromNode.nodeId;
        int toId = toNode.nodeId;

        if (!nodePositions.ContainsKey(fromId) || !nodePositions.ContainsKey(toId))
        {
            Debug.LogWarning("Cannot create edge. Node missing: " + fromNode.nodeName + " -> " + toNode.nodeName);
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

        if (pathFound && path.status == NavMeshPathStatus.PathComplete && path.corners.Length > 1)
        {
            cost = CalculatePathCost(path);
        }
        else
        {
            // Fallback cost if NavMesh path is not available
            cost = Vector3.Distance(fromPosition, toPosition);
            Debug.LogWarning("NavMesh path unavailable. Using straight distance for edge: "
                + fromNode.nodeName + " -> " + toNode.nodeName);
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

        Debug.Log("Blocked edge: " + fromId + " -> " + toId);
    }

    public void UnblockEdge(int fromId, int toId)
    {
        GraphNode fromNode = FindNodeById(fromId);
        GraphNode toNode = FindNodeById(toId);

        if (fromNode == null || toNode == null)
        {
            Debug.LogWarning("Cannot unblock edge. Node not found.");
            return;
        }

        AddEdge(fromNode, toNode);
        Debug.Log("Unblocked edge: " + fromId + " -> " + toId);
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

        Gizmos.color = Color.yellow;

        foreach (var node in nodePositions)
        {
            Gizmos.DrawSphere(node.Value, 0.3f);
        }

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