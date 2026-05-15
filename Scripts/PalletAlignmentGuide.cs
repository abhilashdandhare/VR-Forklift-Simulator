using UnityEngine;


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
    

    private LineRenderer leftGuideLine;
    private LineRenderer rightGuideLine;
    private LineRenderer centerLine;
    private GameObject guideContainer;
    

    private bool isAligned = false;
    private PalletPickup palletPickup;
    
    private void Start()
    {

        palletPickup = GetComponent<PalletPickup>();
        
 
        CreateGuideLines();
        
    
        if (forkTransform == null)
        {
            GameObject forklift = GameObject.Find("Forklift Truck");
            if (forklift != null)
            {
       
                forkTransform = FindChildRecursive(forklift.transform, "Forklift.Fork");
            }
        }
        

        ShowGuides(false);
    }
    
    private void Update()
    {
        if (forkTransform == null) return;
        

        if (palletPickup != null && palletPickup.IsPickedUp())
        {
            ShowGuides(false);
            return;
        }
        
     
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
    

    private void CreateGuideLines()
    {

        guideContainer = new GameObject("AlignmentGuides");
        guideContainer.transform.SetParent(transform);
        guideContainer.transform.localPosition = Vector3.zero;
        guideContainer.transform.localRotation = Quaternion.identity;
        

        leftGuideLine = CreateLine("LeftGuide", new Vector3(-0.6f, 0.1f, 0));
        

        rightGuideLine = CreateLine("RightGuide", new Vector3(0.6f, 0.1f, 0));
        

        centerLine = CreateLine("CenterGuide", new Vector3(0, 0.1f, 0));
        centerLine.startWidth = 0.05f;
        centerLine.endWidth = 0.05f;
    }
    

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
        

        line.SetPosition(0, Vector3.zero);
        line.SetPosition(1, new Vector3(0, 0, guideLength));
        
        return line;
    }
    

    private void UpdateGuidePositions()
    {

    }
    

    private void UpdateGuideColors()
    {
        if (forkTransform == null) return;
        

        Vector3 localForkPos = transform.InverseTransformPoint(forkTransform.position);
        float lateralOffset = Mathf.Abs(localForkPos.x);
        

        float forwardOffset = localForkPos.z;
        bool isBehindPallet = forwardOffset < -0.5f; // Fork approaching from correct side
        
    
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
        

        leftGuideLine.startColor = guideColor;
        leftGuideLine.endColor = guideColor;
        rightGuideLine.startColor = guideColor;
        rightGuideLine.endColor = guideColor;
        centerLine.startColor = guideColor;
        centerLine.endColor = guideColor;
    }

    private void ShowGuides(bool show)
    {
        if (guideContainer != null)
        {
            guideContainer.SetActive(show);
        }
    }
    

    public bool IsAligned()
    {
        return isAligned;
    }

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
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + transform.forward * 1f, 
            new Vector3(alignmentTolerance * 2f, 0.2f, 2f));
    }
}
