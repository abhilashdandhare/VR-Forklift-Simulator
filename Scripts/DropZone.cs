using UnityEngine;

/// <summary>
/// Place this on a trigger volume where the trainee must deliver a pallet.
/// </summary>
public class DropZone : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string dropZoneLabel = "Drop Zone A";
    [SerializeField] private Color emptyColor = new Color(1f, 1f, 0f, 0.3f);
    [SerializeField] private Color filledColor = new Color(0f, 1f, 0f, 0.4f);

    [Header("References")]
    [SerializeField] private Renderer zoneRenderer;
    [SerializeField] private TMPro.TextMeshPro labelText;

    public bool IsOccupied { get; private set; }
    public string Label => dropZoneLabel;

    public System.Action<DropZone> OnPalletPlaced;
    public System.Action<DropZone> OnPalletRemoved;

    private int palletsInZone = 0;

    private void Start()
    {
        SetColor(emptyColor);
        if (labelText != null)
            labelText.text = dropZoneLabel;
    }

    private void OnTriggerEnter(Collider other)
    {
        PalletPickup pallet = other.GetComponent<PalletPickup>();
        if (pallet == null) return;
        if (pallet.IsPickedUp()) return;

        palletsInZone++;
        IsOccupied = true;
        SetColor(filledColor);
        OnPalletPlaced?.Invoke(this);
        Debug.Log($"[DropZone] {dropZoneLabel}: pallet delivered!");
    }

    private void OnTriggerExit(Collider other)
    {
        PalletPickup pallet = other.GetComponent<PalletPickup>();
        if (pallet == null) return;

        palletsInZone = Mathf.Max(0, palletsInZone - 1);
        if (palletsInZone == 0)
        {
            IsOccupied = false;
            SetColor(emptyColor);
            OnPalletRemoved?.Invoke(this);
        }
    }

    private void SetColor(Color color)
    {
        if (zoneRenderer != null)
            zoneRenderer.material.color = color;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = IsOccupied ? filledColor : emptyColor;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
    }
}
