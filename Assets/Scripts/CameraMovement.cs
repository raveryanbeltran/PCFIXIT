using System.Collections;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float fastMoveSpeed = 20f;
    public float rotationSpeed = 3f;
    public float zoomSpeed = 10f;
    
    [Header("Movement Limits")]
    public bool enableBounds = false;
    public Vector3 boundsMin = new Vector3(-10f, 1f, -10f);
    public Vector3 boundsMax = new Vector3(10f, 10f, 10f);
    
    [Header("Camera Clipping Settings")]
    public float minNearClipPlane = 0.01f; // Very small near clipping distance
    public float maxNearClipPlane = 0.1f;  // Default near clipping distance
    public float zoomSensitivity = 0.1f;   // How much zoom affects clipping
    
    private float currentMoveSpeed;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Camera cam;
    
    void Start()
    {
        currentMoveSpeed = moveSpeed;
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        cam = GetComponent<Camera>();
        
        if (cam == null)
        {
            cam = Camera.main;
            Debug.LogWarning("CameraMovement: No Camera component found. Using Camera.main.");
        }
        
        // Set initial near clip plane
        if (cam != null)
        {
            cam.nearClipPlane = maxNearClipPlane;
        }
    }
    
    void Update()
    {
        HandleCameraMovement();
        HandleCameraRotation();
        HandleCameraZoom();
        HandleSpeedModifier();
        HandleResetCamera();
        
        if (enableBounds)
        {
            ConstrainCameraToBounds();
        }
    }
    
    private void HandleCameraMovement()
    {
        Vector3 moveDirection = Vector3.zero;
        
        // Forward/Backward
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            moveDirection += transform.forward;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            moveDirection -= transform.forward;
        
        // Left/Right
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            moveDirection -= transform.right;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            moveDirection += transform.right;
        
        // Up/Down (World space)
        if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.PageUp))
            moveDirection += Vector3.up;
        if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.PageDown))
            moveDirection += Vector3.down;
        
        // Normalize and apply movement
        if (moveDirection != Vector3.zero)
        {
            moveDirection.Normalize();
            transform.position += moveDirection * currentMoveSpeed * Time.deltaTime;
        }
    }
    
    private void HandleCameraRotation()
    {
        // Only rotate when right mouse button is held
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
            float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed;
            
            // Get current rotation
            Vector3 rotation = transform.eulerAngles;
            
            // Apply rotation (invert Y for more intuitive control)
            rotation.x -= mouseY;
            rotation.y += mouseX;
            
            // Clamp vertical rotation to avoid flipping
            rotation.x = ClampAngle(rotation.x, -80f, 80f);
            
            // Apply the new rotation
            transform.rotation = Quaternion.Euler(rotation);
        }
    }
    
    private void HandleCameraZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            // Move forward/backward based on scroll input
            transform.Translate(0, 0, scroll * zoomSpeed, Space.Self);
            
            // Adjust near clipping plane based on zoom level
            AdjustNearClipPlane(scroll);
        }
    }
    
    private void AdjustNearClipPlane(float zoomDirection)
    {
        if (cam == null) return;
        
        // Calculate dynamic near clip plane based on camera's forward movement
        // When zooming in (negative scroll), reduce near clip plane
        // When zooming out (positive scroll), increase near clip plane
        
        float targetNearClip = cam.nearClipPlane;
        
        if (zoomDirection < 0) // Zooming in
        {
            targetNearClip = Mathf.Max(minNearClipPlane, cam.nearClipPlane * 0.8f);
        }
        else // Zooming out
        {
            targetNearClip = Mathf.Min(maxNearClipPlane, cam.nearClipPlane * 1.2f);
        }
        
        cam.nearClipPlane = Mathf.Lerp(cam.nearClipPlane, targetNearClip, Time.deltaTime * 5f);
    }
    
    private void HandleSpeedModifier()
    {
        // Hold Shift for faster movement
        if (Input.GetKey(KeyCode.LeftShift))
        {
            currentMoveSpeed = fastMoveSpeed;
        }
        else
        {
            currentMoveSpeed = moveSpeed;
        }
    }
    
    private void HandleResetCamera()
    {
        // Reset camera to initial position with R key
        if (Input.GetKeyDown(KeyCode.R))
        {
            transform.position = initialPosition;
            transform.rotation = initialRotation;
            
            // Reset near clip plane as well
            if (cam != null)
            {
                cam.nearClipPlane = maxNearClipPlane;
            }
        }
    }
    
    private void ConstrainCameraToBounds()
    {
        Vector3 position = transform.position;
        
        position.x = Mathf.Clamp(position.x, boundsMin.x, boundsMax.x);
        position.y = Mathf.Clamp(position.y, boundsMin.y, boundsMax.y);
        position.z = Mathf.Clamp(position.z, boundsMin.z, boundsMax.z);
        
        transform.position = position;
    }
    
    private float ClampAngle(float angle, float min, float max)
    {
        if (angle < 0f) angle = 360 + angle;
        if (angle > 180f) return Mathf.Max(angle, 360 + min);
        return Mathf.Min(angle, max);
    }
    
    // Public method to focus on a specific position (for future use)
    public void FocusOnPosition(Vector3 position, float distance = 2f)
    {
        StartCoroutine(MoveToPosition(position, distance));
    }
    
    private IEnumerator MoveToPosition(Vector3 targetPosition, float distance)
    {
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        
        // Calculate target camera position
        Vector3 direction = (transform.position - targetPosition).normalized;
        Vector3 targetCamPosition = targetPosition + direction * distance;
        
        // Calculate target rotation to look at the position
        Quaternion targetRotation = Quaternion.LookRotation(targetPosition - targetCamPosition);
        
        float duration = 1f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(startPosition, targetCamPosition, elapsed / duration);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, elapsed / duration);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.position = targetCamPosition;
        transform.rotation = targetRotation;
        
        // Reset near clip plane after focusing
        if (cam != null)
        {
            cam.nearClipPlane = maxNearClipPlane;
        }
    }
}