using UnityEngine;

namespace FiveNightAnomaly.Effects
{
    /// <summary>
    /// Plays a row of sprites on this SpriteRenderer at a fixed rate, for effects that need no Animator: a loop (the
    /// charge glow) or once (the spark), after which a one shot effect removes its GameObject.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SpriteFlipbook : MonoBehaviour
    {
        [SerializeField] private Sprite[] frames;
        [SerializeField, Min(1f)] private float framesPerSecond = 24f;
        [SerializeField] private bool loop;
        [Tooltip("Played once: destroy this GameObject after the last frame. Off = stay on the last frame.")]
        [SerializeField] private bool destroyAtEnd = true;

        private SpriteRenderer spriteRenderer;
        private float startTime;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            startTime = Time.time;
            if (frames != null && frames.Length > 0)
                spriteRenderer.sprite = frames[0];
        }

        /// <summary>Swaps the frames (a looping glow that changes with the hand item) and starts them from the first.</summary>
        public void SetFrames(Sprite[] newFrames)
        {
            frames = newFrames;
            startTime = Time.time;
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
            if (frames != null && frames.Length > 0)
                spriteRenderer.sprite = frames[0];
        }

        private void Update()
        {
            if (frames == null || frames.Length == 0)
                return;
            int frame = Mathf.FloorToInt((Time.time - startTime) * framesPerSecond);
            if (loop)
            {
                frame %= frames.Length;
            }
            else if (frame >= frames.Length)
            {
                if (destroyAtEnd)
                    Destroy(gameObject);
                enabled = false;
                return;
            }
            spriteRenderer.sprite = frames[frame];
        }
    }
}
