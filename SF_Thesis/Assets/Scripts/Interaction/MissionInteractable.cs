using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
public class MissionInteractable : MonoBehaviour, IInteractable
{
    [Header("Mission Event")]
    [Tooltip("Unique id for this interaction, e.g. pc.security, button.power, terminal.labA")]
    [SerializeField] private string interactionId = "pc.security";

    [Tooltip("Event key prefix. Final event will be: interact.<interactionId>")]
    [SerializeField] private string eventPrefix = "interact";

    [Header("Behavior")]
    [SerializeField] private bool oneShot = true;
    [SerializeField] private AudioSource useSfx; // optional
    [SerializeField] private bool disableAfterUse = false; // optional (disable collider/obj)

    private bool used;

    public bool CanInteract()
    {
        return !oneShot || !used;
    }

    public void Interact()
    {
        if (!CanInteract()) return;

        used = true;

        if (useSfx) useSfx.Play();

        // Raise mission event
        // Example: interact.pc.security
        GameEvents.Raise($"{eventPrefix}.{interactionId}", gameObject);

        if (disableAfterUse)
            gameObject.SetActive(false);
    }
}
