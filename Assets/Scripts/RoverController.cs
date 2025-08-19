using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class SensorData
{
    public string name;
    public Vector3 direction;
    public bool hasObstacle;
    public float distance;
    public LineRenderer lineRenderer;

    public SensorData(string sensorName, Vector3 sensorDirection)
    {
        name = sensorName;
        direction = sensorDirection;
        hasObstacle = false;
        distance = 0f;
    }
}

public class RoverController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float rotateSpeed = 150f;
    public float maxBatteryLevel = 100f;

    [Header("Input Settings")]
    public KeyCode forwardKey = KeyCode.W;
    public KeyCode backwardKey = KeyCode.S;
    public KeyCode leftKey = KeyCode.A;
    public KeyCode rightKey = KeyCode.D;

    [Header("Physics")]
    public float batteryDrainRate = 1f; // per second when moving

    [Header("Obstacle Avoidance")]
    public float sensorRange = 3f;
    public LayerMask obstacleLayerMask = -1; // Default to all layers
    public float avoidanceStrength = 2f;
    public Color clearSensorColor = Color.green;
    public Color obstacleDetectedColor = Color.red;
    public float sensorLineWidth = 0.05f;

    [Header("UI")]
    public Text obstacleAvoidanceStatusText;

    private Rigidbody rb;
    private float currentBatteryLevel;
    private Vector3 moveDirection;
    private float rotationInput;

    // Obstacle detection
    private SensorData[] sensors;
    private bool isObstacleAvoidanceActive = false;
    private Vector3 avoidanceDirection = Vector3.zero;
    private float avoidanceRotation = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        currentBatteryLevel = maxBatteryLevel;

        // Add Rigidbody if not present
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        // Initialize obstacle detection sensors
        InitializeSensors();

        Debug.Log("RoboSim Rover initialized with obstacle avoidance. Use WASD keys to control.");
    }

    void Update()
    {
        // Detect obstacles
        DetectObstacles();

        // Calculate obstacle avoidance
        CalculateObstacleAvoidance();

        // Get input
        HandleInput();

        // Update sensor visualization
        UpdateSensorVisualization();

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
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    void HandleInput()
    {
        // Get manual input
        Vector3 manualMoveDirection = Vector3.zero;
        float manualRotationInput = 0f;

        // Forward/Backward movement
        if (Input.GetKey(forwardKey))
            manualMoveDirection += Vector3.forward;
        if (Input.GetKey(backwardKey))
            manualMoveDirection += Vector3.back;

        // Rotation
        if (Input.GetKey(leftKey))
            manualRotationInput = -1f;
        if (Input.GetKey(rightKey))
            manualRotationInput = 1f;

        // Apply obstacle avoidance override when active
        if (isObstacleAvoidanceActive)
        {
            // Override manual input with safety controls
            moveDirection = avoidanceDirection;
            rotationInput = avoidanceRotation;

            // Allow manual backward movement for escape
            if (manualMoveDirection.z < 0)
            {
                moveDirection.z = manualMoveDirection.z;
            }

            // Blend manual rotation with avoidance if they're in same direction
            if (Mathf.Sign(manualRotationInput) == Mathf.Sign(avoidanceRotation) || manualRotationInput == 0)
            {
                rotationInput = Mathf.Max(Mathf.Abs(manualRotationInput), Mathf.Abs(avoidanceRotation)) * Mathf.Sign(avoidanceRotation);
            }
        }
        else
        {
            // Normal manual control when path is clear
            moveDirection = manualMoveDirection;
            rotationInput = manualRotationInput;
        }
    }

    void ApplyMovement()
    {
        // Apply linear movement relative to rover orientation
        Vector3 worldMoveDirection = transform.TransformDirection(moveDirection);
        rb.linearVelocity = new Vector3(
            worldMoveDirection.x * moveSpeed,
            rb.linearVelocity.y,
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

        // Update obstacle avoidance status UI
        if (obstacleAvoidanceStatusText != null)
        {
            if (isObstacleAvoidanceActive)
            {
                obstacleAvoidanceStatusText.text = "OBSTACLE AVOIDANCE ACTIVE";
                obstacleAvoidanceStatusText.color = obstacleDetectedColor;
            }
            else
            {
                obstacleAvoidanceStatusText.text = "Path Clear";
                obstacleAvoidanceStatusText.color = clearSensorColor;
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
        return rb.linearVelocity.magnitude > 0.1f || rb.angularVelocity.magnitude > 0.1f;
    }

    // Initialize the 5 obstacle detection sensors
    void InitializeSensors()
    {
        sensors = new SensorData[5];

        // Define sensor directions (front, front-left, front-right, left, right)
        sensors[0] = new SensorData("Front", Vector3.forward);
        sensors[1] = new SensorData("Front-Left", (Vector3.forward + Vector3.left).normalized);
        sensors[2] = new SensorData("Front-Right", (Vector3.forward + Vector3.right).normalized);
        sensors[3] = new SensorData("Left", Vector3.left);
        sensors[4] = new SensorData("Right", Vector3.right);

        // Create LineRenderer components for visualization
        for (int i = 0; i < sensors.Length; i++)
        {
            GameObject sensorLine = new GameObject($"SensorLine_{sensors[i].name}");
            sensorLine.transform.SetParent(transform);
            sensorLine.transform.localPosition = Vector3.zero;

            LineRenderer lr = sensorLine.AddComponent<LineRenderer>();
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.material.color = clearSensorColor;
            lr.startWidth = sensorLineWidth;
            lr.endWidth = sensorLineWidth;
            lr.positionCount = 2;
            lr.useWorldSpace = true;

            sensors[i].lineRenderer = lr;
        }
    }

    // Cast rays to detect obstacles
    void DetectObstacles()
    {
        for (int i = 0; i < sensors.Length; i++)
        {
            Vector3 rayDirection = transform.TransformDirection(sensors[i].direction);
            Vector3 rayOrigin = transform.position + Vector3.up * 0.5f; // Slightly above ground

            RaycastHit hit;
            if (Physics.Raycast(rayOrigin, rayDirection, out hit, sensorRange, obstacleLayerMask))
            {
                sensors[i].hasObstacle = true;
                sensors[i].distance = hit.distance;
            }
            else
            {
                sensors[i].hasObstacle = false;
                sensors[i].distance = sensorRange;
            }
        }
    }

    // Calculate avoidance direction based on sensor data
    void CalculateObstacleAvoidance()
    {
        isObstacleAvoidanceActive = false;
        avoidanceDirection = Vector3.zero;
        avoidanceRotation = 0f;

        // Check for obstacles that require avoidance
        bool frontObstacle = sensors[0].hasObstacle;
        bool frontLeftObstacle = sensors[1].hasObstacle;
        bool frontRightObstacle = sensors[2].hasObstacle;
        bool leftObstacle = sensors[3].hasObstacle;
        bool rightObstacle = sensors[4].hasObstacle;

        // If front sensor detects obstacle, turn away
        if (frontObstacle)
        {
            isObstacleAvoidanceActive = true;

            // Prefer turning toward the side with more clearance
            if (!rightObstacle && !frontRightObstacle)
            {
                avoidanceRotation = avoidanceStrength; // Turn right
            }
            else if (!leftObstacle && !frontLeftObstacle)
            {
                avoidanceRotation = -avoidanceStrength; // Turn left
            }
            else
            {
                // Both sides blocked, turn toward less blocked side
                float leftClearance = leftObstacle ? 0f : sensors[3].distance;
                float rightClearance = rightObstacle ? 0f : sensors[4].distance;

                if (rightClearance > leftClearance)
                {
                    avoidanceRotation = avoidanceStrength;
                }
                else
                {
                    avoidanceRotation = -avoidanceStrength;
                }
            }

            // Reduce forward speed when obstacle ahead
            avoidanceDirection = Vector3.back * 0.5f;
        }

        // Handle side obstacles
        if (leftObstacle && !frontObstacle)
        {
            isObstacleAvoidanceActive = true;
            avoidanceRotation += avoidanceStrength * 0.5f; // Gentle turn right
        }

        if (rightObstacle && !frontObstacle)
        {
            isObstacleAvoidanceActive = true;
            avoidanceRotation -= avoidanceStrength * 0.5f; // Gentle turn left
        }
    }

    // Update visual representation of sensors
    void UpdateSensorVisualization()
    {
        for (int i = 0; i < sensors.Length; i++)
        {
            Vector3 rayDirection = transform.TransformDirection(sensors[i].direction);
            Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
            Vector3 rayEnd = rayOrigin + rayDirection * sensors[i].distance;

            // Update line positions
            sensors[i].lineRenderer.SetPosition(0, rayOrigin);
            sensors[i].lineRenderer.SetPosition(1, rayEnd);

            // Update color based on obstacle detection
            sensors[i].lineRenderer.material.color = sensors[i].hasObstacle ? obstacleDetectedColor : clearSensorColor;
        }
    }

    // Called when rover hits something
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"Rover collided with: {collision.gameObject.name}");
    }
}