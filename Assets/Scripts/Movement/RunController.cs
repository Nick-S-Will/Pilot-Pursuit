using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace PilotPursuit.Movement
{
    public class RunController : MonoBehaviour
    {
        [SerializeField] private new Rigidbody rigidbody;
        [Header("Move Settings")]
        [SerializeField] private Vector3 groundMoveForce = 1000 * Vector3.one;
        [SerializeField] private Vector3 airMoveForce = 100 * Vector3.one;
        [Header("Physics Checks")]
        [SerializeField] private LayerMask groundMask;
        [SerializeField][Min(0f)] private float maxGroundDistance = .1f, minSpeed = 0.01f;
        [Header("Events")]
        public UnityEvent OnStartMoving;
        public UnityEvent OnStopMoving;

        private Vector2 moveInput;
        private Vector3 lastVelocity;

        public Rigidbody Rigidbody => rigidbody;
        public bool IsOnGround { get; private set; }

        private void Awake()
        {
            if (!CheckReferences()) enabled = false;
        }

        private void FixedUpdate()
        {
            CheckOnGround();

            Run();

            CheckMoving();
        }

        #region Physics Checks
        private void CheckOnGround()
        {
            var groundColliders = new Collider[1];
            var groundColliderCount = Physics.OverlapSphereNonAlloc(transform.position, maxGroundDistance, groundColliders, groundMask);
            IsOnGround = groundColliderCount > 0;
        }

        private void CheckMoving()
        {
            var rigidbodyUp = rigidbody.rotation * Vector3.up;
            var projectedLastSpeed = Vector3.ProjectOnPlane(lastVelocity, rigidbodyUp).magnitude;
            var projectedSpeed = Vector3.ProjectOnPlane(rigidbody.velocity, rigidbodyUp).magnitude;
            var projectedAcceleration = Vector3.ProjectOnPlane(rigidbody.GetAccumulatedForce(), rigidbodyUp).magnitude;

            if (projectedLastSpeed < minSpeed && projectedSpeed >= minSpeed) OnStartMoving.Invoke();
            else if (projectedLastSpeed >= minSpeed && projectedSpeed < minSpeed && projectedAcceleration < minSpeed) OnStopMoving.Invoke();

            lastVelocity = rigidbody.velocity;
        }
        #endregion

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

        #region Debug
        private bool CheckReferences()
        {
            if (rigidbody == null) Debug.LogError($"{nameof(rigidbody)} is not assigned on {name}'s {GetType().Name}");
            else return true;

            return false;
        }
        #endregion
    }
}