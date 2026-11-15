
using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private InteractionManager manager;

    [Header("Configuración")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactionLayers = ~0;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private bool showRaycast = true;
    [SerializeField] private Color idleColor = Color.green;
    [SerializeField] private Color hitColor = Color.red;

    private InteractableObject currentTarget;

    private Vector3 hitPoint;
    private bool hasHit;

    private void Update()
    {
        if (playerCamera == null || manager == null)
            return;

        DetectObject();

        if (Input.GetKeyDown(KeyCode.E))
            TryInteract();
    }

    private void DetectObject()
    {
        currentTarget = null;
        hasHit = false;

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            interactionDistance,
            interactionLayers,
            QueryTriggerInteraction.Ignore
        );

        System.Array.Sort(hits,
            (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.transform.root == transform.root)
                continue;

            hasHit = true;
            hitPoint = hit.point;

            if (showDebugLogs)
                Debug.DrawLine(ray.origin, hit.point, Color.yellow);

            InteractableObject candidate =
                hit.collider.GetComponentInParent<InteractableObject>();

            if (candidate == null || !candidate.CanInteract)
                continue;

            currentTarget = candidate;

            if (showDebugLogs)
                Debug.Log("Objeto detectado: " + candidate.name);

            break;
        }

        if (currentTarget != null)
            manager.ShowPrompt(currentTarget.Data);
        else
            manager.HidePrompt();
    }

    private void TryInteract()
    {
        if (currentTarget == null)
        {
            if (showDebugLogs)
                Debug.Log("No hay objeto interactuable delante.");

            return;
        }

        if (showDebugLogs)
            Debug.Log("Intentando interactuar con: " + currentTarget.name);

        InteractionContext context = new InteractionContext(
            manager,
            this,
            currentTarget,
            gameObject,
            playerCamera
        );

        currentTarget.Interact(context);
    }

    public void ClearTarget()
    {
        currentTarget = null;

        if (manager != null)
            manager.HidePrompt();
    }

    private void OnDrawGizmos()
    {
        if (!showRaycast || playerCamera == null)
            return;

        Vector3 origin = playerCamera.transform.position;
        Vector3 direction = playerCamera.transform.forward;

        Gizmos.color = hasHit ? hitColor : idleColor;

        Vector3 end = hasHit
            ? hitPoint
            : origin + direction * interactionDistance;

        Gizmos.DrawLine(origin, end);
        Gizmos.DrawWireSphere(end, 0.06f);
    }
}
