using UnityEngine;

/// <summary>
/// Attach this script to each Guard GameObject (e.g. SkelMesh_Bodyguard_01, SkelMesh_Bodyguard_02).
/// Implements a vision cone with line-of-sight raycasting.
/// Allows the player to sneak around by hiding behind objects or keeping out of the vision angle!
/// </summary>
public class GuardDetection : MonoBehaviour
{
    [Header("Detection Range")]
    public float maxDetectionDistance = 6f;
    [Range(0f, 360f)]
    [Tooltip("Wider angle means the guard can see more of their peripheral vision!")]
    public float visionAngle = 150f; // vision cone width in degrees
    [Tooltip("If the player gets closer than this distance, they are caught instantly even if they are behind the guard's back!")]
    public float proximityCatchDistance = 1.5f;

    [Header("Visual Feedback (Optional)")]
    [Tooltip("You can attach a Spotlight to this slot, and its color will change dynamically based on state!")]
    public Light visionSpotlight;
    public Color normalColor = Color.green;
    public Color caughtColor = Color.red;

    private Transform playerTransform;

    private void Start()
    {
        // Find player dynamically using the tag
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }

        // Set up spotlight color if assigned
        if (visionSpotlight != null)
        {
            visionSpotlight.color = normalColor;
            visionSpotlight.range = maxDetectionDistance;
            visionSpotlight.spotAngle = visionAngle;
        }
    }

    private void Update()
    {
        if (playerTransform == null) return;

        if (IsPlayerDetected())
        {
            if (visionSpotlight != null) visionSpotlight.color = caughtColor;

            // Trigger GameOver in GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.GameOver();
            }
            else
            {
                Debug.LogError("🚨 Player Caught! (GameManager.Instance is null, make sure GameManager script is in your scene!)");
            }
        }
        else
        {
            if (visionSpotlight != null) visionSpotlight.color = normalColor;
        }
    }

    public bool IsPlayerDetected()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // 1. Proximity Catch: If player is extremely close (standing next to or touching guard), catch them instantly!
        if (distanceToPlayer <= proximityCatchDistance)
        {
            Debug.Log($"🚨 Caught by proximity! Player was too close ({distanceToPlayer:F2}m).");
            return true;
        }

        // 2. Vision Cone Catch: Normal line-of-sight detection
        if (distanceToPlayer > maxDetectionDistance) return false;

        // Check if player is within vision angle
        Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

        if (angleToPlayer < visionAngle / 2f)
        {
            // Perform line-of-sight raycast from guard eye level (1.6f) to player body center (1f)
            Vector3 eyePosition = transform.position + Vector3.up * 1.6f;
            Vector3 playerTargetPosition = playerTransform.position + Vector3.up * 1f;
            Vector3 rayDirection = (playerTargetPosition - eyePosition).normalized;

            RaycastHit hit;
            if (Physics.Raycast(eyePosition, rayDirection, out hit, maxDetectionDistance))
            {
                if (hit.collider.CompareTag("Player"))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void OnDrawGizmos()
    {
        // Draw vision range circle in Scene View
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, maxDetectionDistance);

        // Draw vision cone boundaries
        Vector3 leftBoundary = Quaternion.Euler(0, -visionAngle / 2f, 0) * transform.forward * maxDetectionDistance;
        Vector3 rightBoundary = Quaternion.Euler(0, visionAngle / 2f, 0) * transform.forward * maxDetectionDistance;

        Gizmos.color = Color.yellow;
        Vector3 eyePosition = transform.position + Vector3.up * 1.6f;
        Gizmos.DrawLine(eyePosition, eyePosition + leftBoundary);
        Gizmos.DrawLine(eyePosition, eyePosition + rightBoundary);
    }
}
