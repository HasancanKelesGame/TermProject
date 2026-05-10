using UnityEngine;

namespace TermProject.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera playerCamera;

        [Header("Movement")]
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float sprintSpeed = 8f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float jumpHeight = 1.1f;
        [SerializeField] private float groundedGraceTime = 0.08f;
        [SerializeField] private float jumpBufferTime = 0.08f;
        [SerializeField] private float groundProbeDistance = 0.22f;
        [SerializeField] private LayerMask groundLayers = ~0;

        [Header("Crouch")]
        [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;
        [SerializeField] private bool allowRightControlCrouch = true;
        [SerializeField] private float crouchHeight = 0.95f;
        [SerializeField] private float crouchSpeedMultiplier = 0.55f;
        [SerializeField] private float crouchCameraYOffset = -0.85f;
        [SerializeField] private float crouchTransitionSpeed = 18f;
        [SerializeField] private LayerMask crouchBlockLayers = ~0;

        [Header("Mouse Look")]
        [SerializeField] private float mouseSensitivity = 2.2f;
        [SerializeField] private float minPitch = -80f;
        [SerializeField] private float maxPitch = 80f;

        [Header("Audio")]
        [SerializeField] private AudioSource movementAudioSource;
        [SerializeField] private AudioSource movementLoopAudioSource;
        [SerializeField] private AudioClip walkFootstepSound;
        [SerializeField] private AudioClip runFootstepSound;
        [SerializeField] private AudioClip jumpSound;
        [SerializeField] private AudioClip landSound;
        [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.45f;
        [SerializeField, Range(0f, 1f)] private float jumpVolume = 0.65f;
        [SerializeField, Range(0f, 1f)] private float landVolume = 0.55f;
        [SerializeField, Min(0.1f)] private float walkLoopPitch = 1f;
        [SerializeField, Min(0.1f)] private float runLoopPitch = 1.12f;
        [SerializeField, Min(0f)] private float minAirTimeForLandingSound = 0.15f;

        [Header("Runtime Debug")]
        [SerializeField] private bool debugIsCrouching;
        [SerializeField] private float debugControllerHeight;
        [SerializeField] private Vector3 debugControllerCenter;
        [SerializeField] private Vector3 debugCameraLocalPosition;

        private CharacterController characterController;
        private float verticalVelocity;
        private float cameraPitch;
        private bool controlsEnabled = true;
        private bool grounded;
        private bool jumpedSinceGrounded;
        private float lastGroundedTime = -999f;
        private float lastJumpPressedTime = -999f;
        private float lastLeftGroundTime = -999f;
        private float standingHeight;
        private Vector3 standingCenter;
        private Vector3 standingCameraLocalPosition;
        private Vector3 targetCameraLocalPosition;
        private bool crouching;
        private bool hasGroundedState;

        public bool IsCrouching => crouching;
        public Vector3 BotTargetPoint => playerCamera != null
            ? playerCamera.transform.position
            : transform.position + Vector3.up * (characterController != null ? characterController.center.y : 1.1f);

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            standingHeight = characterController.height;
            standingCenter = characterController.center;

            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }

            if (playerCamera != null)
            {
                standingCameraLocalPosition = playerCamera.transform.localPosition;
                targetCameraLocalPosition = standingCameraLocalPosition;
            }

            if (movementAudioSource == null)
            {
                movementAudioSource = GetComponent<AudioSource>();
            }

            if (movementAudioSource == null)
            {
                movementAudioSource = gameObject.AddComponent<AudioSource>();
            }

            movementAudioSource.playOnAwake = false;
            movementAudioSource.spatialBlend = 0f;

            if (movementLoopAudioSource == null)
            {
                movementLoopAudioSource = gameObject.AddComponent<AudioSource>();
            }

            movementLoopAudioSource.playOnAwake = false;
            movementLoopAudioSource.spatialBlend = 0f;
            movementLoopAudioSource.loop = true;
        }

        private void Start()
        {
            SetCursorLocked(true);
        }

        private void Update()
        {
            if (!controlsEnabled)
            {
                return;
            }

            Look();
            Move();
            UpdateCrouchDebugValues();
        }

        private void LateUpdate()
        {
            ApplyCrouchCameraPosition();
        }

        public void SetControlsEnabled(bool enabled, bool resetVerticalVelocity = true)
        {
            controlsEnabled = enabled;

            if (!enabled)
            {
                if (resetVerticalVelocity)
                {
                    verticalVelocity = 0f;
                }

                StopMovementLoop();
            }
        }

        public void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void Look()
        {
            if (playerCamera == null || Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            transform.Rotate(Vector3.up * mouseX);

            cameraPitch -= mouseY;
            cameraPitch = Mathf.Clamp(cameraPitch, minPitch, maxPitch);
            playerCamera.transform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }

        private void Move()
        {
            bool jumpPressed = Input.GetButtonDown("Jump");
            bool wasGrounded = grounded;

            if (jumpPressed)
            {
                lastJumpPressedTime = Time.time;
            }

            if (grounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            Vector3 input = new Vector3(horizontal, 0f, vertical);
            bool hasMoveInput = input.sqrMagnitude > 0.01f;

            if (input.sqrMagnitude > 1f)
            {
                input.Normalize();
            }

            UpdateCrouch();

            bool sprinting = !crouching && Input.GetKey(KeyCode.LeftShift) && hasMoveInput;
            float targetSpeed = sprinting ? sprintSpeed : walkSpeed;

            if (crouching)
            {
                targetSpeed *= crouchSpeedMultiplier;
            }

            Vector3 move = transform.right * input.x + transform.forward * input.z;

            bool canUseGroundedJump = Time.time - lastGroundedTime <= groundedGraceTime && !jumpedSinceGrounded;
            bool hasBufferedJump = Time.time - lastJumpPressedTime <= jumpBufferTime;

            if (hasBufferedJump && canUseGroundedJump)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                jumpedSinceGrounded = true;
                lastJumpPressedTime = -999f;
                StopMovementLoop();
                PlayMovementSound(jumpSound, jumpVolume);
            }

            verticalVelocity += gravity * Time.deltaTime;

            Vector3 frameMotion = move * targetSpeed;
            frameMotion.y = verticalVelocity;
            CollisionFlags collisionFlags = characterController.Move(frameMotion * Time.deltaTime);

            grounded = ((collisionFlags & CollisionFlags.Below) != 0) || (verticalVelocity <= 0f && ProbeGround());

            if (!hasGroundedState)
            {
                wasGrounded = grounded;
                hasGroundedState = true;
            }

            if (!grounded && wasGrounded)
            {
                lastLeftGroundTime = Time.time;
            }

            if (grounded)
            {
                bool landed = !wasGrounded && Time.time - lastLeftGroundTime >= minAirTimeForLandingSound;

                if (landed)
                {
                    PlayMovementSound(landSound, landVolume);
                }

                lastGroundedTime = Time.time;
                jumpedSinceGrounded = false;

                if (verticalVelocity < 0f)
                {
                    verticalVelocity = -2f;
                }
            }

            UpdateMovementLoop(hasMoveInput, sprinting);
        }

        private void UpdateCrouch()
        {
            bool wantsCrouch = Input.GetKey(crouchKey)
                || allowRightControlCrouch && Input.GetKey(KeyCode.RightControl);

            if (wantsCrouch)
            {
                crouching = true;
            }
            else if (crouching && CanStand())
            {
                crouching = false;
            }

            float targetHeight = crouching ? Mathf.Clamp(crouchHeight, characterController.radius * 2f, standingHeight) : standingHeight;
            float standingBottom = standingCenter.y - standingHeight * 0.5f;
            Vector3 targetCenter = standingCenter;
            targetCenter.y = standingBottom + targetHeight * 0.5f;

            float blend = 1f - Mathf.Exp(-crouchTransitionSpeed * Time.deltaTime);
            characterController.height = Mathf.Lerp(characterController.height, targetHeight, blend);
            characterController.center = Vector3.Lerp(characterController.center, targetCenter, blend);

            targetCameraLocalPosition = standingCameraLocalPosition;

            if (crouching)
            {
                targetCameraLocalPosition += Vector3.up * crouchCameraYOffset;
            }
        }

        private void ApplyCrouchCameraPosition()
        {
            if (playerCamera == null || !controlsEnabled)
            {
                return;
            }

            float blend = 1f - Mathf.Exp(-crouchTransitionSpeed * Time.deltaTime);
            playerCamera.transform.localPosition = Vector3.Lerp(
                playerCamera.transform.localPosition,
                targetCameraLocalPosition,
                blend);
        }

        private bool CanStand()
        {
            float radius = characterController.radius;
            float capsuleHeight = Mathf.Max(standingHeight, radius * 2f);
            Vector3 capsuleCenter = transform.TransformPoint(standingCenter);
            Vector3 up = transform.up;
            float halfLine = Mathf.Max(0f, capsuleHeight * 0.5f - radius);
            Vector3 bottom = capsuleCenter - up * halfLine;
            Vector3 top = capsuleCenter + up * halfLine;
            Collider[] hits = Physics.OverlapCapsule(
                bottom,
                top,
                radius,
                crouchBlockLayers,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] != characterController && !hits[i].transform.IsChildOf(transform))
                {
                    return false;
                }
            }

            return true;
        }

        private void UpdateCrouchDebugValues()
        {
            debugIsCrouching = crouching;

            if (characterController != null)
            {
                debugControllerHeight = characterController.height;
                debugControllerCenter = characterController.center;
            }

            if (playerCamera != null)
            {
                debugCameraLocalPosition = playerCamera.transform.localPosition;
            }
        }

        private bool ProbeGround()
        {
            Vector3 origin = transform.position + Vector3.up * 0.08f;
            return Physics.Raycast(
                origin,
                Vector3.down,
                groundProbeDistance,
                groundLayers,
                QueryTriggerInteraction.Ignore);
        }

        private void UpdateMovementLoop(bool hasMoveInput, bool sprinting)
        {
            if (!grounded || !hasMoveInput)
            {
                StopMovementLoop();
                return;
            }

            AudioClip clip = sprinting && runFootstepSound != null ? runFootstepSound : walkFootstepSound;

            if (clip == null || movementLoopAudioSource == null)
            {
                StopMovementLoop();
                return;
            }

            movementLoopAudioSource.volume = footstepVolume;
            movementLoopAudioSource.pitch = sprinting ? runLoopPitch : walkLoopPitch;
            movementLoopAudioSource.loop = true;

            if (movementLoopAudioSource.clip == clip && movementLoopAudioSource.isPlaying)
            {
                return;
            }

            movementLoopAudioSource.clip = clip;
            movementLoopAudioSource.time = 0f;
            movementLoopAudioSource.Play();
        }

        private void PlayMovementSound(AudioClip clip, float volume)
        {
            if (clip != null && movementAudioSource != null)
            {
                movementAudioSource.PlayOneShot(clip, volume);
            }
        }

        private void StopMovementLoop()
        {
            if (movementLoopAudioSource == null || !movementLoopAudioSource.isPlaying)
            {
                return;
            }

            movementLoopAudioSource.Stop();
        }
    }
}
