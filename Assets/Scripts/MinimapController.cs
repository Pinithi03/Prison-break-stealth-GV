using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Minimap System — Student 4, GV Module (SE3032)
///
/// Creates a professional top-down minimap in the bottom-right corner.
/// Shows:
///   - Prison layout (from orthographic camera)
///   - Player position (white dot)
///   - Guard 1 position (red dot, pulsing when suspicious)
///   - Guard 2 position (orange dot)
///   - Current guard path (coloured line on minimap)
///   - North indicator
///
/// No external assets required — fully procedural.
/// </summary>
public class MinimapController : MonoBehaviour
{
    [Header("References")]
    public Transform        player;
    public Transform        guard1Transform;
    public Transform        guard2Transform;
    public GraphReal        graph;
    public GuardController  guard1;

    [Header("Minimap Settings")]
    public float mapSize      = 220f;   // UI size in pixels
    public float cameraHeight = 45f;    // height of ortho cam above scene
    public float orthoSize    = 18f;    // ortho cam coverage area

    [Header("Marker Sizes")]
    public float playerMarkerSize = 14f;
    public float guardMarkerSize  = 12f;
    public float nodeMarkerSize   = 6f;

    // ── Internal ──────────────────────────────────────────────────────────
    private Camera          minimapCam;
    private RenderTexture   minimapRT;
    private RawImage        minimapImage;
    private RectTransform   minimapRect;
    private GameObject      playerDot;
    private GameObject      guard1Dot;
    private GameObject      guard2Dot;
    private List<GameObject> nodeMarkers = new List<GameObject>();
    private Canvas          minimapCanvas;

    // Path line on minimap (projected)
    private List<GameObject> pathDots = new List<GameObject>();

    // Colors
    private readonly Color playerColor = new Color(1f, 1f, 1f, 1f);
    private readonly Color guard1Color = new Color(0.95f, 0.2f, 0.2f, 1f);
    private readonly Color guard2Color = new Color(0.98f, 0.65f, 0.1f, 1f);
    private readonly Color nodeColor   = new Color(1f, 1f, 0f, 0.5f);
    private readonly Color bgColor     = new Color(0.05f, 0.08f, 0.12f, 0.95f);
    private readonly Color borderColor = new Color(0.3f, 0.5f, 0.8f, 1f);

