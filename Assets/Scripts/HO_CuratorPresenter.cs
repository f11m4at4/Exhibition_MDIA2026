using System.Text;
using UnityEngine;

/// <summary>
/// 큐레이터 해설 단계와 표시 연출을 제어한다.
/// </summary>
public sealed class HO_CuratorPresenter : MonoBehaviour
{
    [SerializeField] private HO_UIManager uiManager;
    [SerializeField] private HO_PlayerController playerController;
    [SerializeField] private Transform curatorVisualRoot;
    [SerializeField] private float presentationDistance = 2f;
    [SerializeField] private string primaryNarrationHint = "Press E to continue.";
    [SerializeField] private string detailedInformationHint = "Press E to close.";
    [SerializeField] private string testCycleHint = "Press Space to continue the exhibit test flow.";

    private PresentationStage currentStage = PresentationStage.None;
    private HO_ExhibitData currentExhibitData;
    private HO_ExhibitData[] testExhibitCache = System.Array.Empty<HO_ExhibitData>();
    private int testExhibitIndex = -1;

    private enum PresentationStage
    {
        None,
        PrimaryNarration,
        DetailedInformation
    }

    /// <summary>
    /// 필수 참조를 확인하고 시작 시 큐레이터 비주얼을 안전하게 숨긴다.
    /// </summary>
    private void Awake()
    {
        SetCuratorVisualActive(false);

        if (!ValidateRequiredReferences())
        {
            enabled = false;
        }
    }

    /// <summary>
    /// 작품 기준 첫 번째 해설 단계를 시작하고 UI와 큐레이터 위치를 갱신한다.
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
    /// 현재 해설의 다음 단계를 진행하거나 마지막 단계라면 종료 처리로 닫는다.
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
    /// 현재 해설을 종료하고 UI, 큐레이터 비주얼, 플레이어 잠금을 정리한다.
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

        SetCuratorVisualActive(false);
        currentExhibitData = null;
        currentStage = PresentationStage.None;
    }

    /// <summary>
    /// 플레이어 입력 흐름이 해설 진행 중인지 외부에서 확인할 수 있게 한다.
    /// </summary>
    public bool IsPresentationActive()
    {
        return currentStage != PresentationStage.None;
    }

    /// <summary>
    /// 프로토타입 테스트용으로 Resources 전시 데이터를 순환하며 첫 해설을 바로 시작한다.
    /// </summary>
    public void RequestTestNextExhibit()
    {
        if (!CanProcessRequest())
        {
            return;
        }

        EnsureTestExhibitCache();

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
    /// 첫 단계 해설 본문과 힌트를 UI에 표시하고 플레이어 제어를 잠근다.
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
    /// 심화 정보 단계 본문과 메타데이터를 UI에 표시하고 종료 힌트로 전환한다.
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
    /// 현재 데이터가 해설 가능한 상태인지 확인하고 새 대상 시작 전에 이전 상태를 정리한다.
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
    /// 큐레이터 비주얼 위치와 회전을 플레이어 기준 2미터 전방 규칙으로 갱신한다.
    /// </summary>
    private void UpdateCuratorVisual()
    {
        if (curatorVisualRoot == null || playerController == null)
        {
            return;
        }

        Vector3 presentationPosition = CalculatePresentationPosition();
        Vector3 lookTarget = playerController.transform.position;
        Vector3 lookDirection = lookTarget - presentationPosition;
        lookDirection.y = 0f;

        curatorVisualRoot.position = presentationPosition;

        if (lookDirection.sqrMagnitude > 0.001f)
        {
            curatorVisualRoot.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        }

        SetCuratorVisualActive(true);
    }

    /// <summary>
    /// 플레이어 전방이 유효하면 앞쪽 2미터, 아니면 측면 보정 위치를 계산한다.
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
    /// 큐레이터 UI 흐름에 쓸 작품 제목 문자열을 데이터 구조 기준으로 조합한다.
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
    /// 심화 정보 단계에서 본문과 작가/연도/매체/키워드를 한 번에 읽을 수 있게 합친다.
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
    /// 기본 힌트와 테스트 순환 힌트를 합쳐 플레이어 입력 안내를 일관되게 만든다.
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
    /// 필수 UI/플레이어 참조가 유효한 상태에서만 공개 요청을 처리하게 한다.
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
    /// 해설 진행에 필요한 필수 참조를 시작 시 한 번 확인한다.
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

        if (curatorVisualRoot == null)
        {
            Debug.LogWarning("HO_CuratorPresenter does not have a curator visual root assigned. UI flow will continue without a visual actor.", this);
        }

        return hasAllReferences;
    }

    /// <summary>
    /// 최소 한 단계 이상 표시할 텍스트가 있을 때만 해설 UI를 열 수 있게 한다.
    /// </summary>
    private static bool HasNarrationContent(HO_ExhibitData exhibitData)
    {
        return !string.IsNullOrWhiteSpace(exhibitData.PrimaryNarration)
            || !string.IsNullOrWhiteSpace(exhibitData.DetailedInformation);
    }

    /// <summary>
    /// 테스트 순환용 전시 데이터 캐시를 한 번만 로드해 Space 디버그 흐름을 단순화한다.
    /// </summary>
    private void EnsureTestExhibitCache()
    {
        if (testExhibitCache.Length > 0)
        {
            return;
        }

        testExhibitCache = Resources.LoadAll<HO_ExhibitData>("Exhibits");
    }

    /// <summary>
    /// 플레이어 조작 잠금을 해설 시작과 단계 전환 시 일관되게 적용한다.
    /// </summary>
    private void LockPlayerControl()
    {
        if (playerController != null)
        {
            playerController.SetControlLocked(true);
        }
    }

    /// <summary>
    /// 큐레이터 루트가 있으면 활성 상태를 바꾸고 없으면 조용히 넘어간다.
    /// </summary>
    private void SetCuratorVisualActive(bool isActive)
    {
        if (curatorVisualRoot != null)
        {
            curatorVisualRoot.gameObject.SetActive(isActive);
        }
    }

    /// <summary>
    /// 심화 정보 본문 끝에 메타데이터 행을 줄바꿈 포함 형식으로 추가한다.
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
    /// 인스펙터에서 거리와 힌트 문자열이 비정상 값이 되지 않도록 최소 범위를 보정한다.
    /// </summary>
    private void OnValidate()
    {
        presentationDistance = Mathf.Max(0.1f, presentationDistance);
        primaryNarrationHint = primaryNarrationHint != null ? primaryNarrationHint.Trim() : string.Empty;
        detailedInformationHint = detailedInformationHint != null ? detailedInformationHint.Trim() : string.Empty;
        testCycleHint = testCycleHint != null ? testCycleHint.Trim() : string.Empty;
    }
}
