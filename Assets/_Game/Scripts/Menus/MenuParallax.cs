using System;
using UnityEngine;
using UnityEngine.UI;

namespace FiveNightAnomaly.Menus
{
    /// <summary>
    /// Scrolls the main menu's background as if the view were walking along the lane. Every layer is a RawImage showing
    /// a strip that repeats without a seam (Menu* sprites, 3840 px wide, imported with Wrap Mode U = Repeat); it moves
    /// at the lane's speed times its own factor: far pines slow, the lane at full speed, the front hills faster.
    /// Runs on unscaled time, so it keeps moving while the game is paused.
    /// </summary>
    public sealed class MenuParallax : MonoBehaviour
    {
        [Serializable]
        private struct Layer
        {
            public RawImage image;
            [Tooltip("Pixels this layer moves for each pixel the lane moves: 0 = still, 1 = the lane, above 1 = in front of it.")]
            public float speed;
        }

        [Tooltip("Canvas pixels per second the lane moves.")]
        [SerializeField, Min(0f)] private float laneSpeed = 70f;
        [Tooltip("On: the view travels to the right, so the layers slide to the left. Off: the other way.")]
        [SerializeField] private bool towardsRight = true;
        [SerializeField] private Layer[] layers = Array.Empty<Layer>();

        private double travel;

        private void Update()
        {
            travel += (towardsRight ? 1.0 : -1.0) * laneSpeed * Time.unscaledDeltaTime;
            foreach (Layer layer in layers)
            {
                Texture texture = layer.image != null ? layer.image.texture : null;
                if (texture == null)
                    continue;
                Rect uv = layer.image.uvRect;
                uv.width = layer.image.rectTransform.rect.width / texture.width;
                uv.x = (float)Repeat(travel * layer.speed / texture.width);
                layer.image.uvRect = uv;
            }
        }

        private static double Repeat(double value) => value - Math.Floor(value);
    }
}
