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

    private TaskManager taskManager;
    private Dictionary<string, TaskUIItem> taskUIItems = new Dictionary<string, TaskUIItem>();

    void Start()
    {
        taskManager = TaskManager.Instance;
        if (taskManager != null)
        {
            taskManager.OnTasksUpdated += UpdateUI;
            InitializeUI();
        }
    }

    void InitializeUI()
    {
        // Clear existing items
        foreach (Transform child in taskPanel.transform)
        {
            Destroy(child.gameObject);
        }
        taskUIItems.Clear();

        // Create UI items for each task
        foreach (var task in taskManager.tasks)
        {
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
        RefreshLayout(); // Refresh the entire layout
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
        RefreshLayout(); // Refresh after updates
    }
    
    void RefreshLayout()
    {
        // Refresh all layouts to handle text changes
        Canvas.ForceUpdateCanvases();
        
        // Refresh scroll view if exists
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f; // Scroll to top
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
        }
        
        // Refresh task panel layout
        LayoutRebuilder.ForceRebuildLayoutImmediate(taskPanel.transform as RectTransform);
        
        // Refresh content size fitters
        foreach (var contentFitter in taskPanel.GetComponentsInChildren<ContentSizeFitter>())
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentFitter.transform as RectTransform);
        }
    }
    
    void UpdateCompletionText()
    {
        if (completionText != null)
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
}