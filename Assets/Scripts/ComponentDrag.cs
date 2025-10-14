using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComponentDrag : MonoBehaviour
{
    private Vector3 offset;
    private Vector3 screenPoint;
    private Vector3 originalPosition;
    private bool isDragging = false;
    private Transform currentSnapPoint = null;

    [Header("Component Info")]
    public string componentName;

    [Header("Snap Settings")]
    public bool enableSnapping = true;
    public float snapDistance = 0.5f;
    public Transform[] snapPoints;

    [Header("Rotation Settings")]
    public bool lockRotation = true;
    
    [Header("Snap Behavior")]
    public bool useCustomSnapPosition = false;
    public Vector3 customSnapPosition = Vector3.zero;
    public bool useCustomSnapRotation = false;
    public Vector3 customSnapRotation = Vector3.zero;

    [Header("Snap Offset (per object)")]
    public Vector3 snapOffset = Vector3.zero;

    [Header("Gizmo Settings")]
    public Vector3 gizmoCubeSize = new Vector3(0.2f, 0.2f, 0.2f);

    [Header("Pre-Snapped Settings")]
    public bool startPreSnapped = false;
    public Transform preSnappedPoint;

    [Header("Tool Requirements")]
    public bool requiresTool = false;

    // Track if this component has been snapped
    private bool hasBeenSnapped = false;

    // Static dictionary to track occupied snap points
    private static Dictionary<Transform, ComponentDrag> occupiedSnapPoints = new Dictionary<Transform, ComponentDrag>();

    void Start()
    {
        originalPosition = transform.position;

        // Handle pre-snapped initialization
    if (startPreSnapped && preSnappedPoint != null)
    {
        InitializePreSnapped();
    }
    }

    private void InitializePreSnapped()
    {
    if (preSnappedPoint == null)
    {
        Debug.LogWarning($"Pre-snapped point not assigned for {componentName}", this);
        return;
    }

    // Check if the assigned snap point is available
    if (occupiedSnapPoints.ContainsKey(preSnappedPoint) && occupiedSnapPoints[preSnappedPoint] != null)
    {
        Debug.LogWarning($"Pre-snapped point {preSnappedPoint.name} is already occupied for {componentName}", this);
        return;
    }

    // Occupy the snap point and position the component
    OccupySnapPoint(preSnappedPoint);
    
    Vector3 finalPosition = CalculateSnapPosition(preSnappedPoint);
    Quaternion finalRotation = CalculateSnapRotation();
    
    transform.SetPositionAndRotation(finalPosition, finalRotation);
    
    // Notify TaskManager if needed
    if (TaskManager.Instance != null)
    {
        TaskManager.Instance.CompleteTask(componentName);
        hasBeenSnapped = true;
    }
    
    Debug.Log($"{componentName} started pre-snapped to {preSnappedPoint.name}");
    }

    private IEnumerator OnMouseDown()
    {
        // Check tool requirements before allowing drag
        if (requiresTool && !CheckToolRequirements())
        {
            yield break; // Exit if tool requirements not met
        }

        isDragging = true;

        if (currentSnapPoint != null)
        {
            ReleaseSnapPoint();
        }

        screenPoint = Camera.main.WorldToScreenPoint(transform.position);
        offset = transform.position - Camera.main.ScreenToWorldPoint(
            new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z));

        while (Input.GetMouseButton(0))
        {
            Vector3 curScreenPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
            Vector3 curPosition = Camera.main.ScreenToWorldPoint(curScreenPoint) + offset;

            if (lockRotation)
            {
                transform.SetPositionAndRotation(curPosition, Quaternion.identity);
            }
            else
            {
                transform.position = curPosition;
            }

            yield return new WaitForFixedUpdate();
        }

        isDragging = false;

        if (enableSnapping)
        {
            TrySnapToPosition();
        }
    }

    private void TrySnapToPosition()
    {
        Transform nearestSnapPoint = null;
        float minDistance = float.MaxValue;

        foreach (Transform snapPoint in snapPoints)
        {
            if (occupiedSnapPoints.ContainsKey(snapPoint) && occupiedSnapPoints[snapPoint] != this)
                continue;

            float distance = Vector3.Distance(transform.position, snapPoint.position);
            if (distance < snapDistance && distance < minDistance)
            {
                minDistance = distance;
                nearestSnapPoint = snapPoint;
            }
        }

        if (nearestSnapPoint != null)
        {
            // NEW: Validate if this component can be snapped here
            if (TaskManager.Instance != null && !TaskManager.Instance.ValidateSnap(nearestSnapPoint, componentName))
            {
                // Wrong component - don't snap and show error
                StartCoroutine(FlashComponentRed());
                return;
            }

            // Final tool check before snapping
            if (requiresTool && !CheckToolRequirements())
            {
                return; // Cancel snap if tool requirements not met
            }

            OccupySnapPoint(nearestSnapPoint);

            Vector3 finalPosition = CalculateSnapPosition(nearestSnapPoint);
            Quaternion finalRotation = CalculateSnapRotation();
            
            transform.SetPositionAndRotation(finalPosition, finalRotation);
            
            if (!hasBeenSnapped && TaskManager.Instance != null)
            {
                TaskManager.Instance.CompleteTask(componentName);
                hasBeenSnapped = true;
            }
        }
    }

    // TOOL MANAGEMENT INTEGRATION
    private bool CheckToolRequirements()
    {
        if (ToolManager.Instance == null)
        {
            Debug.LogWarning("ToolManager not found in scene!");
            return true; // Allow without tools if manager missing
        }

        bool canInstall = ToolManager.Instance.CanInstallComponent(componentName);
        
        if (!canInstall)
        {
            string errorMessage = ToolManager.Instance.GetRequiredToolMessage(componentName);
            ToolManager.ToolType requiredTool = ToolManager.Instance.GetRequiredToolType(componentName); // Fixed: Added ToolManager. prefix
            ShowToolError(errorMessage, requiredTool);
        }

        return canInstall;
    }

    private void ShowToolError(string message, ToolManager.ToolType requiredTool) // Fixed: Added ToolManager. prefix
    {
        Debug.LogWarning($"Tool Error: {message}");
        
        // Enhanced error feedback
        StartCoroutine(ShowEnhancedErrorText(message, requiredTool));
        
        // Visual feedback on the component
        StartCoroutine(FlashComponentRed());
    }

    private IEnumerator ShowEnhancedErrorText(string message, ToolManager.ToolType requiredTool) // Fixed: Added ToolManager. prefix
    {
        // Create world space UI text
        GameObject errorText = new GameObject("ErrorText");
        errorText.transform.position = transform.position + Vector3.up * 0.8f;
        
        TextMesh textMesh = errorText.AddComponent<TextMesh>();
        textMesh.text = $"{message}\nRequired: {requiredTool}";
        textMesh.color = Color.red;
        textMesh.characterSize = 0.08f;
        textMesh.fontSize = 40;
        textMesh.anchor = TextAnchor.MiddleCenter;

        // Make text face camera
        errorText.transform.LookAt(Camera.main.transform);
        errorText.transform.Rotate(0, 180, 0);

        // Add background panel
        GameObject background = GameObject.CreatePrimitive(PrimitiveType.Quad);
        background.transform.SetParent(errorText.transform);
        background.transform.localPosition = new Vector3(0, 0, 0.01f);
        background.transform.localScale = new Vector3(2f, 0.8f, 1f);
        
        Renderer bgRenderer = background.GetComponent<Renderer>();
        bgRenderer.material.color = new Color(0, 0, 0, 0.8f);

        Destroy(errorText, 4f);
        yield return null;
    }

    private IEnumerator FlashComponentRed()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            Color originalColor = renderer.material.color;
            float flashDuration = 0.5f;
            float elapsed = 0f;
            
            while (elapsed < flashDuration)
            {
                float t = Mathf.PingPong(elapsed * 4f, 1f);
                renderer.material.color = Color.Lerp(originalColor, Color.red, t);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            renderer.material.color = originalColor;
        }
    }

    private Vector3 CalculateSnapPosition(Transform snapPoint)
    {
        if (useCustomSnapPosition)
        {
            return customSnapPosition;
        }
        else
        {
            return snapPoint.position - (transform.rotation * snapOffset);
        }
    }

    private Quaternion CalculateSnapRotation()
    {
        if (useCustomSnapRotation)
        {
            return Quaternion.Euler(customSnapRotation);
        }
        else if (lockRotation)
        {
            return Quaternion.identity;
        }
        else
        {
            return transform.rotation;
        }
    }

    private void OccupySnapPoint(Transform snapPoint)
    {
        if (currentSnapPoint != null)
        {
            ReleaseSnapPoint();
        }

        // Update the static dictionary to track occupancy :cite[3]
        if (occupiedSnapPoints.ContainsKey(snapPoint))
        {
            occupiedSnapPoints[snapPoint] = this;
        }
        else
        {
            occupiedSnapPoints.Add(snapPoint, this);
        }

        currentSnapPoint = snapPoint;
    }
    private void ReleaseSnapPoint()
    {
        if (currentSnapPoint != null && occupiedSnapPoints.ContainsKey(currentSnapPoint))
        {
            if (occupiedSnapPoints[currentSnapPoint] == this)
            {
                occupiedSnapPoints.Remove(currentSnapPoint);
            }
        }
        
        if (TaskManager.Instance != null && hasBeenSnapped)
        {
            TaskManager.Instance.ResetTask(componentName);
        }

        currentSnapPoint = null;
        hasBeenSnapped = false;
    }

    void OnDestroy()
    {
        ReleaseSnapPoint();
    }

    void OnDisable()
    {
        ReleaseSnapPoint();
    }

    void OnMouseEnter()
    {
        // Highlight object
    }

    void OnMouseExit()
    {
        if (!isDragging)
        {
            // Restore appearance
        }
    }

    // Draw gizmos so you can see the offset in the Scene view
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        // Calculate where the object will snap to (with offset)
        Vector3 snapPointPos = transform.position + (transform.rotation * snapOffset);

        // Draw a wireframe cube at that position
        Gizmos.DrawWireCube(snapPointPos, gizmoCubeSize);

        // Optional: Draw a line from the object's pivot to the snap offset
        Gizmos.DrawLine(transform.position, snapPointPos);
        
        // Draw custom snap position if enabled
        if (useCustomSnapPosition)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(customSnapPosition, 0.1f);
            Gizmos.DrawLine(transform.position, customSnapPosition);
        }
        
        // Draw snap distance spheres around all snap points
        DrawSnapDistanceGizmos();
    }

    // New method to draw snap distance visualization
    private void DrawSnapDistanceGizmos()
    {
        if (snapPoints == null || snapPoints.Length == 0)
            return;

        foreach (Transform snapPoint in snapPoints)
        {
            if (snapPoint == null)
                continue;

            // Check if this snap point is occupied
            bool isOccupied = occupiedSnapPoints.ContainsKey(snapPoint) && occupiedSnapPoints[snapPoint] != null;

            // Set color based on occupancy
            Gizmos.color = isOccupied ? Color.red : Color.green;

            // Draw wire sphere showing snap distance
            Gizmos.DrawWireSphere(snapPoint.position, snapDistance);

            // Draw a smaller solid sphere at the snap point position
            Gizmos.color = isOccupied ? Color.red : Color.blue;
            Gizmos.DrawSphere(snapPoint.position, 0.05f);

            // Draw line from snap point to show orientation (if needed)
            Gizmos.color = Color.white;
            Gizmos.DrawLine(snapPoint.position, snapPoint.position + snapPoint.forward * 0.2f);

            // Label the snap point with its name
            #if UNITY_EDITOR
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.Label(snapPoint.position + Vector3.up * 0.1f, snapPoint.name);
            #endif
        }
    }
}