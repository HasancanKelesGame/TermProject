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
        [SerializeField] private int maxBotCount = 8;

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

        [Header("Debug")]
        [SerializeField] private int currentWave;
        [SerializeField] private int activeBotCount;
        [SerializeField] private bool spawningWave;

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

            spawningWave = false;
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
            spawningWave = true;
            gameManager?.SetWave(wave);

            int botCount = GetBotCountForWave(wave);

            for (int i = 0; i < botCount; i++)
            {
                SpawnBot(wave);
                RefreshActiveBotCount();

                if (spawnInterval > 0f && i < botCount - 1)
                {
                    yield return new WaitForSeconds(spawnInterval);
                }
            }

            spawningWave = false;
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

        private int GetBotCountForWave(int wave)
        {
            int waveIndex = Mathf.Max(0, wave - 1);
            int count = baseBotCount + waveIndex * botsAddedPerWave;
            return Mathf.Clamp(count, 1, Mathf.Max(1, maxBotCount));
        }

        private float GetBotHealthForWave(int wave)
        {
            int waveIndex = Mathf.Max(0, wave - 1);
            return Mathf.Max(1f, baseBotHealth + waveIndex * healthAddedPerWave);
        }

        private float GetBotDamageForWave(int wave)
        {
            int waveIndex = Mathf.Max(0, wave - 1);
            return Mathf.Max(0f, baseBotDamage + waveIndex * damageAddedPerWave);
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
            return Mathf.Max(minAttackCooldown, cooldown);
        }

        private bool HasRequiredSetup()
        {
            if (botPrefab == null)
            {
                Debug.LogWarning("WaveManager needs a bot prefab before waves can start.");
                return false;
            }

            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogWarning("WaveManager needs at least one spawn point before waves can start.");
                return false;
            }

            return true;
        }

        private void RefreshActiveBotCount()
        {
            RemoveDeadOrMissingBots();
            activeBotCount = activeBots.Count;
            gameManager?.SetActiveBotCount(activeBotCount);
        }
    }
}
