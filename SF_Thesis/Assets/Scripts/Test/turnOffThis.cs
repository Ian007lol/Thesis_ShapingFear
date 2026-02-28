using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
[RequireComponent(typeof(CanvasGroup))]
public class turnOffThis : MonoBehaviour
{
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        canvasGroup.alpha = 1f;
    }

    private void OnDisable()
    {
        // Optional but highly recommended safety reset
        canvasGroup.alpha = 0f;
    }
}
