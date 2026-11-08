using System.Collections;
using FiveNightAnomaly.Effects;
using UnityEngine;

namespace FiveNightAnomaly.Combat
{
    /// <summary>
    /// The straw dummy for testing hits: every hit takes health, a heavy hit (the charge attack) also makes it shake,
    /// and once its health reaches zero it fills up again after a short pause, so testing can go on.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class TrainingDummy : MonoBehaviour, IHittable
    {
        [SerializeField] private HitShake shake;
        [Tooltip("Seconds the dummy stays at zero health before it is full again.")]
        [SerializeField, Min(0f)] private float refillDelay = 1.5f;

        private Health health;

        private void Awake()
        {
            health = GetComponent<Health>();
        }

        public void TakeHit(in HitInfo hit)
        {
            if (health.IsDead)
                return;
            health.Damage(hit.Damage);
            if (hit.Heavy && shake != null)
                shake.Play(hit.Direction);
            if (health.IsDead)
                StartCoroutine(RefillLater());
        }

        private IEnumerator RefillLater()
        {
            yield return new WaitForSeconds(refillDelay);
            health.Refill();
        }
    }
}
