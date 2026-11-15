using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ReadingUI : MonoBehaviour
{
    [Header("Interfaz")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text pageText;

    [Header("Botones")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Button previousButton;

    private PlayerControlLock controlLock;

    private string[] pages;
    private int currentPage;
    private bool isOpen;

    public void Initialize(
        InteractionManager manager,
        PlayerControlLock playerControlLock)
    {
        controlLock = playerControlLock;

        if (panel != null)
            panel.SetActive(false);

        if (nextButton != null)
            nextButton.onClick.AddListener(NextPage);

        if (previousButton != null)
            previousButton.onClick.AddListener(PreviousPage);
    }

    public void Show(InteractionData data)
    {
        if (data == null || data.pages == null ||
            data.pages.Length == 0)
            return;

        pages = data.pages;
        currentPage = 0;

        if (panel != null)
            panel.SetActive(true);

        if (titleText != null)
            titleText.text = data.displayName;

        isOpen = true;

        if (controlLock != null)
            controlLock.Lock();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        UpdatePage();
    }

    private void Update()
    {
        if (!isOpen)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
            Close();

        if (Input.GetKeyDown(KeyCode.RightArrow))
            NextPage();

        if (Input.GetKeyDown(KeyCode.LeftArrow))
            PreviousPage();
    }

    public void NextPage()
    {
        if (!isOpen || pages == null)
            return;

        if (currentPage < pages.Length - 1)
        {
            currentPage++;
            UpdatePage();
        }
    }

    public void PreviousPage()
    {
        if (!isOpen || pages == null)
            return;

        if (currentPage > 0)
        {
            currentPage--;
            UpdatePage();
        }
    }

    private void UpdatePage()
    {
        if (pageText != null)
        {
            pageText.text =
                pages[currentPage] +
                "\n\nPágina " + (currentPage + 1) +
                " / " + pages.Length;
        }

        if (nextButton != null)
            nextButton.interactable = currentPage < pages.Length - 1;

        if (previousButton != null)
            previousButton.interactable = currentPage > 0;
    }

    public void Close()
    {
        if (!isOpen)
            return;

        isOpen = false;

        if (panel != null)
            panel.SetActive(false);

        if (controlLock != null)
            controlLock.Unlock();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}