using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolManager : MonoBehaviour
{
    public static ToolManager Instance { get; private set; }
    
    public enum ToolType 
    { 
        None,
        Screwdriver, 
        AntiStaticStrap, 
        ThermalPaste, 
        CableTies,
        Pliers,
        Multimeter
    }
    
    [Header("Current Tool")]
    public ToolType currentTool = ToolType.None;
    public ToolItem currentToolItem;
    
    [Header("Tool Requirements")]
    public ToolRequirement[] toolRequirements;
    
    [System.Serializable]
    public class ToolRequirement
    {
        public string componentName;
        public ToolType requiredTool;
        public string errorMessage;
    }
    
    [Header("UI References")]
    public UnityEngine.UI.Image currentToolIcon;
    public Sprite[] toolIcons;
    public UnityEngine.UI.Text currentToolText;
    public GameObject toolUIPanel;
    
    // Events for tool changes
    public System.Action<ToolType> OnToolEquipped;
    public System.Action OnToolUnequipped;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }
    
    public void EquipTool(ToolType tool, ToolItem toolItem)
    {
        // Unequip current tool first
        if (currentToolItem != null && currentToolItem != toolItem)
        {
            currentToolItem.UnequipTool();
        }
        
        currentTool = tool;
        currentToolItem = toolItem;
        
        UpdateToolUI();
        OnToolEquipped?.Invoke(tool);
        
        Debug.Log($"Tool equipped: {tool}");
    }
    
    public void UnequipTool()
    {
        if (currentToolItem != null)
        {
            currentToolItem = null;
        }
        
        currentTool = ToolType.None;
        UpdateToolUI();
        OnToolUnequipped?.Invoke();
        
        Debug.Log("Tool unequipped");
    }
    
    public bool CanInstallComponent(string componentName)
    {
        ToolRequirement requirement = System.Array.Find(toolRequirements, 
            r => r.componentName == componentName);
            
        if (requirement == null)
        {
            return true;
        }
        
        return currentTool == requirement.requiredTool;
    }
    
    public string GetRequiredToolMessage(string componentName)
    {
        ToolRequirement requirement = System.Array.Find(toolRequirements, 
            r => r.componentName == componentName);
            
        return requirement?.errorMessage ?? "Tool required for installation.";
    }
    
    public ToolType GetRequiredToolType(string componentName)
    {
        ToolRequirement requirement = System.Array.Find(toolRequirements, 
            r => r.componentName == componentName);
            
        return requirement?.requiredTool ?? ToolType.None;
    }
    
    private void UpdateToolUI()
    {
        // Update tool icon
        if (currentToolIcon != null && toolIcons != null)
        {
            int toolIndex = (int)currentTool;
            if (toolIndex >= 0 && toolIndex < toolIcons.Length)
            {
                currentToolIcon.sprite = toolIcons[toolIndex];
                currentToolIcon.color = currentTool == ToolType.None ? 
                    new Color(1, 1, 1, 0.3f) : Color.white;
            }
        }
        
        // Update tool text
        if (currentToolText != null)
        {
            currentToolText.text = currentTool == ToolType.None ? 
                "No Tool Equipped" : $"Equipped: {currentTool}";
            currentToolText.color = currentTool == ToolType.None ? 
                Color.gray : Color.white;
        }
        
        // Show/hide UI panel
        if (toolUIPanel != null)
        {
            toolUIPanel.SetActive(currentTool != ToolType.None);
        }
    }
    
    void Update()
    {
        // Global unequip with Q key
        if (Input.GetKeyDown(KeyCode.Q) && currentTool != ToolManager.ToolType.None)
        {
            if (currentToolItem != null)
            {
                currentToolItem.UnequipTool();
            }
            else
            {
                UnequipTool();
            }
        }
    }
}