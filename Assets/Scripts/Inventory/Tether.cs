using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class NetworkTetherSystem : NetworkBehaviour
{
    private static Dictionary<ulong, int> GlobalTetherRegistry = new Dictionary<ulong, int>();

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
    public float minPullDistance = 3f;
    public LayerMask grappleLayer;
    public KeyCode tetherKey = KeyCode.E;
    public Material lineMaterial;
    public float ropeWidth = 0.05f;

    [Header("Physics")]
    public float pullForce = 60f;

    public List<TetherInstance> activeTethers = new List<TetherInstance>();

    void Update()
    {
        if (!IsOwner) return;
        if (Input.GetKeyDown(tetherKey)) HandleTetherInput();
    }

    void HandleTetherInput()
    {
        RaycastHit hit;
        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, maxDistance, grappleLayer))
        {
            var networkObj = hit.collider.GetComponent<NetworkObject>();
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

    [ClientRpc]
    void ToggleTetherClientRpc(ulong targetId, Vector3 worldHitPoint, bool isCreating)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId, out var targetNetworkObj))
        {
            GameObject targetGo = targetNetworkObj.gameObject;
            int existingIndex = activeTethers.FindIndex(t => t.targetId == targetId);

            if (!isCreating && existingIndex != -1)
            {
                
                if (activeTethers[existingIndex].line) Destroy(activeTethers[existingIndex].line.gameObject);
                activeTethers.RemoveAt(existingIndex);
            }
            else if (isCreating && existingIndex == -1)
            {
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
        if (!IsOwner) return;
        foreach (var tether in activeTethers)
        {
            if (tether.rb == null) continue;
            Vector3 targetPoint = tether.target.transform.TransformPoint(tether.localOffset);
            Vector3 direction = transform.position - targetPoint;
            if (direction.magnitude > minPullDistance)
                tether.rb.AddForce(direction.normalized * pullForce, ForceMode.Acceleration);
        }
    }

    void LateUpdate()
    {
        for (int i = activeTethers.Count - 1; i >= 0; i--)
        {
            if (activeTethers[i].target == null)
            {
                if (activeTethers[i].line) Destroy(activeTethers[i].line.gameObject);
                activeTethers.RemoveAt(i);
                continue;
            }
            activeTethers[i].line.SetPosition(0, transform.position);
            activeTethers[i].line.SetPosition(1, activeTethers[i].target.transform.TransformPoint(activeTethers[i].localOffset));
        }
    }
}