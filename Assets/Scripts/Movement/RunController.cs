using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace PilotPursuit.Movement
{
    public class RunController : MonoBehaviour
    {
        [SerializeField] private new Rigidbody rigidbody;
        [Header("Run Settings")]
        [SerializeField] private Vector3 groundMoveForce = 1000 * Vector3.one;
        [SerializeField] private Vector3 airMoveForce = 100 * Vector3.one;
        [Header("Physics Checks")]
        [SerializeField] private LayerMask groundMask;
        [Tooltip("Set to 0 to disable ground check (always on ground).")]
        [SerializeField][Min(0f)] private float maxGroundDistance = .1f;
        [SerializeField][Range(0f, 180f)] private float maxInclineAngle = 40f;
        [SerializeField][Min(0f)] private float minSpeed = 0.01f;
        [Header("Events")]
        public UnityEvent OnStartMoving;
        public UnityEvent OnStopMoving;
        [Header("Debug")]
        [SerializeField] private bool logEvents;

        private Vector2 moveInput;
        private float lastProjectedSpeed;

        public Func<Vector3> GetUpDirection { get; set; } = () => Vector3.up;
        public Rigidbody Rigidbody => rigidbody;
        public bool IsOnGround { get; private set; }

        private void Awake()
        {
            if (!CheckReferences()) enabled = false;
            if (logEvents) AddLogToEvents();
        }

        private void FixedUpdate()
        {
            UpdateIsOnGround();

            if (IsFlatEnough()) Run();

            UpdateMovingState();
        }

        #region Run
        public void Run(InputAction.CallbackContext context) => moveInput = context.ReadValue<Vector2>();

        public void Run(Vector2 input) => moveInput = input;

        private void Run()
        {
            if (moveInput == Vector2.zero) return;

            var localMoveInput = new Vector3(moveInput.x, 0, moveInput.y);
            var localMoveForce = Vector3.Scale(localMoveInput, IsOnGround ? groundMoveForce : airMoveForce);

            rigidbody.AddRelativeForce(localMoveForce);
        }
        #endregion

        #region Physics Checks
        private void UpdateIsOnGround()
        {
            if (maxGroundDistance == 0f)
            {
                IsOnGround = true;
                return;
            }

            var groundColliders = new Collider[1];
            var groundColliderCount = Physics.OverlapSphereNonAlloc(transform.position, maxGroundDistance, groundColliders, groundMask);
            IsOnGround = groundColliderCount > 0;
        }

        private bool IsFlatEnough()
        {
            var rigidBodyUp = rigidbody.rotation * Vector3.up;
            var angle = Vector3.Angle(GetUpDirection(), rigidBodyUp);

            return angle <= maxInclineAngle;
        }

        private void UpdateMovingState()
        {
            var rigidbodyUp = rigidbody.rotation * Vector3.up;
            var projectedSpeed = Vector3.ProjectOnPlane(rigidbody.velocity, rigidbodyUp).magnitude;

            if (lastProjectedSpeed < minSpeed && projectedSpeed >= minSpeed) OnStartMoving.Invoke();
            else if (lastProjectedSpeed >= minSpeed && projectedSpeed < minSpeed) OnStopMoving.Invoke();

            lastProjectedSpeed = projectedSpeed;
        }
        #endregion

        #region Debug
        private bool CheckReferences()
        {
            if (rigidbody == null) Debug.LogError($"{nameof(rigidbody)} is not assigned on {name}'s {GetType().Name}");
            else return true;

            return false;
        }

        private void AddLogToEvents()
        {
            OnStartMoving.AddListener(() => Debug.Log(GetType().Name + ": " + nameof(OnStartMoving)));
            OnStopMoving.AddListener(() => Debug.Log(GetType().Name + ": " + nameof(OnStopMoving)));
        }
        #endregion
    }
}