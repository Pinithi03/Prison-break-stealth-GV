using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Algorithm Switcher UI — Student 4, IS + GV Modules
///
/// Creates a professional in-game HUD panel that lets the viva panel:
///   - Switch Guard 1 between BFS / UCS / A* with one click or key press
///   - See live stats: path length, cost, nodes explored
///   - See which algorithm is active (highlighted button)
///
/// Keyboard: 1 = BFS,  2 = UCS,  3 = A*
/// </summary>
public class AlgorithmSwitcherUI : MonoBehaviour
{
    [Header("References")]
    public GuardController guard1;
    public GraphReal        graph;

    // ── Internal UI refs ──────────────────────────────────────────────────
    private Canvas     canvas;
    private GameObject panel;
    private Button[]   algButtons    = new Button[3];
    private Text[]     algLabels     = new Text[3];
    private Text       statsText;
    private Text       pathText;
    private Text       titleText;

    // Algorithm button configs
    private readonly string[] algNames   = { "BFS",     "UCS",     "A*"       };
    private readonly Color[]  algColors  = {
        new Color(0.13f, 0.59f, 0.95f, 1f),   // blue   = BFS
        new Color(0.30f, 0.85f, 0.39f, 1f),   // green  = UCS
        new Color(0.98f, 0.45f, 0.09f, 1f),   // orange = A*
    };
    private readonly Color inactiveColor = new Color(0.15f, 0.15f, 0.20f, 0.9f);

