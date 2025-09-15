using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComponentDrag : MonoBehaviour
{
    private TaskManager taskManager;
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

    [Header("Snap Offset (per object)")]
    public Vector3 snapOffset = Vector3.zero; // Per-object pivot offset

    [Header("Gizmo Settings")]
    public Vector3 gizmoCubeSize = new Vector3(0.2f, 0.2f, 0.2f); // Editable in Inspector

    // Track if this component has been snapped (to prevent multiple completions)
    private bool hasBeenSnapped = false;

    // Static dictionary to track occupied snap points across all instances
    private static Dictionary<Transform, ComponentDrag> occupiedSnapPoints = new Dictionary<Transform, ComponentDrag>();

    void Start()
    {
    originalPosition = transform.position;
    }

    private IEnumerator OnMouseDown()
    {
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
            OccupySnapPoint(nearestSnapPoint);

            // Move pivot so that the offset point aligns with the snap point
            transform.position = nearestSnapPoint.position - (transform.rotation * snapOffset);
            
            // Notify TaskManager that this component has been snapped
            if (!hasBeenSnapped && TaskManager.Instance != null)
            {
                TaskManager.Instance.CompleteTask(componentName);
                hasBeenSnapped = true;
            }
        }

        if (nearestSnapPoint != null && TaskManager.Instance != null)
         {
            TaskManager.Instance.CompleteTask(componentName);
        }
    }

    private void OccupySnapPoint(Transform snapPoint)
    {
        if (currentSnapPoint != null)
        {
            ReleaseSnapPoint();
        }

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
        if (TaskManager.Instance != null)
        {
            TaskManager.Instance.ResetTask(componentName);
        }

        currentSnapPoint = null;
        
        // Reset the snapped status if the component is removed
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
    }
}