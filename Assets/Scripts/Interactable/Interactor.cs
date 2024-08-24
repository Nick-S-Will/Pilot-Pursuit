using UnityEngine;
using UnityEngine.InputSystem;

namespace Interactable
{
    public class Interactor : MonoBehaviour
    {
        [SerializeField] private Transform viewPoint;
        [Header("Physics Checks")]
        [SerializeField][Min(0f)] private float aimAssistRadius = .25f;
        [SerializeField][Min(0f)] private float maxInteractDistance = 1f;
        [SerializeField] private LayerMask interactMask = 1;

        private IInteractable interactable;
        private RaycastHit hitInfo;

        public IInteractable Interactable => interactable;
        public Vector3 InteractPoint => HasInteractable ? hitInfo.point : viewPoint.position + maxInteractDistance * viewPoint.forward;
        public bool HasInteractable => interactable != null;

        protected virtual void Awake()
        {
            if (!CheckReferences()) enabled = false;
        }

        protected virtual void FixedUpdate()
        {
            CheckForInteractable();
        }

        public void Interact(InputAction.CallbackContext context)
        {
            if (!context.performed) return;

            Interact();
        }

        public virtual void Interact()
        {
            if (!isActiveAndEnabled || !HasInteractable) return;

            _ = interactable.Interact(InteractionType.Normal);
        }

        private void CheckForInteractable()
        {
            var ray = new Ray(viewPoint.position, viewPoint.forward);
            var hitInteractable = Physics.SphereCast(ray, aimAssistRadius, out hitInfo, maxInteractDistance, interactMask);

            interactable = hitInteractable ? hitInfo.collider.GetComponentInParent<IInteractable>() : null;
        }

        #region Debug
        protected virtual void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;

            Gizmos.DrawLine(viewPoint.position, InteractPoint);
            if (hitInfo.collider != null) Gizmos.DrawWireSphere(InteractPoint, .5f);
        }

        protected virtual bool CheckReferences()
        {
            if (viewPoint == null) Debug.LogError($"{nameof(viewPoint)} is not assigned on {name}'s {GetType().Name}");
            else return true;

            return false;
        }
        #endregion
    }
}