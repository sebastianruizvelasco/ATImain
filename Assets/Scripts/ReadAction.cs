using UnityEngine;

[CreateAssetMenu(
    fileName = "ReadAction",
    menuName = "Game/Actions/Read")]
public class ReadAction : InteractionAction
{
    public override void Execute(InteractionContext context)
    {
        context.Manager.ShowReading(context.Target.Data);
    }
}