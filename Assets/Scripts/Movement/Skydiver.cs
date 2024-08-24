using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace PilotPursuit.Movement
{
    public class Skydiver : MonoBehaviour
    {
        [Tooltip("Optional " + nameof(RotationController) + " to be disabled when rotating")]
        [SerializeField] private RotationController rotationController;
        [SerializeField] private new Rigidbody rigidbody;
        [SerializeField] private new Collider collider;
        [SerializeField] private Transform bodyForwardPoint;
        [Tooltip("Points to consider when applying lift force. Leave empty for lift to ignore torque")]
        [SerializeField] private Transform[] liftPoints;
        [Header("Rotation Settings")]
        [SerializeField][Min(1e-5f)] private float rotationSpeed = 180f;
        [SerializeField][Min(0f)] private float rotationAngleTolerance = 1f;
        [Header("Lift Settings")]
        [SerializeField][Min(0f)] private float baseLiftForce = 50f;
        [SerializeField][Min(0f)] private float liftPointDirectionWeight = 1f;
        [Header("Physics Checks")]
        [SerializeField] private LayerMask groundMask = 1;
        [SerializeField][Range(0f, 1f)] private float boundExtentScale = .95f;
        [SerializeField][Min(0f)] private float minGroundDistanceToRotate = 1f;
        [Header("Events")]
        public UnityEvent OnRotateHorizontal;
        public UnityEvent OnRotateVertical;
        [Header("Debug")]
        [SerializeField] private Color gizmoColor = Color.white;
        [SerializeField] private Color liftPointColor = Color.green;
        [SerializeField][Min(1e-5f)] private float gizmoRadius = .2f;
        [SerializeField] private bool showLiftPoints, logEvents;

        private Coroutine rotationRoutine;

        public Func<Vector3> GetUpDirection { get; set; } = () => Vector3.up;
        public Vector3 UpDirection => GetUpDirection();
        public Rigidbody Rigidbody => rigidbody;
        public float TotalLiftForce => baseLiftForce + AddedLiftForce;
        public float AddedLiftForce { get; set; }
        public bool IsHorizontal { get; private set; }
        public bool IsRotating => rotationRoutine != null;

        private void Awake()
        {
            if (!CheckReferences()) enabled = false;
            if (logEvents) AddLogToEvents();

            Cursor.lockState = CursorLockMode.Confined;
        }

        private void OnDisable()
        {
            if (IsHorizontal) Rotate(false, true);
        }

        private void FixedUpdate()
        {
            if (!IsHorizontal && IsFarFromGround()) Rotate(true);
            else if (IsHorizontal && IsNearGround()) Rotate(false, true);

            if (IsHorizontal) ApplyLiftForce();
        }

        #region Rotation
        private void Rotate(bool horizontal, bool alwaysComplete = false)
        {
            if (!gameObject.activeInHierarchy || IsHorizontal == horizontal) return;

            IsHorizontal = horizontal;
            if (IsRotating) StopCoroutine(rotationRoutine);
            rotationRoutine = StartCoroutine(RotateRoutine(alwaysComplete));

            if (rotationController) rotationController.enabled = !IsHorizontal;
            if (IsHorizontal) OnRotateHorizontal.Invoke();
            else OnRotateVertical.Invoke();
        }

        private IEnumerator RotateRoutine(bool alwaysComplete = false)
        {
            var targetRotation = GetTargetRotation();
            while (enabled || alwaysComplete)
            {
                var rotation = Quaternion.RotateTowards(rigidbody.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
                rigidbody.MoveRotation(rotation);

                if (Quaternion.Angle(rotation, targetRotation) <= rotationAngleTolerance) break;

                yield return new WaitForFixedUpdate();
            }

            if (enabled || alwaysComplete)
            {
                rigidbody.MoveRotation(targetRotation);
            }

            rotationRoutine = null;
        }

        public Quaternion GetTargetRotation() => GetTargetRotation(IsHorizontal);

        private Quaternion GetTargetRotation(bool horizontal)
        {
            var up = UpDirection;
            var camForward = Vector3.ProjectOnPlane(bodyForwardPoint.forward, up).normalized;
            var forward = horizontal ? -up : camForward;
            var upwards = horizontal ? camForward : up;
            return Quaternion.LookRotation(forward, upwards);
        }
        #endregion

        #region Lift
        private void ApplyLiftForce()
        {
            var liftSurfaceNormal = Rigidbody.rotation * Vector3.back;
            var liftVector = Vector3.Project(-Rigidbody.velocity, liftSurfaceNormal);
            Rigidbody.AddForceAtPosition(TotalLiftForce * liftVector, GetLiftPoint());
        }

        private Vector3 GetLiftPoint()
        {
            if (liftPoints.Length == 0) return Rigidbody.position;

            var positions = liftPoints.Select(t => t.position);
            var averagePosition = positions.Average();
            var directions = positions.Select(pos => (pos - averagePosition).normalized).ToArray();
            var weights = directions.Select(direction => liftPointDirectionWeight * Vector3.Dot(direction, -UpDirection)).ToArray();

            var weightedAverageDirection = Vector3.zero;
            for (int i = 0; i < directions.Length; i++) weightedAverageDirection += weights[i] * directions[i];
            weightedAverageDirection /= directions.Length;

            return averagePosition + weightedAverageDirection;
        }
        #endregion

        #region Physics Checks
        private bool IsNearGround() => IsInDistanceOfGround(GetMinGroundDistance());

        private bool IsFarFromGround() => !IsInDistanceOfGround(2f * GetMinGroundDistance());

        private bool IsInDistanceOfGround(float distance)
        {
            var center = collider.bounds.center;
            var extents = boundExtentScale * collider.bounds.extents;
            var hitGround = Physics.BoxCast(center, extents, -UpDirection, Quaternion.identity, distance, groundMask);

            return hitGround;
        }

        private float GetMinGroundDistance()
        {
            var angleToStraighten = Quaternion.Angle(rigidbody.rotation, GetTargetRotation(false));
            var timeToStraighten = angleToStraighten / rotationSpeed;
            var minGroundDistance = (timeToStraighten * Vector3.Project(rigidbody.velocity, UpDirection)).magnitude;
            minGroundDistance = Mathf.Max(minGroundDistance, minGroundDistanceToRotate);

            return minGroundDistance;
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
            else if (bodyForwardPoint == null) Debug.LogError($"{nameof(bodyForwardPoint)} is not assigned on {name}'s {GetType().Name}");
            else if (liftPoints.Any(point => point == null)) Debug.LogError($"{nameof(liftPoints)} contains a null reference on {name}'s {GetType().Name}");
            else return true;

            return false;
        }

        private void AddLogToEvents()
        {
            OnRotateHorizontal.AddListener(() => Debug.Log(GetType().Name + ": " + nameof(OnRotateHorizontal)));
            OnRotateVertical.AddListener(() => Debug.Log(GetType().Name + ": " + nameof(OnRotateVertical)));
        }
        #endregion
    }
}