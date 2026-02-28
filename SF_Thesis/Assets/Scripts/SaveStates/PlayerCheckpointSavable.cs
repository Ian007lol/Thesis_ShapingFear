using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
[RequireComponent(typeof(SaveId))]
public class PlayerCheckpointSavable : MonoBehaviour, ICheckpointSavable
{
    [System.Serializable]
    public struct PlayerSaveState
    {
        public float px, py, pz;
        public float yaw;
        public float pitch;
    }

    private SaveId _id;
    private CharacterController _cc;

    [Header("Look")]
    [SerializeField] private MouseLook mouseLook;

    public string SaveKey => $"Player:{_id.Id}";

    private void Awake()
    {
        _id = GetComponent<SaveId>();
        _cc = GetComponent<CharacterController>();

        if (mouseLook == null)
            mouseLook = GetComponentInChildren<MouseLook>(true);
    }

    private void OnEnable()  => CheckpointSavableRegistry.Register(this);
    private void OnDisable() => CheckpointSavableRegistry.Unregister(this);

    public string CaptureJson()
    {
        var p = transform.position;

        float yaw = 0f, pitch = 0f;

        // Best source of truth: MouseLook internal state
        if (mouseLook != null)
            mouseLook.GetLookAngles(out yaw, out pitch);
        else
        {
            // Fallback: derive from transforms
            yaw = transform.eulerAngles.y;

            float rawPitch = Camera.main ? Camera.main.transform.localEulerAngles.x : 0f;
            if (rawPitch > 180f) rawPitch -= 360f;
            pitch = rawPitch;
        }

        var st = new PlayerSaveState
        {
            px = p.x, py = p.y, pz = p.z,
            yaw = yaw,
            pitch = pitch
        };

        return JsonUtility.ToJson(st);
    }

    public void RestoreFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return;

        var st = JsonUtility.FromJson<PlayerSaveState>(json);

        // Prevent controllers from fighting during teleport
        if (_cc != null) _cc.enabled = false;
        if (mouseLook != null) mouseLook.enabled = false;

        transform.position = new Vector3(st.px, st.py, st.pz);

        // Restore view direction the checkpoint had
        if (mouseLook != null)
        {
            mouseLook.enabled = true;
            mouseLook.SetLookAngles(st.yaw, st.pitch);
        }
        else
        {
            transform.rotation = Quaternion.Euler(0f, st.yaw, 0f);
        }

        if (_cc != null) _cc.enabled = true;
    }
}
