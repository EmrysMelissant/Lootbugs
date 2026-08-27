using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.Netcode;

public class PlayerPauseMenu : NetworkBehaviour
{
    [Header("UI References")]
    [Tooltip("The root GameObject of the pause menu panel. If unassigned, one will be created automatically under the player Canvas.")]
    [SerializeField] private GameObject pauseMenuPanel;

    [Tooltip("Resume button. If unassigned, will be wired automatically.")]
    [SerializeField] private Button resumeButton;

    [Tooltip("Disconnect/Leave button. If unassigned, will be wired automatically.")]
    [SerializeField] private Button disconnectButton;

    [Header("Scene Settings")]
    [Tooltip("Name of the main menu scene to load on disconnect.")]
    [SerializeField] private string menuSceneName = "MainMenu";

    private bool isPaused = false;
    public bool IsPaused => isPaused;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner)
        {
            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(false);
            }
            this.enabled = false;
            return;
        }

        EnsurePauseMenuUI();
    }

    private void Start()
    {
        if (!IsOwner) return;

        EnsurePauseMenuUI();

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        if (!IsOwner) return;

        isPaused = true;

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        if (!IsOwner) return;

        isPaused = false;

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void DisconnectToMainMenu()
    {
        if (!IsOwner) return;

        isPaused = false;

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.Disconnect();
        }
        else if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        if (Application.CanStreamedLevelBeLoaded(menuSceneName))
        {
            SceneManager.LoadScene(menuSceneName);
        }
        else
        {
            Debug.LogWarning($"[PlayerPauseMenu] Scene '{menuSceneName}' is not in Build Settings.");
        }
    }

    private void EnsurePauseMenuUI()
    {
        // 1. If panel is already assigned, wire buttons if present
        if (pauseMenuPanel != null)
        {
            WireButtons();
            return;
        }

        // 2. Look for existing PauseMenu in child hierarchy
        Transform existingPanel = transform.Find("Canvas/PauseMenuPanel");
        if (existingPanel == null)
        {
            existingPanel = transform.Find("Canvas/PauseMenu");
        }

        if (existingPanel != null)
        {
            pauseMenuPanel = existingPanel.gameObject;
            WireButtons();
            return;
        }

        // 3. Find the player's Canvas
        Canvas playerCanvas = GetComponentInChildren<Canvas>(true);
        if (playerCanvas == null)
        {
            playerCanvas = FindAnyObjectByType<Canvas>();
        }

        if (playerCanvas == null) return;

        // 4. Construct PauseMenu UI dynamically under Player Canvas
        CreateDynamicPauseMenuUI(playerCanvas.transform);
    }

    private void WireButtons()
    {
        if (pauseMenuPanel == null) return;

        if (resumeButton == null)
        {
            Button[] buttons = pauseMenuPanel.GetComponentsInChildren<Button>(true);
            foreach (Button btn in buttons)
            {
                string lowerName = btn.name.ToLower();
                if (lowerName.Contains("resume")) resumeButton = btn;
                else if (lowerName.Contains("disconnect") || lowerName.Contains("leave") || lowerName.Contains("menu")) disconnectButton = btn;
            }
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(ResumeGame);
            resumeButton.onClick.AddListener(ResumeGame);
        }

        if (disconnectButton != null)
        {
            disconnectButton.onClick.RemoveListener(DisconnectToMainMenu);
            disconnectButton.onClick.AddListener(DisconnectToMainMenu);
        }
    }

    private void CreateDynamicPauseMenuUI(Transform canvasParent)
    {
        // Panel Root (FullScreen overlay)
        GameObject panelObj = new GameObject("PauseMenuPanel", typeof(RectTransform), typeof(Image));
        panelObj.transform.SetParent(canvasParent, false);

        var panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;

        var panelBg = panelObj.GetComponent<Image>();
        panelBg.color = new Color(0.02f, 0.04f, 0.08f, 0.75f);

        // Center Card Container
        GameObject cardObj = new GameObject("PauseCard", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        cardObj.transform.SetParent(panelObj.transform, false);

        var cardRect = cardObj.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(420, 310);

        var cardImg = cardObj.GetComponent<Image>();
        cardImg.color = new Color(0.06f, 0.10f, 0.16f, 0.96f);

        var vlg = cardObj.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(32, 32, 32, 32);
        vlg.spacing = 14;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Title
        CreateText(cardObj.transform, "PAUSED", 26, FontStyles.Bold, new Color(0.3f, 0.95f, 1f, 1f), TextAlignmentOptions.Center, 36);

        // Subtitle
        CreateText(cardObj.transform, "Session in progress", 13, FontStyles.Normal, new Color(0.7f, 0.8f, 0.9f, 0.7f), TextAlignmentOptions.Center, 20);

        // Spacer
        CreateSpacer(cardObj.transform, 10);

        // Buttons
        resumeButton = CreateButton(cardObj.transform, "ResumeButton", "RESUME", new Color(0.12f, 0.55f, 0.85f, 1f), Color.white, 44);
        resumeButton.onClick.AddListener(ResumeGame);

        disconnectButton = CreateButton(cardObj.transform, "DisconnectButton", "DISCONNECT TO MENU", new Color(0.20f, 0.25f, 0.32f, 1f), new Color(0.9f, 0.92f, 0.95f, 1f), 44);
        disconnectButton.onClick.AddListener(DisconnectToMainMenu);

        pauseMenuPanel = panelObj;
        pauseMenuPanel.SetActive(false);
    }

    private Button CreateButton(Transform parent, string name, string label, Color bgColor, Color textColor, float height)
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

        return btn;
    }

    private TMP_Text CreateText(Transform parent, string text, float fontSize, FontStyles fontStyle, Color color, TextAlignmentOptions alignment, float preferredHeight)
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

        return tmp;
    }

    private void CreateSpacer(Transform parent, float height)
    {
        GameObject spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
        spacer.transform.SetParent(parent, false);
        var le = spacer.GetComponent<LayoutElement>();
        le.preferredHeight = height;
    }
}
