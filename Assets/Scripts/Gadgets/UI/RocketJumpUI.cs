using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PilotPursuit.Gadgets.UI
{
    public class RocketJumpUI : MonoBehaviour
    {
        [SerializeField] private RocketJumpController rocketJump;
        [SerializeField] private Image fillImage;
        [SerializeField] private TMP_Text rocketCountText;
        [Tooltip("Array of " + nameof(GameObject) + "s to enable/disable with this")]
        [SerializeField] private GameObject[] otherVisuals;
        [Header("Visual Settings")]
        [SerializeField] private Color loadingColor = Color.red;
        [SerializeField] private Color readyColor = Color.green;
        [SerializeField][Range(0f, 1f)] private float maxFill = 1f;

        private void Awake()
        {
            if (!CheckReferences()) enabled = false;
        }

        private void OnDisable()
        {
            SetVisible(false);
        }

        private void Start()
        {
            fillImage.color = readyColor;
            rocketCountText.color = readyColor;
        }

        private void Update()
        {
            SetVisible(rocketJump.enabled);
            if (!rocketJump.enabled) return;

            var color = rocketJump.IsReadyToLaunch ? readyColor : loadingColor;
            fillImage.color = color;
            fillImage.fillAmount = Mathf.Lerp(0, maxFill, rocketJump.LoadingPercent);

            rocketCountText.color = color;
            rocketCountText.text = rocketJump.RocketsInClip.ToString();
        }

        [ContextMenu("Show UI")]
        private void Show() => SetVisible(true);

        [ContextMenu("Hide UI")]
        private void Hide() => SetVisible(false);

        private void SetVisible(bool visible)
        {
            fillImage.enabled = visible;
            rocketCountText.enabled = visible;
            foreach (var obj in otherVisuals) obj.SetActive(visible);
        }

        #region Debug
        private void OnValidate()
        {
            if (fillImage) fillImage.color = readyColor;
            if (rocketCountText) rocketCountText.color = readyColor;
        }

        private bool CheckReferences()
        {
            if (rocketJump == null) Debug.LogError($"{nameof(rocketJump)} is not assigned on {name}'s {GetType().Name}");
            else if (fillImage == null) Debug.LogError($"{nameof(fillImage)} is not assigned on {name}'s {GetType().Name}");
            else if (rocketCountText == null) Debug.LogError($"{nameof(rocketCountText)} is not assigned on {name}'s {GetType().Name}");
            else if (otherVisuals.Any(point => point == null)) Debug.LogError($"{nameof(otherVisuals)} contains a null reference on {name}'s {GetType().Name}");
            else return true;

            return false;
        }
        #endregion
    }
}