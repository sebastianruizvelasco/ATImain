using UnityEngine;

[CreateAssetMenu(
    fileName = "HideAction",
    menuName = "Game/Actions/Hide")]
public class HideAction : InteractionAction
{
    public override void Execute(InteractionContext context)
    {
        HidingSpot spot =
            context.Target.GetComponentInParent<HidingSpot>();

        if (spot != null)
            spot.Enter(context.Player);
    }
}