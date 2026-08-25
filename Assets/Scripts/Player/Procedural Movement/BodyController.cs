using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BodyController : MonoBehaviour
{
    public GameObject[] legTargets;
    public GameObject[] legCubes;
    public GameObject spider;

    public float MoveDistance = 1f;
    public int legSmoothness = 5;
    public int velocitySmoothness = 3;
    public float overStepMultiplier = 1.3f;
    public int waitTimeBetweenSteps = 2;
    
    [Header("Ground Snapping")]
    public LayerMask groundLayer;
    public float raycastDistance = 3f;

    Vector3[] legOriginalPositions;

    Vector3 velocity;
    Vector3 lastVelocity;
    Vector3 lastSpiderPosition;

    List<int> nextIndexToMove = new List<int>();
    List<int> IndexMoving = new List<int>();

    void Start()
    {
        lastSpiderPosition = spider.transform.position;
        legOriginalPositions = new Vector3[legTargets.Length];

        for (int i = 0; i < legTargets.Length; i++)
        {
            legOriginalPositions[i] = legTargets[i].transform.position;
        }
    }

    void FixedUpdate()
    {
        velocity = spider.transform.position - lastSpiderPosition;
        velocity = velocity + velocitySmoothness * lastVelocity;
        velocity = velocity / (velocitySmoothness + 1);

        MoveLegs();

        lastSpiderPosition = spider.transform.position;
        lastVelocity = velocity;
    }

    void MoveLegs()
    {
        for (int i = 0; i < legTargets.Length; i++)
        {
            // CRITICAL FIX: Lock legs that are NOT actively taking a step back to their pinned ground position
            if (!IndexMoving.Contains(i))
            {
                legTargets[i].transform.position = legOriginalPositions[i];
            }

            // Check if the body moved far enough to trigger a step for this leg
            if (Vector3.Distance(legCubes[i].transform.position, legOriginalPositions[i]) >= MoveDistance)
            {
                if (!nextIndexToMove.Contains(i) && !IndexMoving.Contains(i))
                {
                    nextIndexToMove.Add(i);
                }
            }
        }

        // Only move one leg at a time
        if (nextIndexToMove.Count == 0 || IndexMoving.Count != 0)
        {
            return;
        }

        int nextIndex = nextIndexToMove[0];

        // Raycast down from legCube to find the true ground position
        Vector3 targetPosition = GetGroundPosition(legCubes[nextIndex].transform.position);

        // Apply overstep bias based on spider body velocity
        targetPosition += velocity * overStepMultiplier;

        StartCoroutine(Step(nextIndex, targetPosition));
    }

    Vector3 GetGroundPosition(Vector3 origin)
    {
        // Raycast downwards from the leg cube position to hit ground geometry
        if (Physics.Raycast(origin + Vector3.up * 1f, Vector3.down, out RaycastHit hit, raycastDistance, groundLayer))
        {
            return hit.point;
        }
        return origin; // Fallback if no ground hit
    }

    IEnumerator Step(int index, Vector3 MoveTo)
    {
        if (nextIndexToMove.Contains(index))
        {
            nextIndexToMove.Remove(index);
        }
        if (!IndexMoving.Contains(index))
        {
            IndexMoving.Add(index);
        }

        Vector3 startPosition = legOriginalPositions[index];

        for (int i = 1; i <= legSmoothness; i++)
        {
            float t = (float)i / legSmoothness;

            // Arc the leg upward slightly mid-step for a realistic footstep motion
            Vector3 currentPos = Vector3.Lerp(startPosition, MoveTo, t);
            currentPos.y += Mathf.Sin(t * Mathf.PI) * 0.3f; 

            legTargets[index].transform.position = currentPos;
            yield return new WaitForFixedUpdate();
        }

        // Pin ground target to new position
        legOriginalPositions[index] = MoveTo;

        for (int i = 1; i <= waitTimeBetweenSteps; i++)
        {
            yield return new WaitForFixedUpdate();
        }

        if (IndexMoving.Contains(index))
        {
            IndexMoving.Remove(index);
        }
    }
}