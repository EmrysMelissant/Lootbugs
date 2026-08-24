using UnityEngine;
using Unity.Netcode;

public class Scoring : NetworkBehaviour
{
    private int totalScore = 0;

    public int TotalScore => totalScore;
    public static event System.Action<int> OnScoreUpdated;

    private void OnTriggerEnter(Collider other)
    {
        // Only run scoring logic on the server to prevent duplicate score triggers in multiplayer
        if (!IsServer) return;

        if (other.CompareTag("Item") && other.TryGetComponent<Item>(out Item item))
        {
            int itemPoints = item.NetPoints.Value;
            totalScore += itemPoints;

            // Record score in HighScoreManager
            if (HighScoreManager.Instance != null)
            {
                HighScoreManager.Instance.AddScore(totalScore, "Local Player");
            }

            NotifyScoreClientRpc(totalScore);

            // Find all active player components in the scene
            NewClimbing[] players = FindObjectsByType<NewClimbing>(FindObjectsSortMode.None);

            // Award money to every player
            foreach (NewClimbing player in players)
            {
                player.Money += itemPoints * player.gainMultiplier;
            }

            // Despawn networked object across clients, or destroy if local
            if (other.TryGetComponent<NetworkObject>(out NetworkObject netObj))
            {
                netObj.Despawn();
            }
            else
            {
                Destroy(other.gameObject);
            }
        }
    }

    [ClientRpc]
    private void NotifyScoreClientRpc(int newScore)
    {
        totalScore = newScore;
        OnScoreUpdated?.Invoke(newScore);
    }
}
