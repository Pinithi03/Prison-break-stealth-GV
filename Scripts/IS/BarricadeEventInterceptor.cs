// =============================================================================
// BarricadeEventInterceptor.cs
// Student 2 — IS (SE3062) | Prison Break: Silent Escape
// Role: Dynamic Adaptation & Event Interception
//
// ALGORITHM DESCRIPTION:
// ─────────────────────────────────────────────────────────────────────────────
// This script is the core of Student 2's IS contribution. It solves the problem
// of dynamically adapting the AI navigation graph when the player physically
// pushes a barricade into a corridor, blocking guards' patrol routes.
//
// The algorithm operates in four stages:
//
//   1. EVENT DETECTION
//      BarricadePhysics.cs (GV script on same object) calls SendMessage(
//      "OnBarricadeSettled") when the barricade's Rigidbody velocity drops
//      below the settle threshold. This triggers a scan.
//
//   2. EDGE INTERSECTION TEST  [O(E) time complexity]
//      For each edge in the navigation graph (from GraphEdgeRegistry):
//        a. Compute the direction vector: dir = (edgeB.worldPos - edgeA.worldPos)
//        b. Run Physics.BoxCast from edgeA.worldPos in direction dir,
//           with half-extents matching the barricade's collider half-size.
//        c. If the BoxCast hits THIS barricade's collider → the edge is blocked.
//      This gives O(E) per scan where E = number of graph edges.
//
//   3. GRAPH MUTATION  [O(1) amortized per operation via HashSet]
//      Compare newly blocked edges with the set of previously severed edges:
//        - Edges newly blocked → call SeverEdge() on the graph
//        - Edges previously blocked but now clear → call RestoreEdge()
//      Uses a HashSet<(int,int)> for O(1) lookup during comparison.
//
//   4. RECALCULATION TRIGGER + INFINITE LOOP GUARD
//      After graph mutation, call TriggerPathRecalculation().
//      Guard flag _isRecalculating prevents re-triggering while A* runs.
//      If a second barricade event fires during recalculation, a deferred
//      recalculation is queued to fire once A* completes.
//
// DATA STRUCTURES:
//   - HashSet<(int,int)> _severedByThisBarricade  : O(1) add/remove/contains
//     Stores edges currently blocked by THIS barricade.
//     Using a per-barricade set means multiple barricades can independently
//     sever different edges without interfering with each other's state.
//
// COMPLEXITY SUMMARY:
//   - Per settle event: O(E) for edge scan
//   - Per SeverEdge/RestoreEdge: O(1) amortized (HashSet)
//   - Per recalculation trigger: O(1) flag check
//   - Memory: O(k) where k = number of edges blocked by this barricade
//
// =============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PrisonBreak.IS;

[RequireComponent(typeof(BoxCollider))]
public class BarricadeEventInterceptor : MonoBehaviour
{
    // ── Inspector Fields ──────────────────────────────────────────────────────

    [Header("Edge Detection")]
    [Tooltip("Half-extents of the BoxCast used to test edge intersection.\n" +
             "Should roughly match the barricade's collider half-size.")]
    [SerializeField] private Vector3 castHalfExtents = new Vector3(0.4f, 0.5f, 0.4f);

    [Tooltip("Layer mask for Physics.BoxCast. Should only detect THIS barricade.\n" +
             "Set to the 'Barricade' layer.")]
    [SerializeField] private LayerMask barricadeLayerMask;

    [Header("Debug Visualisation")]
    [Tooltip("Draw BoxCast rays in Scene View during edge detection.")]
    [SerializeField] private bool debugDrawCasts = true;

    [Tooltip("Colour of unblocked edges in debug view.")]
    [SerializeField] private Color debugEdgeColor   = new Color(0f, 1f, 1f, 0.5f);

    [Tooltip("Colour of severed edges in debug view.")]
    [SerializeField] private Color debugSeveredColor = new Color(1f, 0.2f, 0.2f, 0.9f);

    // ── Private State ─────────────────────────────────────────────────────────

    /// <summary>
    /// Edges currently severed by THIS barricade (keyed as normalised pairs).
    /// Per-barricade isolation: if barricade A severs edge (1,2) and barricade B
    /// is moved away, restoring its edges will not affect edge (1,2).
    /// </summary>
    private readonly HashSet<(int, int)> _severedByThisBarricade = new HashSet<(int, int)>();

    /// <summary>
    /// Loop guard: prevents double-triggering A* while a recalculation is in progress.
    /// </summary>
    private bool _isRecalculating = false;

    /// <summary>
    /// Set to true if a barricade event fires while recalculation is in progress.
    /// A single deferred recalculation will be queued after A* completes.
    /// </summary>
    private bool _deferredRecalcPending = false;

