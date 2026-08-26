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

    public void Initialize(ulong clientId)
    {
        if (IsServer)
        {
            targetOwnerClientId.Value = clientId;
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