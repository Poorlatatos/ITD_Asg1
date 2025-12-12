using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


/// Name : Jaasper Lee
/// Description : Script to allow user to clean their pet by rubbing on the screen.
/// Date : 17 November 2025


public class Cleaning : MonoBehaviour
{
    [Header("Brush UI")]
    public Sprite brushSprite;
    public Vector2 brushSize = new Vector2(64, 64);

    [Header("Cleaning")]
    public float cleanlinessPerSecond = 30f;
    public float minWorldMovement = 0.002f;
    public float movementScale = 0.03f;

    private Canvas runtimeCanvas;
    private Image brushImage;
    private Camera mainCam;

    private bool pointerDown = false;
    private Vector3 lastWorldPos;
    private bool lastWorldValid = false;

    [Header("Activation")]
    [Tooltip("When true the user may press/touch and rub to clean. Toggle with ToggleCleaning()")]
    public bool cleaningEnabled = false;

    private void Awake()
    {
        mainCam = Camera.main;
        EnsureCanvasAndImage();
        HideBrush();
    }

    /// Initializes the main camera, ensures the UI canvas and brush image exist, and hides the brush.
    private void EnsureCanvasAndImage()
    {
        
        runtimeCanvas = FindFirstObjectByType<Canvas>();
        if (runtimeCanvas == null || runtimeCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            GameObject cgo = new GameObject("BrushCanvas");
            runtimeCanvas = cgo.AddComponent<Canvas>();
            runtimeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cgo.AddComponent<CanvasScaler>();
            cgo.AddComponent<GraphicRaycaster>();
            DontDestroyOnLoad(cgo);
        }
        
        /// Creates the brush UI image as a child of the runtime canvas and configures it for pointer feedback.
        GameObject igo = new GameObject("BrushImage");
        igo.transform.SetParent(runtimeCanvas.transform, false);
        brushImage = igo.AddComponent<Image>();
        brushImage.raycastTarget = false;
        brushImage.rectTransform.sizeDelta = brushSize;
        if (brushSprite != null) brushImage.sprite = brushSprite;
        brushImage.enabled = false;
    }

    /// Handles input each frame; shows/hides the brush and triggers cleaning when enabled.
    private void Update()
    {
        Vector2? screenPos = null;

        if (!cleaningEnabled)
        {
            if (pointerDown) pointerDown = false;
            HideBrush();
            lastWorldValid = false;
            return;
        }

        #if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
        if (Input.GetMouseButtonDown(0)) { pointerDown = true; screenPos = Input.mousePosition; lastWorldValid = false; ShowBrush(); }
        else if (Input.GetMouseButtonUp(0)) { pointerDown = false; HideBrush(); lastWorldValid = false; }
        else if (Input.GetMouseButton(0)) screenPos = Input.mousePosition;
        #else
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began) { pointerDown = true; lastWorldValid = false; ShowBrush(); }
            if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) { pointerDown = false; HideBrush(); lastWorldValid = false; }
            if (t.phase == TouchPhase.Began || t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary) screenPos = t.position;
        }
        #endif

        if (screenPos.HasValue)
        {
            UpdateBrushPosition(screenPos.Value);
            if (pointerDown) TryCleanAt(screenPos.Value);
        }
    }

    /// Positions the on-screen brush image at the given screen coordinates.
    private void UpdateBrushPosition(Vector2 screenPos)
    {
        if (brushImage == null) return;
        brushImage.rectTransform.position = screenPos;
    }

    /// Enables the brush UI image.
    private void ShowBrush()
    {
        if (brushImage != null) brushImage.enabled = true;
    }

    /// Disables the brush UI image.
    private void HideBrush()
    {
        if (brushImage != null) brushImage.enabled = false;
    }

    
    /// Performs a raycast at the given screen position and applies cleanliness to any hit pet based on pointer movement.
    private void TryCleanAt(Vector2 screenPos)
    {
        if (mainCam == null) return;

        Ray ray = mainCam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            var petComp = hit.collider.GetComponentInParent<PetStatsComponent>();
            if (petComp != null && petComp.stats != null)
            {
                Vector3 worldPos = hit.point;

                if (!lastWorldValid)
                {
                    lastWorldPos = worldPos;
                    lastWorldValid = true;
                    return;
                }

                float delta = Vector3.Distance(worldPos, lastWorldPos);
                if (delta >= minWorldMovement)
                {
                    float cleanAmount = cleanlinessPerSecond * (delta / movementScale) * Time.deltaTime;
                    petComp.stats.petCleanliness = Mathf.Clamp(petComp.stats.petCleanliness + cleanAmount, 0f, 100f);

                    if (PetTracker.Instance != null)
                        PetTracker.Instance.RegisterCurrentPet(petComp);

                    lastWorldPos = worldPos;
                }
            }
        }
        else
        {
            lastWorldValid = false;
        }
    }

    /// Toggles the cleaning interaction and updates brush/pointer state accordingly.
    public void ToggleCleaning()
    {
        cleaningEnabled = !cleaningEnabled;

        if (cleaningEnabled)
        {   
            ShowBrush();
        }
        else
        {
            pointerDown = false;
            lastWorldValid = false;
            HideBrush();
        }
    }

    

    /// Enables cleaning mode and shows the brush.
    public void StartCleaning()
    {
        cleaningEnabled = true;
        ShowBrush();
    }

    
    /// Disables cleaning mode, resets pointer state, and hides the brush.
    public void StopCleaning()
    {
        cleaningEnabled = false;
        pointerDown = false;
        lastWorldValid = false;
        HideBrush();
    }
}