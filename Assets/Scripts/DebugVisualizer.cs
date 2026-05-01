using UnityEngine;

public class DebugVisualizer : MonoBehaviour
{
    public bool showDebug = true;
    public GraphDummy graph;
    
    public Color nodeColor = Color.blue;
    public Color edgeColor = Color.yellow;
    public float nodeRadius = 0.3f;

    void Update()
    {
        // Toggle debug view when pressing 'T'
        if (Input.GetKeyDown(KeyCode.T))
        {
            showDebug = !showDebug;
            Debug.Log("Debug Visualizer is now: " + (showDebug ? "ON" : "OFF"));
        }
    }

    void OnDrawGizmos()
    {
        if (!showDebug || graph == null) return;

        // Draw Nodes
        Gizmos.color = nodeColor;
        foreach (var node in graph.nodePositions)
        {
            Gizmos.DrawSphere(node.Value, nodeRadius);
        }

        // Draw Edges
        Gizmos.color = edgeColor;
        foreach (var edge in graph.adjacencyList)
        {
            int fromNode = edge.Key;
            Vector3 fromPos = graph.nodePositions[fromNode];

            foreach (int toNode in edge.Value)
            {
                Vector3 toPos = graph.nodePositions[toNode];
                Gizmos.DrawLine(fromPos, toPos);
            }
        }
    }
}
