using UnityEngine;

/// <summary>
/// Enhanced Guard Detection with 2-tier suspicion system.
/// 
/// TIER 1 — SUSPICIOUS (yellow spotlight, "?" above head):
///   Guard sees the player → suspicion meter fills over suspicionTimeToAlert seconds.
///   If player hides before meter fills → suspicion drains away → guard returns to normal.
///
/// TIER 2 — CAUGHT (red spotlight, "!" above head):
///   Suspicion meter fills completely → GAME OVER.
///
/// Proximity is still an instant catch.
/// </summary>
public class GuardDetection : MonoBehaviour
{
    // ── Detection Range ───────────────────────────────────────────────────
    [Header("Detection Range")]
    public float maxDetectionDistance  = 8f;
    [Range(0f, 360f)]
    public float visionAngle           = 110f;
    public float proximityCatchDistance = 1.5f;

    // ── Suspicion Settings ────────────────────────────────────────────────
    [Header("Suspicion (2-Tier Detection)")]
    [Tooltip("Seconds of sustained sight needed to trigger GAME OVER")]
    public float suspicionTimeToAlert  = 2.0f;
    [Tooltip("How fast suspicion drains when player is out of sight")]
    public float suspicionDecayRate    = 0.8f;

    // ── Spotlight Colours ─────────────────────────────────────────────────
    [Header("Visual Feedback")]
    public Light  visionSpotlight;
    public Color  normalColor     = Color.green;
    public Color  suspiciousColor = Color.yellow;
    public Color  caughtColor     = Color.red;

    // ── Internal state ────────────────────────────────────────────────────
    private enum DetectionState { Normal, Suspicious, Caught }
    private DetectionState state = DetectionState.Normal;

    private float          suspicionLevel = 0f;   // 0 → suspicionTimeToAlert
    private Transform      playerTransform;

    // Guard controller reference (either type)
    private GuardController  gc1;
    private GuardController2 gc2;

    // Floating "?" / "!" indicator
    private TextMesh  indicatorMesh;
    private Transform indicatorTransform;
    private bool      alreadyCaught = false;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;

        gc1 = GetComponent<GuardController>();
        gc2 = GetComponent<GuardController2>();

        if (visionSpotlight != null)
        {
            visionSpotlight.color     = normalColor;
            visionSpotlight.range     = maxDetectionDistance;
            visionSpotlight.spotAngle = visionAngle;
        }

