using UnityEngine;

/// <summary>
/// Attach this to Door GameObjects to connect door open/close state
/// to the navigation graph. When the door closes it blocks the specified
/// edge in the GraphReal; when it opens it unblocks the edge.
/// This is a minimal Student 2 integration for dynamic adaptation.
/// </summary>
public class DoorGraphLink : MonoBehaviour
{
    public DoorController doorController;
    public GraphReal graph;

    [Tooltip("Edge from node ID (source)")]
    public int fromNodeId = -1;
    [Tooltip("Edge to node ID (destination)")]
    public int toNodeId = -1;

    private bool lastOpenedState = false;

    void Start()
    {
        if (doorController == null) doorController = GetComponent<DoorController>();
        if (graph == null) graph = FindObjectOfType<GraphReal>();

        if (doorController == null || graph == null)
        {
            Debug.LogWarning("DoorGraphLink: Missing DoorController or GraphReal reference on " + gameObject.name);
            return;
        }

        lastOpenedState = doorController.isOpened;
        // Ensure graph reflects initial state
        if (lastOpenedState)
            graph.UnblockEdge(fromNodeId, toNodeId);
        else
            graph.BlockEdge(fromNodeId, toNodeId);
    }

    void Update()
    {
        if (doorController == null || graph == null) return;

        if (doorController.isOpened != lastOpenedState)
        {
            lastOpenedState = doorController.isOpened;

            if (lastOpenedState)
            {
                graph.UnblockEdge(fromNodeId, toNodeId);
                Debug.Log($"DoorGraphLink: Unblocked edge {fromNodeId} -> {toNodeId} (door opened)");
            }
            else
            {
                graph.BlockEdge(fromNodeId, toNodeId);
                Debug.Log($"DoorGraphLink: Blocked edge {fromNodeId} -> {toNodeId} (door closed)");
            }
        }
    }
}
