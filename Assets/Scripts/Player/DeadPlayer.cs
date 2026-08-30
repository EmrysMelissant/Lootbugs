using UnityEngine;
using Unity.Netcode;

public class DeadPlayer : NetworkBehaviour, IInteractable
{
    [SerializeField] private string interactText = "F to Resurrect";
    public string InteractionText => interactText;

    // Synchronize the target player's ClientId across network
    private readonly NetworkVariable<ulong> targetOwnerClientId = new NetworkVariable<ulong>(
        ulong.MaxValue,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        // Keep corpse camera disabled by default
        Camera cam = GetComponentInChildren<Camera>(true);
        if (cam != null)
        {
            cam.enabled = false;
            cam.gameObject.SetActive(false);
        }
        AudioListener listener = GetComponentInChildren<AudioListener>(true);
        if (listener != null)
        {
            listener.enabled = false;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        targetOwnerClientId.OnValueChanged += OnTargetOwnerChanged;
        UpdateCameraState();
        if (targetOwnerClientId.Value != ulong.MaxValue)
        {
            UpdateMaterialFromTargetPlayer();
        }
    }

    public override void OnNetworkDespawn()
    {
        targetOwnerClientId.OnValueChanged -= OnTargetOwnerChanged;
        base.OnNetworkDespawn();
    }

    private void OnTargetOwnerChanged(ulong previousValue, ulong newValue)
    {
        UpdateMaterialFromTargetPlayer();
        UpdateCameraState();
    }

    private void UpdateCameraState()
    {
        bool isMyCorpse = NetworkManager.Singleton != null && 
                          targetOwnerClientId.Value != ulong.MaxValue &&
                          targetOwnerClientId.Value == NetworkManager.Singleton.LocalClientId;

        Camera cam = GetComponentInChildren<Camera>(true);
        AudioListener listener = GetComponentInChildren<AudioListener>(true);

        if (cam != null)
        {
            cam.enabled = isMyCorpse;
            cam.gameObject.SetActive(isMyCorpse);
        }
        if (listener != null)
        {
            listener.enabled = isMyCorpse;
        }

        if (TryGetComponent<PlayerCam>(out var playerCam))
        {
            playerCam.enabled = isMyCorpse;
        }
    }

    public void Initialize(ulong clientId, GameObject playerObject = null)
    {
        if (IsServer)
        {
            targetOwnerClientId.Value = clientId;
        }

        if (playerObject != null)
        {
            CopyMaterialFromPlayer(playerObject);
        }
    }

    public void UpdateMaterialFromTargetPlayer()
    {
        if (targetOwnerClientId.Value == ulong.MaxValue) return;

        GameObject playerObj = null;
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.ConnectedClients.TryGetValue(targetOwnerClientId.Value, out var client))
        {
            if (client.PlayerObject != null)
            {
                playerObj = client.PlayerObject.gameObject;
            }
        }

        if (playerObj == null)
        {
            // Search all PlayerControllers (including inactive)
            PlayerController[] controllers = Resources.FindObjectsOfTypeAll<PlayerController>();
            for (int i = 0; i < controllers.Length; i++)
            {
                PlayerController pc = controllers[i];
                if (pc != null && pc.OwnerClientId == targetOwnerClientId.Value)
                {
                    playerObj = pc.gameObject;
                    break;
                }
            }
        }

        if (playerObj != null)
        {
            CopyMaterialFromPlayer(playerObj);
        }
    }

    public void CopyMaterialFromPlayer(GameObject player)
    {
        if (player == null) return;

        Material playerMat = null;
        Renderer[] playerRenderers = player.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < playerRenderers.Length; i++)
        {
            Renderer rend = playerRenderers[i];
            if (rend == null) continue;
            if (rend is ParticleSystemRenderer || rend is TrailRenderer || rend is LineRenderer) continue;

            if (rend.sharedMaterial != null)
            {
                playerMat = rend.sharedMaterial;
                break;
            }
        }

        if (playerMat != null)
        {
            ApplyMaterial(playerMat);
        }
    }

    public void ApplyMaterial(Material mat)
    {
        if (mat == null) return;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer rend = renderers[i];
            if (rend == null) continue;
            if (rend is ParticleSystemRenderer || rend is TrailRenderer || rend is LineRenderer) continue;

            rend.material = mat;
        }
    }

    public void Interact(GameObject interactor)
    {
        if (interactor == null) return;

        // Require the interactor to have PlayerController component
        if (interactor.TryGetComponent(out PlayerController reviverPlayer))
        {
            // Calculate health sacrifice costs
            float healthCost = 20f;
            
            if (reviverPlayer.Health > healthCost)
            {
                reviverPlayer.TakeDamage(healthCost);
                RequestReviveServerRpc();
            }
            else
            {
                // Warn or prevent revive if cost would kill reviver
                Debug.LogWarning("Not enough health to resurrect team member!");
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestReviveServerRpc()
    {
        if (!IsServer) return;

        ulong targetClientId = targetOwnerClientId.Value;
        ulong playerNetObjectId = 0;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.ConnectedClients.TryGetValue(targetClientId, out var client))
        {
            if (client.PlayerObject != null)
            {
                playerNetObjectId = client.PlayerObject.NetworkObjectId;
            }
        }

        ReviveClientRpc(targetClientId, playerNetObjectId, transform.position);

        // Despawn death item marker on server
        if (TryGetComponent(out NetworkObject netObj) && netObj.IsSpawned)
        {
            netObj.Despawn();
        }
    }

    [ClientRpc]
    private void ReviveClientRpc(ulong targetClientId, ulong playerNetObjectId, Vector3 revivePosition)
    {
        PlayerController targetPlayer = null;

        if (NetworkManager.Singleton != null)
        {
            if (playerNetObjectId != 0 && NetworkManager.Singleton.SpawnManager != null &&
                NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerNetObjectId, out var foundObj))
            {
                targetPlayer = foundObj.GetComponent<PlayerController>();
            }
            else if (NetworkManager.Singleton.ConnectedClients != null && NetworkManager.Singleton.ConnectedClients.TryGetValue(targetClientId, out var client))
            {
                if (client.PlayerObject != null)
                {
                    targetPlayer = client.PlayerObject.GetComponent<PlayerController>();
                }
            }
        }

        // Fallback: Search all PlayerControllers (including inactive GameObjects) in the scene
        if (targetPlayer == null)
        {
            PlayerController[] allPlayers = Resources.FindObjectsOfTypeAll<PlayerController>();
            for (int i = 0; i < allPlayers.Length; i++)
            {
                PlayerController pc = allPlayers[i];
                if (pc == null || pc.gameObject.scene.name == null) continue;
                if (pc.OwnerClientId == targetClientId || (playerNetObjectId != 0 && pc.NetworkObjectId == playerNetObjectId))
                {
                    targetPlayer = pc;
                    break;
                }
            }
        }

        if (targetPlayer != null)
        {
            if (!targetPlayer.gameObject.activeSelf)
            {
                targetPlayer.gameObject.SetActive(true);
            }

            targetPlayer.OnRevived(revivePosition);
        }
    }
}