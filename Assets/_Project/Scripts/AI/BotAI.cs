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
        [SerializeField] private float patrolWaitTime = 1.5f;
        [SerializeField] private float stoppingDistance = 1.6f;
        [SerializeField] private float turnSpeed = 9f;

        [Header("Attack")]
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private float attackCooldown = 1f;

        [Header("Death")]
        [SerializeField] private bool destroyAfterDeath = true;
        [SerializeField] private float deathDestroyDelay = 2f;

        [Header("Debug")]
        [SerializeField] private BotState currentState = BotState.Patrol;

        private NavMeshAgent agent;
        private Damageable damageable;
        private float lastSawPlayerTime = -999f;
        private float nextAttackTime;
        private float patrolWaitUntil;
        private int patrolIndex;
        private bool initialized;

        public BotState CurrentState => currentState;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            damageable = GetComponent<Damageable>();

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
        }

        private void Update()
        {
            if (!initialized || currentState == BotState.Dead || !agent.isOnNavMesh)
            {
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
                return;
            }

            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                if (Time.time < patrolWaitUntil)
                {
                    return;
                }

                patrolIndex = Random.Range(0, patrolPoints.Length);
                Transform patrolPoint = patrolPoints[patrolIndex];

                if (patrolPoint != null)
                {
                    SetAgentDestination(patrolPoint.position);
                    patrolWaitUntil = Time.time + patrolWaitTime;
                }
            }
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

            switch (currentState)
            {
                case BotState.Patrol:
                    SetAgentStopped(false);
                    patrolWaitUntil = 0f;
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
                    break;

                case BotState.Retreat:
                    SetAgentStopped(false);
                    break;

                case BotState.Dead:
                    SetAgentStopped(true);
                    ResetAgentPath();
                    break;
            }
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

            return Vector3.Distance(transform.position, player.position) <= attackRange;
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
            ChangeState(BotState.Dead);

            if (destroyAfterDeath)
            {
                Destroy(gameObject, deathDestroyDelay);
            }
        }

        private void SetAgentDestination(Vector3 destination)
        {
            if (agent.isOnNavMesh)
            {
                agent.SetDestination(destination);
            }
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
            }
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
