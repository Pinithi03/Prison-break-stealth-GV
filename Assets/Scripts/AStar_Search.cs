using System.Collections.Generic;
using UnityEngine;

public class AStar_Search : MonoBehaviour
{
    public GraphReal graph;

    public List<int> FindPath(int startNode, int goalNode)
    {
        if (graph == null) return new List<int>();

        List<KeyValuePair<int, float>> frontier = new List<KeyValuePair<int, float>>();
        Dictionary<int, int> cameFrom = new Dictionary<int, int>();
        Dictionary<int, float> costSoFar = new Dictionary<int, float>();

        frontier.Add(new KeyValuePair<int, float>(startNode, 0f));
        cameFrom[startNode] = startNode;
        costSoFar[startNode] = 0f;

        while (frontier.Count > 0)
        {
            frontier.Sort((x, y) => x.Value.CompareTo(y.Value));
            int current = frontier[0].Key;
            frontier.RemoveAt(0);

            if (current == goalNode)
            {
                break;
            }

            if (graph.weightedAdjacencyList.ContainsKey(current))
            {
                foreach (var edge in graph.weightedAdjacencyList[current])
                {
                    int neighbor = edge.Key;
                    float newCost = costSoFar[current] + edge.Value;

                    if (!costSoFar.ContainsKey(neighbor) || newCost < costSoFar[neighbor])
                    {
                        costSoFar[neighbor] = newCost;
                        float priority = newCost + Heuristic(neighbor, goalNode);
                        frontier.Add(new KeyValuePair<int, float>(neighbor, priority));
                        cameFrom[neighbor] = current;
                    }
                }
            }
        }

        return ReconstructPath(cameFrom, startNode, goalNode);
    }

    private float Heuristic(int nodeId, int goalId)
    {
        if (!graph.nodePositions.ContainsKey(nodeId) || !graph.nodePositions.ContainsKey(goalId))
            return 0f;

        return Vector3.Distance(graph.nodePositions[nodeId], graph.nodePositions[goalId]);
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
