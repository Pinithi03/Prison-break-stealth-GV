using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Toggleable Debug Visualizer — Student 4, IS Module (SE3062)
///
/// Press T at runtime to toggle the full debug overlay ON/OFF.
///
/// Draws LIVE mathematical data directly into the 3D Unity scene:
///   YELLOW spheres  = all graph nodes
///   YELLOW lines    = all graph edges (adjacency list)
///   CYAN  spheres   = BFS frontier (nodes currently in the queue)
///   RED   spheres   = visited/expanded nodes (closed set)
///   GREEN lines     = final calculated path (BFS or UCS result)
///   WHITE labels    = node IDs above each sphere
///   MAGENTA lines   = UCS frontier with cost weights
///
/// Works in BOTH Scene view (via Gizmos) and Play mode (via Debug.DrawLine).
/// </summary>
public class DebugVisualizer : MonoBehaviour
{
    // ── References ────────────────────────────────────────────────────────
    [Header("References")]
    public GraphReal  graph;
    public BFS_Search bfsSearch;
    public UCS_Search ucsSearch;

    // ── Toggle ────────────────────────────────────────────────────────────
    [Header("Toggle")]
    public bool showDebug = false;
    public KeyCode toggleKey = KeyCode.T;

    // ── Display Options ───────────────────────────────────────────────────
    [Header("Display Options")]
    public bool showGraphNodes    = true;
    public bool showGraphEdges    = true;
    public bool showBFSFrontier   = true;
    public bool showVisited       = true;
    public bool showFinalPath     = true;
    public bool showNodeLabels    = true;
    public bool showUCSCosts      = true;

    // ── Visual Settings ───────────────────────────────────────────────────
    [Header("Visual Settings")]
    public float nodeRadius        = 0.35f;
    public float pathLineHeight    = 0.15f;   // raise lines above floor

    // ── Colours ───────────────────────────────────────────────────────────
    [Header("Colours")]
    public Color graphNodeColor    = Color.yellow;
    public Color graphEdgeColor    = new Color(1f, 1f, 0f, 0.4f);
    public Color frontierColor     = Color.cyan;
    public Color visitedColor      = Color.red;
    public Color pathColor         = Color.green;
    public Color ucsFrontierColor  = new Color(1f, 0.4f, 1f, 1f);  // magenta

    // ─────────────────────────────────────────────────────────────────────
    void Update()
    {
        // Toggle on T key
        if (Input.GetKeyDown(toggleKey))
        {
            showDebug = !showDebug;
            Debug.Log($"[DebugVisualizer] Debug overlay {(showDebug ? "ON ✅" : "OFF ❌")} — Press T to toggle");
        }

        if (!showDebug || graph == null) return;

        // ── Draw in Play mode via Debug.DrawLine ──────────────────────────
        DrawGraphEdges_PlayMode();
        DrawBFSData_PlayMode();
        DrawUCSData_PlayMode();
        DrawFinalPath_PlayMode();
    }

    // ── Play-mode drawing (Debug.DrawLine — visible in Scene view during play)
    // ─────────────────────────────────────────────────────────────────────

    void DrawGraphEdges_PlayMode()
    {
        if (!showGraphEdges || graph.adjacencyList == null) return;

        foreach (var edge in graph.adjacencyList)
        {
            int fromId = edge.Key;
            if (!graph.nodePositions.ContainsKey(fromId)) continue;
            Vector3 fromPos = Raised(graph.nodePositions[fromId]);

            foreach (int toId in edge.Value)
            {
                if (!graph.nodePositions.ContainsKey(toId)) continue;
                Vector3 toPos = Raised(graph.nodePositions[toId]);
                Debug.DrawLine(fromPos, toPos, graphEdgeColor);
            }
        }
    }

    void DrawBFSData_PlayMode()
    {
        if (bfsSearch == null) return;

        // Frontier nodes (cyan)
        if (showBFSFrontier)
        {
            foreach (int nodeId in bfsSearch.lastFrontier)
            {
                DrawNodeLines(nodeId, frontierColor);
            }
        }

        // Visited nodes (red)
        if (showVisited)
        {
            foreach (int nodeId in bfsSearch.lastVisited)
            {
                DrawNodeLines(nodeId, visitedColor);
            }
        }
    }

