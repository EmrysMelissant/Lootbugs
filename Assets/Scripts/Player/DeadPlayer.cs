using UnityEngine;
using Unity.Netcode;

public class DeadPlayer : NetworkBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    [SerializeField] private string interactText = "E to Resurrect for 20 HP";
    public string InteractionText => interactText;

    [Header("Camera & Look Settings")]
    [SerializeField] private float senseX = 800f;
    [SerializeField] private float senseY = 800f;
    [SerializeField] private Transform orientationTransform;
    [SerializeField] private Transform cameraTransform;

    // Synchronize the target player's ClientId across network
    private readonly NetworkVariable<ulong> targetOwnerClientId = new NetworkVariable<ulong>(
        ulong.MaxValue,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private Camera cachedCamera;
    private AudioListener cachedListener;
    private float xRotation;
    private float yRotation;

    private void Awake()
    {
        // Cache references
        cachedCamera = GetComponentInChildren<Camera>(true);
        cachedListener = GetComponentInChildren<AudioListener>(true);

        if (cameraTransform == null && cachedCamera != null)
        {
            cameraTransform = cachedCamera.transform;
        }

        if (orientationTransform == null)
        {
            Transform orient = transform.Find("orientation");
            if (orient != null)
            {
                orientationTransform = orient;
            }
        }

        // Disable corpse camera and listener by default
        if (cachedCamera != null)
        {
            cachedCamera.enabled = false;
            cachedCamera.gameObject.SetActive(false);
        }
        if (cachedListener != null)
        {
            cachedListener.enabled = false;
        }

        // Disable any attached PlayerCam component to prevent NetworkBehaviour IsOwner conflicts
        if (TryGetComponent<PlayerCam>(out var playerCam))
        {
            playerCam.enabled = false;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        targetOwnerClientId.OnValueChanged += OnTargetOwnerChanged;

        UpdateCameraState();

        if (targetOwnerClientId.Value != ulong.MaxValue)
        {
            UpdateMaterialFromTargetPlayer();
        }
    }

    public override void OnNetworkDespawn()
    {
        targetOwnerClientId.OnValueChanged -= OnTargetOwnerChanged;

        if (cachedCamera != null)
        {
            cachedCamera.enabled = false;
            cachedCamera.gameObject.SetActive(false);
        }
        if (cachedListener != null)
        {
            cachedListener.enabled = false;
        }

        base.OnNetworkDespawn();
    }

    private void Update()
    {
        if (!IsMyCorpse()) return;

        HandleCameraLook();
    }

    private void HandleCameraLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * senseX;
        float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * senseY;

        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(xRotation, yRotation, 0);
        }

        if (orientationTransform != null)
        {
            orientationTransform.localRotation = Quaternion.Euler(0, yRotation, 0);
        }
    }

    private void OnTargetOwnerChanged(ulong previousValue, ulong newValue)
    {
        UpdateMaterialFromTargetPlayer();
        UpdateCameraState();
    }

    public bool IsMyCorpse()
    {
        return NetworkManager.Singleton != null &&
               targetOwnerClientId.Value != ulong.MaxValue &&
               targetOwnerClientId.Value == NetworkManager.Singleton.LocalClientId;
    }

    private void UpdateCameraState()
    {
        bool isMine = IsMyCorpse();

        if (cachedCamera == null)
        {
            cachedCamera = GetComponentInChildren<Camera>(true);
        }
        if (cachedListener == null)
        {
            cachedListener = GetComponentInChildren<AudioListener>(true);
        }

        if (cachedCamera != null)
        {
            cachedCamera.enabled = isMine;
            cachedCamera.gameObject.SetActive(isMine);
            if (isMine)
            {
                cachedCamera.tag = "MainCamera";
            }
        }

        if (cachedListener != null)
        {
            cachedListener.enabled = isMine;
        }

        // Keep PlayerCam disabled on corpse
        if (TryGetComponent<PlayerCam>(out var playerCam))
        {
            playerCam.enabled = false;
        }
    }

    public void Initialize(ulong clientId, GameObject playerObject = null)
    {
        targetOwnerClientId.Value = clientId;

        if (playerObject != null)
        {
            CopyMaterialFromPlayer(playerObject);
        }
    }

    public void UpdateMaterialFromTargetPlayer()
    {
        if (targetOwnerClientId.Value == ulong.MaxValue) return;

        GameObject playerObj = null;
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.ConnectedClients != null &&
            NetworkManager.Singleton.ConnectedClients.TryGetValue(targetOwnerClientId.Value, out var client))
        {
            if (client.PlayerObject != null)
            {
                playerObj = client.PlayerObject.gameObject;
            }
        }

        if (playerObj == null)
        {
            PlayerController[] controllers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            for (int i = 0; i < controllers.Length; i++)
            {
                PlayerController pc = controllers[i];
                if (pc != null && pc.OwnerClientId == targetOwnerClientId.Value)
                {
                    playerObj = pc.gameObject;
                    break;
                }
            }
        }

        if (playerObj != null)
        {
            CopyMaterialFromPlayer(playerObj);
        }
    }

    public void CopyMaterialFromPlayer(GameObject player)
    {
        if (player == null) return;

        Material playerMat = null;
        Renderer[] playerRenderers = player.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < playerRenderers.Length; i++)
        {
            Renderer rend = playerRenderers[i];
            if (rend == null) continue;
            if (rend is ParticleSystemRenderer || rend is TrailRenderer || rend is LineRenderer) continue;

            if (rend.sharedMaterial != null)
            {
                playerMat = rend.sharedMaterial;
                break;
            }
        }

        if (playerMat != null)
        {
            ApplyMaterial(playerMat);
        }
    }

    public void ApplyMaterial(Material mat)
    {
        if (mat == null) return;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer rend = renderers[i];
            if (rend == null) continue;
            if (rend is ParticleSystemRenderer || rend is TrailRenderer || rend is LineRenderer) continue;

            rend.material = mat;
        }
    }

    public void Interact(GameObject interactor)
    {
        if (interactor == null) return;

        // Require the interactor to have PlayerController component
        if (interactor.TryGetComponent(out PlayerController reviverPlayer))
        {
            float healthCost = 20f;

            if (reviverPlayer.Health > healthCost)
            {
                RequestReviveServerRpc();
            }
            else
            {
                Debug.LogWarning("Not enough health to resurrect team member!");
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestReviveServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;

        ulong reviverClientId = rpcParams.Receive.SenderClientId;

        // Release any tethers attached to this corpse before despawning
        if (TryGetComponent(out NetworkObject corpseNetObj))
        {
            NetworkTetherSystem.ReleaseTetherForTarget(corpseNetObj.NetworkObjectId);
        }

        // Verify and deduct health cost from the reviver on the server
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.ConnectedClients != null &&
            NetworkManager.Singleton.ConnectedClients.TryGetValue(reviverClientId, out var reviverClient))
        {
            if (reviverClient.PlayerObject != null && reviverClient.PlayerObject.TryGetComponent(out PlayerController reviverController))
            {
                if (reviverController.Health <= 20f)
                {
                    Debug.LogWarning("Reviver does not have enough health to resurrect.");
                    return;
                }

                reviverController.ApplyDamageDirect(20f);
                reviverController.ResetVelocity();
                reviverController.IgnoreCollisionsWithAllPlayers();
            }
        }

        ulong targetClientId = targetOwnerClientId.Value;
        PlayerController targetPlayer = null;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.ConnectedClients != null &&
            NetworkManager.Singleton.ConnectedClients.TryGetValue(targetClientId, out var targetClient))
        {
            if (targetClient.PlayerObject != null)
            {
                targetPlayer = targetClient.PlayerObject.GetComponent<PlayerController>();
            }
        }

        // Fallback: Search all PlayerControllers in the scene
        if (targetPlayer == null)
        {
            PlayerController[] allPlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            for (int i = 0; i < allPlayers.Length; i++)
            {
                PlayerController pc = allPlayers[i];
                if (pc != null && pc.OwnerClientId == targetClientId)
                {
                    targetPlayer = pc;
                    break;
                }
            }
        }

        if (targetPlayer != null)
        {
            targetPlayer.IgnoreCollisionsWithAllPlayers();
            targetPlayer.ReviveFromCorpse(transform.position, 20f);
        }

        // Despawn death item marker on server
        if (TryGetComponent(out NetworkObject netObj) && netObj.IsSpawned)
        {
            netObj.Despawn(true);
        }
    }
}