using UnityEngine;

/// <summary>
/// Rotates forklift wheels based on movement speed
/// Attach to each wheel object
/// </summary>
public class WheelRotation : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Reference to the forklift's Rigidbody to get speed")]
    [SerializeField] private Rigidbody forkliftRigidbody;
    
    [Tooltip("Radius of the wheel in meters (for realistic rotation speed)")]
    [SerializeField] private float wheelRadius = 0.3f;
    
    [Tooltip("Is this a steering wheel? (front wheels turn)")]
    [SerializeField] private bool isSteeringWheel = false;
    
    [Tooltip("Maximum steering angle if this is a steering wheel")]
    [SerializeField] private float maxSteerAngle = 35f;
    
    // Reference to forklift controller for steering angle
    private ForkliftController forkliftController;
    private float currentRotation = 0f;
    
    private void Start()
    {
        // Try to find forklift rigidbody if not assigned
        if (forkliftRigidbody == null)
        {
            // Look for rigidbody in parent objects
            forkliftRigidbody = GetComponentInParent<Rigidbody>();
        }
        
        // Get forklift controller for steering
        if (forkliftRigidbody != null)
        {
            forkliftController = forkliftRigidbody.GetComponent<ForkliftController>();
        }
        
        if (forkliftRigidbody == null)
        {
            Debug.LogWarning($"WheelRotation on {gameObject.name}: No Rigidbody found! Wheel won't rotate.");
        }
    }
    
    private void Update()
    {
        if (forkliftRigidbody == null) return;
        
        // Get forklift's forward speed
        float forwardSpeed = Vector3.Dot(forkliftRigidbody.velocity, forkliftRigidbody.transform.forward);
        
        // Calculate rotation speed based on wheel circumference
        // Speed (m/s) / Circumference (m) = Rotations per second
        float circumference = 2f * Mathf.PI * wheelRadius;
        float rotationsPerSecond = forwardSpeed / circumference;
        float degreesPerSecond = rotationsPerSecond * 360f;
        
        // Rotate wheel around its X-axis (forward rotation)
        currentRotation += degreesPerSecond * Time.deltaTime;
        transform.localRotation = Quaternion.Euler(currentRotation, 0f, 0f);
        
        // If this is a steering wheel, also rotate around Y-axis
        if (isSteeringWheel && forkliftController != null)
        {
            // Get steering input from controller
            // We'll need to add a public method to get this
            // For now, we can estimate based on angular velocity
            float steerAngle = 0f;
            
            // Simple steering visualization based on turning
            if (forkliftRigidbody.angularVelocity.y != 0)
            {
                steerAngle = Mathf.Clamp(forkliftRigidbody.angularVelocity.y * 10f, -maxSteerAngle, maxSteerAngle);
            }
            
            // Apply steering rotation (around Y-axis, in addition to rolling)
            transform.localRotation = Quaternion.Euler(currentRotation, steerAngle, 0f);
        }
    }
}
