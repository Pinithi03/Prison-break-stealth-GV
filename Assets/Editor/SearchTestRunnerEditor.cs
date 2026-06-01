using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility to run simple search tests without entering Play mode.
/// Adds menu item: Tools/Run Search Tests
/// </summary>
public class SearchTestRunnerEditor
{
    [MenuItem("Tools/Run Search Tests")]
    public static void RunTests()
    {
        GraphReal graph = GameObject.FindObjectOfType<GraphReal>();
        SearchTester tester = GameObject.FindObjectOfType<SearchTester>();

        if (graph == null || tester == null)
        {
            Debug.LogError("SearchTestRunner: Missing GraphReal or SearchTester in scene.");
            return;
        }

        // ensure graph built
        graph.BuildGraphFromSceneNodes();

        // Run algorithms
        if (tester.bfs != null) tester.bfs.graph = graph;
        if (tester.ucs != null) tester.ucs.graph = graph;
        if (tester.aStar != null) tester.aStar.graph = graph;

        int s = tester.startNodeId;
        int g = tester.goalNodeId;

        var bfsPath = tester.bfs != null ? tester.bfs.FindPath(s, g) : null;
        var ucsPath = tester.ucs != null ? tester.ucs.FindPath(s, g) : null;
        var aPath = tester.aStar != null ? tester.aStar.FindPath(s, g) : null;

        Debug.Log($"[EditorTest] BFS path: {(bfsPath==null?"null":string.Join("->", bfsPath))}");
        Debug.Log($"[EditorTest] UCS path: {(ucsPath==null?"null":string.Join("->", ucsPath))}");
        Debug.Log($"[EditorTest] A* path: {(aPath==null?"null":string.Join("->", aPath))}");

        Debug.Log("SearchTestRunner: Tests complete.");
    }
}
