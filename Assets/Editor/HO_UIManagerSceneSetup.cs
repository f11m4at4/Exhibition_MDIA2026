using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 3단계 프로토타입 UI를 씬에 배치하고 HO_UIManager 참조를 연결한다.
/// </summary>
public static class HO_UIManagerSceneSetup
{
    private const string ScenePath = "Assets/Scenes/Exhibition.unity";
    private const string CanvasName = "Canvas_ExhibitionUI";

    [MenuItem("Tools/Exhibition/Apply UI Manager Scene Setup")]
    public static void ApplyFromMenu()
    {
        Apply();
    }

    public static void ApplyFromCommandLine()
    {
        Apply();
        EditorApplication.Exit(0);
    }

    /// <summary>
    /// Exhibition 씬에 최소 UI 구조를 만들고 참조를 저장한다.
    /// </summary>
    public static void Apply()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Canvas canvas = FindOrCreateCanvas();

        GameObject promptPanel = FindOrCreatePanel(canvas.transform, "PromptPanel");
        GameObject narrationPanel = FindOrCreatePanel(canvas.transform, "NarrationPanel");
        GameObject endingPanel = FindOrCreatePanel(canvas.transform, "EndingPanel");

        ConfigurePanel(promptPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(420f, 72f), new Vector2(0f, 54f));
        ConfigurePanel(narrationPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(900f, 420f), Vector2.zero);
        ConfigurePanel(endingPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(820f, 240f), Vector2.zero);

        ConfigurePanelImage(promptPanel, new Color(0f, 0f, 0f, 0.55f));
        ConfigurePanelImage(narrationPanel, new Color(0.07f, 0.07f, 0.07f, 0.88f));
        ConfigurePanelImage(endingPanel, new Color(0.05f, 0.05f, 0.05f, 0.9f));

        Text promptText = FindOrCreateText(promptPanel.transform, "PromptText");
        Text narrationTitleText = FindOrCreateText(narrationPanel.transform, "NarrationTitleText");
        Text narrationBodyText = FindOrCreateText(narrationPanel.transform, "NarrationBodyText");
        Text narrationHintText = FindOrCreateText(narrationPanel.transform, "NarrationHintText");
        Text endingTitleText = FindOrCreateText(endingPanel.transform, "EndingTitleText");
        Text endingBodyText = FindOrCreateText(endingPanel.transform, "EndingBodyText");

