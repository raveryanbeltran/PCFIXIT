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

    [System.Serializable]
    public class SnapPointValidation
    {
        public string snapPointName; // Name of the snap point Transform
        public string requiredComponent; // Which component should go here
        public string errorMessage; // Message to show when wrong component
    }

    public List<ComponentTask> tasks = new List<ComponentTask>();
    public List<SnapPointValidation> snapPointValidations = new List<SnapPointValidation>();
    
    // Event to notify UI when tasks change
    public System.Action OnTasksUpdated;
    public System.Action<int> OnStarsPenalized; // New event for star penalties
    
    private int starsPenalized = 0; // Track how many stars were lost
    
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
            DontDestroyOnLoad(gameObject);
        }
    }
    
    public void CompleteTask(string componentName)
    {
        foreach (var task in tasks)
        {
            if (task.componentName == componentName && !task.isCompleted)
            {
                task.isCompleted = true;
                Debug.Log($"Task completed: {componentName}");
                OnTasksUpdated?.Invoke();
                
                // Check if all tasks are now complete
                CheckAllTasksCompleted();
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

    // NEW METHOD: Check if all tasks are completed and notify GameManager
    public void CheckAllTasksCompleted()
    {
        if (AllTasksCompleted())
        {
            Debug.Log("All tasks completed! Notifying GameManager...");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.CompleteGame();
            }
            else
            {
                Debug.LogError("GameManager instance is null!");
            }
        }
    }

    // NEW METHOD: Validate if component can be snapped to this point
    public bool ValidateSnap(Transform snapPoint, string componentName)
    {
        foreach (var validation in snapPointValidations)
        {
            if (validation.snapPointName == snapPoint.name)
            {
                if (validation.requiredComponent == componentName)
                {
                    // Correct component for this snap point
                    return true;
                }
                else
                {
                    // Wrong component - apply penalty
                    ApplyWrongComponentPenalty(validation.errorMessage, componentName, validation.requiredComponent);
                    return false;
                }
            }
        }
        
        // No validation rule for this snap point - allow it
        return true;
    }

    // NEW METHOD: Apply penalty for wrong component
    private void ApplyWrongComponentPenalty(string errorMessage, string wrongComponent, string correctComponent)
    {
        // Only penalize if we haven't reached maximum penalties (3 stars max, so 3 penalties max)
        if (starsPenalized < 3)
        {
            starsPenalized++;
            Debug.Log($"Wrong component penalty! {starsPenalized}/3 penalties. {wrongComponent} cannot go here.");
            
            // Notify GameManager about star penalty
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ApplyStarPenalty();
            }
            
            // Show error message
            // ShowWrongComponentError(errorMessage, wrongComponent, correctComponent);
            
            // Notify UI about penalty
            OnStarsPenalized?.Invoke(starsPenalized);
        }
        else
        {
            Debug.Log("Maximum penalties reached! No more stars to lose.");
        }
    }

    // NEW METHOD: Show error for wrong component
    private void ShowWrongComponentError(string errorMessage, string wrongComponent, string correctComponent)
    {
        // Create error message in world space
        GameObject errorText = new GameObject("WrongComponentError");
        errorText.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 2f;
        
        TextMesh textMesh = errorText.AddComponent<TextMesh>();
        textMesh.text = $"{errorMessage}\nYou tried: {wrongComponent}\nShould be: {correctComponent}";
        textMesh.color = Color.red;
        textMesh.characterSize = 0.1f;
        textMesh.fontSize = 30;
        textMesh.anchor = TextAnchor.MiddleCenter;

        // Make text face camera
        errorText.transform.LookAt(Camera.main.transform);
        errorText.transform.Rotate(0, 180, 0);

        // Add background
        GameObject background = GameObject.CreatePrimitive(PrimitiveType.Quad);
        background.transform.SetParent(errorText.transform);
        background.transform.localPosition = new Vector3(0, 0, 0.01f);
        background.transform.localScale = new Vector3(4f, 1.5f, 1f);
        
        Renderer bgRenderer = background.GetComponent<Renderer>();
        bgRenderer.material.color = new Color(0, 0, 0, 0.8f);

        // Destroy after 3 seconds
        Destroy(errorText, 3f);
    }

    // NEW METHOD: Get current star rating after penalties
    public int GetCurrentStarRating()
    {
        int baseStars = 3;
        return Mathf.Max(0, baseStars - starsPenalized);
    }

    // NEW METHOD: Reset penalties (for restarting level)
    public void ResetPenalties()
    {
        starsPenalized = 0;
        OnStarsPenalized?.Invoke(0);
    }
}