using System;
using UnityEngine;

public class MobileInputManager : MonoBehaviour
{
    public static MobileInputManager Instance { get; private set; }

    [Header("Platform & Activation")]
    [Tooltip("If true, mobile controls are enabled. Automatically true on Android/iOS.")]
    [SerializeField] private bool forceEnableInEditor = true;

    [Header("Orientation (Tilt) Movement Settings")]
    [Tooltip("Multiplier for tilt angle to movement velocity.")]
    [SerializeField] private float tiltSensitivity = 2.5f;

    [Tooltip("Minimum tilt angle (in degrees) required before movement is registered.")]
    [SerializeField] private float tiltDeadzone = 4.0f;

    [Tooltip("Maximum tilt angle (in degrees) that maps to full speed.")]
    [SerializeField] private float maxTiltAngle = 28.0f;

    [Tooltip("Smoothing speed for tilt movement (higher = faster, lower = smoother).")]
    [SerializeField] private float tiltSmoothing = 14.0f;

    [Tooltip("Invert forward/backward pitch movement.")]
    [SerializeField] private bool invertPitch = false;

    [Tooltip("Invert left/right roll strafe movement.")]
    [SerializeField] private bool invertRoll = false;

    [Header("Touch Look Settings")]
    [Tooltip("Sensitivity for touch drag camera rotation.")]
    [SerializeField] private float touchLookSensitivityX = 0.12f;
    [SerializeField] private float touchLookSensitivityY = 0.10f;
    [SerializeField] private float touchLookSmoothing = 25.0f;

    [Header("Runtime State (Read Only)")]
    [SerializeField] private Vector2 currentMovementInput;
    [SerializeField] private Vector2 currentLookDelta;
    [SerializeField] private bool isSprinting;
    [SerializeField] private bool isGyroAvailable;
    [SerializeField] private Vector3 calibratedNeutralGravity = new Vector3(0f, -0.65f, -0.76f); // Default ~40 deg resting angle

    // Action Trigger Flags
    private bool jumpRequested;
    private bool interactRequested;
    private bool tetherRequested;
    private bool flashlightRequested;
    private bool pauseRequested;

    // Filtered internal values
    private Vector2 rawTiltVector;
    private Vector2 smoothedTiltVector;
    private Vector2 rawTouchLookDelta;
    private Vector2 smoothedTouchLookDelta;

    // Events
    public event Action OnCalibrated;
    public event Action<bool> OnSprintStateChanged;

    public bool IsActive
    {
        get
        {
#if UNITY_ANDROID || UNITY_IOS
            return true;
#else
            return forceEnableInEditor;
#endif
        }
    }

