using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전시물 감상에 필요한 메타데이터와 해설 문구를 보관한다.
/// </summary>
[CreateAssetMenu(fileName = "HO_ExhibitData", menuName = "Exhibition/HO Exhibit Data", order = 2)]
public sealed class HO_ExhibitData : ScriptableObject
{
    // 기본 식별 정보와 방 구분값을 보관한다.
    [SerializeField] private string exhibitId;
    [SerializeField] private string exhibitName;
    [SerializeField] private string artistName;
    [SerializeField] private string productionYear;
    [SerializeField] private string medium;
    [SerializeField] private string roomId;

    // 전시물 요약용 키워드 목록을 보관한다.
    [SerializeField] private string[] representativeKeywords = System.Array.Empty<string>();

    // 1차 해설과 심화 정보를 분리해 이후 UI 단계에서 나누어 표시할 수 있게 한다.
    [TextArea(3, 6)]
    [SerializeField] private string primaryNarration;

    [TextArea(4, 10)]
    [SerializeField] private string detailedInformation;

    public string ExhibitId => exhibitId;
    public string ExhibitName => exhibitName;
    public string ArtistName => artistName;
    public string ProductionYear => productionYear;
    public string Medium => medium;
    public string RoomId => roomId;
    public IReadOnlyList<string> RepresentativeKeywords => representativeKeywords;
    public string PrimaryNarration => primaryNarration;
    public string DetailedInformation => detailedInformation;

    /// <summary>
    /// 다음 단계에서 안전하게 참조할 수 있는 최소 데이터가 채워졌는지 확인한다.
    /// </summary>
    public bool HasValidCoreData()
    {
        return CollectValidationWarnings(null) == 0;
    }

    /// <summary>
    /// 비어 있거나 누락된 값이 있으면 경고 로그로 정리해 준다.
    /// </summary>
    public void LogValidationWarnings(Object logContext = null)
    {
        List<string> warnings = new List<string>();
        int warningCount = CollectValidationWarnings(warnings);

        if (warningCount <= 0)
        {
            return;
        }

        Object context = logContext != null ? logContext : this;
        Debug.LogWarning($"HO_ExhibitData '{name}' has {warningCount} validation warning(s):\n- {string.Join("\n- ", warnings)}", context);
    }

    /// <summary>
    /// 인스펙터 값 정리와 null 배열 방지를 통해 기본 데이터 형태를 유지한다.
    /// </summary>
    private void OnValidate()
    {
        exhibitId = exhibitId != null ? exhibitId.Trim() : string.Empty;
        exhibitName = exhibitName != null ? exhibitName.Trim() : string.Empty;
        artistName = artistName != null ? artistName.Trim() : string.Empty;
        productionYear = productionYear != null ? productionYear.Trim() : string.Empty;
        medium = medium != null ? medium.Trim() : string.Empty;
        roomId = roomId != null ? roomId.Trim() : string.Empty;
        primaryNarration = primaryNarration != null ? primaryNarration.Trim() : string.Empty;
        detailedInformation = detailedInformation != null ? detailedInformation.Trim() : string.Empty;

        if (representativeKeywords == null)
        {
            representativeKeywords = System.Array.Empty<string>();
            return;
        }

        for (int index = 0; index < representativeKeywords.Length; index++)
        {
            representativeKeywords[index] = representativeKeywords[index] != null
                ? representativeKeywords[index].Trim()
                : string.Empty;
        }
    }

    /// <summary>
    /// 필수값 누락 여부를 한곳에서 모아 이후 체크나 로그에 재사용한다.
    /// </summary>
    private int CollectValidationWarnings(List<string> warnings)
    {
        int warningCount = 0;

        warningCount += AddWarningIfEmpty(exhibitId, "Exhibit ID is empty.", warnings);
        warningCount += AddWarningIfEmpty(exhibitName, "Exhibit name is empty.", warnings);
        warningCount += AddWarningIfEmpty(artistName, "Artist name is empty.", warnings);
        warningCount += AddWarningIfEmpty(productionYear, "Production year is empty.", warnings);
        warningCount += AddWarningIfEmpty(medium, "Medium is empty.", warnings);
        warningCount += AddWarningIfEmpty(roomId, "Room ID is empty.", warnings);
        warningCount += AddWarningIfEmpty(primaryNarration, "Primary narration is empty.", warnings);
        warningCount += AddWarningIfEmpty(detailedInformation, "Detailed information is empty.", warnings);

        if (representativeKeywords == null || representativeKeywords.Length == 0)
        {
            warningCount++;
            warnings?.Add("Representative keywords are empty.");
        }

        return warningCount;
    }

    /// <summary>
    /// 문자열 필수값 누락 여부를 공통 규칙으로 확인한다.
    /// </summary>
    private static int AddWarningIfEmpty(string value, string message, List<string> warnings)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        warnings?.Add(message);
        return 1;
    }
}
