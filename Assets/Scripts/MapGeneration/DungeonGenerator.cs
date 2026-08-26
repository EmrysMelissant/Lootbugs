using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    [Header("Generator Settings")]
    public GameObject[] roomPrefabs;
    public int maxRooms = 10;
    public string doorTag = "doorTag";
    public NavMeshSurface surface;

    private List<Transform> openAnchors = new List<Transform>();
    private List<GameObject> spawnedRooms = new List<GameObject>();

    void Start()
    {
        GenerateMap();
        surface.BuildNavMesh();
    }

    public void GenerateMap()
    {
        if (roomPrefabs.Length == 0) return;

        // 1. Spawn the initial starter room
        GameObject startRoom = Instantiate(roomPrefabs[Random.Range(0, roomPrefabs.Length)], Vector3.zero, Quaternion.identity);
        spawnedRooms.Add(startRoom);
        AddAnchorsFromRoom(startRoom);

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

    private bool TryAttachRoom(GameObject roomPrefab, Transform targetAnchor)
    {
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

        // Simple overlap check (Optional bounds validation can be added here)
        
        // Add remaining open anchors to the global list
        newAnchors.Remove(newRoomAnchor);
        openAnchors.AddRange(newAnchors);
        spawnedRooms.Add(newRoom);

        return true;
    }

    private void AddAnchorsFromRoom(GameObject room)
    {
        openAnchors.AddRange(GetAnchors(room));
    }

    private List<Transform> GetAnchors(GameObject room)
    {
        List<Transform> anchors = new List<Transform>();
        foreach (Transform child in room.GetComponentsInChildren<Transform>())
        {
            if (child.CompareTag(doorTag))
            {
                anchors.Add(child);
            }
        }
        return anchors;
    }
}