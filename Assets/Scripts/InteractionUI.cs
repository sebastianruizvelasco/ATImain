using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InteractionUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text interactionText;

    private void Start()
    {
        Hide();
    }

    public void Show(InteractionData data)
    {
        if (data == null)
            return;

        if (panel != null)
            panel.SetActive(true);

        if (interactionText != null)
            interactionText.text = data.prompt;

        if (icon != null)
        {
            icon.sprite = data.icon;
            icon.enabled = data.icon != null;
        }
    }

    public void Hide()
    {
        if (panel != null)
            panel.SetActive(false);
    }
}