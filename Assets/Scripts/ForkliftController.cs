using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Controls forklift movement, steering, and fork operations in VR
/// Designed for Meta Quest 3 with realistic forklift physics
/// </summary>
public class ForkliftController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Maximum forward/backward speed in m/s")]
    [SerializeField] private float maxSpeed = 5f;
    
    [Tooltip("How quickly forklift accelerates")]
    [SerializeField] private float acceleration = 3f;
    
    [Tooltip("Maximum steering angle in degrees")]
    [SerializeField] private float maxSteeringAngle = 35f;
    
    [Tooltip("How quickly steering responds")]
    [SerializeField] private float steeringSensitivity = 2f;
    
    [Header("Fork Settings")]
    [Tooltip("Reference to the fork BONE that controls fork movement (e.g., Def-Fork)")]
    [SerializeField] private Transform forkBone;
    
    [Tooltip("Minimum height forks can go (in local Y)")]
    [SerializeField] private float forkMinHeight = 0f;
    
    [Tooltip("Maximum height forks can lift (in local Y)")]
    [SerializeField] private float forkMaxHeight = 3f;
    
    [Tooltip("Speed at which forks lift/lower (m/s)")]
    [SerializeField] private float forkSpeed = 1f;
    
    [Header("VR Input")]
    [Tooltip("Right controller for gas/fork controls")]
    [SerializeField] private XRController rightController;
    
    [Tooltip("Left controller for steering/brake")]
    [SerializeField] private XRController leftController;
    
    // Internal state
    private Rigidbody rb;
    private float currentSpeed = 0f;
    private float currentSteerAngle = 0f;
    private float currentForkHeight = 0f;
    
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        if (rb == null)
        {
            Debug.LogError("ForkliftController requires a Rigidbody component!");
        }
        
        // Initialize fork height from Z-axis (this rig uses Z for fork height)
        if (forkBone != null)
        {
            currentForkHeight = forkBone.localPosition.z;
        }
        else
        {
            Debug.LogWarning("Fork Bone not assigned! Fork controls won't work. Assign 'Def-Fork' from ForkLift_Rig.");
        }
    }
    
    private void Update()
    {
        // Handle fork controls
        HandleForkControls();
    }
    
    private void FixedUpdate()
    {
        // Handle movement and steering
        HandleMovement();
        HandleSteering();
    }
    
    /// <summary>
    /// Handles forward/backward movement using right trigger OR keyboard
    /// </summary>
    private void HandleMovement()
    {
        if (rb == null) return;
        
        // Get input from right controller trigger (0 to 1)
        float gasInput = 0f;
        float brakeInput = 0f;
        
        // VR Controller Input
        if (rightController != null)
        {
            // Right trigger = gas (forward)
            rightController.inputDevice.TryGetFeatureValue(
                UnityEngine.XR.CommonUsages.trigger, out gasInput);
        }
        
        if (leftController != null)
        {
            // Left trigger = brake/reverse
            leftController.inputDevice.TryGetFeatureValue(
                UnityEngine.XR.CommonUsages.trigger, out brakeInput);
        }
        
        // Keyboard Input (for editor testing)
        if (Input.GetKey(KeyCode.W)) gasInput = 1f;
        if (Input.GetKey(KeyCode.S)) brakeInput = 1f;
        
        // Calculate target speed
        float targetSpeed = (gasInput - brakeInput) * maxSpeed;
        
        // Smooth acceleration
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, 
            acceleration * Time.fixedDeltaTime);
        
        // Apply movement in forklift's forward direction
        Vector3 movement = transform.forward * currentSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movement);
    }
    
    /// <summary>
    /// Handles steering using left controller thumbstick OR keyboard
    /// </summary>
    private void HandleSteering()
    {
        if (rb == null) return;
        
        // Get steering input from left thumbstick (-1 to 1)
        Vector2 thumbstick = Vector2.zero;
        
        if (leftController != null)
        {
            leftController.inputDevice.TryGetFeatureValue(
                UnityEngine.XR.CommonUsages.primary2DAxis, out thumbstick);
        }
        
        // Keyboard Input (for editor testing)
        float steerInput = thumbstick.x;
        if (Input.GetKey(KeyCode.A)) steerInput = -1f;
        if (Input.GetKey(KeyCode.D)) steerInput = 1f;
        
        // Calculate target steering angle
        float targetAngle = steerInput * maxSteeringAngle;
        
        // Smooth steering
        currentSteerAngle = Mathf.MoveTowards(currentSteerAngle, targetAngle,
            steeringSensitivity * maxSteeringAngle * Time.fixedDeltaTime);
        
        // Apply rotation (only when moving)
        if (Mathf.Abs(currentSpeed) > 0.1f)
        {
            float rotationAmount = currentSteerAngle * (currentSpeed / maxSpeed) * Time.fixedDeltaTime;
            Quaternion deltaRotation = Quaternion.Euler(0f, rotationAmount, 0f);
            rb.MoveRotation(rb.rotation * deltaRotation);
        }
    }
    
    /// <summary>
    /// Handles fork lift/lower using right controller thumbstick OR keyboard
    /// </summary>
    private void HandleForkControls()
    {
        if (forkBone == null) return;
        
        // Get fork input from right thumbstick Y-axis
        Vector2 thumbstick = Vector2.zero;
        
        if (rightController != null)
        {
            rightController.inputDevice.TryGetFeatureValue(
                UnityEngine.XR.CommonUsages.primary2DAxis, out thumbstick);
        }
        
        // Up = lift, Down = lower
        float forkInput = thumbstick.y;
        
        // Keyboard Input (for editor testing)
        if (Input.GetKey(KeyCode.E)) forkInput = 1f;  // Lift
        if (Input.GetKey(KeyCode.Q)) forkInput = -1f; // Lower
        
        // Calculate new fork height
        float deltaHeight = forkInput * forkSpeed * Time.deltaTime;
        currentForkHeight = Mathf.Clamp(currentForkHeight + deltaHeight, 
            forkMinHeight, forkMaxHeight);
        
        // Apply fork position to the BONE (this controls the rigged mesh)
        // Note: For this rig, Z-axis controls fork height (not Y)
        Vector3 forkPos = forkBone.localPosition;
        forkPos.z = currentForkHeight;  // Changed from Y to Z
        forkBone.localPosition = forkPos;
        
        // Debug to verify it's working
        if (Mathf.Abs(forkInput) > 0.01f)
        {
            Debug.Log($"[Fork] Input: {forkInput:F2}, Height: {currentForkHeight:F2}, BonePos: {forkBone.localPosition}");
        }
    }
    
    /// <summary>
    /// Gets current forklift speed for UI display and wheel rotation
    /// </summary>
    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }
    
    /// <summary>
    /// Gets current speed in m/s (actual velocity magnitude)
    /// </summary>
    public float GetActualSpeed()
    {
        if (rb != null)
        {
            return rb.linearVelocity.magnitude;
        }
        return Mathf.Abs(currentSpeed);
    }
    
    /// <summary>
    /// Gets forward speed (positive = forward, negative = backward)
    /// </summary>
    public float GetForwardSpeed()
    {
        return currentSpeed;
    }
    
    /// <summary>
    /// Gets current fork height for UI display
    /// </summary>
    public float GetForkHeight()
    {
        return currentForkHeight;
    }
}