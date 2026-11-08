using UnityEngine;

namespace FiveNightAnomaly.Combat
{
    /// <summary>
    /// A flying shot made by a ProjectileAttack. Moves straight along the lane, checks the stretch it covered each frame
    /// (so a fast bolt never skips a target) and lands on the first target or at the end of its range.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class Projectile : MonoBehaviour
    {
        private ProjectileAttack attack;
        private AttackContext context;
        private float travelled;
        private bool landed;

        public void Launch(ProjectileAttack source, in AttackContext from)
        {
            attack = source;
            context = from;
            GetComponent<SpriteRenderer>().flipX = from.Facing < 0f;
        }

        private void Update()
        {
            if (attack == null || landed)
                return;
            float step = attack.Speed * Time.deltaTime;
            Vector3 position = transform.position;
            float next = Mathf.Min(travelled + step, attack.Range);
            float moved = next - travelled;
            float startX = position.x;
            position.x += moved * context.Facing;
            transform.position = position;
            travelled = next;

            var sweep = new Vector2(Mathf.Abs(moved) + 0.3f, attack.Depth * 2f);
            var centre = new Vector2((startX + position.x) / 2f, context.Feet.y);
            var targets = HitArea.Find(centre, sweep, context.Owner != null ? context.Owner.transform : null, new Vector2(startX, context.Feet.y));
            if (targets.Count > 0)
                Land(new Vector2(HitArea.FeetOf(targets[0]).x - 0.15f * context.Facing, position.y), targets[0]);
            else if (travelled >= attack.Range)
                Land(position, null);
        }

        private void Land(Vector2 point, IHittable target)
        {
            landed = true;
            attack.Land(this, context, point, target);
            Destroy(gameObject);
        }
    }
}