        ConfigureText(promptText, "Press E to interact.", 24, TextAnchor.MiddleCenter, FontStyle.Bold);
        ConfigureTextRect(promptText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-24f, -12f));

        ConfigureText(narrationTitleText, "Narration Title", 34, TextAnchor.UpperLeft, FontStyle.Bold);
        ConfigureTextRect(narrationTitleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(30f, -28f), new Vector2(-30f, 70f));

        ConfigureText(narrationBodyText, "Narration body placeholder.", 24, TextAnchor.UpperLeft, FontStyle.Normal);
        ConfigureTextRect(narrationBodyText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(30f, 72f), new Vector2(-30f, -110f));

        ConfigureText(narrationHintText, "Press E to continue.", 20, TextAnchor.LowerRight, FontStyle.Italic);
        ConfigureTextRect(narrationHintText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(30f, 18f), new Vector2(-30f, 52f));

        ConfigureText(endingTitleText, "Exhibition Complete", 34, TextAnchor.UpperCenter, FontStyle.Bold);
        ConfigureTextRect(endingTitleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(40f, -28f), new Vector2(-40f, 70f));

        ConfigureText(endingBodyText, "Thank you for visiting today's exhibition.", 24, TextAnchor.MiddleCenter, FontStyle.Normal);
        ConfigureTextRect(endingBodyText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(40f, 32f), new Vector2(-40f, -90f));

        promptPanel.SetActive(false);
        narrationPanel.SetActive(false);
        endingPanel.SetActive(false);

        HO_UIManager uiManager = canvas.GetComponent<HO_UIManager>();

        if (uiManager == null)
        {
            uiManager = canvas.gameObject.AddComponent<HO_UIManager>();
        }

        SerializedObject serializedObject = new SerializedObject(uiManager);
        serializedObject.FindProperty("promptPanel").objectReferenceValue = promptPanel;
        serializedObject.FindProperty("promptText").objectReferenceValue = promptText;
        serializedObject.FindProperty("narrationPanel").objectReferenceValue = narrationPanel;
        serializedObject.FindProperty("narrationTitleText").objectReferenceValue = narrationTitleText;
        serializedObject.FindProperty("narrationBodyText").objectReferenceValue = narrationBodyText;
        serializedObject.FindProperty("narrationHintText").objectReferenceValue = narrationHintText;
        serializedObject.FindProperty("endingPanel").objectReferenceValue = endingPanel;
        serializedObject.FindProperty("endingTitleText").objectReferenceValue = endingTitleText;
        serializedObject.FindProperty("endingBodyText").objectReferenceValue = endingBodyText;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("HO_UIManager scene setup completed.");
    }

    /// <summary>
    /// 캔버스를 찾거나 새로 만들어 UI의 루트로 사용한다.
    /// </summary>
    private static Canvas FindOrCreateCanvas()
    {
        GameObject canvasObject = GameObject.Find(CanvasName);

        if (canvasObject == null)
        {
            canvasObject = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        }

        canvasObject.layer = LayerMask.NameToLayer("UI");

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = false;
        canvas.sortingOrder = 0;

        CanvasScaler canvasScaler = canvasObject.GetComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.matchWidthOrHeight = 0.5f;

        RectTransform rectTransform = canvasObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;

        return canvas;
    }

    /// <summary>
    /// 지정 이름의 패널을 찾거나 새로 만든다.
    /// </summary>
    private static GameObject FindOrCreatePanel(Transform parent, string panelName)
    {
        Transform existing = parent.Find(panelName);

        if (existing != null)
        {
            return existing.gameObject;
        }

        GameObject panelObject = new GameObject(panelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObject.layer = LayerMask.NameToLayer("UI");
        panelObject.transform.SetParent(parent, false);
        return panelObject;
    }

    /// <summary>
    /// 지정 이름의 텍스트를 찾거나 새로 만든다.
    /// </summary>
    private static Text FindOrCreateText(Transform parent, string textName)
    {
        Transform existing = parent.Find(textName);

        if (existing != null && existing.TryGetComponent(out Text existingText))
        {
            return existingText;
        }

        GameObject textObject = new GameObject(textName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.layer = LayerMask.NameToLayer("UI");
        textObject.transform.SetParent(parent, false);
        return textObject.GetComponent<Text>();
    }

    /// <summary>
    /// 패널 영역의 앵커와 크기를 최소 프로토타입 배치로 맞춘다.
    /// </summary>
    private static void ConfigurePanel(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPosition)
    {
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = sizeDelta;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;
    }

    /// <summary>
    /// 패널 배경 이미지 색을 설정해 각 UI 구역을 구분한다.
    /// </summary>
    private static void ConfigurePanelImage(GameObject panelObject, Color color)
    {
        Image image = panelObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    /// <summary>
    /// 텍스트 공통 스타일과 기본 문구를 설정한다.
    /// </summary>
    private static void ConfigureText(Text text, string message, int fontSize, TextAnchor anchor, FontStyle fontStyle)
    {
        Font defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        if (defaultFont == null)
        {
            defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        text.font = defaultFont;
        text.text = message;
        text.fontSize = fontSize;
        text.alignment = anchor;
        text.fontStyle = fontStyle;
        text.color = Color.white;
        text.supportRichText = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
    }

    /// <summary>
    /// 텍스트 박스의 앵커와 여백을 설정해 후속 단계 연결 자리를 만든다.
    /// </summary>
    private static void ConfigureTextRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;
    }
}
