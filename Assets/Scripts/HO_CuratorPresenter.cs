using System.Text;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 큐레이터 해설 단계와 프레젠테이션 중 이동/시각 표시를 제어한다.
/// </summary>
public sealed class HO_CuratorPresenter : MonoBehaviour
{
    private const float MinimumPresentationDistance = 0.1f;
    private const float DestinationSampleDistance = 2f;

    [SerializeField] private HO_UIManager uiManager;
    [SerializeField] private HO_PlayerController playerController;
    [SerializeField] private Transform curatorVisualRoot;
    [SerializeField] private float presentationDistance = 2f;
    [SerializeField] private float presenterStoppingDistance = 0.15f;
    [SerializeField] private float presenterRotationSpeed = 360f;
    [SerializeField] private string primaryNarrationHint = "Press E to continue.";
    [SerializeField] private string detailedInformationHint = "Press E to close.";
    [SerializeField] private string testCycleHint = "Press Space to continue the exhibit test flow.";

    private PresentationStage currentStage = PresentationStage.None;
    private HO_ExhibitData currentExhibitData;
    private HO_ExhibitData[] testExhibitCache = System.Array.Empty<HO_ExhibitData>();
    private int testExhibitIndex = -1;
    private NavMeshAgent presenterNavMeshAgent;
    private bool hasLoggedNavMeshWarning;

    private enum PresentationStage
    {
        None,
        PrimaryNarration,
        DetailedInformation
    }

    /// <summary>
    /// 필수 참조를 확인하고 런타임에 사용할 NavMeshAgent 참조를 캐시한다.
    /// </summary>
    private void Awake()
    {
        presenterNavMeshAgent = GetComponent<NavMeshAgent>();

        if (!ValidateRequiredReferences())
        {
            enabled = false;
        }
    }

    /// <summary>
    /// 해설 진행 중에는 큐레이터 이동을 갱신하고 아닐 때는 이동을 정리한다.
    /// </summary>
    private void Update()
    {
        if (!enabled)
        {
            return;
        }

        if (IsPresentationActive())
        {
            UpdateCuratorMovement();
            return;
        }

        StopCuratorMovement();
    }

    /// <summary>
    /// 컴포넌트가 꺼질 때 남아 있는 에이전트 경로를 정리한다.
    /// </summary>
    private void OnDisable()
    {
        StopCuratorMovement();
    }

    /// <summary>
    /// 작품 기본 해설 1단계를 시작하고 UI와 큐레이터 표시를 갱신한다.
    /// </summary>
    public void RequestFirstPresentation(HO_ExhibitData exhibitData)
    {
        if (!CanProcessRequest())
        {
            return;
        }

        if (!TryPreparePresentation(exhibitData))
        {
            return;
        }

        ShowPrimaryNarration();
    }

    /// <summary>
    /// 현재 해설의 다음 단계를 진행하거나 마지막 단계면 종료 처리로 넘긴다.
    /// </summary>
    public void RequestNextPresentation()
    {
        if (!CanProcessRequest())
        {
            return;
        }

        if (currentExhibitData == null || currentStage == PresentationStage.None)
        {
            Debug.LogWarning("HO_CuratorPresenter cannot advance because no active presentation exists.", this);
            return;
        }

        if (currentStage == PresentationStage.PrimaryNarration)
        {
            ShowDetailedInformation();
            return;
        }

        ClosePresentation();
    }

    /// <summary>
    /// 현재 해설을 종료하고 UI와 플레이어 잠금을 정리한다.
    /// </summary>
    public void ClosePresentation()
    {
        if (uiManager != null)
        {
            uiManager.HideNarration();
        }

        if (playerController != null)
        {
            playerController.SetControlLocked(false);
        }

        StopCuratorMovement();
        currentExhibitData = null;
        currentStage = PresentationStage.None;
    }

    /// <summary>
    /// 플레이어 입력 흐름에서 해설 진행 중인지 외부에서 확인할 수 있게 한다.
    /// </summary>
    public bool IsPresentationActive()
    {
        return currentStage != PresentationStage.None;
    }

