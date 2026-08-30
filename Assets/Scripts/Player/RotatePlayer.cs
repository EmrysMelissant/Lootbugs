using Unity.Netcode;
using UnityEngine;

public class RotatePlayer : NetworkBehaviour
{
    [SerializeField] public Transform Camera;

    private readonly NetworkVariable<Quaternion> netRotation = new NetworkVariable<Quaternion>(
        Quaternion.identity,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private void Awake()
    {
        if (Camera == null)
        {
            Camera cam = GetComponentInParent<Camera>();
            if (cam == null) cam = transform.root.GetComponentInChildren<Camera>(true);
            if (cam != null) Camera = cam.transform;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsOwner)
        {
            transform.rotation = netRotation.Value;
        }
    }

    private void LateUpdate()
    {
        if (IsOwner)
        {
            if (Camera != null)
            {
                transform.rotation = Camera.rotation;
                if (Quaternion.Angle(netRotation.Value, transform.rotation) > 0.1f)
                {
                    netRotation.Value = transform.rotation;
                }
            }
        }
        else
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, netRotation.Value, Time.deltaTime * 20f);
        }
    }
}
