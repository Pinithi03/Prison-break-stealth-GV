// =============================================================================
// BarricadePhysics.cs
// Student 2 — GV (SE3032) | Prison Break: Silent Escape
// Role: Systems Engineer — Player Interaction Physics
//
// Description:
//   Configures the barricade's Rigidbody for physics-accurate sliding when
//   pushed by the player. Applies Rigidbody constraints to prevent the
//   barricade from tipping or floating. Enables Continuous Collision Detection
//   (CCD) to prevent tunnelling at high push speeds.
//
//   Also acts as the GV-side bridge to the IS BarricadeEventInterceptor —
//   notifies it whenever the barricade settles to a new position.
//
// DESIGN CHOICES (for Viva):
//   - Dynamic Rigidbody (not Kinematic): The physics solver handles mass-based
//     push response. The player doesn't need special force calculation — just
//     AddForce in PlayerController.OnControllerColliderHit. The engine handles
//     the rest (friction, collision, mass).
//   - CCD (Continuous Collision Detection): Prevents the barricade from
//     tunnelling through thin walls if pushed at high speed. CCD sweeps the
//     collider's trajectory each physics step rather than only testing the
//     end-point position.
//   - Constraints: FreezePositionY prevents the barricade from being launched
//     upward on a ramp edge. FreezeRotationX and FreezeRotationZ prevent
//     tipping which would cause visual/gameplay issues and odd collider states.
//
// Unity Setup:
//   1. Create a Barricade GameObject with a mesh + BoxCollider.
//   2. Add a Rigidbody component (leave default settings — this script
//      overwrites the critical settings in Awake()).
//   3. Attach this script to the Barricade GameObject.
//   4. The BarricadeEventInterceptor (IS script) must ALSO be on this object.
//   5. Set the barricade's layer to "Barricade" for collision layer filtering.
//   6. Tag it "Barricade" for IS edge detection.
// =============================================================================

using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
public class BarricadePhysics : MonoBehaviour
{
    // ── Inspector Fields ──────────────────────────────────────────────────────

    [Header("Physics Configuration")]
    [Tooltip("Mass of the barricade in kg. Heavier = harder to push.")]
    [SerializeField] private float mass = 80f;

    [Tooltip("Linear drag — how quickly the barricade decelerates after being pushed.")]
    [SerializeField] private float linearDrag = 4f;

    [Tooltip("Angular drag — resists spinning when colliding at an angle.")]
    [SerializeField] private float angularDrag = 10f;

    [Header("Debug")]
    [Tooltip("Show the settled position in the console when barricade stops moving.")]
    [SerializeField] private bool debugLogSettlement = true;

    // ── Private State ─────────────────────────────────────────────────────────

    private Rigidbody _rb;
    private bool _hasSettled = true;
    private Vector3 _lastPosition;

    // Threshold below which we consider the barricade "settled".
    private const float SettleVelocityThreshold = 0.05f;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        ConfigureRigidbody();
        _lastPosition = transform.position;
    }

    /// <summary>
    /// Monitors velocity to detect when the barricade has settled after being
    /// pushed. Uses FixedUpdate so it's synchronised with the physics step.
    ///
    /// DESIGN CHOICE — FixedUpdate for physics queries:
    ///   Rigidbody.velocity is only meaningful when read in sync with the
    ///   physics simulation (FixedUpdate). Reading it in Update can give
    ///   stale or interpolated values, leading to false "settled" detections.
    /// </summary>
    private void FixedUpdate()
    {
        bool isMoving = _rb.velocity.magnitude > SettleVelocityThreshold
                     || _rb.angularVelocity.magnitude > SettleVelocityThreshold;

        if (isMoving)
        {
            _hasSettled = false;
        }
        else if (!_hasSettled)
        {
            // Barricade just came to rest.
            _hasSettled = true;

            if (debugLogSettlement)
                Debug.Log($"[BarricadePhysics] '{name}' settled at {transform.position}");

            // Notify the IS interceptor that the barricade has moved and settled.
            // The IS script handles its own edge detection from here.
            SendMessage("OnBarricadeSettled", SendMessageOptions.DontRequireReceiver);
        }
    }

    // ── Private Methods ───────────────────────────────────────────────────────

    /// <summary>
    /// Applies the correct Rigidbody settings in code rather than relying on
    /// Inspector defaults. This ensures the settings are always correct even if
    /// someone accidentally changes them in the Inspector.
    /// </summary>
    private void ConfigureRigidbody()
    {
        _rb.mass        = mass;
        _rb.drag        = linearDrag;
        _rb.angularDrag = angularDrag;

        // PHYSICS JUSTIFICATION:
        // FreezePositionY: prevents the barricade from being lifted off the
        //   floor by any upward impulse from ramps or player collision normals.
        // FreezeRotationX + FreezeRotationZ: prevents the barricade tipping
        //   forward/backward (X) or sideways (Z). It can still spin on the Y
        //   axis when hit at an angle, which is intentional (feels natural).
        _rb.constraints = RigidbodyConstraints.FreezePositionY
                        | RigidbodyConstraints.FreezeRotationX
                        | RigidbodyConstraints.FreezeRotationZ;

        // CCD JUSTIFICATION:
        // Discrete CCD only tests the collider at the END of each physics step.
        // If the barricade moves faster than its own thickness in one step,
        // it can pass through thin walls. Continuous mode sweeps the volume
        // along the trajectory, catching any intersection during the move.
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        _rb.isKinematic = false; // Explicit — Dynamic Rigidbody.
    }
}
