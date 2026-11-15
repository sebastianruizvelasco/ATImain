
using UnityEngine;

[CreateAssetMenu(
    fileName = "OpenDoorAction",
    menuName = "Game/Actions/Open Door"
)]
public class OpenDoorAction : InteractionAction
{
    public override void Execute(InteractionContext context)
    {
        if (context == null || context.Target == null)
        {
            Debug.LogError("OpenDoorAction: Target es null.");
            return;
        }

        PhysicalDoor door =
            context.Target.GetComponentInParent<PhysicalDoor>();

        if (door == null)
            door = context.Target.GetComponentInChildren<PhysicalDoor>();

        if (door == null)
        {
            Debug.LogError(
                "No se encontró PhysicalDoor en " +
                context.Target.name
            );
            return;
        }

        Debug.Log("Interactuando con puerta: " + door.name);

        door.TryUnlock(context.Manager.inventory);
    }
}
