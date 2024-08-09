using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace PilotPursuit.Movement
{
    public class JumpController : MonoBehaviour
    {
        [SerializeField] private new Rigidbody rigidbody;
        [Header("Jump Settings")]
        [SerializeField][Min(0f)] private float chargeTime = .4f;
        [SerializeField][Min(0f)] private float jumpTime = .15f, jumpForce = 3000f, jumpBufferTime = .1f, coyoteTime = .1f;
        [Header("Physics Checks")]
        [SerializeField] private LayerMask groundMask;
        [Tooltip("Set to 0 to disable ground check (always on ground).")]
        [SerializeField][Min(0f)] private float maxGroundDistance = .1f;
        /// <summary>
        /// Passes the charge percentage [0, 1] each update of <see cref="ChargeJumpRoutine"/>
        /// </summary>
        [Header("Events")]
        public UnityEvent<float> OnChargingJump;
        public UnityEvent OnJump, OnLand;
        [Header("Debug")]
        [SerializeField] private bool logEvents;

        private Coroutine jumpRoutine;
        private float lastGroundTime, lastJumpTime;

        public Rigidbody Rigidbody => rigidbody;
        public bool IsOnGround => maxGroundDistance == 0f || (lastGroundTime + coyoteTime > Time.time && !HasJustJumped);
        public bool IsChargingJump { get; private set; }
        public bool HasJustJumped => lastJumpTime + jumpTime > Time.time;
        
        private void Awake()
        {
            if (!CheckReferences()) enabled = false;
            if (logEvents) AddLogToEvents();
        }

        private void Start()
        {
            lastGroundTime = -coyoteTime;
            lastJumpTime = -jumpTime;
        }

        private void FixedUpdate()
        {
            UpdateLastGroundTime();
        }

        #region Physics Checks
        private void UpdateLastGroundTime()
        {
            if (maxGroundDistance == 0f || HasJustJumped) return;
            
            var groundColliders = new Collider[1];
            var groundColliderCount = Physics.OverlapSphereNonAlloc(transform.position, maxGroundDistance, groundColliders, groundMask);

            if (groundColliderCount > 0)
            {
                if (!IsOnGround) OnLand.Invoke();
                lastGroundTime = Time.time;
            }
        }
        #endregion

        #region Charge Jump
        public void ChargeJump(InputAction.CallbackContext context)
        {
            if (context.started) ChargeJump();
            else if (context.canceled) TryJump();
        }

        public void ChargeJump()
        {
            if (!enabled) return;

            IsChargingJump = true;
            jumpRoutine ??= StartCoroutine(ChargeJumpRoutine());
        }

        private IEnumerator ChargeJumpRoutine()
        {
            float chargePercent = 0f;
            if (chargeTime == 0f)
            {
                chargePercent = 1f;
                OnChargingJump.Invoke(chargePercent);
            }
            else
            {
                float startTime = Time.time;
                while (IsChargingJump && chargePercent < 1f)
                {
                    chargePercent = Mathf.Min((Time.time - startTime) / chargeTime, 1f);
                    OnChargingJump.Invoke(chargePercent);

                    yield return new WaitForFixedUpdate();
                }
            }
            yield return new WaitWhile(() => IsChargingJump);

            yield return StartCoroutine(TryJumpRoutine(chargePercent));

            jumpRoutine = null;
        }
        #endregion

        #region Jump
        public void TryJump()
        {
            if (!enabled) return;

            if (!IsChargingJump) Debug.LogWarning("Jump must be charged first");
            IsChargingJump = false;
        }

        private IEnumerator TryJumpRoutine(float chargePercent)
        {
            var startTime = Time.time;
            while (Time.time < startTime + jumpBufferTime && !IsOnGround) yield return new WaitForFixedUpdate();
            if (!IsOnGround) yield break;

            lastJumpTime = Time.time;
            OnJump.Invoke();

            if (jumpTime == 0f) ApplyJumpForce(chargePercent, ForceMode.Impulse);
            else
            {
                startTime = Time.time;
                while (Time.time < startTime + jumpTime)
                {
                    ApplyJumpForce(chargePercent);

                    yield return new WaitForFixedUpdate();
                }
            }
        }

        private void ApplyJumpForce(float chargePercent, ForceMode forceMode = ForceMode.Force)
        {
            rigidbody.AddRelativeForce(chargePercent * jumpForce * Vector3.up, forceMode);
        }
        #endregion

        #region Debug
        private bool CheckReferences()
        {
            if (rigidbody == null) Debug.LogError($"{nameof(rigidbody)} is not assigned on {name}'s {GetType().Name}");
            else return true;

            return false;
        }

        private void AddLogToEvents()
        {
            OnChargingJump.AddListener(_ => Debug.Log(GetType().Name + ": " + nameof(OnChargingJump)));
            OnJump.AddListener(() => Debug.Log(GetType().Name + ": " + nameof(OnJump)));
            OnLand.AddListener(() => Debug.Log(GetType().Name + ": " + nameof(OnLand)));
        }
        #endregion
    }
}