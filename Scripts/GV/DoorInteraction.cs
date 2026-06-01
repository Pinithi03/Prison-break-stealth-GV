// =============================================================================
// DoorInteraction.cs
// Student 2 — GV (SE3032) | Prison Break: Silent Escape
// Role: Systems Engineer — Player Interaction Physics
//
// Description:
//   Attach this script DIRECTLY to the door mesh object (Cell_Door,
//   Security_Door, etc.) — no parent pivot/hinge empty object required.
//
//   The door swings open by rotating around one of its own edges using
//   Physics.RotateAround(). The hinge edge stays fixed in world space
//   while the door panel swings through the open angle.
//
//   DESIGN CHOICE — RotateAround vs parent pivot:
//     RotateAround(worldPoint, axis, angle) spins an object around any
//     fixed world position without needing a parent transform.
//     We capture the hinge edge position ONCE before the swing starts
//     (it never moves — that is the nature of a hinge) then rotate
//     incrementally until we reach the target angle.
//
// Unity Setup:
//   1. Select your door object (Cell_Door or Security_Door).
//   2. Add Component → Rigidbody → tick Is Kinematic = true.
//   3. Add Component → DoorInteraction.
//   4. Set Open Angle = 90 (or -90 if it swings the wrong way).
//   5. Toggle 'Hinge On Positive Z' to pick which edge is the hinge.
//      Press Play → press E near the door → it should swing open.
// =============================================================================

using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DoorInteraction : MonoBehaviour
{
    // ── Inspector Fields ──────────────────────────────────────────────────────

    [Header("Door Settings")]
    [Tooltip("How far the door swings open in degrees. Use -90 to flip direction.")]
    [SerializeField] private float openAngle = 90f;

    [Tooltip("Rotation speed in degrees per second.")]
    [SerializeField] private float rotationSpeed = 120f;

    [Tooltip("How close the player must be to press E and interact.")]
    [SerializeField] private float interactionRange = 2.5f;

    [Header("Hinge Side")]
    [Tooltip("Which Z-edge of the door is the hinge?\n" +
             "TRUE  = positive Z edge (try this first).\n" +
             "FALSE = negative Z edge.\n" +
             "Toggle this if the door swings from the wrong side.")]
    [SerializeField] private bool hingeOnPositiveZ = true;

    // ── Private State ─────────────────────────────────────────────────────────

    private bool _isOpen;
    private bool _isMoving;
    private Transform _playerTransform;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        // Force kinematic in case it was forgotten in the Inspector.
        GetComponent<Rigidbody>().isKinematic = true;

        // Find the player by tag so we don't need a hard Inspector reference.
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            _playerTransform = playerObj.transform;
        else
            Debug.LogWarning($"[DoorInteraction] '{name}': No GameObject tagged 'Player' found! " +
                             "Make sure the Player object has the tag 'Player'.");
    }

    private void Update()
    {
        if (_playerTransform == null || _isMoving) return;

        float dist = Vector3.Distance(transform.position, _playerTransform.position);

        if (dist <= interactionRange && Input.GetKeyDown(KeyCode.E))
        {
            StartCoroutine(SwingDoor());
        }
    }

    // ── Core Swing Logic ──────────────────────────────────────────────────────

    private IEnumerator SwingDoor()
    {
        _isMoving = true;

        // ── Calculate the hinge edge in WORLD SPACE ───────────────────────────
        // The door's Z scale gives its width. The hinge is at one Z edge:
        //   halfWidth = localScale.z / 2
        //   hingeWorld = door centre  +  (door's forward direction × halfWidth)
        //
        // We capture this BEFORE the door starts moving.
        // Because RotateAround pivots around a fixed world point, this
        // position stays constant throughout the entire swing — exactly
        // like a real hinge bolted to a door frame.
        float halfWidth   = transform.localScale.z / 2f;
        float hingeSign   = hingeOnPositiveZ ? 1f : -1f;
        Vector3 hingeWorld = transform.position + transform.forward * (hingeSign * halfWidth);

        // ── Swing incrementally toward the target angle ───────────────────────
        float totalDelta = _isOpen ? -openAngle : openAngle; // positive=open, negative=close
        float rotated    = 0f;

        while (Mathf.Abs(rotated) < Mathf.Abs(totalDelta))
        {
            float step = rotationSpeed * Time.deltaTime * Mathf.Sign(totalDelta);

            // Clamp the last step so we never overshoot the target.
            if (Mathf.Abs(rotated + step) > Mathf.Abs(totalDelta))
                step = totalDelta - rotated;

            transform.RotateAround(hingeWorld, Vector3.up, step);
            rotated += step;

            yield return null; // wait one frame before the next step
        }

        _isOpen   = !_isOpen;
        _isMoving = false;
    }
}
