using UnityEngine;

namespace FiveNightAnomaly.Combat
{
    /// <summary>
    /// Something the Pengembara holds in the hand slot (keys 1 to 4): its animations, its normal attack, its charge
    /// attack, and how the charge looks and sounds. Made by Tools > 5 Night Anomaly > Rebuild Pengembara Animations
    /// (Assets/_Game/Data/Hands/Hand*.asset); a new hand item only needs a new asset of this type.
    /// </summary>
    [CreateAssetMenu(menuName = "5 Night Anomaly/Hand Item", fileName = "Hand")]
    public sealed class HandItem : ScriptableObject
    {
        [SerializeField] private string displayName = "Hand";
        [Tooltip("Pengembara.controller for the lantern staff, an override controller with this item's clips for the others.")]
        [SerializeField] private RuntimeAnimatorController animations;
        [SerializeField] private Attack attack;
        [SerializeField] private Attack chargeAttack;
        [Tooltip("Seconds in the Charge pose before letting go releases the charge attack.")]
        [SerializeField, Min(0.05f)] private float chargeTime = 0.6f;

        [Header("Charge look and sound")]
        [SerializeField] private Sprite[] chargeGlow;
        [Tooltip("Centre of the charge glow in the Charge pose, facing right, from the feet (chargeGlow in Pengembara_Anim.json).")]
        [SerializeField] private Vector2 chargeGlowOffset;
        [SerializeField, Min(0.1f)] private float chargeGlowScale = 1f;
        [SerializeField] private AudioClip chargeLoop;

        public string DisplayName => displayName;
        public RuntimeAnimatorController Animations => animations;
        public Attack Attack => attack;
        public Attack ChargeAttack => chargeAttack;
        public float ChargeTime => chargeTime;
        public Sprite[] ChargeGlow => chargeGlow;
        public Vector2 ChargeGlowOffset => chargeGlowOffset;
        public float ChargeGlowScale => chargeGlowScale;
        public AudioClip ChargeLoop => chargeLoop;
    }
}
