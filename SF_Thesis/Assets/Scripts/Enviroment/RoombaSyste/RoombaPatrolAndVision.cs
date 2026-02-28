using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
[RequireComponent(typeof(NavMeshAgent))]
public class RoombaPatrolAndVision : MonoBehaviour
{
    // 📌 Global list so the map overlay can iterate all Roombas
    public static readonly List<RoombaPatrolAndVision> AllRoombas = new List<RoombaPatrolAndVision>();

    [Header("Patrol")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float waitAtPoint = 1.0f;

    [Header("Vision")]
    [Tooltip("How far the Roomba can see humanoids.")]
    [SerializeField] private float visionRange = 8f;
    [Tooltip("Field of view angle in degrees (centered on forward).")]
    [SerializeField] private float visionAngle = 60f;
    [Tooltip("Layers that contain player/monster colliders.")]
    [SerializeField] private LayerMask targetMask;
    [Tooltip("Layers that block line of sight (walls, obstacles).")]
    [SerializeField] private LayerMask obstructionMask;
    [Tooltip("Height offset of the 'eye' relative to Roomba pivot.")]
    [SerializeField] private float eyeHeight = 0.4f;

    [Header("Alert")]
    [Tooltip("Sound played when a Roomba newly detects a humanoid.")]
    [SerializeField] private AudioClip alertClip;
    [SerializeField] private float alertVolume = 1.0f;

    private NavMeshAgent _agent;
    private int _currentIndex;
    private bool _waiting;
    private float _waitTimer;

    // For "newly seen" logic
    private bool _sawPlayerLastFrame;
    private bool _sawMonsterLastFrame;

    // === Public read-only access for map overlay ===
    public float VisionRange => visionRange;
    public float VisionAngle => visionAngle;
    public Vector3 EyePosition => transform.position + Vector3.up * eyeHeight;
    public int GetCurrentWaypointIndex() => _currentIndex;
    public bool GetWaiting() => _waiting;
    public float GetWaitTimer() => _waitTimer;

    public void SetPatrolState(int waypointIndex, bool waiting, float waitTimer)
    {
        _currentIndex = waypointIndex;
        _waiting = waiting;
        _waitTimer = waitTimer;

        if (waypoints != null && waypoints.Length > 0 && _agent != null)
        {
            _currentIndex = Mathf.Clamp(_currentIndex, 0, waypoints.Length - 1);
            _agent.SetDestination(waypoints[_currentIndex].position);
        }
    }

    // Normalized forward projected onto XZ plane
    public Vector3 ForwardXZ
    {
        get
        {
            Vector3 f = transform.forward;
            f.y = 0f;
            return f.sqrMagnitude > 0.0001f ? f.normalized : Vector3.forward;
        }
    }

    public LayerMask ObstructionMask => obstructionMask;


    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    private void OnEnable()
    {
        if (!AllRoombas.Contains(this))
            AllRoombas.Add(this);
    }

    private void OnDisable()
    {
        AllRoombas.Remove(this);
    }

    private void Start()
    {
        if (waypoints != null && waypoints.Length > 0)
        {
            _currentIndex = 0;
            _agent.SetDestination(waypoints[_currentIndex].position);
        }
    }

    private void Update()
    {
        HandlePatrol();
        HandleVision();
    }

    private void HandlePatrol()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        if (_waiting)
        {
            _waitTimer -= Time.deltaTime;
            if (_waitTimer <= 0f)
            {
                _waiting = false;
                GoToNextWaypoint();
            }
            return;
        }

        if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + 0.05f)
        {
            _waiting = true;
            _waitTimer = waitAtPoint;
        }
    }

    private void GoToNextWaypoint()
    {
        if (waypoints.Length <= 1) return;
        _currentIndex = (_currentIndex + 1) % waypoints.Length;
        _agent.SetDestination(waypoints[_currentIndex].position);
    }

    private void HandleVision()
    {
        Vector3 eyePos = EyePosition;

        // Overlap sphere for potential targets
        Collider[] hits = Physics.OverlapSphere(eyePos, visionRange, targetMask, QueryTriggerInteraction.Ignore);

        bool sawPlayerNow = false;
        bool sawMonsterNow = false;

        foreach (var col in hits)
        {
            DetectionTarget target = col.GetComponentInParent<DetectionTarget>();
            if (target == null) continue;

            Vector3 targetCenter = col.bounds.center;
            Vector3 dir = (targetCenter - eyePos);
            float dist = dir.magnitude;
            if (dist <= 0.01f) continue;

            Vector3 dirNorm = dir / dist;

            // FOV check
            float angle = Vector3.Angle(ForwardXZ, new Vector3(dirNorm.x, 0f, dirNorm.z));
            if (angle > visionAngle * 0.5f) continue;

            // LOS check
            if (Physics.Raycast(eyePos, dirNorm, out RaycastHit hit, dist, obstructionMask, QueryTriggerInteraction.Ignore))
            {
                // something blocked the view
                continue;
            }

            // At this point, target is in cone and visible
            var trackedType = target.type == DetectionTarget.TargetType.Player
                ? TrackingManager.TrackedType.Player
                : TrackingManager.TrackedType.Monster;

            TrackingManager.Instance?.ReportSighting(trackedType, targetCenter);

            if (trackedType == TrackingManager.TrackedType.Player)
                sawPlayerNow = true;
            else
                sawMonsterNow = true;
        }

        // Play alert sound on newly seen
        if (alertClip != null)
        {
            if (sawPlayerNow && !_sawPlayerLastFrame)
                AudioSource.PlayClipAtPoint(alertClip, transform.position, alertVolume);

            if (sawMonsterNow && !_sawMonsterLastFrame)
                AudioSource.PlayClipAtPoint(alertClip, transform.position, alertVolume);
        }

        _sawPlayerLastFrame = sawPlayerNow;
        _sawMonsterLastFrame = sawMonsterNow;
    }
    // ===== CHECKPOINT SAVE / RESTORE =====

    [Serializable]
    public struct RoombaSaveState
    {
        // transform
        public float px, py, pz;
        public float rx, ry, rz, rw;

        // patrol
        public int currentIndex;
        public bool waiting;
        public float waitTimer;

        // vision "newly seen" gate
        public bool sawPlayerLastFrame;
        public bool sawMonsterLastFrame;

        // agent essentials
        public bool agentEnabled;
        public bool agentStopped;
        public float agentSpeed;
        public float agentStoppingDistance;

        public bool hasDestination;
        public float destX, destY, destZ;
    }

    public RoombaSaveState CaptureStateForCheckpoint()
    {
        var s = new RoombaSaveState();

        var p = transform.position;
        var r = transform.rotation;
        s.px = p.x; s.py = p.y; s.pz = p.z;
        s.rx = r.x; s.ry = r.y; s.rz = r.z; s.rw = r.w;

        s.currentIndex = _currentIndex;
        s.waiting = _waiting;
        s.waitTimer = _waitTimer;

        s.sawPlayerLastFrame = _sawPlayerLastFrame;
        s.sawMonsterLastFrame = _sawMonsterLastFrame;

        if (_agent != null)
        {
            s.agentEnabled = _agent.enabled;
            s.agentStopped = _agent.isStopped;
            s.agentSpeed = _agent.speed;
            s.agentStoppingDistance = _agent.stoppingDistance;

            if (!_agent.pathPending && _agent.hasPath)
            {
                var d = _agent.destination;
                s.hasDestination = true;
                s.destX = d.x; s.destY = d.y; s.destZ = d.z;
            }
            else
            {
                s.hasDestination = false;
            }
        }

        return s;
    }

    public void RestoreStateFromCheckpoint(RoombaSaveState s)
    {
        // Restore simple fields
        _currentIndex = s.currentIndex;
        _waiting = s.waiting;
        _waitTimer = s.waitTimer;

        _sawPlayerLastFrame = s.sawPlayerLastFrame;
        _sawMonsterLastFrame = s.sawMonsterLastFrame;

        // NavMesh restore: disable -> move -> enable -> warp -> re-apply destination
        if (_agent != null)
        {
            bool wantEnabled = s.agentEnabled;

            _agent.enabled = false;

            transform.position = new Vector3(s.px, s.py, s.pz);
            transform.rotation = new Quaternion(s.rx, s.ry, s.rz, s.rw);

            _agent.enabled = wantEnabled;

            if (_agent.enabled)
            {
                _agent.Warp(transform.position);

                _agent.isStopped = s.agentStopped;
                _agent.speed = s.agentSpeed;
                _agent.stoppingDistance = s.agentStoppingDistance;

                _agent.ResetPath();

                // Prefer deterministic patrol destination if waypoints exist
                if (waypoints != null && waypoints.Length > 0)
                {
                    // clamp index safely
                    if (_currentIndex < 0) _currentIndex = 0;
                    if (_currentIndex >= waypoints.Length) _currentIndex = waypoints.Length - 1;

                    // If waiting, the roomba is at a waypoint; keep it there.
                    // If not waiting, set destination to the current waypoint (or saved destination).
                    Vector3 wp = waypoints[_currentIndex].position;
                    _agent.SetDestination(wp);
                }
                else if (s.hasDestination)
                {
                    _agent.SetDestination(new Vector3(s.destX, s.destY, s.destZ));
                }
            }
        }
        else
        {
            transform.position = new Vector3(s.px, s.py, s.pz);
            transform.rotation = new Quaternion(s.rx, s.ry, s.rz, s.rw);
        }
    }


#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 eyePos = transform.position + Vector3.up * eyeHeight;

        // Vision range
        Gizmos.color = new Color(1f, 1f, 0f, 0.75f);
        Gizmos.DrawWireSphere(eyePos, visionRange);

        // Vision cone wedge
        Gizmos.color = Color.yellow;

        Vector3 forward = ForwardXZ;
        float halfFov = visionAngle * 0.5f;
        int segments = 20;

        float startAngle = -halfFov;
        Vector3 prevDir = Quaternion.AngleAxis(startAngle, Vector3.up) * forward;
        Vector3 prevPoint = eyePos + prevDir.normalized * visionRange;

        Gizmos.DrawLine(eyePos, prevPoint);

        for (int i = 1; i <= segments; i++)
        {
            float t = (float)i / segments;
            float angle = Mathf.Lerp(-halfFov, halfFov, t);
            Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * forward;
            Vector3 point = eyePos + dir.normalized * visionRange;

            Gizmos.DrawLine(prevPoint, point);
            prevPoint = point;

            if (i == segments)
            {
                Gizmos.DrawLine(eyePos, point);
            }
        }
    }
#endif
}
