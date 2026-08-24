using UnityEngine;
using Unity.Netcode;

public class RotatePlayer : NetworkBehaviour
{
    public Transform Camera;
    void Update()
    {
        transform.rotation = Camera.transform.rotation;
    }
}
