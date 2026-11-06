using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Core game manager singleton: tracks score, player lives, level progression, and HUD updates.
// One GameManager per level. Owns score, lives, win/lose, respawn and the HUD texts.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Numbers")]
    public int targetScore = 8;
    public int startLives = 3;
    public float respawnDelay = 0.8f;
    public float respawnProtection = 2f;
    public string levelName = "Level";
    public string objective = "";

    [Header("Scene references (set per level)")]
    public Transform player;
    public Transform respawnPoint;

    [Header("UI (inside this prefab)")]
    public TMP_Text scoreText;
    public TMP_Text heartsText;
    public TMP_Text livesText;
    public TMP_Text powerText;
    public TMP_Text messageText;
    public TMP_Text titleText;
    public TMP_Text objectiveText;
    public GameObject winPanel;
    public GameObject losePanel;
    public TMP_Text winInfoText;
    public TMP_Text loseInfoText;
    public TMP_Text nextButtonLabel;

    public int Score { get; private set; }
    public int Lives { get; private set; }
    public bool GoalOpen => Score >= targetScore;
    public bool PlayerCanMove => state == State.Playing;

    enum State { Playing, Respawning, Won, Lost }
    State state = State.Playing;

    Health health;
    PlayerController controller;
    float messageUntil;
    string lastPower = "";
    const float SpawnHeight = 1.1f;   // spawn points sit on the floor

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;          // survives a scene load, so reset it here too
        Lives = startLives;
        if (player != null)
        {
            health = player.GetComponent<Health>();
            controller = player.GetComponent<PlayerController>();
        }
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
    }

    void Start()
    {
        if (titleText != null) titleText.text = levelName;
        if (objectiveText != null) objectiveText.text = objective;
        RefreshHud();
    }

    void Update()
    {
        if (messageUntil > 0f && Time.unscaledTime > messageUntil)
        {
            if (messageText != null) messageText.text = "";
            messageUntil = 0f;
        }

        // power-up timer on the HUD
        string p = "";
        if (controller != null && controller.BoostRemaining > 0f)
            p = "SPEED BOOST  " + controller.BoostRemaining.ToString("0.0") + "s";
        if (p != lastPower && powerText != null) { powerText.text = p; lastPower = p; }

        if (GameInput.Restart()) Restart();
        if (GameInput.Skip()) NextLevel();     // testing shortcut
    }

    // ---- score ---------------------------------------------------------------
    public void AddScore(int amount)
    {
        Score += amount;
        RefreshHud();
    }

    // Texts are set only when something changes, never every frame.
    public void RefreshHud()
    {
        if (scoreText != null) scoreText.text = "Score: " + Score + " / " + targetScore;
        if (heartsText != null && health != null)
            heartsText.text = "Hearts: " + Mathf.Max(health.current, 0) + " / " + health.max;
        if (livesText != null) livesText.text = "Lives: " + Lives;
    }

    public void ShowMessage(string text, float seconds)
    {
        if (messageText == null) return;
        if (messageText.text != text) messageText.text = text;
        messageUntil = Time.unscaledTime + seconds;
    }

    // ---- lives, respawn, losing ----------------------------------------------
    public void SetCheckpoint(Transform point) => respawnPoint = point;

    public void LoseLife(string reason)
    {
        if (state != State.Playing) return;
        Lives--;
        RefreshHud();
        if (Lives <= 0) Lose(reason);
        else StartCoroutine(Respawn(reason));
    }

    IEnumerator Respawn(string reason)
    {
        state = State.Respawning;
        ShowMessage(reason + "  Respawning...", respawnDelay + 0.6f);
        yield return new WaitForSeconds(respawnDelay);          // a beat

        controller.Teleport(respawnPoint.position + Vector3.up * SpawnHeight);
        health.ResetFull(respawnProtection);                    // moment of protection
        state = State.Playing;
    }

    void Lose(string reason)
    {
        state = State.Lost;
        Time.timeScale = 0f;
        if (loseInfoText != null) loseInfoText.text = reason + "\nScore: " + Score + " / " + targetScore;
        if (losePanel != null) losePanel.SetActive(true);
    }

    // ---- winning ---------------------------------------------------------------
    public void TryFinish()
    {
        if (state != State.Playing) return;
        if (!GoalOpen)
        {
            ShowMessage("Collect " + (targetScore - Score) + " more coin(s) first!", 1.5f);
            return;
        }
        Win();
    }

    void Win()
    {
        state = State.Won;
        Time.timeScale = 0f;
        bool last = SceneManager.GetActiveScene().buildIndex + 1 >= SceneManager.sceneCountInBuildSettings;
        if (nextButtonLabel != null) nextButtonLabel.text = last ? "Play again" : "Next level";
        if (winInfoText != null) winInfoText.text = "Score: " + Score + "    Lives left: " + Lives;
        if (winPanel != null) winPanel.SetActive(true);
    }

    // ---- buttons ---------------------------------------------------------------
    public void Restart()
    {
        Time.timeScale = 1f;                    // first, or the new level never moves
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void NextLevel()
    {
        Time.timeScale = 1f;
        int next = (SceneManager.GetActiveScene().buildIndex + 1) % SceneManager.sceneCountInBuildSettings;
        SceneManager.LoadScene(next);
    }
}
