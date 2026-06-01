using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Uniform Cost Search on the prison graph.
/// Student 4 - IS Module (SE3062)
///
/// UCS guarantees the OPTIMAL (lowest cost) path by always expanding
/// the node with the lowest cumulative cost first.
/// 
/// Time Complexity:  O((V + E) log V)  due to priority queue sorting
/// Space Complexity: O(V)              for frontier and cost tables
/// Data Structure:   Min-Priority Queue (sorted List) — expands cheapest node first
/// Difference from BFS: BFS counts hops; UCS counts real movement cost (distance)
/// </summary>
public class UCS_Search : MonoBehaviour
{
    public GraphReal graph;

    // ── Exposed state for DebugVisualizer ─────────────────────────────────
    [HideInInspector] public List<int>              lastFrontier  = new List<int>();
    [HideInInspector] public List<int>              lastVisited   = new List<int>();
    [HideInInspector] public List<int>              lastPath      = new List<int>();
    [HideInInspector] public Dictionary<int, float> lastCostSoFar = new Dictionary<int, float>();

    // Alias so Member 3's FrontierVisualizer compiles without changes
    public List<int> lastExplored => lastVisited;

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Finds lowest-cost path from startNode to goalNode using UCS.
    /// Edge costs are NavMesh distances stored in weightedAdjacencyList.
    /// Returns ordered list of node IDs, empty if no path found.
    /// </summary>
    public List<int> FindPath(int startNode, int goalNode)
    {
        lastFrontier.Clear();
        lastVisited.Clear();
        lastPath.Clear();
        lastCostSoFar.Clear();

        if (graph == null)
        {
            Debug.LogError("[UCS] GraphReal reference is missing!");
            return new List<int>();
        }

        // ── UCS core data structures ──────────────────────────────────────
        // Priority queue: sorted by cumulative cost (ascending)
        List<KeyValuePair<int, float>> frontier = new List<KeyValuePair<int, float>>();
        Dictionary<int, int>   cameFrom  = new Dictionary<int, int>();
        Dictionary<int, float> costSoFar = new Dictionary<int, float>();
        HashSet<int>           expanded  = new HashSet<int>();

        frontier.Add(new KeyValuePair<int, float>(startNode, 0f));
        cameFrom[startNode]  = startNode;
        costSoFar[startNode] = 0f;

        Debug.Log($"[UCS] Starting cost-optimal search from Node {startNode} to Node {goalNode}");

        // ── UCS expansion loop ────────────────────────────────────────────
        while (frontier.Count > 0)
        {
            // Sort by cost — lowest cost node first (min-priority queue)
            frontier.Sort((a, b) => a.Value.CompareTo(b.Value));

            // Snapshot frontier node IDs for visualizer
            lastFrontier.Clear();
            foreach (var f in frontier) lastFrontier.Add(f.Key);

            int   current     = frontier[0].Key;
            float currentCost = frontier[0].Value;
            frontier.RemoveAt(0);

            // Skip if already fully expanded (cheaper path found earlier)
            if (expanded.Contains(current)) continue;
            expanded.Add(current);
            lastVisited.Add(current);

            // Goal reached
            if (current == goalNode)
            {
                Debug.Log($"[UCS] Goal Node {goalNode} reached! Total cost: {currentCost:F2}");
                break;
            }

            // Expand neighbours using weighted edges
            if (graph.weightedAdjacencyList.ContainsKey(current))
            {
                foreach (var edge in graph.weightedAdjacencyList[current])
                {
                    int   neighbour = edge.Key;
                    float newCost   = costSoFar[current] + edge.Value;

                    if (!costSoFar.ContainsKey(neighbour) || newCost < costSoFar[neighbour])
                    {
                        costSoFar[neighbour] = newCost;
                        frontier.Add(new KeyValuePair<int, float>(neighbour, newCost));
                        cameFrom[neighbour] = current;
                    }
                }
            }
        }

        lastCostSoFar = costSoFar;
        lastPath      = ReconstructPath(cameFrom, startNode, goalNode);

        if (lastPath.Count > 0)
        {
            float totalCost = costSoFar.ContainsKey(goalNode) ? costSoFar[goalNode] : 0f;
            Debug.Log($"[UCS] Path found ({lastPath.Count} nodes, cost {totalCost:F2}): " +
                      string.Join(" -> ", lastPath));
        }
        else
        {
            Debug.LogWarning($"[UCS] No path found from Node {startNode} to Node {goalNode}.");
        }

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

    // ── Animated coroutine version for SearchTester animated mode ─────────
    /// <summary>
    /// Coroutine version of UCS that steps through expansion with a delay.
    /// Used by SearchTester when animateSearch = true.
    /// </summary>
    public System.Collections.IEnumerator FindPathCoroutine(
        int startNode, int goalNode, float stepDelay,
        System.Action<List<int>> onComplete)
    {
        lastFrontier.Clear();
        lastVisited.Clear();
        lastPath.Clear();
        lastCostSoFar.Clear();

        if (graph == null) { onComplete?.Invoke(new List<int>()); yield break; }

        List<KeyValuePair<int, float>> frontier = new List<KeyValuePair<int, float>>();
        Dictionary<int, int>   cameFrom  = new Dictionary<int, int>();
        Dictionary<int, float> costSoFar = new Dictionary<int, float>();
        HashSet<int>           expanded  = new HashSet<int>();

        frontier.Add(new KeyValuePair<int, float>(startNode, 0f));
        cameFrom[startNode]  = startNode;
        costSoFar[startNode] = 0f;

        while (frontier.Count > 0)
        {
            frontier.Sort((a, b) => a.Value.CompareTo(b.Value));
            lastFrontier.Clear();
            foreach (var f in frontier) lastFrontier.Add(f.Key);

            int   current     = frontier[0].Key;
            float currentCost = frontier[0].Value;
            frontier.RemoveAt(0);

            if (expanded.Contains(current)) continue;
            expanded.Add(current);
            lastVisited.Add(current);

            if (current == goalNode) break;

            if (graph.weightedAdjacencyList.ContainsKey(current))
            {
                foreach (var edge in graph.weightedAdjacencyList[current])
                {
                    int   neighbour = edge.Key;
                    float newCost   = costSoFar[current] + edge.Value;
                    if (!costSoFar.ContainsKey(neighbour) || newCost < costSoFar[neighbour])
                    {
                        costSoFar[neighbour] = newCost;
                        frontier.Add(new KeyValuePair<int, float>(neighbour, newCost));
                        cameFrom[neighbour] = current;
                    }
                }
            }
            yield return new UnityEngine.WaitForSeconds(stepDelay);
        }

        lastCostSoFar = costSoFar;
        lastPath      = ReconstructPath(cameFrom, startNode, goalNode);
        onComplete?.Invoke(new List<int>(lastPath));
    }
}
