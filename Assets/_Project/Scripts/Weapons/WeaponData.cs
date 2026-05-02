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

        [Header("Ammo")]
        [SerializeField] private int magazineSize = 30;
        [SerializeField] private int startingReserveAmmo = 90;
        [SerializeField] private float reloadTime = 1.4f;

        public string WeaponName => string.IsNullOrWhiteSpace(weaponName) ? name : weaponName;
        public float Damage => Mathf.Max(0f, damage);
        public float Range => Mathf.Max(1f, range);
        public float FireRate => Mathf.Max(0.1f, fireRate);
        public float SpreadAngle => Mathf.Max(0f, spreadAngle);
        public int MagazineSize => Mathf.Max(1, magazineSize);
        public int StartingReserveAmmo => Mathf.Max(0, startingReserveAmmo);
        public float ReloadTime => Mathf.Max(0.05f, reloadTime);
        public float SecondsPerShot => 1f / FireRate;
    }
}
