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
        public bool IsPilot => isInVehicle && this == vehicle.Pilot;

        private void Awake()
        {
            if (!CheckReferences()) enabled = false;
            if (logEvents) AddLogToEvents();
        }

        private void FixedUpdate()
        {
            if (isInVehicle) TryControlVehicle();
            else _ = CheckForVehicle();
        }

        #region Boarding
        public void Board(InputAction.CallbackContext context)
        {
            if (!enabled || !context.performed) return;

            isInVehicle = TryBoard();
        }

        public bool TryBoard()
        {
            if (!enabled || !HasVehicle || isInVehicle || vehicle.IsFull) return false;

            var closestVacantIndex = vehicle.VacantSeats.MinIndex((seat) => Vector3.Distance(vehicleHitInfo.point, seat.position));
            if (!closestVacantIndex.HasValue)
            {
                Debug.LogError($"Tried boarding a full {nameof(Vehicle)}");
                return false;
            }

            vehicle[closestVacantIndex.Value] = this;

            OnBoard.Invoke();

            return true;
        }

        public void Disembark(InputAction.CallbackContext context)
        {
            if (!enabled || !context.performed) return;

            isInVehicle = TryDisembark();
        }

        public bool TryDisembark()
        {
            if (!enabled || !HasVehicle || !isInVehicle || vehicle.IsEmpty) return false;

            var passengerIndex = vehicle.IndexOf(this);
            if (!passengerIndex.HasValue)
            {
                Debug.LogWarning($"Tried disembarking a {nameof(Vehicle)} that this wasn't on");
                return false;
            }

            vehicle[passengerIndex.Value] = null;

            OnDisembark.Invoke();

            return true;
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