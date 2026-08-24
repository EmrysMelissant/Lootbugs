using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class StartupMenuController : MonoBehaviour
{
    public static StartupMenuController Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject menuCanvas;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject highScoresPanel;
    [SerializeField] private GameObject controlsPanel;
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject inGameHudPanel;

    [Header("Main Menu Elements")]
    [SerializeField] private TMP_Text topHighScoreBadgeText;
    [SerializeField] private Button startHostButton;
    [SerializeField] private Button joinClientButton;
    [SerializeField] private Button highScoresButton;
    [SerializeField] private Button controlsButton;
    [SerializeField] private Button quitButton;

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
    private int currentSessionScore = 0;

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
        // Wire Button Listeners
        if (startHostButton != null) startHostButton.onClick.AddListener(OnStartHostClicked);
        if (joinClientButton != null) joinClientButton.onClick.AddListener(OnJoinClientClicked);
        if (highScoresButton != null) highScoresButton.onClick.AddListener(ShowHighScores);
        if (controlsButton != null) controlsButton.onClick.AddListener(ShowControls);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);

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

        // Initialize state
        ShowMainMenu();
        RefreshHighScoreDisplay();
    }

    private void OnDestroy()
    {
        Scoring.OnScoreUpdated -= HandleScoreUpdated;
        if (HighScoreManager.Instance != null)
        {
            HighScoreManager.Instance.OnScoresChanged -= RefreshHighScoreDisplay;
        }
    }

    private void Update()
    {
        if (isInGame)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
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
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (highScoresPanel != null) highScoresPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (inGameHudPanel != null) inGameHudPanel.SetActive(false);

        if (menuCamera != null) menuCamera.gameObject.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        RefreshHighScoreDisplay();
    }

    public void ShowHighScores()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (highScoresPanel != null) highScoresPanel.SetActive(true);
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);

        PopulateHighScoreList();
    }

    public void ShowControls()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (highScoresPanel != null) highScoresPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(true);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
    }

    public void ShowPauseMenu()
    {
        isPaused = true;
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (highScoresPanel != null) highScoresPanel.SetActive(false);
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
        if (highScoresPanel != null) highScoresPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (inGameHudPanel != null) inGameHudPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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
        isInGame = true;
        isPaused = false;
        currentSessionScore = 0;

        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (highScoresPanel != null) highScoresPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (inGameHudPanel != null) inGameHudPanel.SetActive(true);

        if (menuCamera != null) menuCamera.gameObject.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        UpdateHudText();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ConnectHost();
        }
        else if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.StartHost();
        }
    }

    public void OnJoinClientClicked()
    {
        isInGame = true;
        isPaused = false;
        currentSessionScore = 0;

        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (highScoresPanel != null) highScoresPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (inGameHudPanel != null) inGameHudPanel.SetActive(true);

        if (menuCamera != null) menuCamera.gameObject.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        UpdateHudText();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ConnectClient();
        }
        else if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.StartClient();
        }
    }

    public void DisconnectToMenu()
    {
        isInGame = false;
        isPaused = false;

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

    private void PopulateHighScoreList()
    {
        if (scoreListContainer == null) return;

        // Clear existing generated rows (skip template if it's a child)
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
            // Expecting 4 texts: Rank, Name, Score, Date or single formatted text
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
        TMP_FontAsset font = null;
        if (topHighScoreBadgeText != null) font = topHighScoreBadgeText.font;

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
}
