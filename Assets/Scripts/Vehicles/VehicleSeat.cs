using Interactable;
using UnityEngine;

namespace PilotPursuit.Vehicles
{
    public class VehicleSeat : InputMapSwitchInteractable
    {
        [SerializeField] private Vehicle vehicle;
        [SerializeField] private Transform seatPoint;
        [SerializeField] private bool isPilotSeat;

        public Rigidbody VehicleRigidbody => vehicle.Rigidbody;
        public Transform SeatPoint => seatPoint;
        public override string InteractionName => "Sit";

        public override bool Interact(InteractionType interactionType)
        {
            if (!isActiveAndEnabled) return false;

            var canInteract = interactionType switch
            {
                InteractionType.On => !IsActuated,
                InteractionType.Off => IsActuated,
                _ => false,
            };

            if (canInteract) base.Interact(IsActuated ? InteractionType.Off : InteractionType.On);
            
            return canInteract;
        }

        public void ControlVehicle(Vector3 input)
        {
            if (!isPilotSeat) return;

            vehicle.Control(input);
        }

        #region Debug
        protected override bool CheckReferences()
        {
            if (!base.CheckReferences()) return false;

            if (vehicle == null) Debug.LogError($"{nameof(vehicle)} is not assigned on {name}'s {GetType().Name}");
            else if (seatPoint == null) Debug.LogError($"{nameof(seatPoint)} is not assigned on {name}'s {GetType().Name}");
            else return true;

            return false;
        }
        #endregion
    }
}