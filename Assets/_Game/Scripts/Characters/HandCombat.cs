using System;
using FiveNightAnomaly.Combat;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FiveNightAnomaly.Characters
{
    /// <summary>
    /// The Pengembara's hand slot. Keys 1 to 4 switch between the hand items (lantern staff, Zeus bolt, wizard staff,
    /// bare hands) while he is not attacking: the Animator gets the item's clips, and the charge glow, charge sound and
    /// charge time follow the item. Turns the animation events into the item's attacks: OnAttackSwing / OnChargeSwing
    /// play the whoosh, OnAttackImpact / OnChargeImpact make the attack take effect.
    /// </summary>
    [RequireComponent(typeof(PengembaraController), typeof(AudioSource))]
    public sealed class HandCombat : MonoBehaviour
    {
        [Tooltip("Hand items in slot order: key 1, 2, 3, 4.")]
        [SerializeField] private HandItem[] hands = new HandItem[4];
        [Tooltip("Player/Hand1 to Hand4 of InputSystem_Actions, in the same order as the hands.")]
        [SerializeField] private InputActionReference[] handActions = new InputActionReference[4];
        [Tooltip("Slot held when the scene starts (0 = key 1).")]
        [SerializeField, Min(0)] private int startSlot;
        [SerializeField] private ChargeGlow chargeGlow;
        [SerializeField] private ChargeSound chargeSound;
        [Tooltip("Random pitch change per sound: 0.05 = up to 5 percent higher or lower.")]
        [SerializeField, Range(0f, 0.3f)] private float pitchVariation = 0.05f;

        private PengembaraController controller;
        private Animator animator;
        private SpriteRenderer body;
        private AudioSource audioSource;
        private int current = -1;

        /// <summary>Raised after a hand item is equipped (slot index, item).</summary>
        public event Action<int, HandItem> Equipped;

        public HandItem Current => current >= 0 ? hands[current] : null;

        public int CurrentSlot => current;

        private float Facing => body.flipX ? -1f : 1f;

        private float PitchJitter => 1f + UnityEngine.Random.Range(-pitchVariation, pitchVariation);

        private void Awake()
        {
            controller = GetComponent<PengembaraController>();
            animator = GetComponent<Animator>();
            body = GetComponent<SpriteRenderer>();
            audioSource = GetComponent<AudioSource>();
        }

        private void OnEnable()
        {
            foreach (InputActionReference action in handActions)
            {
                if (action != null)
                    action.action.Enable();
            }
        }

        private void Start()
        {
            Equip(startSlot);
        }

        private void Update()
        {
            if (controller.IsAttacking)
                return;
            for (int slot = 0; slot < handActions.Length && slot < hands.Length; slot++)
            {
                if (slot != current && handActions[slot] != null && handActions[slot].action.WasPressedThisFrame())
                {
                    Equip(slot);
                    break;
                }
            }
        }

        /// <summary>Holds the item of this slot. Returns false when the slot is empty.</summary>
        public bool Equip(int slot)
        {
            if (slot < 0 || slot >= hands.Length || hands[slot] == null)
                return false;
            HandItem hand = hands[slot];
            current = slot;
            if (hand.Animations != null && animator.runtimeAnimatorController != hand.Animations)
                animator.runtimeAnimatorController = hand.Animations;
            controller.SetChargeTime(hand.ChargeTime);
            if (chargeGlow != null)
                chargeGlow.SetLook(hand.ChargeGlow, hand.ChargeGlowOffset, hand.ChargeGlowScale);
            if (chargeSound != null)
                chargeSound.SetClip(hand.ChargeLoop);
            Equipped?.Invoke(slot, hand);
            return true;
        }

        private void OnAttackSwing() => Swing(AttackOf(false));

        private void OnAttackImpact() => Release(AttackOf(false));

        private void OnChargeSwing() => Swing(AttackOf(true));

        private void OnChargeImpact() => Release(AttackOf(true));

        private Attack AttackOf(bool charged)
        {
            HandItem hand = Current;
            if (hand == null)
                return null;
            return charged ? hand.ChargeAttack : hand.Attack;
        }

        private void Swing(Attack attack)
        {
            if (attack == null || attack.SwingSound == null)
                return;
            audioSource.pitch = attack.Pitch * PitchJitter;
            audioSource.PlayOneShot(attack.SwingSound, attack.Volume);
        }

        private void Release(Attack attack)
        {
            if (attack != null)
                attack.Release(new AttackContext(this, transform.position, Facing, audioSource), PitchJitter);
        }

        private void OnDrawGizmosSelected()
        {
            HandItem hand = Current != null ? Current : hands.Length > 0 ? hands[0] : null;
            if (hand == null)
                return;
            float facing = TryGetComponent(out SpriteRenderer spriteRenderer) && spriteRenderer.flipX ? -1f : 1f;
            Gizmos.color = new Color(1f, 0.85f, 0.2f);
            if (hand.Attack != null)
                hand.Attack.DrawGizmos(transform.position, facing);
            Gizmos.color = new Color(1f, 0.45f, 0.1f);
            if (hand.ChargeAttack != null)
                hand.ChargeAttack.DrawGizmos(transform.position, facing);
        }
    }
}
