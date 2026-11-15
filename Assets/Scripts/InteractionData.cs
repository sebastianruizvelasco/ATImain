using UnityEngine;

[CreateAssetMenu(
    fileName = "NewInteraction",
    menuName = "Game/Interaction Data")]
public class InteractionData : ScriptableObject
{
    public string displayName = "Objeto";
    public string prompt = "Interactuar";

    public Sprite icon;

    public InteractionAction action;

    [Header("Inspección")]
    public GameObject inspectionModel;
    [TextArea(3, 8)]
    public string inspectionDescription;

    [Header("Lectura")]
    public string[] pages;
}
