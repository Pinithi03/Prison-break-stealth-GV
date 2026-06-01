using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A* Pathfinding Algorithm Implementation
/// Combines actual cost (g) and heuristic estimate (h) to find optimal paths efficiently.
/// Priority: f = g + h (lower is better)
/// 
/// Student 3 Implementation: A* Search & Heuristic Design
/// - Maintains priority queue ordered by f-cost
/// - Uses Euclidean distance as heuristic function
/// - Tracks visited nodes to prevent redundant exploration
/// - Guarantees optimal path when heuristic is admissible
/// </summary>
public class AStar_Search : MonoBehaviour
{
    public GraphReal graph;
    // Last run diagnostics (populated by FindPath)
    public List<int> lastFrontier = new List<int>();
    public List<int> lastExplored = new List<int>();

    /// <summary>
    /// Finds the optimal path from startNode to goalNode using A* algorithm.
    /// </summary>
    /// <param name="startNode">Starting node ID</param>
    /// <param name="goalNode">Goal node ID</param>
    /// <returns>List of node IDs representing the path, or empty list if no path exists</returns>
    public List<int> FindPath(int startNode, int goalNode)
    {
        if (graph == null) return new List<int>();

        // Priority queue: stores (nodeId, fCost) sorted by fCost
        // fCost = gCost (actual) + hCost (heuristic estimate)
        PriorityQueue<int, float> frontier = new PriorityQueue<int, float>();
        
        // Tracks the best path to each node
        Dictionary<int, int> cameFrom = new Dictionary<int, int>();
        
        // g(n): actual cost from start to node
        Dictionary<int, float> gCost = new Dictionary<int, float>();
        
        // Closed set: nodes already explored (visited)
        HashSet<int> closedSet = new HashSet<int>();

        // Initialize start node
        frontier.Enqueue(startNode, 0f);
        cameFrom[startNode] = startNode;
        gCost[startNode] = 0f;

        int nodesExplored = 0;

        while (frontier.Count > 0)
        {
            int current = frontier.Dequeue();

            // Skip if already explored
            if (closedSet.Contains(current))
                continue;

            closedSet.Add(current);
            nodesExplored++;

            // Snapshot for visualization
            lastExplored = new List<int>(closedSet);
            // Note: heap internals not directly exposed; we store gCost keys not yet explored as frontier
            lastFrontier = new List<int>(gCost.Keys);

            // Goal found - reconstruct path
            if (current == goalNode)
            {
                Debug.Log($"[A*] Path found! Nodes explored: {nodesExplored}");
                return ReconstructPath(cameFrom, startNode, goalNode);
            }

            // Explore neighbors
            if (graph.weightedAdjacencyList.ContainsKey(current))
            {
                foreach (var edge in graph.weightedAdjacencyList[current])
                {
                    int neighbor = edge.Key;
                    float edgeCost = edge.Value;

                    // Skip if already visited
                    if (closedSet.Contains(neighbor))
                        continue;

                    // Calculate new g-cost (actual cost from start)
                    float newGCost = gCost[current] + edgeCost;

                    // Only process if this is a better path to the neighbor
                    if (!gCost.ContainsKey(neighbor) || newGCost < gCost[neighbor])
                    {
                        gCost[neighbor] = newGCost;
                        cameFrom[neighbor] = current;

                        // Calculate h-cost (heuristic estimate to goal)
                        float hCost = Heuristic(neighbor, goalNode);
                        
                        // f-cost = g + h (priority for the queue)
                        float fCost = newGCost + hCost;

                        frontier.Enqueue(neighbor, fCost);
                    }
                }
            }
        }

        Debug.LogWarning($"[A*] No path found between nodes {startNode} and {goalNode}. Nodes explored: {nodesExplored}");
        return new List<int>();
    }

