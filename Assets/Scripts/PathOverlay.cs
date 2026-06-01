using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// In-Game Path Overlay — Student 4, GV Module (SE3032)
///
/// Draws the current IS algorithm path VISIBLY in the Game view
/// using LineRenderer components (unlike Debug.DrawLine which only
/// shows in the Scene view).
///
/// Shows:
///   - Guard 1 path (color = current algorithm color)
///   - Guard 2 path (teal)
///   - Node spheres at each path waypoint
///   - Algorithm label above each guard
///
/// Toggle: T key
/// </summary>
public class PathOverlay : MonoBehaviour
{
    [Header("References")]
    public GuardController  guard1;
    public GuardController2 guard2;
    public GraphReal        graph;

    [Header("Toggle")]
    public bool showOverlay  = true;
    public KeyCode toggleKey = KeyCode.T;

    [Header("Visual")]
    public float lineWidth     = 0.15f;
    public float nodeRadius    = 0.25f;
    public float lineHeight    = 0.3f;   // raise above floor

    // ── Runtime objects ───────────────────────────────────────────────────
    private LineRenderer  lr1, lr2;
    private List<GameObject> nodeSpheres = new List<GameObject>();
    private List<int>     lastPath1 = new List<int>();
    private List<int>     lastPath2 = new List<int>();

    // Algorithm colours
    private readonly Color bfsColor   = new Color(0.13f, 0.59f, 0.95f, 1f);
    private readonly Color ucsColor   = new Color(0.30f, 0.85f, 0.39f, 1f);
    private readonly Color astarColor = new Color(0.98f, 0.45f, 0.09f, 1f);
    private readonly Color guard2Color = new Color(0f,   0.85f, 0.75f, 1f);

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (guard1 == null) guard1 = FindObjectOfType<GuardController>();
        if (guard2 == null) guard2 = FindObjectOfType<GuardController2>();
        if (graph  == null) guard1?.TryGetComponent(out graph);

        lr1 = CreateLineRenderer("PathLine_Guard1", bfsColor);
        lr2 = CreateLineRenderer("PathLine_Guard2", guard2Color);

        Debug.Log("[PathOverlay] In-game path overlay ACTIVE — press T to toggle");
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            showOverlay = !showOverlay;
            lr1.enabled = showOverlay;
            lr2.enabled = showOverlay;
            SetSpheresActive(showOverlay);
            Debug.Log($"[PathOverlay] {(showOverlay ? "ON ✅" : "OFF ❌")}");
        }

        if (!showOverlay) return;

        // Update guard 1 path if it changed
        List<int> path1 = guard1?.lastCalculatedPath ?? new List<int>();
        if (!PathsEqual(path1, lastPath1))
        {
            lastPath1 = new List<int>(path1);
            Color c1  = GetAlgorithmColor(guard1?.activeAlgorithm
                         ?? GuardController.SearchAlgorithm.BFS);
            UpdateLineRenderer(lr1, path1, c1);
            RebuildNodeSpheres(path1, c1);
        }

        // Update guard 2 path if it changed
        List<int> path2 = guard2?.lastCalculatedPath ?? new List<int>();
        if (!PathsEqual(path2, lastPath2))
        {
            lastPath2 = new List<int>(path2);
            UpdateLineRenderer(lr2, path2, guard2Color);
        }
    }

    // ── LineRenderer helpers ──────────────────────────────────────────────
    void UpdateLineRenderer(LineRenderer lr, List<int> path, Color color)
    {
        if (graph == null || path == null || path.Count < 2)
        {
            lr.positionCount = 0;
            return;
        }

        lr.startColor  = color;
        lr.endColor    = new Color(color.r, color.g, color.b, 0.4f);
        lr.positionCount = path.Count;

        for (int i = 0; i < path.Count; i++)
        {
            if (!graph.nodePositions.ContainsKey(path[i])) continue;
            Vector3 pos = graph.nodePositions[path[i]];
            lr.SetPosition(i, new Vector3(pos.x, pos.y + lineHeight, pos.z));
        }
    }

    void RebuildNodeSpheres(List<int> path, Color color)
    {
        // Destroy old spheres
        foreach (var s in nodeSpheres)
            if (s != null) Destroy(s);
        nodeSpheres.Clear();

        if (graph == null || path == null) return;

        for (int i = 0; i < path.Count; i++)
        {
            if (!graph.nodePositions.ContainsKey(path[i])) continue;
            Vector3 pos = graph.nodePositions[path[i]];
            pos.y += lineHeight;

            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = $"PathNode_{i}";
            sphere.transform.position   = pos;
            sphere.transform.localScale = Vector3.one * nodeRadius;
            Destroy(sphere.GetComponent<Collider>()); // no physics

            // Apply color via material
            Renderer r = sphere.GetComponent<Renderer>();
            r.material = new Material(Shader.Find("Sprites/Default"));

            // First and last nodes are bigger and brighter
            if (i == 0)
            {
                r.material.color = Color.white;
                sphere.transform.localScale = Vector3.one * nodeRadius * 1.6f;
            }
            else if (i == path.Count - 1)
            {
                r.material.color = Color.yellow;
                sphere.transform.localScale = Vector3.one * nodeRadius * 1.6f;
            }
            else
            {
                r.material.color = new Color(color.r, color.g, color.b, 0.8f);
            }

            nodeSpheres.Add(sphere);
        }
    }

    LineRenderer CreateLineRenderer(string name, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform);
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.material        = new Material(Shader.Find("Sprites/Default"));
        lr.startWidth      = lineWidth;
        lr.endWidth        = lineWidth * 0.5f;
        lr.startColor      = color;
        lr.endColor        = new Color(color.r, color.g, color.b, 0.3f);
        lr.positionCount   = 0;
        lr.useWorldSpace   = true;
        lr.numCapVertices  = 8;
        lr.numCornerVertices = 4;
        return lr;
    }

    Color GetAlgorithmColor(GuardController.SearchAlgorithm alg)
    {
        switch (alg)
        {
            case GuardController.SearchAlgorithm.BFS:   return bfsColor;
            case GuardController.SearchAlgorithm.UCS:   return ucsColor;
            case GuardController.SearchAlgorithm.AStar: return astarColor;
            default: return bfsColor;
        }
    }

    void SetSpheresActive(bool active)
    {
        foreach (var s in nodeSpheres)
            if (s != null) s.SetActive(active);
    }

    bool PathsEqual(List<int> a, List<int> b)
    {
        if (a == null || b == null) return false;
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
            if (a[i] != b[i]) return false;
        return true;
    }
}
