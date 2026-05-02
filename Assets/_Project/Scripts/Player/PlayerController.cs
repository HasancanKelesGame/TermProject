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

        [Header("Mouse Look")]
        [SerializeField] private float mouseSensitivity = 2.2f;
        [SerializeField] private float minPitch = -80f;
        [SerializeField] private float maxPitch = 80f;

        private CharacterController characterController;
        private float verticalVelocity;
        private float cameraPitch;
        private bool controlsEnabled = true;
        private bool grounded;
        private bool jumpedSinceGrounded;
        private float lastGroundedTime = -999f;
        private float lastJumpPressedTime = -999f;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();

            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }
        }

        private void Start()
        {
            SetCursorLocked(true);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SetCursorLocked(Cursor.lockState != CursorLockMode.Locked);
            }

            if (!controlsEnabled)
            {
                return;
            }

            Look();
            Move();
        }

        public void SetControlsEnabled(bool enabled)
        {
            controlsEnabled = enabled;

            if (!enabled)
            {
                verticalVelocity = 0f;
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

            if (input.sqrMagnitude > 1f)
            {
                input.Normalize();
            }

            float targetSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;
            Vector3 move = transform.right * input.x + transform.forward * input.z;

            bool canUseGroundedJump = Time.time - lastGroundedTime <= groundedGraceTime && !jumpedSinceGrounded;
            bool hasBufferedJump = Time.time - lastJumpPressedTime <= jumpBufferTime;

            if (hasBufferedJump && canUseGroundedJump)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                jumpedSinceGrounded = true;
                lastJumpPressedTime = -999f;
            }

            verticalVelocity += gravity * Time.deltaTime;

            Vector3 frameMotion = move * targetSpeed;
            frameMotion.y = verticalVelocity;
            CollisionFlags collisionFlags = characterController.Move(frameMotion * Time.deltaTime);

            grounded = ((collisionFlags & CollisionFlags.Below) != 0) || (verticalVelocity <= 0f && ProbeGround());

            if (grounded)
            {
                lastGroundedTime = Time.time;
                jumpedSinceGrounded = false;

                if (verticalVelocity < 0f)
                {
                    verticalVelocity = -2f;
                }
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
    }
}
