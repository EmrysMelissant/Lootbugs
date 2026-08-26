using UnityEngine;
using Unity.Netcode;
public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement")]
    private float moveSpeed;
    public float walkSpeed;

    [Header("Jumping")]
    public float airMultiplier;
    public float jumpForce;
    public float jumpCooldown;
    bool readyToJump;
    [Header("Crouching")]
    public float crouchSpeed;
    public float crouchYScale;
    private float startYScale;
    [Header("keyBinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl;

    [Header("Ground Check")]
    public float playerHeight;
    public float groundDrag;
    public LayerMask whatIsGround;
    bool grounded;
    public float landingGravityMultiplier;

    public Transform orientation;
    float horizontalInput;
    float verticalInput;
    Vector3 moveDirection;
    Rigidbody rb;
    private Vector3 surfaceNormal;
    private Vector3 surfacePoint;
    public float detectionDistance;
    public float detectionRadius;
    public float rotationSpeed;
    public MovementState state;
    public enum MovementState
    {
        walking,
        sprinting,
        crouching,
        air
    }
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        startYScale = transform.localScale.y;
        readyToJump = true;

    }
    void MyInput()
    {
        // move
        if (MobileMotionManager.Instance != null)
        {
            Vector2 move = MobileMotionManager.Instance.GetMoveInput();
            horizontalInput = move.x;
            verticalInput = move.y;
        }
        else
        {
            horizontalInput = Input.GetAxisRaw("Horizontal");
            verticalInput = Input.GetAxisRaw("Vertical");
        }

        // jump
        bool jumpPressed = MobileMotionManager.Instance != null ? MobileMotionManager.Instance.IsJumpTriggered() : Input.GetKey(jumpKey);
        if (jumpPressed && readyToJump && grounded)
        {
            readyToJump = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);
        }

        // crouch
        bool crouchActive = MobileMotionManager.Instance != null ? MobileMotionManager.Instance.IsCrouchActive() : Input.GetKey(crouchKey);
        if (crouchActive && Mathf.Abs(transform.localScale.y - crouchYScale) > 0.01f)
        {
            transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
            rb.AddForce(-transform.up * 5f, ForceMode.Impulse);
        }
        else if (!crouchActive && Mathf.Abs(transform.localScale.y - startYScale) > 0.01f)
        {
            transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
        }
    }
    private void StateHandler()
    {
        bool crouchActive = MobileMotionManager.Instance != null ? MobileMotionManager.Instance.IsCrouchActive() : Input.GetKey(crouchKey);
        bool sprintActive = MobileMotionManager.Instance != null ? MobileMotionManager.Instance.IsSprintActive() : Input.GetKey(sprintKey);

        // mode crouching
        if (crouchActive)
        {
            state = MovementState.crouching;
            moveSpeed = crouchSpeed;
        }
        // mode sprinting
        else if (grounded && sprintActive)
        {
            state = MovementState.sprinting;
        }
        // mode walking
        else if (grounded)
        {
            state = MovementState.walking;
            moveSpeed = walkSpeed;
        }
        // mode air
        else
        {
            state = MovementState.air;
        }
    }
    private void MovePlayer()
    {
        moveDirection = orientation.forward.normalized * verticalInput + orientation.right * horizontalInput;

        if (grounded)
        {
            rb.AddForce(moveDirection * moveSpeed * 10f, ForceMode.Force);
        }
        else if (!grounded)
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
        }

    }
    void FixedUpdate()
    {
        if (!IsOwner) return;
        MovePlayer();
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;
        grounded = Physics.Raycast(transform.position, -transform.up, playerHeight * 0.5f + 0.2f, whatIsGround);
        MyInput();
        SpeedControl();
        StateHandler();

        if (grounded)
        {
            rb.linearDamping = groundDrag;
            rb.useGravity = false;
        }
        else
        {
            rb.linearDamping = 0;
            rb.useGravity = true;
        }
    }

    private void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (flatVel.magnitude > moveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * moveSpeed;
            rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
        }

    }

    private void Jump()
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }
    private void ResetJump()
    {
        readyToJump = true;
    }
}