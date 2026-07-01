using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks exhibition flow state and resolves trigger-driven UI or curator output from trigger data assets.
/// </summary>
public sealed class HO_ExhibitionFlowController : MonoBehaviour
{
    private const string DefaultPrologueMessage = "Exhibition flow started.";
    private const string DefaultRoomIntroFormat = "{0} intro entered.";
    private const string DefaultEndingTitle = "Exhibition Complete";
    private const string DefaultEndingBody = "Thank you for visiting the prototype exhibition.";

    [SerializeField] private HO_UIManager uiManager;
    [SerializeField] private HO_CuratorPresenter curatorPresenter;
    [SerializeField] private HO_RoomFlowBinding[] roomSequence = Array.Empty<HO_RoomFlowBinding>();

    private readonly HashSet<int> processedTriggerDataIds = new HashSet<int>();
    private readonly HashSet<string> introducedRoomIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> completedRepresentativeRoomIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private HO_ExhibitionFlowState currentState = HO_ExhibitionFlowState.WaitingForPrologue;

    [Serializable]
    private sealed class HO_RoomFlowBinding
    {
        [SerializeField] private string roomId;

        public string RoomId => roomId;

        /// <summary>
        /// Normalizes the configured room id so flow comparisons remain consistent.
        /// </summary>
        public void Normalize()
        {
            roomId = roomId != null ? roomId.Trim() : string.Empty;
        }
    }

    private enum HO_ExhibitionFlowState
    {
        WaitingForPrologue,
        ExploringRooms,
        AwaitingEpilogue,
        Finished
    }

    /// <summary>
    /// Normalizes serialized values before runtime comparisons begin.
    /// </summary>
    private void Awake()
    {
        NormalizeSerializedValues();
    }

    /// <summary>
    /// Returns the current exhibition state name for debugging and external checks.
    /// </summary>
    public string GetCurrentStateName()
    {
        return currentState.ToString();
    }

    /// <summary>
    /// Processes one trigger data asset and prevents the same trigger asset from completing twice.
    /// </summary>
    public bool TryHandleTriggerEntered(HO_TriggerData triggerData)
    {
        if (triggerData == null)
        {
            Debug.LogWarning("HO_ExhibitionFlowController received a null trigger data reference.", this);
            return false;
        }

        int triggerDataId = triggerData.GetInstanceID();

        if (processedTriggerDataIds.Contains(triggerDataId))
        {
            return false;
        }

        bool wasProcessed = false;

        print("triggerData.TriggerType : " + triggerData.TriggerType);
        if (triggerData.TriggerType == HO_TriggerData.HO_TriggerType.Prologue)
        {
            wasProcessed = ProcessPrologueTrigger(triggerData);
        }
        else if (triggerData.TriggerType == HO_TriggerData.HO_TriggerType.RoomIntro)
        {
            wasProcessed = ProcessRoomIntroTrigger(triggerData);
        }
        else if (triggerData.TriggerType == HO_TriggerData.HO_TriggerType.RepresentativeExhibit)
        {
            wasProcessed = ProcessRepresentativeTrigger(triggerData);
        }
        else if (triggerData.TriggerType == HO_TriggerData.HO_TriggerType.Epilogue)
        {
            wasProcessed = ProcessEpilogueTrigger(triggerData);
        }

        if (wasProcessed)
        {
            processedTriggerDataIds.Add(triggerDataId);
        }

        return wasProcessed;
    }

    /// <summary>
    /// Allows other systems to complete a representative room directly by room id.
    /// </summary>
    public bool TryCompleteRepresentativeRoom(string roomId)
    {
        string normalizedRoomId = NormalizeId(roomId);

        if (string.IsNullOrWhiteSpace(normalizedRoomId))
        {
            Debug.LogWarning("HO_ExhibitionFlowController received an empty room id for completion.", this);
            return false;
        }

        // if (!CanEnterNextRoom(normalizedRoomId))
        // {
        //     return false;
        // }

        return CompleteRepresentativeRoom(normalizedRoomId, null);
    }