    private BoxCollider _collider;
    private INavigationGraph _graph;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        _collider = GetComponent<BoxCollider>();
    }

    private void Start()
    {
        // Retrieve the active navigation graph from the scene registry.
        if (GraphEdgeRegistry.Instance == null)
        {
            Debug.LogError("[BarricadeEventInterceptor] GraphEdgeRegistry not found in scene!");
            enabled = false;
            return;
        }

        _graph = GraphEdgeRegistry.Instance.Graph;

        if (_graph == null)
        {
            Debug.LogError("[BarricadeEventInterceptor] INavigationGraph is null in registry!");
            enabled = false;
            return;
        }

        // Subscribe to the recalculation completion event so we can clear the
        // dirty flag and fire any deferred recalculation.
        _graph.OnRecalculationComplete += HandleRecalculationComplete;

        Debug.Log($"[BarricadeEventInterceptor] '{name}' initialised. " +
                  $"Monitoring {_graph.GetAllEdges().Count} graph edges.");
    }

    private void OnDestroy()
    {
        // Unsubscribe from the event to prevent memory leaks / null-ref calls
        // after the barricade GameObject is destroyed.
        if (_graph != null)
            _graph.OnRecalculationComplete -= HandleRecalculationComplete;

        // Restore all edges this barricade had severed before it was destroyed.
        RestoreAllSeveredEdges();
    }

    // ── Public Event Receivers ────────────────────────────────────────────────

    /// <summary>
    /// Called by BarricadePhysics via SendMessage when the barricade's
    /// Rigidbody velocity drops below the settle threshold.
    ///
    /// This is the ENTRY POINT for the edge detection algorithm.
    /// Using SendMessage (not a direct reference) keeps GV and IS scripts
    /// loosely coupled — BarricadePhysics does not need to know about
    /// BarricadeEventInterceptor's existence.
    /// </summary>
    public void OnBarricadeSettled()
    {
        Debug.Log($"[BarricadeEventInterceptor] Barricade '{name}' settled. Running edge scan...");
        RunEdgeScan();
    }

    // ── Core Algorithm ────────────────────────────────────────────────────────

    /// <summary>
    /// STAGE 1 + 2 + 3: Scans all graph edges for intersection with this
    /// barricade's collider, then mutates the graph accordingly.
    ///
    /// Time Complexity: O(E) where E = total graph edges
    ///   - One Physics.BoxCast per edge
    ///   - One HashSet lookup per edge O(1)
    ///   - Total: O(E)
    /// </summary>
    private void RunEdgeScan()
    {
        IReadOnlyList<GraphEdge> allEdges = _graph.GetAllEdges();

        // Build the NEW set of edges blocked by this barricade's current position.
        var newlyBlockedKeys = new HashSet<(int, int)>();

        foreach (GraphEdge edge in allEdges)
        {
            if (IsEdgeBlockedByThisBarricade(edge))
            {
                newlyBlockedKeys.Add(edge.Key);

                if (debugDrawCasts)
                    Debug.DrawLine(edge.WorldPositionA, edge.WorldPositionB, debugSeveredColor, 2f);
            }
            else
            {
                if (debugDrawCasts)
                    Debug.DrawLine(edge.WorldPositionA, edge.WorldPositionB, debugEdgeColor, 2f);
            }
        }

        // ── STAGE 3: Graph Mutation ───────────────────────────────────────────

        bool graphChanged = false;

        // Find edges that are NEWLY blocked (not in previous severed set).
        foreach (var key in newlyBlockedKeys)
        {
            if (!_severedByThisBarricade.Contains(key))
            {
                _graph.SeverEdge(key.Item1, key.Item2);
                _severedByThisBarricade.Add(key);
                graphChanged = true;

                Debug.Log($"[BarricadeEventInterceptor] NEW block: edge ({key.Item1} ↔ {key.Item2})");
            }
        }

        // Find edges that were previously blocked by this barricade but are now clear.
        var edgesToRestore = new List<(int, int)>();
        foreach (var key in _severedByThisBarricade)
        {
            if (!newlyBlockedKeys.Contains(key))
                edgesToRestore.Add(key);
        }

        foreach (var key in edgesToRestore)
        {
            _graph.RestoreEdge(key.Item1, key.Item2);
            _severedByThisBarricade.Remove(key);
            graphChanged = true;

            Debug.Log($"[BarricadeEventInterceptor] RESTORED: edge ({key.Item1} ↔ {key.Item2})");
        }

        // ── STAGE 4: Trigger Recalculation ───────────────────────────────────

        if (graphChanged)
            TriggerRecalculationGuarded();
        else
            Debug.Log("[BarricadeEventInterceptor] No graph changes detected — no recalculation needed.");
    }

    /// <summary>
    /// Tests whether the line segment of a given graph edge is physically
    /// blocked by this barricade's current collider position.
    ///
    /// METHOD: Physics.BoxCast from edgeA toward edgeB.
    ///   - The box half-extents approximate the corridor width a guard would
    ///     traverse. If the barricade's collider is anywhere along that corridor
    ///     segment, the BoxCast will hit it.
    ///   - We check the specific instance ID of THIS barricade's collider to
    ///     avoid false positives from other objects in the scene.
    ///
    /// ALTERNATIVE CONSIDERED: LineSegment vs AABB math (cheaper CPU-wise)
    ///   - Pure math check would avoid PhysX overhead.
    ///   - However, Physics.BoxCast correctly accounts for the collider's actual
    ///     shape, rotation, and physics layer, making it more accurate for
    ///     irregular barricade orientations after being pushed at an angle.
    /// </summary>
    private bool IsEdgeBlockedByThisBarricade(GraphEdge edge)
    {
        Vector3 origin    = edge.WorldPositionA;
        Vector3 direction = edge.WorldPositionB - edge.WorldPositionA;
        float   distance  = direction.magnitude;

        if (distance < 0.001f) return false; // degenerate edge

        direction.Normalize();

        // Cast a box from node A toward node B.
        // The box represents the "walkable corridor" volume along this edge.
        RaycastHit[] hits = Physics.BoxCastAll(
            center:    origin,
            halfExtents: castHalfExtents,
            direction: direction,
            orientation: Quaternion.LookRotation(direction),
            maxDistance: distance,
            layerMask: barricadeLayerMask
        );

        // Check if ANY hit belongs to THIS barricade's collider specifically.
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == _collider)
                return true;
        }

        return false;
    }

    // ── Recalculation + Loop Guard ─────────────────────────────────────────────

    /// <summary>
    /// STAGE 4 — Triggers pathfinding recalculation with infinite-loop protection.
    ///
    /// LOOP PREVENTION STRATEGY:
    ///   Problem: If recalculation fires an event that causes another barricade
    ///   event, which fires another recalculation, etc. → infinite loop.
    ///
    ///   Solution: Boolean dirty flag `_isRecalculating`.
    ///   - Set TRUE before calling TriggerPathRecalculation().
    ///   - A* calls back OnRecalculationComplete → set FALSE.
    ///   - If a second event fires while _isRecalculating == TRUE, set
    ///     _deferredRecalcPending = TRUE but do NOT trigger immediately.
    ///   - When OnRecalculationComplete fires → check _deferredRecalcPending
    ///     → if true, fire one additional recalculation.
    ///   This guarantees at most ONE outstanding recalculation at any time.
    /// </summary>
    private void TriggerRecalculationGuarded()
    {
        if (_isRecalculating)
        {
            // A recalculation is already in progress. Queue a deferred trigger.
            _deferredRecalcPending = true;
            Debug.Log("[BarricadeEventInterceptor] Recalculation in progress — deferred trigger queued.");
            return;
        }

        _isRecalculating = true;
        _deferredRecalcPending = false;

        Debug.Log("[BarricadeEventInterceptor] Triggering A* path recalculation...");
        _graph.TriggerPathRecalculation();
    }

    /// <summary>
    /// Callback from the graph's OnRecalculationComplete event.
    /// Clears the dirty flag and fires any deferred recalculation.
    /// </summary>
    private void HandleRecalculationComplete()
    {
        _isRecalculating = false;
        Debug.Log("[BarricadeEventInterceptor] Recalculation COMPLETE. Dirty flag cleared.");

        if (_deferredRecalcPending)
        {
            Debug.Log("[BarricadeEventInterceptor] Firing deferred recalculation.");
            TriggerRecalculationGuarded();
        }
    }

    // ── Cleanup ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Restores all edges severed by this barricade.
    /// Called when the barricade is destroyed (e.g. scene cleanup).
    /// </summary>
    private void RestoreAllSeveredEdges()
    {
        if (_graph == null) return;

        foreach (var key in _severedByThisBarricade)
        {
            _graph.RestoreEdge(key.Item1, key.Item2);
            Debug.Log($"[BarricadeEventInterceptor] Cleanup — restored edge ({key.Item1} ↔ {key.Item2})");
        }

        _severedByThisBarricade.Clear();

        if (_severedByThisBarricade.Count == 0 == false)
            _graph.TriggerPathRecalculation();
    }

#if UNITY_EDITOR
    /// <summary>
    /// Scene View debug: highlights which edges this barricade currently severs.
    /// Green = active edge, Red = severed by this barricade.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (_graph == null) return;

        foreach (GraphEdge edge in _graph.GetAllEdges())
        {
            bool severed = _severedByThisBarricade.Contains(edge.Key);
            Gizmos.color = severed ? Color.red : Color.green;
            Gizmos.DrawLine(edge.WorldPositionA, edge.WorldPositionB);
        }
    }
#endif
}
