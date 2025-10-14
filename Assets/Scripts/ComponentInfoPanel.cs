using UnityEngine;
using TMPro; // <-- TextMeshPro namespace

[RequireComponent(typeof(CanvasGroup))]
public class ComponentInfoPanel : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI typeText;
    public TextMeshProUGUI specsText;

    [Header("Animation")]
    public float fadeInTime = 0.3f;
    
    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        // Start invisible
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    void Start()
    {
        // Fade in
        canvasGroup.LeanAlpha(1f, fadeInTime);
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    public void SetInfo(string compName, string compDescription, string compType, string compSpecs)
    {
        if (nameText != null)
            nameText.text = compName;
        
        if (descriptionText != null)
            descriptionText.text = compDescription;
        
        if (typeText != null)
            typeText.text = $"Type: {compType}";
        
        if (specsText != null)
            specsText.text = $"Specs: {compSpecs}";
    }

    public void HidePanel()
    {
        if (canvasGroup != null)
        {
            canvasGroup.LeanAlpha(0f, 0.2f)
                .setOnComplete(() => Destroy(gameObject));
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
