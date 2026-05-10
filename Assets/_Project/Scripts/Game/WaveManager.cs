using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using TermProject.AI;
using TermProject.Player;

namespace TermProject.Game
{
    [DisallowMultipleComponent]
    public sealed class WaveManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private BotAI botPrefab;
        [SerializeField] private Transform player;
        [SerializeField] private Damageable playerDamageable;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private Transform[] patrolPoints;

        [Header("Timing")]
        [SerializeField] private bool autoStartWaves = true;
        [SerializeField] private float firstWaveDelay = 1f;
        [SerializeField] private float spawnInterval = 0.75f;
        [SerializeField] private float nextWaveDelay = 4f;

        [Header("Wave Size")]
        [SerializeField] private int startingWave = 1;
        [SerializeField] private int baseBotCount = 2;
        [SerializeField] private int botsAddedPerWave = 1;
        [SerializeField] private int maxBotCount = 12;

        [Header("Bot Scaling")]
        [SerializeField] private float baseBotHealth = 100f;
        [SerializeField] private float healthAddedPerWave = 20f;
        [SerializeField] private float baseBotDamage = 10f;
        [SerializeField] private float damageAddedPerWave = 1.5f;
        [SerializeField] private float baseBotMoveSpeed = 3.5f;
        [SerializeField] private float moveSpeedAddedPerWave = 0.15f;
        [SerializeField] private float baseAttackCooldown = 1f;
        [SerializeField] private float attackCooldownReductionPerWave = 0.03f;
        [SerializeField] private float minAttackCooldown = 0.45f;

        [Header("Spawn Rules")]
        [SerializeField] private float navMeshSampleRadius = 2f;
        [SerializeField] private float minSpawnDistanceFromPlayer = 6f;
        [SerializeField] private bool avoidSpawningInPlayerView = true;
        [SerializeField] private float playerViewAvoidanceDistance = 14f;
        [SerializeField] private float playerViewAvoidanceAngle = 90f;

        [Header("Crazy Mode")]
        [SerializeField] private bool useCrazyModeOverrides = true;
        [SerializeField] private Transform crazySpawnCenter;
        [SerializeField] private Vector2 crazySpawnAreaSize = new Vector2(34f, 34f);
        [SerializeField] private int crazyBaseBotCount = 5;
        [SerializeField] private int crazyBotsAddedPerWave = 2;
        [SerializeField] private int crazyMaxBotCount = 22;
        [SerializeField, Min(0f)] private float crazySpawnInterval = 0.35f;
        [SerializeField] private float crazyMinSpawnDistanceFromPlayer = 7f;
        [SerializeField] private float crazyNavMeshSampleRadius = 4f;
        [SerializeField] private int crazySpawnAttempts = 48;
        [SerializeField, Range(0.1f, 2f)] private float crazyDamageMultiplier = 0.75f;
        [SerializeField, Range(0.5f, 3f)] private float crazyAttackCooldownMultiplier = 1.25f;

        [Header("Debug")]
        [SerializeField] private int currentWave;
        [SerializeField] private int activeBotCount;

        private readonly List<BotAI> activeBots = new List<BotAI>();
        private Coroutine waveRoutine;

        public int CurrentWave => currentWave;
        public int ActiveBotCount => activeBotCount;
        public bool IsRunning => waveRoutine != null;

