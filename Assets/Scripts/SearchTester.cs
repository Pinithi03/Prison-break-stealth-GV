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
        if (graph == null || bfs == null || ucs == null || aStar == null)
        {
            Debug.LogError("SearchTester is missing references! Assign GraphReal, BFS, UCS and AStar in Inspector.");
            return;
        }

        bfs.graph = graph;
        ucs.graph = graph;
        aStar.graph = graph;

        Debug.Log("=== Starting Search Algorithm Tests on Real Prison Graph ===");

        List<int> bfsPath = bfs.FindPath(startNodeId, goalNodeId);
        PrintPath("BFS", bfsPath);

        List<int> ucsPath = ucs.FindPath(startNodeId, goalNodeId);
        PrintPath("UCS", ucsPath);

        List<int> aStarPath = aStar.FindPath(startNodeId, goalNodeId);
        PrintPath("A*", aStarPath);

        Debug.Log("======================================================");
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
