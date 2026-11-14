using System;
using UnityEngine;

/// <summary>
/// Sistema de estamina para el jugador en primera persona.
/// Se gasta al correr y se regenera tras una pausa. Cuando llega a cero el jugador
/// queda "agotado" y no puede volver a correr hasta recuperar un porcentaje mínimo.
/// FirstPersonController lo usa automáticamente si está en el mismo GameObject.
/// </summary>
[AddComponentMenu("Primera Persona/Player Stamina")]
public class PlayerStamina : MonoBehaviour
{
    [Header("Estamina")]
    [Tooltip("Estamina máxima.")]
    [SerializeField, Min(1f)] private float maxStamina = 100f;

    [Tooltip("Estamina que se gasta por segundo al correr.")]
    [SerializeField, Min(0f)] private float drainPerSecond = 25f;

    [Tooltip("Estamina que se recupera por segundo cuando no se corre.")]
    [SerializeField, Min(0f)] private float regenPerSecond = 18f;

    [Tooltip("Segundos sin correr antes de que empiece la regeneración.")]
    [SerializeField, Min(0f)] private float regenDelay = 1f;

    [Tooltip("Porcentaje de estamina (0-1) que hay que recuperar tras agotarse para volver a correr.")]
    [SerializeField, Range(0f, 1f)] private float recoverThreshold = 0.3f;

    [Header("Barra en pantalla")]
    [Tooltip("Dibuja una barra sencilla de estamina en la parte inferior de la pantalla.")]
    [SerializeField] private bool showBar = true;

    /// <summary>Se lanza una vez cuando la estamina llega a cero.</summary>
    public event Action Exhausted;

    /// <summary>Se lanza cuando el jugador se recupera lo suficiente tras agotarse.</summary>
    public event Action Recovered;

    public float Current { get; private set; }
    public float Max => maxStamina;
    public float Normalized => maxStamina > 0f ? Current / maxStamina : 0f;
    public bool IsExhausted { get; private set; }
    public bool CanSprint => !IsExhausted && Current > 0f;

    private float regenTimer;

    private void Awake()
    {
        Current = maxStamina;
    }

    /// <summary>
    /// Se llama una vez por frame. Gasta estamina si se está corriendo y la regenera en caso contrario.
    /// </summary>
    public void Tick(bool sprinting, float deltaTime)
    {
        if (sprinting && CanSprint)
        {
            Current = Mathf.Max(0f, Current - drainPerSecond * deltaTime);
            regenTimer = regenDelay;

            if (Current <= 0f)
            {
                IsExhausted = true;
                Exhausted?.Invoke();
            }

            return;
        }

        if (regenTimer > 0f)
        {
            regenTimer -= deltaTime;
            return;
        }

        Current = Mathf.Min(maxStamina, Current + regenPerSecond * deltaTime);

        if (IsExhausted && Current >= maxStamina * recoverThreshold)
        {
            IsExhausted = false;
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

        var area = new Rect((Screen.width - width) * 0.5f, Screen.height - 40f, width, height);
        Color previous = GUI.color;

        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(area, Texture2D.whiteTexture);

        GUI.color = IsExhausted
            ? new Color(0.9f, 0.25f, 0.2f)
            : Color.Lerp(new Color(0.95f, 0.75f, 0.2f), new Color(0.4f, 0.85f, 0.4f), Normalized);

        var fill = new Rect(area.x + border, area.y + border, (width - border * 2f) * Normalized, height - border * 2f);
        GUI.DrawTexture(fill, Texture2D.whiteTexture);

        GUI.color = previous;
    }
}
