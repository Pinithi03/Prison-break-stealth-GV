// =============================================================================
// GraphEdgeRegistry.cs
// Student 2 — IS (SE3062) | Prison Break: Silent Escape
// Role: Dynamic Adaptation & Event Interception
//
// Description:
//   Scene-level registry that holds a reference to the active INavigationGraph
//   implementation and provides a single access point for all IS scripts.
//   Also acts as a bridge between the GV world (GameObjects, colliders) and
//   the IS world (graph node IDs, edge data).
//
//   This is implemented as a MonoBehaviour singleton rather than a static class
//   so it can be assigned in the Inspector and serialized by Unity's scene
//   system — making it testable and inspectable.
//
// Unity Setup:
//   1. Attach this to the GameManager GameObject (or a dedicated empty object).
//   2. Assign the 'navigationGraph' field:
//      - During development: assign MockNavigationGraph
//      - At integration: assign S1's real NavGraphExtractor
//   3. BarricadeEventInterceptor will call GraphEdgeRegistry.Instance.Graph
//      to access the active graph.
// =============================================================================

using UnityEngine;
using PrisonBreak.IS;

public class GraphEdgeRegistry : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static GraphEdgeRegistry Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        ValidateGraphAssignment();
    }

    // ── Inspector Fields ──────────────────────────────────────────────────────

    [Header("Active Navigation Graph")]
    [Tooltip("Assign MockNavigationGraph here during development.\n" +
             "Replace with S1's NavGraphExtractor at integration time.")]
    [SerializeField] private MonoBehaviour navigationGraphBehaviour;

    // ── Public Properties ─────────────────────────────────────────────────────

    /// <summary>
    /// The active navigation graph. All IS scripts should access the graph
    /// through this property — never directly reference a concrete class.
    /// </summary>
    public INavigationGraph Graph { get; private set; }

    // ── Private Methods ───────────────────────────────────────────────────────

    private void ValidateGraphAssignment()
    {
        if (navigationGraphBehaviour == null)
        {
            Debug.LogError("[GraphEdgeRegistry] No navigation graph assigned! " +
                           "Assign MockNavigationGraph or S1's NavGraphExtractor.");
            return;
        }

        Graph = navigationGraphBehaviour as INavigationGraph;

        if (Graph == null)
        {
            Debug.LogError($"[GraphEdgeRegistry] Assigned component '{navigationGraphBehaviour.name}' " +
                           $"does not implement INavigationGraph. Check the assignment.");
        }
        else
        {
            Debug.Log($"[GraphEdgeRegistry] Navigation graph active: " +
                      $"{navigationGraphBehaviour.GetType().Name} with " +
                      $"{Graph.GetAllEdges().Count} edges.");
        }
    }
}
