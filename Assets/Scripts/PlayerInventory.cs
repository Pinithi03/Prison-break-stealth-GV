using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages player items and keycards.
/// Attach this component to the Player GameObject.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    private HashSet<string> items = new HashSet<string>();

    public void AddItem(string itemName)
    {
        if (!items.Contains(itemName))
        {
            items.Add(itemName);
            Debug.Log("Inventory Added: " + itemName);
        }
    }

    public bool HasItem(string itemName)
    {
        return items.Contains(itemName);
    }

    public void RemoveItem(string itemName)
    {
        if (items.Contains(itemName))
        {
            items.Remove(itemName);
            Debug.Log("Inventory Removed: " + itemName);
        }
    }

    public int GetKeycardCount()
    {
        int count = 0;
        if (HasItem("Keycard_1")) count++;
        if (HasItem("Keycard_2")) count++;
        if (HasItem("Keycard_3")) count++;
        return count;
    }
}
