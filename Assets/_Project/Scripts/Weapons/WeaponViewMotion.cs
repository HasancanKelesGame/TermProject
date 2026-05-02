using UnityEngine;

namespace TermProject.Weapons
{
    [DisallowMultipleComponent]
    public sealed class WeaponViewMotion : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WeaponController weaponController;
        [SerializeField] private CharacterController characterController;

        [Header("Mouse Sway")]
        [SerializeField] private float mouseSwayPositionAmount = 0.015f;
        [SerializeField] private float mouseSwayRotationAmount = 2f;
        [SerializeField] private float maxMouseSway = 0.06f;

        [Header("Movement Bob")]
        [SerializeField] private float bobFrequency = 8f;
        [SerializeField] private float bobPositionAmount = 0.025f;
        [SerializeField] private float bobRotationAmount = 1.5f;
        [SerializeField] private float minBobSpeed = 0.2f;

        [Header("Jump And Fall")]
        [SerializeField] private float verticalMotionAmount = 0.003f;
        [SerializeField] private float maxVerticalMotion = 0.045f;
        [SerializeField] private float airRotationAmount = 0.12f;

        [Header("Recoil")]
        [SerializeField] private Vector3 recoilPositionKick = new Vector3(0f, 0.018f, -0.075f);
        [SerializeField] private Vector3 recoilRotationKick = new Vector3(-4f, 0.8f, -1.2f);
        [SerializeField] private float recoilReturnSpeed = 18f;
        [SerializeField] private float maxRecoilPosition = 0.16f;
        [SerializeField] private float maxRecoilRotation = 10f;

        [Header("Reload Motion")]
        [SerializeField] private Vector3 reloadPositionOffset = new Vector3(-0.08f, -0.18f, -0.05f);
        [SerializeField] private Vector3 reloadRotationOffset = new Vector3(18f, -26f, 10f);
        [SerializeField] private float reloadDipInPercent = 0.18f;
        [SerializeField] private float reloadReturnPercent = 0.25f;

        [Header("Smoothing")]
        [SerializeField] private float positionSmooth = 14f;
        [SerializeField] private float rotationSmooth = 16f;

        private Vector3 baseLocalPosition;
        private Quaternion baseLocalRotation;
        private Vector3 smoothedPositionOffset;
        private Vector3 smoothedRotationOffset;
        private Vector3 recoilPositionOffset;
        private Vector3 recoilRotationOffset;
        private float bobTimer;
        private float reloadStartTime;
        private float reloadDuration;
        private bool reloadMotionActive;

        private void Awake()
        {
            if (weaponController == null)
            {
                weaponController = GetComponentInParent<WeaponController>();
            }

            if (characterController == null)
            {
                characterController = GetComponentInParent<CharacterController>();
            }

            baseLocalPosition = transform.localPosition;
            baseLocalRotation = transform.localRotation;
        }

        private void OnEnable()
        {
            if (weaponController != null)
            {
                weaponController.ShotFired += AddRecoil;
                weaponController.ReloadStarted += StartReloadMotion;
                weaponController.ReloadFinished += FinishReloadMotion;
            }
        }

        private void OnDisable()
        {
            if (weaponController != null)
            {
                weaponController.ShotFired -= AddRecoil;
                weaponController.ReloadStarted -= StartReloadMotion;
                weaponController.ReloadFinished -= FinishReloadMotion;
            }
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            recoilPositionOffset = Vector3.Lerp(recoilPositionOffset, Vector3.zero, recoilReturnSpeed * deltaTime);
            recoilRotationOffset = Vector3.Lerp(recoilRotationOffset, Vector3.zero, recoilReturnSpeed * deltaTime);

            Vector3 targetPositionOffset = CalculateMouseSwayPosition()
                + CalculateMovementBobPosition()
                + CalculateVerticalMotionPosition()
                + CalculateReloadPosition()
                + recoilPositionOffset;

            Vector3 targetRotationOffset = CalculateMouseSwayRotation()
                + CalculateMovementBobRotation()
                + CalculateVerticalMotionRotation()
                + CalculateReloadRotation()
                + recoilRotationOffset;

            smoothedPositionOffset = Vector3.Lerp(smoothedPositionOffset, targetPositionOffset, positionSmooth * deltaTime);
            smoothedRotationOffset = Vector3.Lerp(smoothedRotationOffset, targetRotationOffset, rotationSmooth * deltaTime);

            transform.localPosition = baseLocalPosition + smoothedPositionOffset;
            transform.localRotation = baseLocalRotation * Quaternion.Euler(smoothedRotationOffset);
        }

