using UnityEngine;

public class InteractionContext
{
    public InteractionManager Manager { get; private set; }
    public PlayerInteractor Interactor { get; private set; }
    public InteractableObject Target { get; private set; }

    public GameObject Player { get; private set; }
    public Camera PlayerCamera { get; private set; }

    public InteractionContext(
        InteractionManager manager,
        PlayerInteractor interactor,
        InteractableObject target,
        GameObject player,
        Camera playerCamera)
    {
        Manager = manager;
        Interactor = interactor;
        Target = target;
        Player = player;
        PlayerCamera = playerCamera;
    }
}