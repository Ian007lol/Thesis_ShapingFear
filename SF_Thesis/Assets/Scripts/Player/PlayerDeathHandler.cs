using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
public class PlayerDeathHandler : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject youDiedUI; // assign in inspector (Canvas/panel root)
    [SerializeField] private float secondsBeforeChoices = 5f;

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Optional: disable player control")]
    [SerializeField] private MonoBehaviour[] scriptsToDisable;

    private bool dead;
    public bool IsDead => dead;

    public void Kill()
    {
        if (dead) return;
        dead = true;

        // disable movement / input
        if (scriptsToDisable != null)
        {
            foreach (var s in scriptsToDisable)
                if (s) s.enabled = false;
        }

        // show UI
        if (youDiedUI)
        {
            youDiedUI.SetActive(true);

            var menu = youDiedUI.GetComponentInChildren<DeathCheckpointMenu>(true);
            if (menu != null) menu.PrepareForDeathScreen();
        }

        // unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // If you ever freeze time on death, keep this realtime wait:
        // Time.timeScale = 0f;

        StartCoroutine(ShowChoicesAfterDelay());
    }

    private IEnumerator ShowChoicesAfterDelay()
    {
        yield return new WaitForSecondsRealtime(secondsBeforeChoices);

        // After 5 seconds, show checkpoint choices
        if (youDiedUI != null)
        {
            var menu = youDiedUI.GetComponentInChildren<DeathCheckpointMenu>(true);
            if (menu != null)
            {
                menu.ShowCheckpointChoices();
                yield break;
            }
        }

        // Fallback if menu not found
        Debug.LogWarning("[Death] No DeathCheckpointMenu found on youDiedUI. Loading main menu.");
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // Optional helper if you want to reset UI when a checkpoint loads
    public void ClearDeathUIAndReenableControls()
    {
        dead = false;

        if (youDiedUI) youDiedUI.SetActive(false);

        if (scriptsToDisable != null)
        {
            foreach (var s in scriptsToDisable)
                if (s) s.enabled = true;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Time.timeScale = 1f;
    }
}