    /// <summary>
    /// 프로토타입 테스트용으로 Resources 전시 데이터를 순환하며 해설을 시작한다.
    /// </summary>
    public void RequestTestNextExhibit()
    {
        if (!CanProcessRequest())
        {
            return;
        }

        LoadTestExhibitCache();

        if (testExhibitCache.Length == 0)
        {
            Debug.LogWarning("HO_CuratorPresenter could not find any HO_ExhibitData assets in Resources/Exhibits.", this);
            return;
        }

        ClosePresentation();
        testExhibitIndex = (testExhibitIndex + 1) % testExhibitCache.Length;
        RequestFirstPresentation(testExhibitCache[testExhibitIndex]);
    }

    /// <summary>
    /// 1차 해설 본문과 힌트를 UI에 표시하고 큐레이터 방향을 갱신한다.
    /// </summary>
    private void ShowPrimaryNarration()
    {
        if (currentExhibitData == null)
        {
            return;
        }

        LockPlayerControl();
        UpdateCuratorVisual();
        uiManager.HidePrompt();
        uiManager.ShowNarration(
            BuildPresentationTitle(currentExhibitData),
            currentExhibitData.PrimaryNarration,
            BuildHintMessage(primaryNarrationHint));
        currentStage = PresentationStage.PrimaryNarration;
    }

    /// <summary>
    /// 상세 정보 단계 본문과 메타데이터를 UI에 표시하며 큐레이터 방향을 유지한다.
    /// </summary>
    private void ShowDetailedInformation()
    {
        if (currentExhibitData == null)
        {
            return;
        }

        LockPlayerControl();
        UpdateCuratorVisual();
        uiManager.ShowNarration(
            BuildPresentationTitle(currentExhibitData),
            BuildDetailedBody(currentExhibitData),
            BuildHintMessage(detailedInformationHint));
        currentStage = PresentationStage.DetailedInformation;
    }

    /// <summary>
    /// 현재 데이터가 해설 가능한 상태인지 확인하고 새 시작 전에 이전 상태를 정리한다.
    /// </summary>
    private bool TryPreparePresentation(HO_ExhibitData exhibitData)
    {
        if (exhibitData == null)
        {
            Debug.LogWarning("HO_CuratorPresenter received a null exhibit data reference.", this);
            ClosePresentation();
            return false;
        }

        exhibitData.LogValidationWarnings(this);

        if (!HasNarrationContent(exhibitData))
        {
            Debug.LogWarning($"HO_CuratorPresenter cannot open presentation because '{exhibitData.name}' does not contain narration text.", this);
            ClosePresentation();
            return false;
        }

        if (currentExhibitData != exhibitData)
        {
            ClosePresentation();
        }

        currentExhibitData = exhibitData;
        return true;
    }

    /// <summary>
    /// 큐레이터의 현재 위치 기준으로 플레이어를 바라보게 맞춘다.
    /// </summary>
    private void UpdateCuratorVisual()
    {
        RotatePresenterTowards(playerController != null ? playerController.transform.position : transform.position);
    }

    /// <summary>
    /// 해설 중에는 플레이어 기준 목표 지점까지 NavMeshAgent 경로를 갱신한다.
    /// </summary>
    private void UpdateCuratorMovement()
    {
        if (playerController == null || presenterNavMeshAgent == null)
        {
            return;
        }

        if (!presenterNavMeshAgent.isOnNavMesh)
        {
            LogNavMeshUnavailable("HO_CuratorPresenter cannot move because the NavMeshAgent is not placed on a NavMesh.");
            RotatePresenterTowards(playerController.transform.position);
            return;
        }

        Vector3 desiredTarget = CalculatePresentationPosition();

        if (!TryResolveNavigationTarget(desiredTarget, out Vector3 navigationTarget))
        {
            RotatePresenterTowards(playerController.transform.position);
            return;
        }

        if (ShouldRefreshDestination(navigationTarget))
        {
            presenterNavMeshAgent.isStopped = false;
            presenterNavMeshAgent.SetDestination(navigationTarget);
        }

        if (!presenterNavMeshAgent.pathPending
            && presenterNavMeshAgent.remainingDistance <= presenterStoppingDistance)
        {
            presenterNavMeshAgent.isStopped = true;
        }

        RotatePresenterTowards(playerController.transform.position);
    }

