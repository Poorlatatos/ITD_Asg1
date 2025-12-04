using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Cleaning : MonoBehaviour
{
    [Header("Brush UI")]
    public Sprite brushSprite; // assign a brush image in Inspector
    public Vector2 brushSize = new Vector2(64, 64);

    [Header("Cleaning")]
    public float cleanlinessPerSecond = 30f; // base cleaning rate
    public float minWorldMovement = 0.002f; // world units movement required to count as a rub
    public float movementScale = 0.03f; // scale factor for how movement maps to cleaning

    private Canvas runtimeCanvas;
    private Image brushImage;
    private Camera mainCam;

    // tracking state
    private bool pointerDown = false;
    private Vector3 lastWorldPos;
    private bool lastWorldValid = false;

    // NEW: only active when toggled by UI button
    [Header("Activation")]
    [Tooltip("When true the user may press/touch and rub to clean. Toggle with ToggleCleaning()")]
    public bool cleaningEnabled = false;

    private void Awake()
    {
        mainCam = Camera.main;
        EnsureCanvasAndImage();
        HideBrush();
    }

    private void EnsureCanvasAndImage()
    {
        // find an existing overlay canvas first
        runtimeCanvas = FindFirstObjectByType<Canvas>();
        if (runtimeCanvas == null || runtimeCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            // create a simple overlay canvas
            GameObject cgo = new GameObject("BrushCanvas");
            runtimeCanvas = cgo.AddComponent<Canvas>();
            runtimeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cgo.AddComponent<CanvasScaler>();
            cgo.AddComponent<GraphicRaycaster>();
            DontDestroyOnLoad(cgo);
        }

        // create brush image
        GameObject igo = new GameObject("BrushImage");
        igo.transform.SetParent(runtimeCanvas.transform, false);
        brushImage = igo.AddComponent<Image>();
        brushImage.raycastTarget = false;
        brushImage.rectTransform.sizeDelta = brushSize;
        if (brushSprite != null) brushImage.sprite = brushSprite;
        brushImage.enabled = false;
    }

    private void Update()
    {
        Vector2? screenPos = null;

        if (!cleaningEnabled)
        {
            // ensure nothing is active while cleaning is disabled
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

    private void UpdateBrushPosition(Vector2 screenPos)
    {
        if (brushImage == null) return;
        // position UI image under pointer
        brushImage.rectTransform.position = screenPos;
    }

    private void ShowBrush()
    {
        if (brushImage != null) brushImage.enabled = true;
    }

    private void HideBrush()
    {
        if (brushImage != null) brushImage.enabled = false;
    }

    private void TryCleanAt(Vector2 screenPos)
    {
        if (mainCam == null) return;

        Ray ray = mainCam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            // check if we hit a pet (or child of pet)
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
                    // map movement to a cleaning amount
                    float cleanAmount = cleanlinessPerSecond * (delta / movementScale) * Time.deltaTime;
                    petComp.stats.petCleanliness = Mathf.Clamp(petComp.stats.petCleanliness + cleanAmount, 0f, 100f);

                    // ensure PetTracker knows this runtime pet so UI updates
                    if (PetTracker.Instance != null)
                        PetTracker.Instance.RegisterCurrentPet(petComp);

                    // update lastWorldPos for continuous rubbing
                    lastWorldPos = worldPos;
                }
            }
        }
        else
        {
            // no hit -> invalidate last world pos so next hit starts fresh
            lastWorldValid = false;
        }
    }

    // NEW public toggle methods for UI button
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

    public void StartCleaning()
    {
        cleaningEnabled = true;
        ShowBrush();
    }

    public void StopCleaning()
    {
        cleaningEnabled = false;
        pointerDown = false;
        lastWorldValid = false;
        HideBrush();
    }
}