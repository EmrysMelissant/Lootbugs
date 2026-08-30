using UnityEngine;

public class LegAimGrounding : MonoBehaviour
{
    [SerializeField] private LayerMask groundMask;
    private GameObject raycastOrigin;

    private void Awake()
    {
        CacheReferences();
    }

    private void OnEnable()
    {
        CacheReferences();
        UpdateGroundPosition();
    }

    private void Start()
    {
        CacheReferences();
        UpdateGroundPosition();
    }

    private void CacheReferences()
    {
        if (raycastOrigin == null && transform.parent != null)
        {
            raycastOrigin = transform.parent.gameObject;
        }

        if (groundMask.value == 0)
        {
            groundMask = LayerMask.GetMask("Ground", "Default", "Environment");
            if (groundMask.value == 0)
            {
                groundMask = ~LayerMask.GetMask("Player", "Ignore Raycast");
            }
        }
    }

    private void Update()
    {
        UpdateGroundPosition();
    }

    private void UpdateGroundPosition()
    {
        if (raycastOrigin == null)
        {
            if (transform.parent != null) raycastOrigin = transform.parent.gameObject;
            else return;
        }

        Vector3 rayStart = raycastOrigin.transform.position + Vector3.up * 0.5f;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 10f, groundMask, QueryTriggerInteraction.Ignore))
        {
            transform.position = hit.point;
        }
    }
}
