using System.Linq;
using UnityEngine;

namespace PilotPursuit.Vehicles
{
    public class Car : Vehicle
    {
        [SerializeField] private WheelCollider[] accelerationWheels;
        [SerializeField] private WheelCollider[] steeringWheels;
        [Header("Driving Settings")]
        [SerializeField][Min(0f)] private float accelerationTorque = 10000f;
        [SerializeField][Range(0f, 90f)] private float maxSteerAngle = 45f;

        protected override void Awake()
        {
            base.Awake();

            if (!CheckReferences()) enabled = false;
        }

        public override void Control(Vector3 input)
        {
            foreach (var wheel in accelerationWheels) wheel.motorTorque = input.z * accelerationTorque;
            foreach (var wheel in steeringWheels) wheel.steerAngle = input.x * maxSteerAngle;
        }

        #region Debug
        private bool CheckReferences()
        {
            if (accelerationWheels.Length < 1) Debug.LogError($"{nameof(accelerationWheels)} array can't be empty on {name}'s {GetType().Name}");
            else if (accelerationWheels.Any(wheel => wheel == null)) Debug.LogError($"{nameof(accelerationWheels)} contains null elements on {name}'s {GetType().Name}");
            else if (steeringWheels.Length < 1) Debug.LogError($"{nameof(steeringWheels)} array can't be empty on {name}'s {GetType().Name}");
            else if (steeringWheels.Any(wheel => wheel == null)) Debug.LogError($"{nameof(steeringWheels)} contains null elements on {name}'s {GetType().Name}");
            else return true;

            return false;
        }
        #endregion
    }
}