using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
public class PlayerControlGroup : MonoBehaviour
{
    [Tooltip("Drag all scripts that should be disabled during cinematics/checkpoint death/etc.")]
    [SerializeField] private MonoBehaviour[] scriptsToToggle;

    public void SetEnabled(bool enabledState)
    {
        if (scriptsToToggle == null) return;

        foreach (var s in scriptsToToggle)
        {
            if (!s) continue;
            s.enabled = enabledState;
        }
    }
}
