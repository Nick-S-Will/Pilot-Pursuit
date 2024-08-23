using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PilotPursuit.Vehicles
{
    public abstract class Vehicle : MonoBehaviour
    {
        [Header("Input Map Switching")]
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private string actionMapName;
        [Header("References")]
        [SerializeField] private new Rigidbody rigidbody;
        [SerializeField] private Transform[] passengerPoints;

        private PassengerController[] passengers;
        private string startActionMapName;

        public Rigidbody Rigidbody => rigidbody;
        public PassengerController this[int index]
        {
            get
            {
                if (index < 0 || index >= passengers.Length) throw new IndexOutOfRangeException($"Index \"{index}\" out of range [0, {passengers.Length - 1}]");
                return passengers[index];
            }
            private set
            {
                if (index < 0 || index >= passengers.Length) throw new IndexOutOfRangeException($"Index \"{index}\" out of range [0, {passengers.Length - 1}]");
                if (passengers[index] && value) throw new Exception($"Tried to override passenger {passengers[index].name} with {value.name}");
                if (passengers[index] == value) return;

                var isBoarding = value != null;
                var passenger = passengers[index] ? passengers[index] : value;

                passengers[index] = isBoarding ? passenger : null;
                passenger.transform.parent = isBoarding ? passengerPoints[index] : null;
                passenger.Rigidbody.SetDynamics(!isBoarding);

                if (isBoarding) passenger.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                else
                {
                    passenger.Rigidbody.velocity = Rigidbody.velocity;
                    passenger.Rigidbody.angularVelocity = Rigidbody.angularVelocity;
                }
            }
        }
        public PassengerController Pilot => passengers[0];
        public int PassengerCount => passengers.Where(passenger => passenger != null).Count();
        public int MaxPassengerCount => passengerPoints.Length;

        protected virtual void Awake()
        {
            if (!CheckReferences()) enabled = false;

            passengers = new PassengerController[passengerPoints.Length];
        }

        #region Boarding
        public bool TryBoard(PassengerController passenger, Vector3 boardPoint)
        {
            if (passenger == null || PassengerCount >= MaxPassengerCount) return false;

            if (passengers.Contains(passenger))
            {
                Debug.LogWarning($"Tried boarding a {nameof(PassengerController)} twice");
                return false;
            }

            var vacantPoints = passengerPoints.Where((point, index) => passengers[index] == null);
            var closestVacantIndex = vacantPoints.MinIndex((point) => Vector3.Distance(boardPoint, point.position));
            if (!closestVacantIndex.HasValue)
            {
                Debug.LogError($"No {nameof(passengerPoints)} are vacant");
                return false;
            }

            this[closestVacantIndex.Value] = passenger;

            startActionMapName = playerInput.currentActionMap.name;
            playerInput.SwitchCurrentActionMap(actionMapName);

            return true;
        }

        public bool TryDisembark(PassengerController passenger)
        {
            if (passenger == null || PassengerCount == 0) return false;

            var passengerIndex = passengers.IndexOf(passenger);
            if (!passengerIndex.HasValue)
            {
                Debug.LogWarning($"Tried disembarking a {nameof(PassengerController)} that wasn't on the vehicle");
                return false;
            }

            this[passengerIndex.Value] = null;

            playerInput.SwitchCurrentActionMap(startActionMapName);
            startActionMapName = null;

            return true;
        }
        #endregion

        #region Control
        public abstract void Control(Vector3 input);
        #endregion

        #region Debug
        protected virtual void OnValidate()
        {
            if (playerInput && playerInput.actions)
            {
                var actionMapNames = playerInput.actions.actionMaps.Select(map => map.name);
                if (!actionMapNames.Contains(actionMapName))
                {
                    Debug.LogError($"{nameof(actionMapName)} \"{actionMapName}\" is not one of {playerInput.actions.name}'s action maps");
                }
            }
        }

        private bool CheckReferences()
        {
            if (playerInput == null) Debug.LogError($"{nameof(playerInput)} is not assigned on {name}'s {GetType().Name}");
            else if (rigidbody == null) Debug.LogError($"{nameof(rigidbody)} is not assigned on {name}'s {GetType().Name}");
            else if (passengerPoints.Length < 1) Debug.LogError($"{nameof(passengerPoints)} can't be empty on {name}'s {GetType().Name}");
            else if (passengerPoints.Any(point => point == null)) Debug.LogError($"{nameof(passengerPoints)} contains null elements on {name}'s {GetType().Name}");
            else return true;

            return false;
        }
        #endregion
    }
}