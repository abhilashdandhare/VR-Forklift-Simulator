using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Central controller for the training scenario system.
/// Attach to an empty GameObject in your scene.
/// Assign your ScenarioData assets and scene groups in the Inspector.
/// </summary>
public class ScenarioManager : MonoBehaviour
{
    public static ScenarioManager Instance { get; private set; }

    [Header("Scenarios")]
    [Tooltip("All available training scenarios - drag ScenarioData assets here")]
    [SerializeField] private ScenarioData[] scenarios;

    [Header("Scene Groups")]
    [Tooltip("Parent GameObjects that group scenario-specific objects (pallets, drop zones, etc.)")]
    [SerializeField] private GameObject[] sceneGroups;

    [Header("UI References")]
    [SerializeField] private GameObject scenarioMenuUI;
    [SerializeField] private GameObject hudUI;
    [SerializeField] private GameObject resultsUI;
    [SerializeField] private TextMeshProUGUI hudTimerText;
    [SerializeField] private TextMeshProUGUI hudTaskText;
    [SerializeField] private TextMeshProUGUI resultsHeaderText;
    [SerializeField] private TextMeshProUGUI resultsSummaryText;

    [Header("Forklift")]
    [SerializeField] private Transform forkliftSpawnPoint;
    [SerializeField] private Transform forkliftTransform;

    // Runtime state
    private ScenarioData currentScenario;
    private int currentScenarioIndex = -1;
    private float elapsedTime;
    private bool isRunning;
    private int completedTasks;
    private List<DropZone> activeDropZones = new List<DropZone>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        ShowMenu();
    }

    private void Update()
    {
        if (!isRunning) return;

        elapsedTime += Time.deltaTime;

        // Update HUD timer
        if (hudTimerText != null)
        {
            if (currentScenario.hasTimeLimit)
            {
                float remaining = currentScenario.timeLimitSeconds - elapsedTime;
                if (remaining <= 0f)
                {
                    remaining = 0f;
                    FailScenario("Time ran out!");
                }
                hudTimerText.text = FormatTime(remaining);
                hudTimerText.color = remaining < 20f ? Color.red : Color.white;
            }
            else
            {
                hudTimerText.text = FormatTime(elapsedTime);
            }
        }
    }

    // Called from scenario menu UI buttons
    public void StartScenario(int index)
    {
        if (index < 0 || index >= scenarios.Length) return;

        currentScenarioIndex = index;
        currentScenario = scenarios[index];

        // Hide all scene groups, activate the correct one
        foreach (GameObject group in sceneGroups)
            group.SetActive(false);

        foreach (GameObject group in sceneGroups)
        {
            if (group.name == currentScenario.sceneGroupName)
            {
                group.SetActive(true);
                break;
            }
        }

        // Reset forklift position
        if (forkliftTransform != null && forkliftSpawnPoint != null)
        {
            forkliftTransform.position = forkliftSpawnPoint.position;
            forkliftTransform.rotation = forkliftSpawnPoint.rotation;
        }

        // Find drop zones in active group
        RegisterDropZones();

        // Reset state
        elapsedTime = 0f;
        completedTasks = 0;
        isRunning = true;

        // Switch UI
        if (scenarioMenuUI != null) scenarioMenuUI.SetActive(false);
        if (resultsUI != null) resultsUI.SetActive(false);
        if (hudUI != null) hudUI.SetActive(true);

        UpdateHUDTask();

        Debug.Log($"[ScenarioManager] Started: {currentScenario.scenarioName}");
    }

    private void RegisterDropZones()
    {
        // Unregister old zones
        foreach (DropZone dz in activeDropZones)
        {
            dz.OnPalletPlaced -= OnPalletDelivered;
        }
        activeDropZones.Clear();

        // Find new zones in the active group
        foreach (GameObject group in sceneGroups)
        {
            if (!group.activeSelf) continue;

            DropZone[] zones = group.GetComponentsInChildren<DropZone>(true);
            foreach (DropZone zone in zones)
            {
                zone.OnPalletPlaced += OnPalletDelivered;
                activeDropZones.Add(zone);
            }
        }

        Debug.Log($"[ScenarioManager] Found {activeDropZones.Count} drop zones");
    }

    private void OnPalletDelivered(DropZone zone)
    {
        completedTasks++;
        Debug.Log($"[ScenarioManager] Task complete! {completedTasks}/{activeDropZones.Count}");

        UpdateHUDTask();

        // Check if all drop zones are filled
        bool allFilled = true;
        foreach (DropZone dz in activeDropZones)
        {
            if (!dz.IsOccupied) { allFilled = false; break; }
        }

        if (allFilled)
        {
            CompleteScenario();
        }
    }

    private void CompleteScenario()
    {
        isRunning = false;

        string grade = GetGrade();
        string timeStr = FormatTime(elapsedTime);

        Debug.Log($"[ScenarioManager] Scenario complete! Time: {timeStr}, Grade: {grade}");

        ShowResults(
            header: "Scenario complete!",
            summary: $"{currentScenario.scenarioName}\n\nTime: {timeStr}\nGrade: {grade}\nTasks: {completedTasks}/{activeDropZones.Count}"
        );
    }

    private void FailScenario(string reason)
    {
        isRunning = false;

        ShowResults(
            header: "Scenario failed",
            summary: $"{currentScenario.scenarioName}\n\nReason: {reason}\nTime: {FormatTime(elapsedTime)}\nTasks: {completedTasks}/{activeDropZones.Count}"
        );
    }

    private void ShowResults(string header, string summary)
    {
        if (hudUI != null) hudUI.SetActive(false);

        if (resultsHeaderText != null) resultsHeaderText.text = header;
        if (resultsSummaryText != null) resultsSummaryText.text = summary;
        if (resultsUI != null) resultsUI.SetActive(true);
    }

    // Called from results UI "Retry" button
    public void RetryScenario()
    {
        StartScenario(currentScenarioIndex);
    }

    // Called from results UI "Menu" button
    public void ShowMenu()
    {
        isRunning = false;

        foreach (GameObject group in sceneGroups)
            group.SetActive(false);

        if (hudUI != null) hudUI.SetActive(false);
        if (resultsUI != null) resultsUI.SetActive(false);
        if (scenarioMenuUI != null) scenarioMenuUI.SetActive(true);
    }

    private void UpdateHUDTask()
    {
        if (hudTaskText == null || currentScenario == null) return;

        int total = activeDropZones.Count;
        int remaining = total - completedTasks;
        hudTaskText.text = $"Deliver pallets: {completedTasks}/{total}";
    }

    private string GetGrade()
    {
        if (!currentScenario.hasTimeLimit) return "Complete";

        float ratio = elapsedTime / currentScenario.timeLimitSeconds;
        if (ratio < 0.5f) return "S";
        if (ratio < 0.7f) return "A";
        if (ratio < 0.85f) return "B";
        return "C";
    }

    private string FormatTime(float seconds)
    {
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        return $"{m:00}:{s:00}";
    }

    // Public accessor for external scripts
    public ScenarioData CurrentScenario => currentScenario;
    public bool IsRunning => isRunning;
    public float ElapsedTime => elapsedTime;
}
