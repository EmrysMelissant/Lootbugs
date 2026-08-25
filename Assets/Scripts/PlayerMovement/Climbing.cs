using Unity.Netcode;
using UnityEngine;

public class Climbing : NetworkBehaviour
{
    [Header("references")]
    public Transform orientation;
    public Rigidbody rb;
    public GameObject player;
    public LayerMask WhatIsWall;
    float targetTime = 1f;
    bool canClimb = true;
    private Quaternion startRotation;
    public float rotationSpeed;
    public float surfaceDistance;
    public float surfaceStickForce;
    private Vector3 surfacePoint;

    [Header("Climbing")]
    public float ClimbSpeed;

    [Header("Detection")]
    public float detectionDistance;
    private RaycastHit hit;
    public float detectionRadius;



    void Start()
    {
        rb = GetComponent<Rigidbody>();
        startRotation = transform.rotation;
    }

    private void Update()
    {
        if (!IsOwner) return;
        FaceWall();
    }
    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            
        }
    }
    private void FaceWall()
    {
        Vector3[] directions = { transform.forward,transform.up, transform.right, -transform.right};
        foreach (Vector3 dir in directions)
        {
            if (Physics.SphereCast(transform.position, detectionRadius, dir, out hit, detectionDistance, WhatIsWall))
            {
                Quaternion targetRotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
                orientation.localRotation = Quaternion.Slerp(orientation.localRotation, targetRotation, rotationSpeed * Time.deltaTime);
                transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, rotationSpeed * Time.deltaTime);
                //Debug.DrawRay(transform.position, directions[i], Color.green);
                surfacePoint = hit.point;
                Vector3 toSurface = surfacePoint - transform.position;
                float distance = toSurface.magnitude;
                if (distance > surfaceDistance)
                {
                    rb.AddForce(toSurface.normalized * surfaceStickForce);
                }
                break; // Stop at the first hit to prioritize directions
            }
        }
    }

}
