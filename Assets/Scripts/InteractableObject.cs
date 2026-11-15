using UnityEngine;
using UnityEngine.Events;

public class InteractableObject : MonoBehaviour, IInteractable
{
    [SerializeField] private InteractionData data;

    [SerializeField] private bool canInteract = true;

    [SerializeField] private UnityEvent onInteract;

    public string DisplayName =>
        data != null ? data.displayName : gameObject.name;

    public string Prompt =>
        data != null ? data.prompt : "Interactuar";

    public bool CanInteract =>
        canInteract && data != null;

    public InteractionData Data => data;

    public void Interact(InteractionContext context)
    {
        if (!CanInteract)
            return;

        context.Manager.ExecuteInteraction(this, context);
    }

    public void InvokeInteractionEvent()
    {
        onInteract?.Invoke();
    }

    public void SetInteractable(bool value)
    {
        canInteract = value;
    }
}