using UnityEngine;

[CreateAssetMenu(
    fileName = "InspectAction",
    menuName = "Game/Actions/Inspect")]
public class InspectAction : InteractionAction
{
    public override void Execute(InteractionContext context)
    {
        context.Manager.ShowInspection(context.Target.Data);
    }
}