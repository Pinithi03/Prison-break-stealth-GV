using System.Collections.Generic;
using UnityEngine;

public class PathVisualizer : MonoBehaviour
{
    public GraphReal graph;
    public List<int> path = new List<int>();
    public Color lineColor = Color.red;

    void OnDrawGizmos()
    {
        if (graph == null || path == null || path.Count < 2) return;
        Gizmos.color = lineColor;
        for (int i = 0; i < path.Count - 1; i++)
        {
            if (!graph.nodePositions.ContainsKey(path[i]) || !graph.nodePositions.ContainsKey(path[i + 1])) continue;
            Gizmos.DrawLine(graph.nodePositions[path[i]], graph.nodePositions[path[i + 1]]);
            Gizmos.DrawSphere(graph.nodePositions[path[i]], 0.1f);
        }
        if (graph.nodePositions.ContainsKey(path[path.Count - 1]))
            Gizmos.DrawSphere(graph.nodePositions[path[path.Count - 1]], 0.12f);
    }
}
