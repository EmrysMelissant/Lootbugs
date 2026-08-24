using UnityEngine;
using Unity.Netcode;
public class MoveCamera : NetworkBehaviour
{
    public Transform cameraPos;
    void Update()
    {
        transform.position = cameraPos.position;
        transform.rotation = cameraPos.rotation;
    }
}
