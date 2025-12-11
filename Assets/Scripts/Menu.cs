using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Name : Jaasper Lee
/// Description : Manages the main menu panel functionality.
/// Date : 18 November 2025
/// </summary>

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
