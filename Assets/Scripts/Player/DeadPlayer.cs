using UnityEngine;
using Unity.Netcode;

public class DeadPlayer : NetworkBehaviour, IInteractable
{
    [SerializeField] private string interactText = "F to Resurrect";
    public string InteractionText => interactText;

    [Header("Death Camera Settings")]
    [Tooltip("Camera activated for the dead player.")]
    [SerializeField] private GameObject deathCamera;

    // Synchronize the target player's ClientId across network
    private readonly NetworkVariable<ulong> targetOwnerClientId = new NetworkVariable<ulong>(
        ulong.MaxValue,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        if (deathCamera == null)
        {
            Transform camTrans = transform.Find("Camera");
            if (camTrans != null)
            {
                deathCamera = camTrans.gameObject;
            }
            else
            {
                Camera cam = GetComponentInChildren<Camera>(true);
                if (cam != null)
                {
                    deathCamera = cam.gameObject;
                }
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        targetOwnerClientId.OnValueChanged += OnTargetOwnerChanged;
        UpdateDeathCamera();
    }

    public override void OnNetworkDespawn()
    {
        targetOwnerClientId.OnValueChanged -= OnTargetOwnerChanged;

        if (deathCamera != null)
        {
            deathCamera.SetActive(false);
        }

        base.OnNetworkDespawn();
    }

    private void OnTargetOwnerChanged(ulong previousValue, ulong newValue)
    {
        UpdateDeathCamera();
    }

    public void Initialize(ulong clientId)
    {
        if (IsServer)
        {
            targetOwnerClientId.Value = clientId;
            UpdateDeathCamera();
        }
    }

    private void UpdateDeathCamera()
    {
        if (NetworkManager.Singleton == null) return;

        bool isLocalDeadPlayer = (NetworkManager.Singleton.LocalClientId == targetOwnerClientId.Value);

        if (deathCamera != null)
        {
            deathCamera.SetActive(isLocalDeadPlayer);

            if (deathCamera.TryGetComponent(out AudioListener listener))
            {
                listener.enabled = isLocalDeadPlayer;
            }
        }

        // Guarantee local player GameObject is fully disabled when dead
        if (isLocalDeadPlayer && NetworkManager.Singleton.LocalClient != null && NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            NetworkManager.Singleton.LocalClient.PlayerObject.gameObject.SetActive(false);
        }
    }

    public void Interact(GameObject interactor)
    {
        if (interactor == null) return;

        // Require the interactor to have NewClimbing component
        if (interactor.TryGetComponent(out NewClimbing reviverPlayer))
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
        if (deathCamera != null)
        {
            deathCamera.SetActive(false);
        }

        NetworkObject playerNetObj = null;

        if (NetworkManager.Singleton != null)
        {
            if (playerNetObjectId != 0 && NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerNetObjectId, out var foundObj))
            {
                playerNetObj = foundObj;
            }
            else if (NetworkManager.Singleton.ConnectedClients.TryGetValue(targetClientId, out var client))
            {
                playerNetObj = client.PlayerObject;
            }
        }

        if (playerNetObj != null)
        {
            playerNetObj.gameObject.SetActive(true);

            if (playerNetObj.TryGetComponent(out NewClimbing targetPlayer))
            {
                targetPlayer.OnRevived(revivePosition);
            }
        }
    }
}