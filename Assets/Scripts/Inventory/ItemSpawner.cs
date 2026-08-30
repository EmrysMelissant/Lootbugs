using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class ItemSpawner : NetworkBehaviour
{
    [Header("Item Prefabs")]
    [Tooltip("List of item prefabs that can be spawned in this room.")]
    [SerializeField] private List<GameObject> itemsToSpawn = new List<GameObject>();

    [Header("Spawn Area & Limits")]
    [Tooltip("Assign a BoxCollider, SphereCollider, or MeshCollider defining the spawn area.")]
    [SerializeField] private Collider spawnAreaCollider;
    
    [Tooltip("Maximum number of items to spawn.")]
    [SerializeField] private int maxItemsToSpawn = 10;

    [Tooltip("Max attempts to find a point inside non-box colliders.")]
    [SerializeField] private int maxAttemptsPerItem = 10;

    [Header("NavMesh Settings")]
    [Tooltip("Whether to snap spawned item positions to the nearest baked NavMesh floor point.")]
    [SerializeField] private bool snapToNavMesh = true;

    [Tooltip("Max distance to sample NavMesh for a valid floor position.")]
    [SerializeField] private float maxNavMeshDistance = 5.0f;

    [Tooltip("Vertical offset above the NavMesh surface to spawn items.")]
    [SerializeField] private float verticalOffset = 0.1f;

    public List<GameObject> ItemsToSpawn => itemsToSpawn;
    public Collider SpawnAreaCollider => spawnAreaCollider;
    public int MaxItemsToSpawn => maxItemsToSpawn;
    public bool SnapToNavMesh => snapToNavMesh;

    private bool hasSpawned = false;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        // If DungeonGenerator is present in the scene, it will explicitly invoke SpawnItems() after NavMesh generation.
        // If no DungeonGenerator exists (e.g. standalone test room scene), spawn now.
        DungeonGenerator generator = FindFirstObjectByType<DungeonGenerator>();
        if (generator == null && !hasSpawned)
        {
            SpawnItems();
        }
    }

    public void SpawnItems()
    {
        if (hasSpawned) return;

        bool isNetworkActive = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        if (isNetworkActive && !NetworkManager.Singleton.IsServer)
        {
            return;
        }

        if (itemsToSpawn == null || itemsToSpawn.Count == 0)
        {
            Debug.LogWarning($"[ItemSpawner] No items to spawn assigned on '{gameObject.name}'!");
            return;
        }

        if (spawnAreaCollider == null)
        {
            Debug.LogError($"[ItemSpawner] Spawn Area Collider is missing on '{gameObject.name}'!");
            return;
        }

        hasSpawned = true;

        for (int i = 0; i < maxItemsToSpawn; i++)
        {
            Vector3 spawnPoint = GetRandomPointInCollider(spawnAreaCollider);

            int randomIndex = Random.Range(0, itemsToSpawn.Count);
            GameObject prefabToSpawn = itemsToSpawn[randomIndex];
            if (prefabToSpawn == null) continue;

            GameObject obj = Instantiate(prefabToSpawn, spawnPoint, Quaternion.identity);
            if (obj == null) continue;

            if (isNetworkActive && NetworkManager.Singleton.IsServer)
            {
                if (obj.TryGetComponent<NetworkObject>(out NetworkObject netObj))
                {
                    if (netObj != null)
                    {
                        netObj.Spawn();
                    }
                }
                else
                {
                    Debug.LogWarning($"[ItemSpawner] '{prefabToSpawn.name}' is missing a NetworkObject component!");
                }
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
                if (snapToNavMesh && NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, maxNavMeshDistance, NavMesh.AllAreas))
                {
                    return hit.position + Vector3.up * verticalOffset;
                }
                return randomPoint;
            }
        }

        Vector3 fallback = bounds.center;
        if (snapToNavMesh && NavMesh.SamplePosition(fallback, out NavMeshHit fallbackHit, maxNavMeshDistance, NavMesh.AllAreas))
        {
            return fallbackHit.position + Vector3.up * verticalOffset;
        }
        return fallback;
    }
}