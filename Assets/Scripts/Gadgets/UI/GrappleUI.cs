using UnityEngine;
using UnityEngine.UI;

namespace PilotPursuit.Gadgets.UI
{
    public class GrappleUI : MonoBehaviour
    {
        [SerializeField] private GrappleController grapple;
        [SerializeField] private Image backgroundImage, fillImage;
        [Header("Visual Settings")]
        [SerializeField] private Color targetColor = Color.white;
        [SerializeField] private Color noTargetColor = Color.black, fillColor = Color.gray;
        [SerializeField][Range(0f, 1f)] private float maxFill = 1f;

        private void Awake()
        {
            if (!CheckReferences()) enabled = false;
        }

        private void OnDisable()
        {
            backgroundImage.enabled = false;
            fillImage.enabled = false;
        }

        private void Start()
        {
            fillImage.color = fillColor;
        }

        private void Update()
        {
            var isVisible = grapple.enabled && !grapple.IsGrappling;
            backgroundImage.enabled = isVisible;
            fillImage.enabled = isVisible;

            if (!isVisible) return;

            var launchRay = new Ray(grapple.LaunchCastTransform.position, grapple.LaunchCastTransform.forward);
            var hasTarget = Physics.SphereCast(launchRay, grapple.GrappleRange, out RaycastHit targetInfo, grapple.RopeLength, grapple.GrappleMask);

            backgroundImage.color = hasTarget ? targetColor : noTargetColor;
            fillImage.enabled = hasTarget;

            if (!hasTarget) return;

            var fillAmount = Mathf.Lerp(0f, maxFill, targetInfo.distance / grapple.RopeLength);
            fillImage.fillAmount = fillAmount;
        }

        private void OnValidate()
        {
            if (backgroundImage) backgroundImage.color = targetColor;
            if (fillImage) fillImage.color = fillColor;
        }

        #region Debug
        private bool CheckReferences()
        {
            if (grapple == null) Debug.LogError($"{nameof(grapple)} is not assigned on {name}'s {GetType().Name}");
            else if (backgroundImage == null) Debug.LogError($"{nameof(backgroundImage)} is not assigned on {name}'s {GetType().Name}");
            else if (fillImage == null) Debug.LogError($"{nameof(fillImage)} is not assigned on {name}'s {GetType().Name}");
            else return true;

            return false;
        }
        #endregion
    }
}