    /// <summary>
    /// Starts one representative exhibit interaction from a scene component using the exhibit asset's room binding.
    /// </summary>
    public bool TryStartRepresentativeExhibit(HO_ExhibitData exhibitData)
    {
        if (exhibitData == null)
        {
            Debug.LogWarning("HO_ExhibitionFlowController received a null exhibit data reference.", this);
            return false;
        }

        if (curatorPresenter != null && curatorPresenter.IsPresentationActive())
        {
            return false;
        }

        string normalizedRoomId = NormalizeId(exhibitData.RoomId);

        if (string.IsNullOrWhiteSpace(normalizedRoomId))
        {
            Debug.LogWarning($"HO_ExhibitionFlowController cannot start exhibit '{exhibitData.name}' because its room id is empty.", this);
            return false;
        }

        if (!introducedRoomIds.Contains(normalizedRoomId))
        {
            introducedRoomIds.Add(normalizedRoomId);
        }

        return CompleteRepresentativeRoom(normalizedRoomId, exhibitData);
    }

    /// <summary>
    /// Checks whether the requested room is currently unlocked in the prototype flow order.
    /// </summary>
    public bool CanEnterNextRoom(string roomId)
    {
        string normalizedRoomId = NormalizeId(roomId);
        int roomIndex = GetRoomIndex(normalizedRoomId);

        if (roomIndex < 0)
        {
            return false;
        }

        if (currentState == HO_ExhibitionFlowState.WaitingForPrologue)
        {
            return false;
        }

        if (roomIndex == 0)
        {
            return true;
        }

        for (int index = 0; index < roomIndex; index++)
        {
            string previousRoomId = NormalizeId(roomSequence[index].RoomId);

            if (!completedRepresentativeRoomIds.Contains(previousRoomId))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Returns whether the exhibition has already reached the finished state.
    /// </summary>
    public bool IsExhibitionFinished()
    {
        return currentState == HO_ExhibitionFlowState.Finished;
    }

    /// <summary>
    /// Opens the exhibition flow when the prologue trigger is entered for the first time.
    /// </summary>
    private bool ProcessPrologueTrigger(HO_TriggerData triggerData)
    {
        // if (currentState != HO_ExhibitionFlowState.WaitingForPrologue)
        // {
        //     return false;
        // }

        // currentState = roomSequence.Length > 0
        //     ? HO_ExhibitionFlowState.ExploringRooms
        //     : HO_ExhibitionFlowState.AwaitingEpilogue;

        ShowPromptOrLog(
            ResolvePromptMessage(triggerData.PromptMessage, DefaultPrologueMessage),
            $"HO_ExhibitionFlowController prologue entered via '{triggerData.name}'.");
        return true;
    }

    /// <summary>
    /// Shows a room intro message once when the configured room becomes available.
    /// </summary>
    private bool ProcessRoomIntroTrigger(HO_TriggerData triggerData)
    {
        string normalizedRoomId = NormalizeId(triggerData != null ? triggerData.RoomId : string.Empty);

        // if (string.IsNullOrWhiteSpace(normalizedRoomId) || !CanEnterNextRoom(normalizedRoomId))
        // {
        //     return false;
        // }

        // if (!introducedRoomIds.Add(normalizedRoomId))
        // {
        //     return false;
        // }

        print("triggerData : " + triggerData.name + " | triggerData.PromptMessage : " + triggerData.PromptMessage + " | normalizedRoomId : " + normalizedRoomId);

        string message = ResolvePromptMessage(
            triggerData.PromptMessage,
            string.Format(DefaultRoomIntroFormat, normalizedRoomId));

        ShowPromptOrLog(message, $"HO_ExhibitionFlowController entered room intro '{normalizedRoomId}'.");
        return true;
    }

    /// <summary>
    /// Marks a representative room as complete and optionally starts curator narration for its exhibit data.
    /// </summary>
    private bool ProcessRepresentativeTrigger(HO_TriggerData triggerData)
    {
        string normalizedRoomId = NormalizeId(triggerData != null ? triggerData.RoomId : string.Empty);

        // if (string.IsNullOrWhiteSpace(normalizedRoomId) || !CanEnterNextRoom(normalizedRoomId))
        // {
        //     return false;
        // }

        if (!introducedRoomIds.Contains(normalizedRoomId))
        {
            introducedRoomIds.Add(normalizedRoomId);
        }

        return CompleteRepresentativeRoom(normalizedRoomId, triggerData.RepresentativeExhibitData);
    }

    /// <summary>
    /// Shows the ending UI once after all representative rooms have been completed.
    /// </summary>
    private bool ProcessEpilogueTrigger(HO_TriggerData triggerData)
    {
        // if (currentState != HO_ExhibitionFlowState.AwaitingEpilogue)
        // {
        //     return false;
        // }

        currentState = HO_ExhibitionFlowState.Finished;

        string resolvedEndingTitle = ResolvePromptMessage(
            triggerData != null ? triggerData.EndingTitle : string.Empty,
            DefaultEndingTitle);
        string resolvedEndingBody = ResolvePromptMessage(
            triggerData != null ? triggerData.EndingBody : string.Empty,
            DefaultEndingBody);

        if (uiManager != null)
        {
            uiManager.HidePrompt();
            uiManager.HideNarration();
            uiManager.ShowEndingMessage(resolvedEndingTitle, resolvedEndingBody);
        }
        else
        {
            Debug.Log($"HO_ExhibitionFlowController ending: {resolvedEndingTitle} - {resolvedEndingBody}", this);
        }

        return true;
    }

    /// <summary>
    /// Records one representative room completion and updates the next exhibition state.
    /// </summary>
    private bool CompleteRepresentativeRoom(string roomId, HO_ExhibitData exhibitData)
    {
        string normalizedRoomId = NormalizeId(roomId);

        if (!completedRepresentativeRoomIds.Add(normalizedRoomId))
        {
            return false;
        }

        if (curatorPresenter != null && exhibitData != null)
        {
            curatorPresenter.RequestFirstPresentation(exhibitData);
        }

        currentState = AreAllRepresentativeRoomsCompleted()
            ? HO_ExhibitionFlowState.AwaitingEpilogue
            : HO_ExhibitionFlowState.ExploringRooms;

        return true;
    }

    /// <summary>
    /// Returns the configured room order index for gate checks.
    /// </summary>
    private int GetRoomIndex(string roomId)
    {
        string normalizedRoomId = NormalizeId(roomId);

        for (int index = 0; index < roomSequence.Length; index++)
        {
            if (string.Equals(NormalizeId(roomSequence[index].RoomId), normalizedRoomId, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    /// <summary>
    /// Checks whether every configured room has already completed its representative trigger.
    /// </summary>
    private bool AreAllRepresentativeRoomsCompleted()
    {
        if (roomSequence.Length == 0)
        {
            return true;
        }

        for (int index = 0; index < roomSequence.Length; index++)
        {
            string normalizedRoomId = NormalizeId(roomSequence[index].RoomId);

            if (string.IsNullOrWhiteSpace(normalizedRoomId))
            {
                continue;
            }

            if (!completedRepresentativeRoomIds.Contains(normalizedRoomId))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Shows a prompt when UI exists, otherwise keeps a log breadcrumb for flow debugging.
    /// </summary>
    private void ShowPromptOrLog(string promptMessage, string fallbackLogMessage)
    {
        if (uiManager != null)
        {
            uiManager.ShowPrompt(promptMessage);
            return;
        }

        Debug.Log(fallbackLogMessage, this);
    }

    /// <summary>
    /// Falls back to the provided default when a trigger data text field is empty.
    /// </summary>
    private static string ResolvePromptMessage(string configuredMessage, string fallbackMessage)
    {
        return string.IsNullOrWhiteSpace(configuredMessage)
            ? fallbackMessage
            : configuredMessage.Trim();
    }

    /// <summary>
    /// Normalizes inspector values so runtime room comparisons stay stable.
    /// </summary>
    private void NormalizeSerializedValues()
    {
        roomSequence = roomSequence ?? Array.Empty<HO_RoomFlowBinding>();

        for (int index = 0; index < roomSequence.Length; index++)
        {
            roomSequence[index]?.Normalize();
        }
    }

    /// <summary>
    /// Trims identifiers before comparing them across room bindings.
    /// </summary>
    private static string NormalizeId(string value)
    {
        return value != null ? value.Trim() : string.Empty;
    }

    /// <summary>
    /// Keeps serialized room values normalized while editing in the inspector.
    /// </summary>
    private void OnValidate()
    {
        NormalizeSerializedValues();
    }
}
