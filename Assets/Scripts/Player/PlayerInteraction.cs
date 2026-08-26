using UnityEngine;
using TMPro;
using Unity.Netcode;

public class PlayerInteraction : NetworkBehaviour
{
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactionLayer;
    [SerializeField] private TMP_Text interactionPrompt;
    private Camera playerCamera;

    private void Start()
    {
        if (!IsOwner) return;

        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null) playerCamera = Camera.main;
            if (playerCamera == null) return;
        }

        HandleRaycasting();
    }

    private void HandleRaycasting()
    {
        RaycastHit hit;

        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, interactionDistance, interactionLayer, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.TryGetComponent(out IInteractable interactable))
            {
                if (interactionPrompt != null)
                {
                    interactionPrompt.text = interactable.InteractionText;
                }

                bool interactPressed = MobileMotionManager.Instance != null ? MobileMotionManager.Instance.IsInteractTriggered(KeyCode.F) : Input.GetKeyDown(KeyCode.F);
                if (interactPressed)
                {
                    interactable.Interact(gameObject);
                }

                return;
            }
        }

        if (interactionPrompt != null)
        {
            interactionPrompt.text = "";
        }
    }
}
