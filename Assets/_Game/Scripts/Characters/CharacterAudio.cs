using UnityEngine;

namespace FiveNightAnomaly.Characters
{
    /// <summary>
    /// Plays the footsteps from the OnFootstep animation event (contact frames of every Walk clip). Footsteps never
    /// repeat the same clip twice in a row and get a slightly different pitch, so walking does not sound mechanical.
    /// The sounds of the attacks belong to the hand items and are played by HandCombat.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class CharacterAudio : MonoBehaviour
    {
        [SerializeField] private AudioClip[] footsteps;
        [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.6f;
        [Tooltip("Random pitch change per step: 0.06 = up to 6 percent higher or lower.")]
        [SerializeField, Range(0f, 0.3f)] private float pitchVariation = 0.06f;

        private AudioSource source;
        private int lastFootstep = -1;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
        }

        private void OnFootstep()
        {
            if (footsteps == null || footsteps.Length == 0)
                return;
            int index = UnityEngine.Random.Range(0, footsteps.Length);
            if (footsteps.Length > 1 && index == lastFootstep)
                index = (index + 1) % footsteps.Length;
            lastFootstep = index;
            AudioClip clip = footsteps[index];
            if (clip == null)
                return;
            source.pitch = 1f + UnityEngine.Random.Range(-pitchVariation, pitchVariation);
            source.PlayOneShot(clip, footstepVolume);
        }
    }
}
