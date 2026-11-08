using System;
using FiveNightAnomaly.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FiveNightAnomaly.Characters
{
    /// <summary>
    /// Side-view lane control for the Pengembara: walks left, right, up and down the lane with the Move action (only on
    /// the painted walkable tiles) and drives the Animator (Idle, Walk, Attack, Charge, ChargeAttack). The Attack button
    /// acts when it is let go: a short press is a normal swing; held longer than holdToCharge the Pengembara goes
    /// straight into the Charge pose (no swing first) and the lantern fills with light. Let go once the charge is ready
    /// for the charge attack, earlier for a normal swing. Built by Tools > 5 Night Anomaly > Rebuild Pengembara Animations.
    /// </summary>
    [RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
    public sealed class PengembaraController : MonoBehaviour
    {
        private static readonly int SpeedParam = Animator.StringToHash("Speed");
        private static readonly int WalkRateParam = Animator.StringToHash("WalkRate");
        private static readonly int AttackParam = Animator.StringToHash("Attack");
        private static readonly int ChargingParam = Animator.StringToHash("Charging");
        private static readonly int ChargeReleaseParam = Animator.StringToHash("ChargeRelease");
        private static readonly int AttackState = Animator.StringToHash("Attack");
        private static readonly int ChargeState = Animator.StringToHash("Charge");
        private static readonly int ChargeAttackState = Animator.StringToHash("ChargeAttack");

        [Header("Input (Assets/Settings/InputSystem_Actions)")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference attackAction;

        [Header("Movement")]
        [Tooltip("Walking speed in units per second.")]
        [SerializeField, Min(0f)] private float moveSpeed = 1.36f;
        [Tooltip("World speed at which the feet of the Walk clip do not slide (walkSpeed in Pengembara_Anim.json). The Walk state plays faster or slower to match the real speed.")]
        [SerializeField, Min(0.01f)] private float walkClipSpeed = 1.36f;
        [Tooltip("Speed up and down the lane as a share of moveSpeed (the lane is seen from the side, so depth moves slower).")]
        [SerializeField, Range(0f, 1f)] private float depthSpeed = 0.7f;
        [Tooltip("Tiles the feet may stand on (LaneGrid/Walkable). Empty = the Pengembara can go anywhere.")]
        [SerializeField] private WalkableArea walkableArea;
        [Tooltip("Half width and half depth of the feet; the whole footprint has to stay on walkable tiles.")]
        [SerializeField] private Vector2 footprint = new Vector2(0.2f, 0.2f);

        [Header("Charge attack")]
        [Tooltip("Seconds the attack button has to stay down before the Charge pose starts. A shorter press is a normal swing.")]
        [SerializeField, Min(0.05f)] private float holdToCharge = 0.18f;
        [Tooltip("Seconds in the Charge pose before letting go releases the charge attack. Letting go earlier swings normally.")]
        [SerializeField, Min(0.05f)] private float chargeTime = 0.6f;

        private Animator animator;
        private SpriteRenderer spriteRenderer;
        private bool holding;
        private float holdTimer;
        private float chargeTimer;

        /// <summary>Raised on the impact frame of the Attack clip (animation event), the moment a hit lands.</summary>
        public event Action AttackImpact;

        /// <summary>Raised on the impact frame of the ChargeAttack clip (animation event).</summary>
        public event Action ChargeImpact;

        public bool FacingLeft => spriteRenderer.flipX;

        /// <summary>True while the Pengembara swings, charges or releases the charge attack (no walking then).</summary>
        public bool IsAttacking { get; private set; }

        public bool IsCharging { get; private set; }

        /// <summary>0 when the Charge pose starts, 1 when the charge attack is ready.</summary>
        public float ChargeProgress => Mathf.Clamp01(chargeTimer / chargeTime);

        public bool IsChargeReady => IsCharging && chargeTimer >= chargeTime;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (moveAction == null || attackAction == null)
            {
                Debug.LogError("PengembaraController needs the Move and Attack actions (Assets/Settings/InputSystem_Actions).", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            moveAction.action.Enable();
            attackAction.action.Enable();
        }

        private void Update()
        {
            int state = animator.IsInTransition(0)
                ? animator.GetNextAnimatorStateInfo(0).shortNameHash
                : animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
            IsCharging = state == ChargeState;
            bool busy = IsCharging || state == AttackState || state == ChargeAttackState;
            chargeTimer = IsCharging ? chargeTimer + Time.deltaTime : 0f;

            if (!holding && attackAction.action.WasPressedThisFrame())
            {
                holding = true;
                holdTimer = 0f;
            }
            if (holding)
            {
                holdTimer += Time.deltaTime;
                if (!attackAction.action.IsPressed())
                {
                    holding = false;
                    animator.SetTrigger(IsChargeReady ? ChargeReleaseParam : AttackParam);
                }
            }
            animator.SetBool(ChargingParam, holding && holdTimer >= holdToCharge);
            IsAttacking = busy;

            Vector2 input = moveAction.action.ReadValue<Vector2>();
            if (busy)
            {
                if (IsCharging && Mathf.Abs(input.x) > 0.1f)
                    spriteRenderer.flipX = input.x < 0f;
                input = Vector2.zero;
            }
            float speed = Move(input);
            animator.SetFloat(SpeedParam, speed);
            animator.SetFloat(WalkRateParam, speed / walkClipSpeed);
        }

        private float Move(Vector2 input)
        {
            if (input.sqrMagnitude < 0.0001f || Time.deltaTime <= 0f)
                return 0f;
            if (Mathf.Abs(input.x) > 0.01f)
                spriteRenderer.flipX = input.x < 0f;

            Vector2 feet = transform.position;
            Vector2 step = new Vector2(input.x, input.y * depthSpeed) * (moveSpeed * Time.deltaTime);
            bool outside = !CanStand(feet);
            Vector2 next = feet;
            if (!Mathf.Approximately(step.x, 0f) && (outside || CanStand(new Vector2(feet.x + step.x, feet.y))))
                next.x += step.x;
            if (!Mathf.Approximately(step.y, 0f) && (outside || CanStand(new Vector2(next.x, feet.y + step.y))))
                next.y += step.y;
            transform.position = new Vector3(next.x, next.y, transform.position.z);
            return (next - feet).magnitude / Time.deltaTime;
        }

        private bool CanStand(Vector2 feet) => walkableArea == null || walkableArea.Contains(feet, footprint);

        /// <summary>Seconds in the Charge pose before the charge attack is ready (set by the equipped hand item).</summary>
        public void SetChargeTime(float seconds)
        {
            chargeTime = Mathf.Max(0.05f, seconds);
        }

        private void OnAttackImpact()
        {
            AttackImpact?.Invoke();
        }

        private void OnChargeImpact()
        {
            ChargeImpact?.Invoke();
        }
    }
}
