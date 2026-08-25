using UnityEngine;
using Unity.Netcode;

public class RotatePlayer : NetworkBehaviour
{
    public Transform Camera;
    void LateUpdate()
    {
        if (!IsOwner || Camera == null) return;
        transform.rotation = Camera.transform.rotation;
    }
}
