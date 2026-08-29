using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.AI.Navigation;

public class SlidingDoor : NetworkBehaviour
{
    [Header("NavMesh Settings")]
    [Tooltip("If true, automatically marks door panel colliders to be ignored by NavMeshSurface builds so NavMesh generates cleanly through the doorway.")]
    [SerializeField] private bool ignoreFromNavMeshBuild = true;

    [Header("Door Panel References")]
    [Tooltip("Primary moving door panel Transform. If null, automatically looks for a child door panel or uses this Transform.")]
    [SerializeField] private Transform doorPanel;

    [Tooltip("Optional secondary door panel Transform for double/split sliding doors.")]
    [SerializeField] private Transform secondaryDoorPanel;

    [Header("Movement Settings")]
    [Tooltip("Local or world space offset applied to doorPanel when opening (e.g. (0, 3, 0) for sliding up, (2.5, 0, 0) for sliding right).")]
    [SerializeField] private Vector3 slideOffset = new Vector3(0f, 3f, 0f);

    [Tooltip("Offset applied to secondaryDoorPanel when opening (e.g. (-2.5, 0, 0) for opposite split sliding).")]
    [SerializeField] private Vector3 secondarySlideOffset = new Vector3(0f, -3f, 0f);

    [Tooltip("If true, slide offsets are relative to the door's local rotation.")]
    [SerializeField] private bool useLocalSpace = true;

    [Tooltip("Speed of the sliding motion (units per second).")]
    [SerializeField] private float slideSpeed = 4f;

