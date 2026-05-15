using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;


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
    [Tooltip("Reference to the fork object that moves up/down")]
    [SerializeField] private Transform forkTransform;
    
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

        if (forkTransform != null)
        {
            currentForkHeight = forkTransform.localPosition.y;
        }
        else
        {
            Debug.LogWarning("Fork Transform not assigned! Fork controls won't work.");
        }
    }
    
    private void Update()
    {
  
        HandleForkControls();
    }
    
    private void FixedUpdate()
    {

        HandleMovement();
        HandleSteering();
    }

    private void HandleMovement()
    {
        if (rb == null) return;
        
  
        float gasInput = 0f;
        float brakeInput = 0f;
        
        if (rightController != null)
        {

            rightController.inputDevice.TryGetFeatureValue(
                UnityEngine.XR.CommonUsages.trigger, out gasInput);
        }
        
        if (leftController != null)
        {

            leftController.inputDevice.TryGetFeatureValue(
                UnityEngine.XR.CommonUsages.trigger, out brakeInput);
        }
        

        float targetSpeed = (gasInput - brakeInput) * maxSpeed;
        

        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, 
            acceleration * Time.fixedDeltaTime);

        Vector3 movement = transform.forward * currentSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movement);
    }

    private void HandleSteering()
    {
        if (rb == null) return;
        

        Vector2 thumbstick = Vector2.zero;
        
        if (leftController != null)
        {
            leftController.inputDevice.TryGetFeatureValue(
                UnityEngine.XR.CommonUsages.primary2DAxis, out thumbstick);
        }
        

        float steerInput = thumbstick.x;
        

        float targetAngle = steerInput * maxSteeringAngle;
        

        currentSteerAngle = Mathf.MoveTowards(currentSteerAngle, targetAngle,
            steeringSensitivity * maxSteeringAngle * Time.fixedDeltaTime);
        

        if (Mathf.Abs(currentSpeed) > 0.1f)
        {
            float rotationAmount = currentSteerAngle * (currentSpeed / maxSpeed) * Time.fixedDeltaTime;
            Quaternion deltaRotation = Quaternion.Euler(0f, rotationAmount, 0f);
            rb.MoveRotation(rb.rotation * deltaRotation);
        }
    }
    

    private void HandleForkControls()
    {
        if (forkTransform == null) return;
        
      
        Vector2 thumbstick = Vector2.zero;
        
        if (rightController != null)
        {
            rightController.inputDevice.TryGetFeatureValue(
                UnityEngine.XR.CommonUsages.primary2DAxis, out thumbstick);
        }
        

        float forkInput = thumbstick.y;
        

        float deltaHeight = forkInput * forkSpeed * Time.deltaTime;
        currentForkHeight = Mathf.Clamp(currentForkHeight + deltaHeight, 
            forkMinHeight, forkMaxHeight);
        

        Vector3 forkPos = forkTransform.localPosition;
        forkPos.y = currentForkHeight;
        forkTransform.localPosition = forkPos;
    }
    

    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }
    

    public float GetForkHeight()
    {
        return currentForkHeight;
    }
}