    private GuardController.SearchAlgorithm currentAlg = GuardController.SearchAlgorithm.BFS;
    private float statsUpdateTimer = 0f;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (guard1 == null) guard1 = FindObjectOfType<GuardController>();
        BuildUI();
        SelectAlgorithm(0); // default BFS
    }

    void Update()
    {
        // Keyboard shortcuts
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectAlgorithm(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectAlgorithm(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectAlgorithm(2);

        // Refresh stats every 0.5s
        statsUpdateTimer += Time.deltaTime;
        if (statsUpdateTimer >= 0.5f) { RefreshStats(); statsUpdateTimer = 0f; }
    }

    // ── UI Construction ───────────────────────────────────────────────────
    void BuildUI()
    {
        // ── Canvas ─────────────────────────────────────────────────────
        GameObject canvasGO = new GameObject("AlgorithmSwitcherCanvas");
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Main panel (TOP-LEFT corner) ────────────────────────────────
        panel = new GameObject("SwitcherPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasGO.transform, false);
        RectTransform prt = panel.GetComponent<RectTransform>();
        prt.anchorMin        = new Vector2(0f, 1f);   // top-left
        prt.anchorMax        = new Vector2(0f, 1f);
        prt.pivot            = new Vector2(0f, 1f);
        prt.anchoredPosition = new Vector2(10f, -10f); // 10px from top-left
        prt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 270);
        prt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,   290);
        SetColor(panel, new Color(0.07f, 0.07f, 0.12f, 0.92f));
        AddRoundedBorder(panel, new Color(0.3f, 0.3f, 0.5f, 0.8f));

        // ── Title ─────────────────────────────────────────────────────────
        GameObject titleGO = CreateText(panel, "Title", "🧠  SEARCH ALGORITHM",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -18), 13, FontStyle.Bold, Color.white);
        titleText = titleGO.GetComponent<Text>();

        // Subtitle
        CreateText(panel, "Sub", "Guard 1 pathfinding | Keys: 1  2  3",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -40), 9, FontStyle.Normal, new Color(0.6f, 0.6f, 0.7f, 1f));

        // Divider
        CreateDivider(panel, -58);

        // ── Algorithm buttons ─────────────────────────────────────────────
        for (int i = 0; i < 3; i++)
        {
            int idx = i; // capture for lambda
            GameObject btnGO = CreateButtonObject(panel,
                algNames[i], new Vector2(0, -72 - i * 48));
            algButtons[i] = btnGO.GetComponent<Button>();
            algLabels[i]  = btnGO.GetComponentInChildren<Text>();
            algButtons[i].onClick.AddListener(() => SelectAlgorithm(idx));
        }

        // Divider
        CreateDivider(panel, -222);

        // ── Stats panel ───────────────────────────────────────────────────
        GameObject statsGO = CreateText(panel, "Stats",
            "Path Length: —\nCost:  —\nExplored: —",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -242), 10, FontStyle.Normal,
            new Color(0.85f, 0.85f, 0.95f, 1f));
        statsText = statsGO.GetComponent<Text>();
        statsText.alignment = TextAnchor.MiddleCenter;
        statsText.lineSpacing = 1.4f;

        // ── Path display ──────────────────────────────────────────────────
        GameObject pathGO = CreateText(panel, "PathDisplay", "",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0, 12), 9, FontStyle.Normal,
            new Color(0.5f, 0.9f, 0.5f, 1f));
        pathText = pathGO.GetComponent<Text>();
        pathText.alignment = TextAnchor.MiddleCenter;
    }

    // ── Select algorithm ──────────────────────────────────────────────────
    void SelectAlgorithm(int idx)
    {
        currentAlg = (GuardController.SearchAlgorithm)idx;
        if (guard1 != null) guard1.SwitchAlgorithm(currentAlg);

        // Update button visuals
        for (int i = 0; i < 3; i++)
        {
            if (algButtons[i] == null) continue;
            bool active = (i == idx);
            ColorBlock cb = algButtons[i].colors;
            cb.normalColor      = active ? algColors[i] : inactiveColor;
            cb.highlightedColor = active ? algColors[i] * 1.2f : new Color(0.25f, 0.25f, 0.35f);
            cb.pressedColor     = algColors[i] * 0.7f;
            algButtons[i].colors = cb;

            if (algLabels[i] != null)
            {
                algLabels[i].color      = active ? Color.white : new Color(0.5f, 0.5f, 0.6f);
                algLabels[i].fontStyle  = active ? FontStyle.Bold : FontStyle.Normal;
                string suffix = i == 0 ? "  [Shortest hops]"
                              : i == 1 ? "  [Lowest cost]"
                              :          "  [Optimal + heuristic]";
                algLabels[i].text = (active ? "▶  " : "    ") + algNames[i] + suffix;
            }
        }

        RefreshStats();
    }

    // ── Stats refresh ─────────────────────────────────────────────────────
    void RefreshStats()
    {
        if (guard1 == null || statsText == null) return;

        string costStr = (currentAlg == GuardController.SearchAlgorithm.UCS)
            ? $"{guard1.lastPathCost:F1} units"
            : "N/A (hops)";

        statsText.text =
            $"Path Length:  <b>{guard1.lastPathLength}</b> nodes\n" +
            $"Cost:             <b>{costStr}</b>\n" +
            $"Explored:       <b>{guard1.lastNodesExplored}</b> nodes";

        // Show path
        if (guard1.lastCalculatedPath != null && guard1.lastCalculatedPath.Count > 0)
            pathText.text = "→ " + string.Join(" → ", guard1.lastCalculatedPath);
        else
            pathText.text = "No path";
    }

    // ── UI Helpers ────────────────────────────────────────────────────────
    GameObject CreatePanel(GameObject parent, string name,
        Vector2 pivot, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent.transform, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.pivot       = pivot;
        rt.anchorMin   = anchorMin;
        rt.anchorMax   = anchorMax;
        rt.anchoredPosition = anchoredPos;
        return go;
    }

    void SetRectSize(GameObject go, float w, float h)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,   h);
    }

    void SetColor(GameObject go, Color c)
    {
        Image img = go.GetComponent<Image>();
        if (img != null) img.color = c;
    }

    void AddRoundedBorder(GameObject go, Color c)
    {
        GameObject border = new GameObject("Border", typeof(RectTransform), typeof(Outline));
        border.transform.SetParent(go.transform, false);
        Outline ol = border.GetComponent<Outline>();
        ol.effectColor    = c;
        ol.effectDistance = new Vector2(1.5f, -1.5f);
    }

    GameObject CreateButtonObject(GameObject parent, string label, Vector2 pos)
    {
        GameObject go = new GameObject("Btn_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent.transform, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 1f);
        rt.anchorMax        = new Vector2(0.5f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = pos;
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 234);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,   40);

        go.GetComponent<Image>().color = inactiveColor;

        // Label
        GameObject lbl = new GameObject("Label", typeof(RectTransform), typeof(Text));
        lbl.transform.SetParent(go.transform, false);
        RectTransform lrt = lbl.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(8, 0); lrt.offsetMax = new Vector2(-8, 0);
        Text t = lbl.GetComponent<Text>();
        t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text      = "    " + label;
        t.fontSize  = 11;
        t.color     = new Color(0.5f, 0.5f, 0.6f);
        t.alignment = TextAnchor.MiddleLeft;

        return go;
    }

    GameObject CreateText(GameObject parent, string name, string content,
        Vector2 pivot, Vector2 anchor, Vector2 pos,
        int size, FontStyle style, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent.transform, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.pivot            = pivot;
        rt.anchorMin        = anchor;
        rt.anchorMax        = anchor;
        rt.anchoredPosition = pos;
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 240);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,   60);
        Text t = go.GetComponent<Text>();
        t.font          = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text          = content;
        t.fontSize      = size;
        t.fontStyle     = style;
        t.color         = color;
        t.alignment     = TextAnchor.MiddleCenter;
        t.supportRichText = true;
        return go;
    }

    void CreateDivider(GameObject parent, float yPos)
    {
        GameObject go = new GameObject("Divider", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent.transform, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 1f);
        rt.anchorMax        = new Vector2(0.5f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0, yPos);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 230);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,   1);
        go.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.5f, 0.5f);
    }
}
