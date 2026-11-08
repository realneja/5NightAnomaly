using UnityEngine;

namespace FiveNightAnomaly.Combat
{
    /// <summary>
    /// Shoots a projectile from the tip along the lane: the Zeus bolt and the wizard fireballs. It flies at the height it
    /// left the hand but hits on the lane (targets up to Depth above or below the shooter). Area 0 hits the first target
    /// only; an area such as 3 x 3 hits everything in that many tiles around the impact (the charged fireball).
    /// </summary>
    [CreateAssetMenu(menuName = "5 Night Anomaly/Attacks/Projectile", fileName = "AttackProjectile")]
    public sealed class ProjectileAttack : Attack
    {
        [SerializeField] private Projectile projectile;
        [Tooltip("Units per second.")]
        [SerializeField, Min(0.1f)] private float speed = 8f;
        [Tooltip("How far it flies before it bursts on its own, in units (1 tile = 1 unit).")]
        [SerializeField, Min(0.1f)] private float range = 10f;
        [Tooltip("How far up or down the lane a target may stand and still be hit.")]
        [SerializeField, Min(0.01f)] private float depth = 0.35f;
        [Tooltip("0 x 0 = only the first target; otherwise the size in tiles of the area hit around the impact.")]
        [SerializeField] private Vector2 area = Vector2.zero;
        [SerializeField, Min(0.1f)] private float scale = 1f;
        [Tooltip("Effect where it lands.")]
        [SerializeField] private GameObject impactEffect;
        [Tooltip("On: the effect sits on the ground at the impact (the blast). Off: at the projectile's height.")]
        [SerializeField] private bool effectOnGround;
        [SerializeField] private AudioClip impactSound;

        public float Speed => speed;
        public float Range => range;
        public float Depth => depth;
        public Vector2 Area => area;

        public override void Release(in AttackContext context, float pitchJitter)
        {
            context.Play(ReleaseSound, Volume, Pitch * pitchJitter);
            if (projectile == null)
                return;
            Projectile shot = Instantiate(projectile, context.Point(Tip), Quaternion.identity);
            shot.transform.localScale = new Vector3(scale, scale, 1f);
            shot.Launch(this, context);
        }

        /// <summary>Called by the projectile where it lands (target hit or end of range).</summary>
        public void Land(Projectile shot, in AttackContext context, Vector2 position, IHittable firstTarget)
        {
            var ground = new Vector2(position.x, context.Feet.y);
            if (area.x > 0f && area.y > 0f)
            {
                var targets = HitArea.Find(ground, area, context.Owner != null ? context.Owner.transform : null, ground);
                for (int i = 0; i < targets.Count; i++)
                    targets[i].TakeHit(HitAt(position, context.Facing));
            }
            else
            {
                firstTarget?.TakeHit(HitAt(position, context.Facing));
            }
            Spawn(impactEffect, effectOnGround ? ground : position, context.Facing);
            context.Play(impactSound, Volume, 1f);
        }

        public override void DrawGizmos(Vector2 feet, float facing)
        {
            base.DrawGizmos(feet, facing);
            Vector2 start = feet + new Vector2(Tip.x * facing, 0f);
            Gizmos.DrawLine(start, start + new Vector2(range * facing, 0f));
            if (area.x > 0f && area.y > 0f)
                Gizmos.DrawWireCube(start + new Vector2(range * facing, 0f), area);
        }
    }
}
