using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ItemSpawner : NetworkBehaviour
{
    [Header("Item Prefabs")]
    public List<GameObject> itemsToSpawn;

    [Header("Spawn Area & Limits")]
    [Tooltip("Assign a BoxCollider, SphereCollider, or MeshCollider defining the spawn area.")]
    [SerializeField] private Collider spawnAreaCollider;
    
    [Tooltip("Maximum number of items to spawn.")]
    [SerializeField] private int maxItemsToSpawn = 10;

    [Tooltip("Max attempts to find a point inside non-box colliders.")]
    [SerializeField] private int maxAttemptsPerItem = 10;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        SpawnItems();
    }

    void SpawnItems()
    {
        if (itemsToSpawn == null || itemsToSpawn.Count == 0)
        {
            Debug.LogWarning("No items to spawn assigned!");
            return;
        }

        if (spawnAreaCollider == null)
        {
            Debug.LogError("Spawn Area Collider is missing on ItemSpawner!");
            return;
        }

        for (int i = 0; i < maxItemsToSpawn; i++)
        {
            Vector3 spawnPoint = GetRandomPointInCollider(spawnAreaCollider);

            int randomIndex = Random.Range(0, itemsToSpawn.Count);
            GameObject prefabToSpawn = itemsToSpawn[randomIndex];


            GameObject obj = Instantiate(prefabToSpawn, spawnPoint, Quaternion.identity);
            if (obj.TryGetComponent<NetworkObject>(out NetworkObject netObj))
            {
                netObj.Spawn();
            }
            else
            {
                Debug.LogWarning($"{prefabToSpawn.name} is missing a NetworkObject component!");
            }
        }
    }
    private Vector3 GetRandomPointInCollider(Collider col)
    {
        Bounds bounds = col.bounds;

        for (int attempt = 0; attempt < maxAttemptsPerItem; attempt++)
        {
            Vector3 randomPoint = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y),
                Random.Range(bounds.min.z, bounds.max.z)
            );

            Vector3 closestPoint = col.ClosestPoint(randomPoint);
            if (closestPoint == randomPoint)
            {
                return randomPoint;
            }
        }

        return bounds.center;
    }
}