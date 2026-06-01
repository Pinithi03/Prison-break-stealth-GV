using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Breadth-First Search on the prison graph.
/// Student 4 - IS Module (SE3062)
/// 
/// BFS guarantees the SHORTEST PATH in terms of number of edges (hops).
/// Time Complexity:  O(V + E)  where V = nodes, E = edges
/// Space Complexity: O(V)      for the queue and visited set
/// Data Structure:   Queue<int> (FIFO) — ensures level-by-level expansion
/// </summary>
public class BFS_Search : MonoBehaviour
{
    public GraphReal graph;

    // ── Exposed state for DebugVisualizer ─────────────────────────────────
    [HideInInspector] public List<int> lastFrontier = new List<int>();  // nodes in queue
    [HideInInspector] public List<int> lastVisited  = new List<int>();  // explored nodes
    [HideInInspector] public List<int> lastPath     = new List<int>();  // final result

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Finds shortest-hop path from startNode to goalNode using BFS.
    /// Returns ordered list of node IDs, empty if no path found.
    /// </summary>
    public List<int> FindPath(int startNode, int goalNode)
    {
        // Reset debug state
        lastFrontier.Clear();
        lastVisited.Clear();
        lastPath.Clear();

        if (graph == null)
        {
            Debug.LogError("[BFS] GraphReal reference is missing!");
            return new List<int>();
        }

        // ── BFS core data structures ──────────────────────────────────────
        Queue<int>           queue    = new Queue<int>();   // FIFO frontier
        Dictionary<int, int> cameFrom = new Dictionary<int, int>();
        HashSet<int>         visited  = new HashSet<int>(); // O(1) lookup

        queue.Enqueue(startNode);
        cameFrom[startNode] = startNode;
        visited.Add(startNode);

        Debug.Log($"[BFS] Starting search from Node {startNode} to Node {goalNode}");

        // ── BFS expansion loop ────────────────────────────────────────────
        while (queue.Count > 0)
        {
            // Snapshot frontier for visualizer
            lastFrontier = new List<int>(queue);

            int current = queue.Dequeue();
            lastVisited.Add(current);

            // Goal reached — stop
            if (current == goalNode)
            {
                Debug.Log($"[BFS] Goal Node {goalNode} reached!");
                break;
            }

            // Expand neighbours from adjacency list
            if (graph.adjacencyList.ContainsKey(current))
            {
                foreach (int neighbour in graph.adjacencyList[current])
                {
                    if (!visited.Contains(neighbour))
                    {
                        visited.Add(neighbour);
                        queue.Enqueue(neighbour);
                        cameFrom[neighbour] = current;
                    }
                }
            }
        }

        lastPath = ReconstructPath(cameFrom, startNode, goalNode);

        if (lastPath.Count > 0)
            Debug.Log($"[BFS] Path found ({lastPath.Count} nodes): " +
                      string.Join(" -> ", lastPath));
        else
            Debug.LogWarning($"[BFS] No path found from Node {startNode} to Node {goalNode}.");

        return new List<int>(lastPath);
    }

    // ─────────────────────────────────────────────────────────────────────
    private List<int> ReconstructPath(Dictionary<int, int> cameFrom, int start, int goal)
    {
        List<int> path = new List<int>();
        if (!cameFrom.ContainsKey(goal)) return path;

        int current = goal;
        while (current != start)
        {
            path.Add(current);
            current = cameFrom[current];
        }
        path.Add(start);
        path.Reverse();
        return path;
    }
}
