
using UnityEngine;
using UnityEngine.Rendering;

[AddComponentMenu("Primera Persona/Stamina Blood Effect")]
[RequireComponent(typeof(PlayerStamina))]
[RequireComponent(typeof(CharacterController))]
public class StaminaBloodEffect : MonoBehaviour
{
    [Header("Aspecto")]
    [SerializeField]
    private Color darkBlood =
        new Color(0.45f, 0f, 0.015f, 1f);

    [SerializeField]
    private Color lightBlood =
        new Color(0.85f, 0.025f, 0.035f, 1f);

    [SerializeField, Min(0.01f)] private float minSize = 0.12f;
    [SerializeField, Min(0.01f)] private float maxSize = 0.35f;

    [SerializeField, Min(1f)] private float lifetime = 30f;
    [SerializeField, Range(0.05f, 1f)] private float fadeFraction = 0.2f;

    [Header("Emision")]
    [SerializeField, Min(0)] private int burstCount = 3;

    [Tooltip("Particulas emitidas por segundo mientras se mueve agotado.")]
    [SerializeField, Min(0f)] private float exhaustedRate = 10f;

    [SerializeField, Min(0.05f)] private float radius = 0.35f;

    [Header("Posicion")]
    [SerializeField] private float heightOffset = 0.03f;

    [Header("Rendimiento")]
    [SerializeField, Min(1)] private int maxStains = 400;

    private PlayerStamina stamina;
    private CharacterController controller;

    private ParticleSystem blood;
    private Transform particleTransform;
    private GameObject particleObject;

    private Material bloodMaterial;
    private Texture2D stainTexture;

    private void Awake()
    {
        stamina = GetComponent<PlayerStamina>();
        controller = GetComponent<CharacterController>();

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

        SetContinuousEmission(false);
    }

    private void Update()
    {
        if (blood == null)
            return;

        // Solo deja sangre cuando está agotado y se mueve.
        Vector3 velocity = controller.velocity;
        velocity.y = 0f;

        bool shouldEmit =
            stamina.IsExhausted &&
            velocity.sqrMagnitude > 0.04f;

        SetContinuousEmission(shouldEmit);
    }

    private void LateUpdate()
    {
        if (particleTransform == null)
            return;

        // Posición de los pies del jugador.
        Vector3 position = new Vector3(
            transform.position.x,
            controller.bounds.min.y + heightOffset,
            transform.position.z
        );

        // El emisor sigue al jugador, pero las manchas no.
        particleTransform.position = position;

        // El suelo utiliza el eje XZ.
        particleTransform.rotation = Quaternion.identity;
    }

    private void OnDestroy()
    {
        if (bloodMaterial != null)
            Destroy(bloodMaterial);

        if (stainTexture != null)
            Destroy(stainTexture);

        if (particleObject != null)
            Destroy(particleObject);
    }

    private void OnExhausted()
    {
        if (blood == null)
            return;

        // Pequeña ráfaga inicial.
        blood.Emit(burstCount);
    }

    private void OnRecovered()
    {
        SetContinuousEmission(false);
    }

    private void SetContinuousEmission(bool enabled)
    {
        if (blood == null)
            return;

        var emission = blood.emission;
        emission.enabled = enabled;
    }

    private void BuildParticleSystem()
    {
        particleObject = new GameObject("Sangre de cansancio");

        particleTransform = particleObject.transform;

        blood = particleObject.AddComponent<ParticleSystem>();

        blood.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // MAIN
        var main = blood.main;

        main.playOnAwake = false;
        main.loop = true;
        main.duration = 1f;

        // Las partículas permanecen en el mundo.
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        main.startLifetime = lifetime;
        main.startSpeed = 0f;
        main.gravityModifier = 0f;

        main.startSize =
            new ParticleSystem.MinMaxCurve(minSize, maxSize);

        main.startRotation =
            new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

        main.startColor =
            new ParticleSystem.MinMaxGradient(darkBlood, lightBlood);

        main.maxParticles = maxStains;

        // SHAPE
        var shape = blood.shape;

        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius;
        shape.radiusThickness = 1f;

        // Círculo acostado en el plano XZ.
        shape.rotation = new Vector3(90f, 0f, 0f);

        // EMISSION
        var emission = blood.emission;

        emission.rateOverTime = exhaustedRate;
        emission.enabled = false;

        // COLOR OVER LIFETIME
        var color = blood.colorOverLifetime;

        color.enabled = true;

        Gradient fade = new Gradient();

        fade.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f - fadeFraction),
                new GradientAlphaKey(0f, 1f)
            }
        );

        color.color = fade;

        // RENDERER
        var renderer = blood.GetComponent<ParticleSystemRenderer>();

        renderer.renderMode =
            ParticleSystemRenderMode.HorizontalBillboard;

        renderer.alignment =
            ParticleSystemRenderSpace.World;

        renderer.sharedMaterial = CreateMaterial();

        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        blood.Play();
    }

    private Material CreateMaterial()
    {
        Shader shader = Shader.Find(
            "Universal Render Pipeline/Particles/Unlit"
        );

        if (shader == null)
        {
            Debug.LogError(
                "No se encontró el shader URP Particles Unlit."
            );

            return null;
        }

        stainTexture = CreateStainTexture(128);

        bloodMaterial = new Material(shader);

        bloodMaterial.name = "SangreCansancio URP";
        bloodMaterial.mainTexture = stainTexture;

        if (bloodMaterial.HasProperty("_BaseMap"))
            bloodMaterial.SetTexture("_BaseMap", stainTexture);

        if (bloodMaterial.HasProperty("_BaseColor"))
            bloodMaterial.SetColor("_BaseColor", Color.white);

        return bloodMaterial;
    }

    private static Texture2D CreateStainTexture(int size)
    {
        Texture2D texture = new Texture2D(
            size,
            size,
            TextureFormat.RGBA32,
            false
        );

        texture.name = "ManchaSangre";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        float center = (size - 1) * 0.5f;

        Color32[] pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;

                float distance = Mathf.Sqrt(dx * dx + dy * dy);

                float alpha = 1f - Mathf.SmoothStep(
                    0.88f,
                    1f,
                    Mathf.Clamp01(distance)
                );

                pixels[y * size + x] = new Color32(
                    255,
                    255,
                    255,
                    (byte)(alpha * 255f)
                );
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);

        return texture;
    }
}