    public Vector2 MovementInput => currentMovementInput;
    public Vector2 LookDelta => currentLookDelta;
    public bool IsSprinting => isSprinting;
    public bool IsGyroAvailable => isGyroAvailable;
    public Vector2 RawTiltVector => rawTiltVector;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializeSensors();
    }

    private void Start()
    {
        // Set default neutral posture calibration
        CalibrateNeutralOrientation();
    }

    private void InitializeSensors()
    {
        isGyroAvailable = SystemInfo.supportsGyroscope;
        if (isGyroAvailable)
        {
            Input.gyro.enabled = true;
            Input.gyro.updateInterval = 0.016f; // ~60 Hz
        }
        Input.multiTouchEnabled = true;
    }

    private void Update()
    {
        if (!IsActive) return;

        UpdateOrientationTilt();
        UpdateTouchLook();
    }

    #region Orientation / Tilt Processing

    private void UpdateOrientationTilt()
    {
        Vector3 currentGravity = GetCurrentDeviceGravity();

        // Compute angle delta relative to calibrated neutral posture
        // In Landscape Left:
        // Device X-axis: tilting left/right (roll -> strafe)
        // Device Y-axis: tilting forward/backward (pitch -> forward/backward)
        float deltaPitch = (currentGravity.y - calibratedNeutralGravity.y) * 90f;
        float deltaRoll = (currentGravity.x - calibratedNeutralGravity.x) * 90f;

        if (invertPitch) deltaPitch = -deltaPitch;
        if (invertRoll) deltaRoll = -deltaRoll;

        // Apply deadzone and clamp to max angle
        float forwardInput = ProcessTiltAxis(-deltaPitch);
        float strafeInput = ProcessTiltAxis(deltaRoll);

        rawTiltVector = new Vector2(strafeInput, forwardInput);

        // Smooth output to eliminate micro-hand tremors
        smoothedTiltVector = Vector2.Lerp(
            smoothedTiltVector,
            rawTiltVector,
            tiltSmoothing * Time.deltaTime
        );

        currentMovementInput = Vector2.ClampMagnitude(smoothedTiltVector * tiltSensitivity, 1f);
    }

    private float ProcessTiltAxis(float angleDegrees)
    {
        float sign = Mathf.Sign(angleDegrees);
        float absAngle = Mathf.Abs(angleDegrees);

        if (absAngle < tiltDeadzone)
        {
            return 0f;
        }

        float normalized = Mathf.Clamp01((absAngle - tiltDeadzone) / Mathf.Max(1f, maxTiltAngle - tiltDeadzone));
        return sign * normalized;
    }

    private Vector3 GetCurrentDeviceGravity()
    {
        if (isGyroAvailable && Input.gyro.enabled)
        {
            return Input.gyro.gravity;
        }
        return Input.acceleration;
    }

    /// <summary>
    /// Calibrates the current phone angle as the zero/resting neutral posture.
    /// </summary>
    public void CalibrateNeutralOrientation()
    {
        Vector3 cur = GetCurrentDeviceGravity();
        if (cur.sqrMagnitude > 0.05f)
        {
            calibratedNeutralGravity = cur;
        }
        smoothedTiltVector = Vector2.zero;
        currentMovementInput = Vector2.zero;

        OnCalibrated?.Invoke();
        Debug.Log($"[MobileInputManager] Calibrated neutral posture gravity: {calibratedNeutralGravity}");
    }

    #endregion

    #region Touch Look Processing

    public void AddTouchLookDelta(Vector2 deltaPixels)
    {
        rawTouchLookDelta.x += deltaPixels.x * touchLookSensitivityX;
        rawTouchLookDelta.y += deltaPixels.y * touchLookSensitivityY;
    }

    private void UpdateTouchLook()
    {
        smoothedTouchLookDelta = Vector2.Lerp(
            smoothedTouchLookDelta,
            rawTouchLookDelta,
            touchLookSmoothing * Time.deltaTime
        );

        currentLookDelta = smoothedTouchLookDelta;

        // Reset accumulation for next frame
        rawTouchLookDelta = Vector2.zero;
    }

    public Vector2 GetLookDelta()
    {
        return currentLookDelta;
    }

    public Vector2 GetMovementInput()
    {
        return currentMovementInput;
    }

    #endregion

    #region Action Control API (Called by Control Island Buttons)

    public void TriggerJump()
    {
        jumpRequested = true;
    }

    public bool ConsumeJump()
    {
        if (jumpRequested)
        {
            jumpRequested = false;
            return true;
        }
        return false;
    }

    public void ToggleSprint()
    {
        SetSprint(!isSprinting);
    }

    public void SetSprint(bool sprinting)
    {
        if (isSprinting != sprinting)
        {
            isSprinting = sprinting;
            OnSprintStateChanged?.Invoke(isSprinting);
        }
    }

    public void TriggerInteract()
    {
        interactRequested = true;
    }

    public bool ConsumeInteract()
    {
        if (interactRequested)
        {
            interactRequested = false;
            return true;
        }
        return false;
    }

    public void TriggerTether()
    {
        tetherRequested = true;
    }

    public bool ConsumeTether()
    {
        if (tetherRequested)
        {
            tetherRequested = false;
            return true;
        }
        return false;
    }

    public void TriggerFlashlight()
    {
        flashlightRequested = true;
    }

    public bool ConsumeFlashlight()
    {
        if (flashlightRequested)
        {
            flashlightRequested = false;
            return true;
        }
        return false;
    }

    public void TriggerPause()
    {
        pauseRequested = true;
    }

    public bool ConsumePause()
    {
        if (pauseRequested)
        {
            pauseRequested = false;
            return true;
        }
        return false;
    }

    #endregion
}
