using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controlador en primera persona basado en CharacterController.
/// Lee las acciones "Move", "Look", "Jump" y "Sprint" del asset
/// InputSystem_Actions a través del componente PlayerInput.
/// </summary>
[AddComponentMenu("Primera Persona/First Person Controller")]
[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Cámara")]
    [Tooltip("Transform que gira en vertical (normalmente la cámara hija). Si se deja vacío se busca una cámara en los hijos.")]
    [SerializeField] private Transform cameraTransform;

    [Header("Movimiento")]
    [Tooltip("Velocidad al caminar (m/s).")]
    [SerializeField, Min(0f)] private float walkSpeed = 4.5f;

    [Tooltip("Velocidad al correr (m/s).")]
    [SerializeField, Min(0f)] private float sprintSpeed = 7.5f;

    [Tooltip("Aceleración y frenado horizontal (m/s²).")]
    [SerializeField, Min(0f)] private float acceleration = 25f;

    [Tooltip("Altura del salto (m).")]
    [SerializeField, Min(0f)] private float jumpHeight = 1.1f;

    [Tooltip("Gravedad aplicada al jugador (negativa).")]
    [SerializeField] private float gravity = -22f;

    [Tooltip("Tiempo extra tras abandonar el suelo en el que todavía se permite saltar (s).")]
    [SerializeField, Min(0f)] private float coyoteTime = 0.12f;

    [Tooltip("Tiempo durante el que se recuerda un salto pulsado antes de tocar el suelo (s).")]
    [SerializeField, Min(0f)] private float jumpBufferTime = 0.1f;

    [Header("Vista")]
    [Tooltip("Sensibilidad del ratón (grados por píxel).")]
    [SerializeField, Min(0.001f)] private float mouseSensitivity = 0.12f;

    [Tooltip("Sensibilidad del mando (grados por segundo).")]
    [SerializeField, Min(0.001f)] private float gamepadSensitivity = 150f;

    [Tooltip("Invertir el eje vertical de la vista.")]
    [SerializeField] private bool invertY;

    [Tooltip("Límite inferior de la rotación vertical.")]
    [SerializeField, Range(-89f, 0f)] private float minPitch = -85f;

    [Tooltip("Límite superior de la rotación vertical.")]
    [SerializeField, Range(0f, 89f)] private float maxPitch = 85f;

    [Header("Cursor")]
    [Tooltip("Bloquea el cursor al empezar. Pulsa Esc para liberarlo y haz clic para volver a bloquearlo.")]
    [SerializeField] private bool lockCursorOnStart = true;

    private CharacterController controller;
    private PlayerInput playerInput;

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction sprintAction;

    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool sprintHeld;
    private float jumpBufferCounter;

    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private float coyoteCounter;

    private float yaw;
    private float pitch;
    private bool cursorLocked;

    /// <summary>Transform usado para la vista en primera persona.</summary>
    public Transform CameraTransform => cameraTransform;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();

        if (cameraTransform == null)
        {
            Camera childCamera = GetComponentInChildren<Camera>(true);
            if (childCamera != null)
                cameraTransform = childCamera.transform;
        }

        if (cameraTransform == null)
        {
            Debug.LogError(
                "FirstPersonController: no se encontró ninguna cámara. Añade una cámara como hija del jugador o asigna 'Camera Transform'.",
                this);
        }

        if (playerInput == null)
        {
            Debug.LogWarning(
                "FirstPersonController: falta el componente PlayerInput. Añádelo al jugador, asigna InputSystem_Actions y deja 'Player' como mapa por defecto.",
                this);
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

        if (cursorLocked && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SetCursorLocked(false);
            return;
        }

        if (!cursorLocked && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            SetCursorLocked(true);
    }

    private void SetCursorLocked(bool locked)
    {
        cursorLocked = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    private void ApplyLook()
    {
        if (lookInput.sqrMagnitude < 0.0001f)
            return;

        bool usingGamepad = playerInput != null && playerInput.currentControlScheme == "Gamepad";

        float yawDelta;
        float pitchDelta;

        if (usingGamepad)
        {
            // En mando el valor es un eje continuo: se aplica por segundo.
            if (lookInput.sqrMagnitude < 0.01f)
                return;

            yawDelta = lookInput.x * gamepadSensitivity * Time.deltaTime;
            pitchDelta = lookInput.y * gamepadSensitivity * Time.deltaTime;
        }
        else
        {
            // En ratón el valor es el desplazamiento en píxeles desde el último frame.
            yawDelta = lookInput.x * mouseSensitivity;
            pitchDelta = lookInput.y * mouseSensitivity;
        }

        yaw += yawDelta;
        pitch += invertY ? pitchDelta : -pitchDelta;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void ApplyMovement()
    {
        bool grounded = controller.isGrounded;

        Vector2 move = moveInput;
        if (move.sqrMagnitude < 0.01f) // Zona muerta para el stick.
            move = Vector2.zero;

        Vector3 wishDirection = transform.right * move.x + transform.forward * move.y;
        if (wishDirection.sqrMagnitude > 1f)
            wishDirection.Normalize();

        float speed = sprintHeld ? sprintSpeed : walkSpeed;
        Vector3 targetVelocity = wishDirection * speed;

        horizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity, targetVelocity, acceleration * Time.deltaTime);

        if (grounded)
        {
            coyoteCounter = coyoteTime;
            if (verticalVelocity < 0f)
                verticalVelocity = -2f; // Mantiene al CharacterController pegado al suelo.
        }
        else
        {
            coyoteCounter -= Time.deltaTime;
        }

        jumpBufferCounter -= Time.deltaTime;

        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            verticalVelocity = Mathf.Sqrt(2f * jumpHeight * -gravity);
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 motion = horizontalVelocity + Vector3.up * verticalVelocity;
        controller.Move(motion * Time.deltaTime);
    }

    // --- Callbacks de PlayerInput (Send Messages) ---

    private void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    private void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }

    private void OnJump(InputValue value)
    {
        if (value.isPressed)
            jumpBufferCounter = jumpBufferTime;
    }

    private void OnSprint(InputValue value)
    {
        sprintHeld = value.isPressed;
    }
}
