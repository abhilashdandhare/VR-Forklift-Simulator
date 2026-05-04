using UnityEngine;

/// <summary>
/// Shows visual alignment guides to help position forklift correctly under pallet
/// Attach to pallet objects
/// </summary>
public class PalletAlignmentGuide : MonoBehaviour
{
    [Header("Guide Settings")]
    [Tooltip("Show alignment guides when forks are nearby")]
    [SerializeField] private bool showGuidesWhenNear = true;
    
    [Tooltip("Distance threshold to show guides")]
    [SerializeField] private float showDistance = 3f;
    
    [Tooltip("Perfect alignment tolerance (meters)")]
    [SerializeField] private float alignmentTolerance = 0.3f;
    
    [Tooltip("Guide line length")]
    [SerializeField] private float guideLength = 2f;
    
    [Header("Colors")]
    [SerializeField] private Color perfectAlignmentColor = Color.green;
    [SerializeField] private Color closeAlignmentColor = Color.yellow;
    [SerializeField] private Color poorAlignmentColor = Color.red;
    
    [Header("References")]
    [Tooltip("Automatically finds fork, or assign manually")]
    [SerializeField] private Transform forkTransform;
    
    // Visual elements
    private LineRenderer leftGuideLine;
    private LineRenderer rightGuideLine;
    private LineRenderer centerLine;
    private GameObject guideContainer;
    
    // State
    private bool isAligned = false;
    private PalletPickup palletPickup;
    
    private void Start()
    {
        // Get PalletPickup component
        palletPickup = GetComponent<PalletPickup>();
        
        // Create visual guide elements
        CreateGuideLines();
        
        // Try to find fork if not assigned
        if (forkTransform == null)
        {
            GameObject forklift = GameObject.Find("Forklift Truck");
            if (forklift != null)
            {
                // Try to find Forklift.Fork
                forkTransform = FindChildRecursive(forklift.transform, "Forklift.Fork");
            }
        }
        
        // Start with guides hidden
        ShowGuides(false);
    }
    
    private void Update()
    {
        if (forkTransform == null) return;
        
        // Don't show guides if pallet is picked up
        if (palletPickup != null && palletPickup.IsPickedUp())
        {
            ShowGuides(false);
            return;
        }
        
        // Check distance to fork
        float distance = Vector3.Distance(transform.position, forkTransform.position);
        
        if (showGuidesWhenNear && distance < showDistance)
        {
            ShowGuides(true);
            UpdateGuidePositions();
            UpdateGuideColors();
        }
        else
        {
            ShowGuides(false);
        }
    }
    
    /// <summary>
    /// Create line renderers for visual guides
    /// </summary>
    private void CreateGuideLines()
    {
        // Create container
        guideContainer = new GameObject("AlignmentGuides");
        guideContainer.transform.SetParent(transform);
        guideContainer.transform.localPosition = Vector3.zero;
        guideContainer.transform.localRotation = Quaternion.identity;
        
        // Create left guide line
        leftGuideLine = CreateLine("LeftGuide", new Vector3(-0.6f, 0.1f, 0));
        
        // Create right guide line
        rightGuideLine = CreateLine("RightGuide", new Vector3(0.6f, 0.1f, 0));
        
        // Create center alignment line
        centerLine = CreateLine("CenterGuide", new Vector3(0, 0.1f, 0));
        centerLine.startWidth = 0.05f;
        centerLine.endWidth = 0.05f;
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
        line.startWidth = 0.1f;
        line.endWidth = 0.1f;
        line.positionCount = 2;
        line.useWorldSpace = false;
        
        // Set default positions (forward line)
        line.SetPosition(0, Vector3.zero);
        line.SetPosition(1, new Vector3(0, 0, guideLength));
        
        return line;
    }
    
    /// <summary>
    /// Update guide line positions
    /// </summary>
    private void UpdateGuidePositions()
    {
        // Lines already positioned via local space
        // Just make sure they're pointing forward relative to pallet
    }
    
    /// <summary>
    /// Update guide colors based on alignment
    /// </summary>
    private void UpdateGuideColors()
    {
        if (forkTransform == null) return;
        
        // Calculate alignment
        // Project fork position onto pallet's local X-axis
        Vector3 localForkPos = transform.InverseTransformPoint(forkTransform.position);
        float lateralOffset = Mathf.Abs(localForkPos.x);
        
        // Check forward/backward alignment (Z-axis)
        float forwardOffset = localForkPos.z;
        bool isBehindPallet = forwardOffset < -0.5f; // Fork approaching from correct side
        
        // Determine color based on alignment quality
        Color guideColor;
        
        if (lateralOffset < alignmentTolerance && isBehindPallet)
        {
            guideColor = perfectAlignmentColor;
            isAligned = true;
        }
        else if (lateralOffset < alignmentTolerance * 2f)
        {
            guideColor = closeAlignmentColor;
            isAligned = false;
        }
        else
        {
            guideColor = poorAlignmentColor;
            isAligned = false;
        }
        
        // Apply color to all lines
        leftGuideLine.startColor = guideColor;
        leftGuideLine.endColor = guideColor;
        rightGuideLine.startColor = guideColor;
        rightGuideLine.endColor = guideColor;
        centerLine.startColor = guideColor;
        centerLine.endColor = guideColor;
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
    /// Check if currently aligned
    /// </summary>
    public bool IsAligned()
    {
        return isAligned;
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
        // Draw alignment tolerance zone
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + transform.forward * 1f, 
            new Vector3(alignmentTolerance * 2f, 0.2f, 2f));
    }
}
