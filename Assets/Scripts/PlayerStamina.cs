
using System;
using UnityEngine;

[AddComponentMenu("Primera Persona/Player Stamina")]
public class PlayerStamina : MonoBehaviour
{
    [Header("Estamina")]
    [SerializeField, Min(1f)] private float maxStamina = 100f;
    [SerializeField, Min(0f)] private float drainPerSecond = 25f;
    [SerializeField, Min(0f)] private float regenPerSecond = 18f;
    [SerializeField, Min(0f)] private float regenDelay = 1f;

    [Tooltip("Estamina necesaria para volver a correr normalmente.")]
    [SerializeField, Range(0f, 1f)] private float recoverThreshold = 0.3f;

    [Header("Fatiga")]
    [Tooltip("Tiempo aproximado para alcanzar la velocidad mínima.")]
    [SerializeField, Min(0.1f)] private float fatigueTime = 3f;

    [Tooltip("Velocidad mínima al estar completamente agotado.")]
    [SerializeField, Range(0.1f, 1f)] private float minExhaustedSpeed = 0.65f;

    [Header("Barra en pantalla")]
    [SerializeField] private bool showBar = true;

    public event Action Exhausted;
    public event Action Recovered;

    public float Current { get; private set; }
    public float Max => maxStamina;

    public float Normalized =>
        maxStamina > 0f ? Current / maxStamina : 0f;

    public bool IsExhausted { get; private set; }

    public bool CanSprint => !IsExhausted && Current > 0f;

    public float Fatigue { get; private set; }

    // Multiplicador de velocidad de carrera.
    public float SprintSpeedMultiplier =>
        IsExhausted
            ? Mathf.Lerp(1f, minExhaustedSpeed, Fatigue)
            : 1f;

    private float regenTimer;

    private void Awake()
    {
        Current = maxStamina;
    }

    /// <summary>
    /// Se ejecuta una vez por frame.
    /// sprinting representa la intención de seguir corriendo.
    /// </summary>
    public void Tick(bool sprinting, float deltaTime)
    {
        if (sprinting)
        {
            // Mientras corres no recuperas estamina.
            regenTimer = regenDelay;

            if (!IsExhausted)
            {
                Current = Mathf.Max(
                    0f,
                    Current - drainPerSecond * deltaTime
                );

                if (Current <= 0f)
                {
                    Current = 0f;
                    IsExhausted = true;
                    Exhausted?.Invoke();
                }
            }
            else
            {
                // Cada vez estás más cansado.
                float fatigueIncrease = deltaTime / fatigueTime;

                Fatigue = Mathf.MoveTowards(
                    Fatigue,
                    1f,
                    fatigueIncrease
                );
            }

            return;
        }

        // Solo comienza la recuperación cuando dejas de correr.
        if (regenTimer > 0f)
        {
            regenTimer = Mathf.Max(0f, regenTimer - deltaTime);
            return;
        }

        Current = Mathf.Min(
            maxStamina,
            Current + regenPerSecond * deltaTime
        );

        if (IsExhausted &&
            Current >= maxStamina * recoverThreshold)
        {
            IsExhausted = false;
            Fatigue = 0f;

            Recovered?.Invoke();
        }
    }

    private void OnGUI()
    {
        if (!showBar)
            return;

        const float width = 220f;
        const float height = 14f;
        const float border = 2f;

        Rect area = new Rect(
            (Screen.width - width) * 0.5f,
            Screen.height - 40f,
            width,
            height
        );

        Color previous = GUI.color;

        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(area, Texture2D.whiteTexture);

        if (IsExhausted)
        {
            GUI.color = Color.Lerp(
                new Color(0.9f, 0.25f, 0.2f),
                new Color(0.45f, 0.05f, 0.05f),
                Fatigue
            );
        }
        else
        {
            GUI.color = Color.Lerp(
                new Color(0.95f, 0.75f, 0.2f),
                new Color(0.4f, 0.85f, 0.4f),
                Normalized
            );
        }

        Rect fill = new Rect(
            area.x + border,
            area.y + border,
            (width - border * 2f) * Normalized,
            height - border * 2f
        );

        GUI.DrawTexture(fill, Texture2D.whiteTexture);

        GUI.color = previous;
    }
}
