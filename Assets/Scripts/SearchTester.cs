using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class SearchTester : MonoBehaviour
{
    public GraphReal graph;
    public BFS_Search bfs;
    public UCS_Search ucs;
    public AStar_Search aStar;
    public PathVisualizer pathVisualizer;

    public int startNodeId = 0;
    public int goalNodeId = 4;

    [Header("Animation")]
    public bool animateSearch = false;
    public float stepDelay = 0.25f;

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
        if (pathVisualizer == null) pathVisualizer = FindObjectOfType<PathVisualizer>();

        Debug.Log("=== Search Algorithm Comparison on Real Prison Graph ===");

        if (animateSearch)
        {
            // Run animated coroutines sequentially
            StartCoroutine(RunAnimatedTests());
        }
        else
        {
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
                if (pathVisualizer != null)
                {
                    pathVisualizer.graph = graph;
                    pathVisualizer.path = aStarPath;
                }
            }

            Debug.Log("========================================================");
        }
    }

    private IEnumerator RunAnimatedTests()
    {
        if (bfs != null)
        {
            bool done = false;
            StartCoroutine(bfs.FindPathCoroutine(startNodeId, goalNodeId, stepDelay, (path) => {
                PrintPath("BFS", path);
                done = true;
            }));
            while (!done) yield return null;
            yield return new WaitForSeconds(0.5f);
        }

        if (ucs != null)
        {
            bool done = false;
            StartCoroutine(ucs.FindPathCoroutine(startNodeId, goalNodeId, stepDelay, (path) => {
                PrintPath("UCS", path);
                done = true;
            }));
            while (!done) yield return null;
            yield return new WaitForSeconds(0.5f);
        }

        if (aStar != null)
        {
            bool done = false;
            StartCoroutine(aStar.FindPathCoroutine(startNodeId, goalNodeId, stepDelay, (path) => {
                PrintPath("A*", path);
                if (pathVisualizer != null)
                {
                    pathVisualizer.graph = graph;
                    pathVisualizer.path = path;
                }
                done = true;
            }));
            while (!done) yield return null;
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
