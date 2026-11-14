using UnityEngine;

/// <summary>
/// Controlador en primera persona basado en CharacterController.
/// Usa el Input Manager antiguo (Edit > Project Settings > Input Manager):
/// ejes "Horizontal", "Vertical", "Mouse X", "Mouse Y" y botón "Jump".
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

    [Header("Entrada")]
    [Tooltip("Tecla para correr.")]
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;

    [Header("Vista")]
    [Tooltip("Sensibilidad del ratón (grados por píxel).")]
    [SerializeField, Min(0.001f)] private float mouseSensitivity = 0.12f;

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
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    private void ApplyLook()
    {
        // Con el cursor liberado no se gira la vista.
        if (lockCursorOnStart && !cursorLocked)
            return;

        // GetAxisRaw devuelve el desplazamiento del ratón en píxeles desde el último frame.
        float lookX = Input.GetAxisRaw("Mouse X");
        float lookY = Input.GetAxisRaw("Mouse Y");

        if (Mathf.Approximately(lookX, 0f) && Mathf.Approximately(lookY, 0f))
            return;

        float yawDelta = lookX * mouseSensitivity;
        float pitchDelta = lookY * mouseSensitivity;

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

        Vector2 move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (move.sqrMagnitude < 0.01f) // Zona muerta.
            move = Vector2.zero;

        Vector3 wishDirection = transform.right * move.x + transform.forward * move.y;
        if (wishDirection.sqrMagnitude > 1f)
            wishDirection.Normalize();

        bool sprintHeld = Input.GetKey(sprintKey);
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

        if (Input.GetButtonDown("Jump"))
            jumpBufferCounter = jumpBufferTime;
        else
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
}
