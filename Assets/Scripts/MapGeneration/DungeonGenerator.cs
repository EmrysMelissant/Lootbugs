using System.Collections.Generic;
using Unity.AI.Navigation;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class DungeonGenerator : MonoBehaviour
{
    [Header("Starter Room Settings")]
    [Tooltip("An existing room in the scene to start generation from (e.g. RunStart). If assigned, the generator builds out from its door anchors.")]
    [SerializeField] private GameObject staticStartRoom;

    [Tooltip("Optional prefab to instantiate as the fixed starter room if no in-scene static room is assigned.")]
    [SerializeField] private GameObject startRoomPrefab;

    [Header("Generator Settings")]
    [Tooltip("Pool of modular room prefabs to spawn and connect.")]
    [SerializeField] private GameObject[] roomPrefabs;

    [Tooltip("Maximum number of rooms to generate in the map.")]
    [SerializeField] private int maxRooms = 10;

    [Tooltip("Tag used to identify door anchor transforms on rooms.")]
    [SerializeField] private string doorTag = "doorTag";

    [Tooltip("NavMeshSurface component to rebuild after generation.")]
    [SerializeField] private NavMeshSurface surface;

    [Header("Enemy Spawning Settings")]
    [Tooltip("Pool of enemy prefabs to randomly choose from.")]
    [SerializeField] private GameObject[] enemyPrefabs;

    [Tooltip("Tag used to identify enemy spawn points.")]
    [SerializeField] private string enemySpawnTag = "Checkpoint";

    [Tooltip("Alternative name substring used to identify enemy spawn points.")]
    [SerializeField] private string enemySpawnNameSubstring = "checkpoint";

    [Tooltip("Whether to prevent spawning enemies in the starting room.")]
    [SerializeField] private bool excludeStartRoom = true;

    [Tooltip("Number of enemies guaranteed to spawn if sufficient spawn points exist.")]
    [SerializeField] private int guaranteedEnemyCount = 3;

    [Tooltip("Initial probability (0-1) for spawning the first extra enemy beyond guaranteed count.")]
    [SerializeField] [Range(0f, 1f)] private float initialExtraSpawnChance = 0.75f;

    [Tooltip("Decay factor (0-1) multiplied to spawn chance for each consecutive extra enemy.")]
    [SerializeField] [Range(0f, 1f)] private float extraSpawnDecayFactor = 0.5f;

    [Tooltip("Minimum spawn probability floor threshold before terminating extra rolls.")]
    [SerializeField] [Range(0f, 1f)] private float minExtraSpawnChance = 0.05f;

    [Tooltip("Maximum total number of enemies that can spawn.")]
    [SerializeField] private int maxEnemyCount = 15;

    public GameObject StaticStartRoom
    {
        get => staticStartRoom;
        set => staticStartRoom = value;
    }

    public GameObject StartRoomPrefab
    {
        get => startRoomPrefab;
        set => startRoomPrefab = value;
    }

    public GameObject[] RoomPrefabs => roomPrefabs;
    public int MaxRooms => maxRooms;
    public string DoorTag => doorTag;
    public NavMeshSurface Surface => surface;

    public GameObject[] EnemyPrefabs
    {
        get => enemyPrefabs;
        set => enemyPrefabs = value;
    }

    public string EnemySpawnTag => enemySpawnTag;
    public string EnemySpawnNameSubstring => enemySpawnNameSubstring;
    public bool ExcludeStartRoom => excludeStartRoom;
    public int GuaranteedEnemyCount => guaranteedEnemyCount;
    public float InitialExtraSpawnChance => initialExtraSpawnChance;
    public float ExtraSpawnDecayFactor => extraSpawnDecayFactor;
    public float MinExtraSpawnChance => minExtraSpawnChance;
    public int MaxEnemyCount => maxEnemyCount;
    public List<GameObject> SpawnedRooms => spawnedRooms;

    [Header("Overlap Prevention Settings")]
    [Tooltip("If true, checks candidate rooms against existing rooms and rejects overlapping placements.")]
    [SerializeField] private bool preventRoomOverlaps = true;

    [Tooltip("Padding in meters subtracted from room bounding boxes to allow touching walls/doorways without false collision.")]
    [SerializeField] private float roomOverlapPadding = 0.5f;

    [Tooltip("Maximum generation loop attempts before completing dungeon layout.")]
    [SerializeField] private int maxPlacementAttempts = 250;

    [Header("NavMesh & Doorway Settings")]
    [Tooltip("Whether to generate NavMeshLinks at connected doorways to guarantee seamless pathfinding across room seams.")]
    [SerializeField] private bool generateDoorwayNavMeshLinks = true;

    [Tooltip("Width of the doorway NavMeshLinks.")]
    [SerializeField] private float doorwayLinkWidth = 2.0f;

    private readonly List<Transform> openAnchors = new List<Transform>();
    private readonly List<GameObject> spawnedRooms = new List<GameObject>();
    private readonly List<Transform> connectedAnchors = new List<Transform>();

    private void Start()
    {
        GenerateMap();

        if (surface != null)
        {
            BakeNavMeshThroughDoors();
        }

        SpawnItemsInRooms();
        SpawnEnemies();
    }

    public void SpawnItemsInRooms()
    {
        // Netcode authority check: only the server/host spawns networked items
        bool isNetworkActive = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        if (isNetworkActive && !NetworkManager.Singleton.IsServer)
        {
            return;
        }

        ItemSpawner[] spawners = FindObjectsByType<ItemSpawner>(FindObjectsSortMode.None);
        for (int i = 0; i < spawners.Length; i++)
        {
            ItemSpawner spawner = spawners[i];
            if (spawner != null)
            {
                spawner.SpawnItems();
            }
        }
    }

    public void GenerateMap()
    {
        openAnchors.Clear();
        spawnedRooms.Clear();
        connectedAnchors.Clear();

        // 1. Resolve and initialize the starting room
        GameObject startRoom = GetOrSpawnStartRoom();
        if (startRoom == null)
        {
            Debug.LogWarning("[DungeonGenerator] No starting room or room prefabs available to generate map.");
            return;
        }

        spawnedRooms.Add(startRoom);
        AddAnchorsFromRoom(startRoom);

        if (roomPrefabs == null || roomPrefabs.Length == 0)
        {
            Debug.LogWarning("[DungeonGenerator] Room prefabs array is empty. Only start room was initialized.");
            return;
        }

        // 2. Loop to attach remaining rooms with overlap prevention
        int attempts = 0;
        while (spawnedRooms.Count < maxRooms && openAnchors.Count > 0 && attempts < maxPlacementAttempts)
        {
            attempts++;

            // Pick a random open anchor on the existing map
            int anchorIndex = Random.Range(0, openAnchors.Count);
            Transform currentAnchor = openAnchors[anchorIndex];

            // Shuffle candidate prefabs to attempt multiple variations at this anchor
            List<GameObject> candidatePrefabs = new List<GameObject>(roomPrefabs);
            for (int i = candidatePrefabs.Count - 1; i > 0; i--)
            {
                int r = Random.Range(0, i + 1);
                GameObject temp = candidatePrefabs[i];
                candidatePrefabs[i] = candidatePrefabs[r];
                candidatePrefabs[r] = temp;
            }

            bool attached = false;
            for (int p = 0; p < candidatePrefabs.Count; p++)
            {
                if (TryAttachRoom(candidatePrefabs[p], currentAnchor))
                {
                    openAnchors.Remove(currentAnchor);
                    attached = true;
                    break;
                }
            }

            // If none of the prefabs fit without overlapping, close this anchor so other branches expand
            if (!attached)
            {
                openAnchors.Remove(currentAnchor);
            }
        }
    }

    public void SpawnEnemies()
    {
        // Netcode authority check: only the server/host spawns networked enemies
        bool isNetworkActive = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        if (isNetworkActive && !NetworkManager.Singleton.IsServer)
        {
            return;
        }

        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogWarning("[DungeonGenerator] No enemy prefabs assigned for spawning.");
            return;
        }

        // 1. Gather all spawn points from rooms
        List<Transform> candidateSpawnPoints = GetEnemySpawnPoints();
        if (candidateSpawnPoints.Count == 0)
        {
            Debug.LogWarning("[DungeonGenerator] No enemy spawn points found across generated rooms.");
            return;
        }

        // 2. Calculate how many enemies to spawn: 3 guaranteed + geometrically decreasing chance for extras
        int enemiesToSpawn = guaranteedEnemyCount;
        float currentChance = initialExtraSpawnChance;

        while (enemiesToSpawn < maxEnemyCount && currentChance >= minExtraSpawnChance)
        {
            if (Random.value <= currentChance)
            {
                enemiesToSpawn++;
                currentChance *= extraSpawnDecayFactor;
            }
            else
            {
                break;
            }
        }

        // Cap to available spawn points so each spawn point is used at most once
        int finalSpawnCount = Mathf.Min(enemiesToSpawn, candidateSpawnPoints.Count);

        // 3. Shuffle candidate spawn points (Fisher-Yates)
        for (int i = candidateSpawnPoints.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            Transform temp = candidateSpawnPoints[i];
            candidateSpawnPoints[i] = candidateSpawnPoints[randomIndex];
            candidateSpawnPoints[randomIndex] = temp;
        }

        // 4. Spawn enemies
        for (int i = 0; i < finalSpawnCount; i++)
        {
            Transform spawnPoint = candidateSpawnPoints[i];
            if (spawnPoint == null) continue;

            GameObject enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            if (enemyPrefab == null) continue;

            Vector3 spawnPosition = spawnPoint.position;
            Quaternion spawnRotation = spawnPoint.rotation;

            // Sample nearest valid NavMesh position within 3.0 units
            if (NavMesh.SamplePosition(spawnPosition, out NavMeshHit hit, 3.0f, NavMesh.AllAreas))
            {
                spawnPosition = hit.position;
            }

            GameObject spawnedEnemy = Instantiate(enemyPrefab, spawnPosition, spawnRotation);

            if (spawnedEnemy != null)
            {
                if (spawnedEnemy.TryGetComponent<NavMeshAgent>(out NavMeshAgent agent))
                {
                    if (agent != null && agent.isOnNavMesh)
                    {
                        agent.Warp(spawnPosition);
                    }
                }

                if (isNetworkActive && NetworkManager.Singleton.IsServer)
                {
                    if (spawnedEnemy.TryGetComponent<NetworkObject>(out NetworkObject netObj))
                    {
                        if (netObj != null)
                        {
                            netObj.Spawn();
                        }
                    }
                }
            }
        }

        // 5. Refresh checkpoint references for enemy patrol behavior
        GameEnviroment.Refresh();
    }

    private List<Transform> GetEnemySpawnPoints()
    {
        List<Transform> spawnPoints = new List<Transform>();
        GameObject startRoom = (spawnedRooms.Count > 0) ? spawnedRooms[0] : null;

        for (int i = 0; i < spawnedRooms.Count; i++)
        {
            GameObject room = spawnedRooms[i];
            if (room == null) continue;

            // Optionally skip the starter room so enemies don't spawn right next to players
            if (excludeStartRoom && (room == startRoom || room == staticStartRoom))
            {
                continue;
            }

            Transform[] allChildren = room.GetComponentsInChildren<Transform>(true);
            int pointsFoundInRoom = 0;

            for (int j = 0; j < allChildren.Length; j++)
            {
                Transform child = allChildren[j];
                if (child == null || child == room.transform) continue;

                if (IsEnemySpawnPoint(child))
                {
                    if (!spawnPoints.Contains(child))
                    {
                        spawnPoints.Add(child);
                        pointsFoundInRoom++;
                    }
                }
            }

            // Fallback: If a non-starter room has no explicit spawn point transforms, use room center
            if (pointsFoundInRoom == 0)
            {
                spawnPoints.Add(room.transform);
            }
        }

        return spawnPoints;
    }

    private bool IsEnemySpawnPoint(Transform candidate)
    {
        if (candidate == null) return false;

        // 1. Tag comparison
        if (!string.IsNullOrEmpty(enemySpawnTag))
        {
            try
            {
                if (candidate.CompareTag(enemySpawnTag)) return true;
            }
            catch (UnityException) { }
        }

        try
        {
            if (candidate.CompareTag("SpawnPoint") || candidate.CompareTag("Checkpoint")) return true;
        }
        catch (UnityException) { }

        // 2. Name substring comparison
        if (!string.IsNullOrEmpty(enemySpawnNameSubstring) &&
            candidate.name.IndexOf(enemySpawnNameSubstring, System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        return false;
    }

    private GameObject GetOrSpawnStartRoom()
    {
        // 1. Use assigned static scene room
        if (staticStartRoom != null)
        {
            return staticStartRoom;
        }

        // 2. Auto-detect common static start room in the scene if unassigned (e.g. "RunStart")
        GameObject foundStaticRoom = GameObject.Find("RunStart");
        if (foundStaticRoom != null)
        {
            staticStartRoom = foundStaticRoom;
            return staticStartRoom;
        }

        // 3. Spawn designated start room prefab if provided
        if (startRoomPrefab != null)
        {
            return Instantiate(startRoomPrefab, transform.position, transform.rotation);
        }

        // 4. Fallback: instantiate a random room from the room prefabs pool
        if (roomPrefabs != null && roomPrefabs.Length > 0)
        {
            return Instantiate(roomPrefabs[Random.Range(0, roomPrefabs.Length)], transform.position, Quaternion.identity);
        }

        return null;
    }

    private bool TryAttachRoom(GameObject roomPrefab, Transform targetAnchor)
    {
        if (roomPrefab == null || targetAnchor == null) return false;

        // Instantiate candidate room
        GameObject newRoom = Instantiate(roomPrefab);
        List<Transform> newAnchors = GetAnchors(newRoom);

        if (newAnchors.Count == 0)
        {
            Destroy(newRoom);
            return false;
        }

        // Shuffle anchors on the candidate room to test all available doorway connections
        List<Transform> candidateAnchors = new List<Transform>(newAnchors);
        for (int i = candidateAnchors.Count - 1; i > 0; i--)
        {
            int r = Random.Range(0, i + 1);
            Transform temp = candidateAnchors[i];
            candidateAnchors[i] = candidateAnchors[r];
            candidateAnchors[r] = temp;
        }

        bool placedSuccessfully = false;
        Transform chosenAnchor = null;

        for (int a = 0; a < candidateAnchors.Count; a++)
        {
            Transform candidateAnchor = candidateAnchors[a];
            if (candidateAnchor == null) continue;

            // Reset local transform before testing orientation
            newRoom.transform.position = Vector3.zero;
            newRoom.transform.rotation = Quaternion.identity;

            // Align rotation: turn incoming room so anchor's forward vector faces opposite target anchor
            Vector3 lookDir = -targetAnchor.forward;
            if (lookDir.sqrMagnitude > 0.0001f)
            {
                Vector3 upDir = Mathf.Abs(Vector3.Dot(lookDir.normalized, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;
                Quaternion targetRotation = Quaternion.LookRotation(lookDir, upDir);
                Quaternion rotationOffset = targetRotation * Quaternion.Inverse(candidateAnchor.localRotation);
                newRoom.transform.rotation = rotationOffset;
            }

            // Align position: move new room so the chosen anchor overlaps the target anchor exactly
            Vector3 positionOffset = targetAnchor.position - candidateAnchor.position;
            newRoom.transform.position += positionOffset;

            // Verify room does not overlap any already spawned rooms
            if (!IsRoomOverlapping(newRoom))
            {
                placedSuccessfully = true;
                chosenAnchor = candidateAnchor;
                break;
            }
        }

        if (!placedSuccessfully)
        {
            // All anchor orientations for this room overlapped, destroy candidate
            Destroy(newRoom);
            return false;
        }

        // Record connection between anchors
        connectedAnchors.Add(targetAnchor);
        connectedAnchors.Add(chosenAnchor);

        // Add remaining open anchors to the global list
        newAnchors.Remove(chosenAnchor);
        openAnchors.AddRange(newAnchors);
        spawnedRooms.Add(newRoom);

        return true;
    }

    private bool IsRoomOverlapping(GameObject candidateRoom)
    {
        if (!preventRoomOverlaps || candidateRoom == null) return false;

        Physics.SyncTransforms();

        Bounds candidateBounds = CalculateRoomBounds(candidateRoom);

        // Subtract overlap padding on horizontal axes to allow adjacent rooms to touch at doorways
        Vector3 shrinkVector = new Vector3(roomOverlapPadding * 2f, Mathf.Min(roomOverlapPadding, 0.25f) * 2f, roomOverlapPadding * 2f);
        if (candidateBounds.size.x > shrinkVector.x && candidateBounds.size.y > shrinkVector.y && candidateBounds.size.z > shrinkVector.z)
        {
            candidateBounds.size -= shrinkVector;
        }

        for (int i = 0; i < spawnedRooms.Count; i++)
        {
            GameObject existingRoom = spawnedRooms[i];
            if (existingRoom == null || existingRoom == candidateRoom) continue;

            Bounds existingBounds = CalculateRoomBounds(existingRoom);
            if (existingBounds.size.x > shrinkVector.x && existingBounds.size.y > shrinkVector.y && existingBounds.size.z > shrinkVector.z)
            {
                existingBounds.size -= shrinkVector;
            }

            if (candidateBounds.Intersects(existingBounds))
            {
                return true; // Overlap detected
            }
        }

        return false;
    }

    public static Bounds CalculateRoomBounds(GameObject room)
    {
        Bounds bounds = new Bounds();
        bool hasBounds = false;

        Collider[] colliders = room.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider col = colliders[i];
            if (col == null || col.isTrigger) continue;

            if (!hasBounds)
            {
                bounds = col.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(col.bounds);
            }
        }

        if (!hasBounds)
        {
            Renderer[] renderers = room.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer rend = renderers[i];
                if (rend == null) continue;

                if (!hasBounds)
                {
                    bounds = rend.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(rend.bounds);
                }
            }
        }

        if (!hasBounds)
        {
            bounds = new Bounds(room.transform.position, new Vector3(8f, 4f, 8f));
        }

        return bounds;
    }

    private void BakeNavMeshThroughDoors()
    {
        if (surface == null) return;

        // 1. Find all SlidingDoor scripts and configure NavMeshModifier exclusions
        SlidingDoor[] doors = FindObjectsByType<SlidingDoor>(FindObjectsSortMode.None);
        List<Collider> temporarilyDisabledColliders = new List<Collider>();

        for (int i = 0; i < doors.Length; i++)
        {
            SlidingDoor door = doors[i];
            if (door == null) continue;

            door.ConfigureNavMeshExclusion();

            // Temporarily disable non-trigger physical door panel colliders during bake
            Collider[] colliders = door.GetComponentsInChildren<Collider>(true);
            for (int c = 0; c < colliders.Length; c++)
            {
                Collider col = colliders[c];
                if (col != null && !col.isTrigger && col.enabled)
                {
                    col.enabled = false;
                    temporarilyDisabledColliders.Add(col);
                }
            }
        }

        // 2. Build NavMesh with fully open doorway openings
        surface.BuildNavMesh();

        // 3. Re-enable door colliders
        for (int i = 0; i < temporarilyDisabledColliders.Count; i++)
        {
            if (temporarilyDisabledColliders[i] != null)
            {
                temporarilyDisabledColliders[i].enabled = true;
            }
        }

        // 4. Create bidirectional NavMeshLinks at connected doorways to guarantee seam connectivity
        if (generateDoorwayNavMeshLinks)
        {
            CreateDoorwayNavMeshLinks();
        }
    }

    private void CreateDoorwayNavMeshLinks()
    {
        for (int i = 0; i < connectedAnchors.Count; i++)
        {
            Transform anchor = connectedAnchors[i];
            if (anchor == null) continue;

            NavMeshLink existingLink = anchor.GetComponent<NavMeshLink>();
            if (existingLink != null) continue;

            NavMeshLink link = anchor.gameObject.AddComponent<NavMeshLink>();
            link.agentTypeID = 0; // Default humanoid agent
            link.width = doorwayLinkWidth;
            link.bidirectional = true;
            link.startPoint = new Vector3(0f, 0f, -0.75f);
            link.endPoint = new Vector3(0f, 0f, 0.75f);
            link.area = 0; // Walkable
        }
    }

    private void AddAnchorsFromRoom(GameObject room)
    {
        if (room == null) return;
        openAnchors.AddRange(GetAnchors(room));
    }

    private List<Transform> GetAnchors(GameObject room)
    {
        List<Transform> anchors = new List<Transform>();
        if (room == null) return anchors;

        foreach (Transform child in room.GetComponentsInChildren<Transform>())
        {
            if (child != null && child.CompareTag(doorTag))
            {
                anchors.Add(child);
            }
        }
        return anchors;
    }
}