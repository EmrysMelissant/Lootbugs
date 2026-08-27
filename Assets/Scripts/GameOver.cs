using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class GameOver : NetworkBehaviour
{
    [Header("Game Over Settings")]
    [Tooltip("The scene to return to when all players are dead.")]
    [SerializeField] private string menuSceneName = "MainMenu";

    [Tooltip("Delay in seconds after all players die before returning to the main menu.")]
    [SerializeField] private float returnDelay = 3f;

    private bool hasSeenAlivePlayer = false;
    private bool isReturningToMenu = false;

    private void Update()
    {
        if (isReturningToMenu) return;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            // In multiplayer, the server/host orchestrates game over
            if (!IsServer) return;
        }

        CheckAllPlayersDead();
    }

    private void CheckAllPlayersDead()
    {
        int totalPlayers = 0;
        int alivePlayers = 0;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client == null || client.PlayerObject == null) continue;

                totalPlayers++;
                NewClimbing player = client.PlayerObject.GetComponent<NewClimbing>();
                if (IsPlayerAlive(player))
                {
                    alivePlayers++;
                }
            }
        }
        else
        {
            NewClimbing[] scenePlayers = FindObjectsByType<NewClimbing>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (scenePlayers != null)
            {
                foreach (var player in scenePlayers)
                {
                    if (player == null) continue;
                    totalPlayers++;
                    if (IsPlayerAlive(player))
                    {
                        alivePlayers++;
                    }
                }
            }
        }

        if (alivePlayers > 0)
        {
            hasSeenAlivePlayer = true;
        }

        // Only trigger Game Over if we have confirmed player presence in the scene and all of them are dead
        if (hasSeenAlivePlayer && totalPlayers > 0 && alivePlayers == 0)
        {
            isReturningToMenu = true;
            StartCoroutine(ReturnToMainMenuCoroutine());
        }
    }

    private bool IsPlayerAlive(NewClimbing player)
    {
        if (player == null) return false;
        if (!player.gameObject.activeInHierarchy) return false;
        if (!player.IsAlive || player.Health <= 0f) return false;
        return true;
    }

    private IEnumerator ReturnToMainMenuCoroutine()
    {
        Debug.Log($"[GameOver] All players are dead! Returning to '{menuSceneName}' in {returnDelay} seconds...");

        yield return new WaitForSeconds(returnDelay);

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && IsServer && IsSpawned)
        {
            ReturnToMainMenuClientRpc();
        }

        ReturnToMenuLocal();
    }

    [ClientRpc]
    private void ReturnToMainMenuClientRpc()
    {
        if (IsServer) return; // Server handles its own return in ReturnToMainMenuCoroutine
        ReturnToMenuLocal();
    }

    private void ReturnToMenuLocal()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Quota.ResetSession();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.Disconnect();
        }
        else if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer))
        {
            NetworkManager.Singleton.Shutdown();
        }

        if (Application.CanStreamedLevelBeLoaded(menuSceneName))
        {
            SceneManager.LoadScene(menuSceneName);
        }
        else
        {
            Debug.LogWarning($"[GameOver] Scene '{menuSceneName}' cannot be loaded. Check Build Settings.");
        }
    }
}
