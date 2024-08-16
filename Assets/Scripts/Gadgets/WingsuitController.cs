using PilotPursuit.Movement;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace PilotPursuit.Gadgets
{
    public class WingsuitController : MonoBehaviour
    {
        [SerializeField] private Skydiver skydiver;
        [Header("Lift Settings")]
        [SerializeField][Min(0f)] private float addedLiftForce = 200f;
        [Header("Tilt Settings")]
        [SerializeField] private Vector2 tiltTorque = 100f * Vector2.one;
        [SerializeField][Range(0f, 90f)] private float maxRoll = 45f, maxPitch = 75f;
        [SerializeField][Min(0f)] private float headingAdjustSpeed = 10f, headingAdjustSpeedThreshold = 1f, headingAngleTolerance = 5f;
        [Header("Events")]
        public UnityEvent OnDeploy;
        public UnityEvent OnRetract;
        [Header("Debug")]
        [SerializeField] private bool logEvents;

        private Coroutine tiltRoutine;
        private Vector2 tiltInput;

        public Rigidbody Rigidbody => skydiver.Rigidbody;
        public bool IsDeployed { get; private set; }

        private void Awake()
        {
            if (!CheckReferences()) enabled = false;
            if (logEvents) AddLogToEvents();

            Cursor.lockState = CursorLockMode.Confined;
        }

        private void FixedUpdate()
        {
            skydiver.AddedLiftForce = IsDeployed ? addedLiftForce : 0f;
        }

        #region Deployment
        public void TryToggle(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                if (IsDeployed) Retract();
                else TryDeploy();
            }
        }

        public void TryDeploy()
        {
            if (!enabled || !skydiver.IsHorizontal || IsDeployed) return;

            IsDeployed = true;
            if (tiltRoutine != null) StopCoroutine(tiltRoutine);
            tiltRoutine = StartCoroutine(TiltRoutine());

            OnDeploy.Invoke();
        }

        public void Retract()
        {
            if (!enabled) return;

            if (!IsDeployed) Debug.LogWarning($"{nameof(WingsuitController)}'s {nameof(IsDeployed)} must be true before {nameof(Retract)}ing");
            IsDeployed = false;

            OnRetract.Invoke();
        }
        #endregion

        #region Tilt
        public void Tilt(InputAction.CallbackContext context)
        {
            if (!enabled) return;

            tiltInput = context.ReadValue<Vector2>();
        }

        private IEnumerator TiltRoutine()
        {
            Rigidbody.DisableRotationConstrains();

            while (enabled && skydiver.IsHorizontal && IsDeployed)
            {
                ApplyTilt(Vector3.right, maxRoll, 0, Vector3.down);
                ApplyTilt(Vector3.up, maxPitch, 1, Vector3.right);

                AdjustHeading();

                yield return new WaitForFixedUpdate();
            }

            Rigidbody.EnableRotationConstrains();

            tiltRoutine = null;
        }

        private void ApplyTilt(Vector3 localSlopeAxis, float maxTilt, int inputIndex, Vector3 localTorqueAxis)
        {
            var slopeAxis = Rigidbody.rotation * localSlopeAxis;
            var tiltMagnitude = Mathf.Abs(Vector3.Angle(slopeAxis, skydiver.UpDirection) - 90f);
            if (tiltMagnitude <= maxTilt)
            {
                var torque = tiltInput[inputIndex] * tiltTorque[inputIndex] * localTorqueAxis;
                Rigidbody.AddRelativeTorque(torque);
            }
            else
            {
                var tiltTorqueDirection = Rigidbody.rotation * localTorqueAxis;
                var tiltTorque = Vector3.Project(Rigidbody.angularVelocity, tiltTorqueDirection);
                Rigidbody.AddTorque(-tiltTorque, ForceMode.VelocityChange);
            }
        }

        private void AdjustHeading()
        {
            if (headingAdjustSpeed == 0f) return;

            var up = skydiver.UpDirection;
            var upPlaneVelocity = Vector3.ProjectOnPlane(Rigidbody.velocity, up);
            if (upPlaneVelocity.magnitude <= headingAdjustSpeedThreshold) return;

            var upPlaneRigidbodyUp = Vector3.ProjectOnPlane(Rigidbody.rotation * Vector3.up, up);
            if (Vector3.Angle(upPlaneRigidbodyUp, upPlaneVelocity) <= headingAngleTolerance) return;

            var torque = headingAdjustSpeed * Vector3.Cross(upPlaneRigidbodyUp, upPlaneVelocity);
            Rigidbody.AddTorque(torque);
        }
        #endregion

        #region Debug
        private bool CheckReferences()
        {
            if (skydiver == null) Debug.LogError($"{nameof(skydiver)} is not assigned on {name}'s {GetType().Name}");
            else return true;

            return false;
        }

        private void AddLogToEvents()
        {
            OnDeploy.AddListener(() => Debug.Log(GetType().Name + ": " + nameof(OnDeploy)));
            OnRetract.AddListener(() => Debug.Log(GetType().Name + ": " + nameof(OnRetract)));
        }
        #endregion
    }
}