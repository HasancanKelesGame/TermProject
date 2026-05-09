using System.Collections;
using UnityEngine;
using TermProject.Game;
using TermProject.UI;

namespace TermProject.Weapons
{
    [DisallowMultipleComponent]
    public sealed class WeaponController : MonoBehaviour
    {
        [System.Serializable]
        public sealed class WeaponSlot
        {
            [SerializeField] private WeaponData weapon;
            [SerializeField] private GameObject visualRoot;
            [SerializeField] private Transform muzzleTransform;
            [SerializeField] private bool automaticFire = true;

            private int magazineAmmo;
            private int reserveAmmo;
            private bool initialized;

            public WeaponData Weapon => weapon;
            public GameObject VisualRoot => visualRoot;
            public Transform MuzzleTransform => muzzleTransform;
            public bool AutomaticFire => automaticFire;
            public int MagazineAmmo => magazineAmmo;
            public int ReserveAmmo => reserveAmmo;
            public bool IsValid => weapon != null;

            public void InitializeAmmo()
            {
                if (initialized || weapon == null)
                {
                    return;
                }

                magazineAmmo = weapon.MagazineSize;
                reserveAmmo = weapon.StartingReserveAmmo;
                initialized = true;
            }

            public void SetAmmo(int magazine, int reserve)
            {
                if (weapon == null)
                {
                    magazineAmmo = 0;
                    reserveAmmo = 0;
                    return;
                }

                magazineAmmo = Mathf.Clamp(magazine, 0, weapon.MagazineSize);
                reserveAmmo = Mathf.Clamp(reserve, 0, weapon.MaxReserveAmmo);
                initialized = true;
            }
        }

