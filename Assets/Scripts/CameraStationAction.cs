using UnityEngine;

[CreateAssetMenu(
    fileName = "CameraStationAction",
    menuName = "Game/Actions/Camera Station")]
public class CameraStationAction : InteractionAction
{
    public override void Execute(InteractionContext context)
    {
        CCTVController cctv =
            FindFirstObjectByType<CCTVController>();

        if (cctv != null)
            cctv.Open();
    }
}