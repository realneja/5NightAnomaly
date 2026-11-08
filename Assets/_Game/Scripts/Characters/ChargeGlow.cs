using UnityEngine;

namespace FiveNightAnomaly.Characters
{
    /// <summary>
    /// Light around the Pengembara's lantern while the charge attack is held: it grows as the charge fills, pops once
    /// when the charge is ready and flickers until the button is let go. Sits on a child with a SpriteRenderer and a
    /// looping SpriteFlipbook (GlowCharge frames).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class ChargeGlow : MonoBehaviour
    {
        [SerializeField] private PengembaraController controller;
        [Tooltip("Lantern centre in the Charge pose, facing right, from the feet (chargeLantern in Pengembara_Anim.json).")]
        [SerializeField] private Vector2 offset = new Vector2(0.71f, 0.73f);
        [Tooltip("Scale while charging: x at the start, y when the charge is ready.")]
        [SerializeField] private Vector2 scale = new Vector2(0.5f, 1.25f);
        [Tooltip("Extra size of the pop when the charge becomes ready (0.45 = 45 percent bigger for a moment).")]
        [SerializeField, Min(0f)] private float readyPop = 0.45f;
        [SerializeField, Min(0.01f)] private float popTime = 0.2f;

        private SpriteRenderer glow;
        private float readySince = -1f;
        private float sizeScale = 1f;

        private void Awake()
        {
            glow = GetComponent<SpriteRenderer>();
            glow.enabled = false;
        }

        /// <summary>Look of the equipped hand item: glow frames, centre (facing right, from the feet) and size.</summary>
        public void SetLook(Sprite[] frames, Vector2 centre, float size)
        {
            if (frames != null && frames.Length > 0 && TryGetComponent(out FiveNightAnomaly.Effects.SpriteFlipbook flipbook))
                flipbook.SetFrames(frames);
            offset = centre;
            sizeScale = size;
        }

        private void LateUpdate()
        {
            bool charging = controller != null && controller.IsCharging;
            glow.enabled = charging;
            if (!charging)
            {
                readySince = -1f;
                return;
            }

            float facing = controller.FacingLeft ? -1f : 1f;
            transform.localPosition = new Vector3(offset.x * facing, offset.y, 0f);
            float progress = controller.ChargeProgress;
            float size = Mathf.Lerp(scale.x, scale.y, progress);
            float alpha = Mathf.Lerp(0.55f, 1f, progress);
            if (controller.IsChargeReady)
            {
                if (readySince < 0f)
                    readySince = Time.time;
                float t = (Time.time - readySince) / popTime;
                if (t < 1f)
                    size *= 1f + readyPop * (1f - t);
                alpha = 0.85f + 0.15f * Mathf.Sin(Time.time * 40f);
            }
            size *= sizeScale;
            transform.localScale = new Vector3(size, size, 1f);
            Color color = glow.color;
            color.a = alpha;
            glow.color = color;
        }
    }
}
