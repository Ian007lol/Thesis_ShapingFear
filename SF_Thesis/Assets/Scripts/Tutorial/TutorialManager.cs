using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private CanvasGroup root;   // whole overlay
    [SerializeField] private TMP_Text bodyText;

    [Header("Definitions")]
    [SerializeField] private TutorialDefinition[] tutorials;
    [SerializeField] private GameObject continueHint; // the TMP object or any GO

    [Header("Player control locks")]
    [SerializeField] private PlayerControlGroup playerControls;

    private bool canContinue;
    private Coroutine gatingRoutine;

    private readonly Dictionary<string, TutorialDefinition> map = new();
    private readonly HashSet<string> shown = new();

    private bool isShowing;
    private float prevTimeScale;

    // Optional: if MouseLook is part of the control group, we can sync it to prevent camera jump.
    private MouseLook cachedMouseLook;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (playerControls == null)
            playerControls = FindFirstObjectByType<PlayerControlGroup>();

        // Cache MouseLook if present anywhere (optional, only used for SyncFromTransforms on re-enable).
        cachedMouseLook = FindFirstObjectByType<MouseLook>();

        map.Clear();
        foreach (var t in tutorials)
        {
            if (t == null || string.IsNullOrWhiteSpace(t.tutorialId)) continue;
            map[t.tutorialId.Trim()] = t;
        }

        HideInstant();
    }

    private void OnEnable()
    {
        GameEvents.OnEvent += OnGameEvent;
    }

    private void OnDisable()
    {
        GameEvents.OnEvent -= OnGameEvent;
    }

    private void Update()
    {
        if (!isShowing) return;
        if (!canContinue) return;

        if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
        {
            Hide();
        }
    }

    private void OnGameEvent(GameEvents.EventData e)
    {
        // Expected: tutorial.show.<id>
        if (e.key == null) return;
        const string prefix = "tutorial.show.";
        if (!e.key.StartsWith(prefix)) return;

        string id = e.key.Substring(prefix.Length);
        Show(id);
    }

    public void Show(string tutorialId)
    {
        if (string.IsNullOrWhiteSpace(tutorialId)) return;
        tutorialId = tutorialId.Trim();

        if (!map.TryGetValue(tutorialId, out var def) || def == null)
        {
            Debug.LogWarning($"[TutorialManager] No tutorial definition for id '{tutorialId}'");
            return;
        }

        if (def.oneShot && shown.Contains(tutorialId))
            return;

        shown.Add(tutorialId);

        if (bodyText) bodyText.text = def.text;

        canContinue = false;
        if (continueHint) continueHint.SetActive(false);

        if (gatingRoutine != null) StopCoroutine(gatingRoutine);
        gatingRoutine = StartCoroutine(ContinueGatingRoutine(def));

        if (root)
        {
            root.alpha = 1f;
            root.interactable = true;
            root.blocksRaycasts = true;
        }

        isShowing = true;

        // Disable player controls during tutorial (single point of truth)
        if (playerControls != null)
            playerControls.SetEnabled(false);

        if (def.pauseTime)
        {
            prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }
    }

    public void Hide()
    {
        if (!isShowing) return;

        if (gatingRoutine != null)
        {
            StopCoroutine(gatingRoutine);
            gatingRoutine = null;
        }
        canContinue = false;

        isShowing = false;

        // restore time
        if (Time.timeScale == 0f)
            Time.timeScale = prevTimeScale <= 0f ? 1f : prevTimeScale;

        // Re-enable player controls
        if (playerControls != null)
            playerControls.SetEnabled(true);

        // Prevent camera jump if MouseLook exists (and is enabled again)
        if (cachedMouseLook != null && cachedMouseLook.enabled)
            cachedMouseLook.SyncFromTransforms();

        HideInstant();
    }

    private void HideInstant()
    {
        if (!root) return;

        root.alpha = 0f;
        root.interactable = false;
        root.blocksRaycasts = false;
    }

    private IEnumerator ContinueGatingRoutine(TutorialDefinition def)
    {
        float showHintAt = Mathf.Max(0f, def.showContinueHintAfterSeconds);
        float allowAt = Mathf.Max(0f, def.allowContinueAfterSeconds);

        float t = 0f;
        bool hintShown = false;

        while (t < Mathf.Max(showHintAt, allowAt))
        {
            t += Time.unscaledDeltaTime;

            if (!hintShown && continueHint != null && t >= showHintAt)
            {
                continueHint.SetActive(true);
                hintShown = true;
            }

            yield return null;
        }

        canContinue = true;

        // If hint should only appear when continuing becomes possible
        if (continueHint != null && !hintShown)
            continueHint.SetActive(true);
    }
}
