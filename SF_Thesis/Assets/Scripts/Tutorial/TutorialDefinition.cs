using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
[CreateAssetMenu(menuName = "Tutorial/Tutorial Definition")]
public class TutorialDefinition : ScriptableObject
{
    [Header("ID")]
    public string tutorialId = "grab.throw";

    [Header("Content")]
    [TextArea(3, 10)]
    public string text = "Press E to pick up objects.\nHold LMB to charge throw.\nRMB cancels.";

    [Header("Behavior")]
    public bool pauseTime = true;
    public bool oneShot = true;
    [Header("Continue timing")]
    public float allowContinueAfterSeconds = 0.6f;  // input locked until this passes
    public float showContinueHintAfterSeconds = 0.8f; // hint appears after this passes

}
