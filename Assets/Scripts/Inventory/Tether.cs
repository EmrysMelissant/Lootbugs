using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class NetworkTetherSystem : NetworkBehaviour
{
    private static readonly Dictionary<ulong, int> GlobalTetherRegistry = new Dictionary<ulong, int>();

    [System.Serializable]
    public class TetherInstance
    {
        public GameObject target;
        public Rigidbody rb;
        public Vector3 localOffset;
        public LineRenderer line;
        public ulong targetId;
    }

    [Header("Settings")]
    public float maxDistance = 50f;
    public float minPullDistance = 1.2f;
    public LayerMask grappleLayer;
    public KeyCode tetherKey = KeyCode.Mouse0;
    public Material lineMaterial;
    public float ropeWidth = 0.05f;

    [Header("Physics")]
    public float pullForce = 60f;

    [Header("Teleport Settings")]
    [Tooltip("Maximum distance the tether can stretch before the target item teleports next to the player.")]
    [SerializeField] private float maxTetherRange = 10f;

    [Tooltip("Distance in front of the player where the teleported target is placed.")]
    [SerializeField] private float teleportOffset = 1.5f;

    public float MaxTetherRange
    {
        get => maxTetherRange;
        set => maxTetherRange = value;
    }

    public float TeleportOffset
    {
        get => teleportOffset;
        set => teleportOffset = value;
    }

    public List<TetherInstance> activeTethers = new List<TetherInstance>();

    private Camera cachedCam;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner)
        {
            cachedCam = GetComponentInChildren<Camera>();
            if (cachedCam == null) cachedCam = Camera.main;
        }
    }

    void Update()
    {
        if (!IsOwner) return;
        if (Input.GetKeyDown(tetherKey)) HandleTetherInput();
    }

    void HandleTetherInput()
    {
        if (cachedCam == null)
        {
            cachedCam = GetComponentInChildren<Camera>();
            if (cachedCam == null) cachedCam = Camera.main;
            if (cachedCam == null) return;
        }

        RaycastHit hit;
        if (Physics.Raycast(cachedCam.transform.position, cachedCam.transform.forward, out hit, maxDistance, grappleLayer))
        {
            var networkObj = hit.collider.GetComponentInParent<NetworkObject>();
            if (networkObj != null)
            {
                ToggleTetherServerRpc(networkObj.NetworkObjectId, hit.point);
            }
        }
    }

    [ServerRpc]
    void ToggleTetherServerRpc(ulong targetId, Vector3 worldHitPoint, ServerRpcParams rpcParams = default)
    {
        int clientId = (int)rpcParams.Receive.SenderClientId;

        if (GlobalTetherRegistry.ContainsKey(targetId))
        {
            if (GlobalTetherRegistry[targetId] == clientId)
            {
                GlobalTetherRegistry.Remove(targetId);
                ToggleTetherClientRpc(targetId, worldHitPoint, false);
            }
            else
            {
                return; 
            }
        }
        else
        {
            GlobalTetherRegistry.Add(targetId, clientId);
            ToggleTetherClientRpc(targetId, worldHitPoint, true);
        }
    }

    public static void ReleaseTetherForTarget(ulong targetId)
    {
        if (GlobalTetherRegistry.ContainsKey(targetId))
        {
            GlobalTetherRegistry.Remove(targetId);
        }

        NetworkTetherSystem[] allSystems = FindObjectsByType<NetworkTetherSystem>(FindObjectsSortMode.None);
        for (int i = 0; i < allSystems.Length; i++)
        {
            if (allSystems[i] != null)
            {
                allSystems[i].ToggleTetherClientRpc(targetId, Vector3.zero, false);
            }
        }
    }

    [ClientRpc]
    void ToggleTetherClientRpc(ulong targetId, Vector3 worldHitPoint, bool isCreating)
    {
        int existingIndex = activeTethers.FindIndex(t => t.targetId == targetId);

        if (!isCreating && existingIndex != -1)
        {
            if (activeTethers[existingIndex].line != null)
            {
                Destroy(activeTethers[existingIndex].line.gameObject);
            }
            activeTethers.RemoveAt(existingIndex);
            return;
        }

        if (isCreating && existingIndex == -1)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SpawnManager != null &&
                NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId, out var targetNetworkObj))
            {
                GameObject targetGo = targetNetworkObj.gameObject;
                CreateLocalVisualTether(targetGo, targetId, worldHitPoint);
            }
        }
    }

    void CreateLocalVisualTether(GameObject targetGo, ulong id, Vector3 worldHitPoint)
    {
        TetherInstance tether = new TetherInstance();
        tether.target = targetGo;
        tether.targetId = id;
        tether.rb = targetGo.GetComponent<Rigidbody>();
        tether.localOffset = targetGo.transform.InverseTransformPoint(worldHitPoint);

        GameObject lineObj = new GameObject("NetTetherLine");
        tether.line = lineObj.AddComponent<LineRenderer>();
        tether.line.material = lineMaterial;
        tether.line.startWidth = ropeWidth;
        tether.line.endWidth = ropeWidth;
        tether.line.positionCount = 2;

        activeTethers.Add(tether);
    }
    
    void FixedUpdate()
    {
        // On Server (or local authority): Apply physical force and handle distance teleportation
        bool isNetworkActive = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        bool hasAuthority = !isNetworkActive || IsServer;

        if (hasAuthority)
        {
            for (int i = 0; i < activeTethers.Count; i++)
            {
                var tether = activeTethers[i];
                if (tether == null || tether.target == null || tether.rb == null) continue;

                Vector3 targetPoint = tether.target.transform.TransformPoint(tether.localOffset);
                Vector3 direction = transform.position - targetPoint;
                float currentDistance = direction.magnitude;

                // Teleport item next to the player if it exceeds the maximum tether range
                if (currentDistance > maxTetherRange)
                {
                    TeleportTargetNextToPlayer(tether);
                    continue;
                }

                if (currentDistance > minPullDistance)
                {
                    tether.rb.AddForce(direction.normalized * pullForce, ForceMode.Acceleration);
                }
            }
        }
    }

    private void TeleportTargetNextToPlayer(TetherInstance tether)
    {
        if (tether == null || tether.target == null) return;

        Vector3 forwardDirection = transform.forward;
        if (forwardDirection.sqrMagnitude < 0.001f) forwardDirection = Vector3.forward;

        Vector3 desiredPosition = transform.position + forwardDirection * teleportOffset + Vector3.up * 0.2f;

        // Obstacle check: avoid placing inside walls if facing an obstacle
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, forwardDirection, out RaycastHit hit, teleportOffset + 0.3f, ~grappleLayer))
        {
            desiredPosition = transform.position + forwardDirection * Mathf.Max(0.5f, hit.distance - 0.4f) + Vector3.up * 0.2f;
        }

        tether.target.transform.position = desiredPosition;

        if (tether.rb != null)
        {
            tether.rb.position = desiredPosition;
            tether.rb.linearVelocity = Vector3.zero;
            tether.rb.angularVelocity = Vector3.zero;
        }
    }

    void LateUpdate()
    {
        for (int i = activeTethers.Count - 1; i >= 0; i--)
        {
            if (activeTethers[i].target == null)
            {
                if (activeTethers[i].line != null)
                {
                    Destroy(activeTethers[i].line.gameObject);
                }
                activeTethers.RemoveAt(i);
                continue;
            }

            if (activeTethers[i].line != null)
            {
                activeTethers[i].line.SetPosition(0, transform.position);
                activeTethers[i].line.SetPosition(1, activeTethers[i].target.transform.TransformPoint(activeTethers[i].localOffset));
            }
        }
    }
}