        BuildIndicator();
    }

    // ─────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (playerTransform == null || alreadyCaught) return;
        if (GameManager.Instance != null && GameManager.Instance.isGameOver) return;

        // ── Proximity: always instant catch ───────────────────────────────
        float dist = Vector3.Distance(transform.position, playerTransform.position);
        if (dist <= proximityCatchDistance)
        {
            TriggerCaught();
            return;
        }

        bool canSee = CanSeePlayer();

        switch (state)
        {
            // ── NORMAL ────────────────────────────────────────────────────
            case DetectionState.Normal:
                if (canSee)
                {
                    state = DetectionState.Suspicious;
                    SetGuardSuspicious(true);
                }
                break;

            // ── SUSPICIOUS ────────────────────────────────────────────────
            case DetectionState.Suspicious:
                if (canSee)
                {
                    suspicionLevel += Time.deltaTime;
                    if (suspicionLevel >= suspicionTimeToAlert)
                    {
                        TriggerCaught();
                        return;
                    }
                }
                else
                {
                    suspicionLevel -= Time.deltaTime * suspicionDecayRate;
                    if (suspicionLevel <= 0f)
                    {
                        suspicionLevel = 0f;
                        state          = DetectionState.Normal;
                        SetGuardSuspicious(false);
                    }
                }
                break;
        }

        UpdateVisuals();
        BillboardIndicator();
    }

    // ─────────────────────────────────────────────────────────────────────
    void TriggerCaught()
    {
        if (alreadyCaught) return;
        alreadyCaught = true;
        state         = DetectionState.Caught;

        if (visionSpotlight != null) visionSpotlight.color = caughtColor;

        // Show "!" briefly
        if (indicatorMesh != null)
        {
            indicatorMesh.text  = "!";
            indicatorMesh.color = Color.red;
            indicatorTransform.gameObject.SetActive(true);
        }

        if (AudioManager.Instance != null) AudioManager.Instance.PlayGuardAlert();
        if (GameManager.Instance  != null) GameManager.Instance.GameOver();
    }

    // ─────────────────────────────────────────────────────────────────────
    void UpdateVisuals()
    {
        float t = suspicionTimeToAlert > 0
            ? suspicionLevel / suspicionTimeToAlert
            : 0f;

        // Spotlight colour: green → yellow → orange (closer to red as suspicion grows)
        Color targetColor = state == DetectionState.Normal
            ? normalColor
            : Color.Lerp(suspiciousColor, caughtColor, t);

        if (visionSpotlight != null)
            visionSpotlight.color = targetColor;

        // Indicator "?" symbol
        if (indicatorMesh != null)
        {
            bool show = state == DetectionState.Suspicious;
            indicatorTransform.gameObject.SetActive(show);
            if (show)
            {
                indicatorMesh.text  = "?";
                indicatorMesh.color = Color.Lerp(Color.yellow, Color.red, t);
                // Scale pulses with suspicion
                float scale = 0.08f + t * 0.04f;
                indicatorTransform.localScale = Vector3.one * scale;
            }
        }
    }

    void BillboardIndicator()
    {
        if (indicatorTransform == null) return;
        Camera cam = Camera.main;
        if (cam != null)
            indicatorTransform.rotation = cam.transform.rotation;
    }

    // ─────────────────────────────────────────────────────────────────────
    bool CanSeePlayer()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer > maxDetectionDistance) return false;

        Vector3 dirToPlayer  = (playerTransform.position - transform.position).normalized;
        float   angleToPlayer = Vector3.Angle(transform.forward, dirToPlayer);

        if (angleToPlayer < visionAngle / 2f)
        {
            Vector3 eyePos    = transform.position + Vector3.up * 1.6f;
            Vector3 playerPos = playerTransform.position + Vector3.up * 1f;
            Vector3 rayDir    = (playerPos - eyePos).normalized;

            RaycastHit hit;
            if (Physics.Raycast(eyePos, rayDir, out hit, maxDetectionDistance))
            {
                if (hit.collider.CompareTag("Player"))
                    return true;
            }
        }
        return false;
    }

    // ─────────────────────────────────────────────────────────────────────
    void SetGuardSuspicious(bool suspicious)
    {
        if (gc1 != null) gc1.SetSuspicious(suspicious);
        if (gc2 != null) gc2.SetSuspicious(suspicious);
    }

    // ─────────────────────────────────────────────────────────────────────
    void BuildIndicator()
    {
        var indicatorGO = new GameObject("SuspicionIndicator");
        indicatorGO.transform.SetParent(transform, false);
        indicatorGO.transform.localPosition = new Vector3(0f, 2.6f, 0f);
        indicatorTransform = indicatorGO.transform;

        indicatorMesh                = indicatorGO.AddComponent<TextMesh>();
        indicatorMesh.text           = "?";
        indicatorMesh.fontSize       = 60;
        indicatorMesh.characterSize  = 0.1f;
        indicatorMesh.anchor         = TextAnchor.MiddleCenter;
        indicatorMesh.alignment      = TextAlignment.Center;
        indicatorMesh.color          = Color.yellow;

        indicatorGO.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, maxDetectionDistance);

        Gizmos.color = Color.yellow;
        Vector3 eye   = transform.position + Vector3.up * 1.6f;
        Vector3 left  = Quaternion.Euler(0, -visionAngle / 2f, 0) * transform.forward * maxDetectionDistance;
        Vector3 right = Quaternion.Euler(0,  visionAngle / 2f, 0) * transform.forward * maxDetectionDistance;
        Gizmos.DrawLine(eye, eye + left);
        Gizmos.DrawLine(eye, eye + right);

        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, proximityCatchDistance);
    }
}
