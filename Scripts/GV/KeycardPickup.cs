// =============================================================================
// KeycardPickup.cs
// Student 2 — GV (SE3032) | Prison Break: Silent Escape
// Role: Systems Engineer — Player Interaction Physics
//
// Description:
//   Handles trigger-based keycard collection. When the player enters the
//   keycard's BoxCollider trigger, this script notifies the GameManager,
//   plays a visual/audio feedback, and destroys the keycard GameObject.
//
//   Three keycards are placed across the prison by S1 (World Builder):
//     - Keycard 1: Near the starting corridor (low risk)
//     - Keycard 2: Inside the security room (moderate risk)
//     - Keycard 3: Deep in a guard patrol zone (high risk)
//
// Unity Setup:
//   1. Create a Keycard GameObject with a mesh (cube/card shape) and
//      a BoxCollider. Set BoxCollider.isTrigger = true.
//   2. Attach this script to each Keycard GameObject.
//   3. Tag the Player GameObject as "Player".
//   4. Optionally assign a pickup audio clip and a particle effect prefab.
//   5. Duplicate this setup for all 3 keycards, placing each in its
//      designated location in the scene.
// =============================================================================

using UnityEngine;

public class KeycardPickup : MonoBehaviour
{
    // ── Inspector Fields ──────────────────────────────────────────────────────

    [Header("Keycard Identity")]
    [Tooltip("Unique ID for this keycard (1, 2, or 3). Used by GameManager.")]
    [SerializeField] private int keycardID = 1;

    [Header("Feedback")]
    [Tooltip("Audio clip to play on pickup (assign in Inspector).")]
    [SerializeField] private AudioClip pickupSound;

    [Tooltip("Particle effect prefab to spawn on pickup (optional).")]
    [SerializeField] private GameObject pickupParticlesPrefab;

    [Header("Visual")]
    [Tooltip("Should the keycard rotate in place to draw player attention?")]
    [SerializeField] private bool rotateInPlace = true;

    [Tooltip("Rotation speed in degrees per second (Y-axis).")]
    [SerializeField] private float rotationSpeed = 90f;

    [Tooltip("Vertical hover amplitude (metres).")]
    [SerializeField] private float hoverAmplitude = 0.1f;

    [Tooltip("Hover cycle frequency (Hz).")]
    [SerializeField] private float hoverFrequency = 1.0f;

    // ── Private State ─────────────────────────────────────────────────────────

    private bool _collected = false;
    private Vector3 _startPosition;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        _startPosition = transform.position;
    }

    /// <summary>
    /// Rotates and hovers the keycard for visual feedback. Runs only when
    /// the keycard hasn't been collected yet. Pure presentation logic —
    /// no physics interaction, runs in Update (frame rate).
    /// </summary>
    private void Update()
    {
        if (_collected || !rotateInPlace) return;

        // Rotate around Y-axis (spin animation).
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);

        // Bob up and down using a sine wave for a hovering effect.
        float newY = _startPosition.y + Mathf.Sin(Time.time * hoverFrequency * 2f * Mathf.PI) * hoverAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    /// <summary>
    /// Trigger-based pickup detection.
    ///
    /// DESIGN CHOICE — OnTriggerEnter vs Raycast:
    ///   A trigger volume is simpler and more reliable than a raycast for
    ///   an item pickup. The player doesn't need to be looking at the keycard —
    ///   they just need to be physically close enough, which matches the game's
    ///   pick-up-by-proximity design. The trigger size is inspectable and
    ///   easy to tune without code changes.
    ///
    ///   We check for the "Player" tag rather than type-casting to a script
    ///   to avoid a hard coupling between KeycardPickup and PlayerController.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // Guard against multiple trigger events (physics can fire twice).
        if (_collected) return;

        if (!other.CompareTag("Player")) return;

        _collected = true;
        Collect();
    }

    // ── Private Methods ───────────────────────────────────────────────────────

    private void Collect()
    {
        Debug.Log($"[KeycardPickup] Keycard {keycardID} collected!");

        // Notify the GameManager to update keycard count and check win condition.
        GameManager.Instance?.OnKeycardCollected(keycardID);

        // Play pickup audio at the world position (persists after GameObject destroyed).
        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position, 0.8f);

        // Spawn particle effect if assigned.
        if (pickupParticlesPrefab != null)
        {
            GameObject fx = Instantiate(pickupParticlesPrefab, transform.position, Quaternion.identity);
            Destroy(fx, 3f); // auto-destroy particles after 3 seconds
        }

        // Destroy this keycard GameObject.
        Destroy(gameObject);
    }
}
