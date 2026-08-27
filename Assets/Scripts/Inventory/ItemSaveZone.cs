using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class ItemSaveZone : MonoBehaviour
{
    [System.Serializable]
    public class SavedItemData
    {
        public GameObject itemObject;
        public Vector3 relativePosition;
        public Quaternion relativeRotation;
    }

    [Header("Settings")]
    [Tooltip("Tags of items that should be saved across scenes. Leave empty to allow all tagged objects.")]
    [SerializeField] private List<string> targetTags = new List<string> { "Item" };

    [Header("Tracked Items (Read Only)")]
    [SerializeField] private List<SavedItemData> savedItems = new List<SavedItemData>();

    public static ItemSaveZone Instance { get; private set; }

    private void Awake()
    {
        // Singleton pattern: preserve ONE persistent container across scenes
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            // Another container already exists (persisted from previous scene).
            // Sync this scene's container transform position to the persistent instance and destroy this duplicate
            Instance.AlignToNewSceneContainer(transform);
            Destroy(gameObject);
            return;
        }

        // Ensure collider is set to trigger
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void AlignToNewSceneContainer(Transform sceneContainerTransform)
    {
        if (sceneContainerTransform == null) return;

        transform.position = sceneContainerTransform.position;
        transform.rotation = sceneContainerTransform.rotation;
        transform.localScale = sceneContainerTransform.localScale;

        RestoreItemPositions();
    }

    private void OnTriggerEnter(Collider other)
    {
        Item itemComp = other.GetComponentInParent<Item>();
        GameObject itemObj = itemComp != null ? itemComp.gameObject : other.gameObject;

        if (IsTargetItem(itemObj) && !IsItemTracked(itemObj))
        {
            SaveItem(itemObj);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Item itemComp = other.GetComponentInParent<Item>();
        GameObject itemObj = itemComp != null ? itemComp.gameObject : other.gameObject;

        if (IsItemTracked(itemObj))
        {
            RemoveItem(itemObj);
        }
    }

    /// <summary>
    /// Saves the item, stores relative offset, and makes it persistent.
    /// </summary>
    public void SaveItem(GameObject item)
    {
        if (item == null) return;

        // Parent item to container to establish local transform relationship
        item.transform.SetParent(transform);
        DontDestroyOnLoad(item);

        SavedItemData data = new SavedItemData
        {
            itemObject = item,
            relativePosition = item.transform.localPosition,
            relativeRotation = item.transform.localRotation
        };

        savedItems.Add(data);
    }

    /// <summary>
    /// Removes the item from tracking when it leaves the container.
    /// </summary>
    public void RemoveItem(GameObject item)
    {
        if (item == null) return;

        savedItems.RemoveAll(data => data.itemObject == item);

        // Unparent and assign to current active scene
        item.transform.SetParent(null);
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.isLoaded)
        {
            SceneManager.MoveGameObjectToScene(item, activeScene);
        }

        if (item.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb.WakeUp();
        }
    }

    /// <summary>
    /// Called automatically whenever a new scene finishes loading.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Try to find any scene ItemSaveZone in the newly loaded scene to align to
        ItemSaveZone[] containers = FindObjectsByType<ItemSaveZone>(FindObjectsSortMode.None);
        foreach (var c in containers)
        {
            if (c != this && c != null)
            {
                AlignToNewSceneContainer(c.transform);
                Destroy(c.gameObject);
                return;
            }
        }

        RestoreItemPositions();
    }

    /// <summary>
    /// Restores every saved item to its relative position & rotation within this container.
    /// </summary>
    public void RestoreItemPositions()
    {
        savedItems.RemoveAll(data => data.itemObject == null);

        foreach (SavedItemData data in savedItems)
        {
            if (data.itemObject == null) continue;

            // Re-apply local offset relative to container
            data.itemObject.transform.SetParent(transform);
            data.itemObject.transform.localPosition = data.relativePosition;
            data.itemObject.transform.localRotation = data.relativeRotation;

            // Reset velocity and wake up Rigidbody so triggers fire in the new scene
            if (data.itemObject.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.WakeUp();
            }
        }
    }

    public bool IsItemTracked(GameObject obj)
    {
        if (obj == null) return false;
        return savedItems.Exists(data => data.itemObject == obj);
    }

    private bool IsTargetItem(GameObject obj)
    {
        if (obj == null) return false;
        if (targetTags == null || targetTags.Count == 0) return true;
        return targetTags.Contains(obj.tag) || obj.GetComponentInParent<Item>() != null;
    }
}

// Backward compatibility alias for any existing serialized references
public class PersistentItemContainer : ItemSaveZone
{
}