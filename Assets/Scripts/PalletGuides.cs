using UnityEngine;

/// <summary>
/// Simple visual guides to help position forklift at pallet
/// Shows blue guide lines when forklift is nearby
/// Distance indicator shows how far away fork is (manual UI setup required)
/// </summary>
public class PalletGuides : MonoBehaviour
{
    [Header("Guide Settings")]
    [Tooltip("Show guides when within this distance")]
    [SerializeField] private float showDistance = 6f;
    
    [Tooltip("Angle cone for showing guides (degrees) - fork must be within this angle facing the pallet")]
    [SerializeField] private float approachAngle = 60f;
    
    [Tooltip("Guide line length")]
    [SerializeField] private float guideLength = 6f;
    
    [Tooltip("Guide line color")]
    [SerializeField] private Color guideColor = new Color(0.5f, 0.8f, 1f, 1f); // Light blue
    
    [Tooltip("Guide line width")]
    [SerializeField] private float lineWidth = 0.1f;
    
    [Header("Guide Positioning")]
    [Tooltip("Left guide line position (local space)")]
    [SerializeField] private Vector3 leftGuidePosition = new Vector3(-0.6f, 0.1f, 0);
    
    [Tooltip("Right guide line position (local space)")]
    [SerializeField] private Vector3 rightGuidePosition = new Vector3(0.6f, 0.1f, 0);
    
    [Tooltip("Guide rotation offset")]
    [SerializeField] private Vector3 guideRotation = Vector3.zero;
    
    [Header("Distance Indicator (Manual Setup)")]
    [Tooltip("Show distance text indicator")]
    [SerializeField] private bool showDistanceIndicator = true;
    
    [Tooltip("Manually created distance UI canvas (child of this pallet)")]
    [SerializeField] private GameObject distanceUICanvas;
    
    [Tooltip("TextMeshPro component for distance display (TMP Text component inside the canvas)")]
    [SerializeField] private TMPro.TextMeshProUGUI distanceTextComponent;
    
    [Tooltip("Font size for distance text")]
    [SerializeField] private float fontSize = 40f;
    
    [Tooltip("Distance from pallet center to front edge (for accurate measurement)")]
    [SerializeField] private float palletFrontOffset = 0.6f;
    
    [Tooltip("Stop distance - turns red (0-0.2m)")]
    [SerializeField] private float stopDistance = 0.2f;
    
    [Tooltip("Caution distance - turns yellow (0.2-1m)")]
    [SerializeField] private float cautionDistance = 1.0f;
    
    [Header("References")]
    [Tooltip("Fork transform - auto-finds if empty")]
    [SerializeField] private Transform forkTransform;
    
    // Visual elements
    private LineRenderer leftGuideLine;
    private LineRenderer rightGuideLine;
    private GameObject guideContainer;
    
    // Components
    private PalletPickup palletPickup;
    
    private void Start()
    {
        // Get PalletPickup component
        palletPickup = GetComponent<PalletPickup>();
        
        // Create visual guide lines
        CreateGuideLines();
        
        // Validate manual UI assignments
        if (showDistanceIndicator)
        {
            if (distanceUICanvas == null)
            {
                Debug.LogWarning($"[PalletGuides] {gameObject.name}: Distance UI Canvas not assigned! Create a World Space Canvas and drag it here.");
            }
            
            if (distanceTextComponent == null)
            {
                Debug.LogWarning($"[PalletGuides] {gameObject.name}: Distance Text Component not assigned! Drag the Text component from your canvas.");
            }
        }
        
        // Try to find fork if not assigned
        if (forkTransform == null)
        {
            GameObject forklift = GameObject.Find("Forklift Truck");
            if (forklift != null)
            {
                forkTransform = FindChildRecursive(forklift.transform, "Forklift.Fork");
            }
        }
        
        // Start with guides hidden
        ShowGuides(false);
        ShowDistanceIndicator(false);
    }
    
