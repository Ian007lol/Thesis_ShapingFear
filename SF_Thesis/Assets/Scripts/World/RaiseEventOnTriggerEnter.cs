using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
public class RaiseEventOnTriggerEnter : MonoBehaviour
{
    public string eventKey = "trigger.entered.test";

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameEvents.Raise(eventKey, gameObject);
            Debug.Log($"[Event] {eventKey} fired by {name}");
        }
    }
}
