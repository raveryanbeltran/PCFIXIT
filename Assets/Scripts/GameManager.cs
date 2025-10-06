using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Game State")]
    public bool isGameActive = false;
    public bool isGameCompleted = false;
    private float startTime;
    private float completionTime;
    
    [Header("Level Management")]
    public string currentLevelName = "RAM_Installation_Level";
    public string menuSceneName = "MenuScene";
    
    [Header("Score Data")]
    public LevelData currentLevelData;
    
    [System.Serializable]
    public class LevelData
    {
        public string levelName;
        public float bestTime;
        public bool isCompleted;
        public int starsEarned;
    }
    
    // Store all level data
    private Dictionary<string, LevelData> levelProgress = new Dictionary<string, LevelData>();
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        LoadGameData(); // Load saved data when GameManager starts
        Debug.Log("GameManager initialized");
    }
    
    void Start()
    {
        StartGame();
    }
    
    public void StartGame()
    {
        if (isGameActive) return;
        
        isGameActive = true;
        isGameCompleted = false;
        startTime = Time.time;
        completionTime = 0f;
        
        Debug.Log($"Game started: {currentLevelName}");
    }
    
    public void CompleteGame()
    {
        if (!isGameActive || isGameCompleted) return;
        
        isGameCompleted = true;
        completionTime = Time.time - startTime;
        
        Debug.Log($"Game completion triggered! Time: {completionTime:F2}s");
        
        // Save level progress
        SaveLevelProgress();
        
        // Show completion UI
        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            uiManager.ShowCompletionScreen(completionTime);
        }
        else
        {
            Debug.LogError("UIManager not found in scene!");
        }
    }
    
    private void SaveLevelProgress()
    {
        // Get or create level data
        if (!levelProgress.ContainsKey(currentLevelName))
        {
            levelProgress[currentLevelName] = new LevelData { levelName = currentLevelName };
        }
        
        LevelData data = levelProgress[currentLevelName];
        data.isCompleted = true;
        
        // FIXED: Only update best time if current time is better (lower)
        if (data.bestTime == 0 || completionTime < data.bestTime)
        {
            data.bestTime = completionTime;
            Debug.Log($"New best time set: {completionTime:F2}s for {currentLevelName}");
        }
        else
        {
            Debug.Log($"Current time: {completionTime:F2}s, Best time remains: {data.bestTime:F2}s");
        }
        
        // Calculate stars based on current completion time (not best time)
        data.starsEarned = CalculateStars(completionTime);
        
        // Save to PlayerPrefs
        SaveGameData();
        
        Debug.Log($"Level progress saved: {currentLevelName}, Current Time: {completionTime:F2}s, Best Time: {data.bestTime:F2}s, Stars: {data.starsEarned}");
    }
    
    private int CalculateStars(float time)
    {
        // Customize these thresholds based on your level difficulty
        if (time <= 30f) return 3;    // Gold
        else if (time <= 60f) return 2; // Silver
        else return 1;                 // Bronze
    }
    
    public float GetCurrentTime()
    {
        if (!isGameActive) return 0f;
        if (isGameCompleted) return completionTime;
        return Time.time - startTime;
    }
    
    public LevelData GetLevelData(string levelName)
    {
        if (levelProgress.ContainsKey(levelName))
            return levelProgress[levelName];
        
        // If level doesn't exist in dictionary, try to load from PlayerPrefs
        return LoadLevelDataFromPlayerPrefs(levelName);
    }
    
    // NEW METHOD: Load level data from PlayerPrefs
    private LevelData LoadLevelDataFromPlayerPrefs(string levelName)
    {
        LevelData data = new LevelData { levelName = levelName };
        
        string key = $"Level_{levelName}";
        
        if (PlayerPrefs.HasKey($"{key}_BestTime"))
        {
            data.bestTime = PlayerPrefs.GetFloat($"{key}_BestTime");
            data.isCompleted = PlayerPrefs.GetInt($"{key}_IsCompleted", 0) == 1;
            data.starsEarned = PlayerPrefs.GetInt($"{key}_Stars", 0);
        }
        
        return data;
    }
    
    // UI Navigation Methods
    public void RestartLevel()
    {
        Debug.Log("Restarting level...");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    public void GoToMenu()
    {
        Debug.Log("Going to menu...");
        SceneManager.LoadScene(menuSceneName);
    }
    
    public void LoadNextLevel()
    {
        Debug.Log("Loading next level...");
        GoToMenu();
    }
    
    // Save/Load system using PlayerPrefs
    private void SaveGameData()
    {
        foreach (var kvp in levelProgress)
        {
            string key = $"Level_{kvp.Key}";
            PlayerPrefs.SetFloat($"{key}_BestTime", kvp.Value.bestTime);
            PlayerPrefs.SetInt($"{key}_IsCompleted", kvp.Value.isCompleted ? 1 : 0);
            PlayerPrefs.SetInt($"{key}_Stars", kvp.Value.starsEarned);
            Debug.Log($"Saved {key}: BestTime={kvp.Value.bestTime}, IsCompleted={kvp.Value.isCompleted}, Stars={kvp.Value.starsEarned}");
        }
        PlayerPrefs.Save();
        Debug.Log("Game data saved to PlayerPrefs");
    }
    
    private void LoadGameData()
    {
        levelProgress.Clear();
        
        // You would typically load all your levels here
        // For now, we'll just initialize with current level
        string key = $"Level_{currentLevelName}";
        
        if (PlayerPrefs.HasKey($"{key}_BestTime"))
        {
            LevelData data = new LevelData
            {
                levelName = currentLevelName,
                bestTime = PlayerPrefs.GetFloat($"{key}_BestTime"),
                isCompleted = PlayerPrefs.GetInt($"{key}_IsCompleted", 0) == 1,
                starsEarned = PlayerPrefs.GetInt($"{key}_Stars", 0)
            };
            
            levelProgress[currentLevelName] = data;
            Debug.Log($"Loaded level data: {currentLevelName}, BestTime: {data.bestTime:F2}s");
        }
        else
        {
            Debug.Log($"No saved data found for {currentLevelName}, starting fresh");
        }
    }
    
    // NEW METHOD: Clear all saved data (useful for testing)
    public void ClearAllSavedData()
    {
        PlayerPrefs.DeleteAll();
        levelProgress.Clear();
        Debug.Log("All saved data cleared!");
    }
    
    // NEW METHOD: Debug current state
    public void DebugCurrentState()
    {
        Debug.Log($"Current Level: {currentLevelName}");
        Debug.Log($"Game Active: {isGameActive}, Completed: {isGameCompleted}");
        Debug.Log($"Current Time: {GetCurrentTime():F2}s");
        
        LevelData data = GetLevelData(currentLevelName);
        Debug.Log($"Best Time: {data.bestTime:F2}s, Stars: {data.starsEarned}, Completed: {data.isCompleted}");
    }
}