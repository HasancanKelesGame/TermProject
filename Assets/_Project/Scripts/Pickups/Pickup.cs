using System.Collections;
using UnityEngine;
using TermProject.Game;
using TermProject.Player;
using TermProject.Weapons;

namespace TermProject.Pickups
{
    [DisallowMultipleComponent]
    public sealed class Pickup : MonoBehaviour
    {
        public enum PickupType
        {
            Health,
            Ammo
        }

        [Header("Pickup")]
        [SerializeField] private PickupType pickupType = PickupType.Health;
        [SerializeField] private int amount = 25;
        [SerializeField] private bool respawn = true;
        [SerializeField] private float respawnDelay = 12f;

        [Header("Visual")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private bool rotateVisual = true;
        [SerializeField] private float rotationSpeed = 90f;
        [SerializeField] private bool bobVisual = true;
        [SerializeField] private float bobHeight = 0.15f;
        [SerializeField] private float bobSpeed = 2f;

        private Collider[] pickupColliders;
        private Renderer[] renderers;
        private Vector3 visualStartLocalPosition;
        private bool available = true;

        private void Awake()
        {
            if (visualRoot == null)
            {
                visualRoot = transform;
            }

            pickupColliders = GetComponentsInChildren<Collider>();
            renderers = GetComponentsInChildren<Renderer>();
            visualStartLocalPosition = visualRoot.localPosition;
        }

        private void Update()
        {
            if (!available || visualRoot == null)
            {
                return;
            }

            if (rotateVisual)
            {
                visualRoot.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);
            }

            if (bobVisual)
            {
                float yOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
                visualRoot.localPosition = visualStartLocalPosition + Vector3.up * yOffset;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!available)
            {
                return;
            }

            PlayerController playerController = other.GetComponentInParent<PlayerController>();

            if (playerController == null)
            {
                return;
            }

            if (TryApply(playerController))
            {
                Consume();
            }
        }

        private bool TryApply(PlayerController playerController)
        {
            switch (pickupType)
            {
                case PickupType.Health:
                    return TryApplyHealth(playerController);

                case PickupType.Ammo:
                    return TryApplyAmmo(playerController);

                default:
                    return false;
            }
        }

        private bool TryApplyHealth(PlayerController playerController)
        {
            Damageable damageable = playerController.GetComponent<Damageable>();

            if (damageable == null || damageable.IsDead || damageable.CurrentHealth >= damageable.MaxHealth)
            {
                return false;
            }

            damageable.Heal(amount);
            return true;
        }

        private bool TryApplyAmmo(PlayerController playerController)
        {
            WeaponController weaponController = playerController.GetComponentInChildren<WeaponController>(true);

            if (weaponController == null || !weaponController.HasReserveAmmoSpace())
            {
                return false;
            }

            return weaponController.AddReserveAmmo(amount) > 0;
        }

        private void Consume()
        {
            if (respawn)
            {
                StartCoroutine(RespawnAfterDelay());
                return;
            }

            Destroy(gameObject);
        }

        private IEnumerator RespawnAfterDelay()
        {
            SetAvailable(false);
            yield return new WaitForSeconds(respawnDelay);
            SetAvailable(true);
        }

        private void SetAvailable(bool value)
        {
            available = value;

            if (value && visualRoot != null)
            {
                visualRoot.localPosition = visualStartLocalPosition;
            }

            for (int i = 0; i < pickupColliders.Length; i++)
            {
                if (pickupColliders[i] != null)
                {
                    pickupColliders[i].enabled = value;
                }
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].enabled = value;
                }
            }
        }
    }
}
