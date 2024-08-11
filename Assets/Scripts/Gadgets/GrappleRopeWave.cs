using UnityEngine;

namespace PilotPursuit.Gadgets
{
    public class GrappleRopeWave : MonoBehaviour
    {
        [SerializeField] private GrappleController grapple;
        [SerializeField] private LineRenderer ropeRenderer;
        [Header("Wave Settings")]
        [SerializeField] private Vector3 localWaveUp = Vector3.up;
        [SerializeField][Min(0f)] private float frequency = 5f, amplitude = 2.5f;
        [SerializeField] private float speed = 1f;

        private void Awake()
        {
            if (!CheckReferences()) enabled = false;
        }

        private void OnDisable()
        {
            ropeRenderer.enabled = false;
        }

        private void Update()
        {
            var isVisible = grapple.enabled && grapple.IsGrappling;
            ropeRenderer.enabled = isVisible;
            if (!isVisible) return;

            var startPoint = grapple.ReelPointTransform.position;
            var middleIndex = (ropeRenderer.positionCount - 1) / 2f;
            var ropeUsage = Mathf.Clamp01(Vector3.Distance(startPoint, grapple.LaunchEndPoint) / grapple.RopeLength);
            var waveUp = (grapple.Rigidbody.rotation * localWaveUp).normalized;
            for (int i = 0; i < ropeRenderer.positionCount; i++)
            {
                var indexPercent = i / (ropeRenderer.positionCount - 1f);
                var basePosition = Vector3.Lerp(startPoint, grapple.LaunchEndPoint, indexPercent);

                var middleIndexProximity = 1f - Mathf.Abs(i - middleIndex) / middleIndex;
                var angle = frequency * (indexPercent - speed * Time.time);
                var waveScale = ropeUsage * middleIndexProximity * amplitude * Mathf.Sin(2f * Mathf.PI * angle);
                var wavePosition = basePosition + waveScale * waveUp;

                ropeRenderer.SetPosition(i, wavePosition);
            }
        }

        #region Debug
        private bool CheckReferences()
        {
            if (grapple == null) Debug.LogError($"{nameof(grapple)} is not assigned on {name}'s {GetType().Name}");
            else if (ropeRenderer == null) Debug.LogError($"{nameof(ropeRenderer)} is not assigned on {name}'s {GetType().Name}");
            else if (ropeRenderer.positionCount < 2) Debug.LogError($"{nameof(ropeRenderer)} on {name}'s {GetType().Name} must have at least 2 positions");
            else return true;

            return false;
        }
        #endregion
    }
}