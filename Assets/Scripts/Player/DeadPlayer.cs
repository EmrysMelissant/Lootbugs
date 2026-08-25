using UnityEngine;
using Unity.Netcode;

public class DeadPlayer : NetworkBehaviour, IInteractable
{
    [SerializeField] private string interactText = "E to Resurrect";
    public string InteractionText => interactText;

    // Track which player this death marker belongs to
    private ulong ownerClientId;

    public void Initialize(ulong clientId)
    {
        ownerClientId = clientId;
    }

    public void Interact(GameObject interactor)
    {
        // Require the interactor to have NewClimbing component
        if (interactor.TryGetComponent(out NewClimbing reviverPlayer))
        {
            // Calculate health sacrifice costs
            float healthCost = 20f;
            
            if (reviverPlayer.Health > healthCost)
            {
                reviverPlayer.Health -= healthCost;
                RequestReviveServerRpc(ownerClientId);
            }
            else
            {
                // Warn or prevent revive if cost would kill reviver
                Debug.LogWarning("Not enough health to resurrect team member!");
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestReviveServerRpc(ulong targetClientId)
    {
        // Find player object on the server and trigger revive
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(targetClientId, out var client))
        {
            if (client.PlayerObject.TryGetComponent(out NewClimbing targetPlayer))
            {
                targetPlayer.ReviveClientRpc(transform.position);
            }
        }

        // Despawn death item marker on server
        GetComponent<NetworkObject>().Despawn();
    }
}