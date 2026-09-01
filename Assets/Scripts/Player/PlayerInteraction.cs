using UnityEngine;
using TMPro;
using Unity.Netcode;

public class PlayerInteraction : NetworkBehaviour
{
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactionLayer;
    [SerializeField] private TMP_Text interactionPrompt;
    public KeyCode interactKey = KeyCode.E;
    private Camera playerCamera;

    private IInteractable currentInteractable;
    public bool HasTargetInteractable => currentInteractable != null;
    public string CurrentInteractableText => currentInteractable != null ? currentInteractable.InteractionText : "";

    private void Awake()
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.text = "";
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner)
        {
            if (interactionPrompt != null)
            {
                interactionPrompt.text = "";
                interactionPrompt.gameObject.SetActive(false);
            }
            this.enabled = false;
            return;
        }

        if (interactionPrompt != null)
        {
            interactionPrompt.text = "";
        }
    }

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
                currentInteractable = interactable;

                if (interactionPrompt != null)
                {
                    interactionPrompt.text = interactable.InteractionText;
                }

                bool interactInput = Input.GetKeyDown(interactKey);
                if (MobileInputManager.Instance != null && MobileInputManager.Instance.ConsumeInteract())
                {
                    interactInput = true;
                }

                if (interactInput)
                {
                    interactable.Interact(gameObject);
                }

                return;
            }
        }

        currentInteractable = null;
        if (interactionPrompt != null)
        {
            interactionPrompt.text = "";
        }
    }

    public void TriggerInteract()
    {
        if (currentInteractable != null)
        {
            currentInteractable.Interact(gameObject);
        }
    }

    [ServerRpc]
    public void RequestStartRunServerRpc(string sceneName)
    {
        if (!IsServer) return;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }
}
