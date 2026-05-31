using System.Collections.Generic;
using UnityEngine;

public class SearchTester : MonoBehaviour
{
    public GraphReal graph;
    public BFS_Search bfs;
    public UCS_Search ucs;
    public AStar_Search aStar;

    public int startNodeId = 0;
    public int goalNodeId = 4;

    void Start()
    {
        if (graph == null)
        {
            Debug.LogError("SearchTester: GraphReal reference is missing!");
            return;
        }

        if (bfs != null) bfs.graph = graph;
        if (ucs != null) ucs.graph = graph;
        if (aStar != null) aStar.graph = graph;

        Debug.Log("=== Search Algorithm Comparison on Real Prison Graph ===");

        if (bfs != null)
        {
            List<int> bfsPath = bfs.FindPath(startNodeId, goalNodeId);
            PrintPath("BFS", bfsPath);
        }

        if (ucs != null)
        {
            List<int> ucsPath = ucs.FindPath(startNodeId, goalNodeId);
            PrintPath("UCS", ucsPath);
        }

        if (aStar != null)
        {
            List<int> aStarPath = aStar.FindPath(startNodeId, goalNodeId);
            PrintPath("A*", aStarPath);
        }

        Debug.Log("========================================================");
    }

    private void PrintPath(string algorithmName, List<int> path)
    {
        if (path == null || path.Count == 0)
        {
            Debug.LogWarning($"{algorithmName}: No path found between Node {startNodeId} and Node {goalNodeId}.");
            return;
        }
        string pathString = string.Join(" -> ", path);
        Debug.Log($"[{algorithmName}] Path ({path.Count} nodes): {pathString}");
    }
}
