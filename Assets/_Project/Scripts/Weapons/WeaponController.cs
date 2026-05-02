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

        [Header("Debug")]
        [SerializeField] private bool drawShotRays;
        [SerializeField] private float debugRayDuration = 0.15f;

        private WeaponData currentWeapon;
        private int magazineAmmo;
        private int reserveAmmo;
        private float nextFireTime;
        private float reloadCompleteTime;
        private bool reloading;

        public WeaponData CurrentWeapon => currentWeapon;
        public int MagazineAmmo => magazineAmmo;
        public int ReserveAmmo => reserveAmmo;
        public bool IsReloading => reloading;

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

            EquipWeapon(startingWeapon);
        }

        private void Start()
        {
            RefreshHud();
        }

        private void Update()
        {
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

        private void HandleInput()
        {
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

        private void TryStartReload()
        {
            if (reloading || reserveAmmo <= 0 || magazineAmmo >= currentWeapon.MagazineSize)
            {
                return;
            }

            reloading = true;
            reloadCompleteTime = Time.time + currentWeapon.ReloadTime;
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
