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
        [Header("Grapple Settings")]
        [SerializeField][Min(1e-5f)] private float ropeLength = 30f;
        [SerializeField][Min(1e-5f)] private float launchSpeed = 30f, grappleRange = 5e-2f, grappleMass = 10f, reelForce = 1000f;
        [SerializeField] private bool disableRotationConstraintsDuringReelTo;
        [Header("Physics Checks")]
        [SerializeField] private LayerMask grappleMask;
        [Header("Events")]
        public UnityEvent OnGrapple;
        public UnityEvent OnGrappleReleased, OnHit, OnReelComplete;
        [Header("Debug")]
        [SerializeField] private bool logEvents;

        private Coroutine grappleRoutine;
        private Vector3 ropeEndPoint;

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
            bool hasHit = false;

            while (IsHoldingLaunch && Vector3.Distance(ropeStartPoint, ropeEndPoint) <= ropeLength)
            {
                var origin = ropeEndPoint - grappleRange * direction;
                var deltaDistance = launchSpeed * Time.fixedDeltaTime;
                hasHit = Physics.SphereCast(origin, grappleRange, direction, out hitInfo, grappleRange + deltaDistance, grappleMask);
                if (hasHit)
                {
                    ropeEndPoint = hitInfo.point;
                    OnHit.Invoke();
                    break;
                }

                ropeEndPoint += deltaDistance * direction;

                if (!enabled) yield return new WaitUntil(() => enabled);
                yield return new WaitForFixedUpdate();
            }

            yield return StartCoroutine(hasHit ? ReelToRoutine(hitInfo) : ReelInRoutine());
            OnReelComplete.Invoke();

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
            if (disableRotationConstraintsDuringReelTo) rigidbody.constraints &= ~RigidbodyConstraints.FreezeRotation; // TODO: Keep player fairly upright for this

            var target = hitInfo.collider.transform;
            var localTargetPoint = target.InverseTransformPoint(hitInfo.point);
            while (IsHoldingLaunch)
            {
                ropeEndPoint = target.TransformPoint(localTargetPoint);
                var deltaToGrapple = ropeEndPoint - rigidbody.position;

                rigidbody.AddForceAtPosition(reelForce * deltaToGrapple, launchPointTransform.position);

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
                speed += acceleration;
                ropeEndPoint = Vector3.MoveTowards(ropeEndPoint, launchPointTransform.position, speed * Time.fixedDeltaTime);

                if (!enabled) yield return new WaitUntil(() => enabled);
                yield return new WaitForFixedUpdate();
            }
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