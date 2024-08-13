using PilotPursuit.Movement;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace PilotPursuit.Gadgets
{
    public class WingSuitController : MonoBehaviour
    {
        [SerializeField] private new Rigidbody rigidbody;
        [SerializeField] private Collider runCollider;
        [Header("Flight Settings")]
        [SerializeField][Min(0f)] private float liftForce = 500f;
        [Header("Rotation Settings")]
        [SerializeField][Min(1e-5f)] private float rotationSpeed = 180f;
        [SerializeField][Min(0f)] private float angleTolerance = 1f;
        [Header("Physics Checks")]
        [SerializeField] private LayerMask groundMask;
        [Header("Events")]
        public UnityEvent OnDeploy;
        public UnityEvent OnRetract;
        [Header("Debug")]
        [SerializeField] private bool logEvents;

        private Coroutine flightRoutine;
        private bool isDeployed;

        public Func<Vector3> GetUpDirection { get; set; } = () => Vector3.up;
        public Rigidbody Rigidbody => rigidbody;
        public bool IsFlying => flightRoutine != null;
        public bool IsDeployed => enabled && isDeployed;

        private void Awake()
        {
            if (!CheckReferences()) enabled = false;
            if (logEvents) AddLogToEvents();
        }

        private void FixedUpdate()
        {
            RetractIfNearGround();
        }

        #region Deployment
        public void TryToggleDeployed(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                if (isDeployed) Retract();
                else TryDeploy();
            }
        }

        public void TryDeploy()
        {
            if (!enabled || IsNearGround()) return;

            isDeployed = true;
            if (IsFlying) StopCoroutine(flightRoutine);
            flightRoutine = StartCoroutine(FlightRoutine());
        }

        public void Retract()
        {
            if (!enabled) return;

            if (isDeployed) OnRetract.Invoke();
            else Debug.LogWarning("Wingsuit must be deployed first");
            isDeployed = false;
        }
        #endregion

        #region Flight 
        private IEnumerator FlightRoutine()
        {
            OnDeploy.Invoke();

            //rigidbody.DisableRotationConstrains();
            StartCoroutine(RotateUpRoutine(Vector3.back));

            while (IsDeployed)
            {
                var rigidbodyBack = rigidbody.rotation * Vector3.back;
                var angle = Vector3.Angle(GetUpDirection(), rigidbodyBack) * Mathf.Deg2Rad;
                var influence = Mathf.Cos(angle);
                rigidbody.AddForce(influence * liftForce * rigidbodyBack);

                yield return new WaitForFixedUpdate();
            }

            yield return StartCoroutine(RotateUpRoutine(Vector3.up, true));
            //rigidbody.EnableRotationConstrains();

            flightRoutine = null;
        }
        #endregion

        #region Rotation
        /// <summary>
        /// Rotates <see cref="rigidbody"/> so its <paramref name="localDirectionToRotate"/> points to <see cref="GetUpDirection"/>.
        /// </summary>
        /// <param name="localDirectionToRotate">The local direction from the <see cref="rigidbody"/> that is made to point up</param>
        /// <param name="alwaysComplete">Whether or not to complete the routine when this is disabled or retracted</param>
        private IEnumerator RotateUpRoutine(Vector3 localDirectionToRotate, bool alwaysComplete = false)
        {
            while (alwaysComplete || IsDeployed)
            {
                var targetRotation = GetTargetRotation(localDirectionToRotate);
                var rotation = Quaternion.RotateTowards(rigidbody.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
                rigidbody.MoveRotation(rotation);

                if (Quaternion.Angle(rotation, targetRotation) <= angleTolerance) break;

                yield return new WaitForFixedUpdate();
            }

            if (alwaysComplete || IsDeployed)
            {
                rigidbody.MoveRotation(GetTargetRotation(localDirectionToRotate));
            }
        }

        /// <summary>
        /// Gets the final rotation for <see cref="rigidbody"/> once its <paramref name="localDirectionToRotate"/> points to <see cref="GetUpDirection"/>
        /// </summary>
        private Quaternion GetTargetRotation(Vector3 localDirectionToRotate)
        {
            var directionToRotate = rigidbody.rotation * localDirectionToRotate;
            var targetDirection = GetUpDirection();
            return rigidbody.rotation * Quaternion.FromToRotation(directionToRotate, targetDirection);
        }
        #endregion

        #region Physics Checks
        private void RetractIfNearGround()
        {
            if (!IsDeployed) return;

            if (IsNearGround()) Retract();
        }

        private bool IsNearGround()
        {
            var bounds = runCollider.bounds;
            var orientation = rigidbody.rotation;
            var maxDistance = GetMinGroundDistance();
            var layerMask = groundMask;
            var isNearGround = Physics.BoxCast(bounds.center, bounds.size / 2f, -GetUpDirection(), orientation, maxDistance, layerMask);

            return isNearGround;
        }

        private float GetMinGroundDistance()
        {
            var angleToStraighten = Quaternion.Angle(rigidbody.rotation, GetTargetRotation(Vector3.up));
            var timeToStraighten = angleToStraighten / rotationSpeed;
            return (timeToStraighten * Vector3.Project(rigidbody.velocity, GetUpDirection())).magnitude;
        }
        #endregion

        #region Debug
        private bool CheckReferences()
        {
            if (rigidbody == null) Debug.LogError($"{nameof(rigidbody)} is not assigned on {name}'s {GetType().Name}");
            else if (runCollider == null) Debug.LogError($"{nameof(runCollider)} is not assigned on {name}'s {GetType().Name}");
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