using UnityEngine;
using UnityEngine.InputSystem;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
public class PlayerInteractor : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference interactAction; // e.g. E

    [Header("Raycast")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private float interactRadius = 0.25f;
    [SerializeField] private LayerMask interactableMask = ~0;

    private void Awake()
    {
        if (!playerCamera) playerCamera = Camera.main;
    }

    private void OnEnable()
    {
        if (interactAction)
        {
            interactAction.action.performed += OnInteract;
            interactAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (interactAction)
        {
            interactAction.action.performed -= OnInteract;
            interactAction.action.Disable();
        }
    }

    private void OnInteract(InputAction.CallbackContext ctx)
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (!Physics.SphereCast(ray, interactRadius, out RaycastHit hit, interactDistance,
                interactableMask, QueryTriggerInteraction.Collide))
            return;

        var interactable = hit.collider.GetComponentInParent<IInteractable>();
        if (interactable == null) return;

        if (interactable.CanInteract())
            interactable.Interact();
    }
}
