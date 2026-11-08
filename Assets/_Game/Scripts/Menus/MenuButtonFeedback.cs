using UnityEngine;
using UnityEngine.EventSystems;

namespace FiveNightAnomaly.Menus
{
    /// <summary>
    /// Makes a menu button grow a little while the pointer is over it or it is selected with the keyboard or a gamepad,
    /// and dip while it is pressed. The size eases with unscaled time, so it also works while the game is paused.
    /// A button clicked with the mouse lets go of its selection again, so it does not stay big after the click.
    /// Target = the part that changes size (empty = this object), e.g. only the handle of a slider.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MenuButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler,
        IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField, Min(0f)] private float hoverScale = 1.06f;
        [SerializeField, Min(0f)] private float pressScale = 0.95f;
        [Tooltip("How fast the size follows, higher is snappier.")]
        [SerializeField, Min(0.01f)] private float speed = 16f;
        [SerializeField] private Transform target;

        private Vector3 rest = Vector3.one;
        private bool hovered;
        private bool selected;
        private bool pressed;

        public void OnPointerEnter(PointerEventData eventData) => hovered = true;

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
            pressed = false;
        }

        public void OnPointerDown(PointerEventData eventData) => pressed = true;

        public void OnPointerUp(PointerEventData eventData)
        {
            pressed = false;
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject)
                EventSystem.current.SetSelectedGameObject(null);
        }

        public void OnSelect(BaseEventData eventData) => selected = true;

        public void OnDeselect(BaseEventData eventData) => selected = false;

        private Transform Target => target != null ? target : transform;

        private void Awake()
        {
            rest = Target.localScale;
        }

        private void OnDisable()
        {
            hovered = selected = pressed = false;
            Target.localScale = rest;
        }

        private void Update()
        {
            float size = pressed ? pressScale : hovered || selected ? hoverScale : 1f;
            Target.localScale = Vector3.Lerp(Target.localScale, rest * size, 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime));
        }
    }
}