    private float pulseTimer = 0f;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        AutoFindReferences();
        BuildMinimapCamera();
        BuildMinimapUI();
        BuildMarkers();
        Debug.Log("[Minimap] Minimap system ACTIVE");
    }

    void AutoFindReferences()
    {
        if (player       == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }
        if (guard1Transform == null && guard1 != null)
            guard1Transform = guard1.transform;
        if (guard2Transform == null)
        {
            GuardController2 g2 = FindObjectOfType<GuardController2>();
            if (g2 != null) guard2Transform = g2.transform;
        }
        if (graph == null) graph = FindObjectOfType<GraphReal>();
    }

    // ── Camera ────────────────────────────────────────────────────────────
    void BuildMinimapCamera()
    {
        GameObject camGO = new GameObject("MinimapCamera");
        camGO.transform.SetParent(transform);

        minimapCam                  = camGO.AddComponent<Camera>();
        minimapCam.orthographic     = true;
        minimapCam.orthographicSize = orthoSize;
        minimapCam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        minimapCam.clearFlags       = CameraClearFlags.SolidColor;
        minimapCam.backgroundColor  = new Color(0.05f, 0.08f, 0.14f, 1f);
        minimapCam.cullingMask      = ~(1 << LayerMask.NameToLayer("UI"));
        minimapCam.depth            = -1;

        // Create RenderTexture
        minimapRT = new RenderTexture((int)mapSize * 2, (int)mapSize * 2, 16);
        minimapCam.targetTexture = minimapRT;
    }

    // ── UI ────────────────────────────────────────────────────────────────
    void BuildMinimapUI()
    {
        // Canvas
        GameObject canvasGO = new GameObject("MinimapCanvas");
        minimapCanvas = canvasGO.AddComponent<Canvas>();
        minimapCanvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        minimapCanvas.sortingOrder = 9;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Single consistent formula for ALL elements ─────────────────────
        // All: anchor=(1,0) bottom-right, pivot=(1,0) bottom-right
        // pos = (-margin, margin) → right-to-left x, bottom-to-top y
        float margin = 10f;

        // 1. Blue border frame (largest, drawn first = behind everything)
        GameObject border = MakeRect(canvasGO, "MinimapBorder", borderColor,
            margin - 3f, margin - 3f,
            mapSize + 6f, mapSize + 6f);
        border.transform.SetAsFirstSibling();

        // 2. Dark background (exact map size)
        MakeRect(canvasGO, "MinimapBG", bgColor,
            margin, margin,
            mapSize, mapSize);

        // 3. RawImage — camera render texture (exact map size, on top of BG)
        GameObject mapGO = new GameObject("MinimapImage", typeof(RectTransform), typeof(RawImage));
        mapGO.transform.SetParent(canvasGO.transform, false);
        minimapImage = mapGO.GetComponent<RawImage>();
        minimapImage.texture = minimapRT;
        RectTransform rt = mapGO.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(1f, 0f);
        rt.anchorMax        = new Vector2(1f, 0f);
        rt.pivot            = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-margin, margin);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, mapSize);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,   mapSize);
        minimapRect = rt;

        // 4. Title label directly above the minimap
        GameObject titleGO = new GameObject("MinimapTitle", typeof(RectTransform), typeof(Text));
        titleGO.transform.SetParent(canvasGO.transform, false);
        RectTransform trt = titleGO.GetComponent<RectTransform>();
        trt.anchorMin        = new Vector2(1f, 0f);
        trt.anchorMax        = new Vector2(1f, 0f);
        trt.pivot            = new Vector2(1f, 0f);
        trt.anchoredPosition = new Vector2(-margin, margin + mapSize + 4f);
        trt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, mapSize);
        trt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 16f);
        Text t = titleGO.GetComponent<Text>();
        t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text      = "MINIMAP  |  \u25cf Player  \u25cf Guard 1  \u25cf Guard 2";
        t.fontSize  = 9;
        t.fontStyle = FontStyle.Bold;
        t.color     = new Color(0.6f, 0.7f, 0.9f, 1f);
        t.alignment = TextAnchor.MiddleRight;
    }

    // Creates a simple colored rectangle anchored to bottom-right
    GameObject MakeRect(GameObject parent, string name, Color color,
        float rightMargin, float bottomMargin, float w, float h)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent.transform, false);
        go.GetComponent<Image>().color = color;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(1f, 0f);
        rt.anchorMax        = new Vector2(1f, 0f);
        rt.pivot            = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-rightMargin, bottomMargin);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,   h);
        return go;
    }

    void BuildLegend(GameObject parent)
    {
        string[] labels = { "● Player", "● Guard 1", "● Guard 2" };
        Color[]  colors = { playerColor, guard1Color, guard2Color };

        for (int i = 0; i < labels.Length; i++)
        {
            GameObject lGO = new GameObject($"Legend_{i}", typeof(RectTransform), typeof(Text));
            lGO.transform.SetParent(parent.transform, false);
            RectTransform rt = lGO.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(1f, 0f);
            rt.anchorMax        = new Vector2(1f, 0f);
            rt.pivot            = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-10f, mapSize + 34f + i * 14f);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, mapSize);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 14f);
            Text t = lGO.GetComponent<Text>();
            t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text      = labels[i];
            t.fontSize  = 9;
            t.color     = colors[i];
            t.alignment = TextAnchor.MiddleRight;
        }
    }

    // ── Markers ───────────────────────────────────────────────────────────
    void BuildMarkers()
    {
        playerDot = CreateMarker("PlayerDot", playerColor, playerMarkerSize);
        guard1Dot = CreateMarker("Guard1Dot", guard1Color, guardMarkerSize);
        guard2Dot = CreateMarker("Guard2Dot", guard2Color, guardMarkerSize);

        // Graph node dots
        if (graph != null)
        {
            foreach (var kvp in graph.nodePositions)
            {
                GameObject dot = CreateMarker($"Node_{kvp.Key}", nodeColor, nodeMarkerSize);
                nodeMarkers.Add(dot);
            }
        }
    }

    GameObject CreateMarker(string name, Color color, float size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(minimapCanvas.transform, false);
        go.GetComponent<Image>().color = color;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,   size);
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        return go;
    }

    // ─────────────────────────────────────────────────────────────────────
    void LateUpdate()
    {
        if (minimapCam == null) return;

        // Follow player (or map center)
        Vector3 camPos = player != null
            ? new Vector3(player.position.x, cameraHeight, player.position.z)
            : new Vector3(0f, cameraHeight, 0f);
        minimapCam.transform.position = camPos;

        pulseTimer += Time.deltaTime;

        // Update marker positions
        UpdateMarkerPosition(playerDot, player?.position ?? Vector3.zero);
        UpdateMarkerPosition(guard1Dot, guard1Transform?.position ?? Vector3.zero);
        UpdateMarkerPosition(guard2Dot, guard2Transform?.position ?? Vector3.zero);

        // Pulse guard1 dot
        if (guard1Dot != null)
        {
            float scale = 1f + Mathf.Sin(pulseTimer * 3f) * 0.2f;
            guard1Dot.transform.localScale = Vector3.one * scale;
        }

        // Update graph node markers
        if (graph != null)
        {
            int i = 0;
            foreach (var kvp in graph.nodePositions)
            {
                if (i < nodeMarkers.Count)
                    UpdateMarkerPosition(nodeMarkers[i], kvp.Value);
                i++;
            }
        }
    }

    void UpdateMarkerPosition(GameObject marker, Vector3 worldPos)
    {
        if (marker == null || minimapCam == null || minimapRect == null) return;

        Vector3 viewportPoint = minimapCam.WorldToViewportPoint(worldPos);

        // Map viewport (0-1) to minimap rect pixels
        Vector2 minimapPos = new Vector2(
            (viewportPoint.x - 0.5f) * mapSize,
            (viewportPoint.z - 0.5f) * mapSize
        );

        // Offset to minimap corner position
        Vector2 anchoredPos = minimapRect.anchoredPosition + minimapPos;
        marker.GetComponent<RectTransform>().anchoredPosition = anchoredPos;
    }

    // ── UI Helpers ────────────────────────────────────────────────────────
    GameObject CreateUIImage(GameObject parent, string name, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent.transform, false);
        go.GetComponent<Image>().color = color;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = anchorMin;
        rt.anchorMax        = anchorMax;
        rt.pivot            = new Vector2(1f, 0f);
        rt.anchoredPosition = pos;
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,   size.y);
        return go;
    }

    void CreateBorder(GameObject parent, float size)
    {
        GameObject go = new GameObject("MinimapBorder", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent.transform, false);
        go.GetComponent<Image>().color = borderColor;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(1f, 0f);
        rt.anchorMax        = new Vector2(1f, 0f);
        rt.pivot            = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-(mapSize * 0.5f + 10f) * 0.5f - 2,
                                           (mapSize * 0.5f + 10f) * 0.5f + 2);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size + 4);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,   size + 4);

        // Put behind minimap image
        go.transform.SetAsFirstSibling();
    }
}
