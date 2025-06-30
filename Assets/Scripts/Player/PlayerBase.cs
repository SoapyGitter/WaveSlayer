using UnityEngine;

public class PlayerBase : MonoBehaviour
{
    [SerializeField] PlayerModel playerModel;

    [Header("Movement")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform playerVisual; // Visual representation (no longer rotated)
    [SerializeField] private bool movementEnabled = true; // New field to control movement
    [SerializeField] private FloatingJoystick joystick;
    [SerializeField] private float acceleration = 10f; // How quickly player reaches max speed
    [SerializeField] private float deceleration = 15f; // How quickly player stops
    // Movement variables
    private Vector2 moveDirection;
    private Vector2 targetVelocity;

    // Animation parameters
    private bool isMoving = false;
    private Animator animator;
    private int currentDirectionIndex = 0; // Store the current direction index (0-11)

    // Rotation settings
    [Header("Rotation")]
    [SerializeField] private bool rotateTowardsMovement = true; // Whether to rotate player with movement

    void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        animator = GetComponentInChildren<Animator>();

        // Configure the rigidbody2D for top-down movement
        if (rb != null)
        {
            rb.gravityScale = 0f; // Disable gravity
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }

    // Update is called once per frame
    void Update()
    {
        HandleInput();
        UpdateAnimator();
    }

    void FixedUpdate()
    {
        Move();
    }

    private void HandleInput()
    {
        // Only process input if movement is enabled
        if (!movementEnabled) return;

        // Get movement input from the Input System's left stick
        Vector2 inputVector = joystick.Direction;

        // Apply any deadzone handling if needed
        if (inputVector.magnitude < 0.1f)
        {
            inputVector = Vector2.zero;
        }

        // Update move direction
        moveDirection = inputVector;
    }

    private void Move()
    {
        // Only move if movement is enabled
        if (!movementEnabled)
        {
            // Don't override velocity if we are dashing or in another state that controls velocity
            return;
        }

        if (playerModel != null)
        {
            // Calculate the target velocity based on input
            targetVelocity = moveDirection * playerModel.MovementSpeed;
            
            // Get current velocity
            Vector2 currentVelocity = rb.linearVelocity;
            
            // Calculate new velocity with smooth acceleration or deceleration
            Vector2 newVelocity;
            
            if (moveDirection.magnitude > 0.1f)
            {
                // Accelerate towards target velocity
                newVelocity = Vector2.Lerp(currentVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
                isMoving = true;
            }
            else
            {
                // Decelerate to zero
                newVelocity = Vector2.Lerp(currentVelocity, Vector2.zero, deceleration * Time.fixedDeltaTime);
                isMoving = currentVelocity.magnitude > 0.4f;
            }
            
            // Apply the new velocity
            rb.linearVelocity = newVelocity;
        }
    }

    private void UpdateAnimator()
    {
        // Update animator if available
        if (animator != null)
        {
            animator.SetBool("IsMoving", isMoving);

            // Set direction parameter for 12-direction animation
            if (isMoving && moveDirection.magnitude > 0.1f)
            {
                animator.SetFloat("MoveX", moveDirection.x);
                animator.SetFloat("MoveY", moveDirection.y);
            }
        }
    }

    // Public methods for dash attack integration

    // Enable or disable player movement control
    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;

        // If disabling movement and not during dash, stop the player
        if (!enabled && rb != null)
        {
            // Don't set velocity to zero here, as dash might be controlling velocity
        }
    }

    // Get player model for other components
    public PlayerModel GetPlayerModel()
    {
        return playerModel;
    }
}