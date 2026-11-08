using System.Collections.Generic;
using UnityEngine;

namespace FiveNightAnomaly.Combat
{
    /// <summary>
    /// Finds what can be hit inside a box on the lane (x = along the lane, y = up and down the lane). Colliders are the
    /// ground footprints of the targets, so the box is placed at feet height, not at the height of the swing.
    /// </summary>
    public static class HitArea
    {
        private static readonly Collider2D[] Found = new Collider2D[32];
        private static readonly List<IHittable> Hits = new List<IHittable>();
        private static ContactFilter2D filter = CreateFilter();

        /// <summary>Every target in the box once, nearest to <paramref name="from"/> first. The list is reused by the next call.</summary>
        public static IReadOnlyList<IHittable> Find(Vector2 centre, Vector2 size, Transform ignore, Vector2 from)
        {
            int count = Physics2D.OverlapBox(centre, size, 0f, filter, Found);
            Hits.Clear();
            for (int i = 0; i < count; i++)
            {
                IHittable target = Found[i].GetComponentInParent<IHittable>();
                if (target == null || Hits.Contains(target))
                    continue;
                if (ignore != null && ((Component)target).transform.IsChildOf(ignore))
                    continue;
                Hits.Add(target);
            }
            Hits.Sort((a, b) => Distance(a, from).CompareTo(Distance(b, from)));
            return Hits;
        }

        public static Vector2 FeetOf(IHittable target) => ((Component)target).transform.position;

        private static float Distance(IHittable target, Vector2 from) => (FeetOf(target) - from).sqrMagnitude;

        private static ContactFilter2D CreateFilter()
        {
            ContactFilter2D noFilter = ContactFilter2D.noFilter;
            noFilter.useTriggers = true;
            return noFilter;
        }
    }
}
