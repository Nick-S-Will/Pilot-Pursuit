using UnityEngine;

namespace PilotPursuit.Vehicles
{
    public abstract class Vehicle : MonoBehaviour
    {
        [SerializeField] private new Rigidbody rigidbody;

        public Rigidbody Rigidbody => rigidbody;

        protected virtual void Awake()
        {
            if (!CheckReferences()) enabled = false;
        }

        public abstract void Control(Vector3 input);

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