using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class PlayerController : NetworkBehaviour
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

    [Header("Health & Stamina UI")]
    [Tooltip("UI Slider displaying player health.")]
    [SerializeField] private Slider healthSlider;

    [Tooltip("UI Slider displaying player stamina.")]
    [SerializeField] private Slider staminaSlider;

    [Tooltip("UI Text label displaying current and max health numbers.")]
    [SerializeField] private TMP_Text healthText;

    [Tooltip("UI Text label displaying current and max stamina numbers.")]
    [SerializeField] private TMP_Text staminaText;

    [Tooltip("UI Image fill displaying player health (alternative to Slider).")]
    [SerializeField] private Image healthFillImage;

    [Tooltip("UI Image fill displaying player stamina (alternative to Slider).")]
    [SerializeField] private Image staminaFillImage;
    [SerializeField] private TMP_Text moneyText;

    public Slider HealthSlider { get => healthSlider; set => healthSlider = value; }
    public Slider StaminaSlider { get => staminaSlider; set => staminaSlider = value; }
    public TMP_Text HealthText { get => healthText; set => healthText = value; }
    public TMP_Text StaminaText { get => staminaText; set => staminaText = value; }
    public Image HealthFillImage { get => healthFillImage; set => healthFillImage = value; }
    public Image StaminaFillImage { get => staminaFillImage; set => staminaFillImage = value; }
    public TMP_Text MoneyText { get => moneyText; set => moneyText = value; }

    public event System.Action<float, float> OnHealthChanged;
    public event System.Action<float, float> OnStaminaChanged;
    public AudioSource audioSource;
    public AudioClip audio;
    [SerializeField, Range(0f, 1f)] private float soundVolume = 0.75f;

    [Header("Movement Settings")]
    [SerializeField, Range(0f, 100f)]
    float maxAcceleration = 10f, maxAirAcceleration = 5f, maxClimbAcceleration = 20f;

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

    [Header("Jumping & Momentum")]
    [SerializeField, Range(0f, 10f)]
    float jumpHeight = 2f;
    [SerializeField, Range(0, 5)]
    int maxAirJumps = 0;
    [SerializeField, Range(0, 10)]
    int coyoteSteps = 4;
    [SerializeField, Range(0, 20)]
    int jumpSnapSuppressSteps = 10;

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

    [Header("Gravity Control")]
    [SerializeField] private bool gravityEnabled = true;
    public bool GravityEnabled => gravityEnabled;

    public void SetGravityEnabled(bool enabled)
    {
        gravityEnabled = enabled;
        if (!enabled)
        {
            velocity = Vector3.zero;
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }
    }

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
        if (body != null)
        {
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.useGravity = false;
        }
        if (NetworkTetherSystem == null)
        {
            NetworkTetherSystem = GetComponent<NetworkTetherSystem>();
        }
        OnValidate();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner)
        {
            AutoBindUI();
            UpdateHealthUI();
            UpdateStaminaUI();
        }
        else
        {
            DisableRemoteUI();
        }
    }

    void Start()
    {
        maxSpeed = walkSpeed;
        maxAcceleration = walkSpeed;
        stamina = maxStamina;
        Health = MaxHealth;

        if (IsOwner)
        {
            AutoBindUI();
            UpdateHealthUI();
            UpdateStaminaUI();
        }
        else
        {
            DisableRemoteUI();
        }
    }

    private void DisableRemoteUI()
    {
        // Deactivate all Screen-Space Overlay Canvases on remote clones so they don't overlay the local player's HUD
        Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas c = canvases[i];
            if (c != null && c.renderMode != RenderMode.WorldSpace)
            {
                c.gameObject.SetActive(false);
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

        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        // Mobile orientation / tilt input support
        if (MobileInputManager.Instance != null && MobileInputManager.Instance.IsActive)
        {
            Vector2 mobileMove = MobileInputManager.Instance.GetMovementInput();
            if (mobileMove.sqrMagnitude > 0.001f || Application.isMobilePlatform)
            {
                input = mobileMove;
            }
        }

        playerInput = Vector2.ClampMagnitude(input, 1f);

        if (playerInputSpace != null)
        {
            rightAxis = ProjectDirectionOnPlane(playerInputSpace.right, upAxis);
            forwardAxis = ProjectDirectionOnPlane(playerInputSpace.forward, upAxis);
        }
        else
        {
            rightAxis = ProjectDirectionOnPlane(Vector3.right, upAxis);
            forwardAxis = ProjectDirectionOnPlane(Vector3.forward, upAxis);
        }

        bool jumpPressed = Input.GetButtonDown("Jump");
        if (MobileInputManager.Instance != null && MobileInputManager.Instance.ConsumeJump())
        {
            jumpPressed = true;
        }

        if (jumpPressed)
        {
            desiredJump = true;
        }

        bool sprintRequested = Input.GetKey(KeyCode.LeftShift);
        if (MobileInputManager.Instance != null && MobileInputManager.Instance.IsSprinting)
        {
            sprintRequested = true;
        }

        if (sprintRequested && stamina > 0)
        {
            sprinting = true;
        }
        else
        {
            sprinting = false;
        }

        float previousStamina = stamina;
        if (sprinting)
        {
            stamina = Mathf.Max(0f, stamina - Time.deltaTime * 10f);
        }
        else if (stamina < maxStamina)
        {
            stamina = Mathf.Min(maxStamina, stamina + Time.deltaTime * staminaRegenRate);
        }

        if (!Mathf.Approximately(previousStamina, stamina))
        {
            UpdateStaminaUI();
        }

        UpdateSpeed();
        UpdateMoneyUI();
    }

    #region UI Integration

    public void UpdateMoneyUI()
    {
        if (!IsOwner) return;
        if (moneyText != null)
        {
            moneyText.text = $"{Mathf.CeilToInt(Money)}";
        }
    }

    public void SetHealthUI(Slider slider, TMP_Text text = null, Image fillImage = null)
    {
        healthSlider = slider;
        if (text != null) healthText = text;
        if (fillImage != null) healthFillImage = fillImage;
        UpdateHealthUI();
    }

    public void SetStaminaUI(Slider slider, TMP_Text text = null, Image fillImage = null)
    {
        staminaSlider = slider;
        if (text != null) staminaText = text;
        if (fillImage != null) staminaFillImage = fillImage;
        UpdateStaminaUI();
    }

    public void UpdateHealthUI()
    {
        if (!IsOwner) return;

        if (healthSlider != null)
        {
            healthSlider.maxValue = MaxHealth;
            healthSlider.value = Health;
        }

        if (healthFillImage != null)
        {
            healthFillImage.fillAmount = MaxHealth > 0f ? Mathf.Clamp01(Health / MaxHealth) : 0f;
        }

        if (healthText != null)
        {
            healthText.text = $"{Mathf.CeilToInt(Health)}%";
        }

        OnHealthChanged?.Invoke(Health, MaxHealth);
    }

    public void UpdateStaminaUI()
    {
        if (!IsOwner) return;

        if (staminaSlider != null)
        {
            staminaSlider.maxValue = maxStamina;
            staminaSlider.value = stamina;
        }

        if (staminaFillImage != null)
        {
            staminaFillImage.fillAmount = maxStamina > 0f ? Mathf.Clamp01(stamina / maxStamina) : 0f;
        }

        if (staminaText != null)
        {
            staminaText.text = $"{Mathf.CeilToInt(stamina)}%";
        }

        OnStaminaChanged?.Invoke(stamina, maxStamina);
    }

    private void AutoBindUI()
    {
        if (!IsOwner) return;

        Canvas playerCanvas = GetComponentInChildren<Canvas>(true);
        Transform searchRoot = playerCanvas != null ? playerCanvas.transform : transform;

        if (healthSlider == null || !healthSlider.transform.IsChildOf(transform))
        {
            healthSlider = null;
            Slider[] sliders = searchRoot.GetComponentsInChildren<Slider>(true);
            foreach (var s in sliders)
            {
                if (s == null) continue;
                string lower = s.name.ToLower();
                if (lower.Contains("health") || lower.Contains("hp"))
                {
                    healthSlider = s;
                    break;
                }
            }
        }

        if (staminaSlider == null || !staminaSlider.transform.IsChildOf(transform))
        {
            staminaSlider = null;
            Slider[] sliders = searchRoot.GetComponentsInChildren<Slider>(true);
            foreach (var s in sliders)
            {
                if (s == null) continue;
                string lower = s.name.ToLower();
                if (lower.Contains("stamina") || lower.Contains("energy") || lower.Contains("sp"))
                {
                    staminaSlider = s;
                    break;
                }
            }
        }

        if (healthText == null || !healthText.transform.IsChildOf(transform))
        {
            healthText = null;
            TMP_Text[] texts = searchRoot.GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in texts)
            {
                if (t == null) continue;
                string lower = t.name.ToLower();
                if (lower.Contains("health") || lower.Contains("hp"))
                {
                    healthText = t;
                    break;
                }
            }
        }

        if (staminaText == null || !staminaText.transform.IsChildOf(transform))
        {
            staminaText = null;
            TMP_Text[] texts = searchRoot.GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in texts)
            {
                if (t == null) continue;
                string lower = t.name.ToLower();
                if (lower.Contains("stamina") || lower.Contains("energy") || lower.Contains("sp"))
                {
                    staminaText = t;
                    break;
                }
            }
        }

        if (moneyText == null || !moneyText.transform.IsChildOf(transform))
        {
            moneyText = null;
            TMP_Text[] texts = searchRoot.GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in texts)
            {
                if (t == null) continue;
                string lower = t.name.ToLower();
                string parentLower = t.transform.parent != null ? t.transform.parent.name.ToLower() : "";
                if (lower.Contains("money") || parentLower.Contains("money") || lower.Contains("gold") || lower.Contains("score"))
                {
                    moneyText = t;
                    break;
                }
            }
        }

        // Notify Quota system to bind quota and currentAmount text
        Quota quota = FindFirstObjectByType<Quota>();
        if (quota != null)
        {
            quota.AssignPlayerUITexts();
        }

        // Setup Mobile Control Island for local player
        if (MobileInputManager.Instance != null && MobileInputManager.Instance.IsActive)
        {
            MobileControlIsland existingIsland = GetComponentInChildren<MobileControlIsland>(true);
            if (existingIsland == null && playerCanvas != null)
            {
                GameObject islandGo = new GameObject("MobileControlIsland", typeof(RectTransform), typeof(MobileControlIsland));
                islandGo.transform.SetParent(playerCanvas.transform, false);
                RectTransform islandRt = islandGo.GetComponent<RectTransform>();
                islandRt.anchorMin = Vector2.zero;
                islandRt.anchorMax = Vector2.one;
                islandRt.sizeDelta = Vector2.zero;
                existingIsland = islandGo.GetComponent<MobileControlIsland>();
            }
            if (existingIsland != null)
            {
                existingIsland.gameObject.SetActive(true);
                existingIsland.BindPlayer(this);
            }
        }
    }

    #endregion

    public void Heal(float amount)
    {
        if (amount <= 0f) return;
        Health = Mathf.Min(MaxHealth, Health + amount);
        UpdateHealthUI();
    }

    public void RestoreStamina(float amount)
    {
        if (amount <= 0f) return;
        stamina = Mathf.Min(maxStamina, stamina + amount);
        UpdateStaminaUI();
    }

    public void Die(bool spawnAtRespawnPoint = false)
    {
        if (IsServer)
        {
            HandleDeathServer(spawnAtRespawnPoint);
        }
        else
        {
            DieServerRpc(spawnAtRespawnPoint);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DieServerRpc(bool spawnAtRespawnPoint = false)
    {
        HandleDeathServer(spawnAtRespawnPoint);
    }

    public void TakeDamage(float damage, Vector3 knockbackForce = default)
    {
        if (damage <= 0f && knockbackForce == Vector3.zero) return;

        if (IsServer)
        {
            ApplyDamage(damage, knockbackForce);
        }
        else
        {
            TakeDamageServerRpc(damage, knockbackForce);
        }
        audioSource.PlayOneShot(audio, soundVolume);
    }

    [ServerRpc(RequireOwnership = false)]
    private void TakeDamageServerRpc(float damage, Vector3 knockbackForce)
    {
        ApplyDamage(damage, knockbackForce);
    }

    private void ApplyDamage(float damage, Vector3 knockbackForce)
    {
        if (!IsServer || !isAlive) return;

        Health = Mathf.Max(0f, Health - damage);
        TakeDamageClientRpc(Health, knockbackForce);

        if (Health <= 0f)
        {
            HandleDeathServer();
        }
    }

    [ClientRpc]
    private void TakeDamageClientRpc(float newHealth, Vector3 knockbackForce)
    {
        Health = newHealth;
        UpdateHealthUI();

        if (IsOwner && isAlive && knockbackForce.sqrMagnitude > 0.001f)
        {
            ApplyKnockback(knockbackForce);
        }
    }

    public void ApplyKnockback(Vector3 force)
    {
        velocity += force;
        if (body != null)
        {
            body.linearVelocity = velocity;
        }
    }

    private void HandleDeathServer(bool spawnAtRespawnPoint = false)
    {
        if (!IsServer || !isAlive) return;

        isAlive = false;
        Health = 0f;

        Vector3 spawnPos;
        if (spawnAtRespawnPoint)
        {
            spawnPos = GetRespawnPosition(transform.position + Vector3.up * 0.5f) + Vector3.up * 0.5f;
        }
        else
        {
            spawnPos = transform.position + Vector3.up * 0.5f;
        }

        if (deadPlayerPrefab != null)
        {
            GameObject corpseObj = Instantiate(deadPlayerPrefab, spawnPos, Quaternion.identity);

            if (corpseObj.TryGetComponent(out DeadPlayer deadComp))
            {
                deadComp.Initialize(OwnerClientId, gameObject);
            }

            if (corpseObj.TryGetComponent(out NetworkObject netObj))
            {
                netObj.Spawn();
            }
        }
        else
        {
            Debug.LogError("deadPlayerPrefab is not assigned on PlayerController!");
        }

        DeathClientRpc();
    }

    public static Vector3 GetRespawnPosition(Vector3 fallbackPosition)
    {
        try
        {
            GameObject[] taggedRespawn = GameObject.FindGameObjectsWithTag("RespawnPoint");
            if (taggedRespawn != null && taggedRespawn.Length > 0 && taggedRespawn[0] != null)
            {
                return taggedRespawn[0].transform.position;
            }
        }
        catch (UnityException) { }

        try
        {
            GameObject[] taggedRespawn2 = GameObject.FindGameObjectsWithTag("Respawn");
            if (taggedRespawn2 != null && taggedRespawn2.Length > 0 && taggedRespawn2[0] != null)
            {
                return taggedRespawn2[0].transform.position;
            }
        }
        catch (UnityException) { }

        GameObject namedObj = GameObject.Find("RespawnPoint");
        if (namedObj != null)
        {
            return namedObj.transform.position;
        }

        namedObj = GameObject.Find("Respawn");
        if (namedObj != null)
        {
            return namedObj.transform.position;
        }

        try
        {
            GameObject[] taggedSpawn = GameObject.FindGameObjectsWithTag("SpawnPoint");
            if (taggedSpawn != null && taggedSpawn.Length > 0 && taggedSpawn[0] != null)
            {
                return taggedSpawn[0].transform.position;
            }
        }
        catch (UnityException) { }

        GameObject spawnNamed = GameObject.Find("SpawnPoint");
        if (spawnNamed != null)
        {
            return spawnNamed.transform.position;
        }

        return fallbackPosition;
    }

    [ClientRpc]
    private void DeathClientRpc()
    {
        isAlive = false;
        Health = 0f;
        UpdateHealthUI();

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

        UpdateHealthUI();
        UpdateStaminaUI();

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
        if (!IsOwner || !isAlive || !gravityEnabled) return;

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

        if (body != null)
        {
            body.linearVelocity = velocity;
        }
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

        if (onGround || Climbing)
        {
            float newX = Mathf.MoveTowards(currentX, playerInput.x * speed, maxSpeedChange);
            float newZ = Mathf.MoveTowards(currentZ, playerInput.y * speed, maxSpeedChange);
            velocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ);
        }
        else
        {
            // Air movement: preserve momentum while allowing steering
            Vector3 desiredDir = (xAxis * playerInput.x + zAxis * playerInput.y);
            float inputMag = Mathf.Clamp01(playerInput.magnitude);

            if (inputMag > 0.01f)
            {
                desiredDir.Normalize();
                float targetSpeed = speed * inputMag;

                // Current speed component along desired input direction
                float speedInDirection = Vector3.Dot(relativeVelocity, desiredDir);

                // Accelerate in desired direction without clamping existing faster momentum
                if (speedInDirection < targetSpeed)
                {
                    float addSpeed = Mathf.Min(maxSpeedChange, targetSpeed - speedInDirection);
                    velocity += desiredDir * addSpeed;
                }
            }
        }
    }

    void UpdateState()
    {
        if (!IsOwner) return;
        stepsSinceLastGrounded += 1;
        stepsSinceLastJump += 1;
        if (body != null)
        {
            velocity = body.linearVelocity;
        }

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

        if (connectedBody != null && (connectedBody.isKinematic || connectedBody.mass >= (body != null ? body.mass : 1f)))
        {
            UpdateConnectionState();
        }
    }

    void Jump()
    {
        if (!IsOwner) return;

        Vector3 jumpDirection;
        bool canCoyoteJump = onGround || (stepsSinceLastGrounded <= coyoteSteps && jumpPhase == 0);

        if (canCoyoteJump)
        {
            jumpDirection = contactNormal;
        }
        else if (OnSteep)
        {
            jumpDirection = steepNormal;
            jumpPhase = 0;
        }
        else if (maxAirJumps > 0 && jumpPhase <= maxAirJumps)
        {
            if (jumpPhase == 0) jumpPhase = 1;
            jumpDirection = upAxis;
        }
        else return;

        stepsSinceLastJump = 0;
        jumpPhase += 1;

        float effectiveGravity = Physics.gravity.magnitude * gravityMultiplier;
        float jumpSpeed = Mathf.Sqrt(2f * effectiveGravity * jumpHeight);

        // Blend contactNormal and upAxis to favor upward impulse while giving a boost away from slopes
        jumpDirection = (jumpDirection + upAxis * 1.5f).normalized;

        // Cancel existing downward vertical velocity so falling jumps are crisp and reach full height
        float verticalSpeed = Vector3.Dot(velocity, upAxis);
        if (verticalSpeed < 0f)
        {
            velocity -= upAxis * verticalSpeed;
        }

        // Add jump velocity without stripping horizontal velocity
        float alignedSpeed = Vector3.Dot(velocity, jumpDirection);
        if (alignedSpeed > 0f)
        {
            jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, jumpSpeed * 0.5f);
        }

        velocity += jumpDirection * jumpSpeed;
    }

    bool SnapToGround()
    {
        if (body == null) return false;
        // Never snap if player is moving upwards (jumping) or within the jump snap suppression period
        if (Vector3.Dot(velocity, upAxis) > 0.05f) return false;
        if (stepsSinceLastJump <= jumpSnapSuppressSteps) return false;
        if (stepsSinceLastGrounded > 1) return false;
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
        if (connectedBody == previousConnectedBody && Time.deltaTime > 0f)
        {
            connectionVelocity = (connectedBody.position - connectionWorldPosition) / Time.deltaTime;
        }
        if (connectedBody != null)
        {
            connectionWorldPosition = connectedBody.position;
        }
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
