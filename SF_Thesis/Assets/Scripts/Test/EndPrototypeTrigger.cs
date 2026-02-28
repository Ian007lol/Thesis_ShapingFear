using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
public class EndPrototypeTrigger : MonoBehaviour
{
    [SerializeField] private GameObject endScreenPanel;
    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

        if (other.CompareTag("Player"))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            triggered = true;
            endScreenPanel.SetActive(true);

            // Optional: stop player movement / time
            Time.timeScale = 0f;
        }
    }
}
