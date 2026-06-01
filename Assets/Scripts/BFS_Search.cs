using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BFS_Search : MonoBehaviour
{
    public GraphReal graph;
    public List<int> lastFrontier = new List<int>();
    public List<int> lastExplored = new List<int>();

    public List<int> FindPath(int startNode, int goalNode)
    {
        if (graph == null) return new List<int>();

        Queue<int> queue = new Queue<int>();
        Dictionary<int, int> cameFrom = new Dictionary<int, int>();

        queue.Enqueue(startNode);
        cameFrom[startNode] = startNode;

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();

            // update diagnostics
            lastExplored.Add(current);
            lastFrontier = new List<int>(queue.ToArray());

            if (current == goalNode)
            {
                break;
            }

            if (graph.adjacencyList.ContainsKey(current))
            {
                foreach (int neighbor in graph.adjacencyList[current])
                {
                    if (!cameFrom.ContainsKey(neighbor))
                    {
                        queue.Enqueue(neighbor);
                        cameFrom[neighbor] = current;
                    }
                }
            }
        }

        return ReconstructPath(cameFrom, startNode, goalNode);
    }

    public IEnumerator FindPathCoroutine(int startNode, int goalNode, float stepDelay, Action<List<int>> onComplete)
    {
        if (graph == null)
        {
            onComplete?.Invoke(new List<int>());
            yield break;
        }

        Queue<int> queue = new Queue<int>();
        Dictionary<int, int> cameFrom = new Dictionary<int, int>();

        queue.Enqueue(startNode);
        cameFrom[startNode] = startNode;

        lastExplored.Clear();
        lastFrontier.Clear();

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();

            // update diagnostics
            lastExplored.Add(current);
            lastFrontier = new List<int>(queue.ToArray());

            if (current == goalNode)
            {
                break;
            }

            if (graph.adjacencyList.ContainsKey(current))
            {
                foreach (int neighbor in graph.adjacencyList[current])
                {
                    if (!cameFrom.ContainsKey(neighbor))
                    {
                        queue.Enqueue(neighbor);
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
