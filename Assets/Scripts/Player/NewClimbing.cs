using UnityEngine;
using Unity.Netcode;

public class NewClimbing : NetworkBehaviour
{
    Vector3 contactNormal, climbNormal, steepNormal;
    int groundContactCount, climbContactCount, steepContactCount;
    float minGroundDotProduct;
    float minClimbDotProduct;
    public NetworkTetherSystem NetworkTetherSystem;

    [Header("Death & Revive Settings")]
    [Tooltip("Prefab spawned on death. Must have NetworkObject and DeadPlayer scripts attached.")]
    public GameObject deadPlayerPrefab;

    [Header("Player Stats")]
    public float MaxHealth = 100f;
    public float Health = 100f;
    public float stamina = 100f;
    public float maxStamina = 100f;
    public float staminaRegenRate = 5f;
    public float Strength = 1f;
    public float gainMultiplier = 1f;
    public float Money = 0f;

    [Header("Movement Settings")]
    [SerializeField, Range(0f, 100f)]
    float maxAcceleration = 10f, maxAirAcceleration = 1f, maxClimbAcceleration = 20f;

    [SerializeField, Range(0f, 100f)]
    float maxSpeed = 10f, maxClimbSpeed = 4f;
    public float sprintSpeed = 15f;
    public float walkSpeed = 10f;
    public float minMoveSpeed = 1f;

    [SerializeField] float gravityMultiplier = 2.5f;

    [Header("Climbing & Slope")]
    [SerializeField, Range(90, 180)]
    float maxClimbAngle = 140f;
    [SerializeField, Range(0f, 90f)]
    float maxGroundAngle = 25f;

    [Header("Jumping")]
    [SerializeField, Range(0f, 10f)]
    float jumpHeight = 2f;
    [SerializeField, Range(0, 5)]
    int maxAirJumps = 0;

    [Header("Detection")]
    [SerializeField, Range(0f, 100f)]
    float maxSnapSpeed = 100f;
    [SerializeField, Min(0f)]
    float probeDistance = 1f;
    [SerializeField]
    LayerMask probeMask = -1;
    [SerializeField]
    LayerMask climbMask = -1;

    [SerializeField]
    Transform playerInputSpace = default;

    int jumpPhase;
    private bool isAlive = true;
    public bool IsAlive => isAlive;
    private bool sprinting;
    Rigidbody body, connectedBody, previousConnectedBody;
    Vector3 upAxis, rightAxis, forwardAxis;
    bool desiredJump;
    bool onGround => groundContactCount > 0;
    Vector2 playerInput;
    Vector3 velocity, connectionVelocity;
    Vector3 connectionWorldPosition;

    bool Climbing => climbContactCount > 0 && stepsSinceLastJump > 2;
    bool OnSteep => steepContactCount > 0;
    int stepsSinceLastGrounded, stepsSinceLastJump;

