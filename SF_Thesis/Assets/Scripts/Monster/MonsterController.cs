using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
public class MonsterController : MonoBehaviour
{
    [SerializeField] private MonsterScript ai;

    private void Awake()
    {
        if (ai == null) ai = GetComponent<MonsterScript>();
    }

    public void SetPaused(bool paused) => ai?.SetPaused(paused);
    public void ForceInvestigate(Vector3 point) => ai?.ForceInvestigate(point);

    public void TeleportTo(Transform marker, bool matchRotation = true)
    {
        if (ai == null || marker == null) return;
        ai.TeleportTo(marker.position, marker.rotation, matchRotation);
    }
}
