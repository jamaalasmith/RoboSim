using UnityEngine;


public class RoverController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotateSpeed = 90f;
    public float maxBatteryLevel = 100f;

    [Header("Input Settings")]
    public KeyCode forwardKey = KeyCode.W;
    public KeyCode backwardKey = KeyCode.S;
    public KeyCode leftKey = KeyCode.A;
    public KeyCode rightKey = KeyCode.D;

    [Header("Physics")]
    public float batteryDrainRate = 1f; // per second when moving

    private Rigidbody rb;
    private float currentBatteryLevel;
    private Vector3 moveDirection;
    private float rotationInput;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        currentBatteryLevel = maxBatteryLevel;

        // Add Rigidbody if not present
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        Debug.Log("RoboSim Rover initialized. Use WASD keys to control.");
    }

    void Update()
    {
        // Get input
        HandleInput();

        // Update UI display
        UpdateUI();
    }

    void FixedUpdate()
    {
        // Apply movement if battery has charge
        if (currentBatteryLevel > 0)
        {
            ApplyMovement();
            UpdateBattery();
        }
        else
        {
            // Stop movement when battery is empty
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    void HandleInput()
    {
        // Reset movement
        moveDirection = Vector3.zero;
        rotationInput = 0f;

        // Forward/Backward movement
        if (Input.GetKey(forwardKey))
            moveDirection += Vector3.forward;
        if (Input.GetKey(backwardKey))
            moveDirection += Vector3.back;

        // Rotation
        if (Input.GetKey(leftKey))
            rotationInput = -1f;
        if (Input.GetKey(rightKey))
            rotationInput = 1f;
    }

    void ApplyMovement()
    {
        // Apply linear movement relative to rover orientation
        Vector3 worldMoveDirection = transform.TransformDirection(moveDirection);
        rb.velocity = new Vector3(
            worldMoveDirection.x * moveSpeed,
            rb.velocity.y,
            worldMoveDirection.z * moveSpeed
        );

        // Apply rotation
        if (Mathf.Abs(rotationInput) > 0.1f)
        {
            rb.angularVelocity = new Vector3(0, rotationInput * rotateSpeed * Mathf.Deg2Rad, 0);
        }
    }

    void UpdateBattery()
    {
        // Drain battery when moving
        bool isMoving = moveDirection.magnitude > 0.1f || Mathf.Abs(rotationInput) > 0.1f;

        if (isMoving)
        {
            currentBatteryLevel -= batteryDrainRate * Time.fixedDeltaTime;
            currentBatteryLevel = Mathf.Max(0, currentBatteryLevel);
        }
    }

    void UpdateUI()
    {
        // Display battery level in console (replace with UI later)
        if (Time.time % 2f < Time.deltaTime) // Every 2 seconds
        {
            Debug.Log($"Battery Level: {currentBatteryLevel:F1}%");

            if (currentBatteryLevel <= 0)
            {
                Debug.Log("Battery depleted! Rover stopped.");
            }
        }
    }

    // Public methods for external control (if needed)
    public void RechargeBattery()
    {
        currentBatteryLevel = maxBatteryLevel;
        Debug.Log("Battery recharged to 100%");
    }

    public float GetBatteryLevel()
    {
        return currentBatteryLevel;
    }

    public bool IsMoving()
    {
        return rb.velocity.magnitude > 0.1f || rb.angularVelocity.magnitude > 0.1f;
    }

    // Called when rover hits something
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"Rover collided with: {collision.gameObject.name}");
    }
}