using UnityEngine;

namespace FiveNightAnomaly.Audio
{
    /// <summary>
    /// The player's volume (0 to 1, the slider on the setting screen), kept in PlayerPrefs and applied to every sound of
    /// the game through AudioListener.volume. It is loaded before the first scene, so it also holds when a game scene is
    /// played straight from the editor. The listener gets the square of the slider value, so the loudness changes about
    /// evenly along the slider instead of all at its low end.
    /// </summary>
    public static class VolumeSettings
    {
        public const string Key = "settings.volume";
        public const float DefaultVolume = 1f;

        public static float Volume { get; private set; } = DefaultVolume;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Load()
        {
            Volume = Mathf.Clamp01(PlayerPrefs.GetFloat(Key, DefaultVolume));
            Apply();
        }

        public static void SetVolume(float volume)
        {
            Volume = Mathf.Clamp01(volume);
            Apply();
            PlayerPrefs.SetFloat(Key, Volume);
        }

        public static void Save() => PlayerPrefs.Save();

        private static void Apply() => AudioListener.volume = Volume * Volume;
    }
}
