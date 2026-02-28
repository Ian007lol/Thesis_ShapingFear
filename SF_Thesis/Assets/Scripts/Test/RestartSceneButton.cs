using UnityEngine;
using UnityEngine.SceneManagement;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
public class RestartSceneButton : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.KeypadMultiply))
        {
            RestartScene();
            Debug.Log("TEST");
        }
    }

    public void RestartScene()
    {   
        Application.Quit();
        Time.timeScale = 1f; // safety, in case it was paused
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
