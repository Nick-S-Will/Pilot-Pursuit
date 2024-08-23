using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace PilotPursuit.Vehicles
{
    public class PassengerController : MonoBehaviour
    {
        [SerializeField] private new Rigidbody rigidbody;
        [SerializeField] private Transform viewPoint;
        [Header("Physics Checks")]
        [SerializeField][Min(0f)] private float aimAssistRadius = .25f;
        [SerializeField][Min(0f)] private float maxBoardingDistance = 1f;
        [SerializeField] private LayerMask vehicleMask = 1;
        [Header("Events")]
        public UnityEvent OnBoard;
        public UnityEvent OnDisembark;
        [Header("Debug")]
        [SerializeField] private bool logEvents;

        private Vehicle vehicle;
        private RaycastHit vehicleHitInfo;
        private Vector3 vehicleControlInput;
        private bool isInVehicle;

        public Rigidbody Rigidbody => rigidbody;
        public bool HasVehicle => vehicle != null;
        public bool IsInVehicle => HasVehicle && isInVehicle;
        public bool IsPilot => IsInVehicle && this == vehicle.Pilot;

        private void Awake()
        {
            if (!CheckReferences()) enabled = false;
            if (logEvents) AddLogToEvents();
        }

        private void FixedUpdate()
        {
            if (IsInVehicle) TryControlVehicle();
            else _ = CheckForVehicle();
        }

        #region Boarding
        public void Board(InputAction.CallbackContext context)
        {
            if (!enabled || !context.performed || !HasVehicle) return;

            isInVehicle = vehicle.TryBoard(this, vehicleHitInfo.point);

            if (isInVehicle) OnBoard.Invoke();
        }

        public void Disembark(InputAction.CallbackContext context)
        {
            if (!enabled || !context.performed || !IsInVehicle) return;

            isInVehicle = !vehicle.TryDisembark(this);

            if (!isInVehicle) OnDisembark.Invoke();
        }
        #endregion

        #region Vehicle Control
        public void InputLocalX(InputAction.CallbackContext context) => InputLocal(0, context.ReadValue<float>());

        public void InputLocalY(InputAction.CallbackContext context) => InputLocal(1, context.ReadValue<float>());

        public void InputLocalZ(InputAction.CallbackContext context) => InputLocal(2, context.ReadValue<float>());

        public void InputLocal(int axisIndex, float value)
        {
            if (!enabled) return;

            vehicleControlInput[axisIndex] = value;
        }

        private void TryControlVehicle()
        {
            if (!IsPilot) return;

            vehicle.Control(vehicleControlInput);
        }
        #endregion

        #region Physics Checks
        private bool CheckForVehicle()
        {
            var ray = new Ray(viewPoint.position, viewPoint.forward);
            var hitVehicle = Physics.SphereCast(ray, aimAssistRadius, out vehicleHitInfo, maxBoardingDistance, vehicleMask);

            vehicle = hitVehicle ? vehicleHitInfo.collider.GetComponentInParent<Vehicle>() : null;

            return vehicle != null;
        }
        #endregion

        #region Debug
        private bool CheckReferences()
        {
            if (rigidbody == null) Debug.LogError($"{nameof(rigidbody)} is not assigned on {name}'s {GetType().Name}");
            else if (viewPoint == null) Debug.LogError($"{nameof(viewPoint)} is not assigned on {name}'s {GetType().Name}");
            else return true;

            return false;
        }

        private void AddLogToEvents()
        {
            OnBoard.AddListener(() => Debug.Log(GetType().Name + ": " + nameof(OnBoard)));
            OnDisembark.AddListener(() => Debug.Log(GetType().Name + ": " + nameof(OnDisembark)));
        }
        #endregion
    }
}