    [Tooltip("Movement animation curve easing (0 to 1).")]
    [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Delay in seconds before closing after all colliders have left the trigger area.")]
    [SerializeField] private float autoCloseDelay = 1.0f;

    [Header("Trigger & Detection Settings")]
    [Tooltip("The trigger collider that detects approaching entities. If null, automatically finds or creates one.")]
    [SerializeField] private Collider triggerCollider;

    [Tooltip("Dimensions of the trigger volume box.")]
    [SerializeField] private Vector3 triggerBoxSize = new Vector3(3.5f, 3.5f, 3.5f);

    [Tooltip("Local center offset of the trigger volume.")]
    [SerializeField] private Vector3 triggerBoxCenter = new Vector3(0f, 1.5f, 0f);

    [Tooltip("Layer mask filter for colliders that can trigger the door.")]
    [SerializeField] private LayerMask triggerLayers = ~0;

    [Tooltip("Allowed tags that can trigger the door (e.g. 'Player', 'Item'). Leave empty to allow any object.")]
    [SerializeField] private List<string> targetTags = new List<string> {};

    [Header("Room Connection Settings")]
    [Tooltip("If true, the door will not open if this is a dead-end anchor with no other room connected in the generated dungeon.")]
    [SerializeField] private bool requireConnectedRoom = true;

    [Tooltip("The door anchor Transform associated with this doorway. If null, automatically detected from this object/parent/children.")]
    [SerializeField] private Transform doorAnchor;

    [Tooltip("Tag used to identify door anchor transforms on rooms.")]
    [SerializeField] private string doorTag = "doorTag";

    [Tooltip("Maximum distance between two door anchors to consider them connected.")]
    [SerializeField] private float anchorConnectionDistance = 2.5f;

    [Tooltip("Secondary verification: check if walkable floor/geometry exists beyond the doorway.")]
    [SerializeField] private bool checkFloorBeyondDoor = true;
    [SerializeField] private float floorCheckDistance = 1.5f;
    [SerializeField] private LayerMask floorLayerMask = ~0;

    [Header("Audio Settings")]
    [Tooltip("AudioSource used for sound playback. Automatically initialized in 3D if null.")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("Sound clip played when the door starts opening.")]
    [SerializeField] private AudioClip openSound;

    [Tooltip("Sound clip played when the door starts closing.")]
    [SerializeField] private AudioClip closeSound;

    [Tooltip("Playback volume for door sound effects.")]
    [SerializeField, Range(0f, 1f)] private float soundVolume = 0.75f;

    [Tooltip("Randomized pitch range for sound variation.")]
    [SerializeField, Range(0.5f, 2f)] private float minPitch = 0.95f;
    [SerializeField, Range(0.5f, 2f)] private float maxPitch = 1.05f;

    [Header("Debug")]
    [Tooltip("Print informative console logs when entities enter trigger or state changes.")]
    [SerializeField] private bool debugLogs = true;

    // Public State & Events
    public bool IsOpen => IsNetworkedSession ? isDoorOpenNetVar.Value : isLocallyOpen;
    public bool HasConnectedRoom { get; private set; } = true;
    public event System.Action OnDoorOpened;
    public event System.Action OnDoorClosed;

    // Network State
    private readonly NetworkVariable<bool> isDoorOpenNetVar = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private bool IsNetworkedSession => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && IsSpawned;

    // Local Movement State
    private bool isLocallyOpen = false;
    private Vector3 primaryClosedPos;
    private Vector3 primaryOpenPos;
    private Vector3 secondaryClosedPos;
    private Vector3 secondaryOpenPos;
    private float currentAnimProgress = 0f; // 0 = fully closed, 1 = fully open

    // Trigger & Proximity Tracking
    private readonly HashSet<Collider> occupantsInTrigger = new HashSet<Collider>();
    private Coroutine closeTimerCoroutine;
    private readonly Collider[] proximityHitBuffer = new Collider[16];
    private float proximityCheckTimer = 0f;
    private const float ProximityCheckInterval = 0.05f; // Active check 20x per second for instant detection

    private void Awake()
    {
        InitializeDoorPanels();
        InitializeAudioSource();
        EnsureTriggerCollider();
        EnsureKinematicRigidbody();
        ConfigureNavMeshExclusion();
        ResolveDoorAnchor();
    }

    private void Start()
    {
        CalculateTargetPositions();
        CheckRoomConnection();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        isDoorOpenNetVar.OnValueChanged += HandleNetworkDoorStateChanged;

        // Sync initial state
        if (isDoorOpenNetVar.Value)
        {
            currentAnimProgress = 1f;
            ApplyPositions(1f);
        }
        else
        {
            currentAnimProgress = 0f;
            ApplyPositions(0f);
        }
    }

    public override void OnNetworkDespawn()
    {
        isDoorOpenNetVar.OnValueChanged -= HandleNetworkDoorStateChanged;
        base.OnNetworkDespawn();
    }

    private void InitializeDoorPanels()
    {
        if (doorPanel == null)
        {
            // Search children for a visual door panel mesh
            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child == null || child == transform) continue;

                string lower = child.name.ToLower();
                if (lower.Contains("door") || lower.Contains("panel") || lower.Contains("mesh") || lower.Contains("slide") || lower.Contains("gate"))
                {
                    doorPanel = child;
                    break;
                }
            }

            if (doorPanel == null)
            {
                doorPanel = transform;
            }
        }

        if (useLocalSpace)
        {
            primaryClosedPos = doorPanel.localPosition;
            if (secondaryDoorPanel != null)
            {
                secondaryClosedPos = secondaryDoorPanel.localPosition;
            }
        }
        else
        {
            primaryClosedPos = doorPanel.position;
            if (secondaryDoorPanel != null)
            {
                secondaryClosedPos = secondaryDoorPanel.position;
            }
        }

