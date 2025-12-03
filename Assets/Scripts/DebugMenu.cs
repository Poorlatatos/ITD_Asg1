using UnityEngine;

public class DebugMenu : MonoBehaviour
{
    [Header("Debug Canvas")]
    [Tooltip("Assign the Canvas GameObject for the debug menu. If left null the script will try to find one with tag 'DebugCanvas'.")]
    public GameObject debugCanvas;

    [Header("Keybinds")]
    [Tooltip("Key to toggle the debug canvas.")]
    public KeyCode toggleKey = KeyCode.F1;
    [Tooltip("Close the debug canvas with Escape when open.")]
    public bool closeWithEscape = true;
    [Tooltip("If true the canvas will start hidden.")]
    public bool startHidden = true;

    void Start()
    {
        if (debugCanvas == null)
        {
            // try to auto-find a canvas tagged "DebugCanvas"
            debugCanvas = GameObject.FindWithTag("DebugCanvas");
        }

        if (debugCanvas != null)
            debugCanvas.SetActive(!startHidden);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleDebugCanvas();
        }

        if (closeWithEscape && Input.GetKeyDown(KeyCode.Escape) && debugCanvas != null && debugCanvas.activeSelf)
        {
            debugCanvas.SetActive(false);
        }
    }

    public void ToggleDebugCanvas()
    {
        if (debugCanvas == null)
        {
            debugCanvas = GameObject.FindWithTag("DebugCanvas");
            if (debugCanvas == null) return;
        }

        debugCanvas.SetActive(!debugCanvas.activeSelf);
    }
}