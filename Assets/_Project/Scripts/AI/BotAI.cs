using System;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using TermProject.Game;
using TermProject.Player;

namespace TermProject.AI
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Damageable))]
    [DisallowMultipleComponent]
    public sealed class BotAI : MonoBehaviour
    {
        public enum BotState
        {
            Patrol,
            Detect,
            Chase,
            Attack,
            Retreat,
            Dead
        }

        [Header("References")]
        [SerializeField] private Transform player;
        [SerializeField] private Damageable playerDamageable;
        [SerializeField] private Transform eyePoint;
        [SerializeField] private Transform[] patrolPoints;

        [Header("Sensing")]
        [SerializeField] private float detectionRange = 18f;
        [SerializeField] private float attackRange = 8f;
        [SerializeField] private float fieldOfViewAngle = 120f;
        [SerializeField] private float loseSightDelay = 2f;
        [SerializeField] private LayerMask lineOfSightLayers = ~0;

        [Header("Movement")]
        [SerializeField] private bool randomPatrol = true;
        [SerializeField] private float patrolWaitTime = 1.5f;
        [SerializeField] private float patrolReachDistance = 0.8f;
        [SerializeField] private float patrolDestinationSpread = 1.2f;
        [SerializeField] private float patrolStuckRetryTime = 3f;
        [SerializeField] private float patrolRetryInterval = 0.5f;
        [SerializeField] private float patrolRecoveryDelay = 2f;
        [SerializeField] private float patrolRecoverySampleRadius = 4f;
        [SerializeField] private float navMeshDestinationSampleRadius = 2f;
        [SerializeField] private float stoppingDistance = 1.6f;
        [SerializeField] private float turnSpeed = 9f;

        [Header("Attack")]
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private float attackCooldown = 1f;
        [SerializeField] private float maxAttackHeightDifference = 2.25f;

        [Header("Death")]
        [SerializeField] private bool destroyAfterDeath = true;
        [SerializeField] private float deathDestroyDelay = 2f;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip attackSound;
        [SerializeField] private AudioClip deathSound;
        [SerializeField, Range(0f, 1f)] private float attackVolume = 0.7f;
        [SerializeField, Range(0f, 1f)] private float deathVolume = 0.8f;

        [Header("Debug")]
        [SerializeField] private BotState currentState = BotState.Patrol;
        [SerializeField] private bool writeDebugLog = true;
        [SerializeField] private float debugLogInterval = 0.5f;

        private NavMeshAgent agent;
        private Damageable damageable;
        private static string debugLogPath;
        private static bool debugLogPathAnnounced;
        private static int nextDebugId;
        private float lastSawPlayerTime = -999f;
        private float nextAttackTime;
        private float nextDebugLogTime;
        private float patrolWaitUntil;
        private float nextPatrolAttemptTime;
        private float patrolRecoveryReadyTime;
        private int patrolIndex;
        private int lastPatrolIndex = -1;
        private int debugId;
        private float patrolDestinationSetTime;
        private Vector3 currentPatrolDestination;
        private bool hasPatrolDestination;
        private bool initialized;
        private bool deathRegistered;

        public BotState CurrentState => currentState;

        public void ConfigureForWave(
            Transform targetPlayer,
            Damageable targetPlayerDamageable,
            Transform[] sharedPatrolPoints,
            float waveAttackDamage,
            float waveAttackCooldown,
            float waveMoveSpeed)
        {
            if (targetPlayer != null)
            {
                player = targetPlayer;
            }

            if (targetPlayerDamageable != null)
            {
                playerDamageable = targetPlayerDamageable;
            }

            if ((patrolPoints == null || patrolPoints.Length == 0) && sharedPatrolPoints != null)
            {
                patrolPoints = sharedPatrolPoints;
            }

            attackDamage = Mathf.Max(0f, waveAttackDamage);
            attackCooldown = Mathf.Max(0.05f, waveAttackCooldown);
            deathRegistered = false;

            if (agent == null)
            {
                agent = GetComponent<NavMeshAgent>();
            }

            if (agent != null)
            {
                agent.speed = Mathf.Max(0.1f, waveMoveSpeed);
                agent.stoppingDistance = stoppingDistance;
            }
        }

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            damageable = GetComponent<Damageable>();
            debugId = ++nextDebugId;

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            if (audioSource != null)
            {
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f;
            }

            if (eyePoint == null)
            {
                eyePoint = transform;
            }

            if (player == null)
            {
                PlayerController playerController = FindFirstObjectByType<PlayerController>();

                if (playerController != null)
                {
                    player = playerController.transform;
                }
            }

            if (playerDamageable == null && player != null)
            {
                playerDamageable = player.GetComponent<Damageable>();
            }

            agent.stoppingDistance = stoppingDistance;
            initialized = true;
            WriteAiDebugLog("Awake", false);
        }

        private void OnEnable()
        {
            if (damageable != null)
            {
                damageable.Died.AddListener(HandleDeath);
            }
        }

        private void OnDisable()
        {
            if (damageable != null)
            {
                damageable.Died.RemoveListener(HandleDeath);
            }
        }

        private void Start()
        {
            ChangeState(BotState.Patrol);
            WriteAiDebugLog("Start", CanSeePlayer());
        }

        private void Update()
        {
            if (!initialized || currentState == BotState.Dead || !agent.isOnNavMesh)
            {
                WriteAiDebugLogThrottled("SkippedUpdate", false);
                return;
            }

            bool canSeePlayer = CanSeePlayer();

            if (canSeePlayer)
            {
                lastSawPlayerTime = Time.time;
            }

            switch (currentState)
            {
                case BotState.Patrol:
                    UpdatePatrol(canSeePlayer);
                    break;

                case BotState.Detect:
                    UpdateDetect(canSeePlayer);
                    break;

                case BotState.Chase:
                    UpdateChase(canSeePlayer);
                    break;

                case BotState.Attack:
                    UpdateAttack(canSeePlayer);
                    break;

                case BotState.Retreat:
                    UpdateRetreat(canSeePlayer);
                    break;
            }

            WriteAiDebugLogThrottled("Tick", canSeePlayer);
        }

        private void UpdatePatrol(bool canSeePlayer)
        {
            if (canSeePlayer)
            {
                ChangeState(BotState.Detect);
                return;
            }

            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                ResetAgentPath();
                hasPatrolDestination = false;
                WriteAiDebugLogThrottled("NoPatrolPoints", canSeePlayer);
                return;
            }

            if (!hasPatrolDestination)
            {
                if (Time.time >= nextPatrolAttemptTime)
                {
                    TrySetNextPatrolDestination();
                }

                return;
            }

            if (agent.pathPending)
            {
                return;
            }

            float reachDistance = Mathf.Max(agent.stoppingDistance, patrolReachDistance);

            if (agent.remainingDistance > reachDistance)
            {
                if (ShouldRetryPatrolDestination())
                {
                    WriteAiDebugLog("PatrolRetry", canSeePlayer);
                    TrySetNextPatrolDestination();
                }

                return;
            }

            if (patrolWaitUntil <= 0f)
            {
                patrolWaitUntil = Time.time + patrolWaitTime;
                WriteAiDebugLog("PatrolWaitStarted", canSeePlayer);
                return;
            }

            if (Time.time < patrolWaitUntil)
            {
                return;
            }

            TrySetNextPatrolDestination();
        }

        private void UpdateDetect(bool canSeePlayer)
        {
            FacePlayer();

            if (!canSeePlayer)
            {
                ChangeState(BotState.Patrol);
                return;
            }

            ChangeState(IsPlayerInAttackRange() ? BotState.Attack : BotState.Chase);
        }

        private void UpdateChase(bool canSeePlayer)
        {
            if (ShouldForgetPlayer(canSeePlayer))
            {
                ChangeState(BotState.Patrol);
                return;
            }

            if (player == null)
            {
                ChangeState(BotState.Patrol);
                return;
            }

            SetAgentDestination(player.position);

            if (canSeePlayer && IsPlayerInAttackRange())
            {
                ChangeState(BotState.Attack);
            }
        }

        private void UpdateAttack(bool canSeePlayer)
        {
            if (ShouldForgetPlayer(canSeePlayer))
            {
                ChangeState(BotState.Patrol);
                return;
            }

            if (!IsPlayerInAttackRange())
            {
                ChangeState(BotState.Chase);
                return;
            }

            FacePlayer();

            if (!canSeePlayer || Time.time < nextAttackTime)
            {
                return;
            }

            nextAttackTime = Time.time + attackCooldown;

            if (playerDamageable != null)
            {
                playerDamageable.ApplyDamage(attackDamage);
                PlaySound(attackSound, attackVolume);
                WriteAiDebugLog("AttackApplied", canSeePlayer);
            }
        }

        private void UpdateRetreat(bool canSeePlayer)
        {
            ChangeState(canSeePlayer ? BotState.Chase : BotState.Patrol);
        }

        private void ChangeState(BotState nextState)
        {
            if (currentState == nextState)
            {
                return;
            }

            currentState = nextState;
            WriteAiDebugLog("StateChanged", false);

            switch (currentState)
            {
                case BotState.Patrol:
                    SetAgentStopped(false);
                    patrolWaitUntil = 0f;
                    nextPatrolAttemptTime = 0f;
                    patrolRecoveryReadyTime = 0f;
                    hasPatrolDestination = false;
                    break;

                case BotState.Detect:
                    SetAgentStopped(true);
                    break;

                case BotState.Chase:
                    SetAgentStopped(false);
                    break;

                case BotState.Attack:
                    SetAgentStopped(true);
                    ResetAgentPath();
                    hasPatrolDestination = false;
                    break;

                case BotState.Retreat:
                    SetAgentStopped(false);
                    break;

                case BotState.Dead:
                    SetAgentStopped(true);
                    ResetAgentPath();
                    hasPatrolDestination = false;
                    break;
            }
        }

        private void TrySetNextPatrolDestination()
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                hasPatrolDestination = false;
                return;
            }

            int startIndex = lastPatrolIndex;
            int checkedPoints = 0;

            while (checkedPoints < patrolPoints.Length)
            {
                patrolIndex = GetNextPatrolIndex(startIndex, checkedPoints);
                Transform patrolPoint = patrolPoints[patrolIndex];
                checkedPoints++;

                if (patrolPoint == null)
                {
                    continue;
                }

                Vector3 destination = GetSpreadPatrolDestination(patrolPoint.position);

                if (!SetPatrolDestination(destination))
                {
                    continue;
                }

                currentPatrolDestination = destination;
                patrolDestinationSetTime = Time.time;
                lastPatrolIndex = patrolIndex;
                hasPatrolDestination = true;
                patrolWaitUntil = 0f;
                nextPatrolAttemptTime = 0f;
                patrolRecoveryReadyTime = 0f;
                WriteAiDebugLog("PatrolDestinationSet", CanSeePlayer());
                return;
            }

            HandleNoValidPatrolDestination();
        }

        private int GetNextPatrolIndex(int startIndex, int checkedPoints)
        {
            if (randomPatrol)
            {
                if (patrolPoints.Length == 1)
                {
                    return 0;
                }

                for (int attempt = 0; attempt < patrolPoints.Length * 2; attempt++)
                {
                    int candidateIndex = UnityEngine.Random.Range(0, patrolPoints.Length);

                    if (candidateIndex != lastPatrolIndex)
                    {
                        return candidateIndex;
                    }
                }

                return (lastPatrolIndex + 1) % patrolPoints.Length;
            }

            return (startIndex + checkedPoints + 1 + patrolPoints.Length) % patrolPoints.Length;
        }

        private void HandleNoValidPatrolDestination()
        {
            hasPatrolDestination = false;
            nextPatrolAttemptTime = Time.time + Mathf.Max(0.05f, patrolRetryInterval);

            if (patrolRecoveryReadyTime <= 0f)
            {
                patrolRecoveryReadyTime = Time.time + Mathf.Max(0f, patrolRecoveryDelay);
            }

            ResetAgentPath();
            WriteAiDebugLog("NoValidPatrolDestination", CanSeePlayer());

            if (Time.time < patrolRecoveryReadyTime)
            {
                return;
            }

            if (TryWarpToPatrolPoint())
            {
                patrolRecoveryReadyTime = 0f;
                nextPatrolAttemptTime = Time.time + Mathf.Max(0.05f, patrolRetryInterval);
                return;
            }

            patrolRecoveryReadyTime = Time.time + Mathf.Max(0.5f, patrolRecoveryDelay);
            WriteAiDebugLog("PatrolRecoveryFailed", CanSeePlayer());
        }

        private bool TryWarpToPatrolPoint()
        {
            if (patrolPoints == null || patrolPoints.Length == 0 || agent == null)
            {
                return false;
            }

            int attempts = Mathf.Max(4, patrolPoints.Length * 2);

            for (int i = 0; i < attempts; i++)
            {
                int candidateIndex = randomPatrol
                    ? UnityEngine.Random.Range(0, patrolPoints.Length)
                    : (lastPatrolIndex + i + 1 + patrolPoints.Length) % patrolPoints.Length;

                Transform patrolPoint = patrolPoints[candidateIndex];

                if (patrolPoint == null)
                {
                    continue;
                }

                if (!NavMesh.SamplePosition(patrolPoint.position, out NavMeshHit hit, patrolRecoverySampleRadius, agent.areaMask))
                {
                    continue;
                }

                if (!agent.Warp(hit.position))
                {
                    continue;
                }

                patrolIndex = candidateIndex;
                lastPatrolIndex = candidateIndex;
                currentPatrolDestination = hit.position;
                patrolDestinationSetTime = Time.time;
                patrolWaitUntil = Time.time + patrolWaitTime;
                hasPatrolDestination = true;
                WriteAiDebugLog("PatrolRecoveryWarp", CanSeePlayer());
                return true;
            }

            return false;
        }

        private Vector3 GetSpreadPatrolDestination(Vector3 patrolPointPosition)
        {
            if (patrolDestinationSpread <= 0f)
            {
                return patrolPointPosition;
            }

            Vector2 offset = UnityEngine.Random.insideUnitCircle * patrolDestinationSpread;
            Vector3 candidate = patrolPointPosition + new Vector3(offset.x, 0f, offset.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, patrolDestinationSpread + 0.5f, agent.areaMask))
            {
                return hit.position;
            }

            return patrolPointPosition;
        }

        private bool ShouldRetryPatrolDestination()
        {
            if (patrolStuckRetryTime <= 0f || Time.time - patrolDestinationSetTime < patrolStuckRetryTime)
            {
                return false;
            }

            float directDistance = Vector3.Distance(transform.position, currentPatrolDestination);

            return agent.pathStatus != NavMeshPathStatus.PathComplete
                || directDistance > patrolReachDistance && agent.velocity.sqrMagnitude < 0.04f;
        }

        private bool CanSeePlayer()
        {
            if (player == null || playerDamageable != null && playerDamageable.IsDead)
            {
                return false;
            }

            Vector3 origin = eyePoint.position;
            Vector3 target = GetPlayerAimPoint();
            Vector3 toPlayer = target - origin;
            float distance = toPlayer.magnitude;

            if (distance > detectionRange || distance <= 0.01f)
            {
                return false;
            }

            float angle = Vector3.Angle(transform.forward, toPlayer.normalized);

            if (angle > fieldOfViewAngle * 0.5f)
            {
                return false;
            }

            if (!Physics.Raycast(origin, toPlayer.normalized, out RaycastHit hit, distance, lineOfSightLayers, QueryTriggerInteraction.Ignore))
            {
                return true;
            }

            return hit.collider.transform == player || hit.collider.transform.IsChildOf(player);
        }

        private bool IsPlayerInAttackRange()
        {
            if (player == null)
            {
                return false;
            }

            Vector3 toPlayer = player.position - transform.position;
            float heightDifference = Mathf.Abs(toPlayer.y);
            toPlayer.y = 0f;

            return heightDifference <= maxAttackHeightDifference && toPlayer.magnitude <= attackRange;
        }

        private bool ShouldForgetPlayer(bool canSeePlayer)
        {
            return !canSeePlayer && Time.time - lastSawPlayerTime > loseSightDelay;
        }

        private Vector3 GetPlayerAimPoint()
        {
            return player.position + Vector3.up * 1.1f;
        }

        private void FacePlayer()
        {
            if (player == null)
            {
                return;
            }

            Vector3 direction = player.position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        private void HandleDeath()
        {
            WriteAiDebugLog("Death", CanSeePlayer());
            ChangeState(BotState.Dead);

            if (!deathRegistered)
            {
                deathRegistered = true;
                GameManager.Instance?.RegisterBotDeath(this);
            }

            PlaySound(deathSound, deathVolume);

            if (destroyAfterDeath)
            {
                Destroy(gameObject, deathDestroyDelay);
            }
        }

        private bool SetAgentDestination(Vector3 destination)
        {
            if (!agent.isOnNavMesh)
            {
                WriteAiDebugLog("DestinationFailedOffNavMesh", CanSeePlayer());
                return false;
            }

            if (NavMesh.SamplePosition(destination, out NavMeshHit hit, navMeshDestinationSampleRadius, agent.areaMask))
            {
                bool result = agent.SetDestination(hit.position);
                WriteAiDebugLogThrottled(result ? "DestinationSetSampled" : "DestinationFailedSampled", CanSeePlayer());
                return result;
            }

            bool fallbackResult = agent.SetDestination(destination);
            WriteAiDebugLogThrottled(fallbackResult ? "DestinationSetRaw" : "DestinationFailedRaw", CanSeePlayer());
            return fallbackResult;
        }

        private bool SetPatrolDestination(Vector3 destination)
        {
            return SetAgentDestination(destination);
        }

        private void SetAgentStopped(bool stopped)
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = stopped;
            }
        }

        private void ResetAgentPath()
        {
            if (agent.isOnNavMesh)
            {
                agent.ResetPath();
                WriteAiDebugLog("ResetPath", CanSeePlayer());
            }
        }

        private void PlaySound(AudioClip clip, float volume)
        {
            if (clip == null)
            {
                return;
            }

            if (audioSource != null)
            {
                audioSource.PlayOneShot(clip, volume);
                return;
            }

            AudioSource.PlayClipAtPoint(clip, transform.position, volume);
        }

        private void WriteAiDebugLogThrottled(string eventName, bool canSeePlayer)
        {
            if (!writeDebugLog || Time.time < nextDebugLogTime)
            {
                return;
            }

            nextDebugLogTime = Time.time + Mathf.Max(0.05f, debugLogInterval);
            WriteAiDebugLog(eventName, canSeePlayer);
        }

        private void WriteAiDebugLog(string eventName, bool canSeePlayer)
        {
            if (!writeDebugLog)
            {
                return;
            }

            try
            {
                EnsureAiDebugLogPath();
                File.AppendAllText(debugLogPath, BuildAiDebugLogRow(eventName, canSeePlayer));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Bot AI debug logging failed: {exception.Message}");
            }
        }

        private static void EnsureAiDebugLogPath()
        {
            if (!string.IsNullOrEmpty(debugLogPath))
            {
                return;
            }

            string fileName = $"TermProject_AI_Debug_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            debugLogPath = Path.Combine(Application.persistentDataPath, fileName);

            string header = "time,frame,botId,botName,event,state,posX,posY,posZ,onNavMesh,isStopped,hasPath,pathPending,pathStatus,remainingDistance,velocity,hasPatrolDestination,patrolIndex,lastPatrolIndex,patrolDestX,patrolDestY,patrolDestZ,patrolWaitRemaining,canSeePlayer,playerDistanceXZ,playerHeightDifference,inAttackRange,destinationX,destinationY,destinationZ\n";
            File.WriteAllText(debugLogPath, header);

            if (!debugLogPathAnnounced)
            {
                debugLogPathAnnounced = true;
                Debug.Log($"Bot AI debug logging started: {debugLogPath}");
            }
        }

        private string BuildAiDebugLogRow(string eventName, bool canSeePlayer)
        {
            CultureInfo culture = CultureInfo.InvariantCulture;
            Vector3 position = transform.position;
            Vector3 destination = agent != null && agent.isOnNavMesh ? agent.destination : Vector3.zero;
            Vector3 velocity = agent != null ? agent.velocity : Vector3.zero;

            bool onNavMesh = agent != null && agent.isOnNavMesh;
            bool isStopped = onNavMesh && agent.isStopped;
            bool hasPath = onNavMesh && agent.hasPath;
            bool pathPending = onNavMesh && agent.pathPending;
            string pathStatus = onNavMesh ? agent.pathStatus.ToString() : "OffNavMesh";
            float remainingDistance = onNavMesh ? agent.remainingDistance : -1f;
            float patrolWaitRemaining = Mathf.Max(0f, patrolWaitUntil - Time.time);
            float playerDistanceXZ = -1f;
            float playerHeightDifference = 0f;
            bool inAttackRange = false;

            if (player != null)
            {
                Vector3 toPlayer = player.position - position;
                playerHeightDifference = Mathf.Abs(toPlayer.y);
                toPlayer.y = 0f;
                playerDistanceXZ = toPlayer.magnitude;
                inAttackRange = playerHeightDifference <= maxAttackHeightDifference && playerDistanceXZ <= attackRange;
            }

            return string.Join(",",
                Time.time.ToString("F3", culture),
                Time.frameCount.ToString(culture),
                debugId.ToString(culture),
                EscapeCsv(name),
                EscapeCsv(eventName),
                currentState.ToString(),
                position.x.ToString("F3", culture),
                position.y.ToString("F3", culture),
                position.z.ToString("F3", culture),
                BoolToCsv(onNavMesh),
                BoolToCsv(isStopped),
                BoolToCsv(hasPath),
                BoolToCsv(pathPending),
                pathStatus,
                remainingDistance.ToString("F3", culture),
                velocity.magnitude.ToString("F3", culture),
                BoolToCsv(hasPatrolDestination),
                patrolIndex.ToString(culture),
                lastPatrolIndex.ToString(culture),
                currentPatrolDestination.x.ToString("F3", culture),
                currentPatrolDestination.y.ToString("F3", culture),
                currentPatrolDestination.z.ToString("F3", culture),
                patrolWaitRemaining.ToString("F3", culture),
                BoolToCsv(canSeePlayer),
                playerDistanceXZ.ToString("F3", culture),
                playerHeightDifference.ToString("F3", culture),
                BoolToCsv(inAttackRange),
                destination.x.ToString("F3", culture),
                destination.y.ToString("F3", culture),
                destination.z.ToString("F3", culture)) + "\n";
        }

        private static string BoolToCsv(bool value)
        {
            return value ? "1" : "0";
        }

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
