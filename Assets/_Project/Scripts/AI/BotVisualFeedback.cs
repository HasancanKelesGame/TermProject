using UnityEngine;
using TermProject.Game;

namespace TermProject.AI
{
    [DisallowMultipleComponent]
    public sealed class BotVisualFeedback : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Damageable damageable;
        [SerializeField] private Renderer[] renderers;

        [Header("Colors")]
        [SerializeField] private Color hitColor = new Color(1f, 0.25f, 0.2f, 1f);
        [SerializeField] private Color deadColor = new Color(0.2f, 0.2f, 0.2f, 1f);

        [Header("Timing")]
        [SerializeField] private float hitFlashDuration = 0.08f;

        private readonly MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
        private float previousHealth;
        private float flashUntilTime;
        private bool dead;

        private void Awake()
        {
            if (damageable == null)
            {
                damageable = GetComponentInParent<Damageable>();
            }

            if (renderers == null || renderers.Length == 0)
            {
                renderers = GetComponentsInChildren<Renderer>();
            }

            if (damageable != null)
            {
                previousHealth = damageable.CurrentHealth;
            }
        }

        private void OnEnable()
        {
            if (damageable == null)
            {
                return;
            }

            damageable.HealthChanged.AddListener(HandleHealthChanged);
            damageable.Died.AddListener(HandleDeath);
            previousHealth = damageable.CurrentHealth;
        }

        private void OnDisable()
        {
            if (damageable == null)
            {
                return;
            }

            damageable.HealthChanged.RemoveListener(HandleHealthChanged);
            damageable.Died.RemoveListener(HandleDeath);
        }

        private void Update()
        {
            if (dead)
            {
                SetColor(deadColor);
                return;
            }

            if (Time.time < flashUntilTime)
            {
                SetColor(hitColor);
                return;
            }

            ClearColor();
        }

        private void HandleHealthChanged(float currentHealth, float maxHealth)
        {
            if (currentHealth < previousHealth)
            {
                flashUntilTime = Time.time + hitFlashDuration;
            }

            previousHealth = currentHealth;
        }

        private void HandleDeath()
        {
            dead = true;
            SetColor(deadColor);
        }

        private void SetColor(Color color)
        {
            foreach (Renderer targetRenderer in renderers)
            {
                if (targetRenderer == null)
                {
                    continue;
                }

                targetRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_Color", color);
                targetRenderer.SetPropertyBlock(propertyBlock);
            }
        }

        private void ClearColor()
        {
            foreach (Renderer targetRenderer in renderers)
            {
                if (targetRenderer == null)
                {
                    continue;
                }

                targetRenderer.SetPropertyBlock(null);
            }
        }
    }
}
