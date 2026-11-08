using UnityEngine;

namespace FiveNightAnomaly.Cameras
{
    /// <summary>Follows a target along the lane on the X axis only, smoothed, inside the lane limits.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [Tooltip("Seconds the camera needs to catch up with the target.")]
        [SerializeField, Min(0f)] private float smoothTime = 0.25f;
        [Tooltip("The camera X stays between these values, so the view never passes the lane ends.")]
        [SerializeField] private Vector2 limitsX = new Vector2(0f, 30f);

        private float velocityX;

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        private void Start()
        {
            if (target != null)
                SetX(Goal());
        }

        private void LateUpdate()
        {
            if (target != null)
                SetX(Mathf.SmoothDamp(transform.position.x, Goal(), ref velocityX, smoothTime));
        }

        private float Goal() => Mathf.Clamp(target.position.x, limitsX.x, limitsX.y);

        private void SetX(float x)
        {
            Vector3 position = transform.position;
            position.x = x;
            transform.position = position;
        }
    }
}
