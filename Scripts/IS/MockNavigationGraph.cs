// =============================================================================
// MockNavigationGraph.cs
// Student 2 — IS (SE3062) | Prison Break: Silent Escape
// Role: Dynamic Adaptation & Event Interception
//
// Description:
//   Stub implementation of INavigationGraph used ONLY during development
//   while waiting for S1 (Student 1) to deliver the real NavMesh-extracted
//   graph. Logs all method calls to the Unity console so S2 can verify
//   the edge-severing logic works correctly in isolation.
//
//   At integration time, replace this with S1's real NavGraphExtractor class
//   in the Inspector (or via code) — no changes needed to BarricadeEventInterceptor.
//
// Unity Setup:
//   1. Attach this script to the GameManager GameObject (temporary).
//   2. In GraphEdgeRegistry, assign this as the INavigationGraph source.
//   3. Populate the 'testEdges' list with a few test edges in the Inspector
//      to simulate the real graph during development.
//   4. REMOVE or disable this component once S1's graph is integrated.
// =============================================================================

using System.Collections.Generic;
using UnityEngine;
using PrisonBreak.IS;

public class MockNavigationGraph : MonoBehaviour, INavigationGraph
{
    // ── Inspector Fields ──────────────────────────────────────────────────────

    [Header("Test Graph Definition")]
    [Tooltip("Define test edges here to simulate the real navigation graph.")]
    [SerializeField] private List<GraphEdge> testEdges = new List<GraphEdge>();

    [Header("Recalculation Simulation")]
    [Tooltip("Simulated delay before OnRecalculationComplete fires (seconds).")]
    [SerializeField] private float simulatedRecalcDelay = 0.3f;

    // ── Private State ─────────────────────────────────────────────────────────

    private readonly HashSet<(int, int)> _severedEdges = new HashSet<(int, int)>();

    // ── INavigationGraph Events ───────────────────────────────────────────────

    public event System.Action OnRecalculationComplete;

    // ── INavigationGraph Implementation ──────────────────────────────────────

    public void SeverEdge(int nodeA, int nodeB)
    {
        var key = nodeA < nodeB ? (nodeA, nodeB) : (nodeB, nodeA);
        bool added = _severedEdges.Add(key);
        if (added)
            Debug.Log($"[MockGraph] Edge SEVERED: ({nodeA} ↔ {nodeB})  |  Total severed: {_severedEdges.Count}");
        else
            Debug.LogWarning($"[MockGraph] Edge ({nodeA} ↔ {nodeB}) was already severed.");
    }

    public void RestoreEdge(int nodeA, int nodeB)
    {
        var key = nodeA < nodeB ? (nodeA, nodeB) : (nodeB, nodeA);
        bool removed = _severedEdges.Remove(key);
        if (removed)
            Debug.Log($"[MockGraph] Edge RESTORED: ({nodeA} ↔ {nodeB})  |  Total severed: {_severedEdges.Count}");
        else
            Debug.LogWarning($"[MockGraph] Tried to restore edge ({nodeA} ↔ {nodeB}) that wasn't severed.");
    }

    public bool IsEdgeActive(int nodeA, int nodeB)
    {
        var key = nodeA < nodeB ? (nodeA, nodeB) : (nodeB, nodeA);
        return !_severedEdges.Contains(key);
    }

    public void TriggerPathRecalculation()
    {
        Debug.Log($"[MockGraph] A* RECALCULATION TRIGGERED. Severed edges: {_severedEdges.Count}. " +
                  $"Simulating {simulatedRecalcDelay}s A* compute time...");
        StartCoroutine(SimulateRecalculation());
    }

    public IReadOnlyList<GraphEdge> GetAllEdges()
    {
        return testEdges;
    }

    // ── Private Methods ───────────────────────────────────────────────────────

    /// <summary>
    /// Simulates A* computation delay before firing OnRecalculationComplete.
    /// In the real implementation, S3's A* fires this callback when it finishes.
    /// </summary>
    private System.Collections.IEnumerator SimulateRecalculation()
    {
        yield return new WaitForSeconds(simulatedRecalcDelay);
        Debug.Log("[MockGraph] Recalculation COMPLETE. Guard paths would be updated here.");
        OnRecalculationComplete?.Invoke();
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor-only Gizmo: draws the mock graph edges in the Scene View as
    /// cyan lines so you can visually verify edge positions during development.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (testEdges == null) return;
        foreach (var edge in testEdges)
        {
            bool severed = !IsEdgeActive(edge.NodeA, edge.NodeB);
            Gizmos.color = severed ? Color.red : Color.cyan;
            Gizmos.DrawLine(edge.WorldPositionA, edge.WorldPositionB);
            Gizmos.DrawSphere(edge.WorldPositionA, 0.12f);
            Gizmos.DrawSphere(edge.WorldPositionB, 0.12f);
        }
    }
#endif
}
