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
        [SerializeField][Min(0f)] private float chargeTime = 1f;
        [SerializeField][Min(0f)] private float jumpTime = 0.25f, jumpForce = 2000f, jumpBufferTime = .1f, coyoteTime = .1f;
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

        public bool CanJump => lastGroundTime + coyoteTime > Time.time;

        private void Awake()
        {
            if (!CheckReferences()) enabled = false;
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
                if (!CanJump) OnLand.Invoke();
                lastGroundTime = Time.time;
            }
        }
        #endregion

        #region Jump
        public void ChargeJump(InputAction.CallbackContext context)
        {
            if (context.started) ChargeJump();
            else if (context.canceled) TryJump();
        }

        public void ChargeJump()
        {
            chargingJump = true;
            jumpRoutine ??= StartCoroutine(ChargeJumpRoutine());
        }

        private IEnumerator ChargeJumpRoutine()
        {
            var elapsedTime = 0f;
            OnChargingJump.Invoke(0f);

            float GetChargePercent() => elapsedTime / chargeTime;

            while (chargingJump && elapsedTime < chargeTime)
            {
                yield return new WaitForFixedUpdate();

                elapsedTime = Mathf.Min(elapsedTime + Time.fixedDeltaTime, chargeTime);
                OnChargingJump.Invoke(GetChargePercent());
            }

            yield return new WaitWhile(() => chargingJump);

            yield return StartCoroutine(TryJumpRoutine(GetChargePercent()));

            jumpRoutine = null;
        }

        public void TryJump()
        {
            if (!chargingJump) Debug.LogWarning("Jump must be charged first");
            chargingJump = false;
        }

        private IEnumerator TryJumpRoutine(float chargePercent)
        {
            var elapsedTime = 0f;
            while (elapsedTime < jumpBufferTime)
            {
                if (CanJump) break;

                elapsedTime += Time.fixedDeltaTime;

                yield return new WaitForFixedUpdate();
            }

            if (!CanJump) yield break;

            OnJump.Invoke();

            elapsedTime = 0f;
            while (elapsedTime < jumpTime)
            {
                rigidbody.AddRelativeForce(chargePercent * jumpForce * Vector3.up);

                elapsedTime += Time.fixedDeltaTime;

                yield return new WaitForFixedUpdate();
            }
        }
        #endregion

        #region Debug
        private bool CheckReferences()
        {
            if (rigidbody == null) Debug.LogError($"{nameof(rigidbody)} is not assigned on {name}'s {nameof(JumpController)}");
            else return true;

            return false;
        }
        #endregion
    }
}