using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class PersistentItemContainer : MonoBehaviour
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

    private void Awake()
    {
        // Keep container alive across scene loads
        DontDestroyOnLoad(gameObject);

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

    private void OnTriggerEnter(Collider other)
    {
        GameObject item = other.gameObject;

        if (IsTargetItem(item) && !IsItemTracked(item))
        {
            SaveItem(item);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        GameObject item = other.gameObject;

        if (IsItemTracked(item))
        {
            RemoveItem(item);
        }
    }

    /// <summary>
    /// Saves the item, stores relative offset, and makes it persistent.
    /// </summary>
    public void SaveItem(GameObject item)
    {
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
        savedItems.RemoveAll(data => data.itemObject == item);

        // Unparent and assign to current active scene
        item.transform.SetParent(null);
        SceneManager.MoveGameObjectToScene(item, SceneManager.GetActiveScene());
    }

    /// <summary>
    /// Called automatically whenever a new scene finishes loading.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RestoreItemPositions();
    }

    /// <summary>
    /// Restores every saved item to its relative position & rotation within this container.
    /// </summary>
    public void RestoreItemPositions()
    {
        // Clean up any destroyed objects from the list first
        savedItems.RemoveAll(data => data.itemObject == null);

        foreach (SavedItemData data in savedItems)
        {
            // Reset velocity if the item has a Rigidbody
            if (data.itemObject.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // Re-apply local offset relative to container
            data.itemObject.transform.SetParent(transform);
            data.itemObject.transform.localPosition = data.relativePosition;
            data.itemObject.transform.localRotation = data.relativeRotation;
        }
    }

    private bool IsItemTracked(GameObject obj)
    {
        return savedItems.Exists(data => data.itemObject == obj);
    }

    private bool IsTargetItem(GameObject obj)
    {
        if (targetTags == null || targetTags.Count == 0) return true;
        return targetTags.Contains(obj.tag);
    }
}