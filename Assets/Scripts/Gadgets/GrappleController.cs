using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace PilotPursuit.Gadgets
{
    public class GrappleController : MonoBehaviour
    {
        [SerializeField] private new Rigidbody rigidbody;
        [SerializeField] private Transform launchPointTransform, launchDirectionTransform;
        [SerializeField] private LineRenderer ropeRenderer;
        [Header("Launch Settings")]
        [SerializeField][Min(1e-5f)] private float ropeLength = 30f;
        [SerializeField][Min(1e-5f)] private float launchSpeed = 30f, grappleRange = 5e-2f;
        [Header("Reel Settings")]
        [SerializeField][Min(1e-5f)] private float reelForce = 500f;
        [SerializeField][Min(1e-5f)] private float grappleMass = 10f, rotationCorrectionSpeed = 180f;
        [SerializeField][Range(0f, 1f)] private float rotationCorrectionBias = 0.5f;
        [SerializeField] private bool disableRotationConstraintsDuringReelTo = true;
        [Header("Physics Checks")]
        [SerializeField] private LayerMask grappleMask;
        [Header("Events")]
        public UnityEvent OnGrapple;
        public UnityEvent OnGrappleReleased, OnHit, OnReelComplete;
        [Header("Debug")]
        [SerializeField] private bool logEvents;

        private Coroutine grappleRoutine;
        private Vector3 ropeEndPoint;

        public Func<Vector3> RotationCorrectionUpDirection { get; set; } = () => Vector3.up;
        public bool IsHoldingLaunch { get; private set; }
        public bool IsGrappling => grappleRoutine != null;

        private void Awake()
        {
            if (!CheckReferences()) enabled = false;
            if (logEvents) AddLogToEvents();

            Cursor.lockState = CursorLockMode.Confined;
        }

        private void OnEnable()
        {
            ropeRenderer.enabled = true;
        }

        private void OnDisable()
        {
            ropeRenderer.enabled = false;
        }

        private void Update()
        {
            UpdateRopeRenderer();
        }

        #region Rope
        private void UpdateRopeRenderer()
        {
            ropeRenderer.SetPosition(0, launchPointTransform.position);
            ropeRenderer.SetPosition(1, IsGrappling ? ropeEndPoint : launchPointTransform.position);
        }
        #endregion

        #region Launch
        public void Launch(InputAction.CallbackContext context)
        {
            if (context.performed) Launch();
            else if (context.canceled) Reel();
        }

        public void Launch()
        {
            if (!enabled) return;

            IsHoldingLaunch = true;
            grappleRoutine ??= StartCoroutine(LaunchRoutine());
        }

        private IEnumerator LaunchRoutine()
        {
            OnGrapple.Invoke();

            var ropeStartPoint = launchPointTransform.position;
            ropeEndPoint = ropeStartPoint;
            var direction = launchDirectionTransform.forward;
            RaycastHit hitInfo = default;

            while (IsHoldingLaunch && Vector3.Distance(ropeStartPoint, ropeEndPoint) <= ropeLength)
            {
                var origin = ropeEndPoint - grappleRange * direction;
                var deltaDistance = launchSpeed * Time.fixedDeltaTime;
                if (Physics.SphereCast(origin, grappleRange, direction, out hitInfo, grappleRange + deltaDistance, grappleMask))
                {
                    ropeEndPoint = hitInfo.point;
                    OnHit.Invoke();
                    break;
                }

                ropeEndPoint += deltaDistance * direction;

                if (!enabled) yield return new WaitUntil(() => enabled);
                yield return new WaitForFixedUpdate();
            }

            yield return StartCoroutine(ReelToRoutine(hitInfo));
            yield return StartCoroutine(ReelInRoutine());

            grappleRoutine = null;
        }
        #endregion

        #region Reel
        public void Reel()
        {
            if (!enabled) return;

            if (IsHoldingLaunch) OnGrappleReleased.Invoke();
            else Debug.LogWarning("Grapple must be launched first");
            IsHoldingLaunch = false;
        }

        private IEnumerator ReelToRoutine(RaycastHit hitInfo)
        {
            if (hitInfo.collider == null) yield break;

            if (disableRotationConstraintsDuringReelTo) rigidbody.constraints &= ~RigidbodyConstraints.FreezeRotation;

            var target = hitInfo.collider.transform;
            var localTargetPoint = target.InverseTransformPoint(hitInfo.point);
            while (IsHoldingLaunch)
            {
                ropeEndPoint = target.TransformPoint(localTargetPoint);
                var deltaToGrapple = ropeEndPoint - rigidbody.position;
                rigidbody.AddForceAtPosition(reelForce * deltaToGrapple, launchPointTransform.position);

                if (disableRotationConstraintsDuringReelTo) ApplyRotationCorrection();

                if (!enabled) yield return new WaitUntil(() => enabled);
                yield return new WaitForFixedUpdate();
            }

            if (disableRotationConstraintsDuringReelTo) rigidbody.constraints |= RigidbodyConstraints.FreezeRotation;
        }

        private IEnumerator ReelInRoutine()
        {
            float speed = 0f, acceleration = reelForce / grappleMass * Time.fixedDeltaTime;
            while (Vector3.Distance(launchPointTransform.position, ropeEndPoint) > 0f)
            {
                if (!enabled) yield return new WaitUntil(() => enabled);
                yield return new WaitForFixedUpdate();

                speed += acceleration;
                ropeEndPoint = Vector3.MoveTowards(ropeEndPoint, launchPointTransform.position, speed * Time.fixedDeltaTime);

                if (disableRotationConstraintsDuringReelTo) ApplyRotationCorrection();
            }

            rigidbody.MoveRotation(GetCorrectRotation());

            OnReelComplete.Invoke();
        }
        #endregion

        #region Rotation Correction
        private void ApplyRotationCorrection()
        {
            var up = RotationCorrectionUpDirection();
            var rigidbodyUp = rigidbody.rotation * up;
            var angleFromUp = Vector3.Angle(rigidbodyUp, up);
            if (angleFromUp == 0f) return;

            var correctionBias = Mathf.Lerp(rotationCorrectionBias, 1f, angleFromUp / 180f);
            var correctionAngle = correctionBias * rotationCorrectionSpeed * Time.fixedDeltaTime;
            var rotation = Quaternion.RotateTowards(rigidbody.rotation, GetCorrectRotation(), correctionAngle);
            rigidbody.MoveRotation(rotation);
        }

        private Quaternion GetCorrectRotation()
        {
            var up = RotationCorrectionUpDirection();
            var forward = Vector3.ProjectOnPlane(rigidbody.rotation * Vector3.forward, up);
            return Quaternion.LookRotation(forward, up);
        }
        #endregion

        #region Debug
        private bool CheckReferences()
        {
            if (rigidbody == null) Debug.LogError($"{nameof(rigidbody)} is not assigned on {name}'s {GetType().Name}");
            else if (launchPointTransform == null) Debug.LogError($"{nameof(launchPointTransform)} is not assigned on {name}'s {GetType().Name}");
            else if (launchDirectionTransform == null) Debug.LogError($"{nameof(launchDirectionTransform)} is not assigned on {name}'s {GetType().Name}");
            else if (ropeRenderer == null) Debug.LogError($"{nameof(ropeRenderer)} is not assigned on {name}'s {GetType().Name}");
            else if (ropeRenderer.positionCount != 2) Debug.LogError($"{nameof(ropeRenderer)} on {name}'s {GetType().Name} should have 2 positions");
            else return true;

            return false;
        }

        private void AddLogToEvents()
        {
            OnGrapple.AddListener(() => Debug.Log(GetType().Name + ": " + nameof(OnGrapple)));
            OnGrappleReleased.AddListener(() => Debug.Log(GetType().Name + ": " + nameof(OnGrappleReleased)));
            OnHit.AddListener(() => Debug.Log(GetType().Name + ": " + nameof(OnHit)));
            OnReelComplete.AddListener(() => Debug.Log(GetType().Name + ": " + nameof(OnReelComplete)));
        }
        #endregion
    }
}