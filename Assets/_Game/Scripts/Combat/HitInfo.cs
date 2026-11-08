using UnityEngine;

namespace FiveNightAnomaly.Combat
{
    /// <summary>
    /// What one attack does to one target: damage, whether it is a heavy hit (the charge attack), where it landed in
    /// the world and which way it pushes (+1 = from left to right, -1 = from right to left).
    /// </summary>
    public readonly struct HitInfo
    {
        public HitInfo(int damage, bool heavy, Vector2 point, float direction)
        {
            Damage = damage;
            Heavy = heavy;
            Point = point;
            Direction = direction;
        }

        public int Damage { get; }
        public bool Heavy { get; }
        public Vector2 Point { get; }
        public float Direction { get; }
    }
}
