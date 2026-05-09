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
        [SerializeField] private RectTransform crosshair;
        [SerializeField] private CanvasGroup hitMarkerGroup;
        [SerializeField] private CanvasGroup damageOverlayGroup;

        [Header("Hit Marker")]
        [SerializeField] private float hitMarkerHoldTime = 0.08f;
        [SerializeField] private float hitMarkerFadeSpeed = 8f;

        [Header("Damage Overlay")]
        [SerializeField] private float damageOverlayAlpha = 0.45f;
        [SerializeField] private float damageOverlayFadeSpeed = 2.8f;

        [Header("Crosshair Recoil")]
        [SerializeField] private float fallbackCrosshairKickPixels = 3f;
        [SerializeField] private float fallbackMaxCrosshairLiftPixels = 16f;
        [SerializeField] private float fallbackCrosshairReturnSpeed = 55f;

        private Vector2 crosshairBasePosition;
        private float crosshairLift;
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

            if (crosshair == null)
            {
                GameObject crosshairObject = GameObject.Find("Crosshair");

                if (crosshairObject != null)
                {
                    crosshair = crosshairObject.GetComponent<RectTransform>();
                }
            }

            if (crosshair != null)
            {
                crosshairBasePosition = crosshair.anchoredPosition;
            }

            SetGroupAlpha(hitMarkerGroup, 0f);
            SetGroupAlpha(damageOverlayGroup, 0f);
        }

        private void OnEnable()
        {
            if (weaponController != null)
            {
                weaponController.DamageableHit += ShowHitMarker;
                weaponController.ShotFired += KickCrosshair;
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
                weaponController.ShotFired -= KickCrosshair;
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
            UpdateCrosshairRecoil();
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

        private void KickCrosshair()
        {
            if (crosshair == null)
            {
                return;
            }

            WeaponData weapon = weaponController != null ? weaponController.CurrentWeapon : null;
            float kick = weapon != null ? weapon.CrosshairKickPixels : fallbackCrosshairKickPixels;
            float maxLift = weapon != null ? weapon.MaxCrosshairLiftPixels : fallbackMaxCrosshairLiftPixels;

            crosshairLift = Mathf.Min(maxLift, crosshairLift + kick);
            ApplyCrosshairPosition();
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

        private void UpdateCrosshairRecoil()
        {
            if (crosshair == null || crosshairLift <= 0f)
            {
                return;
            }

            WeaponData weapon = weaponController != null ? weaponController.CurrentWeapon : null;
            float returnSpeed = weapon != null ? weapon.CrosshairReturnSpeed : fallbackCrosshairReturnSpeed;
            crosshairLift = Mathf.MoveTowards(crosshairLift, 0f, returnSpeed * Time.unscaledDeltaTime);
            ApplyCrosshairPosition();
        }

        private void ApplyCrosshairPosition()
        {
            if (crosshair != null)
            {
                crosshair.anchoredPosition = crosshairBasePosition + Vector2.up * crosshairLift;
            }
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
