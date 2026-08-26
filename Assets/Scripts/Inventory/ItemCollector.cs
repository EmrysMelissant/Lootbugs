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
    private bool disableGravityWhilePulling = true;
    private bool startPulling = false;

    private List<Item> collectedItems = new List<Item>();

    public string InteractionText => interactText;

    private void Awake()
    {
        
        if (targetTransform == null)
        {
            targetTransform = transform;
        }
    }

    public void Interact(GameObject interactor)
    {
        for (int i = 0; i < collectedItems.Count; i++)
        {
            if (collectedItems[i] != null)
            {
                startPulling = !startPulling;
            }
        }
    }

    private void FixedUpdate()
    {
        if (!startPulling) return;
        for (int i = collectedItems.Count - 1; i >= 0; i--)
        {
            Item item = collectedItems[i];

            if (item == null)
            {
                collectedItems.RemoveAt(i);
                continue;
            }

            PullItem(item);
        }
    }

    private void PullItem(Item item)
    {
        Vector3 targetPosition = targetTransform.position;
        if (item.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            if (disableGravityWhilePulling)
            {
                rb.useGravity = false;
            }

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
        if (other.CompareTag("Item"))
        {
            Item item = other.GetComponent<Item>();
            if (item != null && !collectedItems.Contains(item))
            {
                collectedItems.Add(item);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Item"))
        {
            Item item = other.GetComponent<Item>();
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
}
