using UnityEngine;

/// <summary>
/// Attach this script to Keycards and the Hidden_Tool GameObjects.
/// Ensure the collider is set to "Is Trigger" on the GameObject.
/// </summary>
public class KeycardPickup : MonoBehaviour
{
    [Header("Item Settings")]
    [Tooltip("The unique identifier for this item (e.g. Keycard_1, Keycard_2, Keycard_3, Hidden_Tool)")]
    public string itemID;

    private void Start()
    {
        // Try to auto-detect ID from GameObject name if not set
        if (string.IsNullOrEmpty(itemID))
        {
            itemID = gameObject.name;
        }

        // Auto-configure collider
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
        else
        {
            Debug.LogWarning($"KeycardPickup: GameObject '{gameObject.name}' is missing a Collider! Please add one.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerInventory inventory = other.GetComponent<PlayerInventory>();
            if (inventory != null)
            {
                Collect(inventory);
            }
        }
    }

    public void Collect(PlayerInventory inventory)
    {
        inventory.AddItem(itemID);
        
        // Print feedback in Console
        Debug.Log($"🎉 Picked up {itemID}!");

        // Deactivate the item so it disappears from the level
        gameObject.SetActive(false);
    }
}