        [Header("References")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private WeaponData startingWeapon;
        [SerializeField] private Camera aimCamera;
        [SerializeField] private HudController hudController;
        [SerializeField] private Transform muzzleTransform;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioSource automaticFireAudioSource;
        [SerializeField] private AudioSource automaticFireEchoAudioSource;

        [Header("Weapon Slots")]
        [SerializeField] private WeaponSlot[] weaponSlots;
        [SerializeField] private int startingSlotIndex;
        [SerializeField] private bool enableNumberKeySwitching = true;
        [SerializeField] private bool enableScrollWheelSwitching = true;

        [Header("Audio")]
        [SerializeField] private AudioClip weaponSwitchSound;
        [SerializeField, Range(0f, 1f)] private float weaponSwitchVolume = 0.6f;
        [SerializeField, Min(0f)] private float automaticFireStopDelay = 0.08f;
        [SerializeField, Min(0f)] private float automaticFireEchoDuration = 1f;
        [SerializeField, Range(0f, 1f)] private float automaticFireEchoVolumeMultiplier = 0.35f;

        [Header("Hit Detection")]
        [SerializeField] private LayerMask hitLayers = ~0;

        [Header("Input")]
        [SerializeField] private KeyCode reloadKey = KeyCode.R;
        [SerializeField] private bool automaticFire = true;

        [Header("Crazy Mode")]
        [SerializeField] private bool useCrazyModeOverrides = true;
        [SerializeField] private bool crazyModeUnlimitedAmmo = true;
        [SerializeField, Min(1f)] private float crazyModeAutomaticFireRateMultiplier = 1.75f;

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
        private WeaponSlot currentSlot;
        private int currentSlotIndex = -1;
        private int magazineAmmo;
        private int reserveAmmo;
        private float nextFireTime;
        private float reloadCompleteTime;
        private float muzzleLightOffTime;
        private float automaticFireStopTime = -1f;
        private float additionalSpreadAngle;
        private bool reloading;
        private bool inputEnabled = true;
        private bool automaticFireSoundActive;
        private Coroutine automaticFireEchoCoroutine;

        public WeaponData CurrentWeapon => currentWeapon;
        public int CurrentSlotIndex => currentSlotIndex;
        public int MagazineAmmo => magazineAmmo;
        public int ReserveAmmo => reserveAmmo;
        public bool IsReloading => reloading;
        public event System.Action ShotFired;
        public event System.Action<Damageable> DamageableHit;
        public event System.Action<float> ReloadStarted;
        public event System.Action ReloadFinished;

        private void Awake()
        {
            if (gameManager == null)
            {
                gameManager = GameManager.Instance ?? FindFirstObjectByType<GameManager>();
            }

            if (aimCamera == null)
            {
                aimCamera = GetComponentInChildren<Camera>();
            }

            if (hudController == null)
            {
                hudController = FindFirstObjectByType<HudController>();
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            Configure2DAudioSource(audioSource);

            if (automaticFireAudioSource == null)
            {
                automaticFireAudioSource = gameObject.AddComponent<AudioSource>();
            }

            Configure2DAudioSource(automaticFireAudioSource);

            if (automaticFireEchoAudioSource == null)
            {
                automaticFireEchoAudioSource = gameObject.AddComponent<AudioSource>();
            }

            Configure2DAudioSource(automaticFireEchoAudioSource);

            if (muzzleLight != null)
            {
                muzzleLight.enabled = false;
            }

            InitializeWeaponSlots();
            SelectInitialWeapon();
        }

        private void Start()
        {
            RefreshHud();
        }

        private void Update()
        {
            UpdateEffectTimers();
            UpdateAutomaticFireSound();
            UpdateSpreadRecovery();

            if (aimCamera == null)
            {
                return;
            }

            if (currentWeapon != null)
            {
                UpdateReload();
            }

            HandleInput();
        }

        public void EquipWeapon(WeaponData weaponData)
        {
            int matchingSlotIndex = FindSlotIndex(weaponData);

            if (matchingSlotIndex >= 0)
            {
                SelectSlot(matchingSlotIndex);
                return;
            }

            SaveCurrentSlotAmmo();
            StopAutomaticFireSoundNow();
            SetAllSlotVisualsInactive();
            currentSlot = null;
            currentSlotIndex = -1;
            currentWeapon = weaponData;
            reloading = false;
            nextFireTime = 0f;
            reloadCompleteTime = 0f;
            additionalSpreadAngle = 0f;

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

        public void SelectSlot(int slotIndex)
        {
            SelectSlot(slotIndex, true);
        }

        private void SelectSlot(int slotIndex, bool playSwitchSound)
        {
            if (!IsSlotIndexValid(slotIndex) || currentSlotIndex == slotIndex)
            {
                return;
            }

            SaveCurrentSlotAmmo();
            StopAutomaticFireSoundNow();

            if (reloading)
            {
                reloading = false;
                ReloadFinished?.Invoke();
            }

            currentSlotIndex = slotIndex;
            currentSlot = weaponSlots[slotIndex];
            currentSlot.InitializeAmmo();
            currentWeapon = currentSlot.Weapon;
            magazineAmmo = currentSlot.MagazineAmmo;
            reserveAmmo = currentSlot.ReserveAmmo;
            nextFireTime = 0f;
            reloadCompleteTime = 0f;
            additionalSpreadAngle = 0f;
            SetActiveSlotVisual(slotIndex);
            RefreshHud();

            if (playSwitchSound)
            {
                PlaySound(weaponSwitchSound, weaponSwitchVolume);
            }
        }

        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;

            if (!enabled)
            {
                reloading = false;
                additionalSpreadAngle = 0f;
                StopAutomaticFireSoundNow();

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
            SaveCurrentSlotAmmo();
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

            HandleWeaponSwitchInput();

            if (currentWeapon == null)
            {
                return;
            }

            bool firePressed = CurrentWeaponUsesAutomaticFire() ? Input.GetButton("Fire1") : Input.GetButtonDown("Fire1");

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
                if (HasUnlimitedAmmo())
                {
                    magazineAmmo = currentWeapon.MagazineSize;
                }
                else
                {
                    TryStartReload();
                    return;
                }
            }

            nextFireTime = Time.time + GetCurrentSecondsPerShot();

            if (HasUnlimitedAmmo())
            {
                magazineAmmo = currentWeapon.MagazineSize;
            }
            else
            {
                magazineAmmo--;
                SaveCurrentSlotAmmo();
            }

            RefreshHud();
            PlayMuzzleEffects();
            PlayFireSound();
            ShotFired?.Invoke();

            float shotSpreadAngle = GetCurrentSpreadAngle();
            Ray shotRay = GetShotRay(shotSpreadAngle);
            AddShotSpread();

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
                DamageableHit?.Invoke(damageable);
            }
        }

        private Ray GetShotRay(float spread)
        {
            Vector3 direction = aimCamera.transform.forward;

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
            Transform activeMuzzle = GetActiveMuzzleTransform();
            return activeMuzzle != null ? activeMuzzle.position : aimCamera.transform.position;
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
            if (HasUnlimitedAmmo())
            {
                return;
            }

            if (reloading || reserveAmmo <= 0 || magazineAmmo >= currentWeapon.MagazineSize)
            {
                return;
            }

            reloading = true;
            reloadCompleteTime = Time.time + currentWeapon.ReloadTime;
            additionalSpreadAngle = 0f;
            StopAutomaticFireSoundNow();
            PlaySound(currentWeapon.ReloadSound, currentWeapon.ReloadVolume);
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
            SaveCurrentSlotAmmo();
            reloading = false;
            RefreshHud();
            ReloadFinished?.Invoke();
        }

        private void InitializeWeaponSlots()
        {
            if (weaponSlots == null)
            {
                return;
            }

            for (int i = 0; i < weaponSlots.Length; i++)
            {
                weaponSlots[i]?.InitializeAmmo();
            }
        }

        private void SelectInitialWeapon()
        {
            int clampedStartingSlot = Mathf.Clamp(startingSlotIndex, 0, Mathf.Max(0, GetSlotCount() - 1));

            if (IsSlotIndexValid(clampedStartingSlot))
            {
                SelectSlot(clampedStartingSlot, false);
                return;
            }

            EquipWeapon(startingWeapon);
        }

        private void HandleWeaponSwitchInput()
        {
            if (enableNumberKeySwitching)
            {
                for (int i = 0; i < GetSlotCount() && i < 9; i++)
                {
                    if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i)))
                    {
                        SelectSlot(i);
                        return;
                    }
                }
            }

