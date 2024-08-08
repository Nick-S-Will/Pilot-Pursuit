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

        public bool OnGround => lastGroundTime + coyoteTime > Time.time;

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
                if (!OnGround) OnLand.Invoke();
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
            float startTime = Time.time, chargePercent = 0f;
            while (chargingJump && chargePercent < 1f)
            {
                chargePercent = Mathf.Min((Time.time - startTime) / chargeTime, 1f);
                OnChargingJump.Invoke(chargePercent);

                yield return new WaitForFixedUpdate();
            }
            yield return new WaitWhile(() => chargingJump);

            yield return StartCoroutine(TryJumpRoutine(chargePercent));

            jumpRoutine = null;
        }

        public void TryJump()
        {
            if (!chargingJump) Debug.LogWarning("Jump must be charged first");
            chargingJump = false;
        }

        private IEnumerator TryJumpRoutine(float chargePercent)
        {
            var startTime = Time.time;
            while (Time.time < startTime + jumpBufferTime && !OnGround) yield return new WaitForFixedUpdate();
            if (!OnGround) yield break;

            OnJump.Invoke();

            startTime = Time.time;
            while (Time.time < startTime + jumpTime)
            {
                rigidbody.AddRelativeForce(chargePercent * jumpForce * Vector3.up);

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