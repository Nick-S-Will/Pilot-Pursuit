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
        [SerializeField] private Transform launchCastTransform, reelPointTransform;
        [Header("Launch Settings")]
        [SerializeField][Min(1e-5f)] private float launchSpeed = 50f;
        [SerializeField][Min(1e-5f)] private float ropeLength = 30f, grappleRange = 5e-2f;
        [Header("Reel Settings")]
        [SerializeField][Min(1e-5f)] private float reelForce = 200f;
        [SerializeField][Min(1e-5f)] private float grappleMass = 10f;
        [Header("Rotation Correction")]
        [SerializeField][Min(1e-5f)] private float rotationCorrectionSpeed = 360f;
        [SerializeField][Range(0f, 1f)] private float minRotationCorrection = 0.75f;
        [SerializeField] private bool disableRotationConstraintsDuringReelTo = true;
        [Header("Physics Checks")]
        [SerializeField] private LayerMask grappleMask;
        [Header("Events")]
        public UnityEvent OnGrapple;
        public UnityEvent OnGrappleReleased, OnHit, OnReelTo, OnReelIn, OnReelInComplete;
        [Header("Debug")]
        [SerializeField] private bool logEvents;

        private Coroutine grappleRoutine;
        private Vector3 launchEndPoint;
        private bool isHoldingLaunch;

        public Func<Vector3> RotationCorrectionUpDirection { get; set; } = () => Vector3.up;
        public Rigidbody Rigidbody => rigidbody;
        public Transform ReelPointTransform => reelPointTransform;
        public Transform LaunchCastTransform => launchCastTransform;
        public Vector3 LaunchEndPoint => launchEndPoint;
        public float RopeLength => ropeLength;
        public float GrappleRange => grappleRange;
        public LayerMask GrappleMask => grappleMask;
        public bool IsHoldingLaunch => enabled && isHoldingLaunch;
        public bool IsGrappling => grappleRoutine != null;

        private void Awake()
        {
            if (!CheckReferences()) enabled = false;
            if (logEvents) AddLogToEvents();
        }

        #region Launch
        public void Launch(InputAction.CallbackContext context)
        {
            if (context.performed) Launch();
            else if (context.canceled) Reel();
        }

        public void Launch()
        {
            if (!enabled) return;

            isHoldingLaunch = true;
            grappleRoutine ??= StartCoroutine(LaunchRoutine());
        }

        private IEnumerator LaunchRoutine()
        {
            OnGrapple.Invoke();

            var ropeStartPoint = launchCastTransform.position;
            launchEndPoint = ropeStartPoint;
            var direction = launchCastTransform.forward;
            RaycastHit hitInfo = default;

            while (IsHoldingLaunch && Vector3.Distance(ropeStartPoint, launchEndPoint) <= ropeLength)
            {
                var origin = launchEndPoint - grappleRange * direction;
                var deltaDistance = launchSpeed * Time.fixedDeltaTime;
                if (Physics.SphereCast(origin, grappleRange, direction, out hitInfo, grappleRange + deltaDistance, grappleMask))
                {
                    launchEndPoint = hitInfo.point;
                    OnHit.Invoke();
                    break;
                }

                launchEndPoint += deltaDistance * direction;

                yield return new WaitForFixedUpdate();
            }

            yield return StartCoroutine(ReelToRoutine(hitInfo));
            yield return StartCoroutine(ReelInRoutine());

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

            if (disableRotationConstraintsDuringReelTo) rigidbody.constraints &= ~RigidbodyConstraints.FreezeRotation;

            var target = hitInfo.collider.transform;
            var localTargetPoint = target.InverseTransformPoint(hitInfo.point);
            while (IsHoldingLaunch)
            {
                launchEndPoint = target.TransformPoint(localTargetPoint);
                var deltaToGrapple = launchEndPoint - rigidbody.position;
                rigidbody.AddForceAtPosition(reelForce * deltaToGrapple, reelPointTransform.position);

                if (disableRotationConstraintsDuringReelTo) ApplyRotationCorrection();

                yield return new WaitForFixedUpdate();
            }

            if (disableRotationConstraintsDuringReelTo) rigidbody.constraints |= RigidbodyConstraints.FreezeRotation;
        }

        private IEnumerator ReelInRoutine()
        {
            OnReelIn.Invoke();

            float speed = 0f, acceleration = reelForce / grappleMass * Time.fixedDeltaTime;
            while (Vector3.Distance(reelPointTransform.position, launchEndPoint) > 0f)
            {
                yield return new WaitForFixedUpdate();

                speed += acceleration;
                launchEndPoint = Vector3.MoveTowards(launchEndPoint, reelPointTransform.position, speed * Time.fixedDeltaTime);

                if (disableRotationConstraintsDuringReelTo) ApplyRotationCorrection();
            }

            rigidbody.MoveRotation(GetCorrectRotation());

            OnReelInComplete.Invoke();
        }
        #endregion

        #region Rotation Correction
        private void ApplyRotationCorrection()
        {
            var up = RotationCorrectionUpDirection();
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
            var up = RotationCorrectionUpDirection();
            var forward = Vector3.ProjectOnPlane(rigidbody.rotation * Vector3.forward, up);
            return Quaternion.LookRotation(forward, up);
        }
        #endregion

        #region Debug
        private bool CheckReferences()
        {
            if (rigidbody == null) Debug.LogError($"{nameof(rigidbody)} is not assigned on {name}'s {GetType().Name}");
            else if (reelPointTransform == null) Debug.LogError($"{nameof(reelPointTransform)} is not assigned on {name}'s {GetType().Name}");
            else if (launchCastTransform == null) Debug.LogError($"{nameof(launchCastTransform)} is not assigned on {name}'s {GetType().Name}");
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