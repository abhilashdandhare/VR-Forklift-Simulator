using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Detects pallets near forklift forks and triggers pickup/drop
/// Attach this to the Forklift.Fork object with a trigger collider
/// UPDATED: Moves Box Collider to follow fork bone height!
/// </summary>
public class ForkDetector : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the ForkliftController to get fork height")]
    [SerializeField] private ForkliftController forkliftController;
    
    [Tooltip("Reference to the fork bone (Def-Fork) that actually moves up/down")]
    [SerializeField] private Transform forkBone;
    
    [Header("Detection Settings")]
    [Tooltip("How often to check for pickups (seconds)")]
    [SerializeField] private float checkInterval = 0.2f;
    
    // Tracking
    private List<PalletPickup> nearbyPallets = new List<PalletPickup>();
    private PalletPickup currentlyHeldPallet;
    private float lastCheckTime;
    
    // Collider tracking - CRITICAL FIX!
    private BoxCollider boxCollider;
    private Vector3 initialColliderCenter;
    private float initialForkHeight;
    
    private void Start()
    {
        // Get box collider reference
        boxCollider = GetComponent<BoxCollider>();
        
        if (boxCollider != null)
        {
            initialColliderCenter = boxCollider.center;
            Debug.Log($"[ForkDetector] Initial collider center: {initialColliderCenter}");
        }
        else
        {
            Debug.LogError("[ForkDetector] No BoxCollider found! Add a Box Collider with Is Trigger = true");
        }
        
        // Try to find forklift controller if not assigned
        if (forkliftController == null)
        {
            forkliftController = GetComponentInParent<ForkliftController>();
            
            if (forkliftController == null)
            {
                Debug.LogError("[ForkDetector] No ForkliftController found! Assign it manually.");
            }
        }
        
        // Try to find fork bone if not assigned
        if (forkBone == null)
        {
            // Look for Def-Fork in parent hierarchy
            Transform current = transform;
            while (current != null)
            {
                Transform bone = FindDeepChild(current, "Def-Fork");
                if (bone != null)
                {
                    forkBone = bone;
                    Debug.Log("[ForkDetector] Auto-found Def-Fork bone");
                    break;
                }
                current = current.parent;
            }
            
            if (forkBone == null)
            {
                Debug.LogWarning("[ForkDetector] Fork bone not found! Pallets won't lift properly. Assign 'Def-Fork' manually.");
            }
        }
        
        // Store initial fork height
        if (forkBone != null)
        {
            initialForkHeight = forkBone.localPosition.z; // Z-axis for fork height
            Debug.Log($"[ForkDetector] Initial fork height: {initialForkHeight}");
        }
    }
    
    private void Update()
    {
        // CRITICAL: Move collider to follow fork bone!
        UpdateColliderPosition();
        
        // Periodically check for pickup/drop conditions
        if (Time.time - lastCheckTime > checkInterval)
        {
            lastCheckTime = Time.time;
            CheckPickupDrop();
        }
    }
    
    /// <summary>
    /// Move the box collider center to follow fork bone height
    /// </summary>
    private void UpdateColliderPosition()
    {
        if (boxCollider == null || forkBone == null) return;
        
        // Calculate fork height offset from initial position
        float currentForkHeight = forkBone.localPosition.z;
        float heightOffset = currentForkHeight - initialForkHeight;
        
        // Update collider center Y position to match fork height
        // Note: Collider uses Y-axis in local space, bone uses Z-axis
        Vector3 newCenter = initialColliderCenter;
        newCenter.y += heightOffset;
        
        boxCollider.center = newCenter;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Check if the collider belongs to a pallet
        PalletPickup pallet = other.GetComponent<PalletPickup>();
        
        if (pallet != null && !nearbyPallets.Contains(pallet))
        {
            // Pass the fork BONE (not the mesh) so pallet follows vertical movement
            Transform attachPoint = forkBone != null ? forkBone : transform;
            pallet.OnForksEnter(attachPoint);
            nearbyPallets.Add(pallet);
            Debug.Log($"[ForkDetector] Pallet entered fork zone: {pallet.name}");
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        // Check if the collider belongs to a pallet
        PalletPickup pallet = other.GetComponent<PalletPickup>();
        
        if (pallet != null && nearbyPallets.Contains(pallet))
        {
            pallet.OnForksExit();
            nearbyPallets.Remove(pallet);
            Debug.Log($"[ForkDetector] Pallet exited fork zone: {pallet.name}");
        }
    }
    
    private void CheckPickupDrop()
    {
        if (forkliftController == null) return;
        
        // Get current fork height from bone
        float currentHeight = forkBone != null ? forkBone.localPosition.z : 0f;
        
        // DEBUG: Show fork height
        if (nearbyPallets.Count > 0)
        {
            Debug.Log($"[ForkDetector] Fork height: {currentHeight:F2}, Checking {nearbyPallets.Count} pallets");
        }
        
        // Check all nearby pallets
        for (int i = nearbyPallets.Count - 1; i >= 0; i--)
        {
            PalletPickup pallet = nearbyPallets[i];
            
            if (pallet == null)
            {
                nearbyPallets.RemoveAt(i);
                continue;
            }
            
            // Try to pick up if not already picked up
            if (!pallet.IsPickedUp())
            {
                bool pickedUp = pallet.TryPickup(forkBone != null ? forkBone : transform, currentHeight);
                if (pickedUp)
                {
                    currentlyHeldPallet = pallet;
                    Debug.Log($"[ForkDetector] Successfully picked up {pallet.name}");
                }
            }
            // Try to drop if currently picked up
            else
            {
                bool dropped = pallet.TryDrop(currentHeight);
                if (dropped)
                {
                    currentlyHeldPallet = null;
                    Debug.Log($"[ForkDetector] Dropped {pallet.name}");
                }
            }
        }
    }
    
    /// <summary>
    /// Recursively search for a child by name
    /// </summary>
    private Transform FindDeepChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;
            
            Transform found = FindDeepChild(child, childName);
            if (found != null)
                return found;
        }
        return null;
    }
    
    /// <summary>
    /// Get current fork height for external reference
    /// </summary>
    public float GetCurrentForkHeight()
    {
        return forkBone != null ? forkBone.localPosition.z : 0f;
    }
    
    /// <summary>
    /// Check if currently holding a pallet
    /// </summary>
    public bool IsHoldingPallet()
    {
        return currentlyHeldPallet != null;
    }
    
    /// <summary>
    /// Get the currently held pallet (if any)
    /// </summary>
    public PalletPickup GetHeldPallet()
    {
        return currentlyHeldPallet;
    }
    
    private void OnDrawGizmos()
    {
        // Draw the collider in scene view for debugging
        if (boxCollider != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
        }
    }
}