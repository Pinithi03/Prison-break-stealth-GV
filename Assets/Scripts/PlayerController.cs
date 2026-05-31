using UnityEngine;

/// <summary>
/// Player controller for Prison Break: Silent Escape.
/// Supports both First-Person and Third-Person views (toggled with 'C' key).
/// Attach this to the Player GameObject.
/// Requires: CharacterController component on the same GameObject.
/// Setup: Drag Main Camera as a child of Player.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public enum CameraView { FirstPerson, ThirdPerson }

    [Header("Camera Views")]
    public CameraView currentView = CameraView.FirstPerson;
    public float thirdPersonDistance = 3.5f;
    public float thirdPersonHeight = 1.6f;

    [Header("Movement Settings")]
    public float moveSpeed = 4f;
    public float gravity = -9.81f;

    [Header("Mouse Look Settings")]
    public float mouseSensitivity = 2f;
    public Transform cameraTransform;

    [Header("Interaction")]
    public float interactRange = 2.5f;

    [Header("Character Model & Animation")]
    public Transform characterModel;
    private Animator anim;
    private Renderer[] modelRenderers;

    private CharacterController controller;
    private Vector3 velocity;
    private float xRotation = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cameraTransform == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null)
                cameraTransform = cam.transform;
            else
                Debug.LogError("PlayerController: Assign Main Camera to Camera Transform slot in Inspector!");
        }

        InitializeModelAndAnimator();
        UpdateCameraViewSettings();
    }

    void Update()
    {
        // Toggle camera view with C key
        if (Input.GetKeyDown(KeyCode.C))
        {
            currentView = (currentView == CameraView.FirstPerson) ? CameraView.ThirdPerson : CameraView.FirstPerson;
            UpdateCameraViewSettings();
        }

        HandleMouseLook();
        HandleMovement();
        HandleInteraction();

        // Robust dynamic check for animator/model
        if (anim == null)
        {
            InitializeModelAndAnimator();
            UpdateCameraViewSettings();
        }

        UpdateAnimation();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void InitializeModelAndAnimator()
    {
        anim = GetComponentInChildren<Animator>();
        if (anim != null)
        {
            if (characterModel == null)
            {
                characterModel = anim.transform;
            }
            modelRenderers = characterModel.GetComponentsInChildren<Renderer>();

            // Force Root Motion to FALSE to prevent the animation from pushing the player through walls/doors
            anim.applyRootMotion = false;

            // Disable all colliders on the child model to prevent self-collision physics glitches
            Collider[] childColliders = characterModel.GetComponentsInChildren<Collider>();
            foreach (var col in childColliders)
            {
                if (col != null && col.gameObject != this.gameObject)
                {
                    col.enabled = false;
                    Debug.Log($"Disabled child model collider: {col.gameObject.name} to prevent physics conflicts.");
                }
            }
        }
    }

    void UpdateCameraViewSettings()
    {
        bool showModel = (currentView == CameraView.ThirdPerson);
        if (modelRenderers != null)
        {
            foreach (var r in modelRenderers)
            {
                if (r != null)
                    r.enabled = showModel;
            }
        }
    }

    void HandleMouseLook()
    {
        if (cameraTransform == null) return;

        // Raw mouse axes are already delta-time independent. Multiplying by Time.deltaTime caused sluggish and frame-rate-dependent look speed.
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        xRotation -= mouseY;

        if (currentView == CameraView.FirstPerson)
        {
            xRotation = Mathf.Clamp(xRotation, -80f, 80f);
            cameraTransform.localPosition = new Vector3(0f, 1.6f, 0f);
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
        else
        {
            // Narrower range in 3rd person to prevent camera going through ground
            xRotation = Mathf.Clamp(xRotation, -30f, 60f);
            float pitchRad = xRotation * Mathf.Deg2Rad;
            cameraTransform.localPosition = new Vector3(
                0f,
                thirdPersonHeight + Mathf.Sin(pitchRad) * thirdPersonDistance,
                -Mathf.Cos(pitchRad) * thirdPersonDistance
            );
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }

        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleMovement()
    {
        bool isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical   = Input.GetAxis("Vertical");

        Vector3 move = transform.right * horizontal + transform.forward * vertical;
        controller.Move(move * moveSpeed * Time.deltaTime);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void UpdateAnimation()
    {
        if (anim != null && anim.runtimeAnimatorController != null)
        {
            // Calculate speed on the horizontal plane
            float speed = new Vector3(controller.velocity.x, 0f, controller.velocity.z).magnitude;

            // Handle speed fallback when first starting to move
            if (speed < 0.1f)
            {
                float horizontal = Input.GetAxis("Horizontal");
                float vertical = Input.GetAxis("Vertical");
                if (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f)
                {
                    speed = moveSpeed;
                }
            }

            anim.SetFloat("Speed", speed);

            if (speed > 0.1f)
            {
                anim.speed = 1f;
            }
            else
            {
                anim.speed = 0f;
            }
        }
    }

    void HandleInteraction()
    {
        if (!Input.GetKeyDown(KeyCode.E)) return;
        if (cameraTransform == null) return;

        // Start the ray at the player's head level, projected slightly forward (0.6f) to avoid hitting the player's own collider
        Vector3 rayStart = transform.position + Vector3.up * 1.6f + transform.forward * 0.6f;
        Vector3 rayDir = cameraTransform.forward;

        Ray ray = new Ray(rayStart, rayDir);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactRange))
        {
            PlayerInventory inventory = GetComponent<PlayerInventory>();
            if (inventory == null)
            {
                Debug.LogWarning("PlayerController: Interaction failed because PlayerInventory is missing on the Player GameObject! Please select the Player GameObject in Hierarchy and click Add Component -> Player Inventory.");
                return;
            }

            // 1. Try to open Door
            DoorController door = hit.collider.GetComponentInParent<DoorController>();
            if (door == null)
            {
                door = hit.collider.GetComponentInChildren<DoorController>();
            }

            if (door != null)
            {
                door.TryOpen(inventory);
                return;
            }

            // 2. Try to pick up Keycard/Tool
            KeycardPickup pickup = hit.collider.GetComponentInParent<KeycardPickup>();
            if (pickup == null)
            {
                pickup = hit.collider.GetComponentInChildren<KeycardPickup>();
            }

            if (pickup != null)
            {
                pickup.Collect(inventory);
                return;
            }

            Debug.Log("Interacted with: " + hit.collider.gameObject.name);
        }
    }
}

