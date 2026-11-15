using UnityEngine;

[CreateAssetMenu(
    fileName = "PickupKeyAction",
    menuName = "Game/Actions/Pickup Key")]
public class PickupKeyAction : InteractionAction
{
    public override void Execute(InteractionContext context)
    {
        InteractionInventory inventory = context.Manager.inventory;

        if (inventory == null)
            return;

        inventory.PickupBuildingKey();

        context.Target.SetInteractable(false);

        context.Target.gameObject.SetActive(false);
    }
}