        private void Awake()
        {
            if (gameManager == null)
            {
                gameManager = GameManager.Instance ?? FindFirstObjectByType<GameManager>();
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

            currentWave = Mathf.Max(1, startingWave);
        }

        private void Start()
        {
            if (autoStartWaves)
            {
                StartWaves();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!useCrazyModeOverrides)
            {
                return;
            }

            Vector3 center = crazySpawnCenter != null ? crazySpawnCenter.position : transform.position;
            Vector3 halfX = Vector3.right * Mathf.Max(1f, crazySpawnAreaSize.x) * 0.5f;
            Vector3 halfZ = Vector3.forward * Mathf.Max(1f, crazySpawnAreaSize.y) * 0.5f;

            Gizmos.color = new Color(1f, 0.35f, 0.05f, 0.85f);
            Gizmos.DrawLine(center - halfX - halfZ, center + halfX - halfZ);
            Gizmos.DrawLine(center + halfX - halfZ, center + halfX + halfZ);
            Gizmos.DrawLine(center + halfX + halfZ, center - halfX + halfZ);
            Gizmos.DrawLine(center - halfX + halfZ, center - halfX - halfZ);
        }

        public void StartWaves()
        {
            if (waveRoutine != null)
            {
                return;
            }

            waveRoutine = StartCoroutine(RunWaveLoop());
        }

        public void StopWaves()
        {
            if (waveRoutine != null)
            {
                StopCoroutine(waveRoutine);
                waveRoutine = null;
            }

            RefreshActiveBotCount();
        }

        public void ClearActiveBots()
        {
            for (int i = activeBots.Count - 1; i >= 0; i--)
            {
                BotAI bot = activeBots[i];

                if (bot != null)
                {
                    Damageable damageable = bot.GetComponent<Damageable>();

                    if (damageable != null)
                    {
                        damageable.Died.RemoveListener(HandleTrackedBotDied);
                    }

                    Destroy(bot.gameObject);
                }
            }

            activeBots.Clear();
            RefreshActiveBotCount();
        }

        private IEnumerator RunWaveLoop()
        {
            if (!HasRequiredSetup())
            {
                waveRoutine = null;
                yield break;
            }

            currentWave = Mathf.Max(1, startingWave);

            if (firstWaveDelay > 0f)
            {
                yield return new WaitForSeconds(firstWaveDelay);
            }

            while (enabled)
            {
                yield return SpawnWave(currentWave);

                while (activeBots.Count > 0)
                {
                    RemoveDeadOrMissingBots();
                    yield return null;
                }

                currentWave++;

                if (nextWaveDelay > 0f)
                {
                    yield return new WaitForSeconds(nextWaveDelay);
                }
            }

            waveRoutine = null;
        }

        private IEnumerator SpawnWave(int wave)
        {
            gameManager?.SetWave(wave);

            int botCount = GetBotCountForWave(wave);
            float currentSpawnInterval = GetSpawnInterval();

            for (int i = 0; i < botCount; i++)
            {
                SpawnBot(wave);
                RefreshActiveBotCount();

                if (currentSpawnInterval > 0f && i < botCount - 1)
                {
                    yield return new WaitForSeconds(currentSpawnInterval);
                }
            }

            RefreshActiveBotCount();
        }

        private void SpawnBot(int wave)
        {
            if (!TryGetSpawnPosition(out Vector3 spawnPosition))
            {
                Debug.LogWarning("WaveManager could not find a valid NavMesh spawn position.");
                return;
            }

            BotAI bot = Instantiate(botPrefab, spawnPosition, Quaternion.identity);
            Damageable botDamageable = bot.GetComponent<Damageable>();

            if (botDamageable != null)
            {
                botDamageable.SetMaxHealth(GetBotHealthForWave(wave), true);
                botDamageable.Died.AddListener(HandleTrackedBotDied);
            }

            bot.ConfigureForWave(
                player,
                playerDamageable,
                patrolPoints,
                GetBotDamageForWave(wave),
                GetBotAttackCooldownForWave(wave),
                GetBotMoveSpeedForWave(wave));

            activeBots.Add(bot);
        }

        private void HandleTrackedBotDied()
        {
            RemoveDeadOrMissingBots();
            RefreshActiveBotCount();
        }

        private void RemoveDeadOrMissingBots()
        {
            for (int i = activeBots.Count - 1; i >= 0; i--)
            {
                BotAI bot = activeBots[i];

                if (bot == null)
                {
                    activeBots.RemoveAt(i);
                    continue;
                }

                Damageable damageable = bot.GetComponent<Damageable>();

                if (damageable != null && damageable.IsDead)
                {
                    damageable.Died.RemoveListener(HandleTrackedBotDied);
                    activeBots.RemoveAt(i);
                }
            }
        }

        private bool TryGetSpawnPosition(out Vector3 spawnPosition)
        {
            spawnPosition = Vector3.zero;

            if (IsCrazyModeActive() && TryGetCrazySpawnPosition(out spawnPosition))
            {
                return true;
            }

            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                return false;
            }

            int attempts = Mathf.Max(8, spawnPoints.Length * 4);

            for (int i = 0; i < attempts; i++)
            {
                Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

                if (spawnPoint == null || !IsSpawnAllowedByPlayer(spawnPoint.position))
                {
                    continue;
                }

                if (NavMesh.SamplePosition(spawnPoint.position, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
                {
                    spawnPosition = hit.position;
                    return true;
                }
            }

            for (int i = 0; i < spawnPoints.Length; i++)
            {
                Transform spawnPoint = spawnPoints[i];

                if (spawnPoint == null)
                {
                    continue;
                }

                if (NavMesh.SamplePosition(spawnPoint.position, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
                {
                    spawnPosition = hit.position;
                    return true;
                }
            }

            return false;
        }

        private bool IsSpawnAllowedByPlayer(Vector3 candidatePosition)
        {
            if (player == null)
            {
                return true;
            }

            Vector3 toSpawn = candidatePosition - player.position;
            toSpawn.y = 0f;
            float distance = toSpawn.magnitude;

            if (distance < minSpawnDistanceFromPlayer)
            {
                return false;
            }

            if (!avoidSpawningInPlayerView || distance > playerViewAvoidanceDistance || distance <= 0.01f)
            {
                return true;
            }

            Vector3 playerForward = player.forward;
            playerForward.y = 0f;

            if (playerForward.sqrMagnitude <= 0.001f)
            {
                return true;
            }

            float angle = Vector3.Angle(playerForward.normalized, toSpawn.normalized);
            return angle > playerViewAvoidanceAngle * 0.5f;
        }

        private bool TryGetCrazySpawnPosition(out Vector3 spawnPosition)
        {
            spawnPosition = Vector3.zero;

            Vector3 center = crazySpawnCenter != null ? crazySpawnCenter.position : transform.position;
            Vector2 areaSize = new Vector2(
                Mathf.Max(1f, crazySpawnAreaSize.x),
                Mathf.Max(1f, crazySpawnAreaSize.y));
            int attempts = Mathf.Max(1, crazySpawnAttempts);

            for (int i = 0; i < attempts; i++)
            {
                Vector3 candidate = center + new Vector3(
                    Random.Range(-areaSize.x * 0.5f, areaSize.x * 0.5f),
                    0f,
                    Random.Range(-areaSize.y * 0.5f, areaSize.y * 0.5f));

                if (!IsCrazySpawnFarEnoughFromPlayer(candidate))
                {
                    continue;
                }

                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, crazyNavMeshSampleRadius, NavMesh.AllAreas)
                    && IsCrazySpawnFarEnoughFromPlayer(hit.position))
                {
                    spawnPosition = hit.position;
                    return true;
                }
            }

            return false;
        }

        private bool IsCrazySpawnFarEnoughFromPlayer(Vector3 candidatePosition)
        {
            if (player == null)
            {
                return true;
            }

            Vector3 toSpawn = candidatePosition - player.position;
            toSpawn.y = 0f;
            return toSpawn.magnitude >= Mathf.Max(0f, crazyMinSpawnDistanceFromPlayer);
        }

        private int GetBotCountForWave(int wave)
        {
            int waveIndex = Mathf.Max(0, wave - 1);

            if (IsCrazyModeActive())
            {
                int crazyCount = crazyBaseBotCount + waveIndex * crazyBotsAddedPerWave;
                return Mathf.Clamp(crazyCount, 1, Mathf.Max(1, crazyMaxBotCount));
            }

            int count = baseBotCount + waveIndex * botsAddedPerWave;
            return Mathf.Clamp(count, 1, Mathf.Max(1, maxBotCount));
        }

        private float GetSpawnInterval()
        {
            return IsCrazyModeActive() ? Mathf.Max(0f, crazySpawnInterval) : Mathf.Max(0f, spawnInterval);
        }

        private float GetBotHealthForWave(int wave)
        {
            int waveIndex = Mathf.Max(0, wave - 1);
            return Mathf.Max(1f, baseBotHealth + waveIndex * healthAddedPerWave);
        }

        private float GetBotDamageForWave(int wave)
        {
            int waveIndex = Mathf.Max(0, wave - 1);
            float damage = baseBotDamage + waveIndex * damageAddedPerWave;

            if (IsCrazyModeActive())
            {
                damage *= crazyDamageMultiplier;
            }

            return Mathf.Max(0f, damage);
        }

        private float GetBotMoveSpeedForWave(int wave)
        {
            int waveIndex = Mathf.Max(0, wave - 1);
            return Mathf.Max(0.1f, baseBotMoveSpeed + waveIndex * moveSpeedAddedPerWave);
        }

        private float GetBotAttackCooldownForWave(int wave)
        {
            int waveIndex = Mathf.Max(0, wave - 1);
            float cooldown = baseAttackCooldown - waveIndex * attackCooldownReductionPerWave;

            if (IsCrazyModeActive())
            {
                cooldown *= crazyAttackCooldownMultiplier;
            }

            return Mathf.Max(minAttackCooldown, cooldown);
        }

        private bool HasRequiredSetup()
        {
            if (botPrefab == null)
            {
                Debug.LogWarning("WaveManager needs a bot prefab before waves can start.");
                return false;
            }

            if (!IsCrazyModeActive() && (spawnPoints == null || spawnPoints.Length == 0))
            {
                Debug.LogWarning("WaveManager needs at least one spawn point before waves can start.");
                return false;
            }

            return true;
        }

        private bool IsCrazyModeActive()
        {
            return useCrazyModeOverrides && gameManager != null && gameManager.CrazyModeEnabled;
        }

        private void RefreshActiveBotCount()
        {
            RemoveDeadOrMissingBots();
            activeBotCount = activeBots.Count;
            gameManager?.SetActiveBotCount(activeBotCount);
        }
    }
}
