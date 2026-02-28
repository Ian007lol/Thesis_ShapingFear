using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
public class AudioRecorderInteractable : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float interactDistance = 2.5f;

    [Header("Recording")]
    [SerializeField] private PlayRecording recordingAction;

    [Header("Behaviour")]
    [SerializeField] private bool playOnlyOnce = true;
    [SerializeField] private bool disableAfterPlay = true;

    private bool _hasPlayed;
    private Camera _playerCamera;

    private void Awake()
    {
        _playerCamera = Camera.main;
    }

    private void Update()
    {
        if (_hasPlayed && playOnlyOnce) return;
        if (!Input.GetKeyDown(interactKey)) return;
        if (!IsPlayerLookingAtThis()) return;

        Play();
    }

    private bool IsPlayerLookingAtThis()
    {
        if (_playerCamera == null) return false;

        Ray ray = new Ray(_playerCamera.transform.position, _playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
        {
            return hit.collider != null && hit.collider.gameObject == gameObject;
        }

        return false;
    }

    private void Play()
    {
        if (recordingAction == null) return;

        recordingAction.Execute(null);
        _hasPlayed = true;

        if (disableAfterPlay)
        {
            // Hide & disable the recorder completely
            gameObject.SetActive(false);
        }
    }
}
