
using UnityEngine;

[CreateAssetMenu(
    fileName = "OpenDoorAction",
    menuName = "Game/Actions/Open Door")]
public class OpenDoorAction : InteractionAction
{
    public override void Execute(InteractionContext context)
    {
        PhysicalDoor door =
            context.Target.GetComponentInParent<PhysicalDoor>();

        if (door == null)
        {
            Debug.LogWarning("No se encontró PhysicalDoor.");
            return;
        }

        door.Toggle(context.Manager.inventory);
    }
}
