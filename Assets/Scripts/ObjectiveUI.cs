using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Automatically creates a top-right Objectives HUD panel at runtime.
/// Attach this to any GameObject in the scene (e.g. "ObjectiveUI").
/// No manual UI setup needed — everything is built in code.
/// </summary>
public class ObjectiveUI : MonoBehaviour
{
    // ── Colours ──────────────────────────────────────────────────────────────
    private readonly Color colCompleted = new Color(0.45f, 0.85f, 0.45f, 1f);   // green
    private readonly Color colCurrent   = new Color(1.00f, 0.92f, 0.30f, 1f);   // yellow
    private readonly Color colFuture    = new Color(0.55f, 0.55f, 0.55f, 1f);   // grey
    private readonly Color colPanel     = new Color(0.05f, 0.05f, 0.10f, 0.82f);
    private readonly Color colBorder    = new Color(0.20f, 0.60f, 1.00f, 0.70f);

    // ── Internal refs ────────────────────────────────────────────────────────
    private Canvas   canvas;
    private GameObject panelObj;

    private struct Row { public Text check; public Text label; }
    private List<Row> rows = new List<Row>();

    // ── Current-objective pulse animation ────────────────────────────────────
    private float pulseTimer = 0f;

    // ─────────────────────────────────────────────────────────────────────────
    void Start()
    {
        BuildUI();

        if (ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.OnObjectivesChanged += RefreshUI;
            RefreshUI();
        }
        else
        {
            Debug.LogWarning("[ObjectiveUI] ObjectiveManager not found in scene!");
        }
    }

    void OnDestroy()
    {
        if (ObjectiveManager.Instance != null)
            ObjectiveManager.Instance.OnObjectivesChanged -= RefreshUI;
    }

    // ─────────────────────────────────────────────────────────────────────────
    void Update()
    {
        // Pulse the current objective's text brightness
        if (ObjectiveManager.Instance == null) return;

        pulseTimer += Time.deltaTime * 2.5f;
        float brightness = 0.75f + Mathf.Sin(pulseTimer) * 0.25f;

        var currentObj = ObjectiveManager.Instance.GetCurrentObjective();
        if (currentObj == null) return;

        int idx = ObjectiveManager.Instance.objectives.IndexOf(currentObj);
        if (idx >= 0 && idx < rows.Count)
        {
            Color c = colCurrent;
            c.r *= brightness; c.g *= brightness; c.b *= brightness;
            rows[idx].label.color = c;
            rows[idx].check.color = c;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    void BuildUI()
    {
        if (ObjectiveManager.Instance == null) return;
        int count = ObjectiveManager.Instance.objectives.Count;

        // ── Canvas ────────────────────────────────────────────────────────────
        var canvasGO = new GameObject("ObjectiveCanvas");
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode    = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder  = 50;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Outer border glow panel ───────────────────────────────────────────
        float panelW  = 300f;
        float rowH    = 30f;
        float titleH  = 38f;
        float padding = 12f;
        float panelH  = titleH + rowH * count + padding * 2f;

        var borderGO = MakePanel(canvasGO.transform, "Border", panelW + 4f, panelH + 4f,
                                 new Vector2(1, 1), new Vector2(-10f, -10f), colBorder);

        panelObj = MakePanel(borderGO.transform, "ObjectivePanel", panelW, panelH,
                             new Vector2(0.5f, 0.5f), Vector2.zero, colPanel);

        // ── Title ─────────────────────────────────────────────────────────────
        float yOffset = -(padding + titleH * 0.5f);
        var titleText = MakeText(panelObj.transform, "Title", "OBJECTIVES",
                                 panelW - 16f, titleH, new Vector2(0.5f, 1f),
                                 new Vector2(0f, yOffset + titleH * 0.5f - padding));
        titleText.fontSize  = 16;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color     = new Color(0.85f, 0.92f, 1.00f, 1f);
        titleText.alignment = TextAnchor.MiddleCenter;

        // Separator line
        var sep = new GameObject("Separator");
        sep.transform.SetParent(panelObj.transform, false);
        var sepImg  = sep.AddComponent<Image>();
        sepImg.color = new Color(0.20f, 0.60f, 1.00f, 0.35f);
        var sepRect = sep.GetComponent<RectTransform>();
        sepRect.anchorMin       = new Vector2(0.5f, 1f);
        sepRect.anchorMax       = new Vector2(0.5f, 1f);
        sepRect.pivot           = new Vector2(0.5f, 1f);
        sepRect.anchoredPosition = new Vector2(0f, -(padding + titleH));
        sepRect.sizeDelta       = new Vector2(panelW - 20f, 1.5f);

        // ── Objective rows ────────────────────────────────────────────────────
        rows.Clear();
        float startY = -(padding + titleH + 2f);

        for (int i = 0; i < count; i++)
        {
            float rowY = startY - i * rowH - rowH * 0.5f;

            // Checkmark
            var checkText = MakeText(panelObj.transform, $"Check_{i}", "○",
                                     24f, rowH, new Vector2(0f, 1f),
                                     new Vector2(10f, rowY));
            checkText.fontSize  = 13;
            checkText.alignment = TextAnchor.MiddleCenter;

            var labelText = MakeText(panelObj.transform, $"Label_{i}",
                                     ObjectiveManager.Instance.objectives[i].displayText,
                                     panelW - 46f, rowH, new Vector2(0f, 1f),
                                     new Vector2(36f, rowY));
            labelText.fontSize  = 12;
            labelText.alignment = TextAnchor.MiddleLeft;

            rows.Add(new Row { check = checkText, label = labelText });
        }

        RefreshUI();
    }

    // ─────────────────────────────────────────────────────────────────────────
    public void RefreshUI()
    {
        if (ObjectiveManager.Instance == null) return;
        var objectives = ObjectiveManager.Instance.objectives;

        for (int i = 0; i < rows.Count && i < objectives.Count; i++)
        {
            var obj = objectives[i];
            var row = rows[i];

            if (obj.isCompleted)
            {
                row.check.text   = "✓";
                row.check.color  = colCompleted;
                row.label.color  = colCompleted;
                // Strike-through effect using rich text via a wrapper
                row.label.text   = "<color=#6dda6d>" + obj.displayText + "</color>";
                row.label.color  = colCompleted;
            }
            else if (obj.isCurrent)
            {
                row.check.text  = "▶";
                row.check.color = colCurrent;
                row.label.color = colCurrent;
                row.label.text  = obj.displayText;
            }
            else
            {
                row.check.text  = "○";
                row.check.color = colFuture;
                row.label.color = colFuture;
                row.label.text  = obj.displayText;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────
    static GameObject MakePanel(Transform parent, string name, float w, float h,
                                Vector2 anchor, Vector2 pos, Color color)
    {
        var go   = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img  = go.AddComponent<Image>();
        img.color = color;
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin       = anchor;
        rect.anchorMax       = anchor;
        rect.pivot           = anchor;
        rect.anchoredPosition = pos;
        rect.sizeDelta       = new Vector2(w, h);
        return go;
    }

    static Text MakeText(Transform parent, string name, string content,
                         float w, float h, Vector2 anchor, Vector2 pos)
    {
        var go   = new GameObject(name);
        go.transform.SetParent(parent, false);
        var txt  = go.AddComponent<Text>();
        txt.text = content;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (txt.font == null)
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin       = anchor;
        rect.anchorMax       = anchor;
        rect.pivot           = new Vector2(0f, 1f);
        rect.anchoredPosition = pos;
        rect.sizeDelta       = new Vector2(w, h);
        return txt;
    }
}