    private void Update()
    {
        if (forkTransform == null) return;
        
        // Don't show guides if pallet is picked up
        if (palletPickup != null && palletPickup.IsPickedUp())
        {
            ShowGuides(false);
            ShowDistanceIndicator(false);
            return;
        }
        
        // Check distance to fork - measure from FRONT of pallet, not center
        Vector3 palletFrontPosition = transform.position + (-transform.forward * palletFrontOffset);
        float rawDistance = Vector3.Distance(palletFrontPosition, forkTransform.position);
        
        // Clamp distance - once fork passes the front, show distance to front (not behind)
        Vector3 localForkPos = transform.InverseTransformPoint(forkTransform.position);
        float distanceToFront = Mathf.Abs(localForkPos.z); // Distance along Z axis
        
        // Use the minimum of both measurements to prevent distance increasing after contact
        float distance = Mathf.Min(rawDistance, distanceToFront);
        
        // Check if fork is approaching from the correct side (in front of pallet)
        bool isForkInFront = localForkPos.z < 0; // Fork is in front (negative Z in pallet's local space)
        
        // Check if fork is within the approach cone (not just passing by)
        Vector3 directionToFork = (forkTransform.position - transform.position).normalized;
        Vector3 palletForward = -transform.forward; // Negative because guides point backward
        float angleToFork = Vector3.Angle(palletForward, directionToFork);
        
        bool isInApproachCone = angleToFork < approachAngle;
        
        // Show guides only if: within range, fork is in front, AND fork is directly approaching
        bool shouldShowGuides = distance < showDistance && isForkInFront && isInApproachCone;
        
        ShowGuides(shouldShowGuides);
        
        // FIXED: UI shows only if in cone AND this is the most directly faced pallet
        // Store angle for priority comparison
        bool shouldShowUI = distance < showDistance && isForkInFront && isInApproachCone && showDistanceIndicator;
        
        // Additional check: Only show if we're the closest pallet OR the most directly faced
        if (shouldShowUI)
        {
            // Check if there's another pallet closer or more directly faced
            PalletGuides[] allPallets = FindObjectsOfType<PalletGuides>();
            bool isClosestOrMostDirect = true;
            
            foreach (PalletGuides other in allPallets)
            {
                if (other == this) continue; // Skip self
                
                // Calculate other pallet's stats
                Vector3 otherFrontPos = other.transform.position + (-other.transform.forward * other.palletFrontOffset);
                float otherDistance = Vector3.Distance(otherFrontPos, forkTransform.position);
                Vector3 otherLocalForkPos = other.transform.InverseTransformPoint(forkTransform.position);
                bool otherIsForkInFront = otherLocalForkPos.z < 0;
                
                Vector3 otherDirectionToFork = (forkTransform.position - other.transform.position).normalized;
                Vector3 otherPalletForward = -other.transform.forward;
                float otherAngleToFork = Vector3.Angle(otherPalletForward, otherDirectionToFork);
                bool otherIsInCone = otherAngleToFork < other.approachAngle;
                
                // If other pallet is also valid AND has smaller angle (more direct), don't show this one
                if (otherDistance < other.showDistance && otherIsForkInFront && otherIsInCone)
                {
                    if (otherAngleToFork < angleToFork) // Other pallet more directly faced
                    {
                        isClosestOrMostDirect = false;
                        break;
                    }
                }
            }
            
            shouldShowUI = shouldShowUI && isClosestOrMostDirect;
        }
        
        if (shouldShowUI)
        {
            UpdateDistanceDisplay(distance);
            ShowDistanceIndicator(true);
        }
        else
        {
            ShowDistanceIndicator(false);
        }
    }
    
    /// <summary>
    /// Create line renderers for visual guides
    /// </summary>
    private void CreateGuideLines()
    {
        // Create container
        guideContainer = new GameObject("VisualGuides");
        guideContainer.transform.SetParent(transform);
        guideContainer.transform.localPosition = Vector3.zero;
        guideContainer.transform.localRotation = Quaternion.Euler(guideRotation);
        
        // Create left guide line
        leftGuideLine = CreateLine("LeftGuide", leftGuidePosition);
        
        // Create right guide line
        rightGuideLine = CreateLine("RightGuide", rightGuidePosition);
    }
    
    /// <summary>
    /// Create a single line renderer
    /// </summary>
    private LineRenderer CreateLine(string name, Vector3 localPosition)
    {
        GameObject lineObj = new GameObject(name);
        lineObj.transform.SetParent(guideContainer.transform);
        lineObj.transform.localPosition = localPosition;
        lineObj.transform.localRotation = Quaternion.identity;
        
        LineRenderer line = lineObj.AddComponent<LineRenderer>();
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.positionCount = 2;
        line.useWorldSpace = false;
        
        // Set line color (light blue, always the same)
        line.startColor = guideColor;
        line.endColor = guideColor;
        
        // Set line positions (forward line)
        line.SetPosition(0, Vector3.zero);
        line.SetPosition(1, new Vector3(0, 0, guideLength));
        
        return line;
    }
    
    /// <summary>
    /// Update distance display text and color
    /// </summary>
    private void UpdateDistanceDisplay(float distance)
    {
        if (distanceTextComponent == null) return;
        
        // Set font size
        distanceTextComponent.fontSize = fontSize;
        
        // Update text and color based on distance zones (no numbers!)
        if (distance <= stopDistance)
        {
            // RED ZONE (0 - 0.2m) - STOP!
            distanceTextComponent.text = "STOP";
            distanceTextComponent.color = Color.red;
        }
        else if (distance < cautionDistance)
        {
            // YELLOW ZONE (0.2 - 1.0m) - GO SLOW
            distanceTextComponent.text = "GO SLOW";
            distanceTextComponent.color = Color.yellow;
        }
        else
        {
            // GREEN ZONE (1.0m+) - Good to go
            distanceTextComponent.text = "GO";
            distanceTextComponent.color = Color.green;
        }
    }
    
    /// <summary>
    /// Show or hide distance indicator
    /// </summary>
    private void ShowDistanceIndicator(bool show)
    {
        if (distanceUICanvas != null)
        {
            distanceUICanvas.SetActive(show);
        }
    }
    
    /// <summary>
    /// Show or hide guide lines
    /// </summary>
    private void ShowGuides(bool show)
    {
        if (guideContainer != null)
        {
            guideContainer.SetActive(show);
        }
    }
    
    /// <summary>
    /// Recursively find child by name
    /// </summary>
    private Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;
            
            Transform found = FindChildRecursive(child, childName);
            if (found != null)
                return found;
        }
        return null;
    }
    
    private void OnDrawGizmosSelected()
    {
        // Draw show distance sphere
        Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, showDistance);
    }
}