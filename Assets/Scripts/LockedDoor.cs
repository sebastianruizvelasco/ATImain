using UnityEngine;

public class LockedDoor : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private bool requiresKey = true;

    [SerializeField] private Animator animator;

    [SerializeField] private string openAnimation = "Open";

    [SerializeField] private bool destroyAfterOpening = false;

    private bool isOpen;

    public void TryOpen(InteractionInventory inventory)
    {
        if (isOpen)
            return;

        if (requiresKey &&
            (inventory == null || !inventory.HasBuildingKey))
        {
            Debug.Log("Necesitas una llave para abrir esta puerta.");
            return;
        }

        Open();
    }

    private void Open()
    {
        isOpen = true;

        if (animator != null)
            animator.SetTrigger(openAnimation);

        if (destroyAfterOpening)
            Destroy(gameObject);
    }
}