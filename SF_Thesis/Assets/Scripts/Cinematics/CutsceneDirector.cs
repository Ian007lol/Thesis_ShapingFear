using UnityEngine;
using Unity.Cinemachine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
public class CutsceneDirector : MonoBehaviour
{
    public static CutsceneDirector Instance { get; private set; }

    [Header("Cinemachine (Cutscene Cam)")]
    [SerializeField] private CinemachineCamera cineCam;   // CineCam_01
    [SerializeField] private int cutscenePriority = 50;

    [Header("UI")]
    [SerializeField] private LetterboxUI letterbox;

    [Header("Player Control")]
    [SerializeField] private PlayerControlGroup playerControlGroup; // preferred over scriptsToDisable

    private bool _inCutscene;
    public bool IsInCutscene => _inCutscene;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Fix B: ensure cutscene cam cannot take over unless we enable it
        if (cineCam != null)
        {
            cineCam.Priority = cutscenePriority;
            cineCam.enabled = false;     // <- KEY LINE
            cineCam.Follow = null;
            cineCam.LookAt = null;
        }

        if (letterbox != null)
            letterbox.SetVisible(false);
    }

    public void Enter(Transform followLookAtTarget, bool showLetterbox, bool lockPlayer)
    {
        _inCutscene = true;

        if (lockPlayer && playerControlGroup != null)
            playerControlGroup.SetEnabled(false);

        if (letterbox != null)
            letterbox.SetVisible(showLetterbox);

        if (cineCam != null)
        {
            cineCam.Follow = followLookAtTarget;
            cineCam.LookAt = followLookAtTarget;

            cineCam.enabled = true; // <- KEY LINE (now Cinemachine can use it)
        }
    }

    public void SetTarget(Transform followLookAtTarget)
    {
        if (cineCam != null && cineCam.enabled)
        {
            cineCam.Follow = followLookAtTarget;
            cineCam.LookAt = followLookAtTarget;
        }
    }

    public void Exit(bool hideLetterbox, bool unlockPlayer)
    {
        _inCutscene = false;

        if (cineCam != null)
        {
            cineCam.Follow = null;
            cineCam.LookAt = null;
            cineCam.enabled = false;  // <- KEY LINE (prevents hijack forever)
        }

        if (letterbox != null && hideLetterbox)
            letterbox.SetVisible(false);

        if (unlockPlayer && playerControlGroup != null)
            playerControlGroup.SetEnabled(true);
    }
}