    public IEnumerator FindPathCoroutine(int startNode, int goalNode, float stepDelay, System.Action<List<int>> onComplete)
    {
        if (graph == null)
        {
            onComplete?.Invoke(new List<int>());
            yield break;
        }

        PriorityQueue<int, float> frontier = new PriorityQueue<int, float>();
        Dictionary<int, int> cameFrom = new Dictionary<int, int>();
        Dictionary<int, float> gCost = new Dictionary<int, float>();
        HashSet<int> closedSet = new HashSet<int>();

        frontier.Enqueue(startNode, 0f);
        cameFrom[startNode] = startNode;
        gCost[startNode] = 0f;

        lastExplored.Clear();
        lastFrontier.Clear();

        int nodesExplored = 0;

        while (frontier.Count > 0)
        {
            int current = frontier.Dequeue();

            if (closedSet.Contains(current))
            {
                yield return new WaitForSeconds(stepDelay);
                continue;
            }

            closedSet.Add(current);
            nodesExplored++;

            // Snapshot for visualization
            lastExplored = new List<int>(closedSet);
            lastFrontier = new List<int>(gCost.Keys);

            // Goal found - reconstruct path
            if (current == goalNode)
            {
                Debug.Log($"[A*] Path found! Nodes explored: {nodesExplored}");
                var path = ReconstructPath(cameFrom, startNode, goalNode);
                onComplete?.Invoke(path);
                yield break;
            }

            // Explore neighbors
            if (graph.weightedAdjacencyList.ContainsKey(current))
            {
                foreach (var edge in graph.weightedAdjacencyList[current])
                {
                    int neighbor = edge.Key;
                    float edgeCost = edge.Value;

                    if (closedSet.Contains(neighbor))
                        continue;

                    float newGCost = gCost[current] + edgeCost;

                    if (!gCost.ContainsKey(neighbor) || newGCost < gCost[neighbor])
                    {
                        gCost[neighbor] = newGCost;
                        cameFrom[neighbor] = current;

                        float hCost = Heuristic(neighbor, goalNode);
                        float fCost = newGCost + hCost;

                        frontier.Enqueue(neighbor, fCost);
                    }
                }
            }

            yield return new WaitForSeconds(stepDelay);
        }

        Debug.LogWarning($"[A*] No path found between nodes {startNode} and {goalNode}. Nodes explored: {nodesExplored}");
        onComplete?.Invoke(new List<int>());
    }

    /// <summary>
    /// Heuristic function: Euclidean distance to goal
    /// This is admissible (never overestimates) for straight-line movement.
    /// </summary>
    private float Heuristic(int nodeId, int goalId)
    {
        if (!graph.nodePositions.ContainsKey(nodeId) || !graph.nodePositions.ContainsKey(goalId))
            return 0f;

        return Vector3.Distance(graph.nodePositions[nodeId], graph.nodePositions[goalId]);
    }

    /// <summary>
    /// Reconstructs the path by backtracking from goal to start using cameFrom dictionary.
    /// </summary>
    private List<int> ReconstructPath(Dictionary<int, int> cameFrom, int start, int goal)
    {
        List<int> path = new List<int>();
        if (!cameFrom.ContainsKey(goal)) 
            return path;

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

/// <summary>
/// Generic Priority Queue implementation for A* algorithm
/// Uses binary heap structure for efficient O(log n) operations
/// </summary>
public class PriorityQueue<TElement, TPriority> where TPriority : System.IComparable<TPriority>
{
    private List<(TElement element, TPriority priority)> heap = new List<(TElement, TPriority)>();

    public int Count => heap.Count;

    public void Enqueue(TElement element, TPriority priority)
    {
        heap.Add((element, priority));
        int childIndex = heap.Count - 1;
        
        while (childIndex > 0)
        {
            int parentIndex = (childIndex - 1) / 2;
            
            if (heap[childIndex].priority.CompareTo(heap[parentIndex].priority) >= 0)
                break;

            // Swap
            var temp = heap[childIndex];
            heap[childIndex] = heap[parentIndex];
            heap[parentIndex] = temp;

            childIndex = parentIndex;
        }
    }

    public TElement Dequeue()
    {
        if (heap.Count == 0)
            throw new System.InvalidOperationException("Priority queue is empty");

        TElement result = heap[0].element;
        heap[0] = heap[heap.Count - 1];
        heap.RemoveAt(heap.Count - 1);

        int parentIndex = 0;
        while (true)
        {
            int leftChild = 2 * parentIndex + 1;
            int rightChild = 2 * parentIndex + 2;
            int smallestIndex = parentIndex;

            if (leftChild < heap.Count && heap[leftChild].priority.CompareTo(heap[smallestIndex].priority) < 0)
                smallestIndex = leftChild;

            if (rightChild < heap.Count && heap[rightChild].priority.CompareTo(heap[smallestIndex].priority) < 0)
                smallestIndex = rightChild;

            if (smallestIndex == parentIndex)
                break;

            // Swap
            var temp = heap[parentIndex];
            heap[parentIndex] = heap[smallestIndex];
            heap[smallestIndex] = temp;

            parentIndex = smallestIndex;
        }

        return result;
    }
}
