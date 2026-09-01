using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MobileControlIsland : MonoBehaviour
{
    [Header("Island Container")]
    [SerializeField] private RectTransform islandContainer;
    [SerializeField] private Image islandBackground;

    [Header("Control Buttons")]
    [SerializeField] private Button jumpButton;
    [SerializeField] private Button sprintButton;
    [SerializeField] private Button interactButton;
    [SerializeField] private Button tetherButton;
    [SerializeField] private Button flashlightButton;
    [SerializeField] private Button calibrateButton;
    [SerializeField] private Button pauseButton;

    [Header("UI Text & Indicators")]
    [SerializeField] private TMP_Text interactText;
    [SerializeField] private Image interactGlow;
    [SerializeField] private Image sprintActiveGlow;
    [SerializeField] private Image flashlightActiveGlow;
    [SerializeField] private RectTransform tiltDotTransform;
    [SerializeField] private RectTransform tiltRadarBackground;

    [Header("Color Palette (Glassmorphism Sci-Fi)")]
    [SerializeField] private Color islandBgColor = new Color(0.06f, 0.09f, 0.16f, 0.78f);
    [SerializeField] private Color buttonNormalColor = new Color(0.12f, 0.18f, 0.28f, 0.85f);
    [SerializeField] private Color buttonAccentCyan = new Color(0.0f, 0.85f, 1.0f, 1.0f);
    [SerializeField] private Color buttonAccentGold = new Color(1.0f, 0.78f, 0.15f, 1.0f);
    [SerializeField] private Color buttonAccentGreen = new Color(0.15f, 0.95f, 0.55f, 1.0f);
    [SerializeField] private Color buttonInactiveColor = new Color(0.35f, 0.40f, 0.48f, 0.6f);

    private PlayerInteraction cachedInteraction;
    private FlashLight cachedFlashlight;
    private NetworkTetherSystem cachedTether;
    private PlayerPauseMenu cachedPauseMenu;
    private PlayerController cachedPlayerController;

    private bool isFlashlightOn = false;
    private bool isSprinting = false;

    private void Awake()
    {
        EnsureMobileInputManager();
        EnsureUIBuilt();
    }

    private void Start()
    {
        FindLocalPlayerComponents();
        WireButtonEvents();

        if (MobileInputManager.Instance != null)
        {
            MobileInputManager.Instance.OnCalibrated += PlayCalibrateAnimation;
            MobileInputManager.Instance.OnSprintStateChanged += UpdateSprintVisuals;
        }
    }

    private void OnDestroy()
    {
        if (MobileInputManager.Instance != null)
        {
            MobileInputManager.Instance.OnCalibrated -= PlayCalibrateAnimation;
            MobileInputManager.Instance.OnSprintStateChanged -= UpdateSprintVisuals;
        }
    }

    private void Update()
    {
        UpdateInteractButtonState();
        UpdateTiltVisualizer();
    }

    private void EnsureMobileInputManager()
    {
        if (MobileInputManager.Instance == null)
        {
            GameObject inputGo = new GameObject("MobileInputManager");
            inputGo.AddComponent<MobileInputManager>();
            DontDestroyOnLoad(inputGo);
        }
    }

    public void BindPlayer(PlayerController player)
    {
        if (player == null) return;
        cachedPlayerController = player;
        cachedInteraction = player.GetComponent<PlayerInteraction>();
        cachedFlashlight = player.GetComponent<FlashLight>();
        cachedTether = player.GetComponent<NetworkTetherSystem>();
        cachedPauseMenu = player.GetComponent<PlayerPauseMenu>();
    }

    private void FindLocalPlayerComponents()
    {
        if (cachedPlayerController == null)
        {
            cachedPlayerController = GetComponentInParent<PlayerController>();
        }

        if (cachedPlayerController != null)
        {
            if (cachedInteraction == null) cachedInteraction = cachedPlayerController.GetComponent<PlayerInteraction>();
            if (cachedFlashlight == null) cachedFlashlight = cachedPlayerController.GetComponent<FlashLight>();
            if (cachedTether == null) cachedTether = cachedPlayerController.GetComponent<NetworkTetherSystem>();
            if (cachedPauseMenu == null) cachedPauseMenu = cachedPlayerController.GetComponent<PlayerPauseMenu>();
        }
    }

    #region Button Wiring & Actions

    private void WireButtonEvents()
    {
        if (jumpButton != null)
        {
            jumpButton.onClick.RemoveAllListeners();
            jumpButton.onClick.AddListener(OnJumpClicked);
        }

        if (sprintButton != null)
        {
            sprintButton.onClick.RemoveAllListeners();
            sprintButton.onClick.AddListener(OnSprintClicked);
        }

        if (interactButton != null)
        {
            interactButton.onClick.RemoveAllListeners();
            interactButton.onClick.AddListener(OnInteractClicked);
        }

        if (tetherButton != null)
        {
            tetherButton.onClick.RemoveAllListeners();
            tetherButton.onClick.AddListener(OnTetherClicked);
        }

        if (flashlightButton != null)
        {
            flashlightButton.onClick.RemoveAllListeners();
            flashlightButton.onClick.AddListener(OnFlashlightClicked);
        }

        if (calibrateButton != null)
        {
            calibrateButton.onClick.RemoveAllListeners();
            calibrateButton.onClick.AddListener(OnCalibrateClicked);
        }

        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveAllListeners();
            pauseButton.onClick.AddListener(OnPauseClicked);
        }
    }

    private void OnJumpClicked()
    {
        if (MobileInputManager.Instance != null)
        {
            MobileInputManager.Instance.TriggerJump();
        }
        AnimateButtonPunch(jumpButton != null ? jumpButton.transform : null);
    }

    private void OnSprintClicked()
    {
        if (MobileInputManager.Instance != null)
        {
            MobileInputManager.Instance.ToggleSprint();
        }
        AnimateButtonPunch(sprintButton != null ? sprintButton.transform : null);
    }

    private void OnInteractClicked()
    {
        if (MobileInputManager.Instance != null)
        {
            MobileInputManager.Instance.TriggerInteract();
        }
        AnimateButtonPunch(interactButton != null ? interactButton.transform : null);
    }

    private void OnTetherClicked()
    {
        if (MobileInputManager.Instance != null)
        {
            MobileInputManager.Instance.TriggerTether();
        }
        else if (cachedTether != null)
        {
            cachedTether.TriggerTether();
        }
        AnimateButtonPunch(tetherButton != null ? tetherButton.transform : null);
    }

    private void OnFlashlightClicked()
    {
        if (MobileInputManager.Instance != null)
        {
            MobileInputManager.Instance.TriggerFlashlight();
        }
        else if (cachedFlashlight != null)
        {
            cachedFlashlight.ToggleLight();
        }

        isFlashlightOn = !isFlashlightOn;
        if (flashlightActiveGlow != null)
        {
            flashlightActiveGlow.gameObject.SetActive(isFlashlightOn);
        }
        AnimateButtonPunch(flashlightButton != null ? flashlightButton.transform : null);
    }

    private void OnCalibrateClicked()
    {
        if (MobileInputManager.Instance != null)
        {
            MobileInputManager.Instance.CalibrateNeutralOrientation();
        }
        AnimateButtonPunch(calibrateButton != null ? calibrateButton.transform : null);
    }

    private void OnPauseClicked()
    {
        if (MobileInputManager.Instance != null)
        {
            MobileInputManager.Instance.TriggerPause();
        }
        else if (cachedPauseMenu != null)
        {
            cachedPauseMenu.TogglePause();
        }
        AnimateButtonPunch(pauseButton != null ? pauseButton.transform : null);
    }

    private void UpdateSprintVisuals(bool sprinting)
    {
        isSprinting = sprinting;
        if (sprintActiveGlow != null)
        {
            sprintActiveGlow.gameObject.SetActive(isSprinting);
        }
    }

    private void PlayCalibrateAnimation()
    {
        if (calibrateButton != null)
        {
            StartCoroutine(FlashButtonGlow(calibrateButton));
        }
    }

    private IEnumerator FlashButtonGlow(Button btn)
    {
        if (btn == null) yield break;
        Transform t = btn.transform;
        Vector3 orig = t.localScale;
        t.localScale = orig * 1.25f;
        yield return new WaitForSeconds(0.12f);
        t.localScale = orig;
    }

    private void AnimateButtonPunch(Transform target)
    {
        if (target == null) return;
        StartCoroutine(PunchScaleRoutine(target));
    }

    private IEnumerator PunchScaleRoutine(Transform target)
    {
        if (target == null) yield break;
        Vector3 baseScale = Vector3.one;
        target.localScale = baseScale * 0.9f;
        float elapsed = 0f;
        float duration = 0.15f;

        while (elapsed < duration && target != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            target.localScale = Vector3.Lerp(baseScale * 0.9f, baseScale, t);
            yield return null;
        }

        if (target != null) target.localScale = baseScale;
    }

    #endregion

    #region State & Visual Updates

    private void UpdateInteractButtonState()
    {
        if (cachedInteraction == null)
        {
            FindLocalPlayerComponents();
        }

        bool hasInteractable = cachedInteraction != null && cachedInteraction.HasTargetInteractable;

        if (interactGlow != null)
        {
            if (hasInteractable)
            {
                if (!interactGlow.gameObject.activeSelf) interactGlow.gameObject.SetActive(true);
                float pulse = (Mathf.Sin(Time.time * 6f) + 1f) * 0.5f;
                interactGlow.color = Color.Lerp(buttonAccentGold, buttonAccentCyan, pulse);
            }
            else
            {
                if (interactGlow.gameObject.activeSelf) interactGlow.gameObject.SetActive(false);
            }
        }

        if (interactText != null && cachedInteraction != null)
        {
            string prompt = cachedInteraction.CurrentInteractableText;
            if (!string.IsNullOrEmpty(prompt))
            {
                interactText.text = prompt;
            }
            else
            {
                interactText.text = "INTERACT";
            }
        }
    }

    private void UpdateTiltVisualizer()
    {
        if (tiltDotTransform == null || MobileInputManager.Instance == null) return;

        Vector2 tilt = MobileInputManager.Instance.RawTiltVector;
        float maxRadius = 26f;
        tiltDotTransform.anchoredPosition = new Vector2(tilt.x * maxRadius, tilt.y * maxRadius);
    }

    #endregion

    #region Procedural UI Generator (Glassmorphic Floating Island)

    private void EnsureUIBuilt()
    {
        if (islandContainer != null && jumpButton != null) return;

        // Auto-build procedural sleek UI if not pre-wired in inspector
        BuildProceduralControlIsland();
    }

    private void BuildProceduralControlIsland()
    {
        // 1. Touch Look Zone (Backdrop for full-screen camera drag)
        GameObject lookZoneGo = new GameObject("TouchLookZone", typeof(RectTransform), typeof(Image), typeof(TouchLookZone));
        lookZoneGo.transform.SetParent(transform, false);
        RectTransform lookRt = lookZoneGo.GetComponent<RectTransform>();
        lookRt.anchorMin = Vector2.zero;
        lookRt.anchorMax = Vector2.one;
        lookRt.sizeDelta = Vector2.zero;
        Image lookImg = lookZoneGo.GetComponent<Image>();
        lookImg.color = new Color(0, 0, 0, 0); // Transparent raycast target

        // 2. Island Root Dock (Bottom-Right Anchor)
        GameObject islandGo = new GameObject("IslandDock", typeof(RectTransform), typeof(Image));
        islandGo.transform.SetParent(transform, false);
        islandContainer = islandGo.GetComponent<RectTransform>();
        islandContainer.anchorMin = new Vector2(1f, 0f);
        islandContainer.anchorMax = new Vector2(1f, 0f);
        islandContainer.pivot = new Vector2(1f, 0f);
        islandContainer.anchoredPosition = new Vector2(-28f, 28f);
        islandContainer.sizeDelta = new Vector2(360f, 220f);

        islandBackground = islandGo.GetComponent<Image>();
        islandBackground.color = islandBgColor;

        // Create Header Utility Bar on Island (Pause, Calibrate, Flashlight, Tilt Radar)
        GameObject topBar = new GameObject("TopUtilityBar", typeof(RectTransform));
        topBar.transform.SetParent(islandContainer, false);
        RectTransform topRt = topBar.GetComponent<RectTransform>();
        topRt.anchorMin = new Vector2(0f, 1f);
        topRt.anchorMax = new Vector2(1f, 1f);
        topRt.pivot = new Vector2(0.5f, 1f);
        topRt.anchoredPosition = new Vector2(0f, -8f);
        topRt.sizeDelta = new Vector2(340f, 44f);

        // Pause Button
        pauseButton = CreatePillButton(topBar.transform, "PauseBtn", "MENU", new Vector2(25f, -22f), new Vector2(65f, 32f), buttonNormalColor);

        // Flashlight Button
        flashlightButton = CreatePillButton(topBar.transform, "FlashlightBtn", "LIGHT", new Vector2(95f, -22f), new Vector2(65f, 32f), buttonNormalColor);
        flashlightActiveGlow = CreateActiveGlow(flashlightButton.transform, buttonAccentGold);
        flashlightActiveGlow.gameObject.SetActive(false);

        // Calibrate Button
        calibrateButton = CreatePillButton(topBar.transform, "CalibrateBtn", "ZERO TILT", new Vector2(175f, -22f), new Vector2(85f, 32f), buttonNormalColor);

        // Tilt Radar / Gauge
        GameObject radarGo = new GameObject("TiltRadar", typeof(RectTransform), typeof(Image));
        radarGo.transform.SetParent(topBar.transform, false);
        tiltRadarBackground = radarGo.GetComponent<RectTransform>();
        tiltRadarBackground.anchoredPosition = new Vector2(285f, -22f);
        tiltRadarBackground.sizeDelta = new Vector2(36f, 36f);
        Image radarImg = radarGo.GetComponent<Image>();
        radarImg.color = new Color(0.04f, 0.06f, 0.1f, 0.9f);

        GameObject dotGo = new GameObject("TiltDot", typeof(RectTransform), typeof(Image));
        dotGo.transform.SetParent(radarGo.transform, false);
        tiltDotTransform = dotGo.GetComponent<RectTransform>();
        tiltDotTransform.anchoredPosition = Vector2.zero;
        tiltDotTransform.sizeDelta = new Vector2(10f, 10f);
        Image dotImg = dotGo.GetComponent<Image>();
        dotImg.color = buttonAccentCyan;

        // Main Action Cluster (Jump, Sprint, Interact, Tether)
        GameObject actionsCluster = new GameObject("ActionButtonsCluster", typeof(RectTransform));
        actionsCluster.transform.SetParent(islandContainer, false);
        RectTransform actRt = actionsCluster.GetComponent<RectTransform>();
        actRt.anchorMin = new Vector2(0f, 0f);
        actRt.anchorMax = new Vector2(1f, 0f);
        actRt.pivot = new Vector2(0.5f, 0f);
        actRt.anchoredPosition = new Vector2(0f, 12f);
        actRt.sizeDelta = new Vector2(340f, 140f);

        // Sprint Button (Left)
        sprintButton = CreateCircleButton(actionsCluster.transform, "SprintBtn", "SPRINT", new Vector2(40f, 70f), 65f, buttonNormalColor);
        sprintActiveGlow = CreateActiveGlow(sprintButton.transform, buttonAccentGreen);
        sprintActiveGlow.gameObject.SetActive(false);

        // Tether Button (Middle Left)
        tetherButton = CreateCircleButton(actionsCluster.transform, "TetherBtn", "TETHER", new Vector2(115f, 70f), 65f, buttonNormalColor);

        // Interact Button (Middle Right)
        interactButton = CreateCircleButton(actionsCluster.transform, "InteractBtn", "INTERACT", new Vector2(195f, 70f), 75f, buttonNormalColor);
        interactGlow = CreateActiveGlow(interactButton.transform, buttonAccentGold);
        interactGlow.gameObject.SetActive(false);
        interactText = interactButton.GetComponentInChildren<TMP_Text>();

        // Jump Button (Right - Prominent Primary Action)
        jumpButton = CreateCircleButton(actionsCluster.transform, "JumpBtn", "JUMP", new Vector2(285f, 70f), 85f, new Color(0.0f, 0.55f, 0.85f, 0.95f));
    }

    private Button CreateCircleButton(Transform parent, string name, string label, Vector2 pos, float size, Color bgColor)
    {
        GameObject btnGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(parent, false);

        RectTransform rt = btnGo.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(size, size);

        Image img = btnGo.GetComponent<Image>();
        img.color = bgColor;

        Button btn = btnGo.GetComponent<Button>();
        btn.targetGraphic = img;

        // Label
        GameObject txtGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGo.transform.SetParent(btnGo.transform, false);
        RectTransform txtRt = txtGo.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;

        TMP_Text tmp = txtGo.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = size > 70f ? 14f : 11f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;

        return btn;
    }

    private Button CreatePillButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color bgColor)
    {
        GameObject btnGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(parent, false);

        RectTransform rt = btnGo.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = btnGo.GetComponent<Image>();
        img.color = bgColor;

        Button btn = btnGo.GetComponent<Button>();
        btn.targetGraphic = img;

        GameObject txtGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGo.transform.SetParent(btnGo.transform, false);
        RectTransform txtRt = txtGo.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;

        TMP_Text tmp = txtGo.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 11f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;

        return btn;
    }

    private Image CreateActiveGlow(Transform parent, Color glowColor)
    {
        GameObject glowGo = new GameObject("ActiveGlow", typeof(RectTransform), typeof(Image));
        glowGo.transform.SetParent(parent, false);
        RectTransform rt = glowGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = new Vector2(8f, 8f);

        Image img = glowGo.GetComponent<Image>();
        img.color = glowColor;
        img.raycastTarget = false;
        glowGo.transform.SetAsFirstSibling();
        return img;
    }

    #endregion
}
