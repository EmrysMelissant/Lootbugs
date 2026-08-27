using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartRun : MonoBehaviour, IInteractable
{
    [Header("Run Settings")]
    [Tooltip("The name of the scene to load when the run starts.")]
    [SerializeField] private string runSceneName = "MainMap";

    [Tooltip("Prompt key or prefix.")]
    [SerializeField] private string interactText = "E";

    private readonly HashSet<Collider> collidersInZone = new HashSet<Collider>();

    public string InteractionText
    {
        get
        {
            int totalPlayers = GetTotalConnectedPlayerCount();
            int readyPlayers = GetReadyPlayerCount();

            if (totalPlayers > 0 && readyPlayers >= totalPlayers)
            {
                return "Start Run";
            }

            return $"({readyPlayers}/{totalPlayers}) Players Ready";
        }
    }

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other != null)
        {
            collidersInZone.Add(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other != null)
        {
            collidersInZone.Remove(other);
        }
    }

    public void Interact(GameObject interactor)
    {
        int totalPlayers = GetTotalConnectedPlayerCount();
        int readyPlayers = GetReadyPlayerCount();

        if (totalPlayers == 0 || readyPlayers < totalPlayers)
        {
            Debug.Log($"[StartRun] Not all players are ready! ({readyPlayers}/{totalPlayers})");
            return;
        }

        Debug.Log($"[StartRun] All players ready ({readyPlayers}/{totalPlayers})! Starting run...");
        LoadRunScene(interactor);
    }

    private void LoadRunScene(GameObject interactor)
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.SceneManager.LoadScene(runSceneName, LoadSceneMode.Single);
            }
            else
            {
                PlayerInteraction playerInteraction = null;
                if (interactor != null)
                {
                    playerInteraction = interactor.GetComponentInParent<PlayerInteraction>();
                }

                if (playerInteraction != null)
                {
                    playerInteraction.RequestStartRunServerRpc(runSceneName);
                }
                else
                {
                    Debug.LogWarning("[StartRun] Unable to send start run request to server: PlayerInteraction component missing on interactor.");
                }
            }
        }
        else
        {
            SceneManager.LoadScene(runSceneName);
        }
    }

    public int GetTotalConnectedPlayerCount()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            int netCount = NetworkManager.Singleton.ConnectedClientsList.Count;
            if (netCount > 0)
            {
                return netCount;
            }
        }

        NewClimbing[] scenePlayers = FindObjectsByType<NewClimbing>(FindObjectsSortMode.None);
        return scenePlayers != null ? scenePlayers.Length : 0;
    }

    public int GetReadyPlayerCount()
    {
        return GetReadyPlayers().Count;
    }

    public List<NewClimbing> GetReadyPlayers()
    {
        collidersInZone.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);

        List<NewClimbing> readyPlayers = new List<NewClimbing>();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client == null || client.PlayerObject == null) continue;

                NewClimbing player = client.PlayerObject.GetComponent<NewClimbing>();
                if (player == null) continue;

                if (IsPlayerInZone(player))
                {
                    if (!readyPlayers.Contains(player))
                    {
                        readyPlayers.Add(player);
                    }
                }
            }
        }
        else
        {
            foreach (Collider col in collidersInZone)
            {
                if (col == null) continue;

                NewClimbing player = col.GetComponentInParent<NewClimbing>();
                if (player != null && !readyPlayers.Contains(player))
                {
                    readyPlayers.Add(player);
                }
            }
        }

        return readyPlayers;
    }

    private bool IsPlayerInZone(NewClimbing player)
    {
        if (player == null) return false;

        foreach (Collider col in collidersInZone)
        {
            if (col == null) continue;

            if (col.GetComponentInParent<NewClimbing>() == player)
            {
                return true;
            }
        }

        return false;
    }
}

