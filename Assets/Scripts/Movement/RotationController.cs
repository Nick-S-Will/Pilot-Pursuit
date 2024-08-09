using UnityEngine;
using UnityEngine.InputSystem;

namespace PilotPursuit.Movement
{
    public class RotationController : MonoBehaviour
    {
        [SerializeField] private new Rigidbody rigidbody;
        [SerializeField] private Transform cameraTransform;
        [Header("Rotation Settings")]
        [SerializeField] private Vector2 rotationSensitivity = Vector2.one;
        [SerializeField][Range(0f, 180f)] private float maxPitchRotation = 70f;

        private Vector2 rotationInputSinceUpdate;

        public float PitchRotation
        {
            get
            {
                var pitch = cameraTransform.localEulerAngles.x;
                var transformedPitch = pitch > 180f ? pitch - 360f : pitch;

                return transformedPitch;
            }
            private set
            {
                var clampedPitch = Mathf.Clamp(value, -maxPitchRotation, maxPitchRotation);

                var euler = cameraTransform.localEulerAngles;
                cameraTransform.localEulerAngles = new Vector3(clampedPitch, euler.y, euler.z);
            }
        }

        private void Awake()
        {
            if (!CheckReferences()) { enabled = false; return; }

            PitchRotation = cameraTransform.localEulerAngles.x;
        }

        private void Update()
        {
            UpdateCameraRotation();
        }

        private void FixedUpdate()
        {
            UpdateRigidbodyRotation();
        }

        #region Rotate
        public void Rotate(InputAction.CallbackContext context) => Rotate(context.ReadValue<Vector2>());

        public void Rotate(Vector2 rotationInput) => rotationInputSinceUpdate += rotationInput;

        private void UpdateRigidbodyRotation()
        {
            var yawInput = rotationInputSinceUpdate.x;
            if (yawInput == 0f) return;
            rotationInputSinceUpdate.x = 0f;

            var yawDelta = rotationSensitivity.x * yawInput * Time.fixedDeltaTime;
            var bodyRotation = rigidbody.rotation * Quaternion.Euler(0f, yawDelta, 0f);
            rigidbody.MoveRotation(bodyRotation);
        }

        private void UpdateCameraRotation()
        {
            var pitchInput = rotationInputSinceUpdate.y;
            if (pitchInput == 0f) return;
            rotationInputSinceUpdate.y = 0f;

            var pitchDelta = rotationSensitivity.y * pitchInput * Time.deltaTime * Screen.height / Screen.dpi;
            PitchRotation += pitchDelta;
        }
        #endregion

        #region Debug
        private bool CheckReferences()
        {
            if (cameraTransform == null) Debug.LogError($"{nameof(cameraTransform)} is not assigned on {name}'s {GetType().Name}");
            else return true;

            return false;
        }
        #endregion
    }
}