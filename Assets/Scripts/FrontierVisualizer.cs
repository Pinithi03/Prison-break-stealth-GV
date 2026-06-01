using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Draws the search frontier and explored nodes for debugging.
/// Reads data from SearchTester references (BFS/UCS/A*) and renders Gizmos.
/// </summary>
public class FrontierVisualizer : MonoBehaviour
{
    public SearchTester tester;
    public Color frontierColor = Color.magenta;
    public Color exploredColor = Color.cyan;
    public float nodeRadius = 0.15f;
    public bool showFrontier = true;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            showFrontier = !showFrontier;
            Debug.Log("Frontier Visualizer is now: " + (showFrontier ? "ON" : "OFF"));
        }
    }

    void OnDrawGizmos()
    {
        if (!showFrontier) return;
        if (tester == null) tester = FindObjectOfType<SearchTester>();
        if (tester == null) return;

        GraphReal graph = tester.graph;
        if (graph == null) return;

        // Draw for BFS
        if (tester.bfs != null)
        {
            Gizmos.color = exploredColor;
            foreach (int id in tester.bfs.lastExplored)
            {
                if (graph.nodePositions.ContainsKey(id)) Gizmos.DrawSphere(graph.nodePositions[id], nodeRadius);
            }

            Gizmos.color = frontierColor;
            foreach (int id in tester.bfs.lastFrontier)
            {
                if (graph.nodePositions.ContainsKey(id)) Gizmos.DrawWireSphere(graph.nodePositions[id], nodeRadius * 1.2f);
            }
        }

        // Draw for UCS
        if (tester.ucs != null)
        {
            Gizmos.color = exploredColor * 0.8f;
            foreach (int id in tester.ucs.lastExplored)
            {
                if (graph.nodePositions.ContainsKey(id)) Gizmos.DrawSphere(graph.nodePositions[id], nodeRadius * 0.9f);
            }

            Gizmos.color = frontierColor * 0.8f;
            foreach (int id in tester.ucs.lastFrontier)
            {
                if (graph.nodePositions.ContainsKey(id)) Gizmos.DrawWireSphere(graph.nodePositions[id], nodeRadius);
            }
        }

        // Draw for A*
        if (tester.aStar != null)
        {
            Gizmos.color = exploredColor * 0.6f;
            foreach (int id in tester.aStar.lastExplored)
            {
                if (graph.nodePositions.ContainsKey(id)) Gizmos.DrawSphere(graph.nodePositions[id], nodeRadius * 0.7f);
            }

            Gizmos.color = frontierColor * 0.6f;
            foreach (int id in tester.aStar.lastFrontier)
            {
                if (graph.nodePositions.ContainsKey(id)) Gizmos.DrawWireSphere(graph.nodePositions[id], nodeRadius * 0.9f);
            }
        }
    }
}
