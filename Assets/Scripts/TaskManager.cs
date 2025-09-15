using System.Collections.Generic;
using UnityEngine;

public class TaskManager : MonoBehaviour
{
    // Singleton instance
    public static TaskManager Instance { get; private set; }
    
    [System.Serializable]
    public class ComponentTask
    {
        public string componentName;
        public string displayName;
        public bool isCompleted = false;
    }

    public List<ComponentTask> tasks = new List<ComponentTask>();
    
    // Event to notify UI when tasks change
    public System.Action OnTasksUpdated;
    
    private void Awake()
    {
        // Ensure only one instance exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: keep between scenes
        }
    }
    
    public void CompleteTask(string componentName)
    {
        foreach (var task in tasks)
        {
            if (task.componentName == componentName && !task.isCompleted)
            {
                task.isCompleted = true;
                OnTasksUpdated?.Invoke();
                return;
            }
        }
    }
    
    public void ResetTask(string componentName)
    {
        foreach (var task in tasks)
        {
            if (task.componentName == componentName && task.isCompleted)
            {
                task.isCompleted = false;
                OnTasksUpdated?.Invoke();
                return;
            }
        }
    }
    
    public bool AllTasksCompleted()
    {
        foreach (var task in tasks)
        {
            if (!task.isCompleted) return false;
        }
        return true;
    }
}