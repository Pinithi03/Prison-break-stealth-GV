using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds and animates a premium Victory Screen at runtime.
/// Attach to any GameObject. Call VictoryScreen.Instance.Show() to trigger it.
/// No manual Canvas setup required.
/// </summary>
public class VictoryScreen : MonoBehaviour
{
    public static VictoryScreen Instance { get; private set; }

    // ── Runtime-built UI refs ──────────────────────────────────────────────
    private GameObject  rootCanvas;
    private GameObject  overlay;
    private GameObject  panel;
    private Text        titleText;
    private Text        subtitleText;
    private Text        flavorText;
    private GameObject  starsRow;
    private Button      replayBtn;
    private bool        built = false;

    // ── Timing ────────────────────────────────────────────────────────────
    private float gameStartTime;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        gameStartTime = Time.time;
    }

    void Start()
    {
        BuildUI();
    }

    // ─────────────────────────────────────────────────────────────────────
    public void Show()
    {
        if (!built) return;
        rootCanvas.SetActive(true);

        // Unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        StartCoroutine(PlayAnimation());
    }

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator PlayAnimation()
    {
        // ── 1. Fade-in the dark overlay ───────────────────────────────────
        Image overlayImg = overlay.GetComponent<Image>();
        overlayImg.color = new Color(0, 0, 0, 0);
        panel.SetActive(false);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * 1.8f;
            overlayImg.color = new Color(0, 0, 0, Mathf.Lerp(0f, 0.88f, t));
            yield return null;
        }

        // ── 2. Bounce-in the panel ────────────────────────────────────────
        panel.SetActive(true);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.localScale = Vector3.zero;

        t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * 3.0f;
            float s = BounceEaseOut(Mathf.Clamp01(t));
            panelRect.localScale = Vector3.one * s;
            yield return null;
        }
        panelRect.localScale = Vector3.one;

        yield return new WaitForSecondsRealtime(0.15f);

        // ── 3. Type-writer on title ───────────────────────────────────────
        string fullTitle = "YOU ESCAPED!";
        titleText.text = "";
        foreach (char c in fullTitle)
        {
            titleText.text += c;
            yield return new WaitForSecondsRealtime(0.07f);
        }

        yield return new WaitForSecondsRealtime(0.2f);

        // ── 4. Fade in subtitle & flavor ─────────────────────────────────
        subtitleText.gameObject.SetActive(true);
        flavorText.gameObject.SetActive(true);
        starsRow.SetActive(true);

        // Animate stars one by one
        Text[] stars = starsRow.GetComponentsInChildren<Text>();
        foreach (Text star in stars)
        {
            star.gameObject.SetActive(true);
            StartCoroutine(ScaleIn(star.gameObject, 0.4f));
            yield return new WaitForSecondsRealtime(0.18f);
        }

        yield return new WaitForSecondsRealtime(0.3f);

        // Show replay button
        replayBtn.gameObject.SetActive(true);
        StartCoroutine(ScaleIn(replayBtn.gameObject, 0.45f));

        // ── 5. Pulse title forever ────────────────────────────────────────
        StartCoroutine(PulseTitle());

        // Pause the game (but UI still responds because we use unscaledTime)
        Time.timeScale = 0f;
    }

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator ScaleIn(GameObject go, float duration)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) yield break;
        rt.localScale = Vector3.zero;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            rt.localScale = Vector3.one * BounceEaseOut(Mathf.Clamp01(t));
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    IEnumerator PulseTitle()
    {
        float timer = 0f;
        while (true)
        {
            timer += Time.unscaledDeltaTime * 1.4f;
            float brightness = 0.82f + Mathf.Sin(timer) * 0.18f;
            titleText.color = new Color(
                Mathf.Lerp(0.95f, 1.00f, brightness),
                Mathf.Lerp(0.75f, 0.95f, brightness),
                Mathf.Lerp(0.10f, 0.35f, brightness),
                1f);
            yield return null;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    float BounceEaseOut(float x)
    {
        float n1 = 7.5625f, d1 = 2.75f;
        if (x < 1f / d1)        return n1 * x * x;
        else if (x < 2f / d1)   return n1 * (x -= 1.5f / d1) * x + 0.75f;
        else if (x < 2.5f / d1) return n1 * (x -= 2.25f / d1) * x + 0.9375f;
        else                     return n1 * (x -= 2.625f / d1) * x + 0.984375f;
    }

    // ─────────────────────────────────────────────────────────────────────
    public void OnReplay()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ═════════════════════════════════════════════════════════════════════
    // UI Builder — creates everything in code
    // ═════════════════════════════════════════════════════════════════════
    void BuildUI()
    {
        // Root canvas
        rootCanvas = new GameObject("VictoryCanvas");
        var canvas = rootCanvas.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = rootCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        rootCanvas.AddComponent<GraphicRaycaster>();

        // ── Dark overlay ──────────────────────────────────────────────────
        overlay = NewGO("Overlay", rootCanvas.transform);
        var overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = new Color(0, 0, 0, 0.88f);
        Stretch(overlay);

        // ── Glowing outer border ──────────────────────────────────────────
        var border = NewGO("Border", overlay.transform);
        var borderImg = border.AddComponent<Image>();
        borderImg.color = new Color(1f, 0.82f, 0.1f, 0.55f);
        Center(border, 700f, 504f);

        // ── Main panel ────────────────────────────────────────────────────
        panel = NewGO("VictoryPanel", overlay.transform);
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.06f, 0.06f, 0.12f, 0.97f);
        Center(panel, 690f, 496f);

        // ── Gold top accent bar ───────────────────────────────────────────
        var accent = NewGO("Accent", panel.transform);
        var accentImg = accent.AddComponent<Image>();
        accentImg.color = new Color(1f, 0.78f, 0.08f, 1f);
        var accentRect = accent.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.pivot     = new Vector2(0.5f, 1f);
        accentRect.anchoredPosition = Vector2.zero;
        accentRect.sizeDelta = new Vector2(0f, 6f);

        // ── Prison break icon row ─────────────────────────────────────────
        var iconText = MakeText(panel.transform, "Icon", "🏃 🔓 🌅", 650f, 60f, 0f, 195f);
        iconText.fontSize  = 34;
        iconText.alignment = TextAnchor.MiddleCenter;
        iconText.color     = new Color(1f, 1f, 1f, 0.9f);

        // ── Main title ────────────────────────────────────────────────────
        titleText = MakeText(panel.transform, "Title", "", 650f, 100f, 0f, 110f);
        titleText.fontSize  = 72;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color     = new Color(1f, 0.88f, 0.2f, 1f);

        // ── Subtitle ─────────────────────────────────────────────────────
        subtitleText = MakeText(panel.transform, "Subtitle",
            "Prison Break: Silent Escape", 640f, 40f, 0f, 50f);
        subtitleText.fontSize  = 22;
        subtitleText.fontStyle = FontStyle.Italic;
        subtitleText.alignment = TextAnchor.MiddleCenter;
        subtitleText.color     = new Color(0.65f, 0.75f, 1f, 1f);
        subtitleText.gameObject.SetActive(false);

        // ── Divider ───────────────────────────────────────────────────────
        var divider = NewGO("Divider", panel.transform);
        divider.AddComponent<Image>().color = new Color(1f, 0.78f, 0.08f, 0.4f);
        var divRect = divider.GetComponent<RectTransform>();
        divRect.anchorMin = new Vector2(0.5f, 0.5f);
        divRect.anchorMax = new Vector2(0.5f, 0.5f);
        divRect.pivot     = new Vector2(0.5f, 0.5f);
        divRect.anchoredPosition = new Vector2(0f, 10f);
        divRect.sizeDelta = new Vector2(500f, 1.5f);

        // ── Flavor text ───────────────────────────────────────────────────
        float elapsedMinutes = (Time.time - gameStartTime) / 60f;
        string timeStr = $"{(int)elapsedMinutes}m {(int)((elapsedMinutes % 1f) * 60f)}s";
        flavorText = MakeText(panel.transform, "Flavor",
            $"You outfoxed the guards and broke free!\nTime taken:  {timeStr}",
            620f, 60f, 0f, -40f);
        flavorText.fontSize  = 17;
        flavorText.alignment = TextAnchor.MiddleCenter;
        flavorText.color     = new Color(0.75f, 0.80f, 0.90f, 1f);
        flavorText.gameObject.SetActive(false);

        // ── Stars row ─────────────────────────────────────────────────────
        starsRow = NewGO("Stars", panel.transform);
        Center(starsRow, 300f, 50f, 0f, -105f);
        var hl = starsRow.AddComponent<HorizontalLayoutGroup>();
        hl.spacing          = 12f;
        hl.childAlignment   = TextAnchor.MiddleCenter;
        hl.childForceExpandWidth  = false;
        hl.childForceExpandHeight = false;

        for (int i = 0; i < 3; i++)
        {
            var star    = MakeText(starsRow.transform, $"Star{i}", "★", 60f, 60f);
            star.fontSize  = 42;
            star.alignment = TextAnchor.MiddleCenter;
            star.color     = new Color(1f, 0.82f, 0.08f, 1f);
            star.gameObject.SetActive(false);
        }
        starsRow.SetActive(false);

        // ── Replay button ─────────────────────────────────────────────────
        var btnGO = NewGO("ReplayBtn", panel.transform);
        Center(btnGO, 220f, 52f, 0f, -185f);

        // Button background
        var btnImg = btnGO.AddComponent<Image>();
        btnImg.color = new Color(1f, 0.78f, 0.08f, 1f);

        replayBtn = btnGO.AddComponent<Button>();
        var colors = replayBtn.colors;
        colors.normalColor      = new Color(1f, 0.78f, 0.08f, 1f);
        colors.highlightedColor = new Color(1f, 0.92f, 0.30f, 1f);
        colors.pressedColor     = new Color(0.85f, 0.60f, 0.02f, 1f);
        replayBtn.colors        = colors;
        replayBtn.onClick.AddListener(OnReplay);

        var btnLabel = MakeText(btnGO.transform, "BtnText", "▶  PLAY AGAIN", 220f, 52f);
        btnLabel.fontSize  = 20;
        btnLabel.fontStyle = FontStyle.Bold;
        btnLabel.alignment = TextAnchor.MiddleCenter;
        btnLabel.color     = new Color(0.08f, 0.06f, 0.02f, 1f);

        replayBtn.gameObject.SetActive(false);

        // ── Hide until Show() is called ───────────────────────────────────
        rootCanvas.SetActive(false);
        built = true;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────
    static GameObject NewGO(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }

    static void Stretch(GameObject go)
    {
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
    }

    static void Center(GameObject go, float w, float h, float x = 0f, float y = 0f)
    {
        var r = go.GetComponent<RectTransform>();
        if (r == null) r = go.AddComponent<RectTransform>();
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = new Vector2(x, y);
        r.sizeDelta = new Vector2(w, h);
    }

    static Text MakeText(Transform parent, string name, string content,
                         float w = 200f, float h = 40f, float x = 0f, float y = 0f)
    {
        var go = NewGO(name, parent);
        Center(go, w, h, x, y);
        var txt  = go.AddComponent<Text>();
        txt.text = content;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (txt.font == null)
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        txt.supportRichText = true;
        return txt;
    }
}
