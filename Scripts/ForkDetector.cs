using UnityEngine;
using System.Collections.Generic;

public class ForkDetector : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the ForkliftController to get fork height")]
    [SerializeField] private ForkliftController forkliftController;
    
    [Header("Detection Settings")]
    [Tooltip("Detection radius around forks")]
    [SerializeField] private float detectionRadius = 1.5f;
    
    [Tooltip("How often to check for pickups (seconds)")]
    [SerializeField] private float checkInterval = 0.2f;
    
    // Tracking
    private List<PalletPickup> nearbPallets = new List<PalletPickup>();
    private PalletPickup currentlyHeldPallet;
    private float lastCheckTime;
    
    private void Start()
    {
        // Try to find forklift controller if not assigned
        if (forkliftController == null)
        {
            forkliftController = GetComponentInParent<ForkliftController>();
            
            if (forkliftController == null)
            {
                Debug.LogError("[ForkDetector] No ForkliftController found! Assign it manually.");
            }
        }
    }
    
    private void Update()
    {
        // Periodically check for pickup/drop conditions
        if (Time.time - lastCheckTime > checkInterval)
        {
            lastCheckTime = Time.time;
            CheckPickupDrop();
        }
    }
    
    private void FixedUpdate()
    {
        // Detect nearby pallets
        DetectNearbyPallets();
    }
    
    /// <summary>
    /// Detect pallets within detection radius
    /// </summary>
    private void DetectNearbyPallets()
    {
        // Find all colliders in radius
        Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRadius);
        
        // Track which pallets are currently near
        HashSet<PalletPickup> currentlyNear = new HashSet<PalletPickup>();
        
        foreach (Collider col in colliders)
        {
            PalletPickup pallet = col.GetComponent<PalletPickup>();
            
            if (pallet != null)
            {
                currentlyNear.Add(pallet);
                
                // If this is a new pallet, notify it
                if (!nearbyPallets.Contains(pallet))
                {
                    pallet.OnForksEnter(transform);
                    nearbyPallets.Add(pallet);
                }
            }
        }
        
        // Check for pallets that left the radius
        for (int i = nearbyPallets.Count - 1; i >= 0; i--)
        {
            if (!currentlyNear.Contains(nearbyPallets[i]))
            {
                nearbyPallets[i].OnForksExit();
                nearbyPallets.RemoveAt(i);
            }
        }
    }
    
    /// <summary>
    /// Check if conditions are right for pickup or drop
    /// </summary>
    private void CheckPickupDrop()
    {
        if (forkliftController == null) return;
        
        float currentForkHeight = forkliftController.GetForkHeight();
        
        // Check for DROP (if currently holding a pallet)
        if (currentlyHeldPallet != null)
        {
            if (currentlyHeldPallet.TryDrop(currentForkHeight))
            {
                currentlyHeldPallet = null;
            }
        }
        // Check for PICKUP (if not holding anything)
        else
        {
            // Try to pick up the nearest pallet
            foreach (PalletPickup pallet in nearbyPallets)
            {
                if (pallet.TryPickup(transform, currentForkHeight))
                {
                    currentlyHeldPallet = pallet;
                    break; // Only pick up one at a time
                }
            }
        }
    }
    
    /// <summary>
    /// Get currently held pallet (null if none)
    /// </summary>
    public PalletPickup GetHeldPallet()
    {
        return currentlyHeldPallet;
    }
    
    /// <summary>
    /// Check if currently holding a pallet
    /// </summary>
    public bool IsHoldingPallet()
    {
        return currentlyHeldPallet != null;
    }
    
    /// <summary>
    /// Visualize detection radius in editor
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
