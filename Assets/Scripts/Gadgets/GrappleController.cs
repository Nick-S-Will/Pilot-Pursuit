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
        [SerializeField] private Transform launchCameraPoint, launchStartPoint, reelForcePoint;
        [Header("Launch Settings")]
        [SerializeField][Min(1e-5f)] private float launchSpeed = 50f;
        [SerializeField][Min(1e-5f)] private float ropeLength = 30f, grappleRange = .05f;
        [Header("Reel Settings")]
        [SerializeField][Min(1e-5f)] private float reelForce = 1000f;
        [SerializeField][Min(0f)] private float reelUpBiasForce = 500f;
        [SerializeField][Min(1e-5f)] private float grappleMass = 10f;
        [Header("Rotation Correction")]
        [SerializeField][Min(1e-5f)] private float rotationCorrectionSpeed = 360f;
        [SerializeField][Range(0f, 1f)] private float minRotationCorrection = 0.75f;
        [SerializeField] private bool disableRotationConstraintsDuringReelTo = true;
        [Header("Physics Checks")]
        [SerializeField] private LayerMask grappleMask = 1;
        [Header("Events")]
        public UnityEvent OnGrapple;
        public UnityEvent OnGrappleReleased, OnHit, OnReelTo, OnReelIn, OnReelInComplete;
        [Header("Debug")]
        [SerializeField] private bool logEvents;

        private Coroutine grappleRoutine;
        private Vector3 launchEndPoint;
        private bool isHoldingLaunch;

        public Func<Vector3> GetUpDirection { get; set; } = () => Vector3.up;
        public Vector3 UpDirection => GetUpDirection();
        public Rigidbody Rigidbody => rigidbody;
        public Vector3 LaunchStartPoint => launchStartPoint.position;
        public Vector3 LaunchEndPoint => launchEndPoint;
        public float RopeLength => ropeLength;
        public float RopeUsage => IsGrappling ? Mathf.Clamp01(Vector3.Distance(launchStartPoint.position, launchEndPoint) / ropeLength) : 0f;
        public bool IsHoldingLaunch => enabled && isHoldingLaunch;
        public bool IsGrappling => grappleRoutine != null;
        public bool IsReeling { get; private set; }

        private void Awake()
        {
            if (!CheckReferences()) enabled = false;
            if (logEvents) AddLogToEvents();
        }

        public void Toggle(InputAction.CallbackContext context)
        {
            if (context.performed) Launch();
            else if (context.canceled) Reel();
        }

        #region Launch
        public void Launch()
        {
            if (!enabled) return;

            isHoldingLaunch = true;
            grappleRoutine ??= StartCoroutine(LaunchRoutine());
        }

        private IEnumerator LaunchRoutine()
        {
            OnGrapple.Invoke();

            var ropeStartPoint = GetLaunchOrigin();
            launchEndPoint = ropeStartPoint;

            var hasTarget = HasTarget(out RaycastHit targetInfo);
            var direction = hasTarget ? (targetInfo.point - ropeStartPoint).normalized : launchCameraPoint.forward;

            RaycastHit hitInfo = default;
            while (IsHoldingLaunch && Vector3.Distance(ropeStartPoint, launchEndPoint) <= ropeLength)
            {
                if (GrappleWillHit(direction, out hitInfo))
                {
                    launchEndPoint = hitInfo.point;
                    OnHit.Invoke();
                    break;
                }

                launchEndPoint += launchSpeed * Time.fixedDeltaTime * direction;

                yield return new WaitForFixedUpdate();
            }

            IsReeling = true;
            yield return StartCoroutine(ReelToRoutine(hitInfo));
            yield return StartCoroutine(ReelInRoutine());
            IsReeling = false;

            launchEndPoint = Vector3.zero;
            grappleRoutine = null;
        }
        #endregion

        #region Reel
        public void Reel()
        {
            if (!enabled) return;

            if (IsHoldingLaunch) OnGrappleReleased.Invoke();
            else Debug.LogWarning("Grapple must be launched first");
            isHoldingLaunch = false;
        }

        private IEnumerator ReelToRoutine(RaycastHit hitInfo)
        {
            if (hitInfo.collider == null) yield break;

            OnReelTo.Invoke();

            if (disableRotationConstraintsDuringReelTo) rigidbody.DisableRotationConstrains();

            var target = hitInfo.collider.transform;
            var targetBody = hitInfo.collider.attachedRigidbody;
            var localTargetPoint = target.InverseTransformPoint(hitInfo.point);
            while (IsHoldingLaunch)
            {
                launchEndPoint = target.TransformPoint(localTargetPoint);
                var force = reelForce * (launchEndPoint - reelForcePoint.position).normalized + reelUpBiasForce * UpDirection;
                rigidbody.AddForceAtPosition(force, reelForcePoint.position);
                if (targetBody) targetBody.AddForceAtPosition(-force, launchEndPoint);

                if (disableRotationConstraintsDuringReelTo) ApplyRotationCorrection();

                yield return new WaitForFixedUpdate();
            }

            if (disableRotationConstraintsDuringReelTo) rigidbody.EnableRotationConstrains();
        }

        private IEnumerator ReelInRoutine()
        {
            OnReelIn.Invoke();

            float speed = 0f, acceleration = reelForce / grappleMass * Time.fixedDeltaTime;
            while (Vector3.Distance(launchStartPoint.position, launchEndPoint) > 0f)
            {
                yield return new WaitForFixedUpdate();

                speed += acceleration;
                launchEndPoint = Vector3.MoveTowards(launchEndPoint, launchStartPoint.position, speed * Time.fixedDeltaTime);

                if (disableRotationConstraintsDuringReelTo) ApplyRotationCorrection();
            }

            rigidbody.MoveRotation(GetCorrectRotation());

            OnReelInComplete.Invoke();
        }
        #endregion

        #region Rotation Correction
        private void ApplyRotationCorrection()
        {
            var up = UpDirection;
            var rigidbodyUp = rigidbody.rotation * up;
            var angleFromUp = Vector3.Angle(rigidbodyUp, up);
            if (angleFromUp == 0f) return;

            var correctionBias = Mathf.Lerp(minRotationCorrection, 1f, angleFromUp / 180f);
            var correctionAngle = correctionBias * rotationCorrectionSpeed * Time.fixedDeltaTime;
            var rotation = Quaternion.RotateTowards(rigidbody.rotation, GetCorrectRotation(), correctionAngle);
            rigidbody.MoveRotation(rotation);
        }

        private Quaternion GetCorrectRotation()
        {
            var up = UpDirection;
            var rigidbodyForward = rigidbody.rotation * Vector3.forward;
            var correctedForward = Vector3.ProjectOnPlane(rigidbodyForward, up);
            if (correctedForward == Vector3.zero) correctedForward = rigidbodyForward;
            return Quaternion.LookRotation(correctedForward, up);
        }
        #endregion

        #region Physics Checks
        public bool HasTarget(out RaycastHit targetInfo)
        {
            var viewRay = new Ray(GetLaunchOrigin(), launchCameraPoint.forward);
            var hasTarget = Physics.SphereCast(viewRay, grappleRange, out targetInfo, ropeLength, grappleMask);

            return hasTarget;
        }

        private Vector3 GetLaunchOrigin()
        {
            var cameraDeltaToLaunch = launchStartPoint.position - launchCameraPoint.position;
            var origin = launchCameraPoint.position + Vector3.Project(cameraDeltaToLaunch, launchCameraPoint.forward);

            return origin;
        }

        private bool GrappleWillHit(Vector3 travelDirection, out RaycastHit hitInfo)
        {
            var origin = launchEndPoint - grappleRange * travelDirection;
            var deltaDistance = launchSpeed * Time.fixedDeltaTime;
            var willHit = Physics.SphereCast(origin, grappleRange, travelDirection, out hitInfo, grappleRange + deltaDistance, grappleMask);

            return willHit;
        }
        #endregion

        #region Debug
        private bool CheckReferences()
        {
            if (rigidbody == null) Debug.LogError($"{nameof(rigidbody)} is not assigned on {name}'s {GetType().Name}");
            else if (launchCameraPoint == null) Debug.LogError($"{nameof(launchCameraPoint)} is not assigned on {name}'s {GetType().Name}");
            else if (launchStartPoint == null) Debug.LogError($"{nameof(launchStartPoint)} is not assigned on {name}'s {GetType().Name}");
            else if (reelForcePoint == null) Debug.LogError($"{nameof(reelForcePoint)} is not assigned on {name}'s {GetType().Name}");
            else return true;

            return false;
        }

        private void AddLogToEvents()
        {
            OnGrapple.AddListener(() => Debug.Log(GetType().Name + ": " + nameof(OnGrapple)));
            OnGrappleReleased.AddListener(() => Debug.Log(GetType().Name + ": " + nameof(OnGrappleReleased)));
            OnHit.AddListener(() => Debug.Log(GetType().Name + ": " + nameof(OnHit)));
            OnReelTo.AddListener(() => Debug.Log(GetType().Name + ": " + nameof(OnReelTo)));
            OnReelIn.AddListener(() => Debug.Log(GetType().Name + ": " + nameof(OnReelIn)));
            OnReelInComplete.AddListener(() => Debug.Log(GetType().Name + ": " + nameof(OnReelInComplete)));
        }

#pragma warning disable UNT0001
        private void OnEnable() { } // To show inspector 'enabled' checkbox since methods use it
#pragma warning restore UNT0001
        #endregion
    }
}