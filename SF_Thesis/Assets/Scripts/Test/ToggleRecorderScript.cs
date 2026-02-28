using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
public class ToggleRecorderScript : MonoBehaviour
{
    public VoskSpeechToText voskSpeechToText;
    public InputActionReference toggle;
    private Coroutine _stopRoutine;


    private void OnEnable()
    {
        if (toggle != null)
        {
            toggle.action.Enable();
            toggle.action.started += OnPressed;
            toggle.action.canceled += OnReleased;

        }
    }

    private void OnDisable()
    {
        if (toggle != null)
        {
            toggle.action.started -= OnPressed;
            toggle.action.canceled -= OnReleased;
            toggle.action.Disable();
        }
    }


    private void OnPressed(InputAction.CallbackContext context)
    {
        Debug.Log("🎙 Push-to-talk START");
        voskSpeechToText.StartRecording();
    }

    private void OnReleased(InputAction.CallbackContext context)
{
    Debug.Log("✋ Push-to-talk END");

    if (_stopRoutine != null) StopCoroutine(_stopRoutine);
    _stopRoutine = StartCoroutine(StopAfterTail());
}

private IEnumerator StopAfterTail()
{
    yield return new WaitForSeconds(0.12f); // 120ms tail; try 0.08–0.20
    voskSpeechToText.StopRecordingAndProcess();
    _stopRoutine = null;
}
}