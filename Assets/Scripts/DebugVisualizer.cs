using UnityEngine;

public class DebugVisualizer : MonoBehaviour
{
    public bool showDebug = true;
    public GraphReal graph;

    public Color nodeColor = Color.blue;
    public Color edgeColor = Color.yellow;
    public float nodeRadius = 0.3f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            showDebug = !showDebug;
            Debug.Log("Debug Visualizer is now: " + (showDebug ? "ON" : "OFF"));
        }
    }

    void OnDrawGizmos()
    {
        if (!showDebug || graph == null) return;

        Gizmos.color = nodeColor;
        foreach (var node in graph.nodePositions)
        {
            Gizmos.DrawSphere(node.Value, nodeRadius);
        }

        Gizmos.color = edgeColor;
        foreach (var edge in graph.adjacencyList)
        {
            int fromNode = edge.Key;
            if (!graph.nodePositions.ContainsKey(fromNode)) continue;
            Vector3 fromPos = graph.nodePositions[fromNode];

            foreach (int toNode in edge.Value)
            {
                if (!graph.nodePositions.ContainsKey(toNode)) continue;
                Vector3 toPos = graph.nodePositions[toNode];
                Gizmos.DrawLine(fromPos, toPos);
            }
        }
    }
}
