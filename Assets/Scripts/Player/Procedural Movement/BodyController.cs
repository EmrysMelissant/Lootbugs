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

    [Header("Audio Settings")]
    [Tooltip("Optional audio clips played on each procedural leg step. A random clip is selected per step.")]
    [SerializeField] private AudioClip[] stepSounds;
    [Tooltip("Single audio clip fallback if stepSounds array is empty.")]
    [SerializeField] private AudioClip stepSound;
    [SerializeField, Range(0f, 1f)] private float stepVolume = 0.4f;
    [SerializeField, Range(0.5f, 2f)] private float minPitch = 0.85f;
    [SerializeField, Range(0.5f, 2f)] private float maxPitch = 1.25f;
    [Tooltip("Play sound when leg touches the ground (true) or when lifting (false).")]
    [SerializeField] private bool playOnFootPlant = true;
    [Tooltip("Minimum time between step sounds in seconds.")]
    [SerializeField] private float minSoundInterval = 0.04f;

    private AudioSource audioSource;
    private float lastSoundTime = -1f;
    private static AudioClip defaultProceduralStepClip;

    Vector3[] legOriginalPositions;

    Vector3 velocity;
    Vector3 lastVelocity;
    Vector3 lastSpiderPosition;

    List<int> nextIndexToMove = new List<int>();
    List<int> IndexMoving = new List<int>();

    void Awake()
    {
        InitializeAudio();
    }

    void Start()
    {
        if (spider != null)
        {
            lastSpiderPosition = spider.transform.position;
        }

        legOriginalPositions = new Vector3[legTargets.Length];

        for (int i = 0; i < legTargets.Length; i++)
        {
            if (legTargets[i] != null)
            {
                legOriginalPositions[i] = legTargets[i].transform.position;
            }
        }
    }

    private void InitializeAudio()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
        if (audioSource == null && spider != null)
        {
            audioSource = spider.GetComponent<AudioSource>();
        }
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1.0f;
            audioSource.minDistance = 1f;
            audioSource.maxDistance = 20f;
            audioSource.playOnAwake = false;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
        }
    }

    void FixedUpdate()
    {
        if (spider == null) return;

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
            if (legTargets[i] == null || legCubes[i] == null) continue;

            // Lock legs that are NOT actively taking a step back to their pinned ground position
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
        if (nextIndex < 0 || nextIndex >= legCubes.Length || legCubes[nextIndex] == null)
        {
            nextIndexToMove.Remove(nextIndex);
            return;
        }

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

        if (!playOnFootPlant && index >= 0 && index < legTargets.Length && legTargets[index] != null)
        {
            PlayStepSound(legTargets[index].transform.position);
        }

        Vector3 startPosition = legOriginalPositions[index];

        for (int i = 1; i <= legSmoothness; i++)
        {
            float t = (float)i / legSmoothness;

            // Arc the leg upward slightly mid-step for a realistic footstep motion
            Vector3 currentPos = Vector3.Lerp(startPosition, MoveTo, t);
            currentPos.y += Mathf.Sin(t * Mathf.PI) * 0.3f; 

            if (index >= 0 && index < legTargets.Length && legTargets[index] != null)
            {
                legTargets[index].transform.position = currentPos;
            }
            yield return new WaitForFixedUpdate();
        }

        // Pin ground target to new position
        legOriginalPositions[index] = MoveTo;

        if (playOnFootPlant)
        {
            PlayStepSound(MoveTo);
        }

        for (int i = 1; i <= waitTimeBetweenSteps; i++)
        {
            yield return new WaitForFixedUpdate();
        }

        if (IndexMoving.Contains(index))
        {
            IndexMoving.Remove(index);
        }
    }

    private void PlayStepSound(Vector3 position)
    {
        if (Time.time - lastSoundTime < minSoundInterval) return;
        lastSoundTime = Time.time;

        AudioClip clipToPlay = null;

        if (stepSounds != null && stepSounds.Length > 0)
        {
            int validCount = 0;
            for (int i = 0; i < stepSounds.Length; i++)
            {
                if (stepSounds[i] != null) validCount++;
            }

            if (validCount > 0)
            {
                int rand = Random.Range(0, stepSounds.Length);
                clipToPlay = stepSounds[rand];
            }
        }

        if (clipToPlay == null)
        {
            clipToPlay = stepSound;
        }

        if (clipToPlay == null)
        {
            clipToPlay = GetDefaultStepClip();
        }

        if (clipToPlay == null) return;

        if (audioSource != null)
        {
            audioSource.pitch = Random.Range(minPitch, maxPitch);
            audioSource.PlayOneShot(clipToPlay, stepVolume);
        }
        else
        {
            AudioSource.PlayClipAtPoint(clipToPlay, position, stepVolume);
        }
    }

    private static AudioClip GetDefaultStepClip()
    {
        if (defaultProceduralStepClip != null) return defaultProceduralStepClip;

        int sampleRate = 44100;
        int samples = (int)(sampleRate * 0.035f); // 35ms crisp tap
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float decay = Mathf.Exp(-t * 22f);
            float wave1 = Mathf.Sin(2f * Mathf.PI * 220f * (i / (float)sampleRate));
            float wave2 = Mathf.Sin(2f * Mathf.PI * 780f * (i / (float)sampleRate));
            float noise = (Random.value * 2f - 1f) * 0.25f;

            data[i] = (wave1 * 0.5f + wave2 * 0.35f + noise) * decay;
        }

        defaultProceduralStepClip = AudioClip.Create("ProceduralLegStep", samples, 1, sampleRate, false);
        defaultProceduralStepClip.SetData(data, 0);
        return defaultProceduralStepClip;
    }
}