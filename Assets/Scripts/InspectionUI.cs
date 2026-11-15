using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InspectionUI : MonoBehaviour
{
    [Header("Interfaz")]
    [SerializeField] private GameObject panel;
    [SerializeField] private RawImage previewImage;
    [SerializeField] private TMP_Text objectName;
    [SerializeField] private TMP_Text description;

    [Header("Modelo 3D")]
    [SerializeField] private Transform previewRoot;
    [SerializeField] private Camera previewCamera;

    [Header("Rotación")]
    [SerializeField] private float rotationSpeed = 5f;

    private InteractionManager manager;
    private PlayerControlLock controlLock;

    private GameObject currentModel;
    private bool isOpen;

    public void Initialize(
        InteractionManager interactionManager,
        PlayerControlLock playerControlLock)
    {
        manager = interactionManager;
        controlLock = playerControlLock;

        if (panel != null)
            panel.SetActive(false);
    }

    public void Show(InteractionData data)
    {
        if (data == null)
            return;

        CloseCurrentModel();

        if (panel != null)
            panel.SetActive(true);

        if (objectName != null)
            objectName.text = data.displayName;

        if (description != null)
            description.text = data.inspectionDescription;

        if (data.inspectionModel != null && previewRoot != null)
        {
            currentModel = Instantiate(
                data.inspectionModel,
                previewRoot
            );

            currentModel.transform.localPosition = Vector3.zero;
            currentModel.transform.localRotation = Quaternion.identity;
            currentModel.transform.localScale = Vector3.one;
        }

        if (previewCamera != null)
            previewCamera.enabled = true;

        if (previewImage != null)
            previewImage.enabled = data.inspectionModel != null;

        isOpen = true;

        if (controlLock != null)
            controlLock.Lock();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        if (!isOpen)
            return;

        if (Input.GetKeyDown(KeyCode.Escape) ||
            Input.GetMouseButtonDown(1))
        {
            Close();
            return;
        }

        if (currentModel != null && Input.GetMouseButton(0))
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            currentModel.transform.Rotate(
                Vector3.up,
                -mouseX * rotationSpeed,
                Space.World
            );

            currentModel.transform.Rotate(
                Vector3.right,
                mouseY * rotationSpeed,
                Space.World
            );
        }
    }

    public void Close()
    {
        if (!isOpen)
            return;

        isOpen = false;

        CloseCurrentModel();

        if (panel != null)
            panel.SetActive(false);

        if (previewCamera != null)
            previewCamera.enabled = false;

        if (controlLock != null)
            controlLock.Unlock();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void CloseCurrentModel()
    {
        if (currentModel != null)
            Destroy(currentModel);

        currentModel = null;
    }
}