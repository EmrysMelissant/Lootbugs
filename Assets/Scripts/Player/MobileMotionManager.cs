using UnityEngine;

public class MobileMotionManager : MonoBehaviour
{
    public static MobileMotionManager Instance { get; private set; }

    [Header("Motion & Gyroscope Settings")]
    [Tooltip("Enable or disable mobile motion controls.")]
    [SerializeField] private bool motionControlsEnabled = true;

    [Tooltip("Enable motion simulation in Unity Editor (Arrow keys/Mouse for tilt).")]
    [SerializeField] private bool enableEditorSimulation = true;

    [Tooltip("Gyroscope look sensitivity multiplier.")]
    [SerializeField, Range(0.1f, 10f)] private float gyroLookSensitivity = 2.5f;

    [Tooltip("Touch screen swipe look sensitivity.")]
    [SerializeField, Range(0.1f, 10f)] private float touchLookSensitivity = 1.8f;

    [Tooltip("Smoothing factor for gyro look to eliminate hand jitter (higher = smoother, 0 = raw).")]
    [SerializeField, Range(0f, 25f)] private float gyroSmoothing = 15f;

    [Header("Tilt Locomotion Settings")]
    [Tooltip("Sensitivity of tilt angle to movement velocity.")]
    [SerializeField, Range(0.5f, 5f)] private float tiltMoveSensitivity = 2.2f;

    [Tooltip("Deadzone angle in degrees before tilt triggers movement.")]
    [SerializeField, Range(1f, 20f)] private float tiltDeadzoneAngle = 6f;

    [Tooltip("Maximum tilt angle in degrees for full speed locomotion.")]
    [SerializeField, Range(15f, 60f)] private float maxTiltAngle = 30f;

    [Tooltip("Smoothing factor for tilt movement.")]
    [SerializeField, Range(1f, 30f)] private float tiltSmoothing = 12f;

    [Tooltip("Neutral pitch angle in degrees when holding phone comfortably.")]
    [SerializeField, Range(0f, 80f)] private float defaultNeutralPitch = 35f;

    [Header("Physical Motion Gestures")]
    [Tooltip("Enable physical upward phone jerk gesture to jump.")]
    [SerializeField] private bool enableMotionJumpGesture = true;

    [Tooltip("Acceleration threshold in Gs to register an upward jump jerk.")]
    [SerializeField, Range(1.2f, 4.0f)] private float jumpJerkThreshold = 1.8f;

    [Tooltip("Cooldown in seconds between motion gesture jumps.")]
    [SerializeField] private float jumpGestureCooldown = 0.4f;

    // Runtime state
    private bool isGyroAvailable;
    private float calibratedNeutralPitch;
    private float calibratedNeutralRoll;
    private Vector2 currentTiltInput = Vector2.zero;
    private Vector2 targetTiltInput = Vector2.zero;
    private Vector2 smoothedLookDelta = Vector2.zero;
    private Vector2 touchSwipeLookDelta = Vector2.zero;
    private Vector3 lastDeviceAcceleration = Vector3.zero;
    private float lastJumpGestureTime = -10f;

    // Mobile Action States
    private bool touchJumpTriggered;
    private bool touchSprintActive;
    private bool touchCrouchActive;
    private bool touchTetherTriggered;
    private bool touchInteractTriggered;
    private bool touchPauseTriggered;
    private bool motionJumpTriggered;

