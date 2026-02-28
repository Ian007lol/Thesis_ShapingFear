using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
public class DeathCheckpointMenu : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject choicesRoot;          
    [SerializeField] private Transform listParent;            
    [SerializeField] private Button checkpointButtonPrefab;   

    [Header("Optional Buttons")]
    [SerializeField] private Button loadNewestButton;         
    [SerializeField] private Button quitButton;               

    private void Awake()
    {
        HideChoices(); // initial state
    }

    private void OnEnable()
    {
        // This runs every time the death UI GameObject is re-enabled.
        HideChoices();
    }

    /// <summary>
    /// Call this when the player just died / death screen is shown.
    /// </summary>
    public void PrepareForDeathScreen()
    {
        HideChoices();
        ClearList();
    }

    /// <summary>
    /// Call this after the death screen has been visible for 5 seconds.
    /// </summary>
    public void ShowCheckpointChoices()
    {
        if (choicesRoot) choicesRoot.SetActive(true);
        BuildCheckpointButtonsChronological();
    }

    private void HideChoices()
    {
        if (choicesRoot) choicesRoot.SetActive(false);
    }

    private void ClearList()
    {
        if (listParent == null) return;

        for (int i = listParent.childCount - 1; i >= 0; i--)
            Destroy(listParent.GetChild(i).gameObject);

        if (loadNewestButton != null)
            loadNewestButton.gameObject.SetActive(false);
    }

    private void BuildCheckpointButtonsChronological()
    {
        if (listParent == null || checkpointButtonPrefab == null)
        {
            Debug.LogWarning("[DeathMenu] Missing listParent or checkpointButtonPrefab.");
            return;
        }

        ClearList();

        var cm = CheckpointManager.Instance;
        if (cm == null)
        {
            Debug.LogWarning("[DeathMenu] No CheckpointManager.Instance.");
            return;
        }

        List<CheckpointManager.CheckpointInfo> cps = cm.GetCheckpointListChronological(); // oldest -> newest
        if (cps.Count == 0)
        {
            Debug.LogWarning("[DeathMenu] No checkpoints in index.");
            return;
        }

        if (loadNewestButton != null)
        {
            var newest = cps[cps.Count - 1];
            loadNewestButton.onClick.RemoveAllListeners();
            loadNewestButton.onClick.AddListener(() => cm.LoadCheckpointByFileName(newest.fileName));
            loadNewestButton.gameObject.SetActive(true);
        }

        int displayIndex = 1;

        for (int i = cps.Count - 1; i >= 0; i--)
        {
            var info = cps[i];
            var b = Instantiate(checkpointButtonPrefab, listParent);

            var label = b.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = $"{displayIndex}. {info.checkpointId} ({info.sceneName})";

            string file = info.fileName;
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(() => cm.LoadCheckpointByFileName(file));

            displayIndex++;
        }
    }
}
