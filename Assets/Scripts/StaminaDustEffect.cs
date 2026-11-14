using UnityEngine;

/// <summary>
/// Deja una nube de polvo en el suelo cuando se agota la estamina.
/// Al agotarse lanza una ráfaga de partículas planas alrededor de los pies del jugador y,
/// mientras sigue agotado, sigue soltando algunas. Las partículas se quedan en el mundo
/// (no siguen al jugador) y se desvanecen con el tiempo.
/// No necesita materiales ni texturas: genera todo por código.
/// </summary>
[AddComponentMenu("Primera Persona/Stamina Dust Effect")]
[RequireComponent(typeof(PlayerStamina))]
public class StaminaDustEffect : MonoBehaviour
{
    [Header("Aspecto")]
    [Tooltip("Color base del polvo (el alfa controla la opacidad máxima).")]
    [SerializeField] private Color dustColor = new Color(0.78f, 0.72f, 0.62f, 0.8f);

    [Tooltip("Tamaño de cada partícula (m).")]
    [SerializeField, Min(0.01f)] private float particleSize = 0.35f;

    [Tooltip("Segundos que dura cada partícula en el suelo.")]
    [SerializeField, Min(0.1f)] private float particleLifetime = 3f;

    [Header("Emisión")]
    [Tooltip("Partículas que salen de golpe al agotarse la estamina.")]
    [SerializeField, Min(0)] private int burstCount = 30;

    [Tooltip("Partículas por segundo mientras el jugador sigue agotado.")]
    [SerializeField, Min(0f)] private float exhaustedRate = 6f;

    [Tooltip("Radio de la zona del suelo donde aparecen (m).")]
    [SerializeField, Min(0.05f)] private float radius = 0.6f;

    [Tooltip("Altura sobre los pies del jugador (m), para evitar que parpadee con el suelo.")]
    [SerializeField] private float heightOffset = 0.03f;

    private PlayerStamina stamina;
    private ParticleSystem dust;
    private Material material;
    private Texture2D softTexture;

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
        if (softTexture != null)
            Destroy(softTexture);
    }

    private void OnExhausted()
    {
        if (dust == null)
            return;

        dust.Emit(burstCount);
        SetContinuousEmission(true);
    }

    private void OnRecovered()
    {
        SetContinuousEmission(false);
    }

    private void SetContinuousEmission(bool enabled)
    {
        ParticleSystem.EmissionModule emission = dust.emission;
        emission.enabled = enabled;
    }

    private void BuildParticleSystem()
    {
        var go = new GameObject("Polvo de cansancio");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, heightOffset, 0f);

        dust = go.AddComponent<ParticleSystem>();
        dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = dust.main;
        main.playOnAwake = false;
        main.loop = true;
        main.duration = 1f;
        main.simulationSpace = ParticleSystemSimulationSpace.World; // Se quedan en el suelo.
        main.startLifetime = new ParticleSystem.MinMaxCurve(particleLifetime * 0.7f, particleLifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(particleSize * 0.6f, particleSize);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = dustColor;
        main.gravityModifier = 0f;
        main.maxParticles = 300;

        // Zona circular tumbada sobre el suelo (el círculo por defecto está en el plano XY).
        ParticleSystem.ShapeModule shape = dust.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius;
        shape.radiusThickness = 1f;
        shape.rotation = new Vector3(90f, 0f, 0f);

        ParticleSystem.EmissionModule emission = dust.emission;
        emission.rateOverTime = exhaustedRate;
        emission.enabled = false;

        // Aparece rápido y se desvanece despacio.
        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = dust.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = fade;

        // La nube se expande un poco mientras se disipa.
        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = dust.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.5f));

        var particleRenderer = dust.GetComponent<ParticleSystemRenderer>();
        particleRenderer.renderMode = ParticleSystemRenderMode.HorizontalBillboard; // Planas sobre el suelo.
        particleRenderer.sharedMaterial = CreateMaterial();
        particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        particleRenderer.receiveShadows = false;

        dust.Play();
    }

    private Material CreateMaterial()
    {
        // "Sprites/Default" siempre está incluido en las builds y funciona igual en URP y en el pipeline clásico.
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");

        softTexture = CreateSoftCircleTexture(64);
        material = new Material(shader)
        {
            name = "PolvoCansancio (runtime)",
            mainTexture = softTexture
        };
        return material;
    }

    private static Texture2D CreateSoftCircleTexture(int size)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "PolvoSuave (runtime)",
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
                float alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(distance));
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return texture;
    }
}
