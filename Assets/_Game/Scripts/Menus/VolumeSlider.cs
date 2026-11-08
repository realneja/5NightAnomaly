using FiveNightAnomaly.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace FiveNightAnomaly.Menus
{
    /// <summary>
    /// Connects a UI slider to the volume setting: shows the saved volume when the screen opens, applies every change at
    /// once and plays a short sample sound now and then while the slider moves, so the player hears the new level.
    /// The setting is written to disk when the screen closes.
    /// </summary>
    [RequireComponent(typeof(Slider))]
    public sealed class VolumeSlider : MonoBehaviour
    {
        [SerializeField] private AudioSource previewSource;
        [SerializeField] private AudioClip previewSound;
        [Tooltip("Shortest time between two sample sounds while the slider moves.")]
        [SerializeField, Min(0f)] private float previewInterval = 0.15f;

        private Slider slider;
        private float nextPreview;

        private void Awake()
        {
            slider = GetComponent<Slider>();
            slider.onValueChanged.AddListener(OnValueChanged);
        }

        private void OnEnable() => slider.SetValueWithoutNotify(VolumeSettings.Volume);

        private void OnDisable() => VolumeSettings.Save();

        private void OnDestroy() => slider.onValueChanged.RemoveListener(OnValueChanged);

        private void OnValueChanged(float value)
        {
            VolumeSettings.SetVolume(value);
            if (previewSource == null || previewSound == null || Time.unscaledTime < nextPreview)
                return;
            nextPreview = Time.unscaledTime + previewInterval;
            previewSource.PlayOneShot(previewSound);
        }
    }
}
