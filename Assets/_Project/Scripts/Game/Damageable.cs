using UnityEngine;
using UnityEngine.Events;

namespace TermProject.Game
{
    public sealed class Damageable : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private bool destroyOnDeath;

        public UnityEvent<float, float> HealthChanged = new UnityEvent<float, float>();
        public UnityEvent Died = new UnityEvent();
        public event System.Action<float, float, float> Damaged;

        private float currentHealth;
        private bool dead;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public bool IsDead => dead;
        public float HealthPercent => maxHealth <= 0f ? 0f : currentHealth / maxHealth;

        private void Awake()
        {
            currentHealth = maxHealth;
            HealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void ApplyDamage(float amount)
        {
            if (dead || amount <= 0f)
            {
                return;
            }

            float previousHealth = currentHealth;
            currentHealth = Mathf.Max(0f, currentHealth - amount);
            float appliedDamage = previousHealth - currentHealth;
            HealthChanged?.Invoke(currentHealth, maxHealth);
            Damaged?.Invoke(appliedDamage, currentHealth, maxHealth);

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            if (dead || amount <= 0f)
            {
                return;
            }

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            HealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void ResetHealth()
        {
            dead = false;
            currentHealth = maxHealth;
            HealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void SetMaxHealth(float value, bool resetToFullHealth)
        {
            maxHealth = Mathf.Max(1f, value);

            if (resetToFullHealth)
            {
                dead = false;
                currentHealth = maxHealth;
            }
            else
            {
                currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            }

            HealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private void Die()
        {
            if (dead)
            {
                return;
            }

            dead = true;
            Died?.Invoke();

            if (destroyOnDeath)
            {
                Destroy(gameObject);
            }
        }
    }
}
