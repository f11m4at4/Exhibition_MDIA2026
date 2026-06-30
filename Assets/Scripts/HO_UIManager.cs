using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전시에서 공통으로 사용하는 프롬프트, 해설, 종료 메시지 UI를 열고 닫는다.
/// </summary>
public sealed class HO_UIManager : MonoBehaviour
{
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private Text promptText;
    [SerializeField] private GameObject narrationPanel;
    [SerializeField] private Text narrationTitleText;
    [SerializeField] private Text narrationBodyText;
    [SerializeField] private Text narrationHintText;
    [SerializeField] private GameObject endingPanel;
    [SerializeField] private Text endingTitleText;
    [SerializeField] private Text endingBodyText;

    private OpenPanelType currentOpenPanel = OpenPanelType.None;

    private enum OpenPanelType
    {
        None,
        Prompt,
        Narration,
        Ending
    }

    /// <summary>
    /// 필수 참조를 확인하고 시작 시 모든 주요 패널을 닫힌 상태로 정리한다.
    /// </summary>
    private void Awake()
    {
        SetPanelActive(promptPanel, false);
        SetPanelActive(narrationPanel, false);
        SetPanelActive(endingPanel, false);
        currentOpenPanel = OpenPanelType.None;

        if (!ValidateReferences())
        {
            enabled = false;
        }
    }

    /// <summary>
    /// 상호작용 가능 상태를 알리는 프롬프트를 단독으로 표시한다.
    /// </summary>
    public void ShowPrompt(string promptMessage)
    {
        if (!enabled)
        {
            return;
        }

        promptText.text = string.IsNullOrWhiteSpace(promptMessage) ? "Press E to interact." : promptMessage;
        OpenExclusivePanel(OpenPanelType.Prompt);
    }

    /// <summary>
    /// 현재 표시 중인 프롬프트를 닫는다.
    /// </summary>
    public void HidePrompt()
    {
        if (!enabled)
        {
            return;
        }

        if (currentOpenPanel == OpenPanelType.Prompt)
        {
            HideAllPanels();
        }
        else
        {
            SetPanelActive(promptPanel, false);
        }
    }

    /// <summary>
    /// 큐레이터 해설용 제목, 본문, 힌트 텍스트를 채우고 해설 패널만 연다.
    /// </summary>
    public void ShowNarration(string title, string body, string hint)
    {
        if (!enabled)
        {
            return;
        }

        narrationTitleText.text = title ?? string.Empty;
        narrationBodyText.text = body ?? string.Empty;
        narrationHintText.text = hint ?? string.Empty;
        OpenExclusivePanel(OpenPanelType.Narration);
    }

    /// <summary>
    /// 현재 표시 중인 해설 패널을 닫는다.
    /// </summary>
    public void HideNarration()
    {
        if (!enabled)
        {
            return;
        }

        if (currentOpenPanel == OpenPanelType.Narration)
        {
            HideAllPanels();
        }
        else
        {
            SetPanelActive(narrationPanel, false);
        }
    }

    /// <summary>
    /// 전시 종료 메시지 제목과 본문을 채우고 종료 패널만 연다.
    /// </summary>
    public void ShowEndingMessage(string title, string body)
    {
        if (!enabled)
        {
            return;
        }

        endingTitleText.text = title ?? string.Empty;
        endingBodyText.text = body ?? string.Empty;
        OpenExclusivePanel(OpenPanelType.Ending);
    }

    /// <summary>
    /// 현재 열려 있는 모든 주요 패널을 닫는다.
    /// </summary>
    public void HideAllPanels()
    {
        if (!enabled)
        {
            return;
        }

        SetPanelActive(promptPanel, false);
        SetPanelActive(narrationPanel, false);
        SetPanelActive(endingPanel, false);
        currentOpenPanel = OpenPanelType.None;
    }

    /// <summary>
    /// 플레이어 조작을 막아야 하는 차단형 패널이 열려 있는지 확인한다.
    /// </summary>
    public bool HasOpenBlockingPanel()
    {
        if (!enabled)
        {
            return false;
        }

        return narrationPanel.activeSelf || endingPanel.activeSelf;
    }

    /// <summary>
    /// 하나의 주요 패널만 열리도록 다른 패널을 모두 닫고 대상 패널만 연다.
    /// </summary>
    private void OpenExclusivePanel(OpenPanelType panelType)
    {
        SetPanelActive(promptPanel, panelType == OpenPanelType.Prompt);
        SetPanelActive(narrationPanel, panelType == OpenPanelType.Narration);
        SetPanelActive(endingPanel, panelType == OpenPanelType.Ending);
        currentOpenPanel = panelType;
    }

    /// <summary>
    /// 필수 UI 참조가 빠졌는지 확인하고 누락 시 안전 중단용 오류를 남긴다.
    /// </summary>
    private bool ValidateReferences()
    {
        bool hasAllReferences = true;
        hasAllReferences &= ValidateReference(promptPanel, "Prompt Panel");
        hasAllReferences &= ValidateReference(promptText, "Prompt Text");
        hasAllReferences &= ValidateReference(narrationPanel, "Narration Panel");
        hasAllReferences &= ValidateReference(narrationTitleText, "Narration Title Text");
        hasAllReferences &= ValidateReference(narrationBodyText, "Narration Body Text");
        hasAllReferences &= ValidateReference(narrationHintText, "Narration Hint Text");
        hasAllReferences &= ValidateReference(endingPanel, "Ending Panel");
        hasAllReferences &= ValidateReference(endingTitleText, "Ending Title Text");
        hasAllReferences &= ValidateReference(endingBodyText, "Ending Body Text");

        if (!hasAllReferences)
        {
            Debug.LogError("HO_UIManager requires all panel and text references to be assigned.", this);
        }

        return hasAllReferences;
    }

    /// <summary>
    /// 공용 형식으로 참조 누락 여부를 검사한다.
    /// </summary>
    private bool ValidateReference(Object targetReference, string referenceName)
    {
        if (targetReference != null)
        {
            return true;
        }

        Debug.LogError($"HO_UIManager is missing required reference: {referenceName}.", this);
        return false;
    }

    /// <summary>
    /// 패널 참조가 있을 때만 활성 상태를 바꿔 안전하게 UI를 제어한다.
    /// </summary>
    private static void SetPanelActive(GameObject panel, bool isActive)
    {
        if (panel != null)
        {
            panel.SetActive(isActive);
        }
    }
}
