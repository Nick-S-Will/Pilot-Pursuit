using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace PilotPursuit.Player
{
    public class MovementController : MonoBehaviour
    {
        [SerializeField] private new Rigidbody rigidbody;
        [Header("Move Settings")]
        [SerializeField] private Vector3 groundMoveForce = 1000 * Vector3.one;
        [SerializeField] private Vector3 airMoveForce = 100 * Vector3.one;
        [Header("Physics Check")]
        [SerializeField] private LayerMask groundMask;
        [SerializeField][Min(0f)] private float maxGroundDistance = .1f, minSpeed = 0.01f;
        [Header("Events")]
        public UnityEvent OnStartMoving;
        public UnityEvent OnStopMoving;

        private Vector2 moveInput;
        private Vector3 lastVelocity;

        public bool OnGround { get; private set; }

        private void Awake()
        {
            if (!CheckReferences()) enabled = false;

            OnStartMoving.AddListener(() => print("Start"));
            OnStopMoving.AddListener(() => print("Stop"));
        }

        private void FixedUpdate()
        {
            CheckOnGround();

            Move();

            CheckMoving();
        }

        #region Physics Checks
        private void CheckOnGround()
        {
            var groundColliders = new Collider[1];
            var groundColliderCount = Physics.OverlapSphereNonAlloc(transform.position, maxGroundDistance, groundColliders, groundMask);
            OnGround = groundColliderCount > 0;
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

        #region Move
        public void Move(InputAction.CallbackContext context) => moveInput = context.ReadValue<Vector2>();

        private void Move()
        {
            if (moveInput == Vector2.zero) return;

            var localMoveInput = new Vector3(moveInput.x, 0, moveInput.y);
            var localMoveForce = Vector3.Scale(OnGround ? groundMoveForce : airMoveForce, localMoveInput);
            var moveForce = rigidbody.rotation * localMoveForce;

            rigidbody.AddForce(moveForce);
        }
        #endregion

        #region Debug
        private bool CheckReferences()
        {
            if (rigidbody == null) Debug.LogError($"{nameof(rigidbody)} is not assigned on {name}'s {nameof(MovementController)}");
            else return true;

            return false;
        }
        #endregion
    }
}