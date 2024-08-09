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
        [SerializeField][Min(0f)] private float maxGroundDistance = .1f;
        /// <summary>
        /// Passes the charge percentage [0, 1] each update of <see cref="ChargeJumpRoutine"/>
        /// </summary>
        [Header("Events")]
        public UnityEvent<float> OnChargingJump;
        public UnityEvent OnJump, OnLand;

        private Coroutine jumpRoutine;
        private float lastGroundTime;
        private bool chargingJump;

        public Rigidbody Rigidbody => rigidbody;
        public bool IsOnGround => lastGroundTime + coyoteTime > Time.time;

        private void Awake()
        {
            if (!CheckReferences()) enabled = false;
        }

        private void Start()
        {
            lastGroundTime = -coyoteTime;
        }

        private void FixedUpdate()
        {
            UpdateLastGroundTime();
        }

        #region Physics Checks
        private void UpdateLastGroundTime()
        {
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

            chargingJump = true;
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
                while (chargingJump && chargePercent < 1f)
                {
                    chargePercent = Mathf.Min((Time.time - startTime) / chargeTime, 1f);
                    OnChargingJump.Invoke(chargePercent);

                    yield return new WaitForFixedUpdate();
                }
            }
            yield return new WaitWhile(() => chargingJump);

            yield return StartCoroutine(TryJumpRoutine(chargePercent));

            jumpRoutine = null;
        }
        #endregion

        #region Jump
        public void TryJump()
        {
            if (!enabled) return;

            if (!chargingJump) Debug.LogWarning("Jump must be charged first");
            chargingJump = false;
        }

        private IEnumerator TryJumpRoutine(float chargePercent)
        {
            var startTime = Time.time;
            while (Time.time < startTime + jumpBufferTime && !IsOnGround) yield return new WaitForFixedUpdate();
            if (!IsOnGround) yield break;

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
        #endregion
    }
}