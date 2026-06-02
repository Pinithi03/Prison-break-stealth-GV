// =============================================================================
// GameManager.cs
// Student 2 — GV (SE3032) | Prison Break: Silent Escape
// Role: Systems Engineer — Player Interaction Physics
//
// Description:
//   Central game state manager. Tracks keycard collection (3 required),
//   manages win and lose conditions, and controls the HUD. Implemented
//   as a persistent Singleton accessible by all scripts in the scene.
//
//   Win:  Collect all 3 keycards → unlock exit gate → pass through exit
//   Lose: Guard catches the player → game over
//
// Unity Setup:
//   1. Create an empty GameObject named "GameManager" in the scene.
//   2. Attach this script to it.
//   3. Assign UI references (keycardText, gameOverPanel, winPanel) in
//      the Inspector — see HUDController.cs for UI setup.
//   4. Assign the exit gate GameObject (should become active when all
//      3 keycards are collected) to the 'exitGate' field.
//   5. Only ONE GameManager should exist in any scene.
// =============================================================================

using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // DontDestroyOnLoad if needed across scenes — disabled here since the
        // game restarts from scratch on Game Over.
    }

    // ── Inspector Fields ──────────────────────────────────────────────────────

    [Header("Game State")]
    [Tooltip("Total keycards required to unlock the exit. Always 3 per GDD.")]
    [SerializeField] private int totalKeycards = 3;

    [Header("Scene References")]
    [Tooltip("The exit gate — enable interaction on it when all 3 keys collected.")]
    [SerializeField] private GameObject exitGateTrigger;

    [Header("UI References")]
    [Tooltip("TextMeshPro text element showing 'Keycards: X / 3'.")]
    [SerializeField] private TextMeshProUGUI keycardCountText;

    [Tooltip("Game Over panel (shown when guard catches player).")]
    [SerializeField] private GameObject gameOverPanel;

    [Tooltip("Win / Escape panel (shown when player exits through gate).")]
    [SerializeField] private GameObject winPanel;

    [Tooltip("Name of the scene to reload on restart.")]
    [SerializeField] private string sceneToReload = "PrisonBreak";

    // ── Public State ──────────────────────────────────────────────────────────

    /// <summary>Number of keycards the player currently holds.</summary>
    public int KeycardsCollected { get; private set; }

    /// <summary>True once all 3 keycards are collected.</summary>
    public bool HasAllKeycards => KeycardsCollected >= totalKeycards;

    // ── Private State ─────────────────────────────────────────────────────────

    private bool _gameOver;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        // Initialise UI state.
        UpdateKeycardHUD();
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (winPanel     != null) winPanel.SetActive(false);

        // Exit gate starts locked.
        if (exitGateTrigger != null) exitGateTrigger.SetActive(false);

        // Release cursor just in case it gets locked on scene load.
        Time.timeScale = 1f;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by KeycardPickup when the player collects a keycard.
    /// Logs the card ID, updates the HUD, and checks the win state.
    /// </summary>
    public void OnKeycardCollected(int keycardID)
    {
        if (_gameOver) return;

        KeycardsCollected++;
        Debug.Log($"[GameManager] Keycard {keycardID} collected. Total: {KeycardsCollected}/{totalKeycards}");

        UpdateKeycardHUD();

        if (HasAllKeycards)
            OnAllKeycardsCollected();
    }

    /// <summary>
    /// Called by the exit gate trigger when the player passes through with
    /// all keycards. Triggers the win sequence.
    /// </summary>
    public void OnPlayerEscaped()
    {
        if (_gameOver) return;
        _gameOver = true;

        Debug.Log("[GameManager] *** PLAYER ESCAPED — WIN! ***");

        // Show win screen, freeze time.
        if (winPanel != null) winPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        Time.timeScale   = 0f;
    }

    /// <summary>
    /// Called by the guard's catch detection logic (S4 script) when the guard
    /// reaches the player. Triggers game over.
    /// </summary>
    public void OnPlayerCaught()
    {
        if (_gameOver) return;
        _gameOver = true;

        Debug.Log("[GameManager] *** PLAYER CAUGHT — GAME OVER ***");

        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        Time.timeScale   = 0f;
    }

    /// <summary>
    /// Called from the Game Over / Win UI Restart button.
    /// </summary>
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneToReload);
    }

    // ── Private Methods ───────────────────────────────────────────────────────

    /// <summary>
    /// Activates the exit gate trigger so the player can pass through.
    /// Guards will still be active — the final dash to the gate is the
    /// highest-risk moment in the game per the GDD.
    /// </summary>
    private void OnAllKeycardsCollected()
    {
        Debug.Log("[GameManager] All keycards collected — exit gate UNLOCKED.");
        if (exitGateTrigger != null) exitGateTrigger.SetActive(true);
    }

    /// <summary>Updates the keycard counter text on the HUD.</summary>
    private void UpdateKeycardHUD()
    {
        if (keycardCountText != null)
            keycardCountText.text = $"Keycards: {KeycardsCollected} / {totalKeycards}";
    }
}