    void DrawUCSData_PlayMode()
    {
        if (ucsSearch == null) return;

        // UCS frontier (magenta)
        if (showBFSFrontier)
        {
            foreach (int nodeId in ucsSearch.lastFrontier)
            {
                DrawNodeLines(nodeId, ucsFrontierColor);
            }
        }

        // UCS visited (red)
        if (showVisited)
        {
            foreach (int nodeId in ucsSearch.lastVisited)
            {
                DrawNodeLines(nodeId, visitedColor);
            }
        }
    }

    void DrawFinalPath_PlayMode()
    {
        if (!showFinalPath) return;

        // Draw BFS path
        DrawPathLines(bfsSearch?.lastPath, pathColor);

        // Draw UCS path (slightly raised to avoid overlap)
        if (ucsSearch != null && ucsSearch.lastPath != null && ucsSearch.lastPath.Count > 0)
        {
            List<int> ucsPath = ucsSearch.lastPath;
            for (int i = 0; i < ucsPath.Count - 1; i++)
            {
                if (!graph.nodePositions.ContainsKey(ucsPath[i])) continue;
                if (!graph.nodePositions.ContainsKey(ucsPath[i + 1])) continue;
                Vector3 a = Raised(graph.nodePositions[ucsPath[i]],   0.3f);
                Vector3 b = Raised(graph.nodePositions[ucsPath[i + 1]], 0.3f);
                Debug.DrawLine(a, b, new Color(0f, 1f, 0.5f, 1f)); // teal for UCS
            }
        }
    }

    void DrawPathLines(List<int> path, Color color)
    {
        if (path == null || path.Count < 2) return;
        for (int i = 0; i < path.Count - 1; i++)
        {
            if (!graph.nodePositions.ContainsKey(path[i]))     continue;
            if (!graph.nodePositions.ContainsKey(path[i + 1])) continue;
            Vector3 a = Raised(graph.nodePositions[path[i]]);
            Vector3 b = Raised(graph.nodePositions[path[i + 1]]);
            Debug.DrawLine(a, b, color);
        }
    }

    // Draw a small cross/star at a node position using DrawLine
    void DrawNodeLines(int nodeId, Color color)
    {
        if (!graph.nodePositions.ContainsKey(nodeId)) return;
        Vector3 centre = Raised(graph.nodePositions[nodeId]);
        float   r      = nodeRadius;
        Debug.DrawLine(centre + Vector3.left * r,    centre + Vector3.right   * r, color);
        Debug.DrawLine(centre + Vector3.forward * r, centre + Vector3.back    * r, color);
        Debug.DrawLine(centre + Vector3.up * r,      centre + Vector3.down    * r, color);
    }

    // ── Gizmos (Scene view, always visible even when not playing) ─────────
    // ─────────────────────────────────────────────────────────────────────

