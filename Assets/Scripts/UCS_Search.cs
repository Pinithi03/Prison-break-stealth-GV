using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UCS_Search : MonoBehaviour
{
    public GraphReal graph;
    public List<int> lastFrontier = new List<int>();
    public List<int> lastExplored = new List<int>();

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

            // update diagnostics
            if (!lastExplored.Contains(current)) lastExplored.Add(current);
            // frontier snapshot
            lastFrontier = new List<int>();
            foreach (var kv in frontier) lastFrontier.Add(kv.Key);

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
                        frontier.Add(new KeyValuePair<int, float>(neighbor, newCost));
                        cameFrom[neighbor] = current;
                    }
                }
            }
        }

        return ReconstructPath(cameFrom, startNode, goalNode);
    }

    public IEnumerator FindPathCoroutine(int startNode, int goalNode, float stepDelay, System.Action<List<int>> onComplete)
    {
        if (graph == null)
        {
            onComplete?.Invoke(new List<int>());
            yield break;
        }

        List<KeyValuePair<int, float>> frontier = new List<KeyValuePair<int, float>>();
        Dictionary<int, int> cameFrom = new Dictionary<int, int>();
        Dictionary<int, float> costSoFar = new Dictionary<int, float>();

        frontier.Add(new KeyValuePair<int, float>(startNode, 0f));
        cameFrom[startNode] = startNode;
        costSoFar[startNode] = 0f;

        lastExplored.Clear();
        lastFrontier.Clear();

        while (frontier.Count > 0)
        {
            frontier.Sort((x, y) => x.Value.CompareTo(y.Value));
            int current = frontier[0].Key;
            frontier.RemoveAt(0);

            // update diagnostics
            if (!lastExplored.Contains(current)) lastExplored.Add(current);
            // frontier snapshot
            lastFrontier = new List<int>();
            foreach (var kv in frontier) lastFrontier.Add(kv.Key);

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
                        frontier.Add(new KeyValuePair<int, float>(neighbor, newCost));
                        cameFrom[neighbor] = current;
                    }
                }
            }

            yield return new WaitForSeconds(stepDelay);
        }

        var path = ReconstructPath(cameFrom, startNode, goalNode);
        onComplete?.Invoke(path);
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
