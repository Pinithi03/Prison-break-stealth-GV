using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton that manages all game objectives in order.
/// Other scripts call CompleteObjective(id) to advance the tracker.
/// Attach this to a GameObject named "ObjectiveManager" in the scene.
/// </summary>
public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance { get; private set; }

    [System.Serializable]
    public class Objective
    {
        public string id;
        public string displayText;
        [HideInInspector] public bool isCompleted = false;
        [HideInInspector] public bool isCurrent = false;
    }

    // All objectives defined in order
    public List<Objective> objectives = new List<Objective>
    {
        new Objective { id = "find_tool",        displayText = "Find the Hidden Tool" },
        new Objective { id = "break_cell_door",  displayText = "Use Hidden Tool to break the Cell Door (E)" },
        new Objective { id = "find_keycard1",    displayText = "Find Keycard 1 in the Corridor" },
        new Objective { id = "unlock_security",  displayText = "Unlock the Security Room Door (E)" },
        new Objective { id = "find_keycard2",    displayText = "Find Keycard 2 in the Security Room" },
        new Objective { id = "find_keycard3",    displayText = "Find Keycard 3" },
        new Objective { id = "escape",           displayText = "Unlock Exit Gate and Escape! (E)" },
    };

    private int currentIndex = 0;

    // Event so ObjectiveUI can refresh whenever something changes
    public event System.Action OnObjectivesChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Mark first objective as current
        if (objectives.Count > 0)
            objectives[0].isCurrent = true;
    }

    /// <summary>
    /// Call this from any script to complete the objective with the given id.
    /// If it matches the current objective, it advances to the next one.
    /// </summary>
    public void CompleteObjective(string id)
    {
        if (currentIndex >= objectives.Count) return;

        Objective current = objectives[currentIndex];
        if (current.id != id) return;

        current.isCompleted = true;
        current.isCurrent  = false;
        currentIndex++;

        if (currentIndex < objectives.Count)
            objectives[currentIndex].isCurrent = true;

        // Play objective complete chime
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayObjectiveComplete();

        Debug.Log($"[Objectives] Completed: {current.displayText}");
        OnObjectivesChanged?.Invoke();
    }

    public Objective GetCurrentObjective()
    {
        if (currentIndex < objectives.Count)
            return objectives[currentIndex];
        return null;
    }

    public bool IsCompleted(string id)
    {
        var obj = objectives.Find(o => o.id == id);
        return obj != null && obj.isCompleted;
    }
}
