using UnityEngine;

namespace FiveNightAnomaly.Effects
{
    /// <summary>
    /// Shakes this transform left and right for a moment and lets it settle (a heavy hit). Put it on the child that
    /// holds the sprite, so the collider and the health bar stay where they are.
    /// </summary>
    public sealed class HitShake : MonoBehaviour
    {
        [Tooltip("Largest sideways offset in units.")]
        [SerializeField, Min(0f)] private float amplitude = 0.05f;
        [SerializeField, Min(0.01f)] private float duration = 0.3f;
        [Tooltip("Shakes per second.")]
        [SerializeField, Min(1f)] private float frequency = 22f;

        private Vector3 rest;
        private float elapsed = -1f;
        private float direction = 1f;

        private void Awake()
        {
            rest = transform.localPosition;
        }

        /// <summary>Starts the shake; the first push goes in <paramref name="pushDirection"/> (+1 = to the right).</summary>
        public void Play(float pushDirection = 1f)
        {
            elapsed = 0f;
            direction = pushDirection < 0f ? -1f : 1f;
        }

        private void Update()
        {
            if (elapsed < 0f)
                return;
            elapsed += Time.deltaTime;
            if (elapsed >= duration)
            {
                elapsed = -1f;
                transform.localPosition = rest;
                return;
            }
            float fade = 1f - elapsed / duration;
            float offset = amplitude * fade * Mathf.Cos(elapsed * frequency * 2f * Mathf.PI) * direction;
            transform.localPosition = rest + new Vector3(offset, 0f, 0f);
        }
    }
}
