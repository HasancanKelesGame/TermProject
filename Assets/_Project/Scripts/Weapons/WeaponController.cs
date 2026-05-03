using UnityEngine;
using TermProject.Game;
using TermProject.UI;

namespace TermProject.Weapons
{
    [DisallowMultipleComponent]
    public sealed class WeaponController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WeaponData startingWeapon;
        [SerializeField] private Camera aimCamera;
        [SerializeField] private HudController hudController;
        [SerializeField] private Transform muzzleTransform;

        [Header("Hit Detection")]
        [SerializeField] private LayerMask hitLayers = ~0;

        [Header("Input")]
        [SerializeField] private KeyCode reloadKey = KeyCode.R;
        [SerializeField] private bool automaticFire = true;

        [Header("Effects")]
        [SerializeField] private ParticleSystem muzzleFlash;
        [SerializeField] private Light muzzleLight;
        [SerializeField] private float muzzleLightDuration = 0.04f;
        [SerializeField] private GameObject hitImpactPrefab;
        [SerializeField] private float hitImpactLifetime = 1.5f;

        [Header("Debug")]
        [SerializeField] private bool drawShotRays;
        [SerializeField] private float debugRayDuration = 0.15f;

        private WeaponData currentWeapon;
        private int magazineAmmo;
        private int reserveAmmo;
        private float nextFireTime;
        private float reloadCompleteTime;
        private float muzzleLightOffTime;
        private bool reloading;
        private bool inputEnabled = true;

        public WeaponData CurrentWeapon => currentWeapon;
        public int MagazineAmmo => magazineAmmo;
        public int ReserveAmmo => reserveAmmo;
        public bool IsReloading => reloading;
        public event System.Action ShotFired;
        public event System.Action<float> ReloadStarted;
        public event System.Action ReloadFinished;

        private void Awake()
        {
            if (aimCamera == null)
            {
                aimCamera = GetComponentInChildren<Camera>();
            }

            if (hudController == null)
            {
                hudController = FindFirstObjectByType<HudController>();
            }

            if (muzzleLight != null)
            {
                muzzleLight.enabled = false;
            }

            EquipWeapon(startingWeapon);
        }

        private void Start()
        {
            RefreshHud();
        }

        private void Update()
        {
            UpdateEffectTimers();

            if (currentWeapon == null || aimCamera == null)
            {
                return;
            }

            UpdateReload();
            HandleInput();
        }

        public void EquipWeapon(WeaponData weaponData)
        {
            currentWeapon = weaponData;
            reloading = false;
            nextFireTime = 0f;
            reloadCompleteTime = 0f;

            if (currentWeapon == null)
            {
                magazineAmmo = 0;
                reserveAmmo = 0;
                RefreshHud();
                return;
            }

            magazineAmmo = currentWeapon.MagazineSize;
            reserveAmmo = currentWeapon.StartingReserveAmmo;
            RefreshHud();
        }

        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;

