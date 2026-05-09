using UnityEngine;

namespace TermProject.Weapons
{
    [CreateAssetMenu(fileName = "WD_Rifle", menuName = "TermProject/Weapons/Weapon Data")]
    public sealed class WeaponData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string weaponName = "Rifle";

        [Header("Damage")]
        [SerializeField] private float damage = 25f;
        [SerializeField] private float range = 80f;
        [SerializeField] private float fireRate = 7.5f;
        [SerializeField] private float spreadAngle = 0.4f;

        [Header("Spray")]
        [SerializeField] private float spreadIncreasePerShot;
        [SerializeField] private float maxSpreadAngle = 0.4f;
        [SerializeField] private float spreadRecoverySpeed = 6f;

        [Header("View Recoil")]
        [SerializeField] private float recoilPositionMultiplier = 1f;
        [SerializeField] private float recoilRotationMultiplier = 1f;

        [Header("Crosshair Recoil")]
        [SerializeField] private float crosshairKickPixels = 3f;
        [SerializeField] private float maxCrosshairLiftPixels = 16f;
        [SerializeField] private float crosshairReturnSpeed = 55f;

        [Header("Ammo")]
        [SerializeField] private int magazineSize = 30;
        [SerializeField] private int startingReserveAmmo = 90;
        [SerializeField] private int maxReserveAmmo = 90;
        [SerializeField] private float reloadTime = 1.4f;

        [Header("Audio")]
        [SerializeField] private AudioClip fireSound;
        [SerializeField] private AudioClip reloadSound;
        [SerializeField, Range(0f, 1f)] private float fireVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float reloadVolume = 0.7f;

        public string WeaponName => string.IsNullOrWhiteSpace(weaponName) ? name : weaponName;
        public float Damage => Mathf.Max(0f, damage);
        public float Range => Mathf.Max(1f, range);
        public float FireRate => Mathf.Max(0.1f, fireRate);
        public float SpreadAngle => Mathf.Max(0f, spreadAngle);
        public float SpreadIncreasePerShot => Mathf.Max(0f, spreadIncreasePerShot);
        public float MaxSpreadAngle => Mathf.Max(SpreadAngle, maxSpreadAngle);
        public float SpreadRecoverySpeed => Mathf.Max(0f, spreadRecoverySpeed);
        public float RecoilPositionMultiplier => Mathf.Max(0f, recoilPositionMultiplier);
        public float RecoilRotationMultiplier => Mathf.Max(0f, recoilRotationMultiplier);
        public float CrosshairKickPixels => Mathf.Max(0f, crosshairKickPixels);
        public float MaxCrosshairLiftPixels => Mathf.Max(0f, maxCrosshairLiftPixels);
        public float CrosshairReturnSpeed => Mathf.Max(0f, crosshairReturnSpeed);
        public int MagazineSize => Mathf.Max(1, magazineSize);
        public int StartingReserveAmmo => Mathf.Max(0, startingReserveAmmo);
        public int MaxReserveAmmo => Mathf.Max(StartingReserveAmmo, maxReserveAmmo);
        public float ReloadTime => Mathf.Max(0.05f, reloadTime);
        public float SecondsPerShot => 1f / FireRate;
        public AudioClip FireSound => fireSound;
        public AudioClip ReloadSound => reloadSound;
        public float FireVolume => Mathf.Clamp01(fireVolume);
        public float ReloadVolume => Mathf.Clamp01(reloadVolume);
    }
}
