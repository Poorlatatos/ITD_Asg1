using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class Menu : MonoBehaviour
{
    public Image panel;
    public Button openPanelButton;
    public bool isPanelOpen = false;

    private void Start()
    {
        openPanelButton.onClick.AddListener(OpenPanel);
    }

    private void OpenPanel()
    {
        panel.gameObject.SetActive(true);
        isPanelOpen = true;
    }
}
