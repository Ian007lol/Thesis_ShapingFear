using UnityEngine;
using UnityEngine.InputSystem;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
public class MapToggle : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("Canvas (or root GameObject) that contains the map UI.")]
    [SerializeField] private GameObject mapRoot;

    [Header("Input")]
    [Tooltip("Input action for opening the map (e.g. Player/Map).")]
    [SerializeField] private InputActionReference mapAction;

    [Header("Behaviour")]
    [Tooltip("If true, holding the button keeps the map open; releasing closes it. If false, it's a toggle.")]
    [SerializeField] private bool holdToOpen = false;

    private bool isOpen;

    private void Awake()
    {
        if (mapRoot != null)
            mapRoot.SetActive(false);
    }

    private void OnEnable()
    {
        if (mapAction != null)
        {
            if (holdToOpen)
            {
                mapAction.action.started  += OnMapStarted;
                mapAction.action.canceled += OnMapCanceled;
            }
            else
            {
                mapAction.action.performed += OnMapPerformedToggle;
            }

            mapAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (mapAction != null)
        {
            if (holdToOpen)
            {
                mapAction.action.started  -= OnMapStarted;
                mapAction.action.canceled -= OnMapCanceled;
            }
            else
            {
                mapAction.action.performed -= OnMapPerformedToggle;
            }

            mapAction.action.Disable();
        }
    }

    // --- Toggle mode: one press = open/close ---
    private void OnMapPerformedToggle(InputAction.CallbackContext ctx)
    {
        isOpen = !isOpen;
        if (mapRoot != null)
            mapRoot.SetActive(isOpen);
    }

    // --- Hold-to-open mode ---
    private void OnMapStarted(InputAction.CallbackContext ctx)
    {
        isOpen = true;
        if (mapRoot != null)
            mapRoot.SetActive(true);
    }

    private void OnMapCanceled(InputAction.CallbackContext ctx)
    {
        isOpen = false;
        if (mapRoot != null)
            mapRoot.SetActive(false);
    }
}
