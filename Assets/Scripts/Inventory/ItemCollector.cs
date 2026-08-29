using UnityEngine;
using System.Collections.Generic;

public class ItemCollector : MonoBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    [SerializeField] private string interactText = "F";

    [Header("Suction Settings")]
    [Tooltip("The position items will be pulled toward. Defaults to this GameObject if left empty.")]
    [SerializeField] private Transform targetTransform;
    [SerializeField] private float pullSpeed = 8f;
    [SerializeField] private bool disableGravityWhilePulling = true;

    [Header("Detection Settings")]
    [Tooltip("Layer mask used to detect items when scanning area.")]
    [SerializeField] private LayerMask itemLayer = ~0;

    private bool startPulling = false;
    private readonly HashSet<Item> collectedItems = new HashSet<Item>();
    private readonly Collider[] overlapBuffer = new Collider[64];
    private Collider triggerCollider;

    public string InteractionText => interactText;
    public bool IsPulling => startPulling;

    private void Awake()
    {
        if (targetTransform == null)
        {
            targetTransform = transform;
        }

        // Cache the trigger collider
        Collider[] colliders = GetComponents<Collider>();
        foreach (Collider col in colliders)
        {
            if (col.isTrigger)
            {
                triggerCollider = col;
                break;
            }
        }
    }

    private void Start()
    {
        ScanForItemsInZone();
    }

    private void OnEnable()
    {
        ScanForItemsInZone();
    }

    public void Interact(GameObject interactor)
    {
        // Refresh items currently in the collection zone
        ScanForItemsInZone();

        // Toggle pulling state ONCE
        startPulling = !startPulling;

        Debug.Log($"[ItemCollector] {(startPulling ? "Started" : "Stopped")} pulling by {interactor.name}. Items tracked: {collectedItems.Count}");
    }

    public void ScanForItemsInZone()
    {
        int hitCount = 0;
        if (triggerCollider != null)
        {
            Bounds b = triggerCollider.bounds;
            hitCount = Physics.OverlapBoxNonAlloc(b.center, b.extents, overlapBuffer, transform.rotation, itemLayer, QueryTriggerInteraction.Collide);
        }
        else
        {
            hitCount = Physics.OverlapSphereNonAlloc(transform.position, 10f, overlapBuffer, itemLayer, QueryTriggerInteraction.Collide);
        }

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = overlapBuffer[i];
            if (hit == null) continue;

            Item item = hit.GetComponentInParent<Item>();
            if (item != null && !collectedItems.Contains(item))
            {
                collectedItems.Add(item);
            }
        }

        // Clean up any destroyed items
        collectedItems.RemoveWhere(item => item == null);
    }

    private void FixedUpdate()
    {
        if (!startPulling) return;

        // Clean up destroyed items
        collectedItems.RemoveWhere(item => item == null);

        foreach (Item item in collectedItems)
        {
            if (item != null)
            {
                PullItem(item);
            }
        }
    }

    private void PullItem(Item item)
    {
        if (item == null) return;

        // If item is parented to PersistentItemContainer or anything else, unparent to allow free movement
        if (item.transform.parent != null)
        {
            if (ItemSaveZone.Instance != null && ItemSaveZone.Instance.IsItemTracked(item.gameObject))
            {
                ItemSaveZone.Instance.RemoveItem(item.gameObject);
            }
            else
            {
                item.transform.SetParent(null);
            }
        }

        Vector3 targetPosition = targetTransform.position;
        if (item.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            if (disableGravityWhilePulling)
            {
                rb.useGravity = false;
            }

            rb.WakeUp();
            Vector3 newPosition = Vector3.MoveTowards(rb.position, targetPosition, pullSpeed * Time.fixedDeltaTime);
            rb.MovePosition(newPosition);
        }
        else
        {
            item.transform.position = Vector3.MoveTowards(item.transform.position, targetPosition, pullSpeed * Time.fixedDeltaTime);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Item item = other.GetComponentInParent<Item>();
        if (item != null && !collectedItems.Contains(item))
        {
            collectedItems.Add(item);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Item item = other.GetComponentInParent<Item>();
        if (item != null && collectedItems.Contains(item))
        {
            if (disableGravityWhilePulling && item.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.useGravity = true;
            }

            collectedItems.Remove(item);
        }
    }
}
