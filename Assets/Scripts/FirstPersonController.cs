
using UnityEngine;

[AddComponentMenu("Primera Persona/First Person Controller")]
[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Cámara")]
    [SerializeField] private Transform cameraTransform;

    [Header("Movimiento")]
    [SerializeField, Min(0f)] private float walkSpeed = 4.5f;
    [SerializeField, Min(0f)] private float sprintSpeed = 7.5f;
    [SerializeField, Min(0f)] private float acceleration = 25f;

    [Header("Salto")]
    [SerializeField, Min(0f)] private float jumpHeight = 1.1f;
    [SerializeField] private float gravity = -22f;
    [SerializeField, Min(0f)] private float coyoteTime = 0.12f;
    [SerializeField, Min(0f)] private float jumpBufferTime = 0.1f;

    [Header("Entrada")]
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;

    [Header("Vista")]
    [SerializeField, Min(0.001f)] private float mouseSensitivity = 0.12f;
    [SerializeField] private bool invertY = false;
    [SerializeField, Range(-89f, 0f)] private float minPitch = -85f;
    [SerializeField, Range(0f, 89f)] private float maxPitch = 85f;

    [Header("Cursor")]
    [SerializeField] private bool lockCursorOnStart = true;

    private CharacterController controller;
    private PlayerStamina stamina;

    private float jumpBufferCounter;
    private float coyoteCounter;

    private Vector3 horizontalVelocity;
    private float verticalVelocity;

    private float yaw;
    private float pitch;
    private bool cursorLocked;

    public Transform CameraTransform => cameraTransform;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        stamina = GetComponent<PlayerStamina>();

        if (cameraTransform == null)
        {
            Camera childCamera = GetComponentInChildren<Camera>(true);

            if (childCamera != null)
                cameraTransform = childCamera.transform;
        }

        if (cameraTransform == null)
        {
            Debug.LogError(
                "FirstPersonController: No se encontró la cámara.",
                this
            );
        }

        yaw = transform.eulerAngles.y;

        if (cameraTransform != null)
        {
            pitch = cameraTransform.localEulerAngles.x;

            if (pitch > 180f)
                pitch -= 360f;

            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }
    }

    private void OnEnable()
    {
        SetCursorLocked(lockCursorOnStart);
    }

    private void OnDisable()
    {
        SetCursorLocked(false);
    }

    private void Update()
    {
        UpdateCursor();
        ApplyLook();
        ApplyMovement();
    }

    private void UpdateCursor()
    {
        cursorLocked = Cursor.lockState == CursorLockMode.Locked;

        if (!lockCursorOnStart)
            return;

        if (cursorLocked && Input.GetKeyDown(KeyCode.Escape))
        {
            SetCursorLocked(false);
            return;
        }

        if (!cursorLocked && Input.GetMouseButtonDown(0))
            SetCursorLocked(true);
    }

    private void SetCursorLocked(bool locked)
    {
        cursorLocked = locked;

        Cursor.lockState = locked
            ? CursorLockMode.Locked
            : CursorLockMode.None;

        Cursor.visible = !locked;
    }

    private void ApplyLook()
    {
        if (lockCursorOnStart && !cursorLocked)
            return;

        float lookX = Input.GetAxisRaw("Mouse X");
        float lookY = Input.GetAxisRaw("Mouse Y");

        if (Mathf.Approximately(lookX, 0f) &&
            Mathf.Approximately(lookY, 0f))
            return;

        yaw += lookX * mouseSensitivity;

        float pitchDelta = lookY * mouseSensitivity;

        pitch += invertY ? pitchDelta : -pitchDelta;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        if (cameraTransform != null)
        {
            cameraTransform.localRotation =
                Quaternion.Euler(pitch, 0f, 0f);
        }
    }

    private void ApplyMovement()
    {
        float dt = Time.deltaTime;

        bool grounded = controller.isGrounded;

        Vector2 move = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );

        if (move.sqrMagnitude < 0.01f)
            move = Vector2.zero;

        Vector3 wishDirection =
            transform.right * move.x +
            transform.forward * move.y;

        if (wishDirection.sqrMagnitude > 1f)
            wishDirection.Normalize();

        // Intención de correr, sin bloquearla por falta de estamina.
        bool sprinting =
            Input.GetKey(sprintKey) &&
            move != Vector2.zero;

        if (stamina != null)
        {
            stamina.Tick(sprinting, dt);
        }

        // La velocidad disminuye progresivamente cuando hay fatiga.
        float speed = walkSpeed;

        if (sprinting)
        {
            float multiplier = stamina != null
                ? stamina.SprintSpeedMultiplier
                : 1f;

            speed = sprintSpeed * multiplier;
        }

        Vector3 targetVelocity = wishDirection * speed;

        horizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity,
            targetVelocity,
            acceleration * dt
        );

        // Gravedad y suelo.
        if (grounded)
        {
            coyoteCounter = coyoteTime;

            if (verticalVelocity < 0f)
                verticalVelocity = -2f;
        }
        else
        {
            coyoteCounter -= dt;
        }

        // Salto con buffer.
        if (Input.GetButtonDown("Jump"))
            jumpBufferCounter = jumpBufferTime;
        else
            jumpBufferCounter -= dt;

        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            verticalVelocity = Mathf.Sqrt(
                2f * jumpHeight * -gravity
            );

            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
        }

        verticalVelocity += gravity * dt;

        Vector3 motion =
            horizontalVelocity +
            Vector3.up * verticalVelocity;

        controller.Move(motion * dt);
    }
}
