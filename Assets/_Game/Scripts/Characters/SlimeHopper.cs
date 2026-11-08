using System;
using System.Collections;
using FiveNightAnomaly.Combat;
using FiveNightAnomaly.Effects;
using UnityEngine;

namespace FiveNightAnomaly.Characters
{
    /// <summary>
    /// A slime that hops back and forth along the lane between two points around where it starts: crouch, a jump arc that
    /// stretches the body, a squash on landing, a short rest. It can be hit: every hit takes health and stuns it for a
    /// moment, a heavy hit also shakes it; at zero health it flattens and fades (its health bar with it), then pops back
    /// full after a pause.
    /// Only its children move (Body = jump height, Squash = squash and stretch, Shadow on the ground), so the sprite art
    /// stays as it is and the idle breathing of the Visual Animator keeps running underneath.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class SlimeHopper : MonoBehaviour, IHittable
    {
        [Header("Parts")]
        [SerializeField] private Transform body;
        [SerializeField] private Transform squash;
        [SerializeField] private SpriteRenderer sprite;
        [SerializeField] private SpriteRenderer shadow;
        [SerializeField] private HitShake shake;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip landSound;
        [SerializeField, Range(0f, 1f)] private float landVolume = 0.35f;

        [Header("Hopping")]
        [Tooltip("Left and right end of the patrol in units from where the slime starts.")]
        [SerializeField] private Vector2 patrol = new Vector2(-1.2f, 4.6f);
        [SerializeField, Min(0.05f)] private float hopDistance = 0.8f;
        [SerializeField, Min(0f)] private float hopHeight = 0.55f;
        [SerializeField, Min(0.05f)] private float airTime = 0.42f;
        [SerializeField, Min(0f)] private float crouchTime = 0.14f;
        [SerializeField, Min(0f)] private float landTime = 0.16f;
        [Tooltip("Rest between hops in seconds: shortest (x) and longest (y).")]
        [SerializeField] private Vector2 rest = new Vector2(0.25f, 0.55f);
        [Tooltip("Mirror the sprite to the side it hops to.")]
        [SerializeField] private bool faceDirection = true;

        [Header("Hits")]
        [Tooltip("Seconds without hopping after a hit.")]
        [SerializeField, Min(0f)] private float stunTime = 0.35f;
        [Tooltip("Seconds flat and invisible at zero health before it pops back full.")]
        [SerializeField, Min(0f)] private float respawnDelay = 1.5f;

        private Health health;
        private float startX;
        private float direction = 1f;
        private float stunnedUntil;
        private Vector3 shadowScale = Vector3.one;
        private SpriteRenderer[] fadeParts;
        private float[] fadeAlpha;

        public bool IsDefeated { get; private set; }

        private void Awake()
        {
            health = GetComponent<Health>();
            startX = transform.position.x;
            if (shadow != null)
                shadowScale = shadow.transform.localScale;
            fadeParts = GetComponentsInChildren<SpriteRenderer>(true);
            fadeAlpha = new float[fadeParts.Length];
            for (int i = 0; i < fadeParts.Length; i++)
                fadeAlpha[i] = fadeParts[i].color.a;
        }

        private void OnEnable()
        {
            StartCoroutine(HopLoop());
        }

        public void TakeHit(in HitInfo hit)
        {
            if (IsDefeated)
                return;
            health.Damage(hit.Damage);
            stunnedUntil = Time.time + stunTime;
            if (hit.Heavy && shake != null)
                shake.Play(hit.Direction);
            if (health.IsDead)
            {
                StopAllCoroutines();
                StartCoroutine(DefeatAndReturn());
            }
        }

        private IEnumerator HopLoop()
        {
            while (true)
            {
                float until = Time.time + UnityEngine.Random.Range(rest.x, rest.y);
                while (Time.time < until || Time.time < stunnedUntil)
                    yield return null;

                float from = transform.position.x;
                if (from + direction * hopDistance > startX + patrol.y)
                    direction = -1f;
                else if (from + direction * hopDistance < startX + patrol.x)
                    direction = 1f;
                if (faceDirection && sprite != null)
                    sprite.flipX = direction < 0f;
                float to = from + direction * hopDistance;

                yield return Animate(crouchTime, t => SetSquash(1f + 0.12f * Smooth(t), 1f - 0.14f * Smooth(t)));
                yield return Animate(airTime, t =>
                {
                    SetX(Mathf.Lerp(from, to, t));
                    SetHeight(4f * hopHeight * t * (1f - t));
                    float stretch = Mathf.Sin(Mathf.PI * Mathf.Min(1f, t * 1.6f)) * (1f - t);
                    SetSquash(1f - 0.08f * stretch, 1f + 0.12f * stretch);
                });
                SetX(to);
                SetHeight(0f);
                if (audioSource != null && landSound != null)
                {
                    audioSource.pitch = 1f + UnityEngine.Random.Range(-0.06f, 0.06f);
                    audioSource.PlayOneShot(landSound, landVolume);
                }
                yield return Animate(landTime, t =>
                {
                    float k = Mathf.Sin(Mathf.PI * t);
                    SetSquash(1f + 0.15f * k, 1f - 0.14f * k);
                });
                SetSquash(1f, 1f);
            }
        }

        private IEnumerator DefeatAndReturn()
        {
            IsDefeated = true;
            float height = body != null ? body.localPosition.y : 0f;
            yield return Animate(0.12f, t => SetHeight(height * (1f - t)));
            yield return Animate(0.25f, t => SetSquash(Mathf.Lerp(1f, 1.45f, t), Mathf.Lerp(1f, 0.2f, t)));
            yield return Animate(0.3f, t => SetAlpha(1f - t));
            yield return new WaitForSeconds(respawnDelay);
            health.Refill();
            SetAlpha(1f);
            yield return Animate(0.3f, t =>
            {
                float size = Mathf.Lerp(0.2f, 1f, t) + 0.15f * Mathf.Sin(Mathf.PI * t);
                SetSquash(size, size);
            });
            SetSquash(1f, 1f);
            IsDefeated = false;
            StartCoroutine(HopLoop());
        }

        private static IEnumerator Animate(float duration, Action<float> step)
        {
            if (duration <= 0f)
            {
                step(1f);
                yield break;
            }
            for (float time = 0f; time < duration; time += Time.deltaTime)
            {
                step(time / duration);
                yield return null;
            }
            step(1f);
        }

        private static float Smooth(float t) => t * t * (3f - 2f * t);

        private void SetX(float x)
        {
            Vector3 position = transform.position;
            position.x = x;
            transform.position = position;
        }

        private void SetHeight(float height)
        {
            if (body != null)
                body.localPosition = new Vector3(0f, height, 0f);
            if (shadow != null)
            {
                float k = hopHeight > 0f ? 1f - 0.45f * Mathf.Clamp01(height / hopHeight) : 1f;
                shadow.transform.localScale = new Vector3(shadowScale.x * k, shadowScale.y * k, 1f);
            }
        }

        private void SetSquash(float x, float y)
        {
            if (squash != null)
                squash.localScale = new Vector3(x, y, 1f);
        }

        private void SetAlpha(float alpha)
        {
            for (int i = 0; i < fadeParts.Length; i++)
            {
                Color color = fadeParts[i].color;
                color.a = fadeAlpha[i] * alpha;
                fadeParts[i].color = color;
            }
        }

        private void OnDrawGizmosSelected()
        {
            float x0 = Application.isPlaying ? startX : transform.position.x;
            Vector3 y = new Vector3(0f, transform.position.y, 0f);
            Gizmos.color = new Color(1f, 0.6f, 0.1f);
            Gizmos.DrawLine(new Vector3(x0 + patrol.x, 0f) + y, new Vector3(x0 + patrol.y, 0f) + y);
        }
    }
}
