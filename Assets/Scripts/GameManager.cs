using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controls core game loop: Win / Lose conditions, UI toggling, and scene restarts.
/// Attach this component to a GameManager GameObject in the scene.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI Panels")]
    public GameObject winPanel;
    public GameObject losePanel;

    [Header("Game State")]
    public bool isGameOver = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Ensure UI panels are hidden at start
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);

        // Resume time
        Time.timeScale = 1f;
    }

    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        Debug.Log("🚨 GAME OVER! Caught by a guard!");

        // Unlock mouse cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        // Use the animated GameOverScreen if available
        if (GameOverScreen.Instance != null)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayGameOver();
            GameOverScreen.Instance.Show();
        }
        else
        {
            if (losePanel != null) losePanel.SetActive(true);
            Time.timeScale = 0f;
        }
    }

    public void WinGame()
    {
        if (isGameOver) return;
        isGameOver = true;

        Debug.Log("🎉 YOU ESCAPED! You win the game!");

        // Unlock mouse cursor for UI interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        // Use the animated VictoryScreen if available
        if (VictoryScreen.Instance != null)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayVictory();
            VictoryScreen.Instance.Show();
        }
        else
        {
            if (winPanel != null) winPanel.SetActive(true);
            Time.timeScale = 0f;
        }
    }

    public void RestartGame()
    {
        Debug.Log("🔄 Restarting level...");
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