    void OnValidate()
    {
        minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
        minClimbDotProduct = Mathf.Cos(maxClimbAngle * Mathf.Deg2Rad);
    }

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.constraints = RigidbodyConstraints.FreezeRotation;
        body.useGravity = false;
        OnValidate();
    }

    void Start()
    {
        maxSpeed = walkSpeed;
        maxAcceleration = walkSpeed;
        stamina = maxStamina;
        Health = MaxHealth;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner)
        {
            if (MobileMotionManager.Instance == null)
            {
                GameObject motionObj = new GameObject("MobileMotionManager", typeof(MobileMotionManager));
                DontDestroyOnLoad(motionObj);
            }
            if (MobileHUD.Instance == null)
            {
                GameObject hudObj = new GameObject("MobileHUD", typeof(MobileHUD));
                DontDestroyOnLoad(hudObj);
            }
            if (MobileHUD.Instance != null)
            {
                MobileHUD.Instance.SetVisible(true);
            }
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        if (Health <= 0 && isAlive)
        {
            Die();
            return;
        }

        if (!isAlive) return;

        if (MobileMotionManager.Instance != null)
        {
            playerInput = MobileMotionManager.Instance.GetMoveInput();
        }
        else
        {
            playerInput.x = Input.GetAxisRaw("Horizontal");
            playerInput.y = Input.GetAxisRaw("Vertical");
            playerInput = Vector2.ClampMagnitude(playerInput, 1f);
        }

        if (playerInputSpace)
        {
            rightAxis = ProjectDirectionOnPlane(playerInputSpace.right, upAxis);
            forwardAxis = ProjectDirectionOnPlane(playerInputSpace.forward, upAxis);
        }
        else
        {
            rightAxis = ProjectDirectionOnPlane(Vector3.right, upAxis);
            forwardAxis = ProjectDirectionOnPlane(Vector3.forward, upAxis);
        }

        bool jumpPressed = MobileMotionManager.Instance != null ? MobileMotionManager.Instance.IsJumpTriggered() : (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space));
        if (jumpPressed)
        {
            desiredJump = true;
        }

        bool sprintRequested = MobileMotionManager.Instance != null ? MobileMotionManager.Instance.IsSprintActive() : Input.GetKey(KeyCode.LeftShift);
        if (sprintRequested && stamina > 0)
        {
            sprinting = true;
        }
        else if (!sprintRequested || stamina <= 0)
        {
            sprinting = false;
        }

        if (sprinting)
        {
            stamina -= Time.deltaTime * 10f;
        }
        else if (stamina < maxStamina)
        {
            stamina += Time.deltaTime * staminaRegenRate;
        }
        UpdateSpeed();
    }

    public void Die()
    {
        if (IsServer)
        {
            HandleDeathServer();
        }
        else
        {
            DieServerRpc();
        }
    }

    [ServerRpc]
    private void DieServerRpc()
    {
        HandleDeathServer();
    }

    public void TakeDamage(float damage)
    {
        if (damage <= 0f) return;

        if (IsServer)
        {
            ApplyDamage(damage);
        }
        else
        {
            TakeDamageServerRpc(damage);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void TakeDamageServerRpc(float damage)
    {
        ApplyDamage(damage);
    }

    private void ApplyDamage(float damage)
    {
        if (!IsServer || !isAlive) return;

        Health = Mathf.Max(0f, Health - damage);
        TakeDamageClientRpc(Health);

        if (Health <= 0f)
        {
            HandleDeathServer();
        }
    }

    [ClientRpc]
    private void TakeDamageClientRpc(float newHealth)
    {
        Health = newHealth;
    }

    private void HandleDeathServer()
    {
        if (!IsServer || !isAlive) return;

        isAlive = false;
        Health = 0f;

        Vector3 spawnPos = transform.position + Vector3.up * 0.5f;

        if (deadPlayerPrefab != null)
        {
            GameObject corpseObj = Instantiate(deadPlayerPrefab, spawnPos, Quaternion.identity);

            if (corpseObj.TryGetComponent(out NetworkObject netObj))
            {
                netObj.Spawn();
            }

            if (corpseObj.TryGetComponent(out DeadPlayer deadComp))
            {
                deadComp.Initialize(OwnerClientId);
            }
        }
        else
        {
            Debug.LogError("deadPlayerPrefab is not assigned on NewClimbing!");
        }

        DeathClientRpc();
    }

    [ClientRpc]
    private void DeathClientRpc()
    {
        isAlive = false;
        Health = 0f;

        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
        }

        // Fully disable the player prefab GameObject
        gameObject.SetActive(false);
    }

    public void OnRevived(Vector3 revivePosition)
    {
        isAlive = true;
        Health = 20f; // Revive with 20 health
        stamina = maxStamina;

        // Teleport to corpse position
        if (body != null)
        {
            body.position = revivePosition;
            body.linearVelocity = Vector3.zero;
        }
        transform.position = revivePosition;

        this.enabled = true;
    }


    void UpdateSpeed()
    {
        float targetBaseSpeed = sprinting ? sprintSpeed : walkSpeed;
        float totalWeight = GetTotalTetherWeight();
        float effectiveSpeed = Mathf.Max(minMoveSpeed, targetBaseSpeed - (totalWeight / Strength));

        maxSpeed = effectiveSpeed;
        maxAcceleration = effectiveSpeed;
    }

    float GetTotalTetherWeight()
    {
        if (NetworkTetherSystem == null) return 0f;

        float weight = 0f;
        for (int i = 0; i < NetworkTetherSystem.activeTethers.Count; i++)
        {
            var tether = NetworkTetherSystem.activeTethers[i];
            if (tether != null && tether.target != null)
            {
                if (tether.target.TryGetComponent<Item>(out Item item))
                {
                    weight += item.NetHeavy.Value;
                }
                else if (tether.target.TryGetComponent<DeadPlayer>(out _))
                {
                    weight += 2f;
                }
            }
        }
        return weight;
    }

    void FixedUpdate()
    {
        if (!IsOwner || !isAlive) return;

        upAxis = -Physics.gravity.normalized;
        UpdateState();
        AdjustVelocity();

        if (desiredJump)
        {
            desiredJump = false;
            Jump();
        }

        if (Climbing)
        {
            velocity -= contactNormal * (maxClimbAcceleration * 3.0f * Time.deltaTime);
        }
        else if (onGround && velocity.sqrMagnitude < 0.01f)
        {
            velocity += contactNormal * (Vector3.Dot(Physics.gravity, contactNormal) * Time.deltaTime);
        }
        else
        {
            velocity += Physics.gravity * gravityMultiplier * Time.deltaTime;
        }

        body.linearVelocity = velocity;
        ClearState();
    }

    void AdjustVelocity()
    {
        float acceleration, speed;
        Vector3 xAxis, zAxis;

        if (Climbing)
        {
            acceleration = maxClimbAcceleration;
            speed = maxClimbSpeed;
            xAxis = rightAxis;
            zAxis = Vector3.Cross(rightAxis, contactNormal);
        }
        else
        {
            acceleration = onGround ? maxAcceleration : maxAirAcceleration;
            speed = maxSpeed;
            xAxis = rightAxis;
            zAxis = forwardAxis;
        }

        xAxis = ProjectDirectionOnPlane(xAxis, contactNormal);
        zAxis = ProjectDirectionOnPlane(zAxis, contactNormal);

        Vector3 relativeVelocity = velocity - connectionVelocity;
        float currentX = Vector3.Dot(relativeVelocity, xAxis);
        float currentZ = Vector3.Dot(relativeVelocity, zAxis);

        float maxSpeedChange = acceleration * Time.deltaTime;

        float newX = Mathf.MoveTowards(currentX, playerInput.x * speed, maxSpeedChange);
        float newZ = Mathf.MoveTowards(currentZ, playerInput.y * speed, maxSpeedChange);

        velocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ);
    }

    void UpdateState()
    {
        if (!IsOwner) return;
        stepsSinceLastGrounded += 1;
        stepsSinceLastJump += 1;
        velocity = body.linearVelocity;

        if (CheckClimbing() || onGround || SnapToGround() || CheckSteepContacts())
        {
            stepsSinceLastGrounded = 0;
            if (stepsSinceLastJump > 1) jumpPhase = 0;
            if (groundContactCount > 1) contactNormal.Normalize();
        }
        else
        {
            contactNormal = upAxis;
        }

        if (connectedBody && (connectedBody.isKinematic || connectedBody.mass >= body.mass))
        {
            UpdateConnectionState();
        }
    }

    void Jump()
    {
        if (!IsOwner) return;
        Vector3 jumpDirection;
        if (onGround) jumpDirection = contactNormal;
        else if (OnSteep) { jumpDirection = steepNormal; jumpPhase = 0; }
        else if (maxAirJumps > 0 && jumpPhase <= maxAirJumps)
        {
            if (jumpPhase == 0) jumpPhase = 1;
            jumpDirection = contactNormal;
        }
        else return;

        stepsSinceLastJump = 0;
        jumpPhase += 1;
        float jumpSpeed = Mathf.Sqrt(2f * Physics.gravity.magnitude * jumpHeight);
        jumpDirection = (jumpDirection + upAxis).normalized;

        float alignedSpeed = Vector3.Dot(velocity, jumpDirection);
        if (alignedSpeed > 0f) jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);

        velocity += jumpDirection * jumpSpeed;
    }

    bool SnapToGround()
    {
        if (stepsSinceLastGrounded > 1 || stepsSinceLastJump <= 2) return false;
        if (velocity.magnitude > maxSnapSpeed) return false;
        if (!Physics.Raycast(body.position, -upAxis, out RaycastHit hit, probeDistance, probeMask)) return false;
        if (Vector3.Dot(upAxis, hit.normal) < minGroundDotProduct) return false;

        groundContactCount = 1;
        contactNormal = hit.normal;
        float dot = Vector3.Dot(velocity, hit.normal);
        if (dot > 0f) velocity = (velocity - hit.normal * dot).normalized * velocity.magnitude;
        connectedBody = hit.rigidbody;
        return true;
    }

    void UpdateConnectionState()
    {
        if (connectedBody == previousConnectedBody)
        {
            connectionVelocity = (connectedBody.position - connectionWorldPosition) / Time.deltaTime;
        }
        connectionWorldPosition = connectedBody.position;
        previousConnectedBody = connectedBody;
    }

    bool CheckClimbing()
    {
        if (Climbing)
        {
            if (climbContactCount > 1) { climbNormal.Normalize(); }
            groundContactCount = climbContactCount;
            contactNormal = climbNormal;
            return true;
        }
        return false;
    }

    bool CheckSteepContacts()
    {
        if (steepContactCount > 1)
        {
            steepNormal.Normalize();
            if (Vector3.Dot(upAxis, steepNormal) >= minGroundDotProduct)
            {
                groundContactCount = 1;
                contactNormal = steepNormal;
                return true;
            }
        }
        return false;
    }

    void EvaluateCollision(Collision collision)
    {
        bool isClimbableLayer = (climbMask & (1 << collision.gameObject.layer)) != 0;

        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;
            float upDot = Vector3.Dot(upAxis, normal);

            if (upDot >= minGroundDotProduct)
            {
                groundContactCount += 1;
                if (upDot > Vector3.Dot(upAxis, contactNormal))
                {
                    contactNormal = normal;
                }
                connectedBody = collision.rigidbody;
            }
            else
            {
                if (isClimbableLayer && upDot >= minClimbDotProduct)
                {
                    climbContactCount += 1;
                    climbNormal = normal;
                }
            }
        }
    }

    void OnCollisionEnter(Collision c) => EvaluateCollision(c);
    void OnCollisionStay(Collision c) => EvaluateCollision(c);

    void ClearState()
    {
        groundContactCount = climbContactCount = steepContactCount = 0;
        contactNormal = climbNormal = steepNormal = connectionVelocity = Vector3.zero;
        connectedBody = null;
    }

    Vector3 ProjectDirectionOnPlane(Vector3 direction, Vector3 normal) =>
        (direction - normal * Vector3.Dot(direction, normal)).normalized;
}