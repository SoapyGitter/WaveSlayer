using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameTimer : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI timerText; // Use Text component if not using TextMeshPro
    [SerializeField] private TextMeshProUGUI fpsText; // Text component for displaying FPS
    
    [Header("Timer Settings")]
    [SerializeField] private bool startOnAwake = true;
    [SerializeField] private bool countUp = true; // true = count up from zero, false = count down
    [SerializeField] private float countdownStartTime = 60f; // Time in seconds to count down from when countUp is false
    
    [Header("FPS Display Settings")]
    [SerializeField] private bool showFPS = true; // Toggle to show/hide FPS counter
    [SerializeField] private float fpsUpdateInterval = 0.5f; // How often to update the FPS display in seconds
    [SerializeField] private bool useColorCoding = true; // Color code FPS based on performance
    
    // Timer variables
    private float elapsedTime = 0f;
    private bool isRunning = false;
    
    // FPS calculation variables
    private float fpsAccumulator = 0f;
    private int fpsFrameCount = 0;
    private float fpsNextUpdateTime = 0f;
    private float currentFPS = 0f;
    
    void Awake()
    {
        // Make sure we have a text component reference
        if (timerText == null)
        {
            timerText = GetComponent<TextMeshProUGUI>();
            if (timerText == null)
            {
                timerText = GetComponentInChildren<TextMeshProUGUI>();
            }
            
            // Try regular Text component if TextMeshPro is not found
            if (timerText == null)
            {
                Text regularText = GetComponentInChildren<Text>();
                if (regularText != null)
                {
                    Debug.LogWarning("TextMeshProUGUI not found, using regular Text component instead. Consider upgrading to TextMeshPro for better text rendering.");
                }
                else
                {
                    Debug.LogError("No text component found for timer display! Please assign a TextMeshProUGUI or Text component.");
                }
            }
        }
        
        // Initialize FPS display
        if (fpsText != null)
        {
            fpsText.gameObject.SetActive(showFPS);
            fpsNextUpdateTime = Time.unscaledTime + fpsUpdateInterval;
        }
        
        // Initialize timer value based on mode
        if (!countUp)
        {
            elapsedTime = countdownStartTime; // Set initial time for countdown
        }
        // For countUp, elapsedTime is already 0f by default.
        
        // Start with initial display
        UpdateTimerDisplay();
        
        // Start the timer if configured to do so
        if (startOnAwake)
        {
            StartTimer();
        }
    }
    
    void Update()
    {
        // Update timer display
        if (isRunning)
        {
            if (countUp)
            {
                // Count up from zero
                elapsedTime += Time.deltaTime;
            }
            else
            {
                // Count down to zero (if implementing a countdown timer)
                elapsedTime -= Time.deltaTime;
                if (elapsedTime <= 0f)
                {
                    elapsedTime = 0f;
                    StopTimer();
                    // Optionally trigger an event when timer reaches zero
                }
            }
            
            UpdateTimerDisplay();
        }
        
        // Update FPS display
        if (showFPS && fpsText != null)
        {
            UpdateFPSDisplay();
        }
    }
    
    private void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            // Calculate minutes and seconds
            int minutes = Mathf.FloorToInt(elapsedTime / 60f);
            int seconds = Mathf.FloorToInt(elapsedTime % 60f);
            
            // Format as mm:ss with leading zeros
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }
    
    private void UpdateFPSDisplay()
    {
        // Increment the frame counter
        fpsFrameCount++;
        
        // Add unscaled delta time to avoid slow motion affecting the FPS calculation
        fpsAccumulator += Time.unscaledDeltaTime;
        
        // Check if it's time to update the display
        if (Time.unscaledTime > fpsNextUpdateTime)
        {
            // Calculate FPS
            currentFPS = fpsFrameCount / fpsAccumulator;
            
            // Update the text display
            if (fpsText != null)
            {
                string fpsString = currentFPS.ToString("F1"); // Display with 1 decimal place
                
                if (useColorCoding)
                {
                    // Color-code based on performance
                    string colorTag = GetFPSColorTag(currentFPS);
                    fpsText.text = $"{colorTag}{fpsString} FPS</color>";
                }
                else
                {
                    fpsText.text = $"{fpsString} FPS";
                }
            }
            
            // Reset for next update
            fpsFrameCount = 0;
            fpsAccumulator = 0f;
            fpsNextUpdateTime = Time.unscaledTime + fpsUpdateInterval;
        }
    }
    
    private string GetFPSColorTag(float fps)
    {
        // Color code the FPS value based on performance
        if (fps >= 60f)
            return "<color=#00FF00>"; // Green (excellent)
        else if (fps >= 40f)
            return "<color=#AAFF00>"; // Light green (very good)
        else if (fps >= 30f)
            return "<color=#FFFF00>"; // Yellow (good)
        else if (fps >= 20f)
            return "<color=#FF8800>"; // Orange (acceptable)
        else
            return "<color=#FF0000>"; // Red (poor)
    }
    
    // Toggle FPS display
    public void ToggleFPSDisplay(bool show)
    {
        showFPS = show;
        if (fpsText != null)
        {
            fpsText.gameObject.SetActive(show);
        }
    }
    
    // Public methods to control the timer
    
    public void StartTimer()
    {
        isRunning = true;
    }
    
    public void StopTimer()
    {
        isRunning = false;
    }
    
    public void ResetTimer()
    {
        if (countUp)
        {
            elapsedTime = 0f;
        }
        else
        {
            elapsedTime = countdownStartTime; // Reset to the initial countdown time
        }
        UpdateTimerDisplay();
    }
    
    public void PauseTimer()
    {
        isRunning = false;
    }
    
    public void ResumeTimer()
    {
        isRunning = true;
    }
    
    // Get the current elapsed time
    public float GetElapsedTime()
    {
        return elapsedTime;
    }
    
    // Set a specific time (useful for loading saved games)
    public void SetTime(float newTime)
    {
        elapsedTime = newTime;
        UpdateTimerDisplay();
    }
    
    // Get the current FPS
    public float GetCurrentFPS()
    {
        return currentFPS;
    }
} 