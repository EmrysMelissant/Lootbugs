using UnityEngine;

public class LegAimGrounding : MonoBehaviour
{
    GameObject raycastOrigin;
    int layerMask;
    void Start()
    {
        raycastOrigin = transform.parent.gameObject;
        layerMask = LayerMask.GetMask("Ground");
    }

    void Update()
    {
        RaycastHit hit;
        if (Physics.Raycast(raycastOrigin.transform.position, -transform.up, out hit, Mathf.Infinity))
        {
            transform.position = hit.point;
        }
    }
}
