using TMPro;
using UnityEngine;
using TermProject.Game;

namespace TermProject.UI
{
    [DisallowMultipleComponent]
    public sealed class HudController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Damageable playerHealth;
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private TMP_Text ammoText;
        [SerializeField] private TMP_Text waveText;
        [SerializeField] private TMP_Text killsText;
        [SerializeField] private TMP_Text scoreText;

        [Header("Starting Values")]
        [SerializeField] private int startingWave = 1;
        [SerializeField] private int startingKills;
        [SerializeField] private int startingScore;

        private int currentAmmo = -1;
        private int reserveAmmo = -1;
        private int currentWave;
        private int kills;
        private int score;

        private void Awake()
        {
            currentWave = Mathf.Max(1, startingWave);
            kills = Mathf.Max(0, startingKills);
            score = Mathf.Max(0, startingScore);
        }

        private void OnEnable()
        {
            SubscribeToHealth();
            RefreshAll();
        }

        private void OnDisable()
        {
            UnsubscribeFromHealth();
        }

        public void SetPlayerHealth(Damageable damageable)
        {
            if (playerHealth == damageable)
            {
                return;
            }

            UnsubscribeFromHealth();
            playerHealth = damageable;
            SubscribeToHealth();
            RefreshHealth();
        }

        public void SetAmmo(int current, int reserve)
        {
            currentAmmo = current;
            reserveAmmo = reserve;
            RefreshAmmo();
        }

        public void SetWave(int wave)
        {
            currentWave = Mathf.Max(1, wave);
            RefreshWave();
        }

        public void SetKills(int value)
        {
            kills = Mathf.Max(0, value);
            RefreshKills();
        }

        public void SetScore(int value)
        {
            score = Mathf.Max(0, value);
            RefreshScore();
        }

        private void SubscribeToHealth()
        {
            if (playerHealth != null && playerHealth.HealthChanged != null)
            {
                playerHealth.HealthChanged.AddListener(UpdateHealthText);
            }
        }

        private void UnsubscribeFromHealth()
        {
            if (playerHealth != null && playerHealth.HealthChanged != null)
            {
                playerHealth.HealthChanged.RemoveListener(UpdateHealthText);
            }
        }

        private void RefreshAll()
        {
            RefreshHealth();
            RefreshAmmo();
            RefreshWave();
            RefreshKills();
            RefreshScore();
        }

        private void RefreshHealth()
        {
            if (playerHealth == null)
            {
                SetText(healthText, "HEALTH --/--");
                return;
            }

            UpdateHealthText(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }

        private void UpdateHealthText(float current, float max)
        {
            SetText(healthText, $"HEALTH {Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}");
        }

        private void RefreshAmmo()
        {
            string value = currentAmmo < 0 || reserveAmmo < 0
                ? "AMMO --/--"
                : $"AMMO {currentAmmo}/{reserveAmmo}";

            SetText(ammoText, value);
        }

        private void RefreshWave()
        {
            SetText(waveText, $"WAVE {currentWave}");
        }

        private void RefreshKills()
        {
            SetText(killsText, $"KILLS {kills}");
        }

        private void RefreshScore()
        {
            SetText(scoreText, $"SCORE {score}");
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }
    }
}
