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
        if (existingPanel == null)
        {
            existingPanel = transform.Find("Canvas/PauseCard");
        }

        if (existingPanel != null)
        {
            pauseMenuPanel = existingPanel.gameObject;
            WireButtons();
        }
    }

    private void WireButtons()
    {
        if (pauseMenuPanel == null) return;

        if (resumeButton == null)
        {
            Button[] buttons = pauseMenuPanel.GetComponentsInChildren<Button>(true);
            foreach (Button btn in buttons)
            {
                if (btn == null) continue;
                string lowerName = btn.name.ToLower();
                if (lowerName.Contains("resume")) resumeButton = btn;
                else if (lowerName.Contains("disconnect") || lowerName.Contains("leave") || lowerName.Contains("menu") || lowerName.Contains("quit")) disconnectButton = btn;
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
}

