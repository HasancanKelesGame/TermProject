using UnityEngine;
using UnityEngine.AI;

namespace TermProject.AI
{
    [DisallowMultipleComponent]
    public sealed class BotVisualController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BotAI botAI;
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private Animator animator;
        [SerializeField] private ParticleSystem muzzleFlash;
        [SerializeField] private Light muzzleLight;

        [Header("Animator Parameters")]
        [SerializeField] private string horizontalParameter = "X";
        [SerializeField] private string forwardParameter = "Y";
        [SerializeField] private string speedParameter = "Speed";
        [SerializeField] private string onGroundParameter = "OnGround";
        [SerializeField] private string aimingParameter = "Aiming";
        [SerializeField] private string shootParameter = "Shoot";
        [SerializeField] private string deadParameter = "Dead";

        [Header("Animation Tuning")]
        [SerializeField] private bool alwaysAim = true;
        [SerializeField] private float maxAnimationSpeed = 4f;
        [SerializeField] private float parameterDampTime = 0.12f;
        [SerializeField] private bool disableRootMotion = true;

        [Header("Muzzle Flash")]
        [SerializeField] private bool useParticleMuzzleFlash;
        [SerializeField] private float muzzleLightDuration = 0.05f;
        [SerializeField] private Color muzzleFlashColor = new Color(1f, 0.45f, 0.05f, 1f);
        [SerializeField] private bool configureMuzzleFlashOnAwake = true;
        [SerializeField] private bool forceRuntimeMuzzleFlashMaterial = true;

        private int horizontalHash;
        private int forwardHash;
        private int speedHash;
        private int onGroundHash;
        private int aimingHash;
        private int shootHash;
        private int deadHash;
        private float muzzleLightOffTime;

        private void Awake()
        {
            if (botAI == null)
            {
                botAI = GetComponentInParent<BotAI>();
            }

            if (agent == null)
            {
                agent = GetComponentInParent<NavMeshAgent>();
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            CacheAnimatorHashes();

            if (animator != null && disableRootMotion)
            {
                animator.applyRootMotion = false;
            }

            if (muzzleLight != null)
            {
                muzzleLight.enabled = false;
            }

            if (useParticleMuzzleFlash && configureMuzzleFlashOnAwake)
            {
                ConfigureMuzzleFlash();
            }
        }

        private void OnEnable()
        {
            if (botAI != null)
            {
                botAI.AttackPerformed += PlayShootVisuals;
            }
        }

        private void OnDisable()
        {
            if (botAI != null)
            {
                botAI.AttackPerformed -= PlayShootVisuals;
            }
        }

        private void Update()
        {
            UpdateAnimator();
            UpdateMuzzleLight();
        }

        private void CacheAnimatorHashes()
        {
            horizontalHash = Animator.StringToHash(horizontalParameter);
            forwardHash = Animator.StringToHash(forwardParameter);
            speedHash = Animator.StringToHash(speedParameter);
            onGroundHash = Animator.StringToHash(onGroundParameter);
            aimingHash = Animator.StringToHash(aimingParameter);
            shootHash = Animator.StringToHash(shootParameter);
            deadHash = Animator.StringToHash(deadParameter);
        }

        private void UpdateAnimator()
        {
            if (animator == null)
            {
                return;
            }

            Vector3 worldVelocity = agent != null ? agent.velocity : Vector3.zero;
            Vector3 localVelocity = transform.InverseTransformDirection(worldVelocity);
            float animationSpeed = maxAnimationSpeed > 0f
                ? Mathf.Clamp01(worldVelocity.magnitude / maxAnimationSpeed)
                : worldVelocity.magnitude;
            bool dead = botAI != null && botAI.CurrentState == BotAI.BotState.Dead;
            bool aiming = alwaysAim || botAI != null && botAI.CurrentState != BotAI.BotState.Patrol;

            animator.SetFloat(horizontalHash, Mathf.Clamp(localVelocity.x / Mathf.Max(0.1f, maxAnimationSpeed), -1f, 1f), parameterDampTime, Time.deltaTime);
            animator.SetFloat(forwardHash, Mathf.Clamp(localVelocity.z / Mathf.Max(0.1f, maxAnimationSpeed), -1f, 1f), parameterDampTime, Time.deltaTime);
            animator.SetFloat(speedHash, animationSpeed, parameterDampTime, Time.deltaTime);
            animator.SetBool(onGroundHash, true);
            animator.SetBool(aimingHash, aiming && !dead);
            animator.SetBool(deadHash, dead);
        }

        private void PlayShootVisuals()
        {
            if (animator != null)
            {
                animator.SetTrigger(shootHash);
            }

            if (useParticleMuzzleFlash && muzzleFlash != null)
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

        private void ConfigureMuzzleFlash()
        {
            if (muzzleFlash == null)
            {
                return;
            }

            ParticleSystem.MainModule main = muzzleFlash.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.08f;
            main.startLifetime = 0.05f;
            main.startSpeed = 0.15f;
            main.startSize = 0.18f;
            main.startColor = muzzleFlashColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = muzzleFlash.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, 6)
            });

            ParticleSystem.ShapeModule shape = muzzleFlash.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 8f;
            shape.radius = 0.02f;

            ParticleSystemRenderer renderer = muzzleFlash.GetComponent<ParticleSystemRenderer>();

            if (renderer == null)
            {
                return;
            }

            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.maxParticleSize = 0.08f;

            if (forceRuntimeMuzzleFlashMaterial
                || renderer.sharedMaterial == null
                || renderer.sharedMaterial.shader == null
                || renderer.sharedMaterial.shader.name == "Hidden/InternalErrorShader")
            {
                Shader shader = Shader.Find("Legacy Shaders/Particles/Additive");

                if (shader == null)
                {
                    shader = Shader.Find("Particles/Standard Unlit");
                }

                if (shader != null)
                {
                    Material material = new Material(shader)
                    {
                        color = muzzleFlashColor
                    };

                    renderer.material = material;
                }
            }
        }

        private void UpdateMuzzleLight()
        {
            if (muzzleLight != null && muzzleLight.enabled && Time.time >= muzzleLightOffTime)
            {
                muzzleLight.enabled = false;
            }
        }
    }
}
