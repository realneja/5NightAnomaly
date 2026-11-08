using UnityEngine;

namespace FiveNightAnomaly.Combat
{
    /// <summary>
    /// Red health bar drawn in the world above an enemy or the test dummy (sprites BarEnemyFrame, BarEnemyTrail and
    /// BarEnemyFill in Sprites/UI/World, on the UI sorting layer). The red fill drops at once on a hit; the light trail
    /// behind it follows a moment later, so the size of the last hit stays visible. A refill sweeps the bar up again.
    /// </summary>
    public sealed class HealthBar : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private SpriteRenderer fill;
        [SerializeField] private SpriteRenderer trail;
        [Tooltip("Seconds the trail waits after a hit before it shrinks.")]
        [SerializeField, Min(0f)] private float trailDelay = 0.35f;
        [Tooltip("Speed of the trail and of a refill, in full bars per second.")]
        [SerializeField, Min(0.1f)] private float speed = 1.5f;

        private float width;
        private float shown = 1f;
        private float trailShown = 1f;
        private float trailWait;

        private void Awake()
        {
            width = fill.sprite.bounds.size.x;
        }

        private void LateUpdate()
        {
            float target = health.Fraction;
            if (target < shown)
            {
                shown = target;
                trailWait = trailDelay;
            }
            else
            {
                shown = Mathf.MoveTowards(shown, target, speed * Time.deltaTime);
            }

            if (trailShown <= shown)
                trailShown = shown;
            else if (trailWait > 0f)
                trailWait -= Time.deltaTime;
            else
                trailShown = Mathf.MoveTowards(trailShown, shown, speed * Time.deltaTime);

            Place(fill.transform, shown);
            Place(trail.transform, trailShown);
        }

        private void Place(Transform bar, float amount)
        {
            bar.localScale = new Vector3(amount, 1f, 1f);
            bar.localPosition = new Vector3(-width * (1f - amount) / 2f, 0f, 0f);
        }
    }
}
