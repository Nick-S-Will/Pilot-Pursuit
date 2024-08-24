using UnityEngine;

namespace PilotPursuit.Vehicles.VFX
{
    public class WheelVisualEffect : MonoBehaviour
    {
        [SerializeField] private WheelCollider wheelCollider;
        [SerializeField] private Transform meshTransform;
        [Header("Damping Settings")]
        [SerializeField][Min(0f)] private float rotationDamping = 2f;

        private Vector3 wheelPosition;
        private Quaternion meshStartRotation, wheelRotation;

        private void Awake()
        {
            if (!CheckReferences()) enabled = false;

            meshStartRotation = meshTransform.localRotation;
        }

        private void Update()
        {
            wheelCollider.GetWorldPose(out wheelPosition, out wheelRotation);

            var targetRotation = wheelRotation * meshStartRotation;
            var angleToTarget = Quaternion.Angle(meshTransform.rotation, targetRotation);
            var maxRotationAngle = Mathf.Exp(-rotationDamping) * angleToTarget;
            var rotation = Quaternion.RotateTowards(meshTransform.rotation, targetRotation, maxRotationAngle);
            meshTransform.SetPositionAndRotation(wheelPosition, rotation);
        }

        #region Debug
        private bool CheckReferences()
        {
            if (wheelCollider == null) Debug.LogError($"{nameof(wheelCollider)} is not assigned on {name}'s {GetType().Name}");
            else if (meshTransform == null) Debug.LogError($"{nameof(meshTransform)} is not assigned on {name}'s {GetType().Name}");
            else return true;

            return false;
        }
        #endregion
    }
}