using UnityEngine;

/// <summary>
/// Simple visual guides to help position forklift at pallet
/// Shows blue guide lines when forklift is nearby
/// No alignment detection - just visual reference!
/// </summary>
public class PalletGuides : MonoBehaviour
{
    [Header("Guide Settings")]
    [Tooltip("Show guides when within this distance")]
    [SerializeField] private float showDistance = 6f;
    
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
    
    [Tooltip("Center guide line position (local space)")]
    [SerializeField] private Vector3 centerGuidePosition = new Vector3(0, 0.1f, 0);
    
    [Tooltip("Guide rotation offset")]
    [SerializeField] private Vector3 guideRotation = Vector3.zero;
    
    [Header("References")]
    [Tooltip("Fork transform - auto-finds if empty")]
    [SerializeField] private Transform forkTransform;
    
    // Visual elements
    private LineRenderer leftGuideLine;
    private LineRenderer rightGuideLine;
    private LineRenderer centerLine;
    private GameObject guideContainer;
    
    // Components
    private PalletPickup palletPickup;
    
    private void Start()
    {
        // Get PalletPickup component
        palletPickup = GetComponent<PalletPickup>();
        
        // Create visual guide lines
        CreateGuideLines();
        
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
        
        // Show guides if within range
        if (distance < showDistance)
        {
            ShowGuides(true);
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
        guideContainer = new GameObject("VisualGuides");
        guideContainer.transform.SetParent(transform);
        guideContainer.transform.localPosition = Vector3.zero;
        guideContainer.transform.localRotation = Quaternion.Euler(guideRotation);
        
        // Create left guide line
        leftGuideLine = CreateLine("LeftGuide", leftGuidePosition);
        
        // Create right guide line
        rightGuideLine = CreateLine("RightGuide", rightGuidePosition);
        
        // Create center guide line
        centerLine = CreateLine("CenterGuide", centerGuidePosition);
        centerLine.startWidth = lineWidth * 0.5f;
        centerLine.endWidth = lineWidth * 0.5f;
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