        CalculateTargetPositions();
    }

    private void CalculateTargetPositions()
    {
        if (doorPanel == null) return;

        if (useLocalSpace)
        {
            primaryOpenPos = primaryClosedPos + slideOffset;
            if (secondaryDoorPanel != null)
            {
                secondaryOpenPos = secondaryClosedPos + secondarySlideOffset;
            }
        }
        else
        {
            primaryOpenPos = primaryClosedPos + slideOffset;
            if (secondaryDoorPanel != null)
            {
                secondaryOpenPos = secondaryClosedPos + secondarySlideOffset;
            }
        }
    }

    private void InitializeAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1.0f; // 3D sound
            audioSource.minDistance = 2f;
            audioSource.maxDistance = 25f;
            audioSource.playOnAwake = false;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
        }
    }

    private void EnsureTriggerCollider()
    {
        if (triggerCollider == null)
        {
            // Check locally and on child objects for an existing trigger collider
            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null && colliders[i].isTrigger)
                {
                    triggerCollider = colliders[i];
                    break;
                }
            }
        }

        if (triggerCollider == null)
        {
            // Add a dedicated trigger BoxCollider
            BoxCollider box = GetComponent<BoxCollider>();
            if (box == null)
            {
                box = gameObject.AddComponent<BoxCollider>();
            }
            box.isTrigger = true;
            box.size = triggerBoxSize;
            box.center = triggerBoxCenter;
            triggerCollider = box;
        }
        else
        {
            // Ensure collider is marked as trigger
            triggerCollider.isTrigger = true;
        }

        // If trigger collider is on a child object, attach proxy to forward events
        if (triggerCollider.gameObject != gameObject)
        {
            SlidingDoorTriggerProxy proxy = triggerCollider.GetComponent<SlidingDoorTriggerProxy>();
            if (proxy == null)
            {
                proxy = triggerCollider.gameObject.AddComponent<SlidingDoorTriggerProxy>();
            }
            proxy.parentDoor = this;
        }
    }

    private void EnsureKinematicRigidbody()
    {
        // A kinematic Rigidbody on the trigger GameObject ensures PhysX reliably dispatches OnTriggerEnter
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    public void ConfigureNavMeshExclusion()
    {
        if (!ignoreFromNavMeshBuild) return;

        Transform[] panels = new Transform[] { doorPanel, secondaryDoorPanel };
        for (int i = 0; i < panels.Length; i++)
        {
            Transform p = panels[i];
            if (p == null) continue;

            NavMeshModifier modifier = p.GetComponent<NavMeshModifier>();
            if (modifier == null)
            {
                modifier = p.gameObject.AddComponent<NavMeshModifier>();
            }
            modifier.overrideArea = false;
            modifier.ignoreFromBuild = true;

            Collider[] colliders = p.GetComponentsInChildren<Collider>(true);
            for (int c = 0; c < colliders.Length; c++)
            {
                if (colliders[c] == null || colliders[c] == triggerCollider) continue;

                NavMeshModifier childMod = colliders[c].GetComponent<NavMeshModifier>();
                if (childMod == null)
                {
                    childMod = colliders[c].gameObject.AddComponent<NavMeshModifier>();
                }
                childMod.overrideArea = false;
                childMod.ignoreFromBuild = true;
            }
        }
    }

    private void Update()
    {
        UpdateDoorMotion();
    }

    private void FixedUpdate()
    {
        PerformProximityOverlapCheck();
    }

    private void PerformProximityOverlapCheck()
    {
        proximityCheckTimer += Time.fixedDeltaTime;
        if (proximityCheckTimer < ProximityCheckInterval) return;
        proximityCheckTimer = 0f;

        Vector3 center;
        Vector3 halfExtents;
        Quaternion orientation = transform.rotation;

        if (triggerCollider is BoxCollider box)
        {
            center = box.transform.TransformPoint(box.center);
            Vector3 lossy = box.transform.lossyScale;
            halfExtents = Vector3.Scale(box.size * 0.5f, new Vector3(Mathf.Abs(lossy.x), Mathf.Abs(lossy.y), Mathf.Abs(lossy.z)));
            orientation = box.transform.rotation;
        }
        else if (triggerCollider is SphereCollider sphere)
        {
            center = sphere.transform.TransformPoint(sphere.center);
            float radius = sphere.radius * Mathf.Max(sphere.transform.lossyScale.x, Mathf.Max(sphere.transform.lossyScale.y, sphere.transform.lossyScale.z));
            halfExtents = Vector3.one * radius;
        }
        else
        {
            center = transform.TransformPoint(triggerBoxCenter);
            halfExtents = triggerBoxSize * 0.5f;
        }

        int hitCount = Physics.OverlapBoxNonAlloc(center, halfExtents, proximityHitBuffer, orientation, triggerLayers, QueryTriggerInteraction.Ignore);
        bool hasValidOccupant = false;

        for (int i = 0; i < hitCount; i++)
        {
            Collider col = proximityHitBuffer[i];
            if (col == null || col == triggerCollider || col.transform.IsChildOf(transform)) continue;

            if (IsValidTriggerCollider(col))
            {
                hasValidOccupant = true;
                if (!occupantsInTrigger.Contains(col))
                {
                    OnTriggerEnter(col);
                }
            }
        }

        if (!hasValidOccupant && occupantsInTrigger.Count > 0)
        {
            List<Collider> toRemove = new List<Collider>(occupantsInTrigger);
            for (int i = 0; i < toRemove.Count; i++)
            {
                OnTriggerExit(toRemove[i]);
            }
        }
    }

    private void UpdateDoorMotion()
    {
        bool targetOpen = IsNetworkedSession ? isDoorOpenNetVar.Value : isLocallyOpen;
        float targetProgress = targetOpen ? 1f : 0f;

        if (!Mathf.Approximately(currentAnimProgress, targetProgress))
        {
            float offsetMagnitude = Mathf.Max(slideOffset.magnitude, 0.01f);
            float step = (slideSpeed / offsetMagnitude) * Time.deltaTime;
            currentAnimProgress = Mathf.MoveTowards(currentAnimProgress, targetProgress, step);

            float curveValue = movementCurve.Evaluate(currentAnimProgress);
            ApplyPositions(curveValue);
        }
    }

    private void ApplyPositions(float t)
    {
        if (doorPanel != null)
        {
            Vector3 targetPrimary = Vector3.Lerp(primaryClosedPos, primaryOpenPos, t);
            if (useLocalSpace)
            {
                doorPanel.localPosition = targetPrimary;
            }
            else
            {
                doorPanel.position = targetPrimary;
            }
        }

        if (secondaryDoorPanel != null)
        {
            Vector3 targetSecondary = Vector3.Lerp(secondaryClosedPos, secondaryOpenPos, t);
            if (useLocalSpace)
            {
                secondaryDoorPanel.localPosition = targetSecondary;
            }
            else
            {
                secondaryDoorPanel.position = targetSecondary;
            }
        }
    }

    #region Room Connection & Anchor Verification

    private void ResolveDoorAnchor()
    {
        if (doorAnchor != null) return;

        // 1. Check this GameObject
        try
        {
            if (CompareTag(doorTag))
            {
                doorAnchor = transform;
                return;
            }
        }
        catch (UnityException) { }

        // 2. Check parent / room root for child with doorTag
        Transform root = transform.root;
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        float closestDist = float.MaxValue;
        Transform bestAnchor = null;

        for (int i = 0; i < children.Length; i++)
        {
            Transform t = children[i];
            if (t == null) continue;

            bool isAnchor = false;
            try
            {
                if (t.CompareTag(doorTag)) isAnchor = true;
            }
            catch (UnityException) { }

            if (!isAnchor && t.name.IndexOf("anchor", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                isAnchor = true;
            }

            if (isAnchor)
            {
                float d = Vector3.Distance(transform.position, t.position);
                if (d < closestDist)
                {
                    closestDist = d;
                    bestAnchor = t;
                }
            }
        }

        doorAnchor = bestAnchor != null ? bestAnchor : transform;
    }

    public bool CheckRoomConnection()
    {
        if (!requireConnectedRoom)
        {
            HasConnectedRoom = true;
            return true;
        }

        // Check if DungeonGenerator exists and has generated rooms
        DungeonGenerator generator = FindFirstObjectByType<DungeonGenerator>();
        if (generator == null || generator.SpawnedRooms == null || generator.SpawnedRooms.Count <= 1)
        {
            // Standalone scene or single test room: permit door to open
            HasConnectedRoom = true;
            return true;
        }

        ResolveDoorAnchor();
        Transform anchor = doorAnchor != null ? doorAnchor : transform;
        Transform myRoot = transform.root;

        // 1. Check for overlapping door anchor from another room in the generated dungeon
        List<GameObject> rooms = generator.SpawnedRooms;
        for (int r = 0; r < rooms.Count; r++)
        {
            GameObject otherRoom = rooms[r];
            if (otherRoom == null || otherRoom.transform == myRoot) continue;

            Transform[] otherAnchors = otherRoom.GetComponentsInChildren<Transform>(true);
            for (int a = 0; a < otherAnchors.Length; a++)
            {
                Transform otherAnchor = otherAnchors[a];
                if (otherAnchor == null) continue;

                bool isAnchor = false;
                try
                {
                    if (otherAnchor.CompareTag(doorTag)) isAnchor = true;
                }
                catch (UnityException) { }

                if (!isAnchor && otherAnchor.name.IndexOf("anchor", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    isAnchor = true;
                }

                if (isAnchor)
                {
                    float dist = Vector3.Distance(anchor.position, otherAnchor.position);
                    if (dist <= anchorConnectionDistance)
                    {
                        HasConnectedRoom = true;
                        return true;
                    }
                }
            }
        }

        // 2. Also check all scene objects tagged with doorTag
        GameObject[] allDoorAnchors = null;
        try
        {
            allDoorAnchors = GameObject.FindGameObjectsWithTag(doorTag);
        }
        catch (UnityException) { }

        if (allDoorAnchors != null)
        {
            for (int i = 0; i < allDoorAnchors.Length; i++)
            {
                GameObject otherObj = allDoorAnchors[i];
                if (otherObj == null) continue;

                Transform otherTransform = otherObj.transform;
                if (otherTransform == anchor || otherTransform.IsChildOf(myRoot))
                {
                    continue; // Belongs to this same room
                }

                float dist = Vector3.Distance(anchor.position, otherTransform.position);
                if (dist <= anchorConnectionDistance)
                {
                    HasConnectedRoom = true;
                    return true;
                }
            }
        }

        // 3. Check if a floor / ground collider from another room exists just beyond the doorway
        if (checkFloorBeyondDoor)
        {
            Vector3 checkPos = anchor.position + anchor.forward * floorCheckDistance + Vector3.up * 1f;
            if (Physics.Raycast(checkPos, Vector3.down, out RaycastHit hit, 4f, floorLayerMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider != null && !hit.collider.transform.IsChildOf(myRoot))
                {
                    HasConnectedRoom = true;
                    return true;
                }
            }
        }

        HasConnectedRoom = false;
        return false;
    }

    #endregion

    #region Trigger Area Handling

    public void OnTriggerEnter(Collider other)
    {
        

        if (!IsValidTriggerCollider(other))
        {
            if (debugLogs) Debug.Log($"<color=#FF5555>[SlidingDoor]</color> Ignored '{other.name}' (tag: {other.tag}, layer: {LayerMask.LayerToName(other.gameObject.layer)}) - did not match target tags/layers.");
            return;
        }

        // If this door requires a connected room and there is no room attached, do not open
        if (requireConnectedRoom && !CheckRoomConnection())
        {
            if (debugLogs) Debug.Log($"<color=#FFAA00>[SlidingDoor]</color> Trigger entered by '{other.name}', but no connected room is attached to this anchor.");
            return;
        }

        occupantsInTrigger.Add(other);

        if (closeTimerCoroutine != null)
        {
            StopCoroutine(closeTimerCoroutine);
            closeTimerCoroutine = null;
        }

        if (debugLogs) Debug.Log($"<color=#00FF66>[SlidingDoor]</color> Opening door! Triggered by '{other.name}'.");
        SetDoorState(true);
    }

    public void OnTriggerExit(Collider other)
    {
        if (occupantsInTrigger.Contains(other))
        {
            occupantsInTrigger.Remove(other);
        }

        // Clean up any destroyed colliders
        occupantsInTrigger.RemoveWhere(col => col == null);

        if (occupantsInTrigger.Count == 0)
        {
            if (autoCloseDelay > 0f)
            {
                if (closeTimerCoroutine != null) StopCoroutine(closeTimerCoroutine);
                closeTimerCoroutine = StartCoroutine(AutoCloseRoutine());
            }
            else
            {
                SetDoorState(false);
            }
        }
    }

    private IEnumerator AutoCloseRoutine()
    {
        yield return new WaitForSeconds(autoCloseDelay);

        occupantsInTrigger.RemoveWhere(col => col == null);
        if (occupantsInTrigger.Count == 0)
        {
            if (debugLogs) Debug.Log("<color=#FFAA00>[SlidingDoor]</color> Closing door after auto-close delay.");
            SetDoorState(false);
        }
    }

    private bool IsValidTriggerCollider(Collider col)
    {
        if (col == null || col.isTrigger) return false;

        // Ignore self and child colliders of this door
        if (col.transform.IsChildOf(transform)) return false;

        // Check Layer
        if ((triggerLayers.value & (1 << col.gameObject.layer)) == 0)
        {
            return false;
        }

        // Check if this collider belongs to a Player
        if (col.GetComponentInParent<PlayerController>() != null || 
            col.GetComponent<PlayerController>() != null ||
            col.CompareTag("Player") || 
            (col.transform.root != null && col.transform.root.CompareTag("Player")))
        {
            return true;
        }

        // Check Tags
        if (targetTags != null && targetTags.Count > 0)
        {
            for (int i = 0; i < targetTags.Count; i++)
            {
                string tag = targetTags[i];
                if (string.IsNullOrEmpty(tag)) continue;

                if (col.CompareTag(tag) || 
                    (col.transform.parent != null && col.transform.parent.CompareTag(tag)) || 
                    (col.transform.root != null && col.transform.root.CompareTag(tag)))
                {
                    return true;
                }
            }
            return false;
        }

        return true;
    }

    #endregion

    #region State & Audio

    public void SetDoorState(bool open)
    {
        // If networked, request server or execute on server
        if (IsNetworkedSession)
        {
            if (IsServer)
            {
                if (isDoorOpenNetVar.Value != open)
                {
                    isDoorOpenNetVar.Value = open;
                }
            }
            else
            {
                RequestDoorStateServerRpc(open);
            }
        }
        else
        {
            if (isLocallyOpen != open)
            {
                isLocallyOpen = open;
                PlayDoorSound(open);

                if (open) OnDoorOpened?.Invoke();
                else OnDoorClosed?.Invoke();
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestDoorStateServerRpc(bool open)
    {
        if (!IsServer) return;
        if (isDoorOpenNetVar.Value != open)
        {
            isDoorOpenNetVar.Value = open;
        }
    }

    private void HandleNetworkDoorStateChanged(bool previousValue, bool newValue)
    {
        PlayDoorSound(newValue);

        if (newValue) OnDoorOpened?.Invoke();
        else OnDoorClosed?.Invoke();
    }

    private void PlayDoorSound(bool opening)
    {
        AudioClip clipToPlay = opening ? openSound : closeSound;
        if (clipToPlay == null) return;

        if (audioSource != null)
        {
            audioSource.pitch = Random.Range(minPitch, maxPitch);
            audioSource.PlayOneShot(clipToPlay, soundVolume);
        }
        else
        {
            AudioSource.PlayClipAtPoint(clipToPlay, transform.position, soundVolume);
        }
    }

    #endregion

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Visualize open position in scene view
        Vector3 start = doorPanel != null ? doorPanel.position : transform.position;
        Vector3 end = start + (useLocalSpace ? transform.TransformDirection(slideOffset) : slideOffset);

        Gizmos.color = (requireConnectedRoom && !HasConnectedRoom) ? Color.red : Color.green;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawWireCube(end, Vector3.one * 0.3f);

        if (secondaryDoorPanel != null)
        {
            Vector3 secStart = secondaryDoorPanel.position;
            Vector3 secEnd = secStart + (useLocalSpace ? transform.TransformDirection(secondarySlideOffset) : secondarySlideOffset);

            Gizmos.color = (requireConnectedRoom && !HasConnectedRoom) ? Color.red : Color.cyan;
            Gizmos.DrawLine(secStart, secEnd);
            Gizmos.DrawWireCube(secEnd, Vector3.one * 0.3f);
        }

        // Draw trigger area box
        Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.25f);
        Gizmos.DrawWireCube(transform.TransformPoint(triggerBoxCenter), triggerBoxSize);

        // Draw anchor connection radius
        Transform anchor = doorAnchor != null ? doorAnchor : transform;
        if (anchor != null)
        {
            Gizmos.color = HasConnectedRoom ? new Color(0f, 1f, 0f, 0.4f) : new Color(1f, 0f, 0f, 0.4f);
            Gizmos.DrawWireSphere(anchor.position, anchorConnectionDistance);
        }
    }
#endif
}

public class SlidingDoorTriggerProxy : MonoBehaviour
{
    public SlidingDoor parentDoor;

    private void OnTriggerEnter(Collider other)
    {
        if (parentDoor != null) parentDoor.OnTriggerEnter(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (parentDoor != null) parentDoor.OnTriggerExit(other);
    }
}
