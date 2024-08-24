using System.Linq;
using TMPro;
using UnityEngine;

namespace PilotPursuit.Vehicles.UI
{
    public class VehicleInteractorUI : MonoBehaviour
    {
        [SerializeField] private VehicleInteractor vehicleInteractor;
        [SerializeField] private TMP_Text promptText;
        [Tooltip("Array of " + nameof(GameObject) + "s to enable/disable with this")]
        [SerializeField] private GameObject[] otherVisuals;

        private void Awake()
        {
            if (!CheckReferences()) enabled = false;
        }

        private void OnDisable()
        {
            SetVisible(false);
        }

        private void Update()
        {
            var isVisible = vehicleInteractor.enabled && vehicleInteractor.HasInteractable && !vehicleInteractor.IsInVehicle;
            SetVisible(isVisible);
            if (!isVisible) return;

            promptText.text = vehicleInteractor.Interactable.InteractionName;
        }

        private void SetVisible(bool visible)
        {
            promptText.enabled = visible;
            foreach (var obj in otherVisuals) obj.SetActive(visible);
        }

        #region Debug
        private bool CheckReferences()
        {
            if (vehicleInteractor == null) Debug.LogError($"{nameof(vehicleInteractor)} is not assigned on {name}'s {GetType().Name}");
            else if (promptText == null) Debug.LogError($"{nameof(promptText)} is not assigned on {name}'s {GetType().Name}");
            else if (otherVisuals.Any(obj => obj == null)) Debug.LogError($"{nameof(otherVisuals)} contains a null reference on {name}'s {GetType().Name}");
            else return true;

            return false;
        }

        [ContextMenu("Show UI")]
        private void Show() => SetVisible(true);

        [ContextMenu("Hide UI")]
        private void Hide() => SetVisible(false);
        #endregion
    }
}