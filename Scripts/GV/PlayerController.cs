// =============================================================================
// PlayerController.cs
// Student 2 — GV (SE3032) | Prison Break: Silent Escape
// Role: Systems Engineer — Player Interaction Physics
//
// Description:
//   First-person player controller using Unity's CharacterController component.
//   Handles WASD movement, sprint/walk speed toggle (stealth mechanic),
//   mouse look with pitch clamping, and forwards player push force to
//   Rigidbody objects (barricades) via OnControllerColliderHit.
//
// Unity Setup:
//   1. Add CharacterController component to the Player GameObject.
//   2. Attach this script to the Player GameObject.
//   3. Assign the Main Camera (child of Player) to the 'playerCamera' field.
//   4. Tag the Player GameObject as "Player".
//   5. Set Player layer to "Player" and configure collision matrix in
//      Project Settings > Physics so Player doesn't collide with itself.
// =============================================================================

using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    // ── Inspector Fields ──────────────────────────────────────────────────────

    [Header("Camera")]
    [Tooltip("Assign the child Camera transform here.")]
    [SerializeField] private Transform playerCamera;

    [Header("Movement Speeds")]
    [Tooltip("Default walk speed (metres per second).")]
    [SerializeField] private float walkSpeed = 3.5f;

    [Tooltip("Sprint speed. Hold Left Shift to sprint.")]
    [SerializeField] private float sprintSpeed = 6.5f;

    [Tooltip("Crouching speed for maximum stealth.")]
    [SerializeField] private float crouchSpeed = 1.8f;

    [Header("Mouse Look")]
    [Tooltip("Mouse sensitivity — horizontal and vertical.")]
    [SerializeField] private float mouseSensitivity = 2.0f;

    [Tooltip("Maximum upward pitch angle (degrees).")]
    [SerializeField] private float maxPitchUp = 80f;

    [Tooltip("Maximum downward pitch angle (degrees).")]
    [SerializeField] private float maxPitchDown = 80f;

    [Header("Physics")]
    [Tooltip("Downward gravity applied each FixedUpdate (m/s²).")]
    [SerializeField] private float gravity = -9.81f;

    [Tooltip("Force multiplied by mass applied to pushed Rigidbodies.")]
    [SerializeField] private float pushForce = 4.5f;

    // ── Private State ─────────────────────────────────────────────────────────

    private CharacterController _cc;
    private Vector3 _velocity;       // accumulated vertical velocity (gravity)
    private float _pitchAngle;       // current camera pitch (degrees), clamped
    private bool _isCrouching;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();

        // Lock and hide the cursor for FPS mouse look.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// Mouse look runs in Update so it matches the render frame rate exactly,
    /// preventing the stuttering you get when look is tied to FixedUpdate.
    /// </summary>
    private void Update()
    {
        HandleMouseLook();
        HandleMovement();
    }

    // ── Private Methods ───────────────────────────────────────────────────────

    /// <summary>
    /// Rotates the player body (Y-axis / yaw) and the camera (X-axis / pitch).
    /// Pitch is clamped to prevent the camera from flipping.
    /// </summary>
    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Rotate entire player GameObject horizontally (yaw).
        transform.Rotate(Vector3.up * mouseX);

        // Accumulate pitch and clamp it before applying.
        _pitchAngle -= mouseY;
        _pitchAngle = Mathf.Clamp(_pitchAngle, -maxPitchDown, maxPitchUp);
        playerCamera.localEulerAngles = new Vector3(_pitchAngle, 0f, 0f);
    }

    /// <summary>
    /// Reads WASD + sprint/crouch input, builds a movement vector, and
    /// applies gravity. All motion goes through CharacterController.Move()
    /// which handles collision response and sliding automatically.
    ///
    /// DESIGN CHOICE — CharacterController vs Rigidbody:
    ///   We use CharacterController because it gives direct, responsive
    ///   control without fighting the physics solver. A Rigidbody player
    ///   would require careful friction/drag tuning and can wobble when
    ///   colliding with other Rigidbodies (barricades). CharacterController
    ///   sidesteps those issues while still honouring PhysX collision geometry.
    /// </summary>
    private void HandleMovement()
    {
        // Ground check — reset vertical velocity when grounded.
        if (_cc.isGrounded && _velocity.y < 0f)
            _velocity.y = -2f; // small negative keeps grounded flag stable

        // Determine current speed mode.
        float speed = walkSpeed;
        if (Input.GetKey(KeyCode.LeftShift))
            speed = sprintSpeed;
        else if (Input.GetKey(KeyCode.LeftControl) || _isCrouching)
            speed = crouchSpeed;

        // Build move direction relative to the player's facing direction.
        float h = Input.GetAxisRaw("Horizontal"); // A/D
        float v = Input.GetAxisRaw("Vertical");   // W/S
        Vector3 move = (transform.right * h + transform.forward * v).normalized;

        // Apply horizontal movement.
        _cc.Move(move * speed * Time.deltaTime);

        // Accumulate and apply gravity.
        _velocity.y += gravity * Time.deltaTime;
        _cc.Move(_velocity * Time.deltaTime);
    }

    /// <summary>
    /// Called automatically by CharacterController when the player walks into
    /// a collider that has a non-kinematic Rigidbody (e.g. a barricade).
    ///
    /// DESIGN CHOICE — Push via OnControllerColliderHit:
    ///   We read the collision normal and apply AddForce() to the hit
    ///   Rigidbody. This keeps the push physically grounded in the engine's
    ///   solver — mass, drag, and constraints are all respected automatically.
    ///   pushForce is serialized so it can be tuned in the Inspector without
    ///   recompiling, and is multiplied by the object's mass so heavier
    ///   barricades require more sustained pressure to move.
    /// </summary>
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody hitRb = hit.collider.attachedRigidbody;

        // Skip if there's no Rigidbody, or if it's kinematic (e.g. doors).
        if (hitRb == null || hitRb.isKinematic) return;

        // Only push objects on the horizontal plane — prevents launching them upward.
        if (hit.moveDirection.y < -0.3f) return;

        // Project push direction onto the horizontal plane only.
        Vector3 pushDir = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z);

        // Apply force proportional to the object's mass for realistic feel.
        hitRb.AddForce(pushDir * pushForce * hitRb.mass, ForceMode.Force);
    }
}
