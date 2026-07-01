using System;
using UnityEngine;

/// <summary>
/// 전시 프로토타입의 전역 상태와 공통 참조를 관리한다.
/// </summary>
public sealed class HO_GameManager : MonoBehaviour
{
    /// <summary>
    /// 다른 프로토타입 시스템이 조회할 최소 전시 상태를 정의한다.
    /// </summary>
    public enum HO_ExhibitionState
    {
        Prologue,
        Exploring,
        Presenting,
        Ending
    }

    [SerializeField] private HO_PlayerController playerController;
    [SerializeField] private HO_UIManager uiManager;
    [SerializeField] private HO_CuratorPresenter curatorPresenter;
    [SerializeField] private HO_ExhibitionFlowController exhibitionFlowController;
    [SerializeField] private HO_ExhibitionState initialState = HO_ExhibitionState.Prologue;
    [SerializeField] private bool hideAllUiOnAwake = true;

    private HO_ExhibitionState currentState;
    private bool isUiOpen;
    private bool hasLoggedMissingReferenceSummary;

    /// <summary>
    /// 플레이어, UI, 큐레이터, 흐름 컨트롤러 순서로 공통 참조를 검증하고 시작 상태를 초기화한다.
    /// </summary>
    private void Awake()
    {
        currentState = initialState;

        ValidateSharedReference(playerController, nameof(playerController));
        ValidateSharedReference(uiManager, nameof(uiManager));
        ValidateSharedReference(curatorPresenter, nameof(curatorPresenter));
        ValidateSharedReference(exhibitionFlowController, nameof(exhibitionFlowController));
        LogMissingReferenceSummary();

        if (hideAllUiOnAwake && uiManager != null)
        {
            uiManager.HideAllPanels();
        }

        SyncStateFromConnectedSystems();
        ApplySharedControlState();
    }

    /// <summary>
    /// Keeps the snapshot state and player lock rule aligned with the currently connected UI and flow systems.
    /// </summary>
    private void Update()
    {
        SyncStateFromConnectedSystems();
        ApplySharedControlState();
    }

    /// <summary>
    /// Stores a new shared exhibition state from one central entry point for future system requests.
    /// </summary>
    public void RequestStateChange(HO_ExhibitionState nextState)
    {
        currentState = nextState;
        ApplySharedControlState();
    }

    /// <summary>
    /// Returns the current prototype exhibition state for lightweight status checks.
    /// </summary>
    public HO_ExhibitionState GetCurrentState()
    {
        return currentState;
    }

    /// <summary>
    /// Returns whether a blocking UI or presentation is currently forcing the player into a locked state.
    /// </summary>
    public bool IsUiOpenOrPlayerLocked()
    {
        return isUiOpen || IsPresentationBlockingInput() || IsEndingStateActive();
    }

    /// <summary>
    /// Re-applies the shared UI and player control rule immediately when another system wants a manual refresh.
    /// </summary>
    public void RefreshSharedControlState()
    {
        SyncStateFromConnectedSystems();
        ApplySharedControlState();
    }

    /// <summary>
    /// Exposes the connected player controller reference for scene wiring checks without duplicating lookups.
    /// </summary>
    public HO_PlayerController GetPlayerController()
    {
        return playerController;
    }

    /// <summary>
    /// Exposes the connected UI manager reference for scene wiring checks without duplicating lookups.
    /// </summary>
    public HO_UIManager GetUiManager()
    {
        return uiManager;
    }

    /// <summary>
    /// Exposes the connected curator presenter reference for scene wiring checks without duplicating lookups.
    /// </summary>
    public HO_CuratorPresenter GetCuratorPresenter()
    {
        return curatorPresenter;
    }

    /// <summary>
    /// Exposes the connected flow controller reference for scene wiring checks without duplicating lookups.
    /// </summary>
    public HO_ExhibitionFlowController GetExhibitionFlowController()
    {
        return exhibitionFlowController;
    }

    /// <summary>
    /// Synchronizes the state snapshot from existing systems so the prototype has one place to query.
    /// </summary>
    private void SyncStateFromConnectedSystems()
    {
        if (exhibitionFlowController != null && exhibitionFlowController.IsExhibitionFinished())
        {
            currentState = HO_ExhibitionState.Ending;
            return;
        }

        if (IsPresentationBlockingInput())
        {
            currentState = HO_ExhibitionState.Presenting;
            return;
        }

        if (exhibitionFlowController != null
            && string.Equals(exhibitionFlowController.GetCurrentStateName(), "WaitingForPrologue", StringComparison.Ordinal))
        {
            currentState = HO_ExhibitionState.Prologue;
            return;
        }

        if (currentState != HO_ExhibitionState.Ending)
        {
            currentState = HO_ExhibitionState.Exploring;
        }
    }

    /// <summary>
    /// Applies the shared rule that blocking UI or ending flow locks the player, and a clear screen unlocks them.
    /// </summary>
    private void ApplySharedControlState()
    {
        isUiOpen = uiManager != null && uiManager.HasOpenBlockingPanel();

        if (playerController == null)
        {
            return;
        }

        bool shouldLockPlayer = isUiOpen || IsPresentationBlockingInput() || IsEndingStateActive();
        playerController.SetControlLocked(shouldLockPlayer);
    }

    /// <summary>
    /// Checks whether the curator presentation flow is the current reason for a blocked player state.
    /// </summary>
    private bool IsPresentationBlockingInput()
    {
        return curatorPresenter != null && curatorPresenter.IsPresentationActive();
    }

    /// <summary>
    /// Checks whether the prototype should keep the player locked because the exhibition is ending.
    /// </summary>
    private bool IsEndingStateActive()
    {
        return currentState == HO_ExhibitionState.Ending
            || (exhibitionFlowController != null && exhibitionFlowController.IsExhibitionFinished());
    }

    /// <summary>
    /// Logs a warning for each missing shared reference so scene setup issues are visible during prototype wiring.
    /// </summary>
    private void ValidateSharedReference(UnityEngine.Object targetReference, string referenceName)
    {
        if (targetReference != null)
        {
            return;
        }

        Debug.LogWarning($"HO_GameManager is missing shared reference: {referenceName}.", this);
    }

    /// <summary>
    /// Emits one summary warning after individual checks so missing scene wiring is easy to spot in logs.
    /// </summary>
    private void LogMissingReferenceSummary()
    {
        if (hasLoggedMissingReferenceSummary)
        {
            return;
        }

        if (playerController != null
            && uiManager != null
            && curatorPresenter != null
            && exhibitionFlowController != null)
        {
            return;
        }

        hasLoggedMissingReferenceSummary = true;
        Debug.LogWarning("HO_GameManager started with one or more missing shared references.", this);
    }
}
