using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FiveNightAnomaly.Combat
{
    /// <summary>
    /// Calls lightning from the sky: Strikes bolts, one after another, on the targets ahead (nearest first, again from
    /// the nearest when there are fewer targets than bolts). With no target in range they land in a row in front of the
    /// caster. Every bolt hits what stands around its strike point.
    /// </summary>
    [CreateAssetMenu(menuName = "5 Night Anomaly/Attacks/Sky Strike", fileName = "AttackSkyStrike")]
    public sealed class SkyStrikeAttack : Attack
    {
        [SerializeField, Min(1)] private int strikes = 3;
        [Tooltip("How far ahead targets are looked for, in units.")]
        [SerializeField, Min(0.5f)] private float range = 7f;
        [Tooltip("Distance between bolts when nothing is in range.")]
        [SerializeField, Min(0.1f)] private float spacing = 1.6f;
        [Tooltip("Seconds between bolts.")]
        [SerializeField, Min(0f)] private float interval = 0.15f;
        [Tooltip("Half size of the hit box around a strike point: x along the lane, y up and down the lane.")]
        [SerializeField] private Vector2 radius = new Vector2(0.6f, 0.45f);
        [Tooltip("LightningSky effect; its sprite centre is Lightning Height above the strike point.")]
        [SerializeField] private GameObject lightning;
        [SerializeField] private float lightningHeight = 4f;
        [SerializeField] private AudioClip strikeSound;

        public override void Release(in AttackContext context, float pitchJitter)
        {
            context.Play(ReleaseSound, Volume, Pitch * pitchJitter);
            context.Owner.StartCoroutine(Strike(context, StrikePoints(context)));
        }

        private List<Vector2> StrikePoints(in AttackContext context)
        {
            var ahead = new Vector2(range, 4f);
            Vector2 centre = context.Feet + new Vector2(range / 2f * context.Facing, 0f);
            var targets = HitArea.Find(centre, ahead, context.Owner.transform, context.Feet);
            var points = new List<Vector2>(strikes);
            for (int i = 0; i < strikes; i++)
            {
                if (targets.Count > 0)
                {
                    Vector2 feet = HitArea.FeetOf(targets[i % targets.Count]);
                    float jitter = i < targets.Count ? 0f : ((i % 2 == 0 ? 1f : -1f) * 0.22f);
                    points.Add(feet + new Vector2(jitter, 0f));
                }
                else
                {
                    points.Add(context.Feet + new Vector2(spacing * (i + 1) * context.Facing, 0f));
                }
            }
            return points;
        }

        private IEnumerator Strike(AttackContext context, List<Vector2> points)
        {
            foreach (Vector2 point in points)
            {
                Spawn(lightning, point + new Vector2(0f, lightningHeight), context.Facing);
                context.Play(strikeSound, Volume, 1f);
                var targets = HitArea.Find(point, radius * 2f, context.Owner != null ? context.Owner.transform : null, point);
                for (int i = 0; i < targets.Count; i++)
                    targets[i].TakeHit(HitAt(point + new Vector2(0f, 0.6f), context.Facing));
                if (interval > 0f)
                    yield return new WaitForSeconds(interval);
            }
        }

        public override void DrawGizmos(Vector2 feet, float facing)
        {
            Gizmos.DrawWireCube(feet + new Vector2(range / 2f * facing, 0f), new Vector2(range, 4f));
        }
    }
}
