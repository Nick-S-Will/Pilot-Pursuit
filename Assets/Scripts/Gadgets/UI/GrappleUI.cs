using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PilotPursuit.Gadgets.UI
{
    public class GrappleUI : MonoBehaviour
    {
        [SerializeField] private GrappleController grapple;
        [SerializeField] private Image backgroundImage, fillImage;
        [Header("Visual Settings")]
        [SerializeField] private Color targetColor = Color.green;
        [SerializeField] private Color noTargetColor = Color.black, grapplingColor = Color.red, fillColor = Color.blue;
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
            var isVisible = grapple.enabled;
            backgroundImage.enabled = isVisible;
            fillImage.enabled = isVisible;
            if (!isVisible) return;

            if (grapple.IsGrappling)
            {
                backgroundImage.color = grapplingColor;
                fillImage.fillAmount = GetFillAmount(grapple.RopeUsage);
            }
            else
            {
                var hasTarget = grapple.HasTarget(out RaycastHit targetInfo);
                backgroundImage.color = hasTarget ? targetColor : noTargetColor;
                fillImage.enabled = hasTarget;
                if (!hasTarget) return;

                var fillAmount = GetFillAmount(targetInfo.distance / grapple.RopeLength);
                fillImage.fillAmount = fillAmount;
            }
        }

        private float GetFillAmount(float percent) => Mathf.Lerp(0f, maxFill, percent);

        #region Debug
        private void OnValidate()
        {
            if (backgroundImage) backgroundImage.color = targetColor;
            if (fillImage) fillImage.color = fillColor;
        }

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