    /// <summary>
    /// 플레이어 앞 목표가 실패하면 플레이어 위치 자체를 대체 목적지로 다시 샘플링한다.
    /// </summary>
    private bool TryResolveNavigationTarget(Vector3 desiredTarget, out Vector3 navigationTarget)
    {
        if (TrySampleNavigationTarget(desiredTarget, out navigationTarget))
        {
            return true;
        }

        if (playerController != null
            && TrySampleNavigationTarget(playerController.transform.position, out navigationTarget))
        {
            Debug.LogWarning("HO_CuratorPresenter fell back to the player's position because the player-forward presentation target was off the NavMesh.", this);
            return true;
        }

        navigationTarget = desiredTarget;
        LogNavMeshUnavailable("HO_CuratorPresenter could not sample a NavMesh destination near the presentation target or the player position.");
        return false;
    }

    /// <summary>
    /// 주어진 월드 위치 근처에서 실제 경로 계산에 쓸 NavMesh 목적지를 샘플링한다.
    /// </summary>
    private bool TrySampleNavigationTarget(Vector3 worldPosition, out Vector3 navigationTarget)
    {
        if (NavMesh.SamplePosition(worldPosition, out NavMeshHit hit, DestinationSampleDistance, NavMesh.AllAreas))
        {
            hasLoggedNavMeshWarning = false;
            navigationTarget = hit.position;
            return true;
        }

        navigationTarget = worldPosition;
        return false;
    }

    /// <summary>
    /// 목적지가 의미 있게 바뀐 경우에만 Agent 경로를 다시 계산한다.
    /// </summary>
    private bool ShouldRefreshDestination(Vector3 navigationTarget)
    {
        if (!presenterNavMeshAgent.hasPath)
        {
            return true;
        }

        return (presenterNavMeshAgent.destination - navigationTarget).sqrMagnitude > 0.04f;
    }

    /// <summary>
    /// 해설이 닫히거나 이동이 끝났을 때 Agent 경로를 정리한다.
    /// </summary>
    private void StopCuratorMovement()
    {
        if (presenterNavMeshAgent == null || !presenterNavMeshAgent.isOnNavMesh)
        {
            return;
        }

        presenterNavMeshAgent.isStopped = true;
        presenterNavMeshAgent.ResetPath();
    }

    /// <summary>
    /// 플레이어 전방이 유효하면 정면 2미터, 아니면 측면 보정 방향으로 목표 지점을 계산한다.
    /// </summary>
    private Vector3 CalculatePresentationPosition()
    {
        Transform playerTransform = playerController.transform;
        Vector3 flatForward = playerTransform.forward;
        flatForward.y = 0f;

        if (flatForward.sqrMagnitude < 0.001f)
        {
            flatForward = playerTransform.right;
            flatForward.y = 0f;
        }

        flatForward.Normalize();
        return playerTransform.position + (flatForward * presentationDistance);
    }

