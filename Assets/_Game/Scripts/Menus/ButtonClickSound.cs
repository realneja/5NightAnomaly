using UnityEngine;
using UnityEngine.UI;

namespace FiveNightAnomaly.Menus
{
    /// <summary>
    /// Plays a short sound each time this button is pressed, with the mouse, Enter or the gamepad's south button
    /// (Button.onClick). All buttons share one 2D AudioSource that is kept across scene loads, so the click of PLAY is
    /// still heard while the game scene loads instead of stopping with the menu. It also plays while the listener is
    /// paused, for a pause menu later.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class ButtonClickSound : MonoBehaviour
    {
        [SerializeField] private AudioClip clickSound;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;

        private static AudioSource shared;
        private Button button;

        private void Awake() => button = GetComponent<Button>();

        private void OnEnable() => button.onClick.AddListener(Play);

        private void OnDisable() => button.onClick.RemoveListener(Play);

        private void Play()
        {
            if (clickSound == null)
                return;
            if (shared == null)
            {
                var go = new GameObject("ButtonClickSound");
                DontDestroyOnLoad(go);
                shared = go.AddComponent<AudioSource>();
                shared.playOnAwake = false;
                shared.spatialBlend = 0f;
                shared.ignoreListenerPause = true;
            }
            shared.PlayOneShot(clickSound, volume);
        }
    }
}
