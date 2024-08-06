using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PilotPursuit.Movement
{
    public class RotationController : MonoBehaviour
    {
        [SerializeField] private new Rigidbody rigidbody;
        [Header("Rotation Settings")]
        [SerializeField] private Vector2 rotationSensitivity = Vector2.one;

        private void Awake()
        {
            if (!CheckReferences()) enabled = false;
        }

        public void Rotate(InputAction.CallbackContext context)
        {
            var rotationInput = context.ReadValue<Vector2>(); // TODO: Make cinemachine extension to update camera pitch to match y
            var rotationDelta = Time.deltaTime * Vector2.Scale(rotationInput, rotationSensitivity);

            var rotation = rigidbody.rotation * Quaternion.Euler(0f, rotationDelta.x, 0);
            rigidbody.MoveRotation(rotation);
        }

        #region Debug
        private bool CheckReferences()
        {
            if (rigidbody == null) Debug.LogError($"{nameof(rigidbody)} is not assigned on {name}'s {nameof(RunController)}");
            else return true;

            return false;
        }
        #endregion
    }
}