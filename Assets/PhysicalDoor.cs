using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PhysicalDoor : MonoBehaviour
{
    public enum DoorType
    {
        Hinge,
        Sliding
    }

    [Header("Tipo de puerta")]
    [SerializeField] private DoorType doorType;

    [Header("Cerradura")]
    [SerializeField] private bool requiresKey;

    [Header("Física")]
    [SerializeField] private Rigidbody doorRigidbody;
    [SerializeField] private float unlockedMass = 15f;

    [Header("Capas que ignora")]
    [SerializeField] private LayerMask ignoredLayers;

    [Header("Puerta con bisagra")]
    [SerializeField] private HingeJoint hingeJoint;

    [Header("Puerta corrediza")]
    [SerializeField] private ConfigurableJoint slidingJoint;
    [SerializeField] private Vector3 slidingAxisLocal = Vector3.right;
    [SerializeField] private float slidingDistance = 1.5f;

    private bool isUnlocked;

    private void Awake()
    {
        if (doorRigidbody == null)
            doorRigidbody = GetComponent<Rigidbody>();

        if (doorRigidbody == null)
        {
            Debug.LogError("PhysicalDoor: Falta Rigidbody.", this);
            enabled = false;
            return;
        }

        doorRigidbody.isKinematic = false;
        doorRigidbody.mass = unlockedMass;
        doorRigidbody.interpolation =
            RigidbodyInterpolation.Interpolate;

        doorRigidbody.collisionDetectionMode =
            CollisionDetectionMode.Continuous;

        IgnoreSelectedLayers();

        if (doorType == DoorType.Hinge)
            SetupHingeDoor();
        else
            SetupSlidingDoor();
    }

    // IGNORAR COLISIONES POR LAYER
    private void IgnoreSelectedLayers()
    {
        int doorLayer = gameObject.layer;

        for (int i = 0; i < 32; i++)
        {
            if ((ignoredLayers.value & (1 << i)) != 0)
            {
                Physics.IgnoreLayerCollision(
                    doorLayer,
                    i,
                    true
                );
            }
        }
    }

    // CONFIGURACIÓN BISAGRA
    private void SetupHingeDoor()
    {
        if (hingeJoint == null)
            hingeJoint = GetComponent<HingeJoint>();

        if (hingeJoint == null)
        {
            Debug.LogError("PhysicalDoor: Falta HingeJoint.", this);
            enabled = false;
            return;
        }

        hingeJoint.connectedBody = null;
        hingeJoint.useMotor = false;
        hingeJoint.useSpring = false;

        JointLimits limits = hingeJoint.limits;
        limits.min = 0f;
        limits.max = 0f;

        hingeJoint.limits = limits;
        hingeJoint.useLimits = true;
    }

    // CONFIGURACIÓN CORREDIZA
    private void SetupSlidingDoor()
    {
        if (slidingJoint == null)
            slidingJoint = GetComponent<ConfigurableJoint>();

        if (slidingJoint == null)
        {
            Debug.LogError(
                "PhysicalDoor: Falta ConfigurableJoint.",
                this
            );
            enabled = false;
            return;
        }

        Vector3 axis = slidingAxisLocal.normalized;

        if (axis.sqrMagnitude < 0.001f)
            axis = Vector3.right;

        slidingJoint.autoConfigureConnectedAnchor = false;
        slidingJoint.connectedBody = null;

        slidingJoint.connectedAnchor =
            transform.TransformPoint(slidingJoint.anchor);

        slidingJoint.axis = axis;

        Vector3 secondary = Vector3.up;

        if (Mathf.Abs(Vector3.Dot(axis, secondary)) > 0.95f)
            secondary = Vector3.forward;

        slidingJoint.secondaryAxis =
            Vector3.Cross(axis, secondary).normalized;

        slidingJoint.xMotion = ConfigurableJointMotion.Limited;
        slidingJoint.yMotion = ConfigurableJointMotion.Locked;
        slidingJoint.zMotion = ConfigurableJointMotion.Locked;

        slidingJoint.angularXMotion =
            ConfigurableJointMotion.Locked;

        slidingJoint.angularYMotion =
            ConfigurableJointMotion.Locked;

        slidingJoint.angularZMotion =
            ConfigurableJointMotion.Locked;

        SoftJointLimit limit = slidingJoint.linearLimit;
        limit.limit = 0f;

        slidingJoint.linearLimit = limit;
    }

    // DESBLOQUEAR PUERTA
    public void TryUnlock(InteractionInventory inventory)
    {
        if (isUnlocked)
            return;

        if (requiresKey &&
            (inventory == null || !inventory.HasBuildingKey))
        {
            Debug.Log("Necesitas una llave para abrir esta puerta.");
            return;
        }

        isUnlocked = true;

        if (doorType == DoorType.Hinge)
        {
            hingeJoint.useLimits = false;
        }
        else
        {
            SoftJointLimit limit = slidingJoint.linearLimit;
            limit.limit = slidingDistance;

            slidingJoint.linearLimit = limit;
        }

        Debug.Log("Puerta desbloqueada. Ahora puedes moverla.");
    }

    public bool IsUnlocked => isUnlocked;
}