    // Properties
    public bool IsMotionActive => motionControlsEnabled && (Application.isMobilePlatform || enableEditorSimulation);
    public bool IsGyroAvailable => isGyroAvailable;
    public float GyroLookSensitivity { get => gyroLookSensitivity; set => gyroLookSensitivity = Mathf.Clamp(value, 0.1f, 10f); }
    public float TouchLookSensitivity { get => touchLookSensitivity; set => touchLookSensitivity = Mathf.Clamp(value, 0.1f, 10f); }
    public float TiltMoveSensitivity { get => tiltMoveSensitivity; set => tiltMoveSensitivity = Mathf.Clamp(value, 0.5f, 5f); }
    public float TiltDeadzoneAngle => tiltDeadzoneAngle;
    public float MaxTiltAngle => maxTiltAngle;
    public Vector2 CurrentTiltOffset => currentTiltInput;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeSensors();
    }

    private void Start()
    {
        // Optimize mobile platform settings
        if (Application.isMobilePlatform)
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
        }

        CalibrateNeutralPosture();
    }

    private void InitializeSensors()
    {
        isGyroAvailable = SystemInfo.supportsGyroscope;
        if (isGyroAvailable)
        {
            Input.gyro.enabled = true;
            Input.gyro.updateInterval = 0.016f; // ~60 Hz
        }
        else
        {
            Debug.Log("[MobileMotionManager] Gyroscope not detected on this hardware. Falling back to accelerometer and touch look.");
        }
    }

    /// <summary>
    /// Calibrates the current phone holding angle as the neutral (zero-velocity / forward look) posture.
    /// </summary>
    public void CalibrateNeutralPosture()
    {
        if (isGyroAvailable && Input.gyro.enabled)
        {
            Vector3 gravity = Input.gyro.gravity;
            if (gravity.sqrMagnitude > 0.01f)
            {
                // In landscape mode, gravity.y represents pitch and gravity.x represents roll
                calibratedNeutralPitch = Mathf.Atan2(-gravity.y, -gravity.z) * Mathf.Rad2Deg;
                calibratedNeutralRoll = Mathf.Atan2(gravity.x, -gravity.z) * Mathf.Rad2Deg;
            }
            else
            {
                calibratedNeutralPitch = defaultNeutralPitch;
                calibratedNeutralRoll = 0f;
            }
        }
        else
        {
            Vector3 accel = Input.acceleration;
            if (accel.sqrMagnitude > 0.01f)
            {
                calibratedNeutralPitch = Mathf.Atan2(-accel.y, -accel.z) * Mathf.Rad2Deg;
                calibratedNeutralRoll = Mathf.Atan2(accel.x, -accel.z) * Mathf.Rad2Deg;
            }
            else
            {
                calibratedNeutralPitch = defaultNeutralPitch;
                calibratedNeutralRoll = 0f;
            }
        }

        currentTiltInput = Vector2.zero;
        targetTiltInput = Vector2.zero;
        Debug.Log($"[MobileMotionManager] Neutral posture calibrated: Pitch={calibratedNeutralPitch:F1}°, Roll={calibratedNeutralRoll:F1}°");
    }

    private void Update()
    {
        if (!motionControlsEnabled) return;

        UpdateMotionTracking();
        UpdateMotionGestures();
        UpdateEditorSimulation();
    }

    private void LateUpdate()
    {
        // Reset single-frame pulse triggers at end of frame
        touchJumpTriggered = false;
        touchTetherTriggered = false;
        touchInteractTriggered = false;
        touchPauseTriggered = false;
        motionJumpTriggered = false;
        touchSwipeLookDelta = Vector2.zero;
    }

    private void UpdateMotionTracking()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // 1. Process Tilt Locomotion (Accelerometer / Gravity)
        Vector3 accel = isGyroAvailable && Input.gyro.enabled ? Input.gyro.gravity : Input.acceleration;
        if (accel.sqrMagnitude > 0.01f)
        {
            float rawPitch = Mathf.Atan2(-accel.y, -accel.z) * Mathf.Rad2Deg;
            float rawRoll = Mathf.Atan2(accel.x, -accel.z) * Mathf.Rad2Deg;

            float deltaPitch = rawPitch - calibratedNeutralPitch;
            float deltaRoll = rawRoll - calibratedNeutralRoll;

            // Handle pitch (Forward/Backward): Tilting down/forward (positive deltaPitch) moves forward
            float pitchInput = 0f;
            if (Mathf.Abs(deltaPitch) > tiltDeadzoneAngle)
            {
                float sign = Mathf.Sign(deltaPitch);
                float magnitude = (Mathf.Abs(deltaPitch) - tiltDeadzoneAngle) / (maxTiltAngle - tiltDeadzoneAngle);
                pitchInput = sign * Mathf.Clamp01(magnitude) * tiltMoveSensitivity;
            }

            // Handle roll (Left/Right Strafe): Tilting right (positive deltaRoll) strafes right
            float rollInput = 0f;
            if (Mathf.Abs(deltaRoll) > tiltDeadzoneAngle)
            {
                float sign = Mathf.Sign(deltaRoll);
                float magnitude = (Mathf.Abs(deltaRoll) - tiltDeadzoneAngle) / (maxTiltAngle - tiltDeadzoneAngle);
                rollInput = sign * Mathf.Clamp01(magnitude) * tiltMoveSensitivity;
            }

            targetTiltInput = new Vector2(Mathf.Clamp(rollInput, -1f, 1f), Mathf.Clamp(pitchInput, -1f, 1f));
        }

        // Smooth tilt locomotion
        currentTiltInput = Vector2.Lerp(currentTiltInput, targetTiltInput, dt * tiltSmoothing);

        // 2. Process Gyroscope 3D Look Delta
        Vector2 rawLookDelta = Vector2.zero;
        if (isGyroAvailable && Input.gyro.enabled)
        {
            Vector3 rotationRate = Input.gyro.rotationRateUnbiased;

            // In Landscape Left orientation:
            // rotationRate.x = Pitch rate (tilting phone up/down)
            // rotationRate.y = Yaw rate (panning phone left/right in room)
            float pitchDelta = -rotationRate.x * gyroLookSensitivity * 60f * dt;
            float yawDelta = -rotationRate.y * gyroLookSensitivity * 60f * dt;

            rawLookDelta = new Vector2(yawDelta, pitchDelta);
        }

        // Combine with touch look swipe delta
        rawLookDelta += touchSwipeLookDelta * touchLookSensitivity;

        // Smooth look delta
        if (gyroSmoothing > 0f)
        {
            smoothedLookDelta = Vector2.Lerp(smoothedLookDelta, rawLookDelta, dt * gyroSmoothing);
        }
        else
        {
            smoothedLookDelta = rawLookDelta;
        }
    }

    private void UpdateMotionGestures()
    {
        if (!enableMotionJumpGesture) return;

        Vector3 deviceAccel = Input.acceleration;
        Vector3 userAccel = isGyroAvailable && Input.gyro.enabled ? Input.gyro.userAcceleration : (deviceAccel - lastDeviceAcceleration);
        lastDeviceAcceleration = deviceAccel;

        // Upward jerk / sudden upward flick detection in landscape orientation
        float upwardImpulse = userAccel.y;
        if (upwardImpulse > jumpJerkThreshold && (Time.time - lastJumpGestureTime) > jumpGestureCooldown)
        {
            lastJumpGestureTime = Time.time;
            motionJumpTriggered = true;
            Debug.Log($"[MobileMotionManager] Physical Upward Jump Gesture detected! Impulse={upwardImpulse:F2}G");
        }
    }

    private void UpdateEditorSimulation()
    {
#if UNITY_EDITOR
        if (!enableEditorSimulation) return;

        // Simulate tilt locomotion with Arrow Keys / IJKL in editor
        float simX = 0f;
        float simY = 0f;
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.L)) simX += 1f;
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.J)) simX -= 1f;
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.I)) simY += 1f;
        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.K)) simY -= 1f;

        if (simX != 0f || simY != 0f)
        {
            targetTiltInput = new Vector2(simX, simY).normalized;
            currentTiltInput = Vector2.Lerp(currentTiltInput, targetTiltInput, Time.deltaTime * tiltSmoothing);
        }

        // Simulate recalibration in Editor with C key
        if (Input.GetKeyDown(KeyCode.C))
        {
            CalibrateNeutralPosture();
        }