            if (!enabled)
            {
                reloading = false;

                if (muzzleLight != null)
                {
                    muzzleLight.enabled = false;
                }
            }
        }

        public int AddReserveAmmo(int amount)
        {
            if (currentWeapon == null || amount <= 0)
            {
                return 0;
            }

            int previousReserve = reserveAmmo;
            reserveAmmo = Mathf.Min(currentWeapon.MaxReserveAmmo, reserveAmmo + amount);
            RefreshHud();
            return reserveAmmo - previousReserve;
        }

        public bool HasReserveAmmoSpace()
        {
            return currentWeapon != null && reserveAmmo < currentWeapon.MaxReserveAmmo;
        }

        private void HandleInput()
        {
            if (!inputEnabled)
            {
                return;
            }

            if (Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            bool firePressed = automaticFire ? Input.GetButton("Fire1") : Input.GetButtonDown("Fire1");

            if (firePressed)
            {
                TryFire();
            }

            if (Input.GetKeyDown(reloadKey))
            {
                TryStartReload();
            }
        }

        private void TryFire()
        {
            if (reloading || Time.time < nextFireTime)
            {
                return;
            }

            if (magazineAmmo <= 0)
            {
                TryStartReload();
                return;
            }

            nextFireTime = Time.time + currentWeapon.SecondsPerShot;
            magazineAmmo--;
            RefreshHud();
            PlayMuzzleEffects();
            ShotFired?.Invoke();

            Ray shotRay = GetShotRay();

            if (!Physics.Raycast(shotRay, out RaycastHit hit, currentWeapon.Range, hitLayers, QueryTriggerInteraction.Ignore))
            {
                if (drawShotRays)
                {
                    Debug.DrawRay(GetVisualShotOrigin(), shotRay.direction * currentWeapon.Range, Color.red, debugRayDuration);
                }

                return;
            }

            if (drawShotRays)
            {
                Debug.DrawLine(GetVisualShotOrigin(), hit.point, Color.red, debugRayDuration);
            }

            SpawnHitImpact(hit);

            Damageable damageable = hit.collider.GetComponentInParent<Damageable>();

            if (damageable != null)
            {
                damageable.ApplyDamage(currentWeapon.Damage);
            }
        }

        private Ray GetShotRay()
        {
            Vector3 direction = aimCamera.transform.forward;
            float spread = currentWeapon.SpreadAngle;

            if (spread > 0f)
            {
                float spreadRadius = Mathf.Tan(spread * Mathf.Deg2Rad);
                Vector2 offset = Random.insideUnitCircle * spreadRadius;
                direction = (
                    aimCamera.transform.forward
                    + aimCamera.transform.right * offset.x
                    + aimCamera.transform.up * offset.y).normalized;
            }

            return new Ray(aimCamera.transform.position, direction);
        }

        private Vector3 GetVisualShotOrigin()
        {
            return muzzleTransform != null ? muzzleTransform.position : aimCamera.transform.position;
        }

        private void PlayMuzzleEffects()
        {
            if (muzzleFlash != null)
            {
                muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                muzzleFlash.Play();
            }

            if (muzzleLight != null)
            {
                muzzleLight.enabled = true;
                muzzleLightOffTime = Time.time + muzzleLightDuration;
            }
        }

        private void SpawnHitImpact(RaycastHit hit)
        {
            if (hitImpactPrefab == null)
            {
                return;
            }

            Quaternion rotation = Quaternion.LookRotation(hit.normal);
            Vector3 position = hit.point + hit.normal * 0.01f;
            GameObject impact = Instantiate(hitImpactPrefab, position, rotation);
            Destroy(impact, hitImpactLifetime);
        }

        private void UpdateEffectTimers()
        {
            if (muzzleLight != null && muzzleLight.enabled && Time.time >= muzzleLightOffTime)
            {
                muzzleLight.enabled = false;
            }
        }

        private void TryStartReload()
        {
            if (reloading || reserveAmmo <= 0 || magazineAmmo >= currentWeapon.MagazineSize)
            {
                return;
            }

            reloading = true;
            reloadCompleteTime = Time.time + currentWeapon.ReloadTime;
            ReloadStarted?.Invoke(currentWeapon.ReloadTime);
        }

        private void UpdateReload()
        {
            if (!reloading || Time.time < reloadCompleteTime)
            {
                return;
            }

            int neededAmmo = currentWeapon.MagazineSize - magazineAmmo;
            int loadedAmmo = Mathf.Min(neededAmmo, reserveAmmo);

            magazineAmmo += loadedAmmo;
            reserveAmmo -= loadedAmmo;
            reloading = false;
            RefreshHud();
            ReloadFinished?.Invoke();
        }

        private void RefreshHud()
        {
            if (hudController == null)
            {
                return;
            }

            if (currentWeapon == null)
            {
                hudController.SetAmmo(-1, -1);
                return;
            }

            hudController.SetAmmo(magazineAmmo, reserveAmmo);
        }
    }
}
