using System.Collections.Generic;
using UnityEngine;

public class GraphReal : MonoBehaviour
{
    public Dictionary<int, Vector3> nodePositions = new Dictionary<int, Vector3>();
    public Dictionary<int, List<int>> adjacencyList = new Dictionary<int, List<int>>();
    public Dictionary<int, Dictionary<int, float>> weightedAdjacencyList =
        new Dictionary<int, Dictionary<int, float>>();

    void Awake()
    {
        BuildGraph();
        PrintGraph();
    }

    void BuildGraph()
    {
        nodePositions.Clear();
        adjacencyList.Clear();
        weightedAdjacencyList.Clear();

        // REAL prison nodes
        // Later you can edit these positions to match your Unity scene exactly

        AddNode(0, new Vector3(-5.98f, 0.65f, 5.13f));    // Cell
        AddNode(1, new Vector3(-4f,0.65f, 5.13f));    // Cell Door
        AddNode(2, new Vector3(1.91f, 0.65f, 5.13f));    //  Main Corridor
        AddNode(3, new Vector3(2.99f,0.65f,-6.69f));   // Security Door
        AddNode(4, new Vector3(12, 0, 5));   // Security Room
        AddNode(5, new Vector3(16, 0, 2));   // Exit Gate

        // Edges
        AddEdge(0, 1);

        AddEdge(1, 0);
        AddEdge(1, 2);

        AddEdge(2, 1);
        AddEdge(2, 3);
        AddEdge(2, 5);

        AddEdge(3, 2);
        AddEdge(3, 4);

        AddEdge(4, 3);

        AddEdge(5, 2);

        Debug.Log("Real Prison Graph Loaded");
    }

    void AddNode(int id, Vector3 position)
    {
        nodePositions.Add(id, position);
        adjacencyList.Add(id, new List<int>());
        weightedAdjacencyList.Add(id, new Dictionary<int, float>());
    }

    void AddEdge(int fromNode, int toNode)
    {
        if (!nodePositions.ContainsKey(fromNode) || !nodePositions.ContainsKey(toNode))
        {
            Debug.LogError("Cannot create edge. Node ID missing.");
            return;
        }

        adjacencyList[fromNode].Add(toNode);

        float cost = Vector3.Distance(nodePositions[fromNode], nodePositions[toNode]);
        weightedAdjacencyList[fromNode].Add(toNode, cost);
    }

    void PrintGraph()
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
    void OnDrawGizmos()
{
    if (nodePositions == null || adjacencyList == null)
    {
        return;
    }

    // Draw nodes
    Gizmos.color = Color.yellow;

    foreach (var node in nodePositions)
    {
        Gizmos.DrawSphere(node.Value, 0.3f);
    }

    // Draw edges
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