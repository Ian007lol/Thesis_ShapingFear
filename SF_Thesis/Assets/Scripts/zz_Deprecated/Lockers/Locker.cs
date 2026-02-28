using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
public class Locker : MonoBehaviour
{
    [Header("Setup")]
    public Transform seatPoint;              // where the player sits/stands inside
    public Animator animator;                // Animator with a bool "IsOpen"
    public float openTime = 0.6f;            // seconds your open anim takes
    public float closeTime = 0.6f;           // seconds your close anim takes
    public Collider doorBlocker;             // (optional) collider that blocks entry when closed

    [Header("UX")]
    public float interactRange = 2.0f;       // how close the player must be (raycast checks this)

    [Header("Audio/FX Hooks (optional)")]
    public AudioSource sfxOpen;
    public AudioSource sfxClose;

    bool isOpen;
    public bool IsOpen => isOpen;
    public bool IsOccupied { get; private set; }

    void Reset()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
    }

    public bool CanUse(Transform user)
    {
        if (IsOccupied) return false;
        float d = Vector3.Distance(user.position, transform.position);
        return d <= interactRange;
    }

    public void SetOpen(bool open)
    {
        isOpen = open;
        if (animator) animator.SetBool("IsOpen", isOpen);
        if (doorBlocker) doorBlocker.enabled = !isOpen;

        if (open && sfxOpen) sfxOpen.Play();
        if (!open && sfxClose) sfxClose.Play();
    }

    public void MarkOccupied(bool occ) => IsOccupied = occ;
}
