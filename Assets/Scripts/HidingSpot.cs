using UnityEngine;

public class HidingSpot : MonoBehaviour
{
    [Header("Posiciones")]
    [SerializeField] private Transform hidePoint;
    [SerializeField] private Transform exitPoint;

    [Header("Referencias")]
    [SerializeField] private PlayerControlLock controlLock;

    private GameObject player;
    private bool isHiding;

    private void Update()
    {
        if (isHiding && Input.GetKeyDown(KeyCode.E))
        {
            Exit();
        }
    }

    public void Enter(GameObject playerObject)
    {
        if (isHiding || playerObject == null)
            return;

        player = playerObject;
        isHiding = true;

        if (hidePoint != null)
        {
            player.transform.SetPositionAndRotation(
                hidePoint.position,
                hidePoint.rotation
            );
        }

        if (controlLock != null)
            controlLock.Lock();
    }

    public void Exit()
    {
        if (!isHiding || player == null)
            return;

        if (exitPoint != null)
        {
            player.transform.SetPositionAndRotation(
                exitPoint.position,
                exitPoint.rotation
            );
        }

        isHiding = false;

        if (controlLock != null)
            controlLock.Unlock();

        player = null;
    }

    public bool IsHiding()
    {
        return isHiding;
    }
}