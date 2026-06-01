using UnityEngine;

/// <summary>
/// Student 2 — IS Module (SE3062): Dynamic Adaptation & Event Interception
/// 
/// Attach this script to any Barricade GameObject in the scene.
/// Requirements on the same GameObject:
///   - Rigidbody   (NOT kinematic — allows the player to physically push it)
///   - BoxCollider (solid, not trigger — blocks movement and NavMesh)
///
/// How it works:
///   1. Player walks into the barricade → Rigidbody physics pushes it.
///   2. When the barricade settles (velocity drops below settleThreshold),
///      this script calls graph.BlockEdge(fromNodeId, toNodeId) to sever
///      the graph edge that runs through this corridor.
///   3. GraphReal fires its OnEdgeChanged event → guard AI recalculates path.
///   4. If the barricade is pushed back out of the way (Y angle returns close
///      to origin), graph.UnblockEdge() restores the edge.
///
/// To configure in Unity Inspector:
///   - fromNodeId / toNodeId : the two graph node IDs on either side of this barricade
///   - blockingDistance      : how far the barricade must move to count as "blocking"
///   - settleThreshold       : velocity magnitude below which barricade is "stopped"
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class BarricadePush : MonoBehaviour
{
    [Header("Graph Edge This Barricade Controls")]
    [Tooltip("Node ID on one side of the corridor")]
    public int fromNodeId = -1;
    [Tooltip("Node ID on the other side of the corridor")]
    public int toNodeId = -1;

    [Header("Blocking Detection")]
    [Tooltip("How many units the barricade must move from its start position to be considered 'blocking' the corridor")]
    public float blockingDistance = 0.8f;
    [Tooltip("Rigidbody speed below this value means the barricade has settled")]
    public float settleThreshold = 0.05f;
    [Tooltip("Seconds to wait after settling before checking block state (prevents rapid toggling)")]
    public float settleDelay = 0.4f;

    // ── Internal state ────────────────────────────────────────────────────────
    private GraphReal graph;
    private Rigidbody rb;
    private Vector3 startPosition;

    private bool isCurrentlyBlocking = false;
    private bool wasMoving          = false;
    private float settleTimer       = 0f;

    // ─────────────────────────────────────────────────────────────────────────

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Freeze rotation so barricade only slides, not tumbles
        rb.freezeRotation = true;

        // Find the GraphReal in the scene automatically
        graph = FindObjectOfType<GraphReal>();
        if (graph == null)
        {
            Debug.LogWarning($"[BarricadePush] '{gameObject.name}': No GraphReal found in scene! " +
                             "Edge severing will not work.");
            return;
        }

        if (fromNodeId < 0 || toNodeId < 0)
        {
            Debug.LogWarning($"[BarricadePush] '{gameObject.name}': fromNodeId or toNodeId is not set! " +
                             "Please assign node IDs in the Inspector.");
        }

        startPosition = transform.position;

        Debug.Log($"[BarricadePush] '{gameObject.name}' ready — controls edge {fromNodeId} → {toNodeId}. " +
                  $"Blocking distance: {blockingDistance}m");
    }

    void FixedUpdate()
    {
        if (graph == null) return;

        float speed = rb.velocity.magnitude;
        bool moving = speed > settleThreshold;

        if (moving)
        {
            // Barricade is being pushed — reset settle timer
            wasMoving   = true;
            settleTimer = 0f;
        }
        else if (wasMoving)
        {
            // Just stopped — start settle countdown
            settleTimer += Time.fixedDeltaTime;

            if (settleTimer >= settleDelay)
            {
                wasMoving = false;
                settleTimer = 0f;
                EvaluateBlockState();
            }
        }
    }

    /// <summary>
    /// Check how far the barricade has moved from its start position.
    /// If it has moved far enough to block the corridor, sever the edge.
    /// If it has moved back close to start, restore the edge.
    /// </summary>
    private void EvaluateBlockState()
    {
        float displacement = Vector3.Distance(transform.position, startPosition);
        bool shouldBlock   = displacement >= blockingDistance;

        if (shouldBlock && !isCurrentlyBlocking)
        {
            // ── Barricade placed: sever the graph edge ──────────────────────
            isCurrentlyBlocking = true;
            graph.BlockEdge(fromNodeId, toNodeId);
            Debug.Log($"[BarricadePush] '{gameObject.name}' is blocking corridor. " +
                      $"Edge {fromNodeId} → {toNodeId} severed. " +
                      $"(Displacement: {displacement:F2}m)");
        }
        else if (!shouldBlock && isCurrentlyBlocking)
        {
            // ── Barricade removed: restore the graph edge ───────────────────
            isCurrentlyBlocking = false;
            graph.UnblockEdge(fromNodeId, toNodeId);
            Debug.Log($"[BarricadePush] '{gameObject.name}' cleared from corridor. " +
                      $"Edge {fromNodeId} → {toNodeId} restored. " +
                      $"(Displacement: {displacement:F2}m)");
        }
    }

    /// <summary>
    /// Visual debug — draw a line between start position and current position
    /// and show a sphere if blocking.
    /// </summary>
    void OnDrawGizmos()
    {
        // Show start position
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(Application.isPlaying ? startPosition : transform.position, 0.2f);

        if (!Application.isPlaying) return;

        // Show current blocking state
        Gizmos.color = isCurrentlyBlocking ? Color.red : Color.green;
        Gizmos.DrawLine(startPosition, transform.position);
        Gizmos.DrawWireSphere(transform.position, 0.25f);
    }
}
