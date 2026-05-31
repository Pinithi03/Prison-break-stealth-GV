using UnityEngine;

/// <summary>
/// Controls door locking, unlocking, and smooth opening animation.
/// Attach this component to any Door GameObjects in the scene.
/// </summary>
public class DoorController : MonoBehaviour
{
    public enum DoorType { Rotate, Slide }

    [Header("Requirements")]
    [Tooltip("Item name needed to unlock this door (e.g. Hidden_Tool, Keycard_2)")]
    public string requiredItemID;
    [Tooltip("If true, requires Keycard_1, Keycard_2, and Keycard_3 to unlock")]
    public bool requiresAllKeycards = false;

    [Header("Open Style")]
    public DoorType doorType = DoorType.Rotate;
    [Tooltip("For Rotate: Euler angles rotation offset. For Slide: Position offset in local space.")]
    public Vector3 openOffset = new Vector3(0f, 90f, 0f);
    public float openSpeed = 3f;

    [Header("State")]
    public bool isOpened = false;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 targetPosition;
    private Quaternion targetRotation;

    void Start()
    {
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;

        if (doorType == DoorType.Rotate)
        {
            targetRotation = initialRotation * Quaternion.Euler(openOffset);
            targetPosition = initialPosition;
        }
        else
        {
            targetRotation = initialRotation;
            targetPosition = initialPosition + openOffset;
        }
    }

    void Update()
    {
        if (isOpened)
        {
            if (doorType == DoorType.Rotate)
            {
                transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * openSpeed);
            }
            else
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * openSpeed);
            }
        }
    }

    public bool TryOpen(PlayerInventory inventory)
    {
        if (isOpened) return true;

        if (requiresAllKeycards)
        {
            if (inventory.HasItem("Keycard_1") && inventory.HasItem("Keycard_2") && inventory.HasItem("Keycard_3"))
            {
                OpenDoor();
                return true;
            }
            else
            {
                Debug.Log("🔒 Locked! You need all 3 keycards to escape!");
                return false;
            }
        }

        if (string.IsNullOrEmpty(requiredItemID) || inventory.HasItem(requiredItemID))
        {
            OpenDoor();
            return true;
        }

        Debug.Log($"🔒 Locked! You need the '{requiredItemID}' to open this.");
        return false;
    }

    private void OpenDoor()
    {
        isOpened = true;
        Debug.Log($"🔓 Door '{gameObject.name}' is now open!");
    }
}
