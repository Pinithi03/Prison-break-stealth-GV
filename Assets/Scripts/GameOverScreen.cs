using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds and animates a premium Game Over screen at runtime.
/// Attach to any GameObject. Called automatically by GameManager.GameOver().
/// No manual Canvas setup required.
/// </summary>
public class GameOverScreen : MonoBehaviour
{
    public static GameOverScreen Instance { get; private set; }

    // ── Colours ───────────────────────────────────────────────────────────
    private readonly Color colRed        = new Color(0.90f, 0.15f, 0.15f, 1f);
    private readonly Color colRedDark    = new Color(0.55f, 0.05f, 0.05f, 1f);
    private readonly Color colRedGlow    = new Color(0.95f, 0.20f, 0.10f, 0.60f);
    private readonly Color colPanel      = new Color(0.06f, 0.03f, 0.03f, 0.97f);
    private readonly Color colSubtitle   = new Color(0.75f, 0.55f, 0.55f, 1f);
    private readonly Color colFuture     = new Color(0.65f, 0.50f, 0.50f, 1f);

    // ── Internal refs ─────────────────────────────────────────────────────
    private GameObject rootCanvas;
    private GameObject overlay;
    private GameObject panel;
    private Text       titleText;
    private Text       subtitleText;
    private Text       flavorText;
    private Button     retryBtn;
    private bool       built = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
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

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        StartCoroutine(PlayAnimation());
    }

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator PlayAnimation()
    {
        // ── 1. Red flash then dark fade-in ────────────────────────────────
        Image overlayImg = overlay.GetComponent<Image>();
        panel.SetActive(false);

        // Quick red flash
        overlayImg.color = new Color(0.8f, 0f, 0f, 0f);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * 4f;
            overlayImg.color = new Color(0.7f, 0f, 0f, Mathf.Lerp(0f, 0.6f, t));
            yield return null;
        }
        // Fade to dark
        t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * 2f;
            overlayImg.color = Color.Lerp(
                new Color(0.7f, 0f, 0f, 0.6f),
                new Color(0f,   0f, 0f, 0.88f), t);
            yield return null;
        }

        // ── 2. Shake + slam-in the panel ──────────────────────────────────
        panel.SetActive(true);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.localScale = new Vector3(1.4f, 1.4f, 1f);

        t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * 4.5f;
            float s = Mathf.Lerp(1.4f, 1f, ElasticEaseOut(Mathf.Clamp01(t)));
            panelRect.localScale = new Vector3(s, s, 1f);

            // Screen shake
            if (t < 0.4f)
            {
                float shake = (1f - t / 0.4f) * 8f;
                panelRect.anchoredPosition = new Vector2(
                    Random.Range(-shake, shake),
                    Random.Range(-shake, shake));
            }
            else
            {
                panelRect.anchoredPosition = Vector2.zero;
            }
            yield return null;
        }
        panelRect.localScale        = Vector3.one;
        panelRect.anchoredPosition  = Vector2.zero;

        yield return new WaitForSecondsRealtime(0.1f);

        // ── 3. Type-writer title ──────────────────────────────────────────
        string full = "YOU WERE CAUGHT!";
        titleText.text = "";
        foreach (char c in full)
        {
            titleText.text += c;
            yield return new WaitForSecondsRealtime(0.055f);
        }

        yield return new WaitForSecondsRealtime(0.2f);

        // ── 4. Fade in subtitle & flavor ──────────────────────────────────
        subtitleText.gameObject.SetActive(true);
        flavorText.gameObject.SetActive(true);

        yield return new WaitForSecondsRealtime(0.25f);

        // ── 5. Bounce in retry button ─────────────────────────────────────
        retryBtn.gameObject.SetActive(true);
        StartCoroutine(ScaleIn(retryBtn.gameObject, 0.4f));

        // ── 6. Pulse title red forever ────────────────────────────────────
        StartCoroutine(PulseTitle());

        Time.timeScale = 0f;
    }

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator ScaleIn(GameObject go, float duration)
    {
        var rt = go.GetComponent<RectTransform>();
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
            timer += Time.unscaledDeltaTime * 1.8f;
            float b = 0.80f + Mathf.Sin(timer) * 0.20f;
            titleText.color = new Color(b, 0.08f * b, 0.08f * b, 1f);
            yield return null;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    float BounceEaseOut(float x)
    {
        float n1 = 7.5625f, d1 = 2.75f;
        if      (x < 1f / d1)        return n1 * x * x;
        else if (x < 2f / d1)        return n1 * (x -= 1.5f / d1)  * x + 0.75f;
        else if (x < 2.5f / d1)      return n1 * (x -= 2.25f / d1) * x + 0.9375f;
        else                          return n1 * (x -= 2.625f / d1)* x + 0.984375f;
    }

    float ElasticEaseOut(float x)
    {
        if (x == 0f || x == 1f) return x;
        return Mathf.Pow(2f, -10f * x) * Mathf.Sin((x * 10f - 0.75f) * (2f * Mathf.PI) / 3f) + 1f;
    }

    // ─────────────────────────────────────────────────────────────────────
    public void OnRetry()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ═════════════════════════════════════════════════════════════════════
    // UI Builder
    // ═════════════════════════════════════════════════════════════════════
    void BuildUI()
    {
        // ── Canvas ────────────────────────────────────────────────────────
        rootCanvas = new GameObject("GameOverCanvas");
        var canvas = rootCanvas.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 99;
        var scaler = rootCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        rootCanvas.AddComponent<GraphicRaycaster>();

        // ── Dark overlay ──────────────────────────────────────────────────
        overlay = NewGO("Overlay", rootCanvas.transform);
        overlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.88f);
        Stretch(overlay);

        // ── Red outer glow border ─────────────────────────────────────────
        var border = NewGO("Border", overlay.transform);
        border.AddComponent<Image>().color = colRedGlow;
        Center(border, 700f, 484f);

        // ── Inner dark panel ──────────────────────────────────────────────
        panel = NewGO("GameOverPanel", overlay.transform);
        panel.AddComponent<Image>().color = colPanel;
        Center(panel, 690f, 476f);

        // ── Red top accent bar ────────────────────────────────────────────
        var accent = NewGO("Accent", panel.transform);
        accent.AddComponent<Image>().color = colRed;
        var ar = accent.GetComponent<RectTransform>();
        ar.anchorMin = new Vector2(0f, 1f); ar.anchorMax = new Vector2(1f, 1f);
        ar.pivot     = new Vector2(0.5f, 1f);
        ar.anchoredPosition = Vector2.zero;
        ar.sizeDelta = new Vector2(0f, 6f);

        // ── Guard icon row ────────────────────────────────────────────────
        var iconText = MakeText(panel.transform, "Icon", "🔦 👮 🚨", 650f, 58f, 0f, 188f);
        iconText.fontSize  = 32;
        iconText.alignment = TextAnchor.MiddleCenter;
        iconText.color     = new Color(1f, 0.6f, 0.6f, 0.9f);

        // ── Main title ────────────────────────────────────────────────────
        titleText = MakeText(panel.transform, "Title", "", 650f, 95f, 0f, 105f);
        titleText.fontSize  = 64;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color     = colRed;

        // ── Subtitle ──────────────────────────────────────────────────────
        subtitleText = MakeText(panel.transform, "Subtitle",
            "Prison Break: Silent Escape", 640f, 36f, 0f, 48f);
        subtitleText.fontSize  = 20;
        subtitleText.fontStyle = FontStyle.Italic;
        subtitleText.alignment = TextAnchor.MiddleCenter;
        subtitleText.color     = colSubtitle;
        subtitleText.gameObject.SetActive(false);

        // ── Divider ───────────────────────────────────────────────────────
        var divider = NewGO("Divider", panel.transform);
        divider.AddComponent<Image>().color = new Color(0.8f, 0.1f, 0.1f, 0.4f);
        var dr = divider.GetComponent<RectTransform>();
        dr.anchorMin = dr.anchorMax = dr.pivot = new Vector2(0.5f, 0.5f);
        dr.anchoredPosition = new Vector2(0f, 14f);
        dr.sizeDelta = new Vector2(500f, 1.5f);

        // ── Flavor text ───────────────────────────────────────────────────
        flavorText = MakeText(panel.transform, "Flavor",
            "The guard spotted you! Stay in the shadows.\nAvoid their line of sight and try again.",
            620f, 70f, 0f, -50f);
        flavorText.fontSize  = 17;
        flavorText.alignment = TextAnchor.MiddleCenter;
        flavorText.color     = colFuture;
        flavorText.gameObject.SetActive(false);

        // ── Try Again button ──────────────────────────────────────────────
        var btnGO = NewGO("RetryBtn", panel.transform);
        Center(btnGO, 230f, 54f, 0f, -178f);

        // Dark red background
        btnGO.AddComponent<Image>().color = colRedDark;

        retryBtn = btnGO.AddComponent<Button>();
        var colors               = retryBtn.colors;
        colors.normalColor       = colRedDark;
        colors.highlightedColor  = colRed;
        colors.pressedColor      = new Color(0.35f, 0.02f, 0.02f, 1f);
        retryBtn.colors          = colors;
        retryBtn.onClick.AddListener(OnRetry);

        // Red border on button
        var btnBorder = NewGO("BtnBorder", btnGO.transform);
        btnBorder.AddComponent<Image>().color = colRed;
        var bbr = btnBorder.GetComponent<RectTransform>();
        bbr.anchorMin = Vector2.zero; bbr.anchorMax = Vector2.one;
        bbr.offsetMin = new Vector2(-2f, -2f); bbr.offsetMax = new Vector2(2f, 2f);

        // Make border render behind
        btnBorder.transform.SetAsFirstSibling();

        var btnLabel = MakeText(btnGO.transform, "BtnText", "↺  TRY AGAIN", 230f, 54f);
        btnLabel.fontSize  = 20;
        btnLabel.fontStyle = FontStyle.Bold;
        btnLabel.alignment = TextAnchor.MiddleCenter;
        btnLabel.color     = new Color(1f, 0.75f, 0.75f, 1f);

        retryBtn.gameObject.SetActive(false);

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
        var txt = go.AddComponent<Text>();
        txt.text = content;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (txt.font == null)
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        txt.supportRichText = true;
        return txt;
    }
}
