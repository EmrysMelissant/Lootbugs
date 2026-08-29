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
            if (!isActiveAndEnabled) return "";

            int totalAlivePlayers = GetTotalAlivePlayerCount();
            int readyPlayers = GetReadyPlayerCount();

            if (totalAlivePlayers > 0 && readyPlayers >= totalAlivePlayers)
            {
                return "Ready";
            }

            return $"({readyPlayers}/{totalAlivePlayers}) Players Ready";
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

    private void OnDisable()
    {
        collidersInZone.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActiveAndEnabled) return;

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
        if (!isActiveAndEnabled) return;

        int totalAlivePlayers = GetTotalAlivePlayerCount();
        int readyPlayers = GetReadyPlayerCount();

        if (totalAlivePlayers == 0 || readyPlayers < totalAlivePlayers)
        {
            Debug.Log($"[StartRun] Not all alive players are ready! ({readyPlayers}/{totalAlivePlayers})");
            return;
        }

        Debug.Log($"[StartRun] All alive players ready ({readyPlayers}/{totalAlivePlayers})! Starting run...");
        
        if (runSceneName != "MainHub" && runSceneName != "MainMenu")
        {
            Quota.MarkRunStarted();
        }

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

    public int GetTotalAlivePlayerCount()
    {
        return GetAlivePlayers().Count;
    }

    public List<PlayerController> GetAlivePlayers()
    {
        List<PlayerController> alivePlayers = new List<PlayerController>();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client == null || client.PlayerObject == null) continue;

                PlayerController player = client.PlayerObject.GetComponent<PlayerController>();
                if (IsPlayerAlive(player) && !alivePlayers.Contains(player))
                {
                    alivePlayers.Add(player);
                }
            }
        }
        else
        {
            PlayerController[] scenePlayers = FindObjectsByType<PlayerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (scenePlayers != null)
            {
                foreach (var player in scenePlayers)
                {
                    if (IsPlayerAlive(player) && !alivePlayers.Contains(player))
                    {
                        alivePlayers.Add(player);
                    }
                }
            }
        }

        return alivePlayers;
    }

    public int GetReadyPlayerCount()
    {
        return GetReadyPlayers().Count;
    }

    public List<PlayerController> GetReadyPlayers()
    {
        collidersInZone.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);

        List<PlayerController> readyPlayers = new List<PlayerController>();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client == null || client.PlayerObject == null) continue;

                PlayerController player = client.PlayerObject.GetComponent<PlayerController>();
                if (IsPlayerAlive(player) && IsPlayerInZone(player))
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

                PlayerController player = col.GetComponentInParent<PlayerController>();
                if (IsPlayerAlive(player) && !readyPlayers.Contains(player))
                {
                    readyPlayers.Add(player);
                }
            }
        }

        return readyPlayers;
    }

    private bool IsPlayerAlive(PlayerController player)
    {
        if (player == null) return false;
        if (!player.gameObject.activeInHierarchy) return false;
        if (!player.IsAlive || player.Health <= 0f) return false;
        return true;
    }

    private bool IsPlayerInZone(PlayerController player)
    {
        if (player == null) return false;

        foreach (Collider col in collidersInZone)
        {
            if (col == null) continue;

            if (col.GetComponentInParent<PlayerController>() == player)
            {
                return true;
            }
        }

        return false;
    }
}

