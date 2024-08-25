using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ControlSchemeInputDisplay : MonoBehaviour
{
    [SerializeField] private Image inputImage;
    [Space]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private string actionMapName, actionName;
    [Space]
    [SerializeField] private ControlSchemeSprite[] controlSchemeSprites;

    private void Awake()
    {
        if (!CheckReferences()) enabled = false;

        playerInput.controlsChangedEvent.AddListener(UpdateGraphic);

        UpdateGraphic(playerInput);
    }

    private void OnDestroy()
    {
        if (playerInput) playerInput.controlsChangedEvent.RemoveListener(UpdateGraphic);
    }

    private void UpdateGraphic(PlayerInput playerInput)
    {
        var controlScheme = playerInput.currentControlScheme;
        var controlSchemeIndex = controlSchemeSprites.Select(scheme => scheme.controlSchemeName).IndexOf(controlScheme);
        if (!controlSchemeIndex.HasValue) throw new ArgumentException($"{nameof(playerInput)}'s control schemes don't match the validated ones", nameof(playerInput));

        inputImage.sprite = controlSchemeSprites[controlSchemeIndex.Value].inputSprite;
    }

    #region Debug
    [Serializable]
    private struct ControlSchemeSprite
    {
        public string controlSchemeName;
        public Sprite inputSprite;
    }

    private void OnValidate()
    {
        if (playerInput == null || playerInput.actions == null) return;

        var actionMap = playerInput.actions.FindActionMap(actionMapName);
        if (actionMap == null)
        {
            Debug.LogError($"Given {nameof(actionMapName)} is not one of {playerInput.actions.name}'s action maps");
        }
        else if (actionMap.FindAction(actionName) == null)
        {
            Debug.LogError($"Given {nameof(actionName)}is not one of {actionMapName}'s actions");
        }
        
        var controlSchemes = playerInput.actions.controlSchemes;
        if (controlSchemeSprites.Length != controlSchemes.Count) Array.Resize(ref controlSchemeSprites, controlSchemes.Count);
        for (int i = 0; i < controlSchemes.Count; i++) controlSchemeSprites[i].controlSchemeName = controlSchemes[i].name;
    }

    protected virtual bool CheckReferences()
    {
        if (inputImage == null) Debug.LogError($"{nameof(inputImage)} is not assigned on {name}'s {GetType().Name}");
        if (playerInput == null) Debug.LogError($"{nameof(playerInput)} is not assigned on {name}'s {GetType().Name}");
        else if (playerInput.actions == null) Debug.LogError($"{nameof(playerInput)}'s {nameof(InputActionAsset)} is not assigned on {name}'s {GetType().Name}");
        else if (controlSchemeSprites.Any(sprite => sprite.inputSprite == null)) Debug.LogError($"{nameof(controlSchemeSprites)} contains null sprites on {name}'s {GetType().Name}");
        else return true;

        return false;
    }
    #endregion
}