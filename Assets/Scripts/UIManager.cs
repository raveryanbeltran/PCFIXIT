using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject taskPanel;
    public GameObject taskItemPrefab;
    public Text completionText;
    public ScrollRect scrollRect; // Add reference to scroll rect if using one

    [Header("Game UI")]
    public Text timerText;
    // public Text completionText; // REMOVE THIS DUPLICATE - already declared above

    [Header("Completion Screen")]
    public GameObject completionScreen;
    public Text completionTimeText;
    public Text bestTimeText;
    public Text starsText;
    public GameObject[] starIcons; // Assign 3 star objects in inspector

    [Header("Navigation Buttons")]
    public Button restartButton;
    public Button menuButton;
    public Button nextLevelButton;

    private TaskManager taskManager;
    private Dictionary<string, TaskUIItem> taskUIItems = new Dictionary<string, TaskUIItem>();

    void Start()
    {
        // Set up button listeners FIRST
        SetupButtonListeners();
        
        // Hide completion screen at start
        if (completionScreen != null)
            completionScreen.SetActive(false);
        
        // Initialize task system
        taskManager = TaskManager.Instance;
        if (taskManager != null)
        {
            taskManager.OnTasksUpdated += UpdateUI;
            InitializeUI();
        }
        else
        {
            Debug.LogWarning("TaskManager instance not found!");
        }
    }

    private void SetupButtonListeners()
    {
        // Set up button listeners for completion screen
        if (restartButton != null)
            restartButton.onClick.AddListener(() => GameManager.Instance?.RestartLevel());
        else
            Debug.LogWarning("Restart button not assigned in UIManager");
        
        if (menuButton != null)
            menuButton.onClick.AddListener(() => GameManager.Instance?.GoToMenu());
        else
            Debug.LogWarning("Menu button not assigned in UIManager");
        
        if (nextLevelButton != null)
            nextLevelButton.onClick.AddListener(() => GameManager.Instance?.LoadNextLevel());
        else
            Debug.LogWarning("Next level button not assigned in UIManager");
    }

    void Update()
    {
        UpdateTimerUI();
        CheckForCompletion();
    }

    private void UpdateTimerUI()
    {
        if (timerText != null && GameManager.Instance != null && GameManager.Instance.isGameActive)
        {
            float currentTime = GameManager.Instance.GetCurrentTime();
            timerText.text = FormatTime(currentTime);
        }
    }

    private void CheckForCompletion()
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameCompleted && completionScreen != null && !completionScreen.activeInHierarchy)
        {
            ShowCompletionScreen(GameManager.Instance.GetCurrentTime());
        }
    }

    void InitializeUI()
    {
        if (taskPanel == null)
        {
            Debug.LogError("TaskPanel reference is null in UIManager!");
            return;
        }

        // Clear existing items
        foreach (Transform child in taskPanel.transform)
        {
            Destroy(child.gameObject);
        }
        taskUIItems.Clear();

        // Create UI items for each task
        foreach (var task in taskManager.tasks)
        {
            if (taskItemPrefab == null)
            {
                Debug.LogError("TaskItemPrefab reference is null in UIManager!");
                continue;
            }

            GameObject taskUI = Instantiate(taskItemPrefab, taskPanel.transform);
            TaskUIItem taskUIItem = taskUI.GetComponent<TaskUIItem>();
            
            if (taskUIItem != null)
            {
                taskUIItem.SetTask(task.displayName, task.isCompleted);
                taskUIItems.Add(task.componentName, taskUIItem);
                
                // Force content size fitter to update
                ContentSizeFitter fitter = taskUI.GetComponent<ContentSizeFitter>();
                if (fitter != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(taskUI.transform as RectTransform);
                }
            }
        }
        
        UpdateCompletionText();
        RefreshLayout();
    }

    void UpdateUI()
    {
        foreach (var task in taskManager.tasks)
        {
            if (taskUIItems.ContainsKey(task.componentName))
            {
                taskUIItems[task.componentName].SetTask(task.displayName, task.isCompleted);
            }
        }
        
        UpdateCompletionText();
        RefreshLayout();
        
        // Check if all tasks are completed
        if (taskManager.AllTasksCompleted() && GameManager.Instance != null && !GameManager.Instance.isGameCompleted)
        {
            GameManager.Instance.CompleteGame();
        }
    }
    
    void RefreshLayout()
    {
        Canvas.ForceUpdateCanvases();
        
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
        }
        
        if (taskPanel != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(taskPanel.transform as RectTransform);
        }
        
        foreach (var contentFitter in taskPanel.GetComponentsInChildren<ContentSizeFitter>())
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentFitter.transform as RectTransform);
        }
    }
    
    void UpdateCompletionText()
    {
        if (completionText != null && taskManager != null)
        {
            int completed = 0;
            foreach (var task in taskManager.tasks)
            {
                if (task.isCompleted) completed++;
            }
            
            completionText.text = $"Tasks Completed: {completed}/{taskManager.tasks.Count}";
            
            if (taskManager.AllTasksCompleted())
            {
                completionText.color = Color.green;
                completionText.text += " - All Tasks Complete!";
            }
            else
            {
                completionText.color = Color.white;
            }
        }
    }

    public void ShowCompletionScreen(float completionTime)
    {
        if (completionScreen == null)
        {
            Debug.LogError("Completion screen reference is null!");
            return;
        }
        
        completionScreen.SetActive(true);
        
        // Display completion time
        if (completionTimeText != null)
            completionTimeText.text = $"Completion Time: {FormatTime(completionTime)}";
        
        // Get level data for best time
        string levelName = GameManager.Instance.currentLevelName;
        GameManager.LevelData levelData = GameManager.Instance.GetLevelData(levelName);
        
        if (bestTimeText != null)
            bestTimeText.text = $"Best Time: {FormatTime(levelData.bestTime)}";
        
        if (starsText != null)
            starsText.text = $"Rating: {levelData.starsEarned}/3 Stars";
        
        // Show star rating
        UpdateStarDisplay(levelData.starsEarned);
        
        // Disable game UI
        if (taskPanel != null) taskPanel.SetActive(false);
        if (timerText != null) timerText.gameObject.SetActive(false);
        if (completionText != null) completionText.gameObject.SetActive(false);
        
        Debug.Log("Completion screen shown");
    }

    private void UpdateStarDisplay(int stars)
    {
        if (starIcons == null || starIcons.Length == 0)
        {
            Debug.LogWarning("Star icons array not set up in UIManager");
            return;
        }
        
        for (int i = 0; i < starIcons.Length; i++)
        {
            if (starIcons[i] != null)
                starIcons[i].SetActive(i < stars);
        }
    }

    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        int milliseconds = Mathf.FloorToInt((timeInSeconds * 100f) % 100f);
        return $"{minutes:00}:{seconds:00}.{milliseconds:00}";
    }
}