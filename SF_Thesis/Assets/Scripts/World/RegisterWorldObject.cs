using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
public class RegisterWorldObject : MonoBehaviour
{
    [Tooltip("Unique ID used by StepActions, e.g. door.A-12, pc.security, light.hallway")]
    public string id;

    private void OnEnable()
    {
        WorldRegistry.Register(id, gameObject);
    }

    private void OnDisable()
    {
        WorldRegistry.Unregister(id, gameObject);
    }
}
