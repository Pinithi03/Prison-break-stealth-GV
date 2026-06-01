using UnityEngine;

/// <summary>
/// Simple in-game GUI to toggle debug visuals.
/// Uses immediate-mode OnGUI for simplicity.
/// </summary>
public class DebugUI : MonoBehaviour
{
    public DebugVisualizer debugVisualizer;
    public PathVisualizer pathVisualizer;
    public FrontierVisualizer frontierVisualizer;

    void Start()
    {
        if (debugVisualizer == null) debugVisualizer = FindObjectOfType<DebugVisualizer>();
        if (pathVisualizer == null) pathVisualizer = FindObjectOfType<PathVisualizer>();
        if (frontierVisualizer == null) frontierVisualizer = FindObjectOfType<FrontierVisualizer>();
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 200, 200));
        GUILayout.Label("Debug Controls");

        if (debugVisualizer != null)
        {
            bool on = GUI.Toggle(new Rect(10, 30, 180, 20), debugVisualizer.showDebug, "Graph Debug (T)");
            if (on != debugVisualizer.showDebug) debugVisualizer.showDebug = on;
        }

        if (pathVisualizer != null)
        {
            bool has = GUI.Toggle(new Rect(10, 55, 180, 20), pathVisualizer != null && pathVisualizer.path != null && pathVisualizer.path.Count > 0, "Show Path");
            // no-op: existence of path controls rendering; users can clear path to hide
            if (!has && pathVisualizer != null) pathVisualizer.path = new System.Collections.Generic.List<int>();
        }

        if (frontierVisualizer != null)
        {
            // toggle by enabling/disabling component
            bool enabled = frontierVisualizer.enabled;
            bool on2 = GUI.Toggle(new Rect(10, 80, 180, 20), enabled, "Show Frontier");
            if (on2 != enabled) frontierVisualizer.enabled = on2;
        }

        GUILayout.EndArea();
    }
}
