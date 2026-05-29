using System.Collections.Generic;
using UnityEngine;

public class SearchTester : MonoBehaviour
{
    public BFS_Search bfs;
    public UCS_Search ucs;

    void Start()
    {
        if (bfs == null || ucs == null)
        {
            Debug.LogError("SearchTester is missing references to BFS or UCS scripts!");
            return;
        }

        Debug.Log("=== Starting Search Algorithm Tests ===");

        // Test BFS from Node 0 to Node 4
        List<int> bfsPath = bfs.FindPath(0, 4);
        PrintPath("BFS", bfsPath);

        // Test UCS from Node 0 to Node 4
        List<int> ucsPath = ucs.FindPath(0, 4);
        PrintPath("UCS", ucsPath);
        
        Debug.Log("=======================================");
    }

    private void PrintPath(string algorithmName, List<int> path)
    {
        if (path == null || path.Count == 0)
        {
            Debug.LogWarning($"{algorithmName} could not find a path.");
            return;
        }

        string pathString = string.Join(" -> ", path);
        Debug.Log($"[{algorithmName} Path Found]: {pathString}");
    }
}
