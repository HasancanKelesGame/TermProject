using UnityEngine;
using TermProject.AI;
using TermProject.UI;

namespace TermProject.Game
{
    [DisallowMultipleComponent]
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private HudController hudController;

        [Header("Run State")]
        [SerializeField] private int startingWave = 1;

        [Header("Scoring")]
        [SerializeField] private int scorePerKill = 100;

        private int currentWave;
        private int totalKills;
        private int score;

        public int CurrentWave => currentWave;
        public int TotalKills => totalKills;
        public int Score => score;

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

            currentWave = Mathf.Max(1, startingWave);
        }

        private void Start()
        {
            RefreshHud();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void RegisterBotDeath(BotAI bot)
        {
            totalKills++;
            score += Mathf.Max(0, scorePerKill);
            RefreshHud();
        }

        public void SetWave(int wave)
        {
            currentWave = Mathf.Max(1, wave);
            RefreshHud();
        }

        public void ResetRun()
        {
            currentWave = Mathf.Max(1, startingWave);
            totalKills = 0;
            score = 0;
            RefreshHud();
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