        private Vector3 CalculateMouseSwayPosition()
        {
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                return Vector3.zero;
            }

            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            Vector3 offset = new Vector3(-mouseX, -mouseY, 0f) * mouseSwayPositionAmount;
            return Vector3.ClampMagnitude(offset, maxMouseSway);
        }

        private Vector3 CalculateMouseSwayRotation()
        {
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                return Vector3.zero;
            }

            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            return new Vector3(
                mouseY * mouseSwayRotationAmount,
                -mouseX * mouseSwayRotationAmount,
                -mouseX * mouseSwayRotationAmount * 0.5f);
        }

        private Vector3 CalculateMovementBobPosition()
        {
            if (characterController == null)
            {
                return Vector3.zero;
            }

            Vector3 planarVelocity = characterController.velocity;
            planarVelocity.y = 0f;
            float speed = planarVelocity.magnitude;

            if (!characterController.isGrounded || speed < minBobSpeed)
            {
                bobTimer = 0f;
                return Vector3.zero;
            }

            float speedMultiplier = Mathf.Clamp(speed / 5f, 0.65f, 1.6f);
            bobTimer += Time.deltaTime * bobFrequency * speedMultiplier;

            return new Vector3(
                Mathf.Cos(bobTimer * 0.5f) * bobPositionAmount * 0.5f,
                Mathf.Sin(bobTimer) * bobPositionAmount,
                0f);
        }

        private Vector3 CalculateMovementBobRotation()
        {
            if (characterController == null || !characterController.isGrounded)
            {
                return Vector3.zero;
            }

            Vector3 planarVelocity = characterController.velocity;
            planarVelocity.y = 0f;

            if (planarVelocity.magnitude < minBobSpeed)
            {
                return Vector3.zero;
            }

            return new Vector3(
                Mathf.Sin(bobTimer) * bobRotationAmount,
                0f,
                Mathf.Cos(bobTimer * 0.5f) * bobRotationAmount);
        }

        private Vector3 CalculateVerticalMotionPosition()
        {
            if (characterController == null || characterController.isGrounded)
            {
                return Vector3.zero;
            }

            float offsetY = Mathf.Clamp(-characterController.velocity.y * verticalMotionAmount, -maxVerticalMotion, maxVerticalMotion);
            return new Vector3(0f, offsetY, 0f);
        }

        private Vector3 CalculateVerticalMotionRotation()
        {
            if (characterController == null || characterController.isGrounded)
            {
                return Vector3.zero;
            }

            float pitch = Mathf.Clamp(characterController.velocity.y * airRotationAmount, -maxRecoilRotation, maxRecoilRotation);
            return new Vector3(pitch, 0f, 0f);
        }

        private void AddRecoil()
        {
            recoilPositionOffset = Vector3.ClampMagnitude(recoilPositionOffset + recoilPositionKick, maxRecoilPosition);
            recoilRotationOffset = Vector3.ClampMagnitude(recoilRotationOffset + recoilRotationKick, maxRecoilRotation);
        }

        private Vector3 CalculateReloadPosition()
        {
            return reloadPositionOffset * CalculateReloadBlend();
        }

        private Vector3 CalculateReloadRotation()
        {
            return reloadRotationOffset * CalculateReloadBlend();
        }

        private float CalculateReloadBlend()
        {
            if (!reloadMotionActive || reloadDuration <= 0f)
            {
                return 0f;
            }

            float progress = Mathf.Clamp01((Time.time - reloadStartTime) / reloadDuration);
            float dipInEnd = Mathf.Clamp01(reloadDipInPercent);
            float returnStart = Mathf.Clamp01(1f - reloadReturnPercent);

            if (progress >= 1f)
            {
                reloadMotionActive = false;
                return 0f;
            }

            if (progress < dipInEnd)
            {
                return Mathf.SmoothStep(0f, 1f, progress / Mathf.Max(0.01f, dipInEnd));
            }

            if (progress > returnStart)
            {
                return Mathf.SmoothStep(1f, 0f, (progress - returnStart) / Mathf.Max(0.01f, 1f - returnStart));
            }

            return 1f;
        }

        private void StartReloadMotion(float duration)
        {
            reloadStartTime = Time.time;
            reloadDuration = Mathf.Max(0.05f, duration);
            reloadMotionActive = true;
        }

        private void FinishReloadMotion()
        {
            reloadMotionActive = false;
        }
    }
}
