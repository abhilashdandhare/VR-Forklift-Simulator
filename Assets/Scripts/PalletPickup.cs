using UnityEngine;

/// <summary>
/// Makes an object pickupable by forklift forks
/// Handles attachment, detachment, and physics
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PalletPickup : MonoBehaviour
{
    [Header("Pickup Settings")]
    [Tooltip("Height threshold - fork must be this high to pick up pallet")]
    [SerializeField] private float pickupHeightThreshold = 0.3f;
    
    [Tooltip("Height threshold - fork must be this low to drop pallet")]
    [SerializeField] private float dropHeightThreshold = 0.2f;
    
    [Tooltip("Visual feedback color when near forks")]
    [SerializeField] private Color nearForksColor = Color.yellow;
    
    [Tooltip("Vertical offset - adjust this to make pallet sit properly on fork (negative = lower)")]
    [SerializeField] private float verticalOffset = -0.5f;
    
    [Header("Status (Read-Only - for debugging)")]
    [SerializeField] private bool isPickedUp = false;
    [SerializeField] private bool isNearForks = false;
    
    // Components
    private Rigidbody rb;
    private Collider palletCollider;
    private Transform attachedToFork;
    private Vector3 localAttachOffset;
    private Material[] originalMaterials;
    private Renderer[] renderers;
    
    // Original physics settings
    private bool originalUseGravity;
    private bool originalIsKinematic;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        palletCollider = GetComponent<Collider>();
        renderers = GetComponentsInChildren<Renderer>();
        
        // Store original materials for highlight effect
        if (renderers.Length > 0)
        {
            originalMaterials = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                originalMaterials[i] = renderers[i].material;
            }
        }
        
        // Store original physics settings
        originalUseGravity = rb.useGravity;
        originalIsKinematic = rb.isKinematic;
    }
    
    private void Update()
    {
        // If attached to fork, follow fork position
        if (isPickedUp && attachedToFork != null)
        {
            // Keep pallet at a fixed offset from the fork
            // This maintains the relative position as the fork lifts/lowers
            transform.position = attachedToFork.TransformPoint(localAttachOffset);
            transform.rotation = attachedToFork.rotation;
        }
    }
    
    /// <summary>
    /// Called when forks enter the pallet's trigger zone
    /// </summary>
    public void OnForksEnter(Transform forkTransform)
    {
        isNearForks = true;
        
        // Visual feedback - highlight pallet
        HighlightPallet(true);
        
        Debug.Log($"[Pallet] Forks near {gameObject.name}");
    }
    
    /// <summary>
    /// Called when forks exit the pallet's trigger zone
    /// </summary>
    public void OnForksExit()
    {
        isNearForks = false;
        
        // Remove highlight
        if (!isPickedUp)
        {
            HighlightPallet(false);
        }
        
        Debug.Log($"[Pallet] Forks left {gameObject.name}");
    }
    
    /// <summary>
    /// Attempt to pick up pallet when forks lift
    /// </summary>
    public bool TryPickup(Transform forkTransform, float forkHeight)
    {
        // Can't pick up if already picked up
        if (isPickedUp) return false;
        
        // Forks must be near
        if (!isNearForks) return false;
        
        // Fork must be high enough
        if (forkHeight < pickupHeightThreshold) return false;
        
        // Attach pallet to fork
        AttachToFork(forkTransform);
        
        return true;
    }
    
    /// <summary>
    /// Attempt to drop pallet when forks lower
    /// </summary>
    public bool TryDrop(float forkHeight)
    {
        // Can't drop if not picked up
        if (!isPickedUp) return false;
        
        // Fork must be low enough
        if (forkHeight > dropHeightThreshold) return false;
        
        // Detach pallet
        DetachFromFork();
        
        return true;
    }
    
    /// <summary>
    /// Attach pallet to fork
    /// </summary>
    private void AttachToFork(Transform forkTransform)
    {
        isPickedUp = true;
        attachedToFork = forkTransform;
        
        // Calculate local offset from fork (in fork's local space)
        localAttachOffset = forkTransform.InverseTransformPoint(transform.position);
        
        // Apply vertical offset to make pallet sit ON the fork (not inside it)
        localAttachOffset.y += verticalOffset;
        
        // Disable physics while attached
        rb.useGravity = false;
        rb.isKinematic = true;
        
        // Disable collider to prevent dragging on ground
        if (palletCollider != null)
        {
            palletCollider.enabled = false;
        }
        
        // Change highlight color
        HighlightPallet(true, Color.green);
        
        Debug.Log($"[Pallet] {gameObject.name} picked up! Offset: {localAttachOffset}");
    }
    
    /// <summary>
    /// Detach pallet from fork
    /// </summary>
    private void DetachFromFork()
    {
        isPickedUp = false;
        attachedToFork = null;
        
        // Re-enable physics
        rb.useGravity = originalUseGravity;
        rb.isKinematic = originalIsKinematic;
        
        // Re-enable collider
        if (palletCollider != null)
        {
            palletCollider.enabled = true;
        }
        
        // Reset velocity to prevent flying away
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        
        // Remove highlight
        HighlightPallet(false);
        
        Debug.Log($"[Pallet] {gameObject.name} dropped!");
    }
    
    /// <summary>
    /// Highlight pallet with color
    /// </summary>
    private void HighlightPallet(bool highlight, Color? color = null)
    {
        if (renderers.Length == 0) return;
        
        Color highlightColor = color ?? nearForksColor;
        
        for (int i = 0; i < renderers.Length; i++)
        {
            if (highlight)
            {
                // Create new material with emission
                Material highlightMat = new Material(originalMaterials[i]);
                highlightMat.EnableKeyword("_EMISSION");
                highlightMat.SetColor("_EmissionColor", highlightColor * 0.3f);
                renderers[i].material = highlightMat;
            }
            else
            {
                // Restore original material
                renderers[i].material = originalMaterials[i];
            }
        }
    }
    
    /// <summary>
    /// Check if pallet is currently picked up
    /// </summary>
    public bool IsPickedUp()
    {
        return isPickedUp;
    }
    
    /// <summary>
    /// Check if pallet is near forks
    /// </summary>
    public bool IsNearForks()
    {
        return isNearForks;
    }
    
    /// <summary>
    /// Force drop (for emergency situations)
    /// </summary>
    public void ForceDrop()
    {
        if (isPickedUp)
        {
            DetachFromFork();
        }
    }
}