// =============================================================================
// DoorInteraction.cs
// Student 2 — GV (SE3032) | Prison Break: Silent Escape
// Role: Systems Engineer — Player Interaction Physics
//
// Description:
//   Physics-based door interaction. The player presses E when close to a door
//   to open or close it. The door rotates around its pivot using a Coroutine
//   with Quaternion.RotateTowards for smooth, frame-rate-independent motion.
//
//   The door uses a Kinematic Rigidbody — it is solid to the physics engine
//   (guards, barricades cannot push through it) but we control its transform
//   directly without fighting the physics solver.
//
// Unity Setup:
//   1. Create a Door GameObject. Set its pivot point at the HINGE EDGE
//      (not the centre) — create an empty parent at the hinge, make the
//      door mesh a child of that parent.
//   2. Add a Rigidbody to the parent. Set isKinematic = true.
//   3. Add a BoxCollider to the door mesh child (not trigger).
//   4. Attach this script to the hinge parent GameObject.
//   5. Set interactionRange to the desired detection radius.
// =============================================================================

using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DoorInteraction : MonoBehaviour
{
    // ── Inspector Fields ──────────────────────────────────────────────────────

    [Header("Door Settings")]
    [Tooltip("How far open the door rotates (degrees around local Y-axis).")]
    [SerializeField] private float openAngle = 90f;

    [Tooltip("Degrees per second the door rotates.")]
    [SerializeField] private float rotationSpeed = 120f;

    [Tooltip("Maximum distance from which the player can interact.")]
    [SerializeField] private float interactionRange = 2.5f;

    [Header("Optional Interaction Prompt")]
    [Tooltip("Assign a world-space Canvas TextMeshPro object for '[E] Open' hint (optional).")]
    [SerializeField] private GameObject interactionPrompt;

    // ── Private State ─────────────────────────────────────────────────────────

    private bool _isOpen;
    private bool _isMoving;
    private Quaternion _closedRotation;
    private Quaternion _openRotation;
    private Transform _playerTransform;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        // Cache the closed (default) rotation and compute the open target.
        _closedRotation = transform.localRotation;
        _openRotation   = Quaternion.Euler(transform.localEulerAngles + new Vector3(0f, openAngle, 0f));

        // Find player by tag — avoids hard references.
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) _playerTransform = playerObj.transform;

        // Start with prompt hidden.
        if (interactionPrompt != null) interactionPrompt.SetActive(false);
    }

    /// <summary>
    /// Checks distance every frame to show/hide the interaction prompt and
    /// detect the E key press. Using Update (not FixedUpdate) so input feels
    /// instant — key polling is presentation-layer logic, not physics.
    /// </summary>
    private void Update()
    {
        if (_playerTransform == null) return;

        float dist = Vector3.Distance(transform.position, _playerTransform.position);
        bool inRange = dist <= interactionRange;

        // Toggle the prompt visibility.
        if (interactionPrompt != null)
            interactionPrompt.SetActive(inRange && !_isMoving);

        // Handle E-key interaction.
        if (inRange && !_isMoving && Input.GetKeyDown(KeyCode.E))
        {
            StartCoroutine(RotateDoor());
        }
    }

    // ── Private Methods ───────────────────────────────────────────────────────

    /// <summary>
    /// Smoothly rotates the door between closed and open states.
    ///
    /// DESIGN CHOICE — Coroutine + Quaternion.RotateTowards vs Lerp:
    ///   RotateTowards moves at a constant angular speed (degrees/sec) rather
    ///   than an exponential ease that Lerp produces. This feels more physical
    ///   and matches a real door pivot. The Coroutine runs independently of
    ///   Update so it can be awaited and the _isMoving flag stays reliable.
    ///
    ///   We do NOT use Rigidbody.MoveRotation here because kinematic Rigidbody
    ///   rotation via MoveRotation requires FixedUpdate timing — using it in a
    ///   Coroutine tied to Update timing would cause stutter. Direct transform
    ///   assignment is safe for kinematic Rigidbodies and avoids the mismatch.
    /// </summary>
    private IEnumerator RotateDoor()
    {
        _isMoving = true;
        Quaternion target = _isOpen ? _closedRotation : _openRotation;

        while (Quaternion.Angle(transform.localRotation, target) > 0.05f)
        {
            transform.localRotation = Quaternion.RotateTowards(
                transform.localRotation,
                target,
                rotationSpeed * Time.deltaTime
            );
            yield return null; // wait one frame
        }

        // Snap to exact target to eliminate floating-point residuals.
        transform.localRotation = target;
        _isOpen = !_isOpen;
        _isMoving = false;
    }
}
