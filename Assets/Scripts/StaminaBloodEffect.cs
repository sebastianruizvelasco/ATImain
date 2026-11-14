using UnityEngine;

/// <summary>
/// Deja manchas de sangre en el suelo cuando se agota la estamina.
/// Al agotarse suelta una ráfaga de manchas planas alrededor de los pies del jugador y,
/// mientras sigue agotado, sigue soltando algunas. Una vez creada, cada mancha:
///  - se queda exactamente donde apareció (sin velocidad ni movimiento),
///  - mantiene su tamaño,
///  - tarda mucho en desaparecer (se desvanece solo al final de su vida).
/// No necesita materiales ni texturas: genera todo por código.
/// </summary>
[AddComponentMenu("Primera Persona/Stamina Blood Effect")]
[RequireComponent(typeof(PlayerStamina))]
public class StaminaBloodEffect : MonoBehaviour
{
    [Header("Aspecto")]
    [Tooltip("Color más oscuro de la sangre.")]
    [SerializeField] private Color darkBlood = new Color(0.70f, 0.00f, 0.02f, 1f);

    [Tooltip("Color más claro de la sangre. Cada mancha toma un color entre los dos.")]
    [SerializeField] private Color lightBlood = new Color(1.00f, 0.05f, 0.05f, 1f);

    [Tooltip("Tamaño mínimo de cada mancha (m).")]
    [SerializeField, Min(0.01f)] private float minSize = 0.12f;

    [Tooltip("Tamaño máximo de cada mancha (m).")]
    [SerializeField, Min(0.01f)] private float maxSize = 0.35f;

    [Tooltip("Segundos que dura cada mancha en el suelo antes de desaparecer.")]
    [SerializeField, Min(1f)] private float lifetime = 30f;

    [Tooltip("Parte final de la vida (0-1) en la que la mancha se desvanece. El resto del tiempo es totalmente visible.")]
    [SerializeField, Range(0.05f, 1f)] private float fadeFraction = 0.2f;

    [Header("Emisión")]
    [Tooltip("Manchas que salen de golpe al agotarse la estamina.")]
    [SerializeField, Min(0)] private int burstCount = 40;

    [Tooltip("Manchas por segundo mientras el jugador sigue agotado.")]
    [SerializeField, Min(0f)] private float exhaustedRate = 5f;

    [Tooltip("Radio de la zona del suelo donde aparecen (m).")]
    [SerializeField, Min(0.05f)] private float radius = 0.7f;

    [Tooltip("Altura sobre los pies del jugador (m), para evitar que parpadee con el suelo.")]
    [SerializeField] private float heightOffset = 0.03f;

    [Tooltip("Máximo de manchas vivas a la vez.")]
    [SerializeField, Min(1)] private int maxStains = 400;

    private PlayerStamina stamina;
    private ParticleSystem blood;
    private Material material;
    private Texture2D stainTexture;

    private void Awake()
    {
        stamina = GetComponent<PlayerStamina>();
        BuildParticleSystem();
    }

    private void OnEnable()
    {
        stamina.Exhausted += OnExhausted;
        stamina.Recovered += OnRecovered;
    }

    private void OnDisable()
    {
        stamina.Exhausted -= OnExhausted;
        stamina.Recovered -= OnRecovered;
    }

    private void OnDestroy()
    {
        if (material != null)
            Destroy(material);
        if (stainTexture != null)
            Destroy(stainTexture);
    }

    private void OnExhausted()
    {
        if (blood == null)
            return;

        blood.Emit(burstCount);
        SetContinuousEmission(true);
    }

    private void OnRecovered()
    {
        SetContinuousEmission(false);
    }

    private void SetContinuousEmission(bool enabled)
    {
        ParticleSystem.EmissionModule emission = blood.emission;
        emission.enabled = enabled;
    }

    private void BuildParticleSystem()
    {
        var go = new GameObject("Sangre de cansancio");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, heightOffset, 0f);

        blood = go.AddComponent<ParticleSystem>();
        blood.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = blood.main;
        main.playOnAwake = false;
        main.loop = true;
        main.duration = 1f;
        main.simulationSpace = ParticleSystemSimulationSpace.World; // Se quedan en el suelo, no siguen al jugador.
        main.startLifetime = lifetime;
        main.startSpeed = 0f;          // Sin movimiento: la posición queda fijada al nacer.
        main.gravityModifier = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize); // Se elige al nacer y no cambia.
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(darkBlood, lightBlood);
        main.maxParticles = maxStains;

        // Zona circular tumbada sobre el suelo (el círculo por defecto está en el plano XY).
        ParticleSystem.ShapeModule shape = blood.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius;
        shape.radiusThickness = 1f;
        shape.rotation = new Vector3(90f, 0f, 0f);

        ParticleSystem.EmissionModule emission = blood.emission;
        emission.rateOverTime = exhaustedRate;
        emission.enabled = false;

        // Solo cambia la opacidad: totalmente visible casi toda la vida y se desvanece al final.
        // No hay módulos de tamaño ni de velocidad durante la vida, así que no se mueve ni cambia de tamaño.
        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = blood.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f - fadeFraction),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = fade;

        var particleRenderer = blood.GetComponent<ParticleSystemRenderer>();
        particleRenderer.renderMode = ParticleSystemRenderMode.HorizontalBillboard; // Planas sobre el suelo.
        particleRenderer.sharedMaterial = CreateMaterial();
        particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        particleRenderer.receiveShadows = false;

        blood.Play();
    }

    private Material CreateMaterial()
    {
        // "Sprites/Default" siempre está incluido en las builds y funciona igual en URP y en el pipeline clásico.
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");

        stainTexture = CreateStainTexture(64);
        material = new Material(shader)
        {
            name = "SangreCansancio (runtime)",
            mainTexture = stainTexture
        };
        return material;
    }

    /// <summary>Mancha redonda totalmente opaca, con solo un filo muy fino suavizado.</summary>
    private static Texture2D CreateStainTexture(int size)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "ManchaSangre (runtime)",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        float center = (size - 1) * 0.5f;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center)) / center;
                float alpha = 1f - Mathf.SmoothStep(0.88f, 1f, Mathf.Clamp01(distance));
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return texture;
    }
}
