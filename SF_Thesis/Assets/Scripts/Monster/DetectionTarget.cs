using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
public class DetectionTarget : MonoBehaviour
{
    public enum TargetType
    {
        Player,
        Monster
    }

    [Header("Detection Target")]
    public TargetType type;
}
