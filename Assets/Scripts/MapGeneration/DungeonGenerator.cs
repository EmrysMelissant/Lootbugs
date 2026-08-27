using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

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

    private readonly List<Transform> openAnchors = new List<Transform>();
    private readonly List<GameObject> spawnedRooms = new List<GameObject>();

    private void Start()
    {
        GenerateMap();

        if (surface != null)
        {
            surface.BuildNavMesh();
        }
    }

    public void GenerateMap()
    {
        openAnchors.Clear();
        spawnedRooms.Clear();

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

        // 2. Loop to attach remaining rooms
        int attempts = 0;
        while (spawnedRooms.Count < maxRooms && openAnchors.Count > 0 && attempts < 100)
        {
            attempts++;

            // Pick a random open anchor on the existing map
            Transform currentAnchor = openAnchors[Random.Range(0, openAnchors.Count)];

            // Pick a random room prefab to spawn
            GameObject prefabToSpawn = roomPrefabs[Random.Range(0, roomPrefabs.Length)];

            // Try to attach the new room to the current anchor
            if (TryAttachRoom(prefabToSpawn, currentAnchor))
            {
                openAnchors.Remove(currentAnchor);
            }
        }
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

        // Temporary instance to manipulate transforms
        GameObject newRoom = Instantiate(roomPrefab);
        List<Transform> newAnchors = GetAnchors(newRoom);

        if (newAnchors.Count == 0)
        {
            Destroy(newRoom);
            return false;
        }

        // Pick an anchor on the incoming room to connect with
        Transform newRoomAnchor = newAnchors[Random.Range(0, newAnchors.Count)];

        // Calculate required rotation: turn incoming room so anchor's forward vector faces opposite target anchor
        Vector3 lookDir = -targetAnchor.forward;
        if (lookDir.sqrMagnitude > 0.0001f)
        {
            Vector3 upDir = Mathf.Abs(Vector3.Dot(lookDir.normalized, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;
            Quaternion targetRotation = Quaternion.LookRotation(lookDir, upDir);
            Quaternion rotationOffset = targetRotation * Quaternion.Inverse(newRoomAnchor.localRotation);
            newRoom.transform.rotation = rotationOffset;
        }

        // Align position: move new room so the chosen anchor overlaps the target anchor exactly
        Vector3 positionOffset = targetAnchor.position - newRoomAnchor.position;
        newRoom.transform.position += positionOffset;

        // Add remaining open anchors to the global list
        newAnchors.Remove(newRoomAnchor);
        openAnchors.AddRange(newAnchors);
        spawnedRooms.Add(newRoom);

        return true;
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