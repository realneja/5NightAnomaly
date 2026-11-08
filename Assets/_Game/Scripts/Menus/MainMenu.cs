using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace FiveNightAnomaly.Menus
{
    /// <summary>
    /// The main menu (scene MainMenu). PLAY loads the game scene; SETTING and CREDIT fade the title and buttons out and
    /// their screen in, BACK (or Esc, or the gamepad's east button) goes back. Nothing is selected while the mouse is
    /// used, so the screens look like the design; the first arrow, W/S, Tab or gamepad up/down selects the first item
    /// of the screen, after that Enter or the gamepad's south button presses the selected one. A screen opened with the
    /// keyboard selects its first item at once, and BACK then selects the button that opened it again.
    /// Runs after the EventSystem, so that first press only selects and does not also move on to the next item.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public sealed class MainMenu : MonoBehaviour
    {
        [Tooltip("Scene loaded by PLAY. It must be in the scene list of the build profile.")]
        [SerializeField] private string playScene = "NightLaneDemo";

        [Header("Screens")]
        [Tooltip("The title with the PLAY, SETTING and CREDIT buttons.")]
        [SerializeField] private CanvasGroup home;
        [SerializeField] private CanvasGroup settingScreen;
        [SerializeField] private CanvasGroup creditScreen;
        [SerializeField, Min(0f)] private float fadeTime = 0.18f;

        [Header("First item of each screen (keyboard and gamepad)")]
        [SerializeField] private GameObject homeFirst;
        [SerializeField] private GameObject settingFirst;
        [SerializeField] private GameObject creditFirst;

        private CanvasGroup current;
        private GameObject opener;
        private Coroutine switching;

        public void Play() => SceneManager.LoadScene(playScene);

        public void OpenSetting() => Open(settingScreen);

        public void OpenCredit() => Open(creditScreen);

        public void Back()
        {
            if (switching != null || current == home)
                return;
            bool keyboard = Selected != null;
            switching = StartCoroutine(Switch(current, home, keyboard ? (opener != null ? opener : homeFirst) : null));
        }

        private void Awake()
        {
            current = home;
            Show(home, true);
            Show(settingScreen, false);
            Show(creditScreen, false);
        }

        private void Update()
        {
            if (switching != null)
                return;
            if (current != home && CancelPressed())
            {
                Back();
                return;
            }
            GameObject first = current == settingScreen ? settingFirst : current == creditScreen ? creditFirst : homeFirst;
            if (EventSystem.current != null && first != null && Selected == null && NavigationPressed())
                EventSystem.current.SetSelectedGameObject(first);
        }

        private void Open(CanvasGroup screen)
        {
            if (switching != null || screen == null || current != home)
                return;
            opener = Selected;
            GameObject first = screen == settingScreen ? settingFirst : creditFirst;
            switching = StartCoroutine(Switch(home, screen, opener != null ? first : null));
        }

        private IEnumerator Switch(CanvasGroup from, CanvasGroup to, GameObject select)
        {
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
            from.interactable = to.interactable = false;
            from.blocksRaycasts = to.blocksRaycasts = false;
            yield return Fade(from, 1f, 0f);
            from.gameObject.SetActive(false);
            to.gameObject.SetActive(true);
            yield return Fade(to, 0f, 1f);
            Show(to, true);
            current = to;
            switching = null;
            if (select != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(select);
        }

        private IEnumerator Fade(CanvasGroup group, float from, float to)
        {
            for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(from, to, t / fadeTime);
                yield return null;
            }
            group.alpha = to;
        }

        private static void Show(CanvasGroup group, bool visible)
        {
            if (group == null)
                return;
            group.gameObject.SetActive(visible);
            group.alpha = visible ? 1f : 0f;
            group.interactable = group.blocksRaycasts = visible;
        }

        private static GameObject Selected => EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;

        private static bool NavigationPressed()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.upArrowKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame
                || keyboard.wKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame || keyboard.tabKey.wasPressedThisFrame))
                return true;
            Gamepad gamepad = Gamepad.current;
            return gamepad != null && (gamepad.dpad.up.wasPressedThisFrame || gamepad.dpad.down.wasPressedThisFrame
                || gamepad.leftStick.up.wasPressedThisFrame || gamepad.leftStick.down.wasPressedThisFrame);
        }

        private static bool CancelPressed()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                return true;
            Gamepad gamepad = Gamepad.current;
            return gamepad != null && gamepad.buttonEast.wasPressedThisFrame;
        }
    }
}
