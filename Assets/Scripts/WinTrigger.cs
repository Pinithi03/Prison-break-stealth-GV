using UnityEngine;

/// <summary>
/// Attach this script to a trigger collider positioned at the exit gate.
/// Triggers the Win Game UI when the player reaches the escape point.
/// </summary>
public class WinTrigger : MonoBehaviour
{
    private void Start()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.WinGame();
            }
            else
            {
                Debug.LogError("🎉 Player Escaped! (GameManager.Instance is null, make sure GameManager script is in your scene!)");
            }
        }
    }
}
