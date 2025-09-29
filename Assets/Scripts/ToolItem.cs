using System.Collections;
using UnityEngine;

public class ToolItem : MonoBehaviour
{
    [Header("Tool Settings")]
    public ToolManager.ToolType toolType;
    public string toolName;
    
    [Header("Tool Position Settings")]
    public Vector3 equippedOffset = new Vector3(0.3f, -0.2f, 0.5f); // Position relative to camera
    public Vector3 equippedRotation = new Vector3(0f, 0f, 0f); // Rotation when equipped
    
    [Header("Visual Feedback")]
    public Material highlightMaterial;
    public GameObject equippedVisual;
    public float hoverScaleAmount = 1.2f;
    
    private Material originalMaterial;
    private Renderer toolRenderer;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 originalScale;
    private bool isEquipped = false;
    private bool isHovered = false;
    
    void Start()
    {
        toolRenderer = GetComponent<Renderer>();
        if (toolRenderer != null)
        {
            originalMaterial = toolRenderer.material;
        }
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        originalScale = transform.localScale;
        
        if (equippedVisual != null)
        {
            equippedVisual.SetActive(false);
        }
    }
    
    void Update()
    {
        // Only check for input if we're hovering over this tool
        if (isHovered && Input.GetKeyDown(KeyCode.E))
        {
            if (!isEquipped)
            {
                EquipTool();
            }
            // Unequip is handled by ToolManager with Q key or pressing E on the equipped tool
        }
        
        // If equipped, make the tool follow the camera
        if (isEquipped)
        {
            FollowCamera();
        }
    }
    
    void OnMouseEnter()
    {
        isHovered = true;
        if (!isEquipped)
        {
            ShowHoverEffect();
        }
        else
        {
            // Show unequip prompt when hovering over equipped tool
            ShowEquipPrompt();
        }
    }
    
    void OnMouseExit()
    {
        isHovered = false;
        if (!isEquipped)
        {
            HideHoverEffect();
        }
        else
        {
            // Hide unequip prompt when not hovering
            HideEquipPrompt();
        }
    }
    
    private void FollowCamera()
    {
        if (Camera.main == null) return;
        
        // Get camera transform
        Transform cameraTransform = Camera.main.transform;
        
        // Calculate position based on camera position and offset
        Vector3 targetPosition = cameraTransform.position + 
                               cameraTransform.right * equippedOffset.x +
                               cameraTransform.up * equippedOffset.y +
                               cameraTransform.forward * equippedOffset.z;
        
        // Calculate rotation based on camera rotation plus additional rotation
        Quaternion targetRotation = cameraTransform.rotation * Quaternion.Euler(equippedRotation);
        
        // Apply position and rotation directly (no lerping to avoid lag)
        transform.position = targetPosition;
        transform.rotation = targetRotation;
    }
    
    private void ShowHoverEffect()
    {
        if (toolRenderer != null && highlightMaterial != null)
        {
            toolRenderer.material = highlightMaterial;
        }
        
        transform.localScale = originalScale * hoverScaleAmount;
        ShowEquipPrompt();
    }
    
    private void HideHoverEffect()
    {
        if (toolRenderer != null && originalMaterial != null)
        {
            toolRenderer.material = originalMaterial;
        }
        
        transform.localScale = originalScale;
        HideEquipPrompt();
    }
    
    private void ShowEquipPrompt()
    {
        HideEquipPrompt(); // Clean up any existing prompt first
        
        GameObject prompt = new GameObject("EquipPrompt");
        TextMesh textMesh = prompt.AddComponent<TextMesh>();
        textMesh.text = isEquipped ? "Press E to Unequip" : "Press E to Equip";
        textMesh.color = isEquipped ? Color.yellow : Color.green;
        textMesh.characterSize = 0.05f;
        textMesh.fontSize = 30;
        textMesh.anchor = TextAnchor.MiddleCenter;
        
        prompt.transform.position = transform.position + Vector3.up * 0.3f;
        prompt.transform.SetParent(transform);
    }
    
    private void HideEquipPrompt()
    {
        Transform existingPrompt = transform.Find("EquipPrompt");
        if (existingPrompt != null)
        {
            Destroy(existingPrompt.gameObject);
        }
    }
    
    public void EquipTool()
    {
        if (isEquipped) return;
        
        Debug.Log($"EQUIPPING: {toolName}");
        isEquipped = true;
        isHovered = false; // Reset hover state
        
        // Notify ToolManager
        if (ToolManager.Instance != null)
        {
            ToolManager.Instance.EquipTool(toolType, this);
        }
        
        // Visual feedback
        if (equippedVisual != null) equippedVisual.SetActive(true);
        HideHoverEffect();
        
        // Store original rotation
        originalRotation = transform.rotation;
        
        Debug.Log($"Tool equipped and following camera");
    }
    
    public void UnequipTool()
    {
        if (!isEquipped) return;
        
        Debug.Log($"UNEQUIPPING: {toolName}");
        isEquipped = false;
        
        // Notify ToolManager
        if (ToolManager.Instance != null)
        {
            ToolManager.Instance.UnequipTool();
        }
        
        // Visual feedback
        if (equippedVisual != null) equippedVisual.SetActive(false);
        
        // Return to workbench
        StartCoroutine(ReturnToWorkbench());
    }
    
    private IEnumerator ReturnToWorkbench()
    {
        float duration = 0.5f;
        float elapsed = 0f;
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.position = Vector3.Lerp(startPosition, originalPosition, t);
            transform.rotation = Quaternion.Lerp(startRotation, originalRotation, t);
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, t);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.position = originalPosition;
        transform.rotation = originalRotation;
        transform.localScale = originalScale;
        
        Debug.Log($"{toolName} returned to workbench");
    }
}