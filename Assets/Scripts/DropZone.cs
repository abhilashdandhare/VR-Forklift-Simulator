using UnityEngine;

/// <summary>
/// Drop zone that validates pallet placement professionally:
/// - Checks pallet CENTER is inside zone (not just touching)
/// - Snaps pallet to perfect position when placed correctly
/// - Shows visual feedback (yellow → green)
/// - Optional: snaps pallet to center for clean placement
/// </summary>
public class DropZone : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string dropZoneLabel = "Zone A";
    
    [Tooltip("The Quad child object that sits on the floor - used as actual center reference")]
    [SerializeField] private Transform floorMarker;
    
    [Tooltip("How close pallet center must be to zone center to count as valid placement")]
    [SerializeField] private float validationRadius = 1.2f;
    
    [Tooltip("Snap pallet to center of drop zone when placed")]
    [SerializeField] private bool snapToCenter = true;
    
    [Tooltip("How fast pallet snaps to center position")]
    [SerializeField] private float snapSpeed = 8f;

    [Header("Visual")]
    [SerializeField] private Color emptyColor = new Color(1f, 0.85f, 0f, 0.35f);
    [SerializeField] private Color nearColor = new Color(1f, 0.5f, 0f, 0.45f);
    [SerializeField] private Color filledColor = new Color(0f, 1f, 0.3f, 0.5f);
    [SerializeField] private Renderer zoneRenderer;
    [SerializeField] private TMPro.TextMeshPro labelText;

    // State
    public bool IsOccupied { get; private set; }
    public string Label => dropZoneLabel;

    public System.Action<DropZone> OnPalletPlaced;
    public System.Action<DropZone> OnPalletRemoved;

    private PalletPickup currentPallet;
    private bool isSnapping = false;
    private Vector3 snapTargetPosition;

    private void Start()
    {
        SetColor(emptyColor);
        if (labelText != null)
            labelText.text = dropZoneLabel;
    }

    private void Update()
    {
        // Snap pallet smoothly to center if needed
        if (isSnapping && currentPallet != null)
        {
            currentPallet.transform.position = Vector3.Lerp(
                currentPallet.transform.position,
                snapTargetPosition,
                Time.deltaTime * snapSpeed
            );

            // Stop snapping when close enough
            if (Vector3.Distance(currentPallet.transform.position, snapTargetPosition) < 0.01f)
            {
                currentPallet.transform.position = snapTargetPosition;
                isSnapping = false;
            }
        }

        // Continuously validate if pallet is in zone
        if (currentPallet != null && !currentPallet.IsPickedUp())
        {
            ValidatePlacement();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Ignore if already occupied
        if (IsOccupied) return;

        PalletPickup pallet = other.GetComponent<PalletPickup>();
        if (pallet == null) return;

        // Ignore if pallet is currently being carried
        if (pallet.IsPickedUp()) return;

        currentPallet = pallet;
        ValidatePlacement();
    }

    private void OnTriggerExit(Collider other)
    {
        PalletPickup pallet = other.GetComponent<PalletPickup>();
        if (pallet == null || pallet != currentPallet) return;

        // Only clear if pallet is not snapped/placed
        if (!IsOccupied)
        {
            currentPallet = null;
            SetColor(emptyColor);
        }
    }

    /// <summary>
    /// Check if pallet center is properly inside drop zone
    /// </summary>
    private void ValidatePlacement()
    {
        if (currentPallet == null) return;
        if (currentPallet.IsPickedUp())
        {
            if (IsOccupied)
            {
                IsOccupied = false;
                isSnapping = false;
                SetColor(emptyColor);

                // UNFREEZE pallet so it can be carried again
                Rigidbody rb = currentPallet.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                }

                OnPalletRemoved?.Invoke(this);
            }
            return;
        }

        // Use FloorMarker position if assigned, otherwise fall back to transform
        // This fixes pivot height mismatch issues
        Vector3 rawZoneCenter = floorMarker != null ? floorMarker.position : transform.position;

        // Check distance from pallet CENTER to zone CENTER (horizontal only)
        Vector3 palletCenter = currentPallet.transform.position;
        
        // Flatten both to Y:0 so height difference never affects distance
        Vector3 zoneCenter   = new Vector3(rawZoneCenter.x, 0, rawZoneCenter.z);
        Vector3 palletFlat   = new Vector3(palletCenter.x,  0, palletCenter.z);

        float horizontalDistance = Vector3.Distance(palletFlat, zoneCenter);

        Debug.Log($"[DropZone] {dropZoneLabel} | Distance: {horizontalDistance:F2} | Radius: {validationRadius}");

        if (horizontalDistance <= validationRadius)
        {
            // Valid placement!
            if (!IsOccupied)
            {
                IsOccupied = true;
                SetColor(filledColor);
                OnPalletPlaced?.Invoke(this);
                Debug.Log($"[DropZone] {dropZoneLabel}: Valid placement! Distance: {horizontalDistance:F2}m");

                // FREEZE the pallet completely - stops ALL bouncing!
                Rigidbody rb = currentPallet.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.isKinematic = true; // Freeze completely!
                }

                // Snap pallet to perfect center position
                if (snapToCenter)
                {
                    Vector3 snapBase = floorMarker != null ? floorMarker.position : transform.position;
                    snapTargetPosition = new Vector3(
                        snapBase.x,
                        currentPallet.transform.position.y,
                        snapBase.z
                    );
                    isSnapping = true;
                }
            }
        }
        else
        {
            // Pallet in zone but not centered enough - orange warning
            SetColor(nearColor);
            
            if (IsOccupied)
            {
                IsOccupied = false;
                isSnapping = false;

                // Unfreeze so pallet can move/be picked up
                Rigidbody rb = currentPallet.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                }

                OnPalletRemoved?.Invoke(this);
            }
        }
    }

    private void SetColor(Color color)
    {
        if (zoneRenderer != null)
            zoneRenderer.material.color = color;
    }

    private void OnDrawGizmos()
    {
        // Draw at FloorMarker position if assigned, otherwise at transform
        Vector3 center = floorMarker != null ? floorMarker.position : transform.position;
        
        // Draw validation radius circle on floor
        Gizmos.color = IsOccupied ? filledColor : emptyColor;
        Gizmos.DrawWireSphere(new Vector3(center.x, center.y + 0.05f, center.z), validationRadius);
        
        // Draw zone boundary
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
    }
}