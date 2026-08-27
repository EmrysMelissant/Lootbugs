using UnityEngine;
using Unity.Netcode;

public class Scoring : NetworkBehaviour
{
    private int totalScore = 0;

    public int TotalScore => totalScore;
    public static event System.Action<int> OnScoreUpdated;

    private void OnTriggerEnter(Collider other)
    {
        // Only run scoring and kill logic on the server to prevent duplicate execution
        if (!IsServer) return;

        // Kill any player entering the scoring area
        NewClimbing player = other.GetComponentInParent<NewClimbing>();
        if (player != null && player.IsAlive)
        {
            player.Die();
            return;
        }

        Item item = other.GetComponentInParent<Item>();
        if (item != null)
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
            foreach (NewClimbing p in players)
            {
                p.Money += itemPoints * p.gainMultiplier;
            }

            // Despawn networked object across clients, or destroy if local
            if (item.TryGetComponent<NetworkObject>(out NetworkObject netObj) && netObj.IsSpawned)
            {
                netObj.Despawn();
            }
            else
            {
                Destroy(item.gameObject);
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
