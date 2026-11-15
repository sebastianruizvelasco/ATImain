using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CCTVController : MonoBehaviour
{
    [Header("Interfaz")]
    [SerializeField] private GameObject cctvPanel;
    [SerializeField] private RawImage cameraDisplay;
    [SerializeField] private TMP_Text cameraName;

    [Header("Cámaras")]
    [SerializeField] private RenderTexture[] cameraFeeds;
    [SerializeField] private string[] cameraNames;

    [Header("Controles")]
    [SerializeField] private PlayerControlLock controlLock;

    private int currentCamera;
    private bool isOpen;

    private void Start()
    {
        if (cctvPanel != null)
            cctvPanel.SetActive(false);
    }

    private void Update()
    {
        if (!isOpen)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
            Close();

        if (Input.GetKeyDown(KeyCode.A) ||
            Input.GetKeyDown(KeyCode.LeftArrow))
        {
            PreviousCamera();
        }

        if (Input.GetKeyDown(KeyCode.D) ||
            Input.GetKeyDown(KeyCode.RightArrow))
        {
            NextCamera();
        }
    }

    public void Open()
    {
        if (isOpen || cameraFeeds == null || cameraFeeds.Length == 0)
            return;

        isOpen = true;
        currentCamera = 0;

        if (controlLock != null)
            controlLock.Lock();

        if (cctvPanel != null)
            cctvPanel.SetActive(true);

        UpdateCamera();
    }

    public void Close()
    {
        if (!isOpen)
            return;

        isOpen = false;

        if (cctvPanel != null)
            cctvPanel.SetActive(false);

        if (controlLock != null)
            controlLock.Unlock();
    }

    public void NextCamera()
    {
        if (cameraFeeds == null || cameraFeeds.Length == 0)
            return;

        currentCamera++;

        if (currentCamera >= cameraFeeds.Length)
            currentCamera = 0;

        UpdateCamera();
    }

    public void PreviousCamera()
    {
        if (cameraFeeds == null || cameraFeeds.Length == 0)
            return;

        currentCamera--;

        if (currentCamera < 0)
            currentCamera = cameraFeeds.Length - 1;

        UpdateCamera();
    }

    private void UpdateCamera()
    {
        if (cameraDisplay != null)
            cameraDisplay.texture = cameraFeeds[currentCamera];

        if (cameraName != null)
        {
            if (cameraNames != null &&
                currentCamera < cameraNames.Length)
            {
                cameraName.text = cameraNames[currentCamera];
            }
            else
            {
                cameraName.text = "Cámara " + (currentCamera + 1);
            }
        }
    }
}