#endif
    }

    // ==========================================
    // Public Input Feeds for Gameplay Controllers
    // ==========================================

    /// <summary>
    /// Combined movement input (Tilt Locomotion + Touch/Keyboard fallback).
    /// </summary>
    public Vector2 GetMoveInput()
    {
        // Standard keyboard input
        Vector2 keyboardInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        // If keyboard is actively pressed, give priority or combine with tilt
        if (keyboardInput.sqrMagnitude > 0.01f)
        {
            return Vector2.ClampMagnitude(keyboardInput + currentTiltInput, 1f);
        }

        return currentTiltInput;
    }

    /// <summary>
    /// Look delta in degrees this frame (Gyroscope + Touch Swipe + Mouse delta).
    /// </summary>
    public Vector2 GetLookDelta(float mouseSenseX = 1f, float mouseSenseY = 1f)
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSenseX;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSenseY;
        Vector2 mouseDelta = new Vector2(mouseX, mouseY);

        return mouseDelta + smoothedLookDelta;
    }

    /// <summary>
    /// Returns true if jump is triggered (Touch Jump button, Physical upward jerk gesture, or Space key).
    /// </summary>
    public bool IsJumpTriggered()
    {
        return touchJumpTriggered || motionJumpTriggered || Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space);
    }

    /// <summary>
    /// Returns true if sprint is active (Touch Sprint button or Left Shift).
    /// </summary>
    public bool IsSprintActive()
    {
        return touchSprintActive || Input.GetKey(KeyCode.LeftShift);
    }

    /// <summary>
    /// Returns true if crouch is active (Touch Crouch button or Left Control).
    /// </summary>
    public bool IsCrouchActive()
    {
        return touchCrouchActive || Input.GetKey(KeyCode.LeftControl);
    }

    /// <summary>
    /// Returns true if tether action is triggered (Touch Tether button or E key).
    /// </summary>
    public bool IsTetherTriggered(KeyCode defaultKey = KeyCode.E)
    {
        return touchTetherTriggered || Input.GetKeyDown(defaultKey);
    }

    /// <summary>
    /// Returns true if interact action is triggered (Touch Interact button or F key).
    /// </summary>
    public bool IsInteractTriggered(KeyCode defaultKey = KeyCode.F)
    {
        return touchInteractTriggered || Input.GetKeyDown(defaultKey);
    }

    /// <summary>
    /// Returns true if pause is triggered (Touch Pause button or Escape key).
    /// </summary>
    public bool IsPauseTriggered()
    {
        return touchPauseTriggered || Input.GetKeyDown(KeyCode.Escape);
    }

    // ==========================================
    // UI Event Callbacks called from MobileHUD
    // ==========================================

    public void OnTouchJumpPressed()
    {
        touchJumpTriggered = true;
    }

    public void SetTouchSprint(bool active)
    {
        touchSprintActive = active;
    }

    public void ToggleTouchSprint()
    {
        touchSprintActive = !touchSprintActive;
    }

    public void SetTouchCrouch(bool active)
    {
        touchCrouchActive = active;
    }

    public void ToggleTouchCrouch()
    {
        touchCrouchActive = !touchCrouchActive;
    }

    public void OnTouchTetherPressed()
    {
        touchTetherTriggered = true;
    }

    public void OnTouchInteractPressed()
    {
        touchInteractTriggered = true;
    }

    public void OnTouchPausePressed()
    {
        touchPauseTriggered = true;
    }

    public void AddTouchSwipeLookDelta(Vector2 delta)
    {
        touchSwipeLookDelta += delta;
    }
}
