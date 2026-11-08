using UnityEngine;

namespace FiveNightAnomaly.Combat
{
    /// <summary>
    /// Hits what stands in front of the feet on the release frame: the lantern staff and the bare hands. The zone lies
    /// on the lane: Reach is how far forward it goes, Depth how far up or down the lane a target may stand. A spark (if
    /// set) appears where the hit lands, or at the tip when nothing is in reach.
    /// </summary>
    [CreateAssetMenu(menuName = "5 Night Anomaly/Attacks/Melee", fileName = "AttackMelee")]
    public sealed class MeleeAttack : Attack
    {
        [Tooltip("Start and end of the hit zone in front of the feet, in units.")]
        [SerializeField] private Vector2 reach = new Vector2(0.2f, 1.1f);
        [Tooltip("Half height of the hit zone: how far up or down the lane a target may stand.")]
        [SerializeField, Min(0.01f)] private float depth = 0.3f;
        [Tooltip("Effect where the hit lands. Empty = none.")]
        [SerializeField] private GameObject spark;
        [Tooltip("Play the release sound when nothing is hit (a staff hits the ground, a fist only sounds on a body).")]
        [SerializeField] private bool releaseSoundOnMiss = true;

        public override void Release(in AttackContext context, float pitchJitter)
        {
            Vector2 tip = context.Point(Tip);
            Vector2 centre = context.Feet + new Vector2((reach.x + reach.y) / 2f * context.Facing, 0f);
            var targets = HitArea.Find(centre, new Vector2(reach.y - reach.x, depth * 2f), context.Owner.transform, context.Feet);
            int hits = targets.Count;
            for (int i = 0; i < hits; i++)
            {
                IHittable target = targets[i];
                Vector2 feet = HitArea.FeetOf(target);
                float x = context.Facing > 0f ? Mathf.Min(tip.x, feet.x) : Mathf.Max(tip.x, feet.x);
                var point = new Vector2(x, feet.y + Tip.y);
                target.TakeHit(HitAt(point, context.Facing));
                Spawn(spark, point, context.Facing);
            }
            if (hits == 0)
                Spawn(spark, tip, context.Facing);
            if (hits > 0 || releaseSoundOnMiss)
                context.Play(ReleaseSound, Volume, Pitch * pitchJitter);
        }

        public override void DrawGizmos(Vector2 feet, float facing)
        {
            base.DrawGizmos(feet, facing);
            Gizmos.DrawWireCube(feet + new Vector2((reach.x + reach.y) / 2f * facing, 0f), new Vector2(reach.y - reach.x, depth * 2f));
        }
    }
}
