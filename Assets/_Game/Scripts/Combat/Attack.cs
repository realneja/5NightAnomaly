using UnityEngine;

namespace FiveNightAnomaly.Combat
{
    /// <summary>
    /// One attack of a hand item: its normal attack or its charge attack. The hand's animation clips drive it through
    /// HandCombat: the swing event plays the whoosh, the release event (impact frame) makes the attack take effect.
    /// Subclasses decide what that is: MeleeAttack (hit what is in reach), ProjectileAttack (bolt, fireball),
    /// SkyStrikeAttack (lightning from the sky).
    /// </summary>
    public abstract class Attack : ScriptableObject
    {
        [SerializeField, Min(0)] private int damage = 10;
        [Tooltip("Heavy hits make the target shake.")]
        [SerializeField] private bool heavy;
        [Tooltip("Where the attack comes out, facing right, in units from the feet (attackTip / chargeTip in Pengembara_Anim.json).")]
        [SerializeField] private Vector2 tip = new Vector2(1f, 0.5f);

        [Header("Sound")]
        [SerializeField] private AudioClip swingSound;
        [Tooltip("Played on the release frame (melee: the hit; leave empty for projectiles, which sound when they land).")]
        [SerializeField] private AudioClip releaseSound;
        [SerializeField, Range(0f, 1f)] private float volume = 0.8f;
        [Tooltip("Below 1 sounds deeper and heavier (the charge attacks reuse sounds this way).")]
        [SerializeField, Range(0.5f, 1.5f)] private float pitch = 1f;

        public int Damage => damage;
        public bool Heavy => heavy;
        public Vector2 Tip => tip;
        public AudioClip SwingSound => swingSound;
        public AudioClip ReleaseSound => releaseSound;
        public float Volume => volume;
        public float Pitch => pitch;

        /// <summary>The release frame: the attack takes effect.</summary>
        public abstract void Release(in AttackContext context, float pitchJitter);

        /// <summary>Editor help: draws where the attack reaches (selected Pengembara, Scene view).</summary>
        public virtual void DrawGizmos(Vector2 feet, float facing)
        {
            Gizmos.DrawWireSphere(feet + new Vector2(tip.x * facing, tip.y), 0.05f);
        }

        protected HitInfo HitAt(Vector2 point, float facing) => new HitInfo(damage, heavy, point, facing);

        protected static void Spawn(GameObject prefab, Vector2 point, float facing, float scale = 1f)
        {
            if (prefab == null)
                return;
            GameObject effect = Instantiate(prefab, point, Quaternion.identity);
            if (!Mathf.Approximately(scale, 1f))
                effect.transform.localScale = new Vector3(scale, scale, 1f);
            if (effect.TryGetComponent(out SpriteRenderer effectRenderer))
                effectRenderer.flipX = facing < 0f;
        }
    }
}
