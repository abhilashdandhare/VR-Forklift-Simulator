using UnityEngine;

/// <summary>
/// Handles all forklift audio:
/// - Engine sound pitch changes with speed
/// - Hydraulic sound plays when fork moves
/// - UI click sound for buttons
/// Attach to Forklift Truck GameObject
/// </summary>
public class ForkliftAudio : MonoBehaviour
{
    [Header("Audio Sources")]
    [Tooltip("Audio source for engine sound")]
    [SerializeField] private AudioSource engineSource;
    
    [Tooltip("Audio source for hydraulic/fork sound")]
    [SerializeField] private AudioSource hydraulicSource;

    [Header("Audio Clips")]
    [Tooltip("Engine loop sound")]
    [SerializeField] private AudioClip engineClip;
    
    [Tooltip("Hydraulic/fork movement sound")]
    [SerializeField] private AudioClip hydraulicClip;
    
    [Tooltip("UI button click sound")]
    [SerializeField] private AudioClip uiClickClip;

    [Header("Engine Settings")]
    [Tooltip("Pitch when forklift is idle (not moving)")]
    [SerializeField] private float idlePitch = 0.6f;
    
    [Tooltip("Pitch when forklift is at max speed")]
    [SerializeField] private float maxPitch = 1.8f;
    
    [Tooltip("How fast pitch changes with speed")]
    [SerializeField] private float pitchSmoothSpeed = 3f;
    
    [Tooltip("Volume when idle")]
    [SerializeField] private float idleVolume = 0.3f;
    
    [Tooltip("Volume when driving at full speed")]
    [SerializeField] private float drivingVolume = 0.7f;

    [Header("Hydraulic Settings")]
    [Tooltip("Volume of fork movement sound")]
    [SerializeField] private float hydraulicVolume = 0.6f;
    
    [Tooltip("How fast fork must move to trigger sound (units per second)")]
    [SerializeField] private float forkMoveThreshold = 0.01f;

    [Header("References")]
    [Tooltip("ForkliftController to read speed")]
    [SerializeField] private ForkliftController forkliftController;
    
    [Tooltip("Def-Fork bone to detect movement")]
    [SerializeField] private Transform forkBone;

    // Internal tracking
    private float lastForkHeight;
    private float targetPitch;
    private float currentPitch;

    // Static audio source for UI sounds (shared across scene)
    private static AudioSource uiAudioSource;

    private void Start()
    {
        // Auto-find ForkliftController if not assigned
        if (forkliftController == null)
            forkliftController = GetComponent<ForkliftController>();

        // Auto-find fork bone if not assigned
        if (forkBone == null)
            forkBone = FindDeepChild(transform.root, "Def-Fork");

        // Setup engine source
        if (engineSource != null && engineClip != null)
        {
            engineSource.clip = engineClip;
            engineSource.loop = true;
            engineSource.pitch = idlePitch;
            engineSource.volume = idleVolume;
            engineSource.spatialBlend = 1f;
            engineSource.Play();
        }

        // Setup hydraulic source
        if (hydraulicSource != null && hydraulicClip != null)
        {
            hydraulicSource.clip = hydraulicClip;
            hydraulicSource.loop = true;
            hydraulicSource.volume = hydraulicVolume;
            hydraulicSource.spatialBlend = 1f;
            hydraulicSource.Stop();
        }

        // Store initial fork height
        if (forkBone != null)
            lastForkHeight = forkBone.localPosition.z;

        // Create static UI audio source if needed
        if (uiAudioSource == null)
        {
            GameObject uiAudio = new GameObject("UIAudioSource");
            DontDestroyOnLoad(uiAudio);
            uiAudioSource = uiAudio.AddComponent<AudioSource>();
            uiAudioSource.spatialBlend = 0f; // 2D sound for UI
            uiAudioSource.volume = 0.8f;
        }

        // Assign UI clip to static source
        if (uiClickClip != null)
            uiAudioSource.clip = uiClickClip;

        currentPitch = idlePitch;
        targetPitch = idlePitch;
    }

    private void Update()
    {
        UpdateEngineSound();
        UpdateHydraulicSound();
    }

    /// <summary>
    /// Adjusts engine pitch and volume based on forklift speed
    /// </summary>
    private void UpdateEngineSound()
    {
        if (engineSource == null) return;

        // Get speed from ForkliftController
        float speed = 0f;
        if (forkliftController != null)
            speed = Mathf.Abs(forkliftController.GetCurrentSpeed());

        // Normalize speed to 0-1 range
        float maxSpeed = 2f; // matches ForkliftController max speed
        float speedRatio = Mathf.Clamp01(speed / maxSpeed);

        // Calculate target pitch and volume
        targetPitch = Mathf.Lerp(idlePitch, maxPitch, speedRatio);
        float targetVolume = Mathf.Lerp(idleVolume, drivingVolume, speedRatio);

        // Smooth pitch change
        currentPitch = Mathf.Lerp(currentPitch, targetPitch, Time.deltaTime * pitchSmoothSpeed);
        engineSource.pitch = currentPitch;
        engineSource.volume = targetVolume;
    }

    /// <summary>
    /// Plays hydraulic sound when fork is moving
    /// </summary>
    private void UpdateHydraulicSound()
    {
        if (hydraulicSource == null || forkBone == null) return;

        float currentForkHeight = forkBone.localPosition.z;
        float forkMoveDelta = Mathf.Abs(currentForkHeight - lastForkHeight);
        
        // Check keyboard input directly as backup detection
        bool forkKeyHeld = Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.Q);
        
        // Check actual position movement OR key being held
        bool forkIsMoving = forkMoveDelta > forkMoveThreshold || forkKeyHeld;

        if (forkIsMoving && !hydraulicSource.isPlaying)
        {
            hydraulicSource.Play();
        }
        else if (!forkIsMoving && hydraulicSource.isPlaying)
        {
            hydraulicSource.Stop();
        }

        lastForkHeight = currentForkHeight;
    }

    /// <summary>
    /// Call this from UI buttons OnClick
    /// </summary>
    public static void PlayUIClick()
    {
        if (uiAudioSource != null && uiAudioSource.clip != null)
        {
            uiAudioSource.PlayOneShot(uiAudioSource.clip);
        }
    }

    /// <summary>
    /// Recursively find child transform by name
    /// </summary>
    private Transform FindDeepChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName) return child;
            Transform found = FindDeepChild(child, childName);
            if (found != null) return found;
        }
        return null;
    }
}