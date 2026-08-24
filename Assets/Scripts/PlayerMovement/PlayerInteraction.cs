using UnityEngine;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactionLayer;
    [SerializeField] private TMP_Text interactionPrompt;
    private Camera playerCamera;

    void Start()
    {
        playerCamera = Camera.main;
    }

    void Update()
    {
        HandleRaycasting();
    }

    private void HandleRaycasting()
    {
        RaycastHit hit;

        // Perform the raycast
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, interactionDistance, interactionLayer, QueryTriggerInteraction.Ignore))
        {
            // Check if the object hit has an IInteractable component
            if (hit.collider.TryGetComponent(out IInteractable interactable))
            {
                if (interactionPrompt != null)
                {
                    interactionPrompt.text = interactable.InteractionText;
                }

                if (Input.GetKeyDown(KeyCode.F))
                {
                    interactable.Interact(gameObject);
                }

                return; // Exit early only when actively looking at an interactable item
            }
        }

        // Clear prompt text if looking at nothing OR looking at a non-interactable object
        if (interactionPrompt != null)
        {
            interactionPrompt.text = "";
        }
    }
}
