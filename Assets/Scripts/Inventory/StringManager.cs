using UnityEngine;

public class StringManager : MonoBehaviour
{
    [Header("Anchors")]
    public Transform Anchor1;
    public Transform Anchor2;

    [Header("Visuals")]
    public int segments = 12;
    public float droop = 0.6f;
    public float maxSagPerMeter = 0.25f;

    [Header("Physics(Spring)")]
    public float stringLength = 5f;
    public float slack = 0.5f;
    public float spring = 50f;
    public float damping = 6f;

    [Header("GroundClamp")]
    public float ropeClearance = 0.04f;

    private LineRenderer lr;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.useWorldSpace = true;

        if(Mathf.Approximately(lr.startWidth, 0f) && Mathf.Approximately(lr.endWidth, 0f))
        {
            lr.startWidth = 0.05f;
            lr.endWidth = 0.05f;
        }

        lr.positionCount = segments + 1;
    }


    void Update()
    {
        DrawRope();
    }

    private void DrawRope()
    {
        if (!Anchor1|| !Anchor2 || !lr) return;
            
        Vector3 start = Anchor1.position;
        Vector3 end = Anchor2.position;

        if(lr.positionCount != segments + 1)
        {
            lr.positionCount = segments + 1;
        }

        float distance = Vector3.Distance(start, end);
        float effectiveDroop = Mathf.Min(droop, Mathf.Max(0f, maxSagPerMeter * distance));

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 point = Vector3.Lerp(start, end, t);

            // Apply droop
            float sag = effectiveDroop * Mathf.Sin(Mathf.PI * t);
            point.y -= sag;

            float minY = 0f + ropeClearance;
            if (point.y < minY) point.y = minY;
            lr.SetPosition(i, point);
        }

       
    } 

    public void Init(GameObject anchor1, GameObject anchor2)
    {
        Anchor1 = anchor1.GetComponent<Transform>();
        Anchor2 = anchor2.GetComponent<Transform>();
        Rigidbody rb1 = anchor1.GetComponentInChildren<Rigidbody>();
        SpringJoint joint = anchor1.AddComponent<SpringJoint>();
        joint.connectedBody = anchor2.GetComponentInChildren<Rigidbody>();
        joint.autoConfigureConnectedAnchor = true;
        joint.minDistance = 0f;
        joint.maxDistance = stringLength + slack;
        joint.spring = spring;
        joint.damper = damping;
        joint.tolerance = 0.025f;
    }
}
