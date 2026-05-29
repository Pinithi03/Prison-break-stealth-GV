using System.Collections.Generic;
using UnityEngine;

public class UCS_Search : MonoBehaviour
{
    public GraphDummy graph;

    public List<int> FindPath(int startNode, int goalNode)
    {
        if (graph == null) return new List<int>();

        // Simple priority queue using a List and sorting
        List<KeyValuePair<int, float>> frontier = new List<KeyValuePair<int, float>>();
        Dictionary<int, int> cameFrom = new Dictionary<int, int>();
        Dictionary<int, float> costSoFar = new Dictionary<int, float>();

        frontier.Add(new KeyValuePair<int, float>(startNode, 0f));
        cameFrom[startNode] = startNode;
        costSoFar[startNode] = 0f;

        while (frontier.Count > 0)
        {
            // Sort to simulate priority queue (lowest cost first)
            frontier.Sort((x, y) => x.Value.CompareTo(y.Value));
            int current = frontier[0].Key;
            frontier.RemoveAt(0);

            if (current == goalNode)
            {
                break;
            }

            if (graph.adjacencyList.ContainsKey(current))
            {
                foreach (int neighbor in graph.adjacencyList[current])
                {
                    float newCost = costSoFar[current] + Vector3.Distance(graph.nodePositions[current], graph.nodePositions[neighbor]);

                    if (!costSoFar.ContainsKey(neighbor) || newCost < costSoFar[neighbor])
                    {
                        costSoFar[neighbor] = newCost;
                        frontier.Add(new KeyValuePair<int, float>(neighbor, newCost));
                        cameFrom[neighbor] = current;
                    }
                }
            }
        }

        return ReconstructPath(cameFrom, startNode, goalNode);
    }

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
