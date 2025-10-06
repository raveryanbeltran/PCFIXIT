using UnityEngine;

public class PrefReset : MonoBehaviour
{
    void Update()
    {
        // Press F1 to debug current state
        if (Input.GetKeyDown(KeyCode.F1) && GameManager.Instance != null)
        {
            GameManager.Instance.DebugCurrentState();
        }
        
        // Press F2 to clear saved data (for testing)
        if (Input.GetKeyDown(KeyCode.F2) && GameManager.Instance != null)
        {
            GameManager.Instance.ClearAllSavedData();
        }
    }
}