    /// <summary>
    /// Presenter 루트가 현재 위치에서 플레이어 쪽을 향하도록 수평 회전만 갱신한다.
    /// </summary>
    private void RotatePresenterTowards(Vector3 lookTarget)
    {
        Vector3 lookDirection = lookTarget - transform.position;
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, presenterRotationSpeed * Time.deltaTime);
    }

    /// <summary>
    /// 같은 NavMesh 경고가 매 프레임 반복되지 않도록 한 번만 출력한다.
    /// </summary>
    private void LogNavMeshUnavailable(string message)
    {
        if (hasLoggedNavMeshWarning)
        {
            return;
        }

        hasLoggedNavMeshWarning = true;
        Debug.LogWarning(message, this);
    }

    /// <summary>
    /// 큐레이터 UI 제목 문자열을 데이터 구조 기준으로 조합한다.
    /// </summary>
    private static string BuildPresentationTitle(HO_ExhibitData exhibitData)
    {
        if (!string.IsNullOrWhiteSpace(exhibitData.ExhibitName))
        {
            return exhibitData.ExhibitName;
        }

        return exhibitData.name;
    }

    /// <summary>
    /// 상세 정보 단계에서 본문과 작가/연도/매체/키워드를 한 번에 읽히게 합친다.
    /// </summary>
    private static string BuildDetailedBody(HO_ExhibitData exhibitData)
    {
        StringBuilder builder = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(exhibitData.DetailedInformation))
        {
            builder.Append(exhibitData.DetailedInformation.Trim());
        }

        AppendMetadataLine(builder, "Artist", exhibitData.ArtistName);
        AppendMetadataLine(builder, "Year", exhibitData.ProductionYear);
        AppendMetadataLine(builder, "Medium", exhibitData.Medium);
        AppendMetadataLine(builder, "Room", exhibitData.RoomId);

        if (exhibitData.RepresentativeKeywords != null && exhibitData.RepresentativeKeywords.Count > 0)
        {
            string keywordLine = string.Join(", ", exhibitData.RepresentativeKeywords);
            AppendMetadataLine(builder, "Keywords", keywordLine);
        }

        return builder.ToString().Trim();
    }

    /// <summary>
    /// 기본 힌트와 테스트 순환 힌트를 합쳐 플레이어 입력 안내를 만든다.
    /// </summary>
    private string BuildHintMessage(string primaryHint)
    {
        if (string.IsNullOrWhiteSpace(testCycleHint))
        {
            return primaryHint ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(primaryHint))
        {
            return testCycleHint;
        }

        return $"{primaryHint} {testCycleHint}";
    }

    /// <summary>
    /// 필수 UI와 플레이어 참조가 유효할 때만 해설 요청을 처리한다.
    /// </summary>
    private bool CanProcessRequest()
    {
        if (!enabled)
        {
            return false;
        }

        if (uiManager == null || playerController == null)
        {
            Debug.LogWarning("HO_CuratorPresenter is missing required references and cannot process presentation requests.", this);
            return false;
        }

        return true;
    }

    /// <summary>
    /// 해설 진행에 필요한 필수 참조를 시작 전에 확인한다.
    /// </summary>
    private bool ValidateRequiredReferences()
    {
        bool hasAllReferences = true;

        if (uiManager == null)
        {
            Debug.LogError("HO_CuratorPresenter requires a HO_UIManager reference.", this);
            hasAllReferences = false;
        }

        if (playerController == null)
        {
            Debug.LogError("HO_CuratorPresenter requires a HO_PlayerController reference.", this);
            hasAllReferences = false;
        }

        if (presenterNavMeshAgent == null)
        {
            Debug.LogError("HO_CuratorPresenter requires an existing NavMeshAgent on the same GameObject.", this);
            hasAllReferences = false;
        }

        if (curatorVisualRoot == null)
        {
            Debug.LogWarning("HO_CuratorPresenter requires a curator visual root reference for visual alignment checks.", this);
        }

        return hasAllReferences;
    }

    /// <summary>
    /// 최소 한 단계 이상 표시할 텍스트가 있을 때만 해설 UI를 연다.
    /// </summary>
    private static bool HasNarrationContent(HO_ExhibitData exhibitData)
    {
        return !string.IsNullOrWhiteSpace(exhibitData.PrimaryNarration)
            || !string.IsNullOrWhiteSpace(exhibitData.DetailedInformation);
    }

    /// <summary>
    /// 테스트 순환용 전시 데이터 캐시를 한 번만 로드한다.
    /// </summary>
    private void LoadTestExhibitCache()
    {
        if (testExhibitCache.Length > 0)
        {
            return;
        }

        testExhibitCache = Resources.LoadAll<HO_ExhibitData>("Exhibits");
    }

    /// <summary>
    /// 플레이어 조작 잠금을 해설 시작과 단계 전환 때 동일하게 적용한다.
    /// </summary>
    private void LockPlayerControl()
    {
        if (playerController != null)
        {
            playerController.SetControlLocked(true);
        }
    }

    /// <summary>
    /// 상세 정보 본문 뒤에 메타데이터 줄을 빈 줄 간격으로 추가한다.
    /// </summary>
    private static void AppendMetadataLine(StringBuilder builder, string label, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (builder.Length > 0)
        {
            builder.AppendLine();
            builder.AppendLine();
        }

        builder.Append(label);
        builder.Append(": ");
        builder.Append(value.Trim());
    }

    /// <summary>
    /// 인스펙터 값이 비정상 범위로 내려가지 않도록 프로토타입 기준 최소값을 유지한다.
    /// </summary>
    private void OnValidate()
    {
        presentationDistance = Mathf.Max(MinimumPresentationDistance, presentationDistance);
        presenterStoppingDistance = Mathf.Max(0.01f, presenterStoppingDistance);
        presenterRotationSpeed = Mathf.Max(1f, presenterRotationSpeed);
        primaryNarrationHint = primaryNarrationHint != null ? primaryNarrationHint.Trim() : string.Empty;
        detailedInformationHint = detailedInformationHint != null ? detailedInformationHint.Trim() : string.Empty;
        testCycleHint = testCycleHint != null ? testCycleHint.Trim() : string.Empty;
    }
}
