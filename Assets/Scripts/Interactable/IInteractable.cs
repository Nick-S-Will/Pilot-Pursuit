namespace Interactable
{
    public interface IInteractable
    {
        public string InteractionName { get; }

        public bool Interact(InteractionType interactionType);
    }
}