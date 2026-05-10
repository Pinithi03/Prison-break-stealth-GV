// =============================================================================
// ExitGateTrigger.cs
// Student 2 — GV (SE3032) | Prison Break: Silent Escape
// Role: Systems Engineer — Player Interaction Physics
//
// Description:
//   Placed on the exit gate trigger zone. When the player steps inside with
//   all 3 keycards, triggers the win sequence via GameManager.
//   If the player steps inside WITHOUT all keycards, shows a UI warning.
//
// Unity Setup:
//   1. Create a trigger collider at the exit gate (BoxCollider, isTrigger=true).
//   2. Attach this script to it.
//   3. Assign to GameManager's 'exitGateTrigger' field — it starts disabled
//      and is enabled by GameManager when all 3 keycards are collected.
// =============================================================================

using UnityEngine;
using TMPro;

public class ExitGateTrigger : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI warningText;
    [SerializeField] private float warningDisplayTime = 2.5f;

    private Coroutine _warningCoroutine;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (GameManager.Instance != null && GameManager.Instance.HasAllKeycards)
        {
            GameManager.Instance.OnPlayerEscaped();
        }
        else
        {
            // Should not normally happen since this object is disabled until
            // all keycards are collected, but guard against it anyway.
            Debug.LogWarning("[ExitGateTrigger] Player reached gate without all keycards.");
            ShowWarning("You need all 3 keycards to escape!");
        }
    }

    private void ShowWarning(string msg)
    {
        if (warningText == null) return;
        warningText.text = msg;
        warningText.gameObject.SetActive(true);

        if (_warningCoroutine != null) StopCoroutine(_warningCoroutine);
        _warningCoroutine = StartCoroutine(HideWarningAfterDelay());
    }

    private System.Collections.IEnumerator HideWarningAfterDelay()
    {
        yield return new WaitForSeconds(warningDisplayTime);
        if (warningText != null) warningText.gameObject.SetActive(false);
    }
}
