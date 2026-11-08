using UnityEngine;

namespace FiveNightAnomaly.Characters
{
    /// <summary>
    /// Plays the charging loop (Pengembara_ChargeLoop) while the Pengembara charges: it fades in, rises in pitch as the
    /// charge fills, holds the top pitch while the charge is ready and fades out as soon as the button is let go. Sits on
    /// a child with its own looping AudioSource, so the one shot sounds of CharacterAudio keep their own pitch.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class ChargeSound : MonoBehaviour
    {
        [SerializeField] private PengembaraController controller;
        [SerializeField, Range(0f, 1f)] private float volume = 0.5f;
        [Tooltip("Pitch when the charge starts (x) and when it is ready (y).")]
        [SerializeField] private Vector2 pitch = new Vector2(0.85f, 1.25f);
        [Tooltip("Seconds to fade in and out, so the loop never starts or stops with a click.")]
        [SerializeField, Min(0.01f)] private float fadeTime = 0.08f;

        private AudioSource source;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            source.volume = 0f;
        }

        /// <summary>Charge loop of the equipped hand item.</summary>
        public void SetClip(AudioClip clip)
        {
            if (source == null)
                source = GetComponent<AudioSource>();
            if (source.clip == clip)
                return;
            source.Stop();
            source.volume = 0f;
            source.clip = clip;
        }

        private void Update()
        {
            bool charging = controller != null && controller.IsCharging;
            source.volume = Mathf.MoveTowards(source.volume, charging ? volume : 0f, volume / fadeTime * Time.deltaTime);
            if (charging)
            {
                source.pitch = Mathf.Lerp(pitch.x, pitch.y, controller.ChargeProgress);
                if (!source.isPlaying)
                    source.Play();
            }
            else if (source.isPlaying && source.volume <= 0f)
            {
                source.Stop();
            }
        }
    }
}
