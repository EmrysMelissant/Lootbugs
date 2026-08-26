using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MobileHUD : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public static MobileHUD Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private bool autoCreateUIIfMissing = true;
    [SerializeField] private bool showTiltVisualizer = true;
    [SerializeField] private bool enableTouchSwipeLook = true;

    [Header("UI References (Optional - Auto-generated if null)")]
    [SerializeField] private Canvas hudCanvas;
    [SerializeField] private RectTransform safeAreaContainer;
    [SerializeField] private Button jumpButton;
    [SerializeField] private Button sprintButton;
    [SerializeField] private Button crouchButton;
    [SerializeField] private Button tetherButton;
    [SerializeField] private Button interactButton;
    [SerializeField] private Button calibrateButton;
    [SerializeField] private Button pauseButton;

    [Header("Visualizer Elements")]
    [SerializeField] private RectTransform tiltBall;
    [SerializeField] private RectTransform tiltBaseRing;
    [SerializeField] private Image sprintButtonImage;
    [SerializeField] private Image crouchButtonImage;

    // Runtime state
    private bool isInitialized;
    private Vector2 lastTouchPos;
    private int lookTouchId = -1;
    private bool isSprinting;
    private bool isCrouching;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        EnsureMobileMotionManager();
        if (autoCreateUIIfMissing && (hudCanvas == null || safeAreaContainer == null))
        {
            BuildMobileUI();
        }

        ApplySafeArea();
        isInitialized = true;
    }

    private void EnsureMobileMotionManager()
    {
        if (MobileMotionManager.Instance == null)
        {
            GameObject motionObj = new GameObject("MobileMotionManager", typeof(MobileMotionManager));
            DontDestroyOnLoad(motionObj);
        }
    }

    private void Update()
    {
        if (!isInitialized) return;

        UpdateTiltVisualizer();
    }

    public void SetVisible(bool visible)
    {
        if (hudCanvas != null)
        {
            hudCanvas.gameObject.SetActive(visible);
        }
    }

    private void ApplySafeArea()
    {
        if (safeAreaContainer == null) return;

        Rect safeArea = Screen.safeArea;
        Vector2 minAnchor = safeArea.position;
        Vector2 maxAnchor = minAnchor + safeArea.size;

        minAnchor.x /= Screen.width;
        minAnchor.y /= Screen.height;
        maxAnchor.x /= Screen.width;
        maxAnchor.y /= Screen.height;

        safeAreaContainer.anchorMin = minAnchor;
        safeAreaContainer.anchorMax = maxAnchor;
    }

    private void UpdateTiltVisualizer()
    {
        if (tiltBall == null || MobileMotionManager.Instance == null || !showTiltVisualizer) return;

        Vector2 tiltOffset = MobileMotionManager.Instance.CurrentTiltOffset;
        float maxRadius = 45f; // Visual radius inside tilt ring
        tiltBall.anchoredPosition = new Vector2(tiltOffset.x * maxRadius, tiltOffset.y * maxRadius);
    }

    // ==========================================
    // Touch Drag for Right-Side Look Assist
    // ==========================================

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!enableTouchSwipeLook) return;

        // Only register touches on the right half of the screen for camera look
        if (eventData.position.x > Screen.width * 0.45f)
        {
            lookTouchId = eventData.pointerId;
            lastTouchPos = eventData.position;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!enableTouchSwipeLook || eventData.pointerId != lookTouchId) return;

        Vector2 delta = eventData.position - lastTouchPos;
        lastTouchPos = eventData.position;

        if (MobileMotionManager.Instance != null)
        {
            // Normalize delta relative to screen DPI / resolution for uniform feel
            float scale = 60f / Mathf.Max(Screen.dpi, 160f);
            MobileMotionManager.Instance.AddTouchSwipeLookDelta(new Vector2(delta.x * scale, -delta.y * scale));
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId == lookTouchId)
        {
            lookTouchId = -1;
        }
    }

    // ==========================================
    // Button Event Handlers
    // ==========================================

    public void OnJumpClicked()
    {
        if (MobileMotionManager.Instance != null)
        {
            MobileMotionManager.Instance.OnTouchJumpPressed();
        }
    }

    public void OnSprintClicked()
    {
        isSprinting = !isSprinting;
        if (MobileMotionManager.Instance != null)
        {
            MobileMotionManager.Instance.SetTouchSprint(isSprinting);
        }

        if (sprintButtonImage != null)
        {
            sprintButtonImage.color = isSprinting ? new Color(1f, 0.75f, 0.1f, 0.95f) : new Color(0.15f, 0.22f, 0.32f, 0.85f);
        }
    }

    public void OnCrouchClicked()
    {
        isCrouching = !isCrouching;
        if (MobileMotionManager.Instance != null)
        {
            MobileMotionManager.Instance.SetTouchCrouch(isCrouching);
        }

        if (crouchButtonImage != null)
        {
            crouchButtonImage.color = isCrouching ? new Color(0.3f, 0.85f, 1f, 0.95f) : new Color(0.15f, 0.22f, 0.32f, 0.85f);
        }
    }

    public void OnTetherClicked()
    {
        if (MobileMotionManager.Instance != null)
        {
            MobileMotionManager.Instance.OnTouchTetherPressed();
        }
    }

    public void OnInteractClicked()
    {
        if (MobileMotionManager.Instance != null)
        {
            MobileMotionManager.Instance.OnTouchInteractPressed();
        }
    }

    public void OnCalibrateClicked()
    {
        if (MobileMotionManager.Instance != null)
        {
            MobileMotionManager.Instance.CalibrateNeutralPosture();
        }
    }

    public void OnPauseClicked()
    {
        if (MobileMotionManager.Instance != null)
        {
            MobileMotionManager.Instance.OnTouchPausePressed();
        }

        if (StartupMenuController.Instance != null)
        {
            StartupMenuController.Instance.PauseGame();
        }
    }

    // ==========================================
    // Procedural Cyberpunk Mobile UI Generator
    // ==========================================

    private void BuildMobileUI()
    {
        // 1. Root Canvas
        GameObject canvasObj = new GameObject("MobileHUD_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObj.transform.SetParent(transform, false);

        hudCanvas = canvasObj.GetComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        hudCanvas.sortingOrder = 90;

        var scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // 2. Safe Area Container
        GameObject safeAreaObj = new GameObject("SafeAreaContainer", typeof(RectTransform));
        safeAreaObj.transform.SetParent(canvasObj.transform, false);
        safeAreaContainer = safeAreaObj.GetComponent<RectTransform>();
        safeAreaContainer.anchorMin = Vector2.zero;
        safeAreaContainer.anchorMax = Vector2.one;
        safeAreaContainer.offsetMin = Vector2.zero;
        safeAreaContainer.offsetMax = Vector2.zero;

        // 3. Right-Side Touch Look Area (Transparent touch catcher)
        GameObject lookAreaObj = new GameObject("TouchLookArea", typeof(RectTransform), typeof(Image));
        lookAreaObj.transform.SetParent(safeAreaContainer, false);
        var lookAreaRect = lookAreaObj.GetComponent<RectTransform>();
        lookAreaRect.anchorMin = new Vector2(0.4f, 0f);
        lookAreaRect.anchorMax = new Vector2(1f, 0.9f);
        lookAreaRect.offsetMin = Vector2.zero;
        lookAreaRect.offsetMax = Vector2.zero;
        var lookAreaImg = lookAreaObj.GetComponent<Image>();
        lookAreaImg.color = Color.clear;
        lookAreaImg.raycastTarget = true;

        // 4. Action Buttons (Right Side)
        // Jump Button (Bottom Right Large Button)
        jumpButton = CreateTouchButton(safeAreaContainer, "JumpButton", "🦘 JUMP", new Vector2(1f, 0f), new Vector2(-120, 120), new Vector2(130, 130), new Color(0.1f, 0.6f, 0.85f, 0.85f), Color.white, 20);
        jumpButton.onClick.AddListener(OnJumpClicked);

        // Tether Button (Grapple / E)
        tetherButton = CreateTouchButton(safeAreaContainer, "TetherButton", "🧲 TETHER", new Vector2(1f, 0f), new Vector2(-260, 95), new Vector2(105, 105), new Color(0.65f, 0.15f, 0.85f, 0.85f), Color.white, 16);
        tetherButton.onClick.AddListener(OnTetherClicked);

        // Interact Button (Use / F)
        interactButton = CreateTouchButton(safeAreaContainer, "InteractButton", "✋ USE", new Vector2(1f, 0f), new Vector2(-120, 260), new Vector2(105, 105), new Color(0.15f, 0.75f, 0.45f, 0.85f), Color.white, 16);
        interactButton.onClick.AddListener(OnInteractClicked);

        // Sprint Button (Toggle Sprint)
        sprintButton = CreateTouchButton(safeAreaContainer, "SprintButton", "⚡ SPRINT", new Vector2(1f, 0f), new Vector2(-250, 220), new Vector2(95, 95), new Color(0.15f, 0.22f, 0.32f, 0.85f), new Color(1f, 0.85f, 0.3f, 1f), 15);
        sprintButton.onClick.AddListener(OnSprintClicked);
        sprintButtonImage = sprintButton.GetComponent<Image>();

        // Crouch Button (Toggle Crouch)
        crouchButton = CreateTouchButton(safeAreaContainer, "CrouchButton", "🛡️ CROUCH", new Vector2(1f, 0f), new Vector2(-360, 95), new Vector2(90, 90), new Color(0.15f, 0.22f, 0.32f, 0.85f), new Color(0.4f, 0.85f, 1f, 1f), 14);
        crouchButton.onClick.AddListener(OnCrouchClicked);
        crouchButtonImage = crouchButton.GetComponent<Image>();

        // 5. Top Bar Buttons
        // Recalibrate Gyro Button (Top Center / Left)
        calibrateButton = CreateTouchButton(safeAreaContainer, "CalibrateButton", "🎯 RE-CENTER GYRO", new Vector2(0.5f, 1f), new Vector2(0, -50), new Vector2(240, 56), new Color(0.12f, 0.35f, 0.55f, 0.9f), new Color(0.4f, 0.95f, 1f, 1f), 15);
        calibrateButton.onClick.AddListener(OnCalibrateClicked);

        // Pause Menu Button (Top Right)
        pauseButton = CreateTouchButton(safeAreaContainer, "PauseButton", "⏸️ MENU", new Vector2(1f, 1f), new Vector2(-70, -50), new Vector2(110, 56), new Color(0.85f, 0.25f, 0.25f, 0.9f), Color.white, 15);
        pauseButton.onClick.AddListener(OnPauseClicked);

        // 6. Tilt Locomotion Visualizer (Lower Left)
        if (showTiltVisualizer)
        {
            BuildTiltVisualizer(safeAreaContainer);
        }
    }

    private void BuildTiltVisualizer(Transform parent)
    {
        // Visualizer Base Container
        GameObject visContainer = new GameObject("TiltVisualizer", typeof(RectTransform));
        visContainer.transform.SetParent(parent, false);
        var visRect = visContainer.GetComponent<RectTransform>();
        visRect.anchorMin = new Vector2(0f, 0f);
        visRect.anchorMax = new Vector2(0f, 0f);
        visRect.anchoredPosition = new Vector2(140, 140);
        visRect.sizeDelta = new Vector2(150, 150);

        // Outer Ring
        GameObject ringObj = new GameObject("OuterRing", typeof(RectTransform), typeof(Image));
        ringObj.transform.SetParent(visContainer.transform, false);
        tiltBaseRing = ringObj.GetComponent<RectTransform>();
        tiltBaseRing.anchorMin = new Vector2(0.5f, 0.5f);
        tiltBaseRing.anchorMax = new Vector2(0.5f, 0.5f);
        tiltBaseRing.sizeDelta = new Vector2(130, 130);
        var ringImg = ringObj.GetComponent<Image>();
        ringImg.color = new Color(0.08f, 0.18f, 0.28f, 0.7f);
        ringImg.raycastTarget = false;

        // Deadzone Ring
        GameObject deadzoneObj = new GameObject("DeadzoneRing", typeof(RectTransform), typeof(Image));
        deadzoneObj.transform.SetParent(visContainer.transform, false);
        var deadzoneRect = deadzoneObj.GetComponent<RectTransform>();
        deadzoneRect.anchorMin = new Vector2(0.5f, 0.5f);
        deadzoneRect.anchorMax = new Vector2(0.5f, 0.5f);
        deadzoneRect.sizeDelta = new Vector2(35, 35);
        var deadzoneImg = deadzoneObj.GetComponent<Image>();
        deadzoneImg.color = new Color(0.2f, 0.45f, 0.65f, 0.4f);
        deadzoneImg.raycastTarget = false;

        // Tilt Motion Ball
        GameObject ballObj = new GameObject("TiltBall", typeof(RectTransform), typeof(Image));
        ballObj.transform.SetParent(visContainer.transform, false);
        tiltBall = ballObj.GetComponent<RectTransform>();
        tiltBall.anchorMin = new Vector2(0.5f, 0.5f);
        tiltBall.anchorMax = new Vector2(0.5f, 0.5f);
        tiltBall.sizeDelta = new Vector2(26, 26);
        var ballImg = ballObj.GetComponent<Image>();
        ballImg.color = new Color(0.2f, 0.95f, 0.85f, 0.95f);
        ballImg.raycastTarget = false;

        // Label: PHONE TILT
        GameObject labelObj = new GameObject("TiltLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObj.transform.SetParent(visContainer.transform, false);
        var labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 0f);
        labelRect.anchorMax = new Vector2(0.5f, 0f);
        labelRect.anchoredPosition = new Vector2(0, -22);
        labelRect.sizeDelta = new Vector2(160, 24);
        var labelTmp = labelObj.GetComponent<TextMeshProUGUI>();
        labelTmp.text = "PHONE TILT MOVE";
        labelTmp.fontSize = 11;
        labelTmp.fontStyle = FontStyles.Bold;
        labelTmp.alignment = TextAlignmentOptions.Center;
        labelTmp.color = new Color(0.5f, 0.85f, 1f, 0.85f);
        labelTmp.raycastTarget = false;
    }

    private Button CreateTouchButton(Transform parent, string name, string label, Vector2 anchor, Vector2 anchoredPos, Vector2 size, Color bgColor, Color textColor, float fontSize)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);

        var rect = btnObj.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        var img = btnObj.GetComponent<Image>();
        img.color = bgColor;
        img.raycastTarget = true;

        var btn = btnObj.GetComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = bgColor;
        colors.highlightedColor = new Color(Mathf.Min(bgColor.r * 1.3f, 1f), Mathf.Min(bgColor.g * 1.3f, 1f), Mathf.Min(bgColor.b * 1.3f, 1f), 1f);
        colors.pressedColor = new Color(bgColor.r * 0.7f, bgColor.g * 0.7f, bgColor.b * 0.7f, 1f);
        btn.colors = colors;

        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(btnObj.transform, false);
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(4, 4);
        textRect.offsetMax = new Vector2(-4, -4);

        var tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = textColor;
        tmp.raycastTarget = false;

        return btn;
    }
}
