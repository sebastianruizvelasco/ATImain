using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    [Header("Referencias")]
    public InteractionUI interactionUI;
    public InspectionUI inspectionUI;
    public ReadingUI readingUI;

    public PlayerControlLock controlLock;
    public InteractionInventory inventory;

    private void Awake()
    {
        if (inspectionUI != null)
            inspectionUI.Initialize(this, controlLock);

        if (readingUI != null)
            readingUI.Initialize(this, controlLock);
    }

    public void ExecuteInteraction(
        InteractableObject target,
        InteractionContext context)
    {
        if (target == null || target.Data == null)
            return;

        InteractionAction action = target.Data.action;

        if (action != null)
            action.Execute(context);

        target.InvokeInteractionEvent();
    }

    public void ShowPrompt(InteractionData data)
    {
        if (interactionUI != null)
            interactionUI.Show(data);
    }

    public void HidePrompt()
    {
        if (interactionUI != null)
            interactionUI.Hide();
    }

    public void ShowInspection(InteractionData data)
    {
        if (inspectionUI != null)
            inspectionUI.Show(data);
    }

    public void ShowReading(InteractionData data)
    {
        if (readingUI != null)
            readingUI.Show(data);
    }
}