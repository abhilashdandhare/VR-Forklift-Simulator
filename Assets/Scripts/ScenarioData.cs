using UnityEngine;

[CreateAssetMenu(fileName = "NewScenario", menuName = "Forklift Training/Scenario")]
public class ScenarioData : ScriptableObject
{
    [Header("Scenario Info")]
    public string scenarioName = "New Scenario";
    [TextArea(2, 4)]
    public string description = "Pick up the pallet and place it in the drop zone.";
    public Sprite thumbnail;

    [Header("Difficulty")]
    public DifficultyLevel difficulty = DifficultyLevel.Beginner;
    public float timeLimitSeconds = 120f;
    public bool hasTimeLimit = true;

    [Header("Tasks")]
    public ScenarioTask[] tasks;

    [Header("Scene")]
    [Tooltip("Name of the GameObject group to activate for this scenario")]
    public string sceneGroupName;
}

[System.Serializable]
public class ScenarioTask
{
    public string taskDescription;
    public TaskType type;
    public string targetObjectName;
}

public enum TaskType
{
    PickupPallet,
    DeliverToDropZone,
    StackPallet,
    NavigateThroughAisle,
}

public enum DifficultyLevel
{
    Beginner,
    Intermediate,
    Advanced,
}
