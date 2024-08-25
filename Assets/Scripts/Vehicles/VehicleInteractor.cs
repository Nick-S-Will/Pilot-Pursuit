using Interactable;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace PilotPursuit.Vehicles
{
    public class VehicleInteractor : Interactor
    {
        [Space]
        [SerializeField] private new Rigidbody rigidbody;
        [Header("Events")]
        public UnityEvent OnSit;
        public UnityEvent OnStand;
        [Header("Debug")]
        [SerializeField] private bool logEvents;

        private VehicleSeat vehicleSeat;
        private Vector3 vehicleControlInput;

        public Rigidbody Rigidbody => rigidbody;
        public bool IsInVehicle => vehicleSeat != null;

        protected override void Awake()
        {
            base.Awake();
            if (logEvents) AddLogToEvents();
        }

        protected override void FixedUpdate()
        {
            if (IsInVehicle) ControlVehicle(); 
            else base.FixedUpdate();
        }

        public override void Interact()
        {
            if (!isActiveAndEnabled || !HasInteractable) return;

            if (Interactable is not VehicleSeat vehicleSeat)
            {
                base.Interact();
                return;
            }

            if (!vehicleSeat.Interact(IsInVehicle ? InteractionType.Off : InteractionType.On)) return;

            if (vehicleSeat.IsActuated) Sit(vehicleSeat);
            else Stand();
        }

        #region Sitting
        private void Sit(VehicleSeat vehicleSeat)
        {
            transform.parent = vehicleSeat.SeatPoint;
            transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            Rigidbody.DisableDynamics();
            this.vehicleSeat = vehicleSeat;

            OnSit.Invoke();
        }

        private void Stand()
        {
            if (vehicleSeat == null) return;

            transform.parent = null;
            Rigidbody.EnableDynamics();
            Rigidbody.velocity = vehicleSeat.VehicleRigidbody.velocity;
            Rigidbody.angularVelocity = vehicleSeat.VehicleRigidbody.angularVelocity;
            vehicleSeat = null;

            OnStand.Invoke();
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

        private void ControlVehicle()
        {
            if (!IsInVehicle) return;

            vehicleSeat.ControlVehicle(vehicleControlInput);
        }
        #endregion

        #region Debug
        protected override bool CheckReferences()
        {
            if (!base.CheckReferences()) return false;

            if (rigidbody == null) Debug.LogError($"{nameof(rigidbody)} is not assigned on {name}'s {GetType().Name}");
            else return true;

            return false;
        }

        private void AddLogToEvents()
        {
            OnSit.AddListener(() => Debug.Log(GetType().Name + ": " + nameof(OnSit)));
            OnStand.AddListener(() => Debug.Log(GetType().Name + ": " + nameof(OnStand)));
        }
        #endregion
    }
}