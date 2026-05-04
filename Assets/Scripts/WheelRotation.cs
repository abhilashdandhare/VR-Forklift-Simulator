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
            // Look up the hierarchy for "Forklift Truck" object
            Transform current = transform;
            while (current != null)
            {
                if (current.name.Contains("Forklift Truck") || current.name.Contains("ForkliftTruck"))
                {
                    forkliftRigidbody = current.GetComponent<Rigidbody>();
                    if (forkliftRigidbody != null)
                    {
                        Debug.Log($"[WheelRotation] {gameObject.name} found Rigidbody on {current.name}");
                        break;
                    }
                }
                current = current.parent;
            }
            
            // If still not found, try GetComponentInParent
            if (forkliftRigidbody == null)
            {
                forkliftRigidbody = GetComponentInParent<Rigidbody>();
                if (forkliftRigidbody != null)
                {
                    Debug.Log($"[WheelRotation] {gameObject.name} found Rigidbody via GetComponentInParent");
                }
            }
        }
        else
        {
            Debug.Log($"[WheelRotation] {gameObject.name} using manually assigned Rigidbody");
        }
        
        // Get forklift controller for steering
        if (forkliftRigidbody != null)
        {
            forkliftController = forkliftRigidbody.GetComponent<ForkliftController>();
        }
        
        if (forkliftRigidbody == null)
        {
            Debug.LogError($"[WheelRotation] {gameObject.name}: No Rigidbody found! Wheel won't rotate.");
        }
    }
    
    private void Update()
    {
        if (forkliftRigidbody == null) return;
        
        // Get forklift's forward speed from controller (more reliable than rigidbody.velocity)
        float forwardSpeed = 0f;
        
        if (forkliftController != null)
        {
            forwardSpeed = forkliftController.GetForwardSpeed();
        }
        else
        {
            // Fallback: try to get from velocity
            forwardSpeed = Vector3.Dot(forkliftRigidbody.linearVelocity, forkliftRigidbody.transform.forward);
        }
        
        // Debug every 60 frames (roughly once per second)
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[WheelRotation] {gameObject.name}: Speed={forwardSpeed:F2}, Rotation={currentRotation:F1}");
        }
        
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