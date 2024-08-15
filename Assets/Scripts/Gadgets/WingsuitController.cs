using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace PilotPursuit.Gadgets
{
    public class WingSuitController : MonoBehaviour
    {
        [SerializeField] private new Rigidbody rigidbody;
        [SerializeField] private new Collider collider;
        [SerializeField] private Transform cameraPoint;
        [Tooltip("Points to consider when applying lift force")]
        [SerializeField] private Transform[] liftPoints;
        [Header("Flight Settings")]
        [SerializeField][Min(0f)] private float liftForce = 500f;
        [SerializeField][Min(0f)] private float liftPointDirectionWeight = 1f;
        [SerializeField] private Vector2 tiltTorque = 100f * Vector2.one;
        [SerializeField][Range(0f, 1f)] private float tiltInputDeadZone = .1f;
        [Header("Rotation Settings")]
        [Tooltip("Determines how much to bias the base flight rotation towards the camera forward")]
        [SerializeField][Range(1e-5f, 1f)] private float cameraDirectionBias = .01f;
        [SerializeField][Min(1e-5f)] private float rotationSpeed = 180f;
        [Header("Physics Checks")]
        [SerializeField] private LayerMask groundMask;
        [SerializeField][Min(0f)] private float angleTolerance = 1f;
        [Header("Events")]
        public UnityEvent OnDeploy;
        public UnityEvent OnRetract, OnRetracted;
        [Header("Debug")]
        [SerializeField] private Color gizmoColor = Color.white;
        [SerializeField] private Color liftPointColor = Color.green;
        [SerializeField][Min(1e-5f)] private float gizmoRadius = .2f;
        [SerializeField] private bool showLiftPoints, logEvents;

        private Coroutine flightRoutine;
        private Vector2 tiltInput;

        public Func<Vector3> GetUpDirection { get; set; } = () => Vector3.up;
        public Rigidbody Rigidbody => rigidbody;
        public bool IsFlying => flightRoutine != null;
        public bool IsDeployed { get; private set; }

        private void Awake()
        {
            if (!CheckReferences()) enabled = false;
            if (logEvents) AddLogToEvents();

            Cursor.lockState = CursorLockMode.Confined;
        }

        private void FixedUpdate()
        {
            if (IsDeployed && IsNearGround()) Retract();
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
            if (!enabled || IsDeployed || IsNearGround()) return;

            IsDeployed = true;
            if (IsFlying) StopCoroutine(flightRoutine);
            flightRoutine = StartCoroutine(FlightRoutine());
        }

        public void Retract()
        {
            if (!enabled) return;

            if (!IsDeployed) Debug.LogWarning($"{nameof(WingSuitController)}'s {nameof(IsDeployed)} must be true before {nameof(Retract)}ing");
            IsDeployed = false;
        }
        #endregion

        #region Flight
        public void Tilt(InputAction.CallbackContext context)
        {
            if (!enabled) return;

            tiltInput = context.ReadValue<Vector2>();
        }

        private IEnumerator FlightRoutine()
        {
            OnDeploy.Invoke();
            rigidbody.DisableRotationConstrains();
            yield return StartCoroutine(RotateRoutine());

            while (enabled && IsDeployed)
            {
                var rigidbodyBack = rigidbody.rotation * Vector3.back;
                var lift = Vector3.Project(-rigidbody.velocity, rigidbodyBack);
                rigidbody.AddForceAtPosition(liftForce * lift, GetLiftPoint());

                if (tiltInput.magnitude > tiltInputDeadZone)
                {
                    var yawTorque = tiltInput.x * tiltTorque.x * Vector3.back;
                    var pitchTorque = tiltInput.y * tiltTorque.y * Vector3.right;
                    var torque = yawTorque + pitchTorque;
                    rigidbody.AddRelativeTorque(torque);
                }

                yield return new WaitForFixedUpdate();
            }

            OnRetract.Invoke();
            yield return StartCoroutine(RotateRoutine(true));
            rigidbody.EnableRotationConstrains();
            OnRetracted.Invoke();

            flightRoutine = null;
        }

        private Vector3 GetLiftPoint()
        {
            var positions = liftPoints.Select(t => t.position);
            var averagePosition = positions.Average();
            var directions = positions.Select(pos => (pos - averagePosition).normalized).ToArray();

            var downDirection = -GetUpDirection();
            var weights = directions.Select(direction => liftPointDirectionWeight * Vector3.Dot(direction, downDirection)).ToArray();

            var weightedAverageDirection = Vector3.zero;
            for (int i = 0; i < directions.Length; i++) weightedAverageDirection += weights[i] * directions[i];
            weightedAverageDirection /= directions.Length;

            return averagePosition + weightedAverageDirection;
        }
        #endregion

        #region Rotation
        private IEnumerator RotateRoutine(bool alwaysComplete = false)
        {
            while (alwaysComplete || enabled && IsDeployed)
            {
                var targetRotation = GetTargetRotation(IsDeployed);
                var rotation = Quaternion.RotateTowards(rigidbody.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
                rigidbody.MoveRotation(rotation);

                if (Quaternion.Angle(rotation, targetRotation) <= angleTolerance) break;

                yield return new WaitForFixedUpdate();
            }

            if (alwaysComplete || enabled && IsDeployed)
            {
                rigidbody.MoveRotation(GetTargetRotation(IsDeployed));
            }
        }

        private Quaternion GetTargetRotation(bool isDeployed)
        {
            var camForward = Vector3.ProjectOnPlane(cameraPoint.forward, GetUpDirection()).normalized;
            var forward = isDeployed ? Vector3.Lerp(-GetUpDirection(), camForward, cameraDirectionBias) : camForward;
            return Quaternion.LookRotation(forward, GetUpDirection());
        }
        #endregion

        #region Physics Checks
        private bool IsNearGround()
        {
            var bounds = collider.bounds;
            var maxDistance = GetMinGroundDistance();
            var isNearGround = Physics.BoxCast(bounds.center, bounds.size / 2f, -GetUpDirection(), Quaternion.identity, maxDistance, groundMask);

            return isNearGround;
        }

        private float GetMinGroundDistance()
        {
            var angleToStraighten = Quaternion.Angle(rigidbody.rotation, GetTargetRotation(false));
            var timeToStraighten = angleToStraighten / rotationSpeed;
            return (timeToStraighten * Vector3.Project(rigidbody.velocity, GetUpDirection())).magnitude;
        }
        #endregion

        #region Debug
        private void OnDrawGizmos()
        {
            if (showLiftPoints)
            {
                Gizmos.color = gizmoColor;
                for (int i = 0; i < liftPoints.Length; i++)
                {
                    var point = liftPoints[i];
                    if (point == null) continue;

                    Gizmos.DrawWireSphere(point.position, gizmoRadius);
                    Gizmos.DrawLine(point.position, liftPoints[(i + 1) % liftPoints.Length].position);
                }

                Gizmos.color = liftPointColor;
                Gizmos.DrawWireSphere(GetLiftPoint(), gizmoRadius);
            }
        }

        private bool CheckReferences()
        {
            if (rigidbody == null) Debug.LogError($"{nameof(rigidbody)} is not assigned on {name}'s {GetType().Name}");
            else if (collider == null) Debug.LogError($"{nameof(collider)} is not assigned on {name}'s {GetType().Name}");
            else if (cameraPoint == null) Debug.LogError($"{nameof(cameraPoint)} is not assigned on {name}'s {GetType().Name}");
            else if (liftPoints.Length < 4) Debug.LogError($"{nameof(liftPoints)} must have at least 4 elements on {name}'s {GetType().Name}");
            else if (liftPoints.Any(point => point == null)) Debug.LogError($"{nameof(liftPoints)} contains a null reference on {name}'s {GetType().Name}");
            else return true;

            return false;
        }

        private void AddLogToEvents()
        {
            OnDeploy.AddListener(() => Debug.Log(GetType().Name + ": " + nameof(OnDeploy)));
            OnRetract.AddListener(() => Debug.Log(GetType().Name + ": " + nameof(OnRetract)));
            OnRetracted.AddListener(() => Debug.Log(GetType().Name + ": " + nameof(OnRetracted)));
        }
        #endregion
    }
}