            if (!enableScrollWheelSwitching || GetSlotCount() <= 1)
            {
                return;
            }

            float scroll = Input.GetAxis("Mouse ScrollWheel");

            if (scroll > 0.01f)
            {
                SelectNextSlot(1);
            }
            else if (scroll < -0.01f)
            {
                SelectNextSlot(-1);
            }
        }

        private void SelectNextSlot(int direction)
        {
            int slotCount = GetSlotCount();

            if (slotCount <= 0)
            {
                return;
            }

            int startIndex = currentSlotIndex >= 0 ? currentSlotIndex : 0;

            for (int step = 1; step <= slotCount; step++)
            {
                int candidateIndex = (startIndex + direction * step + slotCount) % slotCount;

                if (IsSlotIndexValid(candidateIndex))
                {
                    SelectSlot(candidateIndex);
                    return;
                }
            }
        }

        private void SaveCurrentSlotAmmo()
        {
            if (currentSlot != null)
            {
                currentSlot.SetAmmo(magazineAmmo, reserveAmmo);
            }
        }

        private void SetActiveSlotVisual(int slotIndex)
        {
            if (weaponSlots == null)
            {
                return;
            }

            for (int i = 0; i < weaponSlots.Length; i++)
            {
                GameObject visualRoot = weaponSlots[i]?.VisualRoot;

                if (visualRoot != null)
                {
                    visualRoot.SetActive(i == slotIndex);
                }
            }
        }

        private void SetAllSlotVisualsInactive()
        {
            if (weaponSlots == null)
            {
                return;
            }

            for (int i = 0; i < weaponSlots.Length; i++)
            {
                GameObject visualRoot = weaponSlots[i]?.VisualRoot;

                if (visualRoot != null)
                {
                    visualRoot.SetActive(false);
                }
            }
        }

        private bool CurrentWeaponUsesAutomaticFire()
        {
            return currentSlot != null ? currentSlot.AutomaticFire : automaticFire;
        }

        private Transform GetActiveMuzzleTransform()
        {
            return currentSlot != null && currentSlot.MuzzleTransform != null
                ? currentSlot.MuzzleTransform
                : muzzleTransform;
        }

        private int FindSlotIndex(WeaponData weaponData)
        {
            if (weaponData == null || weaponSlots == null)
            {
                return -1;
            }

            for (int i = 0; i < weaponSlots.Length; i++)
            {
                if (weaponSlots[i] != null && weaponSlots[i].Weapon == weaponData)
                {
                    return i;
                }
            }

            return -1;
        }

        private bool IsSlotIndexValid(int slotIndex)
        {
            return weaponSlots != null
                && slotIndex >= 0
                && slotIndex < weaponSlots.Length
                && weaponSlots[slotIndex] != null
                && weaponSlots[slotIndex].IsValid;
        }

        private int GetSlotCount()
        {
            return weaponSlots == null ? 0 : weaponSlots.Length;
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

            if (HasUnlimitedAmmo())
            {
                hudController.SetUnlimitedAmmo(currentWeapon.MagazineSize);
                return;
            }

            hudController.SetAmmo(magazineAmmo, reserveAmmo);
        }

        private float GetCurrentSpreadAngle()
        {
            if (currentWeapon == null)
            {
                return 0f;
            }

            return Mathf.Min(currentWeapon.MaxSpreadAngle, currentWeapon.SpreadAngle + additionalSpreadAngle);
        }

        private void AddShotSpread()
        {
            if (currentWeapon == null || currentWeapon.SpreadIncreasePerShot <= 0f)
            {
                return;
            }

            float maxAdditionalSpread = Mathf.Max(0f, currentWeapon.MaxSpreadAngle - currentWeapon.SpreadAngle);
            additionalSpreadAngle = Mathf.Min(maxAdditionalSpread, additionalSpreadAngle + currentWeapon.SpreadIncreasePerShot);
        }

        private void UpdateSpreadRecovery()
        {
            if (additionalSpreadAngle <= 0f || currentWeapon == null)
            {
                return;
            }

            if (ShouldHoldCurrentSpray())
            {
                return;
            }

            additionalSpreadAngle = Mathf.MoveTowards(
                additionalSpreadAngle,
                0f,
                currentWeapon.SpreadRecoverySpeed * Time.deltaTime);
        }

        private bool ShouldHoldCurrentSpray()
        {
            return inputEnabled
                && currentWeapon != null
                && CurrentWeaponUsesAutomaticFire()
                && !reloading
                && Cursor.lockState == CursorLockMode.Locked
                && Input.GetButton("Fire1");
        }

        private void PlaySound(AudioClip clip, float volume)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip, volume);
            }
        }

        private void PlayFireSound()
        {
            if (currentWeapon == null || currentWeapon.FireSound == null)
            {
                return;
            }

            if (!CurrentWeaponUsesAutomaticFire() || automaticFireAudioSource == null)
            {
                PlaySound(currentWeapon.FireSound, currentWeapon.FireVolume);
                return;
            }

            automaticFireAudioSource.clip = currentWeapon.FireSound;
            automaticFireAudioSource.loop = false;
            automaticFireAudioSource.volume = currentWeapon.FireVolume;
            automaticFireAudioSource.Play();
            automaticFireSoundActive = true;
            automaticFireStopTime = -1f;
        }

        private void UpdateAutomaticFireSound()
        {
            if (!automaticFireSoundActive)
            {
                if (automaticFireStopTime > 0f && Time.time >= automaticFireStopTime)
                {
                    if (automaticFireAudioSource != null && automaticFireAudioSource.isPlaying)
                    {
                        automaticFireAudioSource.Stop();
                    }

                    automaticFireStopTime = -1f;
                }

                return;
            }

            if (ShouldKeepAutomaticFireSoundActive())
            {
                return;
            }

            PlayAutomaticFireEcho();
            automaticFireSoundActive = false;
            automaticFireStopTime = Time.time + automaticFireStopDelay;
        }

        private bool ShouldKeepAutomaticFireSoundActive()
        {
            return inputEnabled
                && currentWeapon != null
                && CurrentWeaponUsesAutomaticFire()
                && !reloading
                && (magazineAmmo > 0 || HasUnlimitedAmmo())
                && Cursor.lockState == CursorLockMode.Locked
                && Input.GetButton("Fire1");
        }

        private float GetCurrentSecondsPerShot()
        {
            if (currentWeapon == null)
            {
                return 0.1f;
            }

            float secondsPerShot = currentWeapon.SecondsPerShot;

            if (IsCrazyModeActive() && CurrentWeaponUsesAutomaticFire())
            {
                secondsPerShot /= Mathf.Max(1f, crazyModeAutomaticFireRateMultiplier);
            }

            return secondsPerShot;
        }

        private bool HasUnlimitedAmmo()
        {
            return IsCrazyModeActive() && crazyModeUnlimitedAmmo;
        }

        private bool IsCrazyModeActive()
        {
            return useCrazyModeOverrides && gameManager != null && gameManager.CrazyModeEnabled;
        }

        private void StopAutomaticFireSoundNow()
        {
            automaticFireSoundActive = false;
            automaticFireStopTime = -1f;

            if (automaticFireAudioSource != null)
            {
                automaticFireAudioSource.Stop();
            }

            StopAutomaticFireEcho();
        }

        private static void Configure2DAudioSource(AudioSource source)
        {
            if (source == null)
            {
                return;
            }

            source.playOnAwake = false;
            source.spatialBlend = 0f;
        }

        private void PlayAutomaticFireEcho()
        {
            if (currentWeapon == null
                || currentWeapon.FireSound == null
                || automaticFireEchoAudioSource == null
                || automaticFireEchoDuration <= 0f)
            {
                return;
            }

            if (automaticFireEchoCoroutine != null)
            {
                StopCoroutine(automaticFireEchoCoroutine);
            }

            automaticFireEchoCoroutine = StartCoroutine(PlayAutomaticFireEchoRoutine(
                currentWeapon.FireSound,
                currentWeapon.FireVolume * automaticFireEchoVolumeMultiplier));
        }

        private IEnumerator PlayAutomaticFireEchoRoutine(AudioClip clip, float volume)
        {
            automaticFireEchoAudioSource.Stop();
            automaticFireEchoAudioSource.clip = clip;
            automaticFireEchoAudioSource.loop = false;
            automaticFireEchoAudioSource.volume = volume;
            automaticFireEchoAudioSource.time = 0f;
            automaticFireEchoAudioSource.Play();

            float startedAt = Time.time;
            float duration = Mathf.Min(automaticFireEchoDuration, clip.length);

            while (automaticFireEchoAudioSource != null && Time.time - startedAt < duration)
            {
                float elapsed = Time.time - startedAt;
                float fade = Mathf.Clamp01(1f - elapsed / Mathf.Max(0.01f, duration));
                automaticFireEchoAudioSource.volume = volume * fade;
                yield return null;
            }

            if (automaticFireEchoAudioSource != null)
            {
                automaticFireEchoAudioSource.Stop();
            }

            automaticFireEchoCoroutine = null;
        }

        private void StopAutomaticFireEcho()
        {
            if (automaticFireEchoCoroutine != null)
            {
                StopCoroutine(automaticFireEchoCoroutine);
                automaticFireEchoCoroutine = null;
            }

            if (automaticFireEchoAudioSource != null)
            {
                automaticFireEchoAudioSource.Stop();
            }
        }
    }
}
