using UnityEngine;
using TMPro;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
public class ObjectiveUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI objectiveText;
    [SerializeField] private string defaultText = "";

    private void Awake()
    {
        if (objectiveText == null)
            objectiveText = GetComponent<TextMeshProUGUI>();

        if (objectiveText != null)
            objectiveText.text = defaultText;
    }

    private void OnEnable()
    {
        GameEvents.OnEvent += OnGameEvent;
    }

    private void OnDisable()
    {
        GameEvents.OnEvent -= OnGameEvent;
    }

    private void OnGameEvent(GameEvents.EventData e)
    {
        if (objectiveText == null) return;

        // MissionManager raises this with payload = MissionStepDefinition
        if (e.key == "mission.step.started")
        {
            var step = e.payload as MissionStepDefinition;
            if (step != null)
                objectiveText.text = step.objectiveText;
        }
        else if (e.key == "mission.completed")
        {
            objectiveText.text = defaultText; // or "Objective complete"
        }
    }
}
