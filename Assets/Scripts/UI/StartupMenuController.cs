using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class StartupMenuController : MonoBehaviour
{
    public static StartupMenuController Instance { get; private set; }

    private const string PrefsKeyLastIp = "Lootbugs_LastConnectedIP";
    private const string PrefsKeyLastPort = "Lootbugs_LastConnectedPort";

    [Header("Scene Transition Settings")]
    [Tooltip("The hub scene to load when starting a host or joining a game.")]
    [SerializeField] private string hubSceneName = "MainHub";

    [Header("Panels")]
    [SerializeField] private GameObject menuCanvas;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject joinGamePanel;
    [SerializeField] private GameObject controlsPanel;
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject inGameHudPanel;

    [Header("Main Menu Elements")]
    [SerializeField] private TMP_Text hostIpBadgeText;
    [SerializeField] private Button startHostButton;
    [SerializeField] private Button joinClientButton;
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

    [Header("Controls Panel Elements")]
    [SerializeField] private Button backFromControlsButton;

    [Header("Pause Menu Elements")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button disconnectButton;

    [Header("In-Game HUD Elements")]
    [SerializeField] private TMP_Text currentScoreHudText;

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
        if (controlsButton != null) controlsButton.onClick.AddListener(ShowControls);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);

        // Wire Join Game Button Listeners
        if (connectButton != null) connectButton.onClick.AddListener(OnConnectClicked);
        if (quickLocalhostButton != null) quickLocalhostButton.onClick.AddListener(OnQuickLocalhostClicked);
        if (cancelConnectButton != null) cancelConnectButton.onClick.AddListener(CancelConnectionAttempt);
        if (backFromJoinButton != null) backFromJoinButton.onClick.AddListener(ShowMainMenu);

        // Wire Sub-panel Button Listeners
        if (backFromControlsButton != null) backFromControlsButton.onClick.AddListener(ShowMainMenu);

        if (resumeButton != null) resumeButton.onClick.AddListener(ResumeGame);
        if (disconnectButton != null) disconnectButton.onClick.AddListener(DisconnectToMenu);

        // Events
        Scoring.OnScoreUpdated += HandleScoreUpdated;

        // Netcode Callbacks
        RegisterNetworkCallbacks();

        // Initialize saved network settings
        InitializeSavedNetworkSettings();

        // Initialize state
        ShowMainMenu();
        RefreshHostIpDisplay();
    }

    private void OnDestroy()
    {
        Scoring.OnScoreUpdated -= HandleScoreUpdated;
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
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (inGameHudPanel != null) inGameHudPanel.SetActive(false);

        if (menuCamera != null) menuCamera.gameObject.SetActive(true);

        SetConnectingUIState(false);
        SetStatus("", Color.white);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        RefreshHostIpDisplay();
    }

    public void ShowJoinGame()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (joinGamePanel != null) joinGamePanel.SetActive(true);
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (inGameHudPanel != null) inGameHudPanel.SetActive(false);

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

        SetConnectingUIState(false);
        SetStatus("", Color.white);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ShowControls()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (joinGamePanel != null) joinGamePanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(true);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
    }

    public void ShowPauseMenu()
    {
        isPaused = true;
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (joinGamePanel != null) joinGamePanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
        if (inGameHudPanel != null) inGameHudPanel.SetActive(true);

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
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (inGameHudPanel != null) inGameHudPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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

            // Load the MainHub scene and spawn player on network
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && NetworkManager.Singleton.IsServer)
            {
                if (SceneManager.GetActiveScene().name != hubSceneName)
                {
                    if (NetworkManager.Singleton.SceneManager != null)
                    {
                        NetworkManager.Singleton.SceneManager.LoadScene(hubSceneName, LoadSceneMode.Single);
                    }
                    else
                    {
                        SceneManager.LoadScene(hubSceneName);
                    }
                }
            }
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

        SetConnectingUIState(true);
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
            SetConnectingUIState(false);
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
            CancelConnectionAttempt();
            SetStatus($"Connection to {ip}:{port} timed out.\nCheck Host IP & ensure Host is running with port {port} open.", new Color(1f, 0.4f, 0.4f, 1f));
        }
    }

    public void CancelConnectionAttempt()
    {
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

        SetConnectingUIState(false);
        SetStatus("", Color.white);
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton != null && clientId == NetworkManager.Singleton.LocalClientId)
        {
            SetConnectingUIState(false);
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
            SetConnectingUIState(false);

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
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (inGameHudPanel != null) inGameHudPanel.SetActive(true);

        if (menuCamera != null) menuCamera.gameObject.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        UpdateHudText();
    }

    public void DisconnectToMenu()
    {
        isInGame = false;
        isPaused = false;
        isConnecting = false;

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

        if (SceneManager.GetActiveScene().name != "MainMenu" && Application.CanStreamedLevelBeLoaded("MainMenu"))
        {
            SceneManager.LoadScene("MainMenu");
        }
        else
        {
            ShowMainMenu();
        }
    }

    public void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
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

    private void SetConnectingUIState(bool connecting)
    {
        isConnecting = connecting;
        SetNetworkButtonsInteractable(!connecting);

        if (connectButton != null) connectButton.gameObject.SetActive(!connecting);
        if (quickLocalhostButton != null) quickLocalhostButton.gameObject.SetActive(!connecting);
        if (backFromJoinButton != null) backFromJoinButton.gameObject.SetActive(!connecting);
        if (cancelConnectButton != null) cancelConnectButton.gameObject.SetActive(connecting);
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

    private void InitializeSavedNetworkSettings()
    {
        if (ipInputField != null)
        {
            ipInputField.text = PlayerPrefs.GetString(PrefsKeyLastIp, "127.0.0.1");
        }
        if (portInputField != null)
        {
            portInputField.text = PlayerPrefs.GetInt(PrefsKeyLastPort, 7777).ToString();
        }
    }
}

