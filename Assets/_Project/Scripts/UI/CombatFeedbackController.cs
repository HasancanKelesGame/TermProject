using UnityEngine;
using TermProject.Game;
using TermProject.Player;
using TermProject.Weapons;

namespace TermProject.UI
{
    [DisallowMultipleComponent]
    public sealed class CombatFeedbackController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WeaponController weaponController;
        [SerializeField] private Damageable playerHealth;
        [SerializeField] private CanvasGroup hitMarkerGroup;
        [SerializeField] private CanvasGroup damageOverlayGroup;

        [Header("Hit Marker")]
        [SerializeField] private float hitMarkerHoldTime = 0.08f;
        [SerializeField] private float hitMarkerFadeSpeed = 8f;

        [Header("Damage Overlay")]
        [SerializeField] private float damageOverlayAlpha = 0.45f;
        [SerializeField] private float damageOverlayFadeSpeed = 2.8f;

        private float hitMarkerHoldUntil;

        private void Awake()
        {
            if (weaponController == null)
            {
                weaponController = FindFirstObjectByType<WeaponController>();
            }

            if (playerHealth == null)
            {
                PlayerController playerController = FindFirstObjectByType<PlayerController>();

                if (playerController != null)
                {
                    playerHealth = playerController.GetComponent<Damageable>();
                }
            }

            SetGroupAlpha(hitMarkerGroup, 0f);
            SetGroupAlpha(damageOverlayGroup, 0f);
        }

        private void OnEnable()
        {
            if (weaponController != null)
            {
                weaponController.DamageableHit += ShowHitMarker;
            }

            if (playerHealth != null)
            {
                playerHealth.Damaged += ShowDamageOverlay;
            }
        }

        private void OnDisable()
        {
            if (weaponController != null)
            {
                weaponController.DamageableHit -= ShowHitMarker;
            }

            if (playerHealth != null)
            {
                playerHealth.Damaged -= ShowDamageOverlay;
            }
        }

        private void Update()
        {
            UpdateHitMarker();
            UpdateDamageOverlay();
        }

        private void ShowHitMarker(Damageable target)
        {
            if (target == null || target == playerHealth)
            {
                return;
            }

            hitMarkerHoldUntil = Time.unscaledTime + hitMarkerHoldTime;
            SetGroupAlpha(hitMarkerGroup, 1f);
        }

        private void ShowDamageOverlay(float damageAmount, float currentHealth, float maxHealth)
        {
            if (damageAmount <= 0f)
            {
                return;
            }

            SetGroupAlpha(damageOverlayGroup, damageOverlayAlpha);
        }

        private void UpdateHitMarker()
        {
            if (hitMarkerGroup == null || Time.unscaledTime < hitMarkerHoldUntil)
            {
                return;
            }

            float alpha = Mathf.MoveTowards(hitMarkerGroup.alpha, 0f, hitMarkerFadeSpeed * Time.unscaledDeltaTime);
            SetGroupAlpha(hitMarkerGroup, alpha);
        }

        private void UpdateDamageOverlay()
        {
            if (damageOverlayGroup == null)
            {
                return;
            }

            float alpha = Mathf.MoveTowards(damageOverlayGroup.alpha, 0f, damageOverlayFadeSpeed * Time.unscaledDeltaTime);
            SetGroupAlpha(damageOverlayGroup, alpha);
        }

        private static void SetGroupAlpha(CanvasGroup group, float alpha)
        {
            if (group == null)
            {
                return;
            }

            group.alpha = Mathf.Clamp01(alpha);
            group.blocksRaycasts = false;
            group.interactable = false;
        }
    }
}
