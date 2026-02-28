using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
public class LetterboxUI : MonoBehaviour
{
    [SerializeField] private GameObject barsRoot;

    private void Awake()
    {
        if (barsRoot) barsRoot.SetActive(false);
    }

    public void SetVisible(bool value)
    {
        if (barsRoot) barsRoot.SetActive(value);
    }
}