    void OnDrawGizmos()
    {
        if (!showDebug || graph == null) return;

        // ── Graph nodes ───────────────────────────────────────────────────
        if (showGraphNodes && graph.nodePositions != null)
        {
            foreach (var kvp in graph.nodePositions)
            {
                int     id  = kvp.Key;
                Vector3 pos = Raised(kvp.Value);

                // Colour by state
                Color c = graphNodeColor;
                if (bfsSearch != null && bfsSearch.lastVisited.Contains(id))  c = visitedColor;
                if (bfsSearch != null && bfsSearch.lastFrontier.Contains(id)) c = frontierColor;
                if (bfsSearch != null && bfsSearch.lastPath.Contains(id))     c = pathColor;

                Gizmos.color = c;
                Gizmos.DrawSphere(pos, nodeRadius);

                // Node ID label
#if UNITY_EDITOR
                if (showNodeLabels)
                {
                    UnityEditor.Handles.color = Color.white;
                    UnityEditor.Handles.Label(pos + Vector3.up * 0.5f, $"N{id}");
                }

                // UCS cost label
                if (showUCSCosts && ucsSearch != null &&
                    ucsSearch.lastCostSoFar.ContainsKey(id))
                {
                    float cost = ucsSearch.lastCostSoFar[id];
                    UnityEditor.Handles.color = new Color(1f, 0.8f, 0f, 1f);
                    UnityEditor.Handles.Label(pos + Vector3.up * 0.85f, $"g={cost:F1}");
                }
#endif
            }
        }

        // ── Graph edges ───────────────────────────────────────────────────
        if (showGraphEdges && graph.adjacencyList != null)
        {
            Gizmos.color = graphEdgeColor;
            foreach (var edge in graph.adjacencyList)
            {
                if (!graph.nodePositions.ContainsKey(edge.Key)) continue;
                Vector3 from = Raised(graph.nodePositions[edge.Key]);
                foreach (int to in edge.Value)
                {
                    if (!graph.nodePositions.ContainsKey(to)) continue;
                    Gizmos.DrawLine(from, Raised(graph.nodePositions[to]));
                }
            }
        }

        // ── BFS frontier ──────────────────────────────────────────────────
        if (showBFSFrontier && bfsSearch != null)
        {
            Gizmos.color = frontierColor;
            foreach (int id in bfsSearch.lastFrontier)
            {
                if (!graph.nodePositions.ContainsKey(id)) continue;
                Gizmos.DrawWireSphere(Raised(graph.nodePositions[id]), nodeRadius * 1.4f);
            }
        }

        // ── BFS visited ───────────────────────────────────────────────────
        if (showVisited && bfsSearch != null)
        {
            Gizmos.color = visitedColor;
            foreach (int id in bfsSearch.lastVisited)
            {
                if (!graph.nodePositions.ContainsKey(id)) continue;
                Gizmos.DrawWireSphere(Raised(graph.nodePositions[id]), nodeRadius * 1.1f);
            }
        }

        // ── UCS frontier (magenta) ────────────────────────────────────────
        if (showBFSFrontier && ucsSearch != null)
        {
            Gizmos.color = ucsFrontierColor;
            foreach (int id in ucsSearch.lastFrontier)
            {
                if (!graph.nodePositions.ContainsKey(id)) continue;
                Gizmos.DrawWireSphere(Raised(graph.nodePositions[id]), nodeRadius * 1.4f);
            }
        }

        // ── Final BFS path (thick green) ──────────────────────────────────
        if (showFinalPath && bfsSearch != null && bfsSearch.lastPath.Count > 1)
        {
            Gizmos.color = pathColor;
            for (int i = 0; i < bfsSearch.lastPath.Count - 1; i++)
            {
                int a = bfsSearch.lastPath[i], b = bfsSearch.lastPath[i + 1];
                if (!graph.nodePositions.ContainsKey(a)) continue;
                if (!graph.nodePositions.ContainsKey(b)) continue;
                Gizmos.DrawLine(Raised(graph.nodePositions[a]), Raised(graph.nodePositions[b]));
            }
        }

        // ── Final UCS path (teal) ─────────────────────────────────────────
        if (showFinalPath && ucsSearch != null && ucsSearch.lastPath.Count > 1)
        {
            Gizmos.color = new Color(0f, 1f, 0.5f, 1f);
            for (int i = 0; i < ucsSearch.lastPath.Count - 1; i++)
            {
                int a = ucsSearch.lastPath[i], b = ucsSearch.lastPath[i + 1];
                if (!graph.nodePositions.ContainsKey(a)) continue;
                if (!graph.nodePositions.ContainsKey(b)) continue;
                Gizmos.DrawLine(Raised(graph.nodePositions[a], 0.3f),
                                Raised(graph.nodePositions[b], 0.3f));
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────
    Vector3 Raised(Vector3 pos, float extra = 0f)
        => new Vector3(pos.x, pos.y + pathLineHeight + extra, pos.z);
}
