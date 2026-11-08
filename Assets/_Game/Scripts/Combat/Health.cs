using System;
using UnityEngine;

namespace FiveNightAnomaly.Combat
{
    /// <summary>Health points of an enemy, a building or the test dummy. HealthBar shows them; Died fires at zero.</summary>
    public sealed class Health : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maxHealth = 100;

        private int current;

        public event Action<Health> Changed;
        public event Action<Health> Died;

        public int Max => maxHealth;
        public int Current => current;
        public float Fraction => (float)current / maxHealth;
        public bool IsDead => current <= 0;

        private void Awake()
        {
            current = maxHealth;
        }

        public void Damage(int amount)
        {
            if (amount <= 0 || IsDead)
                return;
            current = Mathf.Max(0, current - amount);
            Changed?.Invoke(this);
            if (current == 0)
                Died?.Invoke(this);
        }

        public void Refill()
        {
            current = maxHealth;
            Changed?.Invoke(this);
        }
    }
}
