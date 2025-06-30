using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    // Singleton pattern
    public static GameManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField]
    private TextMeshProUGUI scoreText;

    [SerializeField]
    private GameObject gameOverPanel;

    [SerializeField]
    private TextMeshProUGUI finalScoreText;

    [SerializeField]
    private TextMeshProUGUI highScoreText;

    [SerializeField]
    private TextMeshProUGUI levelText; // Text to display current level

    [SerializeField]
    private TextMeshProUGUI experienceText; // Text to display current XP

    [SerializeField]
    private Slider experienceBarR; // Bar to show progress to next level
    [SerializeField]
    private Slider experienceBarL; // Bar to show progress to next level

    [Header("Game Settings")]
    [SerializeField]
    private float gameDifficultyCurve = 0.1f; // How quickly the game gets harder

    [Header("Leveling System")]
    [SerializeField]
    private int baseExperienceToLevel = 100; // Base XP needed for level 1

    [SerializeField]
    private float logGrowthFactor = 0.3f; // How much logarithmic growth affects the curve

    [SerializeField]
    private float linearGrowthFactor = 0.4f; // How much linear growth affects the curve

    [SerializeField]
    private float expGrowthFactor = 0.001f; // How much exponential growth affects the curve

    [SerializeField]
    private float expGrowthPower = 1.5f; // Exponent for the power growth component

    // Game state
    private int currentScore = 0;
    private float gameTime = 0f;
    private bool isGameOver = false;

    // Level system
    private int currentLevel = 1;
    private int currentExperience = 0;
    private int experienceToNextLevel;

    // References to managers
    private DemonSpawner demonSpawner;
    private GameTimer gameTimer;

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Find references
        demonSpawner = FindFirstObjectByType<DemonSpawner>();
        gameTimer = FindFirstObjectByType<GameTimer>();

        // Initialize game
        InitializeGame();
    }

    private void InitializeGame()
    {
        // Reset game state
        currentScore = 0;
        gameTime = 0f;
        isGameOver = false;

        // Reset level system
        currentLevel = 1;
        currentExperience = 0;
        CalculateExperienceToNextLevel();

        // Set up experience slider
        if (experienceBarR != null)
        {
            experienceBarR.minValue = 0f;
            experienceBarR.maxValue = 1f;
            experienceBarR.value = 0f;
        }


        // Set up experience slider
        if (experienceBarL != null)
        {
            experienceBarL.minValue = 0f;
            experienceBarL.maxValue = 1f;
            experienceBarL.value = 0f;
        }

        // Update UI
        UpdateScoreUI();
        UpdateLevelUI();

        // Hide game over panel
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        // Start timer
        if (gameTimer != null)
        {
            gameTimer.ResetTimer();
            gameTimer.StartTimer();
        }
    }

    private void Update()
    {
        if (isGameOver)
            return;

        // Update game time
        gameTime += Time.deltaTime;

        // Increase difficulty over time
        if (demonSpawner != null)
        {
            // Example of how difficulty could increase:
            // - Adjust spawn rates
            // - Increase number of enemies
            // - Spawn tougher enemies

            // This is just a placeholder - you'll want to implement your own difficulty curve
            float difficultyMultiplier = 1f + (gameTime * gameDifficultyCurve);
        }
    }

    public void AddScore(int points)
    {
        if (isGameOver)
            return;

        currentScore += points;
        UpdateScoreUI();
    }

    public void AddExperience(int experience)
    {
        if (isGameOver)
            return;

        currentExperience += experience;

        // Check for level up
        while (currentExperience >= experienceToNextLevel)
        {
            LevelUp();
        }

        UpdateLevelUI();
    }

    private void LevelUp()
    {
        // Subtract experience needed for this level
        currentExperience -= experienceToNextLevel;

        // Increase level
        currentLevel++;

        // Calculate new experience needed for next level
        CalculateExperienceToNextLevel();

        // Play level up effect or sound here
        Debug.Log("Level Up! Now level " + currentLevel);

        // Could grant rewards, increase player stats, etc.
    }

    private void CalculateExperienceToNextLevel()
    {
        // Custom formula combining logarithmic, linear, and exponential growth
        // This creates a curve that starts slow but accelerates at higher levels

        // Logarithmic component (dominant at early levels)
        float logComponent = logGrowthFactor * Mathf.Log(currentLevel + 1, 2);

        // Linear component (steady growth)
        float linearComponent = linearGrowthFactor * currentLevel;

        // Exponential component (becomes dominant at higher levels)
        float expComponent = expGrowthFactor * Mathf.Pow(currentLevel, expGrowthPower);

        // Combine components and multiply by base experience
        experienceToNextLevel = Mathf.RoundToInt(
            baseExperienceToLevel * (1.0f + logComponent + linearComponent + expComponent)
        );

        // Ensure minimum experience requirement
        experienceToNextLevel = Mathf.Max(baseExperienceToLevel, experienceToNextLevel);

        // Debug log to show the experience curve
        Debug.Log($"Level {currentLevel} requires {experienceToNextLevel} experience");
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = currentScore.ToString("N0");
        }
    }

    private void UpdateLevelUI()
    {
        // Update level text
        if (levelText != null)
        {
            levelText.text = "LVL " + currentLevel.ToString();
        }

        // Update experience text
        if (experienceText != null)
        {
            experienceText.text = currentExperience + " / " + experienceToNextLevel;
        }

        // Update experience bar
        if (experienceBarR != null && experienceBarL != null)
        {
            float fillAmount = (float)currentExperience / experienceToNextLevel;
            experienceBarR.value = fillAmount;
            experienceBarL.value = fillAmount;
        }
    }

    public void PlayerDied()
    {
        if (isGameOver)
            return;

        // Game over
        isGameOver = true;

        // Stop timer
        if (gameTimer != null)
        {
            gameTimer.StopTimer();
        }

        // Show game over panel
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);

            // Update final score
            if (finalScoreText != null)
            {
                finalScoreText.text = "Score: " + currentScore.ToString("N0");
            }

            // Check for high score
            int highScore = PlayerPrefs.GetInt("HighScore", 0);
            if (currentScore > highScore)
            {
                // New high score
                highScore = currentScore;
                PlayerPrefs.SetInt("HighScore", highScore);
                PlayerPrefs.Save();
            }

            // Update high score text
            if (highScoreText != null)
            {
                highScoreText.text = "High Score: " + highScore.ToString("N0");
            }
        }
    }

    // Public methods for UI buttons

    public void RestartGame()
    {
        // Reload the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitToMainMenu()
    {
        // Load the main menu scene
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
