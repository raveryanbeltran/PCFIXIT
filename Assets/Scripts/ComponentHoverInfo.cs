using UnityEngine;
using UnityEngine.UI;

public class ComponentHoverInfo : MonoBehaviour
{
    [Header("Component Information")]
    public string componentName;
    [TextArea(3, 5)]
    public string description;
    public string type;
    public string specifications;
    
    [Header("UI Settings")]
    public GameObject infoPanelPrefab;
    public Vector2 screenOffset = new Vector2(0, 120f);
    
    private GameObject infoPanelInstance;
    private bool isHovered = false;
    private Canvas overlayCanvas;
    private float lastUpdateTime;
    private bool mouseButtonWasDown = false;

    void Start()
    {
        overlayCanvas = GameObject.FindObjectOfType<Canvas>();
    }

    void OnMouseEnter()
    {
        if (infoPanelPrefab == null || overlayCanvas == null) return;
        if (Input.GetMouseButton(0)) return; // Don't show if mouse button is down
        
        ShowInfoPanel();
    }

    void OnMouseExit()
    {
        if (Input.GetMouseButton(0)) return; // Don't hide if we're dragging
        
        HideInfoPanel();
    }

    void Update()
    {
        // Check for drag state changes
        bool mouseButtonIsDown = Input.GetMouseButton(0);
        
        // If mouse button was just pressed and we're showing panel, hide it
        if (!mouseButtonWasDown && mouseButtonIsDown && isHovered)
        {
            HideInfoPanel();
        }
        // If mouse button was just released and we're hovered, show panel
        else if (mouseButtonWasDown && !mouseButtonIsDown && IsMouseOverComponent())
        {
            Invoke("CheckMouseOverAfterDrag", 0.1f);
        }
        
        mouseButtonWasDown = mouseButtonIsDown;
        
        // Update panel position if needed
        if (isHovered && infoPanelInstance != null && !mouseButtonIsDown)
        {
            if (Time.time - lastUpdateTime >= 0.1f)
            {
                UpdateInfoPanelPosition();
                lastUpdateTime = Time.time;
            }
        }
    }

    private bool IsMouseOverComponent()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        return Physics.Raycast(ray, out hit) && hit.collider.gameObject == gameObject;
    }

    private void CheckMouseOverAfterDrag()
    {
        if (IsMouseOverComponent() && !Input.GetMouseButton(0))
        {
            ShowInfoPanel();
        }
    }

    private void ShowInfoPanel()
    {
        if (infoPanelInstance != null) return;
        if (Input.GetMouseButton(0)) return; // Safety check
        
        isHovered = true;
        infoPanelInstance = Instantiate(infoPanelPrefab, overlayCanvas.transform);
        
        ComponentInfoPanel panelScript = infoPanelInstance.GetComponent<ComponentInfoPanel>();
        if (panelScript != null)
        {
            panelScript.SetInfo(componentName, description, type, specifications);
        }
        
        UpdateInfoPanelPosition();
    }

    private void UpdateInfoPanelPosition()
    {
        if (infoPanelInstance == null || Camera.main == null) return;
        
        Vector3 screenPosition = Camera.main.WorldToScreenPoint(transform.position);
        screenPosition += (Vector3)screenOffset;
        screenPosition = KeepPanelOnScreen(screenPosition);
        infoPanelInstance.transform.position = screenPosition;
    }

    private Vector3 KeepPanelOnScreen(Vector3 screenPosition)
    {
        if (infoPanelInstance == null) return screenPosition;
        
        RectTransform panelRect = infoPanelInstance.GetComponent<RectTransform>();
        if (panelRect == null) return screenPosition;
        
        Vector2 panelSize = panelRect.sizeDelta;
        screenPosition.x = Mathf.Clamp(screenPosition.x, panelSize.x / 2f, Screen.width - panelSize.x / 2f);
        screenPosition.y = Mathf.Clamp(screenPosition.y, panelSize.y / 2f, Screen.height - panelSize.y / 2f);
        
        return screenPosition;
    }

    private void HideInfoPanel()
    {
        isHovered = false;
        
        if (infoPanelInstance != null)
        {
            ComponentInfoPanel panelScript = infoPanelInstance.GetComponent<ComponentInfoPanel>();
            if (panelScript != null)
            {
                panelScript.HidePanel();
            }
            else
            {
                Destroy(infoPanelInstance);
            }
            infoPanelInstance = null;
        }
    }

    void OnDestroy() => HideInfoPanel();
    void OnDisable() => HideInfoPanel();
}