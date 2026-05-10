using UnityEngine;

namespace TermProject.Game
{
    [DisallowMultipleComponent]
    public sealed class DamageHitbox : MonoBehaviour
    {
        [SerializeField] private Damageable damageable;
        [SerializeField, Min(0f)] private float damageMultiplier = 1f;
        [SerializeField] private bool criticalHit;
        [SerializeField] private AudioClip hitSound;
        [SerializeField, Range(0f, 1f)] private float hitSoundVolume = 0.8f;

        public Damageable Target => damageable;
        public float DamageMultiplier => damageMultiplier;
        public bool CriticalHit => criticalHit;
        public AudioClip HitSound => hitSound;
        public float HitSoundVolume => hitSoundVolume;

        private void Awake()
        {
            if (damageable == null)
            {
                damageable = GetComponentInParent<Damageable>();
            }
        }

        public bool ApplyDamage(float baseDamage, out Damageable damagedTarget)
        {
            damagedTarget = damageable;

            if (damageable == null)
            {
                return false;
            }

            damageable.ApplyDamage(baseDamage * damageMultiplier);
            return true;
        }
    }
}
