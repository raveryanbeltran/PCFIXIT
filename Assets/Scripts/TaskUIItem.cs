using UnityEngine;
using UnityEngine.UI;

public class TaskUIItem : MonoBehaviour
{
    public Text taskText;
    public Image checkmark;
    
    public void SetTask(string text, bool completed)
    {
        taskText.text = text;
        checkmark.gameObject.SetActive(completed);
    }
}