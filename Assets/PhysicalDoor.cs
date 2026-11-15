
using System.Collections;
using UnityEngine;

public class PhysicalDoor : MonoBehaviour
{
    public enum DoorType
    {
        Hinge,
        Sliding
    }

    [Header("Tipo de puerta")]
    [SerializeField] private DoorType doorType;

    [Header("Configuración general")]
    [SerializeField] private float movementSpeed = 2f;
    [SerializeField] private bool startsOpen;

    [Header("Cerradura")]
    [SerializeField] private bool requiresKey;

    [Header("Puerta de bisagra")]
    [SerializeField] private Transform doorPivot;
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private bool invertDirection;

    [Header("Puerta lateral")]
    [SerializeField] private Vector3 slidingDirection = Vector3.right;
    [SerializeField] private float slidingDistance = 2f;

    private Vector3 closedPosition;
    private Vector3 openPosition;

    private Quaternion closedRotation;
    private Quaternion openRotation;

    private bool isOpen;
    private bool isMoving;

    private Coroutine movementCoroutine;

    private void Awake()
    {
        if (doorType == DoorType.Hinge)
        {
            if (doorPivot == null)
            {
                Debug.LogError(
                    "PhysicalDoor: Falta asignar DoorPivot.", this);
                enabled = false;
                return;
            }

            closedRotation = doorPivot.localRotation;

            float angle = invertDirection ? -openAngle : openAngle;

            openRotation = closedRotation *
                Quaternion.Euler(0f, angle, 0f);
        }
        else
        {
            closedPosition = transform.localPosition;

            openPosition = closedPosition +
                transform.parent.TransformDirection(slidingDirection.normalized)
                * slidingDistance;

            if (transform.parent == null)
            {
                openPosition = closedPosition +
                    slidingDirection.normalized * slidingDistance;
            }
        }

        isOpen = startsOpen;

        ApplyPosition();
    }

    public void Toggle(InteractionInventory inventory)
    {
        if (isMoving)
            return;

        if (requiresKey &&
            (inventory == null || !inventory.HasBuildingKey))
        {
            Debug.Log("La puerta está cerrada con llave.");
            return;
        }

        isOpen = !isOpen;

        if (movementCoroutine != null)
            StopCoroutine(movementCoroutine);

        movementCoroutine = StartCoroutine(MoveDoor());
    }

    private IEnumerator MoveDoor()
    {
        isMoving = true;

        if (doorType == DoorType.Hinge)
        {
            Quaternion target = isOpen ? openRotation : closedRotation;

            while (Quaternion.Angle(doorPivot.localRotation, target) > 0.1f)
            {
                doorPivot.localRotation = Quaternion.RotateTowards(
                    doorPivot.localRotation,
                    target,
                    movementSpeed * 90f * Time.deltaTime
                );

                yield return null;
            }

            doorPivot.localRotation = target;
        }
        else
        {
            Vector3 target = isOpen ? openPosition : closedPosition;

            while (Vector3.Distance(transform.localPosition, target) > 0.001f)
            {
                transform.localPosition = Vector3.MoveTowards(
                    transform.localPosition,
                    target,
                    movementSpeed * Time.deltaTime
                );

                yield return null;
            }

            transform.localPosition = target;
        }

        isMoving = false;
        movementCoroutine = null;
    }

    private void ApplyPosition()
    {
        if (doorType == DoorType.Hinge)
        {
            doorPivot.localRotation =
                isOpen ? openRotation : closedRotation;
        }
        else
        {
            transform.localPosition =
                isOpen ? openPosition : closedPosition;
        }
    }

    public bool IsOpen => isOpen;
    public bool IsMoving => isMoving;
}
