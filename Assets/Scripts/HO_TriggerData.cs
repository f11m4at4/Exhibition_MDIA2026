using UnityEngine;

/// <summary>
/// Stores the minimum inspector-driven output data used by exhibition triggers.
/// </summary>
[CreateAssetMenu(fileName = "HO_TriggerData", menuName = "Exhibition/HO Trigger Data", order = 3)]
public sealed class HO_TriggerData : ScriptableObject
{
    /// <summary>
    /// Identifies which exhibition flow step this trigger data is intended to drive.
    /// </summary>
    public enum HO_TriggerType
    {
        Prologue,
        RoomIntro,
        RepresentativeExhibit,
        Epilogue
    }

    [SerializeField] private HO_TriggerType triggerType = HO_TriggerType.RoomIntro;
    [SerializeField] private string roomId;

    [TextArea(2, 4)]
    [SerializeField] private string promptMessage;

    [SerializeField] private string endingTitle = "Exhibition Complete";

    [TextArea(3, 6)]
    [SerializeField] private string endingBody = "Thank you for visiting the prototype exhibition.";

    [SerializeField] private HO_ExhibitData representativeExhibitData;

    public HO_TriggerType TriggerType => triggerType;
    public string RoomId => roomId;
    public string PromptMessage => promptMessage;
    public string EndingTitle => endingTitle;
    public string EndingBody => endingBody;
    public HO_ExhibitData RepresentativeExhibitData => representativeExhibitData;

    /// <summary>
    /// Trims inspector text so runtime UI output stays predictable.
    /// </summary>
    private void OnValidate()
    {
        roomId = roomId != null ? roomId.Trim() : string.Empty;
        promptMessage = promptMessage != null ? promptMessage.Trim() : string.Empty;
        endingTitle = endingTitle != null ? endingTitle.Trim() : string.Empty;
        endingBody = endingBody != null ? endingBody.Trim() : string.Empty;
    }
}
