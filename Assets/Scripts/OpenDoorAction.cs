using UnityEngine;

[CreateAssetMenu(
    fileName = "OpenDoorAction",
    menuName = "Game/Actions/Open Door")]
public class OpenDoorAction : InteractionAction
{
    public override void Execute(InteractionContext context)
    {
        LockedDoor door =
            context.Target.GetComponentInParent<LockedDoor>();

        if (door == null)
            return;

        door.TryOpen(context.Manager.inventory);
    }
}