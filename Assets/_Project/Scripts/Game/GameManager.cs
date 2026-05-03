using UnityEngine;
using UnityEngine.SceneManagement;
using TermProject.AI;
using TermProject.Player;
using TermProject.UI;
using TermProject.Weapons;

namespace TermProject.Game
{
    [DisallowMultipleComponent]
    public sealed class GameManager : MonoBehaviour
    {
        public enum GameState
        {
            Playing,
            GameOver
        }

        public static GameManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private HudController hudController;
        [SerializeField] private GameOverMenuController gameOverMenu;
        [SerializeField] private WaveManager waveManager;
        [SerializeField] private Damageable playerHealth;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private WeaponController weaponController;

        [Header("Run State")]
        [SerializeField] private int startingWave = 1;

        [Header("Scoring")]
        [SerializeField] private int scorePerKill = 100;

        private int currentWave;
        private int activeBots;
        private int totalKills;
        private int score;
        private GameState currentState = GameState.Playing;
        private bool subscribedToPlayerDeath;

        public int CurrentWave => currentWave;
        public int ActiveBots => activeBots;
        public int TotalKills => totalKills;
        public int Score => score;
        public GameState CurrentState => currentState;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (hudController == null)
            {
                hudController = FindFirstObjectByType<HudController>();
            }

            if (gameOverMenu == null)
            {
                gameOverMenu = FindFirstObjectByType<GameOverMenuController>(FindObjectsInactive.Include);
            }

            if (waveManager == null)
            {
                waveManager = FindFirstObjectByType<WaveManager>();
            }

            if (playerController == null)
            {
                playerController = FindFirstObjectByType<PlayerController>();
            }

            if (weaponController == null)
            {
                weaponController = FindFirstObjectByType<WeaponController>();
            }

            if (playerHealth == null && playerController != null)
            {
                playerHealth = playerController.GetComponent<Damageable>();
            }

            gameOverMenu?.SetGameManager(this);
            currentWave = Mathf.Max(1, startingWave);
            Time.timeScale = 1f;
        }

        private void Start()
        {
            currentState = GameState.Playing;
            SubscribeToPlayerDeath();
            gameOverMenu?.Hide();
            playerController?.SetControlsEnabled(true);
            weaponController?.SetInputEnabled(true);
            RefreshHud();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            UnsubscribeFromPlayerDeath();
        }

        public void RegisterBotDeath(BotAI bot)
        {
            if (currentState == GameState.GameOver)
            {
                return;
            }

            totalKills++;
            score += Mathf.Max(0, scorePerKill);
            RefreshHud();
        }

        public void SetWave(int wave)
        {
            currentWave = Mathf.Max(1, wave);
            RefreshHud();
        }

        public void SetActiveBotCount(int count)
        {
            activeBots = Mathf.Max(0, count);
        }

        public void ResetRun()
        {
            currentState = GameState.Playing;
            currentWave = Mathf.Max(1, startingWave);
            activeBots = 0;
            totalKills = 0;
            score = 0;
            Time.timeScale = 1f;
            gameOverMenu?.Hide();
            playerHealth?.ResetHealth();
            playerController?.SetControlsEnabled(true);
            playerController?.SetCursorLocked(true);
            weaponController?.SetInputEnabled(true);
            RefreshHud();
        }

        public void RestartCurrentScene()
        {
            Time.timeScale = 1f;
            Scene activeScene = SceneManager.GetActiveScene();

            if (activeScene.buildIndex >= 0)
            {
                SceneManager.LoadScene(activeScene.buildIndex);
            }
            else
            {
                SceneManager.LoadScene(activeScene.name);
            }
        }

        private void HandlePlayerDied()
        {
            if (currentState == GameState.GameOver)
            {
                return;
            }

            currentState = GameState.GameOver;
            waveManager?.StopWaves();
            playerController?.SetControlsEnabled(false);
            playerController?.SetCursorLocked(false);
            weaponController?.SetInputEnabled(false);
            gameOverMenu?.Show(currentWave, totalKills, score);
            Time.timeScale = 0f;
        }

        private void SubscribeToPlayerDeath()
        {
            if (subscribedToPlayerDeath || playerHealth == null)
            {
                return;
            }

            playerHealth.Died.AddListener(HandlePlayerDied);
            subscribedToPlayerDeath = true;
        }

        private void UnsubscribeFromPlayerDeath()
        {
            if (!subscribedToPlayerDeath || playerHealth == null)
            {
                return;
            }

            playerHealth.Died.RemoveListener(HandlePlayerDied);
            subscribedToPlayerDeath = false;
        }

        private void RefreshHud()
        {
            if (hudController == null)
            {
                return;
            }

            hudController.SetWave(currentWave);
            hudController.SetKills(totalKills);
            hudController.SetScore(score);
        }
    }
}
