using UnityEngine;
using TMPro;
using System.Collections;

public class VoskResultText : MonoBehaviour 
{
    public VoskSpeechToText VoskSpeechToText;
    public TextMeshProUGUI ResultText;

    [Tooltip("Seconds before the text clears")]
    public float ClearDelay = 3f;

    private Coroutine _clearCoroutine;

    void Awake()
    {
        VoskSpeechToText.OnTranscriptionResult += OnTranscriptionResult;
    }

    private void OnDestroy()
    {
        VoskSpeechToText.OnTranscriptionResult -= OnTranscriptionResult;
    }

    private void OnTranscriptionResult(string obj)
    {
        var result = new RecognitionResult(obj);

        // Build final text cleanly
        ResultText.text = result.Phrases.Length > 0
            ? result.Phrases[0].Text
            : "";

        // Restart clear timer
        if (_clearCoroutine != null)
            StopCoroutine(_clearCoroutine);

        _clearCoroutine = StartCoroutine(ClearAfterDelay());
    }

    private IEnumerator ClearAfterDelay()
    {
        yield return new WaitForSeconds(ClearDelay);
        ResultText.text = "";
        _clearCoroutine = null;
    }
}
