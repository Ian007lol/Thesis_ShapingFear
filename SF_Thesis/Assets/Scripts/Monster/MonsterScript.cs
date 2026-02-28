using UnityEngine;
using UnityEngine.AI;
using System;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class MonsterScript : MonoBehaviour
{
    public enum State { Wander, Investigate, Chase, GuardSafeZone, Search, Attack }

    [Header("Refs")]
    public Transform attackTarget;               

    [Header("Audio")]
    [SerializeField] private MonsterAudio monsterAudio;
    [SerializeField] private float heardYouCooldown = 3.0f;
    [SerializeField, Range(0f, 1f)] private float screamProximityThreshold = 0.55f; 
    private bool lastNoiseWasPlayer = false;

    [Header("Contact Kill")]
    public bool enableContactKill = true;
    public float contactKillRange = 0.9f; 

    private float lastHeardYouTime = -999f;
    private bool screamedThisChase = false;

    private bool _investigateArrived;

    [Header("Speeds")]
    public float walkSpeed = 2.2f;
    public float runSpeed = 4.2f;
    public float turnSpeed = 720f;

    [Header("Wander")]
    public float wanderRadius = 12f; 
    public float wanderMinRadius = 4f; 
    public float wanderMinPause = 0.8f;
    public float wanderMaxPause = 2.2f;

    [Header("Hearing")]
    public float hearingForgetTime = 3f; 
    public bool requireReachable = true; 

    [Header("Chase/Search")]
    public float chaseRepathInterval = 0.15f;
    public float investigateTime = 3f;
    public float searchTime = 6f;
    public float attackRange = 1.7f;

    [Header("Safe Zone Guarding")]
    public float boundarySearchRadius = 12f;
    public float boundaryStandOff = 0.35f;
    public float guardTime = 6f;

    
    [Header("Lock-On (Hearing Certainty)")]
    public bool enableLockOn = true;
    public float lockOnRadius = 2.5f; 
    public float certaintyThreshold = 0.7f; 
    public float certaintyGainPerEvent = 0.4f;
    public float certaintyDecayPerSecond = 0.25f;
    public float unlockIfNoNoiseFor = 2.0f;
    public float unlockDistance = 15f; 
    public float attackStoppingDistance = 0.2f;
    public float attackRepathInterval = 0.05f;

    //Debug generated with ChatGPT
    [Header("Debug")]
    public bool debugConsole = true; 
    public bool debugGizmoBar = true; 
    public Vector3 debugBarOffset = new Vector3(0, 2.0f, 0); 

    //Wander Debug generated with ChatGPT
    [Header("Wander Debug")]
    public bool debugDrawWanderDonut = true;
    public Color debugOuterColor = new Color(0f, 0.8f, 1f, 0.8f);
    public Color debugInnerColor = new Color(1f, 0.3f, 0.3f, 0.8f);
    public Color debugFillColor = new Color(0f, 0.6f, 1f, 0.15f);

    [Header("Decoy Interaction")]
    [Tooltip("How close the monster must get to a VoiceDecoy to destroy it.")]
    public float decoyDestroyRadius = 1.2f;
    [Tooltip("Delay before destroying the decoy after reaching it (for dramatic effect).")]
    public float decoyDestroyDelay = 0.8f;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    //Animation Sync generated with ChatGPT
    [Header("Animation Sync")]
    [SerializeField] private string moveSpeedParam = "MoveSpeed";
    [SerializeField] private float animAtWalk = 0.55f; // tune in play mode
    [SerializeField] private float animAtRun = 1.25f; // tune in play mode
    [SerializeField] private float animSmooth = 0.12f; // smoothing time

    private VoiceDecoy currentDecoyTarget;
    private bool destroyingDecoy = false;

    private bool paused;
    private bool listeningEnabled = true;

    [Header("Kill")]
    public float killRange = 1.2f; 
    public float killWindup = 0.0f; 

    private bool hasKilledPlayer = false;
    private float lastRealPlayerNoiseTime = -999f;

    // runtime
    private NavMeshAgent agent;
    private State state;
    private float stateTimer, repathTimer;

    private Vector3 lastHeard;
    private bool heardRecently;
    private float heardTimer;

    // wander
    private Vector3 wanderTarget;
    private float wanderPauseTimer;

    // guard
    private Vector3 guardPoint;

    private bool attackGrowlPlayed = false;
    private bool killInProgress = false;

    // lock-on
    private bool lockedOn;
    private float certainty; 
    private float sinceLastNoise; 

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.autoBraking = false; 

        if (!monsterAudio) monsterAudio = GetComponent<MonsterAudio>();
        if (!animator) animator = GetComponentInChildren<Animator>();
    }

    void OnEnable()
    {
        Noise.OnNoise += OnNoise;
        StartCoroutine(DelayedStart());
    }

    void OnDisable()
    {
        Noise.OnNoise -= OnNoise;
        if (agent != null) agent.isStopped = true;
    }

    private IEnumerator DelayedStart() //method generated with ChatGPT
    {
        // Wait until NavMeshAgent is fully initialized
        yield return null;

        if (!agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out var hit, 2f, agent.areaMask))
            {
                agent.Warp(hit.position);
            }
            else
            {
                Debug.LogError("Monster could not find NavMesh at startup.");
                yield break;
            }
        }

        agent.isStopped = false;
        agent.speed = walkSpeed;
        agent.stoppingDistance = 0f;
        StartWander();
    }

    void Update()
    {
        if (paused) return;
        if (killInProgress) return;

        ContactKillCheck(); 

        // hearing memory/decay
        if (heardRecently)
        {
            heardTimer += Time.deltaTime;
            if (heardTimer > hearingForgetTime) heardRecently = false;
        }

        // certainty decay + timers
        if (certainty > 0f) certainty = Mathf.Max(0f, certainty - certaintyDecayPerSecond * Time.deltaTime);
        sinceLastNoise += Time.deltaTime;

        // lock / unlock checks
        if (enableLockOn && attackTarget != null && currentDecoyTarget == null)
        {
            Vector3 tgtPos = attackTarget.position;
            bool targetInSafe = SafeAreaVolume.IsPointInside(tgtPos);
            float distToTarget = Vector3.Distance(transform.position, tgtPos);

            // Try to acquire lock
            if (!lockedOn && !targetInSafe)
            {
                bool nearEnough = distToTarget <= lockOnRadius;
                bool confident = certainty >= certaintyThreshold && (Time.time - lastRealPlayerNoiseTime) <= hearingForgetTime;

                if (requireReachable && NavMesh.SamplePosition(tgtPos, out var hit, 2f, agent.areaMask))
                {
                    var path = new NavMeshPath();
                    if (!agent.CalculatePath(hit.position, path) || path.status != NavMeshPathStatus.PathComplete)
                    {
                        nearEnough = false;
                        confident = false;
                    }
                }

                if (nearEnough || confident)
                {
                    lockedOn = true;
                    
                    agent.stoppingDistance = attackStoppingDistance;
                }
            }

            if (lockedOn)
            {
                if (targetInSafe || (distToTarget > unlockDistance && sinceLastNoise >= unlockIfNoNoiseFor))
                {
                    lockedOn = false;
                }
            }
        }

        
        if (heardRecently && SafeAreaVolume.IsPointInside(lastHeard))
        {
            if (TryGetNearestBoundary(lastHeard, boundarySearchRadius, agent.areaMask, out var edge, out guardPoint, boundaryStandOff))
                SetState(State.GuardSafeZone);
            else
                StartWander();

            heardRecently = false; 
        }

        
        stateTimer += Time.deltaTime;

        switch (state)
        {
            case State.Wander: WanderTick(); break;
            case State.Investigate: InvestigateTick(); break;
            case State.Chase: ChaseTick(); break;
            case State.GuardSafeZone: GuardTick(); break;
            case State.Search: SearchTick(); break;
            case State.Attack: AttackTick(); break;
        }

        if (monsterAudio != null && agent != null)
        {
            bool runMode = (state == State.Chase || state == State.Attack);
            monsterAudio.UpdateFootsteps(agent.velocity, runMode);
        }

        if (animator != null && agent != null) // IF snippet generated with ChatGPT
        {
            float worldSpeed = new Vector3(agent.velocity.x, 0f, agent.velocity.z).magnitude;

            // 0..1 where 0 = stopped, 1 = runSpeed
            float t = (runSpeed > 0.01f) ? Mathf.Clamp01(worldSpeed / runSpeed) : 0f;

            // Calibrated mapping: walking and running can have different “visual multipliers”
            float targetAnimSpeed = Mathf.Lerp(0f, animAtRun, t);

            // Optional: force walk to hit animAtWalk exactly when in walk mode
            if (state != State.Chase && state != State.Attack && walkSpeed > 0.01f)
            {
                float walkT = Mathf.Clamp01(worldSpeed / walkSpeed);
                targetAnimSpeed = Mathf.Lerp(0f, animAtWalk, walkT);
            }

            animator.SetFloat(moveSpeedParam, targetAnimSpeed, animSmooth, Time.deltaTime);
        }

        //orient monster to where he is going
        Vector3 v = agent.desiredVelocity;
        v.y = 0f;
        if (v.sqrMagnitude > 0.01f)
        {
            var r = Quaternion.LookRotation(v.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, r, turnSpeed * Time.deltaTime);
        }

        if (monsterAudio != null && attackTarget != null) //If snippet generated with ChatGPT
        {
            bool isChasing = state == State.Chase || state == State.Attack;
            bool playerSafe = SafeAreaVolume.IsPointInside(attackTarget.position);

            monsterAudio.UpdatePresence(
                transform.position,
                attackTarget.position,
                isChasing,
                playerSafe
            );
        }
    }

    public void ForceInvestigate(Vector3 point) //method generated with ChatGPT
    {
        // Stop anything that could override our forced state (decoy destroy, delayed start, etc.)
        StopAllCoroutines();

        // Clear decoy runtime so it doesn't fight the forced command
        currentDecoyTarget = null;
        destroyingDecoy = false;

        // Break lock-on and chase memory
        lockedOn = false;
        certainty = 0f;
        sinceLastNoise = 0f;

        // If you set heardRecently=true, your safe-zone guard logic can override it immediately.
        heardRecently = false;
        heardTimer = 0f;

        // Snap to NavMesh to avoid silent SetDestination failures
        Vector3 target = point;
        if (agent != null && agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(point, out var hit, 2f, agent.areaMask))
                target = hit.position;
        }

        lastHeard = target;
        SetState(State.Investigate);

        // Debug: confirm we actually set a path
        if (agent != null && agent.isOnNavMesh)
        {
            bool ok = agent.SetDestination(lastHeard);
            Debug.Log($"[Monster] ForceInvestigate -> ok={ok}, target={lastHeard}, pathStatus={agent.pathStatus}");
        }
        else
        {
            Debug.LogWarning("[Monster] ForceInvestigate called but agent is null or not on NavMesh.");
        }
    }

    private void ContactKillCheck()
    {
        if (!enableContactKill) return;
        if (hasKilledPlayer) return;
        if (paused) return;
        if (attackTarget == null) return;

        // Don't kill in safe areas
        if (SafeAreaVolume.IsPointInside(attackTarget.position)) return;

        // If the player is already dead, don't re-trigger, the monster will spam it otherwise. Duh.
        var death = attackTarget.GetComponentInParent<PlayerDeathHandler>();
        if (death != null && death.IsDead) return;

        float d = Vector3.Distance(transform.position, attackTarget.position);
        if (!killInProgress && !hasKilledPlayer && d <= contactKillRange)
        {
            killInProgress = true;
            hasKilledPlayer = true;
            StartCoroutine(KillPlayerSequence());
            return;
        }
    }

    public void TeleportTo(Vector3 position, Quaternion rotation, bool matchRotation = true) //method generated with ChatGPT
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.Warp(position);
        }
        else
        {
            transform.position = position;
        }

        if (matchRotation) transform.rotation = rotation;

        // Clear pathing so it doesn't instantly continue old path
        //if (agent != null) agent.ResetPath();
    }


    private void OnNoise(Noise.NoiseEvent ev)
    {
        if (paused || !listeningEnabled) return;

        
        if (SafeAreaVolume.IsPointInside(ev.position)) return;
        var mic = attackTarget.GetComponentInParent<PlayerMicNoiseEmitter>();
        bool isRedirecting = (mic != null && mic.voiceRedirectTarget != null);
        
        if (requireReachable && !isRedirecting)
        {
            if (!NavMesh.SamplePosition(ev.position, out var sample, 2f, agent.areaMask)) return;

            var path = new NavMeshPath();
            if (!agent.CalculatePath(sample.position, path) || path.status != NavMeshPathStatus.PathComplete) return;
        }

        float dist = Vector3.Distance(transform.position, ev.position);
        if (dist <= ev.loudness)
        {
            lastHeard = ev.position;
            heardRecently = true;
            heardTimer = 0f;
            sinceLastNoise = 0f;

            float proximity = Mathf.Clamp01(1f - (dist / Mathf.Max(0.01f, ev.loudness)));
            bool isDecoy = (currentDecoyTarget != null);
            bool isPlayerNoise = lastNoiseWasPlayer;

            if (lastNoiseWasPlayer) lastRealPlayerNoiseTime = Time.time;

            
            if (isPlayerNoise)
            {
                float gain = certaintyGainPerEvent * Mathf.Lerp(0.5f, 1f, proximity);
                certainty = Mathf.Clamp01(certainty + gain);
            }

            
            currentDecoyTarget = null;
            lastNoiseWasPlayer = false;

            if (ev.source != null)
            {
                currentDecoyTarget = ev.source.GetComponent<VoiceDecoy>();

                
                if (attackTarget != null && ev.source != null && ev.source.transform.IsChildOf(attackTarget))
                {
                    lastNoiseWasPlayer = !isRedirecting;
                }
            }

            bool wasChasingOrAttacking = (state == State.Chase || state == State.Attack);

            
            bool justBecameConfident = enableLockOn && !lockedOn && certainty >= certaintyThreshold;
            if (!wasChasingOrAttacking && (justBecameConfident || dist <= lockOnRadius))
            {
                monsterAudio?.PlayHeardYouScream();
            }

            bool wasRunningState = (state == State.Chase || state == State.Attack);

         
            if (!wasRunningState && Time.time >= lastHeardYouTime + heardYouCooldown)
            {
             
                if (proximity >= screamProximityThreshold)
                {
                    monsterAudio?.PlayHeardYouScream();
                    lastHeardYouTime = Time.time;
                    screamedThisChase = true;
                }
            }

            
            SetState(State.Chase);
        }
    }

   
    private void StartWander()
    {
        state = State.Wander;
        stateTimer = repathTimer = 0f;

        agent.isStopped = false;
        agent.speed = walkSpeed;
        agent.stoppingDistance = 0.0f;

        if (!PickWanderPoint(out wanderTarget))
        {
            agent.isStopped = true;
            return;
        }

        agent.SetDestination(wanderTarget);
        wanderPauseTimer = UnityEngine.Random.Range(wanderMinPause, wanderMaxPause);
    }

    private void SetState(State s)
    {
        
        if (killInProgress) return;

        State prev = state;
        if (prev == s) return;

        state = s;
        stateTimer = repathTimer = 0f;

       
        if (prev == State.Attack && s != State.Attack)
            attackGrowlPlayed = false;

        switch (s)
        {
            case State.Investigate:
                _investigateArrived = false;
                agent.isStopped = false;
                agent.speed = walkSpeed;
                agent.stoppingDistance = 0.0f;
                agent.SetDestination(lastHeard);
                break;

            case State.Chase:
                agent.isStopped = false;
                agent.speed = runSpeed;
                agent.stoppingDistance = 0.0f;
                break;

            case State.GuardSafeZone:
                agent.isStopped = false;
                agent.speed = walkSpeed;
                agent.stoppingDistance = 0.0f;
                agent.SetDestination(guardPoint);
                break;

            case State.Search:
                agent.isStopped = false;
                agent.speed = walkSpeed;
                agent.stoppingDistance = 0.0f;
                agent.SetDestination(lastHeard);
                break;

            case State.Attack:
                agent.isStopped = false;
                agent.speed = runSpeed;
                agent.stoppingDistance = attackStoppingDistance;
                if (!attackGrowlPlayed)
                {
                    attackGrowlPlayed = true;
                    monsterAudio?.PlayAttackGrowl();
                }
                break;

            case State.Wander:
                StartWander();
                break;
        }
    }

    private void WanderTick()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.05f)
        {
            wanderPauseTimer -= Time.deltaTime;
            if (wanderPauseTimer <= 0f)
            {
                if (PickWanderPoint(out wanderTarget))
                    agent.SetDestination(wanderTarget);

                wanderPauseTimer = UnityEngine.Random.Range(wanderMinPause, wanderMaxPause);
            }
        }
    }

    private void InvestigateTick()
    {
        if (agent.pathPending) return;

        if (agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {

            if (!_investigateArrived)
            {
                _investigateArrived = true;
                stateTimer = 0f; 
                agent.ResetPath(); 
                return;
            }

            
            if (stateTimer >= investigateTime)
            {
                monsterAudio?.PlayFrustrated();
                SetState(State.Search);
            }
        }
    }

    private void ChaseTick()
    {
        
        if (currentDecoyTarget != null && !destroyingDecoy)
        {
            if (currentDecoyTarget.gameObject == null)
            {
               
                currentDecoyTarget = null;
            }
            else
            {
                float distToDecoy = Vector3.Distance(transform.position, currentDecoyTarget.transform.position);

             
                if (distToDecoy <= decoyDestroyRadius)
                {
                    destroyingDecoy = true;
                    agent.isStopped = true; 
                    StartCoroutine(DestroyDecoyAfterDelay());
                    return;
                }
            }
        }

       
        repathTimer += Time.deltaTime;

        if (lockedOn && currentDecoyTarget == null && attackTarget != null && !SafeAreaVolume.IsPointInside(attackTarget.position))
        {
            if (repathTimer >= chaseRepathInterval)
            {
                repathTimer = 0f;
                agent.SetDestination(attackTarget.position);
            }

            float d = Vector3.Distance(transform.position, attackTarget.position);
            if (d <= attackRange)
            {
                SetState(State.Attack);
                return;
            }
        }
        else
        {
            if (repathTimer >= chaseRepathInterval)
            {
                repathTimer = 0f;
                agent.SetDestination(lastHeard);
            }

            
            if (currentDecoyTarget == null && attackTarget != null)
            {
                float d = Vector3.Distance(transform.position, attackTarget.position);
                if (d <= attackRange)
                {
                    SetState(State.Attack);
                    return;
                }
            }

            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
            {
                SetState(State.Search);
            }
        }
    }

    private void GuardTick()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.05f)
        {
            Vector3 look = (lastHeard - transform.position);
            look.y = 0f;
            if (look.sqrMagnitude > 0.001f)
            {
                var r = Quaternion.LookRotation(look.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, r, turnSpeed * Time.deltaTime);
            }
        }

        if (stateTimer >= guardTime)
        {
            StartWander();
        }
    }

    private void SearchTick()
    {
        if (stateTimer >= searchTime)
        {
            monsterAudio?.PlayFrustrated();
            StartWander();
        }
    }

    private void AttackTick()
    {
        
        repathTimer += Time.deltaTime;

        if (attackTarget == null)
        {
            SetState(State.Search);
            return;
        }

        
        if (SafeAreaVolume.IsPointInside(attackTarget.position))
        {
            lockedOn = false;
            SetState(State.Search);
            return;
        }

        if (repathTimer >= attackRepathInterval)
        {
            repathTimer = 0f;
            agent.SetDestination(attackTarget.position);
        }

        float d = Vector3.Distance(transform.position, attackTarget.position);

        if (!killInProgress && !hasKilledPlayer && d <= contactKillRange)
        {
            killInProgress = true;
            hasKilledPlayer = true;
            StartCoroutine(KillPlayerSequence());
            return;
        }

   
        if (d > attackRange * 1.25f)
        {
            SetState(State.Chase);
        }
    }

    private IEnumerator KillPlayerSequence()
    {
        agent.isStopped = true;
        agent.ResetPath();

        if (attackTarget != null)
        {
            Vector3 look = attackTarget.position - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
        }

        if (killWindup > 0f)
            yield return new WaitForSeconds(killWindup);

        if (attackTarget == null) yield break;

        var death = attackTarget.GetComponentInParent<PlayerDeathHandler>();
        if (death != null && !death.IsDead)
        {
            monsterAudio?.PlayKill();
            death.Kill();
        }
    }

    
    private bool PickWanderPoint(out Vector3 result) //method generated with ChatGPT
    {
        result = transform.position;
        const int MaxTries = 30;

        // clamp so inner never exceeds outer
        float outer = Mathf.Max(0.01f, wanderRadius);
        float inner = Mathf.Clamp(wanderMinRadius, 0f, outer - 0.01f);

        for (int i = 0; i < MaxTries; i++)
        {
            // --- Uniform over donut area ---
            // pick angle
            float a = UnityEngine.Random.value * Mathf.PI * 2f;

            // pick radius with sqrt for uniform area distribution
            float t = UnityEngine.Random.value;
            float r = Mathf.Sqrt(Mathf.Lerp(inner * inner, outer * outer, t));

            Vector3 p = transform.position + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);

            if (!NavMesh.SamplePosition(p, out var hit, 2.0f, agent.areaMask)) continue;
            if (SafeAreaVolume.IsPointInside(hit.position)) continue;

            //enforce reachable wander destinations
            if (requireReachable)
            {
                var path = new NavMeshPath();
                if (!agent.CalculatePath(hit.position, path) || path.status != NavMeshPathStatus.PathComplete) continue;
            }

            result = hit.position;
            return true;
        }

        return false;
    }

    private bool TryGetNearestBoundary(Vector3 insidePoint, float searchRadius, int areaMask, out NavMeshHit boundaryHit, out Vector3 guardPos, float standOff = 0.25f) //method generated with ChatGPT
    {
        boundaryHit = default;
        guardPos = Vector3.zero;

        if (NavMesh.SamplePosition(insidePoint, out var nearest, searchRadius, areaMask))
        {
            if (NavMesh.FindClosestEdge(nearest.position, out boundaryHit, areaMask))
            {
                guardPos = boundaryHit.position + boundaryHit.normal * standOff;
                return true;
            }
        }

        return false;
    }

    private System.Collections.IEnumerator DestroyDecoyAfterDelay()
    {
        // Optional: monster stands still, maybe play an animation later
        yield return new WaitForSeconds(decoyDestroyDelay);

        if (currentDecoyTarget != null)
        {
            currentDecoyTarget.DestroyByMonster();
            currentDecoyTarget = null;
        }

        // Reset hearing state because sound was fake
        heardRecently = false;
        certainty = 0f;
        sinceLastNoise = 0f;

        destroyingDecoy = false;
        agent.isStopped = false;

        // After destroying, begin searching
        SetState(State.Search);
    }

    public void SetPaused(bool value) //method generated with ChatGPT
    {
        paused = value;
        if (agent != null)
        {
            agent.isStopped = value;
            if (value) agent.ResetPath();
        }
    }

    public void SetListeningEnabled(bool value) //method generated with ChatGPT
    {
        listeningEnabled = value;
    }

    [Serializable]
    public struct MonsterSaveState //method generated with ChatGPT
    {
        // transform
        public float px, py, pz;
        public float rx, ry, rz, rw;

        // FSM + key runtime
        public int state;
        public float stateTimer;
        public float repathTimer;

        public float lastHeardX, lastHeardY, lastHeardZ;
        public bool heardRecently;
        public float heardTimer;

        public float wanderTargetX, wanderTargetY, wanderTargetZ;
        public float wanderPauseTimer;

        public float guardPointX, guardPointY, guardPointZ;

        // lock-on
        public bool lockedOn;
        public float certainty;
        public float sinceLastNoise;

        // misc
        public bool paused;
        public bool listeningEnabled;

        // chase flags
        public bool screamedThisChase;
        public float lastHeardYouTime;
        public bool hasKilledPlayer;

        // agent essentials
        public bool agentEnabled;
        public bool agentStopped;
        public float agentSpeed;
        public float agentStoppingDistance;

        public bool hasDestination;
        public float destX, destY, destZ;

        // decoy targeting (we will NOT restore a decoy reference reliably in v1)
    }

    public MonsterSaveState CaptureStateForCheckpoint() //method generated with ChatGPT
    {
        var s = new MonsterSaveState();

        var p = transform.position;
        var r = transform.rotation;

        s.px = p.x; s.py = p.y; s.pz = p.z;
        s.rx = r.x; s.ry = r.y; s.rz = r.z; s.rw = r.w;

        s.state = (int)state;
        s.stateTimer = stateTimer;
        s.repathTimer = repathTimer;

        s.lastHeardX = lastHeard.x; s.lastHeardY = lastHeard.y; s.lastHeardZ = lastHeard.z;
        s.heardRecently = heardRecently;
        s.heardTimer = heardTimer;

        s.wanderTargetX = wanderTarget.x; s.wanderTargetY = wanderTarget.y; s.wanderTargetZ = wanderTarget.z;
        s.wanderPauseTimer = wanderPauseTimer;

        s.guardPointX = guardPoint.x; s.guardPointY = guardPoint.y; s.guardPointZ = guardPoint.z;

        s.lockedOn = lockedOn;
        s.certainty = certainty;
        s.sinceLastNoise = sinceLastNoise;

        s.paused = paused;
        s.listeningEnabled = listeningEnabled;

        s.screamedThisChase = screamedThisChase;
        s.lastHeardYouTime = lastHeardYouTime;
        s.hasKilledPlayer = hasKilledPlayer;

        if (agent != null)
        {
            s.agentEnabled = agent.enabled;
            s.agentStopped = agent.isStopped;
            s.agentSpeed = agent.speed;
            s.agentStoppingDistance = agent.stoppingDistance;

            // destination: only meaningful if it has a path / was set
            // (Unity doesn't expose "hasDestination", so we approximate)
            if (!agent.pathPending && agent.hasPath)
            {
                var d = agent.destination;
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

    public void RestoreStateFromCheckpoint(MonsterSaveState s) //method generated with ChatGPT
    {
        // Stop coroutines that would fight state (esp. decoy destroy)
        StopAllCoroutines();

        // Reset decoy-related runtime safely
        currentDecoyTarget = null;
        destroyingDecoy = false;

        // Restore basic flags
        paused = s.paused;
        listeningEnabled = s.listeningEnabled;

        screamedThisChase = s.screamedThisChase;
        lastHeardYouTime = s.lastHeardYouTime;
        hasKilledPlayer = s.hasKilledPlayer;

        // ✅ Hard reset runtime-only kill gates (they should never persist across checkpoint loads)
        killInProgress = false;
        hasKilledPlayer = false;
        attackGrowlPlayed = false; // if you added this gate

        // ✅ Also reset decoy / destroy logic so nothing is stuck
        currentDecoyTarget = null;
        destroyingDecoy = false;

        // ✅ Ensure we are active again after a respawn
        paused = false;
        listeningEnabled = true;

        state = (State)s.state;
        stateTimer = s.stateTimer;
        repathTimer = s.repathTimer;

        lastHeard = new Vector3(s.lastHeardX, s.lastHeardY, s.lastHeardZ);
        heardRecently = s.heardRecently;
        heardTimer = s.heardTimer;

        wanderTarget = new Vector3(s.wanderTargetX, s.wanderTargetY, s.wanderTargetZ);
        wanderPauseTimer = s.wanderPauseTimer;

        guardPoint = new Vector3(s.guardPointX, s.guardPointY, s.guardPointZ);

        lockedOn = s.lockedOn;
        certainty = s.certainty;
        sinceLastNoise = s.sinceLastNoise;

        // NavMeshAgent: safest restore = disable -> warp -> enable -> set params -> set destination
        if (agent != null)
        {
            bool wantEnabled = s.agentEnabled;

            agent.enabled = false;
            transform.position = new Vector3(s.px, s.py, s.pz);
            transform.rotation = new Quaternion(s.rx, s.ry, s.rz, s.rw);
            agent.enabled = wantEnabled;

            if (agent.enabled)
            {
                agent.Warp(transform.position);
                agent.isStopped = s.agentStopped || paused;
                agent.speed = s.agentSpeed;
                agent.stoppingDistance = s.agentStoppingDistance;

                agent.ResetPath();

                if (!agent.isStopped && s.hasDestination)
                {
                    agent.SetDestination(new Vector3(s.destX, s.destY, s.destZ));
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
//gizmos generated with ChatGPT
    private void OnDrawGizmos()
    {
        if (!debugGizmoBar) return;

        // draw bar above the monster to show certainty (green) vs threshold (red line)
        Vector3 pos = transform.position + debugBarOffset;
        float barWidth = 1.5f;
        float barHeight = 0.15f;

        // certainty bar background
        Gizmos.color = Color.gray;
        Gizmos.DrawCube(pos, new Vector3(barWidth, barHeight, 0.02f));

        // fill amount
        float fill = Mathf.Clamp01(certainty);
        Gizmos.color = Color.green;
        Vector3 fillSize = new Vector3(barWidth * fill, barHeight * 0.9f, 0.02f);
        Vector3 fillPos = pos - new Vector3((barWidth - fillSize.x) * 0.5f, 0, 0);
        Gizmos.DrawCube(fillPos, fillSize);

        // threshold marker
        Gizmos.color = Color.red;
        float threshX = pos.x - barWidth / 2 + barWidth * certaintyThreshold;
        Gizmos.DrawLine(new Vector3(threshX, pos.y - barHeight / 2, pos.z), new Vector3(threshX, pos.y + barHeight / 2, pos.z));
    }

    private void OnDrawGizmosSelected()
    {
        if (!debugDrawWanderDonut) return;

        Vector3 center = transform.position;
        center.y += 0.05f; // lift slightly so it doesn't Z-fight the ground

        float outer = Mathf.Max(0.01f, wanderRadius);
        float inner = Mathf.Clamp(wanderMinRadius, 0f, outer - 0.01f);

        const int SEGMENTS = 64;

        // --- Outer circle ---
        Gizmos.color = debugOuterColor;
        DrawCircle(center, outer, SEGMENTS);

        // --- Inner circle (hole) ---
        if (inner > 0.01f)
        {
            Gizmos.color = debugInnerColor;
            DrawCircle(center, inner, SEGMENTS);
        }

        // --- Fill (radial spokes so it reads as a donut) ---
        Gizmos.color = debugFillColor;
        for (int i = 0; i < SEGMENTS; i++)
        {
            float a = (i / (float)SEGMENTS) * Mathf.PI * 2f;
            Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            Gizmos.DrawLine(center + dir * inner, center + dir * outer);
        }
    }

    // Helper
    private void DrawCircle(Vector3 center, float radius, int segments)
    {
        Vector3 prev = center + new Vector3(radius, 0f, 0f);

        for (int i = 1; i <= segments; i++)
        {
            float a = (i / (float)segments) * Mathf.PI * 2f;
            Vector3 next = center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
#endif
}
