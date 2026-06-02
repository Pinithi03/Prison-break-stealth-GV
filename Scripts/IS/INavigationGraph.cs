// =============================================================================
// INavigationGraph.cs
// Student 2 — IS (SE3062) | Prison Break: Silent Escape
// Role: Dynamic Adaptation & Event Interception
//
// Description:
//   Interface contract that decouples S2's edge-severing IS logic from S1's
//   concrete graph implementation. S2's BarricadeEventInterceptor only depends
//   on this interface — never on S1's specific class.
//
//   This follows the Dependency Inversion Principle:
//     "High-level modules (edge severing logic) should not depend on
//     low-level modules (graph storage format). Both should depend on
//     abstractions (this interface)."
//
//   Swap Strategy:
//     Development: MockNavigationGraph implements this → S2 tests in isolation
//     Integration: S1's real NavGraphExtractor implements this → plug in the
//                  real graph with ZERO changes to BarricadeEventInterceptor
//
// =============================================================================

namespace PrisonBreak.IS
{
    /// <summary>
    /// Contract for any navigation graph used by the prison AI system.
    /// S1 implements this. S2 calls it. S3 listens to it for recalculation.
    /// </summary>
    public interface INavigationGraph
    {
        // ── Graph Mutation ────────────────────────────────────────────────────

        /// <summary>
        /// Removes the traversal edge between two nodes.
        /// After severing, pathfinding algorithms cannot route through this edge.
        /// </summary>
        /// <param name="nodeA">ID of the first node.</param>
        /// <param name="nodeB">ID of the second node (edge is bidirectional).</param>
        void SeverEdge(int nodeA, int nodeB);

        /// <summary>
        /// Restores a previously severed edge.
        /// After restoring, pathfinding can route through this edge again.
        /// </summary>
        /// <param name="nodeA">ID of the first node.</param>
        /// <param name="nodeB">ID of the second node.</param>
        void RestoreEdge(int nodeA, int nodeB);

        /// <summary>
        /// Returns true if the edge between nodeA and nodeB is currently active
        /// (not severed). Used by edge detection to skip already-severed edges.
        /// </summary>
        bool IsEdgeActive(int nodeA, int nodeB);

        // ── Pathfinding Events ────────────────────────────────────────────────

        /// <summary>
        /// Signals to the pathfinding system (S3 — A*) that the graph has
        /// changed and all active guard paths should be recalculated.
        /// S3's pathfinding implementation subscribes to handle this.
        /// </summary>
        void TriggerPathRecalculation();

        /// <summary>
        /// Event raised after TriggerPathRecalculation() completes.
        /// S2's BarricadeEventInterceptor subscribes to this to clear its
        /// dirty flag and allow future recalculation triggers.
        /// </summary>
        event System.Action OnRecalculationComplete;

        // ── Graph Query ───────────────────────────────────────────────────────

        /// <summary>
        /// Returns all graph edges as node-pair tuples.
        /// Used by BarricadeEventInterceptor to iterate edges for intersection tests.
        /// </summary>
        System.Collections.Generic.IReadOnlyList<GraphEdge> GetAllEdges();
    }

    /// <summary>
    /// Represents a single undirected edge in the navigation graph.
    /// Stores both node IDs and their 3D world positions for intersection math.
    /// </summary>
    [System.Serializable]
    public struct GraphEdge
    {
        public int NodeA;
        public int NodeB;
        public UnityEngine.Vector3 WorldPositionA;
        public UnityEngine.Vector3 WorldPositionB;

        public GraphEdge(int nodeA, int nodeB, UnityEngine.Vector3 posA, UnityEngine.Vector3 posB)
        {
            NodeA = nodeA;
            NodeB = nodeB;
            WorldPositionA = posA;
            WorldPositionB = posB;
        }

        /// <summary>
        /// Normalised key for use in HashSets — ensures (1,2) == (2,1).
        /// </summary>
        public (int, int) Key => NodeA < NodeB ? (NodeA, NodeB) : (NodeB, NodeA);
    }
}
