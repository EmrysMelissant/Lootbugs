using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class StartupMenuController : MonoBehaviour
{
    public static StartupMenuController Instance { get; private set; }

    private const string PrefsKeyLastIp = "Lootbugs_LastConnectedIP";
    private const string PrefsKeyLastPort = "Lootbugs_LastConnectedPort";

    [Header("Panels")]
    [SerializeField] private GameObject menuCanvas;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject joinGamePanel;
    [SerializeField] private GameObject highScoresPanel;
    [SerializeField] private GameObject controlsPanel;
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject inGameHudPanel;

    [Header("Main Menu Elements")]
    [SerializeField] private TMP_Text topHighScoreBadgeText;
    [SerializeField] private TMP_Text hostIpBadgeText;
    [SerializeField] private Button startHostButton;
    [SerializeField] private Button joinClientButton;
    [SerializeField] private Button highScoresButton;
    [SerializeField] private Button controlsButton;
    [SerializeField] private Button quitButton;

    [Header("Join Game Panel Elements")]
    [SerializeField] private TMP_InputField ipInputField;
    [SerializeField] private TMP_InputField portInputField;
    [SerializeField] private TMP_Text connectionStatusText;
    [SerializeField] private Button connectButton;
    [SerializeField] private Button quickLocalhostButton;
    [SerializeField] private Button cancelConnectButton;
    [SerializeField] private Button backFromJoinButton;

    [Header("High Scores Panel Elements")]
    [SerializeField] private Transform scoreListContainer;
    [SerializeField] private GameObject scoreRowPrefab;
    [SerializeField] private TMP_Text emptyScoresText;
    [SerializeField] private Button clearScoresButton;
    [SerializeField] private Button backFromScoresButton;

    [Header("Controls Panel Elements")]
    [SerializeField] private Button backFromControlsButton;

    [Header("Pause Menu Elements")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button pauseHighScoresButton;
    [SerializeField] private Button disconnectButton;

    [Header("In-Game HUD Elements")]
    [SerializeField] private TMP_Text currentScoreHudText;
    [SerializeField] private TMP_Text highScoreHudText;

    [Header("Scene Elements")]
    [SerializeField] private Camera menuCamera;

    private bool isInGame = false;
    private bool isPaused = false;
    private bool isConnecting = false;
    private int currentSessionScore = 0;
    private Coroutine connectionTimeoutCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Wire Main Menu Button Listeners
        if (startHostButton != null) startHostButton.onClick.AddListener(OnStartHostClicked);
        if (joinClientButton != null) joinClientButton.onClick.AddListener(ShowJoinGame);
        if (highScoresButton != null) highScoresButton.onClick.AddListener(ShowHighScores);
        if (controlsButton != null) controlsButton.onClick.AddListener(ShowControls);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);

        // Wire Join Game Button Listeners
        if (connectButton != null) connectButton.onClick.AddListener(OnConnectClicked);
        if (quickLocalhostButton != null) quickLocalhostButton.onClick.AddListener(OnQuickLocalhostClicked);
        if (cancelConnectButton != null) cancelConnectButton.onClick.AddListener(CancelConnectionAttempt);
        if (backFromJoinButton != null) backFromJoinButton.onClick.AddListener(ShowMainMenu);

        // Wire Sub-panel Button Listeners
        if (clearScoresButton != null) clearScoresButton.onClick.AddListener(OnClearScoresClicked);
        if (backFromScoresButton != null) backFromScoresButton.onClick.AddListener(OnBackToPreviousPanel);
        if (backFromControlsButton != null) backFromControlsButton.onClick.AddListener(ShowMainMenu);

        if (resumeButton != null) resumeButton.onClick.AddListener(ResumeGame);
        if (pauseHighScoresButton != null) pauseHighScoresButton.onClick.AddListener(ShowHighScores);
        if (disconnectButton != null) disconnectButton.onClick.AddListener(DisconnectToMenu);

        // Events
        Scoring.OnScoreUpdated += HandleScoreUpdated;
        if (HighScoreManager.Instance != null)
        {
            HighScoreManager.Instance.OnScoresChanged += RefreshHighScoreDisplay;
        }

        // Netcode Callbacks
        RegisterNetworkCallbacks();

        // Ensure Join Game & Controls UI are created and wired
        EnsureJoinGameUI();
        EnsureControlsUI();

        // Initialize state
        ShowMainMenu();
        RefreshHighScoreDisplay();
        RefreshHostIpDisplay();
    }

    private void OnDestroy()
    {
        Scoring.OnScoreUpdated -= HandleScoreUpdated;
        if (HighScoreManager.Instance != null)
        {
            HighScoreManager.Instance.OnScoresChanged -= RefreshHighScoreDisplay;
        }

        UnregisterNetworkCallbacks();
    }

    private void RegisterNetworkCallbacks()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
        }
    }

    private void UnregisterNetworkCallbacks()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
        }
    }

    private void Update()
    {
        if (isInGame)
        {
            bool pausePressed = MobileMotionManager.Instance != null ? MobileMotionManager.Instance.IsPauseTriggered() : Input.GetKeyDown(KeyCode.Escape);
            if (pausePressed)
            {
                if (isPaused)
                {
                    if (highScoresPanel != null && highScoresPanel.activeSelf)
                    {
                        ShowPauseMenu();
                    }
                    else
                    {
                        ResumeGame();
                    }
                }
                else
                {
                    PauseGame();
                }
            }
        }
    }

    public void ShowMainMenu()
    {
        isPaused = false;
        isInGame = false;
        isConnecting = false;

        if (connectionTimeoutCoroutine != null)
        {
            StopCoroutine(connectionTimeoutCoroutine);
            connectionTimeoutCoroutine = null;
        }

        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (joinGamePanel != null) joinGamePanel.SetActive(false);
        if (highScoresPanel != null) highScoresPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (inGameHudPanel != null) inGameHudPanel.SetActive(false);

        if (MobileHUD.Instance != null) MobileHUD.Instance.SetVisible(false);

        if (menuCamera != null) menuCamera.gameObject.SetActive(true);

        SetNetworkButtonsInteractable(true);
        if (cancelConnectButton != null) cancelConnectButton.gameObject.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        RefreshHostIpDisplay();
        RefreshHighScoreDisplay();
    }

    public void ShowJoinGame()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (joinGamePanel != null) joinGamePanel.SetActive(true);
        if (highScoresPanel != null) highScoresPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (inGameHudPanel != null) inGameHudPanel.SetActive(false);

        if (MobileHUD.Instance != null) MobileHUD.Instance.SetVisible(false);

        if (ipInputField != null)
        {
            ipInputField.text = PlayerPrefs.GetString(PrefsKeyLastIp, "127.0.0.1");
            ipInputField.Select();
            ipInputField.ActivateInputField();
        }

        if (portInputField != null)
        {
            portInputField.text = PlayerPrefs.GetInt(PrefsKeyLastPort, 7777).ToString();
        }

        SetNetworkButtonsInteractable(true);
        if (cancelConnectButton != null) cancelConnectButton.gameObject.SetActive(false);
        SetStatus("", Color.white);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ShowHighScores()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (joinGamePanel != null) joinGamePanel.SetActive(false);
        if (highScoresPanel != null) highScoresPanel.SetActive(true);
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);

        if (MobileHUD.Instance != null) MobileHUD.Instance.SetVisible(false);

        PopulateHighScoreList();
    }

    public void ShowControls()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (joinGamePanel != null) joinGamePanel.SetActive(false);
        if (highScoresPanel != null) highScoresPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(true);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);

        if (MobileHUD.Instance != null) MobileHUD.Instance.SetVisible(false);
    }

    public void ShowPauseMenu()
    {
        isPaused = true;
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (joinGamePanel != null) joinGamePanel.SetActive(false);
        if (highScoresPanel != null) highScoresPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
        if (inGameHudPanel != null) inGameHudPanel.SetActive(true);

        if (MobileHUD.Instance != null) MobileHUD.Instance.SetVisible(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void PauseGame()
    {
        ShowPauseMenu();
    }

    public void ResumeGame()
    {
        isPaused = false;
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (joinGamePanel != null) joinGamePanel.SetActive(false);
        if (highScoresPanel != null) highScoresPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (inGameHudPanel != null) inGameHudPanel.SetActive(true);

        if (MobileHUD.Instance != null) MobileHUD.Instance.SetVisible(true);

        if (!Application.isMobilePlatform)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void OnBackToPreviousPanel()
    {
        if (isInGame)
        {
            ShowPauseMenu();
        }
        else
        {
            ShowMainMenu();
        }
    }

    public void OnStartHostClicked()
    {
        ushort port = 7777;
        if (portInputField != null && ushort.TryParse(portInputField.text.Trim(), out ushort parsedPort) && parsedPort > 0)
        {
            port = parsedPort;
        }

        SaveNetworkSettings();
        SetStatus($"Starting Host on port {port}...", new Color(0.3f, 0.9f, 1f, 1f));

        bool started = false;
        if (GameManager.Instance != null)
        {
            started = GameManager.Instance.ConnectHost(port);
        }
        else if (NetworkManager.Singleton != null)
        {
            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                transport.SetConnectionData("127.0.0.1", port, "0.0.0.0");
            }
            started = NetworkManager.Singleton.StartHost();
        }

        if (started)
        {
            EnterInGameMode();
        }
        else
        {
            SetStatus("Failed to start Host. Check network adapter or port availability.", new Color(1f, 0.35f, 0.35f, 1f));
        }
    }

    public void OnConnectClicked()
    {
        string targetIp = GetTargetIp();
        ushort port = GetParsedPort();
        SaveNetworkSettings();

        isConnecting = true;
        SetNetworkButtonsInteractable(false);
        if (cancelConnectButton != null) cancelConnectButton.gameObject.SetActive(true);

        SetStatus($"Connecting to {targetIp}:{port}...", new Color(1f, 0.85f, 0.3f, 1f));

        bool started = false;
        if (GameManager.Instance != null)
        {
            started = GameManager.Instance.ConnectClient(targetIp, port);
        }
        else if (NetworkManager.Singleton != null)
        {
            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                transport.SetConnectionData(targetIp, port, "0.0.0.0");
            }
            started = NetworkManager.Singleton.StartClient();
        }

        if (started)
        {
            if (connectionTimeoutCoroutine != null) StopCoroutine(connectionTimeoutCoroutine);
            connectionTimeoutCoroutine = StartCoroutine(ConnectionTimeoutRoutine(targetIp, port, 15f));
        }
        else
        {
            isConnecting = false;
            SetNetworkButtonsInteractable(true);
            if (cancelConnectButton != null) cancelConnectButton.gameObject.SetActive(false);
            SetStatus($"Failed to start connection to {targetIp}:{port}.", new Color(1f, 0.35f, 0.35f, 1f));
        }
    }

    public void OnQuickLocalhostClicked()
    {
        if (ipInputField != null) ipInputField.text = "127.0.0.1";
        if (portInputField != null) portInputField.text = "7777";
        OnConnectClicked();
    }

    private IEnumerator ConnectionTimeoutRoutine(string ip, ushort port, float timeoutSeconds)
    {
        yield return new WaitForSeconds(timeoutSeconds);

        if (isConnecting && !isInGame)
        {
            SetStatus($"Connection to {ip}:{port} timed out.\nCheck Host IP & ensure Host is running with port {port} open.", new Color(1f, 0.4f, 0.4f, 1f));
            CancelConnectionAttempt();
        }
    }

    public void CancelConnectionAttempt()
    {
        if (connectionTimeoutCoroutine != null)
        {
            StopCoroutine(connectionTimeoutCoroutine);
            connectionTimeoutCoroutine = null;
        }

        isConnecting = false;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.Disconnect();
        }
        else if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        SetNetworkButtonsInteractable(true);
        if (cancelConnectButton != null) cancelConnectButton.gameObject.SetActive(false);
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton != null && clientId == NetworkManager.Singleton.LocalClientId)
        {
            isConnecting = false;
            if (connectionTimeoutCoroutine != null)
            {
                StopCoroutine(connectionTimeoutCoroutine);
                connectionTimeoutCoroutine = null;
            }

            EnterInGameMode();
            SetStatus("Connected!", new Color(0.3f, 1f, 0.6f, 1f));
        }
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton != null && clientId == NetworkManager.Singleton.LocalClientId)
        {
            bool wasInGame = isInGame;
            isConnecting = false;

            if (connectionTimeoutCoroutine != null)
            {
                StopCoroutine(connectionTimeoutCoroutine);
                connectionTimeoutCoroutine = null;
            }

            if (wasInGame)
            {
                DisconnectToMenu();
                SetStatus("Disconnected from host.", new Color(1f, 0.4f, 0.4f, 1f));
            }
            else
            {
                SetNetworkButtonsInteractable(true);
                if (cancelConnectButton != null) cancelConnectButton.gameObject.SetActive(false);
                SetStatus("Connection failed: Host unreachable or rejected.", new Color(1f, 0.35f, 0.35f, 1f));
            }
        }
    }

    private void EnterInGameMode()
    {
        isInGame = true;
        isPaused = false;
        isConnecting = false;
        currentSessionScore = 0;

        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (joinGamePanel != null) joinGamePanel.SetActive(false);
        if (highScoresPanel != null) highScoresPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (inGameHudPanel != null) inGameHudPanel.SetActive(true);

        if (MobileHUD.Instance != null) MobileHUD.Instance.SetVisible(true);

        if (menuCamera != null) menuCamera.gameObject.SetActive(false);

        if (!Application.isMobilePlatform)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        UpdateHudText();
    }

    public void DisconnectToMenu()
    {
        isInGame = false;
        isPaused = false;
        isConnecting = false;

        if (MobileHUD.Instance != null) MobileHUD.Instance.SetVisible(false);

        if (connectionTimeoutCoroutine != null)
        {
            StopCoroutine(connectionTimeoutCoroutine);
            connectionTimeoutCoroutine = null;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.Disconnect();
        }
        else if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        ShowMainMenu();
    }

    public void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnClearScoresClicked()
    {
        if (HighScoreManager.Instance != null)
        {
            HighScoreManager.Instance.ClearScores();
        }
        PopulateHighScoreList();
        RefreshHighScoreDisplay();
    }

    private void HandleScoreUpdated(int newScore)
    {
        currentSessionScore = newScore;
        UpdateHudText();
    }

    private void UpdateHudText()
    {
        if (currentScoreHudText != null)
        {
            currentScoreHudText.text = $"SCORE: {currentSessionScore:N0}";
        }

        int topScore = HighScoreManager.Instance != null ? HighScoreManager.Instance.GetTopHighScore() : 0;
        int displayHighScore = Mathf.Max(topScore, currentSessionScore);
        if (highScoreHudText != null)
        {
            highScoreHudText.text = $"HIGH SCORE: {displayHighScore:N0}";
        }
    }

    public void RefreshHighScoreDisplay()
    {
        int topScore = HighScoreManager.Instance != null ? HighScoreManager.Instance.GetTopHighScore() : 0;
        if (topHighScoreBadgeText != null)
        {
            topHighScoreBadgeText.text = $"RECORD: {topScore:N0} PTS";
        }
        UpdateHudText();
    }

    public void RefreshHostIpDisplay()
    {
        if (hostIpBadgeText != null)
        {
            string localIp = GameManager.GetLocalIPAddress();
            hostIpBadgeText.text = $"YOUR LAN IP: <color=#4ef2b8>{localIp}</color>";
        }
    }

    public void SetStatus(string message, Color color)
    {
        if (connectionStatusText != null)
        {
            connectionStatusText.text = message;
            connectionStatusText.color = color;
            connectionStatusText.gameObject.SetActive(!string.IsNullOrEmpty(message));
        }
    }

    private void SetNetworkButtonsInteractable(bool interactable)
    {
        if (startHostButton != null) startHostButton.interactable = interactable;
        if (joinClientButton != null) joinClientButton.interactable = interactable;
        if (connectButton != null) connectButton.interactable = interactable;
        if (quickLocalhostButton != null) quickLocalhostButton.interactable = interactable;
        if (ipInputField != null) ipInputField.interactable = interactable;
        if (portInputField != null) portInputField.interactable = interactable;
    }

    private string GetTargetIp()
    {
        if (ipInputField != null && !string.IsNullOrWhiteSpace(ipInputField.text))
        {
            return ipInputField.text.Trim();
        }
        return PlayerPrefs.GetString(PrefsKeyLastIp, "127.0.0.1");
    }

    private ushort GetParsedPort()
    {
        if (portInputField != null && ushort.TryParse(portInputField.text.Trim(), out ushort parsedPort) && parsedPort > 0)
        {
            return parsedPort;
        }
        return (ushort)PlayerPrefs.GetInt(PrefsKeyLastPort, 7777);
    }

    private void SaveNetworkSettings()
    {
        if (ipInputField != null && !string.IsNullOrWhiteSpace(ipInputField.text))
        {
            PlayerPrefs.SetString(PrefsKeyLastIp, ipInputField.text.Trim());
        }
        if (portInputField != null && ushort.TryParse(portInputField.text.Trim(), out ushort parsedPort))
        {
            PlayerPrefs.SetInt(PrefsKeyLastPort, parsedPort);
        }
        PlayerPrefs.Save();
    }

    private void EnsureJoinGameUI()
    {
        TMP_FontAsset font = topHighScoreBadgeText != null ? topHighScoreBadgeText.font : null;

        // 1. Host IP badge on Main Menu
        if (hostIpBadgeText == null && mainMenuPanel != null)
        {
            Transform mainCard = startHostButton != null ? startHostButton.transform.parent : mainMenuPanel.transform;
            GameObject badgeObj = new GameObject("HostIpBadgeText", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            badgeObj.transform.SetParent(mainCard, false);
            badgeObj.transform.SetSiblingIndex(startHostButton != null ? startHostButton.transform.GetSiblingIndex() : 0);

            var le = badgeObj.GetComponent<LayoutElement>();
            le.preferredHeight = 24;

            hostIpBadgeText = badgeObj.GetComponent<TextMeshProUGUI>();
            hostIpBadgeText.fontSize = 13;
            hostIpBadgeText.alignment = TextAlignmentOptions.Center;
            hostIpBadgeText.color = new Color(0.75f, 0.85f, 1f, 0.85f);
            if (font != null) hostIpBadgeText.font = font;
        }

        // 2. If JoinGamePanel is already wired in inspector, finish setup
        if (joinGamePanel != null && ipInputField != null && portInputField != null)
        {
            ipInputField.text = PlayerPrefs.GetString(PrefsKeyLastIp, "127.0.0.1");
            portInputField.text = PlayerPrefs.GetInt(PrefsKeyLastPort, 7777).ToString();
            return;
        }

        // 3. Dynamically construct JoinGamePanel under menuCanvas / root
        Transform canvasTransform = menuCanvas != null ? menuCanvas.transform : (mainMenuPanel != null ? mainMenuPanel.transform.parent : transform);
        if (canvasTransform == null) return;

        GameObject panelObj = new GameObject("JoinGamePanel", typeof(RectTransform));
        panelObj.transform.SetParent(canvasTransform, false);
        var panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        joinGamePanel = panelObj;

        // Card Container
        GameObject cardObj = new GameObject("JoinGameCard", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        cardObj.transform.SetParent(panelObj.transform, false);
        var cardRect = cardObj.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(520, 540);

        var cardImg = cardObj.GetComponent<Image>();
        cardImg.color = new Color(0.04f, 0.08f, 0.14f, 0.94f);

        var vlg = cardObj.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(36, 36, 32, 32);
        vlg.spacing = 12;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Title
        CreateTextElement(cardObj.transform, "JOIN MULTIPLAYER", 26, FontStyles.Bold, new Color(0.3f, 0.95f, 1f, 1f), TextAlignmentOptions.Center, font, 36);

        // Subtitle
        CreateTextElement(cardObj.transform, "Enter the Host's IP address or LAN address to join.", 14, FontStyles.Normal, new Color(0.7f, 0.8f, 0.9f, 0.8f), TextAlignmentOptions.Center, font, 24);

        // Spacer
        CreateSpacer(cardObj.transform, 6);

        // Input Fields Row (IP and Port)
        GameObject inputRow = new GameObject("InputRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        inputRow.transform.SetParent(cardObj.transform, false);
        var rowLe = inputRow.GetComponent<LayoutElement>();
        rowLe.preferredHeight = 72;

        var hlg = inputRow.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 12;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        string savedIp = PlayerPrefs.GetString(PrefsKeyLastIp, "127.0.0.1");
        string savedPort = PlayerPrefs.GetInt(PrefsKeyLastPort, 7777).ToString();

        ipInputField = CreateInputField(inputRow.transform, "HOST IP ADDRESS", savedIp, "e.g. 192.168.1.50", font, 320, 44);
        portInputField = CreateInputField(inputRow.transform, "PORT", savedPort, "7777", font, 110, 44);

        // Quick Localhost Button
        quickLocalhostButton = CreateButton(cardObj.transform, "QuickLocalhostButton", "QUICK JOIN: LOCALHOST (127.0.0.1)", new Color(0.12f, 0.25f, 0.38f, 0.9f), new Color(0.4f, 0.9f, 1f, 1f), font, 36);
        quickLocalhostButton.onClick.AddListener(OnQuickLocalhostClicked);

        // Status Text
        GameObject statusObj = new GameObject("ConnectionStatusText", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        statusObj.transform.SetParent(cardObj.transform, false);
        var statusLe = statusObj.GetComponent<LayoutElement>();
        statusLe.preferredHeight = 32;

        connectionStatusText = statusObj.GetComponent<TextMeshProUGUI>();
        connectionStatusText.fontSize = 13;
        connectionStatusText.alignment = TextAlignmentOptions.Center;
        connectionStatusText.textWrappingMode = TextWrappingModes.Normal;
        connectionStatusText.color = new Color(1f, 0.85f, 0.3f, 1f);
        if (font != null) connectionStatusText.font = font;
        connectionStatusText.text = "";

        // Cancel Connect Button (Initially Hidden)
        cancelConnectButton = CreateButton(cardObj.transform, "CancelConnectButton", "CANCEL CONNECTION", new Color(0.85f, 0.25f, 0.25f, 0.9f), Color.white, font, 40);
        cancelConnectButton.onClick.AddListener(CancelConnectionAttempt);
        cancelConnectButton.gameObject.SetActive(false);

        // Connect Button
        connectButton = CreateButton(cardObj.transform, "ConnectButton", "CONNECT / JOIN GAME", new Color(0.15f, 0.65f, 0.95f, 1f), Color.white, font, 46);
        connectButton.onClick.AddListener(OnConnectClicked);

        // Back Button
        backFromJoinButton = CreateButton(cardObj.transform, "BackFromJoinButton", "BACK TO MAIN MENU", new Color(0.2f, 0.24f, 0.3f, 0.9f), new Color(0.8f, 0.85f, 0.9f, 1f), font, 40);
        backFromJoinButton.onClick.AddListener(ShowMainMenu);

        panelObj.SetActive(false);
    }

    private TMP_InputField CreateInputField(Transform parent, string labelText, string initialValue, string placeholderText, TMP_FontAsset font, float width, float height)
    {
        GameObject container = new GameObject("FieldContainer_" + labelText, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        container.transform.SetParent(parent, false);

        var containerLe = container.GetComponent<LayoutElement>();
        containerLe.preferredWidth = width;
        containerLe.flexibleWidth = width > 200 ? 1f : 0f;
        containerLe.preferredHeight = height + 24f;

        var vlg = container.GetComponent<VerticalLayoutGroup>();
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 4;

        // Label Text
        CreateTextElement(container.transform, labelText, 12, FontStyles.Bold, new Color(0.65f, 0.8f, 0.95f, 0.9f), TextAlignmentOptions.Left, font, 18);

        // Input Box
        GameObject inputObj = new GameObject("InputBox", typeof(RectTransform), typeof(Image), typeof(TMP_InputField), typeof(LayoutElement));
        inputObj.transform.SetParent(container.transform, false);

        var inputLe = inputObj.GetComponent<LayoutElement>();
        inputLe.preferredHeight = height;
        inputLe.flexibleWidth = 1f;

        var bgImg = inputObj.GetComponent<Image>();
        bgImg.color = new Color(0.06f, 0.11f, 0.18f, 0.95f);
        bgImg.raycastTarget = true;

        // Text Viewport (Mask)
        GameObject textViewport = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        textViewport.transform.SetParent(inputObj.transform, false);
        var viewportRect = textViewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(12, 4);
        viewportRect.offsetMax = new Vector2(-12, -4);

        // Placeholder Text
        GameObject placeholderObj = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
        placeholderObj.transform.SetParent(textViewport.transform, false);
        var placeholderRect = placeholderObj.GetComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.sizeDelta = Vector2.zero;

        var placeholderTmp = placeholderObj.GetComponent<TextMeshProUGUI>();
        placeholderTmp.text = placeholderText;
        placeholderTmp.fontSize = 15;
        placeholderTmp.fontStyle = FontStyles.Italic;
        placeholderTmp.alignment = TextAlignmentOptions.Left;
        placeholderTmp.color = new Color(0.45f, 0.55f, 0.65f, 0.6f);
        placeholderTmp.raycastTarget = false;
        if (font != null) placeholderTmp.font = font;

        // Main Text
        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(textViewport.transform, false);
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        var textTmp = textObj.GetComponent<TextMeshProUGUI>();
        textTmp.text = initialValue;
        textTmp.fontSize = 15;
        textTmp.alignment = TextAlignmentOptions.Left;
        textTmp.color = new Color(0.9f, 0.98f, 1f, 1f);
        textTmp.raycastTarget = false;
        if (font != null) textTmp.font = font;

        // Setup TMP_InputField
        var inputField = inputObj.GetComponent<TMP_InputField>();
        inputField.targetGraphic = bgImg;
        inputField.textViewport = viewportRect;
        inputField.textComponent = textTmp;
        inputField.placeholder = placeholderTmp;
        inputField.fontAsset = font;
        inputField.interactable = true;
        inputField.text = initialValue;

        return inputField;
    }

    private Button CreateButton(Transform parent, string name, string label, Color bgColor, Color textColor, TMP_FontAsset font, float height)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        btnObj.transform.SetParent(parent, false);

        var le = btnObj.GetComponent<LayoutElement>();
        le.preferredHeight = height;
        le.flexibleWidth = 1f;

        var img = btnObj.GetComponent<Image>();
        img.color = bgColor;
        img.raycastTarget = true;

        var btn = btnObj.GetComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = bgColor;
        colors.highlightedColor = new Color(Mathf.Min(bgColor.r * 1.3f, 1f), Mathf.Min(bgColor.g * 1.3f, 1f), Mathf.Min(bgColor.b * 1.3f, 1f), 1f);
        colors.pressedColor = new Color(bgColor.r * 0.75f, bgColor.g * 0.75f, bgColor.b * 0.75f, 1f);
        btn.colors = colors;

        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(btnObj.transform, false);
        var rect = textObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        var tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 14;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = textColor;
        tmp.raycastTarget = false;
        if (font != null) tmp.font = font;

        return btn;
    }

    private TMP_Text CreateTextElement(Transform parent, string text, float fontSize, FontStyles fontStyle, Color color, TextAlignmentOptions alignment, TMP_FontAsset font, float preferredHeight)
    {
        GameObject textObj = new GameObject("TextElement", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        textObj.transform.SetParent(parent, false);

        var le = textObj.GetComponent<LayoutElement>();
        le.preferredHeight = preferredHeight;
        le.flexibleWidth = 1f;

        var tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = fontStyle;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.raycastTarget = false;
        if (font != null) tmp.font = font;

        return tmp;
    }

    private void CreateSpacer(Transform parent, float height)
    {
        GameObject spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
        spacer.transform.SetParent(parent, false);
        var le = spacer.GetComponent<LayoutElement>();
        le.preferredHeight = height;
    }

    private void PopulateHighScoreList()
    {
        if (scoreListContainer == null) return;

        for (int i = scoreListContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = scoreListContainer.GetChild(i);
            if (scoreRowPrefab != null && child.gameObject == scoreRowPrefab) continue;
            Destroy(child.gameObject);
        }

        var scores = HighScoreManager.Instance != null ? HighScoreManager.Instance.GetScoreList() : null;
        bool hasScores = scores != null && scores.Count > 0;

        if (emptyScoresText != null)
        {
            emptyScoresText.gameObject.SetActive(!hasScores);
        }

        if (!hasScores) return;

        for (int i = 0; i < scores.Count; i++)
        {
            var entry = scores[i];
            GameObject rowObj;

            if (scoreRowPrefab != null)
            {
                rowObj = Instantiate(scoreRowPrefab, scoreListContainer);
                rowObj.SetActive(true);
            }
            else
            {
                rowObj = CreateDefaultScoreRow(scoreListContainer);
            }

            var texts = rowObj.GetComponentsInChildren<TMP_Text>();
            if (texts.Length >= 4)
            {
                texts[0].text = $"#{i + 1}";
                texts[1].text = entry.playerName;
                texts[2].text = $"{entry.score:N0}";
                texts[3].text = entry.date;
            }
            else if (texts.Length >= 1)
            {
                texts[0].text = $"#{i + 1}  {entry.playerName} - {entry.score:N0} PTS ({entry.date})";
            }
        }
    }

    private GameObject CreateDefaultScoreRow(Transform parent)
    {
        GameObject row = new GameObject("ScoreRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(parent, false);

        var rect = row.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 36);

        var hlg = row.GetComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;
        hlg.spacing = 10;

        Color textColor = new Color(0.9f, 0.95f, 1f, 1f);
        TMP_FontAsset font = topHighScoreBadgeText != null ? topHighScoreBadgeText.font : null;

        CreateCell(row.transform, "#1", TextAlignmentOptions.Left, font, textColor, 60);
        CreateCell(row.transform, "Player", TextAlignmentOptions.Left, font, textColor, 180);
        CreateCell(row.transform, "0", TextAlignmentOptions.Right, font, new Color(0.3f, 1f, 0.6f, 1f), 120);
        CreateCell(row.transform, "Date", TextAlignmentOptions.Right, font, new Color(0.7f, 0.7f, 0.7f, 1f), 120);

        return row;
    }

    private TMP_Text CreateCell(Transform parent, string text, TextAlignmentOptions alignment, TMP_FontAsset font, Color color, float preferredWidth)
    {
        GameObject cell = new GameObject("Cell", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        cell.transform.SetParent(parent, false);

        var le = cell.GetComponent<LayoutElement>();
        le.preferredWidth = preferredWidth;

        var tmp = cell.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.alignment = alignment;
        tmp.fontSize = 18;
        tmp.color = color;
        if (font != null) tmp.font = font;

        return tmp;
    }

    private void EnsureControlsUI()
    {
        if (controlsPanel != null)
        {
            // If controlsPanel already exists in the scene, ensure back button listener is wired
            if (backFromControlsButton == null)
            {
                backFromControlsButton = controlsPanel.GetComponentInChildren<Button>();
            }
            if (backFromControlsButton != null)
            {
                backFromControlsButton.onClick.RemoveAllListeners();
                backFromControlsButton.onClick.AddListener(ShowMainMenu);
            }
            return;
        }

        // Dynamically construct a Cyberpunk ControlsPanel if not configured in scene
        Transform canvasTransform = menuCanvas != null ? menuCanvas.transform : (mainMenuPanel != null ? mainMenuPanel.transform.parent : transform);
        if (canvasTransform == null) return;

        TMP_FontAsset font = topHighScoreBadgeText != null ? topHighScoreBadgeText.font : null;

        GameObject panelObj = new GameObject("ControlsPanel", typeof(RectTransform));
        panelObj.transform.SetParent(canvasTransform, false);
        var panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        controlsPanel = panelObj;

        // Card Container
        GameObject cardObj = new GameObject("ControlsCard", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        cardObj.transform.SetParent(panelObj.transform, false);
        var cardRect = cardObj.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(620, 600);

        var cardImg = cardObj.GetComponent<Image>();
        cardImg.color = new Color(0.04f, 0.08f, 0.14f, 0.95f);

        var vlg = cardObj.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(36, 36, 28, 28);
        vlg.spacing = 8;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Title
        CreateTextElement(cardObj.transform, "GAME CONTROLS", 24, FontStyles.Bold, new Color(0.3f, 0.95f, 1f, 1f), TextAlignmentOptions.Center, font, 32);

        // Section: Android Motion Controls
        CreateTextElement(cardObj.transform, "📱 ANDROID MOTION CONTROLS", 15, FontStyles.Bold, new Color(0.2f, 0.9f, 0.6f, 1f), TextAlignmentOptions.Left, font, 22);
        CreateTextElement(cardObj.transform, "• Tilt Phone Forward/Back: Move Forward & Backward\n• Tilt Phone Left/Right: Strafe Left & Right\n• Gyroscope: Turn & Tilt Phone to Look / Aim in 3D\n• Flick Phone Upward: Motion Jump Gesture\n• Touch Buttons: 🦘 Jump, ⚡ Sprint, 🧲 Tether, ✋ Use, 🎯 Re-Center Gyro", 13, FontStyles.Normal, new Color(0.85f, 0.95f, 1f, 0.9f), TextAlignmentOptions.Left, font, 80);

        CreateSpacer(cardObj.transform, 6);

        // Section: Desktop Controls
        CreateTextElement(cardObj.transform, "💻 DESKTOP (KEYBOARD & MOUSE)", 15, FontStyles.Bold, new Color(0.4f, 0.75f, 1f, 1f), TextAlignmentOptions.Left, font, 22);
        CreateTextElement(cardObj.transform, "• WASD: Move / Climb Surfaces\n• Mouse: First-Person Look & Aim\n• Space: Jump | Left Shift: Sprint | Left Ctrl: Crouch\n• E: Grapple & Tether Loot Items | F: Interact / Collect\n• Escape: Pause Menu / Settings", 13, FontStyles.Normal, new Color(0.85f, 0.92f, 1f, 0.85f), TextAlignmentOptions.Left, font, 80);

        CreateSpacer(cardObj.transform, 12);

        // Back Button
        backFromControlsButton = CreateButton(cardObj.transform, "BackFromControlsButton", "BACK TO MAIN MENU", new Color(0.2f, 0.24f, 0.3f, 0.9f), new Color(0.8f, 0.85f, 0.9f, 1f), font, 40);
        backFromControlsButton.onClick.AddListener(ShowMainMenu);

        panelObj.SetActive(false);
    }
}
