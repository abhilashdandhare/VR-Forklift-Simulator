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
    
    private List<PalletPickup> nearbPallets = new List<PalletPickup>();
    private PalletPickup currentlyHeldPallet;
    private float lastCheckTime;
    
    private void Start()
    {
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
        if (Time.time - lastCheckTime > checkInterval)
        {
            lastCheckTime = Time.time;
            CheckPickupDrop();
        }
    }
    
    private void FixedUpdate()
    {
        DetectNearbyPallets();
    }
    

    private void DetectNearbyPallets()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRadius);
        
        HashSet<PalletPickup> currentlyNear = new HashSet<PalletPickup>();
        
        foreach (Collider col in colliders)
        {
            PalletPickup pallet = col.GetComponent<PalletPickup>();
            
            if (pallet != null)
            {
                currentlyNear.Add(pallet);
                
                if (!nearbyPallets.Contains(pallet))
                {
                    pallet.OnForksEnter(transform);
                    nearbyPallets.Add(pallet);
                }
            }
        }
        
        for (int i = nearbyPallets.Count - 1; i >= 0; i--)
        {
            if (!currentlyNear.Contains(nearbyPallets[i]))
            {
                nearbyPallets[i].OnForksExit();
                nearbyPallets.RemoveAt(i);
            }
        }
    }

    private void CheckPickupDrop()
    {
        if (forkliftController == null) return;
        
        float currentForkHeight = forkliftController.GetForkHeight();
        
        if (currentlyHeldPallet != null)
        {
            if (currentlyHeldPallet.TryDrop(currentForkHeight))
            {
                currentlyHeldPallet = null;
            }
        }
        else
        {
            foreach (PalletPickup pallet in nearbyPallets)
            {
                if (pallet.TryPickup(transform, currentForkHeight))
                {
                    currentlyHeldPallet = pallet;
                    break; 
                }
            }
        }
    }

    public PalletPickup GetHeldPallet()
    {
        return currentlyHeldPallet;
    }
    

    public bool IsHoldingPallet()
    {
        return currentlyHeldPallet != null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
