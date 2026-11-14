using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Asistente de editor para montar un jugador en primera persona en la escena actual:
/// CharacterController + FirstPersonController + PlayerInput y una cámara hija.
/// </summary>
public static class FirstPersonSetup
{
    private const string ActionsAssetPath = "Assets/InputSystem_Actions.inputactions";
    private const string MaterialsFolder = "Assets/Materials";
    private const string GroundMaterialPath = "Assets/Materials/Suelo.mat";
    private const string ObstacleMaterialPath = "Assets/Materials/Obstaculos.mat";

    private const string PlayerName = "Player";
    private const string GroundName = "Suelo";
    private const string ObstaclesName = "Obstaculos de prueba";

    private const float PlayerHeight = 1.8f;
    private const float EyeHeight = 1.65f;

    [MenuItem("Herramientas/Primera Persona/Crear jugador en primera persona", priority = 0)]
    public static void CreateFirstPersonPlayer()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Primera Persona",
                "Sal del modo Play antes de crear el jugador.",
                "Entendido");
            return;
        }

        FirstPersonController existing = Object.FindAnyObjectByType<FirstPersonController>();
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            EditorGUIUtility.PingObject(existing.gameObject);
            EditorUtility.DisplayDialog(
                "Primera Persona",
                "Ya existe un jugador en primera persona en la escena.",
                "Vale");
            return;
        }

        // --- Jugador ---
        var player = new GameObject(PlayerName);
        Undo.RegisterCreatedObjectUndo(player, "Crear jugador en primera persona");
        player.transform.position = new Vector3(0f, 0.05f, 0f);

        var characterController = player.AddComponent<CharacterController>();
        characterController.height = PlayerHeight;
        characterController.radius = 0.3f;
        characterController.center = new Vector3(0f, PlayerHeight * 0.5f, 0f);
        characterController.slopeLimit = 45f;
        characterController.stepOffset = 0.3f;
        characterController.skinWidth = 0.02f;
        characterController.minMoveDistance = 0f;

        try
        {
            player.tag = "Player";
        }
        catch (UnityException)
        {
            // La etiqueta "Player" no existe en el proyecto; no es imprescindible.
        }

        // --- Cámara (se reutiliza la Main Camera si existe) ---
        Camera camera = Camera.main;
        if (camera == null)
            camera = Object.FindAnyObjectByType<Camera>();

        if (camera == null)
        {
            var cameraObject = new GameObject("Main Camera");
            Undo.RegisterCreatedObjectUndo(cameraObject, "Crear cámara");
            cameraObject.tag = "MainCamera";
            camera = cameraObject.AddComponent<Camera>();
        }

        if (Object.FindAnyObjectByType<AudioListener>() == null)
            camera.gameObject.AddComponent<AudioListener>();

        Undo.SetTransformParent(camera.transform, player.transform, "Anclar cámara al jugador");
        camera.transform.localPosition = new Vector3(0f, EyeHeight, 0f);
        camera.transform.localRotation = Quaternion.identity;

        Undo.RecordObject(camera, "Ajustar cámara en primera persona");
        camera.nearClipPlane = 0.05f;
        camera.fieldOfView = 70f;

        // --- Controlador ---
        var controller = player.AddComponent<FirstPersonController>();
        var controllerSerialized = new SerializedObject(controller);
        SerializedProperty cameraProperty = controllerSerialized.FindProperty("cameraTransform");
        if (cameraProperty != null)
        {
            cameraProperty.objectReferenceValue = camera.transform;
            controllerSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // --- PlayerInput con el asset de acciones del proyecto ---
        var playerInput = player.AddComponent<PlayerInput>();
        var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsAssetPath);
        if (actions != null)
        {
            var inputSerialized = new SerializedObject(playerInput);
            inputSerialized.FindProperty("m_Actions").objectReferenceValue = actions;
            inputSerialized.FindProperty("m_DefaultActionMap").stringValue = "Player";
            inputSerialized.FindProperty("m_NotificationBehavior").enumValueIndex =
                (int)PlayerNotifications.SendMessages;
            inputSerialized.ApplyModifiedPropertiesWithoutUndo();
        }
        else
        {
            Debug.LogWarning(
                $"FirstPersonSetup: no se encontró {ActionsAssetPath}. Asigna manualmente el asset de acciones al PlayerInput.",
                playerInput);
        }

        // --- Suelo y obstáculos de prueba ---
        EnsureGround();
        EnsureTestObstacles();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = player;

        if (SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.FrameSelected();

        Debug.Log(
            "Jugador en primera persona creado. Controles: WASD para moverte, ratón para mirar, " +
            "Espacio para saltar, Shift para correr y Esc para liberar el cursor.",
            player);
    }

    private static void EnsureGround()
    {
        GameObject existing = GameObject.Find(GroundName);
        if (existing != null && existing.GetComponent<Collider>() != null)
            return;

        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        Undo.RegisterCreatedObjectUndo(ground, "Crear suelo");
        ground.name = GroundName;
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(5f, 1f, 5f); // Plano de 50 x 50 m.
        ground.GetComponent<MeshRenderer>().sharedMaterial =
            GetOrCreateMaterial(GroundMaterialPath, "Suelo", new Color(0.42f, 0.45f, 0.5f), 0.1f);

        GameObjectUtility.SetStaticEditorFlags(
            ground,
            StaticEditorFlags.BatchingStatic |
            StaticEditorFlags.OccludeeStatic |
            StaticEditorFlags.ReflectionProbeStatic);
    }

    private static void EnsureTestObstacles()
    {
        if (GameObject.Find(ObstaclesName) != null)
            return;

        var parent = new GameObject(ObstaclesName);
        Undo.RegisterCreatedObjectUndo(parent, "Crear obstáculos de prueba");

        Material material = GetOrCreateMaterial(
            ObstacleMaterialPath, "Obstaculos", new Color(0.75f, 0.55f, 0.35f), 0.2f);

        CreateBox(parent.transform, material, "Caja", new Vector3(3f, 0.5f, 5f), new Vector3(3f, 1f, 3f), Vector3.zero);
        CreateBox(parent.transform, material, "Pilar", new Vector3(-4f, 1f, 8f), new Vector3(2f, 2f, 2f), Vector3.zero);
        CreateBox(parent.transform, material, "Plataforma", new Vector3(0f, 0.25f, 11f), new Vector3(6f, 0.5f, 6f), Vector3.zero);
        CreateBox(parent.transform, material, "Escalon 1", new Vector3(4f, 0.15f, 12f), new Vector3(4f, 0.3f, 1.5f), Vector3.zero);
        CreateBox(parent.transform, material, "Escalon 2", new Vector3(4f, 0.3f, 13.5f), new Vector3(4f, 0.6f, 1.5f), Vector3.zero);
    }

    private static void CreateBox(Transform parent, Material material, string name, Vector3 position, Vector3 scale, Vector3 eulerAngles)
    {
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(box, "Crear obstáculo");
        box.name = name;
        box.transform.SetParent(parent, false);
        box.transform.position = position;
        box.transform.localScale = scale;
        box.transform.localEulerAngles = eulerAngles;
        box.GetComponent<MeshRenderer>().sharedMaterial = material;

        GameObjectUtility.SetStaticEditorFlags(
            box,
            StaticEditorFlags.BatchingStatic |
            StaticEditorFlags.OccludeeStatic |
            StaticEditorFlags.ReflectionProbeStatic);
    }

    private static Material GetOrCreateMaterial(string path, string assetName, Color color, float smoothness)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
            return material;

        if (!AssetDatabase.IsValidFolder(MaterialsFolder))
            AssetDatabase.CreateFolder("Assets", "Materials");

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        material = new Material(shader) { name = assetName };

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", smoothness);

        AssetDatabase.CreateAsset(material, path);
        AssetDatabase.SaveAssets();
        return material;
    }
}
