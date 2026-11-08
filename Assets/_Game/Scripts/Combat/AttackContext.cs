using UnityEngine;

namespace FiveNightAnomaly.Combat
{
    /// <summary>Who attacks, from where and which way: what an Attack needs when its release frame comes.</summary>
    public readonly struct AttackContext
    {
        public AttackContext(MonoBehaviour owner, Vector2 feet, float facing, AudioSource audio)
        {
            Owner = owner;
            Feet = feet;
            Facing = facing;
            Audio = audio;
        }

        /// <summary>The attacker; runs coroutines and is never hit by its own attack.</summary>
        public MonoBehaviour Owner { get; }

        public Vector2 Feet { get; }

        /// <summary>+1 facing right, -1 facing left.</summary>
        public float Facing { get; }

        public AudioSource Audio { get; }

        /// <summary>A point given facing right from the feet, turned to the side the attacker faces.</summary>
        public Vector2 Point(Vector2 local) => Feet + new Vector2(local.x * Facing, local.y);

        public void Play(AudioClip clip, float volume, float pitch)
        {
            if (clip == null || Audio == null)
                return;
            Audio.pitch = pitch;
            Audio.PlayOneShot(clip, volume);
        }
    }
}
