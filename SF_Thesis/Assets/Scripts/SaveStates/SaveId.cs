using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
[DisallowMultipleComponent]
public class SaveId : MonoBehaviour
{
    [SerializeField] private string id;

    public string Id => id;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Generate once, then keep it stable.
        if (string.IsNullOrWhiteSpace(id))
        {
            id = System.Guid.NewGuid().ToString("N");
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif
}
