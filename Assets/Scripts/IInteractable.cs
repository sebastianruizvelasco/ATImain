using UnityEngine;

public interface IInteractable
{
    string DisplayName { get; }
    string Prompt { get; }
    bool CanInteract { get; }

    void Interact(InteractionContext context);
}
