using UnityEngine;

namespace FiveNightAnomaly.Backgrounds
{
    /// <summary>
    /// Moves a background layer by a fraction of the camera's horizontal movement and keeps a repeating layer
    /// centred on the camera. followFactor 1 = fixed on screen (sky), 0 = fixed in the world (ground),
    /// between 0 and 1 = farther away (moves slower), negative = foreground (moves faster than the world).
    /// Draw the layer's SpriteRenderer in Tiled mode, three repeats wide.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public sealed class ParallaxLayer : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;
        [SerializeField, Range(-1f, 1f)] private float followFactor;
        [Tooltip("Width of one repeat of the sprite in units (15 for the 1920 px night layers). 0 = no repeat.")]
        [SerializeField, Min(0f)] private float repeatWidth = 15f;

        private float startX;
        private float cameraStartX;

        private void Start()
        {
            startX = transform.position.x;
            cameraStartX = cameraTransform.position.x;
        }

        private void LateUpdate()
        {
            float cameraX = cameraTransform.position.x;
            float x = startX + (cameraX - cameraStartX) * followFactor;
            if (repeatWidth > 0f)
                x += Mathf.Round((cameraX - x) / repeatWidth) * repeatWidth;
            Vector3 position = transform.position;
            position.x = x;
            transform.position = position;
        }
    }
}
