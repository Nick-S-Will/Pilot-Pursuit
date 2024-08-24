using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Interactable
{
    public abstract class InputMapSwitchInteractable : MonoBehaviour, IInteractable
    {
        [Header("Input Map Switching")]
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private string actionMapName;

        public abstract string InteractionName { get; }
        public bool IsActuated { get; private set; }

        protected virtual void Awake()
        {
            if (!CheckReferences()) enabled = false;
        }

        public virtual bool Interact(InteractionType interactionType)
        {
            if (!isActiveAndEnabled) return false;

            IsActuated = interactionType switch
            {
                InteractionType.On => true,
                InteractionType.Off => false,
                _ => !IsActuated,
            };

            playerInput.SwitchCurrentActionMap(IsActuated ? actionMapName : playerInput.defaultActionMap);

            return true;
        }

        #region Debug
        protected virtual void OnValidate()
        {
            if (playerInput && playerInput.actions)
            {
                var actionMapNames = playerInput.actions.actionMaps.Select(map => map.name);
                if (!actionMapNames.Contains(actionMapName))
                {
                    Debug.LogError($"{nameof(actionMapName)} \"{actionMapName}\" is not one of {playerInput.actions.name}'s action maps");
                }
            }
        }

        protected virtual bool CheckReferences()
        {
            if (playerInput == null) Debug.LogError($"{nameof(playerInput)} is not assigned on {name}'s {GetType().Name}");
            else return true;

            return false;
        }
        #endregion
    }
}