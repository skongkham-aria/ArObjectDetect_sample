using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manual UI setup script for the Detection Tester
/// Attach this to a GameObject and assign the DetectionTester reference
/// </summary>
public class DetectionUI : MonoBehaviour
{
    [Header("UI References")]
    public DetectionTester detectionTester;
    public Canvas canvas;
    public Text statusText;
    public Text resultsText;
    public RawImage imageDisplay;
    public Button testButton;
    public Button initButton;

    void Start()
    {
        SetupUI();
        
        // Link buttons if they exist
        if (testButton != null)
        {
            testButton.onClick.AddListener(() => {
                if (detectionTester != null)
                    detectionTester.RunTest();
            });
        }
        
        if (initButton != null)
        {
            initButton.onClick.AddListener(() => {
                if (detectionTester != null)
                    detectionTester.InitializeOnly();
            });
        }
        
        // Auto-find DetectionTester if not assigned
        if (detectionTester == null)
        {
            detectionTester = FindObjectOfType<DetectionTester>();
        }
        
        // Assign UI elements to the DetectionTester
        if (detectionTester != null)
        {
            detectionTester.uiCanvas = canvas;
            detectionTester.statusText = statusText;
            detectionTester.resultsText = resultsText;
            detectionTester.imageDisplay = imageDisplay;
        }
    }

    void SetupUI()
    {
        if (canvas == null)
        {
            canvas = GetComponentInChildren<Canvas>();
        }
        
        if (statusText == null)
        {
            statusText = GameObject.Find("StatusText")?.GetComponent<Text>();
        }
        
        if (resultsText == null)
        {
            resultsText = GameObject.Find("ResultsText")?.GetComponent<Text>();
        }
        
        if (imageDisplay == null)
        {
            imageDisplay = GameObject.Find("ImageDisplay")?.GetComponent<RawImage>();
        }
        
        if (testButton == null)
        {
            testButton = GameObject.Find("TestButton")?.GetComponent<Button>();
        }
        
        if (initButton == null)
        {
            initButton = GameObject.Find("InitButton")?.GetComponent<Button>();
        }
    }

    /// <summary>
    /// Create a complete UI setup programmatically
    /// </summary>
    [ContextMenu("Create Full UI")]
    public void CreateFullUI()
    {
        // Create Canvas if needed
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("DetectionCanvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        // Create EventSystem if needed
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        CreateStatusText();
        CreateResultsText();
        CreateImageDisplay();
        CreateButtons();
        
        Debug.Log("Full UI created successfully!");
    }

    void CreateStatusText()
    {
        GameObject statusGO = new GameObject("StatusText");
        statusGO.transform.SetParent(canvas.transform, false);
        statusText = statusGO.AddComponent<Text>();
        statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statusText.fontSize = 20;
        statusText.color = Color.white;
        statusText.text = "Ready to test detection";
        statusText.alignment = TextAnchor.MiddleCenter;
        
        // Add background
        Image bg = statusGO.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.7f);
        
        RectTransform rt = statusText.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.anchoredPosition = new Vector2(0, -25);
        rt.sizeDelta = new Vector2(-20, 40);
    }

    void CreateResultsText()
    {
        GameObject resultsGO = new GameObject("ResultsText");
        resultsGO.transform.SetParent(canvas.transform, false);
        resultsText = resultsGO.AddComponent<Text>();
        resultsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        resultsText.fontSize = 14;
        resultsText.color = Color.yellow;
        resultsText.text = "Test results will appear here...";
        resultsText.alignment = TextAnchor.UpperLeft;
        
        // Add background
        Image bg = resultsGO.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.5f);
        
        RectTransform rt = resultsText.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(0.45f, 1);
        rt.offsetMin = new Vector2(10, 60);
        rt.offsetMax = new Vector2(-10, -60);
    }

    void CreateImageDisplay()
    {
        GameObject imageGO = new GameObject("ImageDisplay");
        imageGO.transform.SetParent(canvas.transform, false);
        imageDisplay = imageGO.AddComponent<RawImage>();
        imageDisplay.color = Color.white;
        
        // Add background
        GameObject bgGO = new GameObject("ImageBackground");
        bgGO.transform.SetParent(imageGO.transform, false);
        bgGO.transform.SetSiblingIndex(0);
        Image bg = bgGO.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        
        RectTransform bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        
        RectTransform rt = imageDisplay.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.55f, 0);
        rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = new Vector2(10, 60);
        rt.offsetMax = new Vector2(-10, -60);
    }

    void CreateButtons()
    {
        // Test Button
        GameObject testButtonGO = new GameObject("TestButton");
        testButtonGO.transform.SetParent(canvas.transform, false);
        testButton = testButtonGO.AddComponent<Button>();
        
        Image testBtnImg = testButtonGO.AddComponent<Image>();
        testBtnImg.color = new Color(0.2f, 0.8f, 0.2f, 0.8f);
        
        GameObject testBtnTextGO = new GameObject("Text");
        testBtnTextGO.transform.SetParent(testButtonGO.transform, false);
        Text testBtnText = testBtnTextGO.AddComponent<Text>();
        testBtnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        testBtnText.fontSize = 16;
        testBtnText.color = Color.white;
        testBtnText.text = "Run Full Test";
        testBtnText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform testBtnTextRT = testBtnText.GetComponent<RectTransform>();
        testBtnTextRT.anchorMin = Vector2.zero;
        testBtnTextRT.anchorMax = Vector2.one;
        testBtnTextRT.offsetMin = Vector2.zero;
        testBtnTextRT.offsetMax = Vector2.zero;
        
        RectTransform testBtnRT = testButton.GetComponent<RectTransform>();
        testBtnRT.anchorMin = new Vector2(0, 0);
        testBtnRT.anchorMax = new Vector2(0, 0);
        testBtnRT.anchoredPosition = new Vector2(80, 30);
        testBtnRT.sizeDelta = new Vector2(140, 35);

        // Init Button
        GameObject initButtonGO = new GameObject("InitButton");
        initButtonGO.transform.SetParent(canvas.transform, false);
        initButton = initButtonGO.AddComponent<Button>();
        
        Image initBtnImg = initButtonGO.AddComponent<Image>();
        initBtnImg.color = new Color(0.2f, 0.2f, 0.8f, 0.8f);
        
        GameObject initBtnTextGO = new GameObject("Text");
        initBtnTextGO.transform.SetParent(initButtonGO.transform, false);
        Text initBtnText = initBtnTextGO.AddComponent<Text>();
        initBtnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        initBtnText.fontSize = 16;
        initBtnText.color = Color.white;
        initBtnText.text = "Init Only";
        initBtnText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform initBtnTextRT = initBtnText.GetComponent<RectTransform>();
        initBtnTextRT.anchorMin = Vector2.zero;
        initBtnTextRT.anchorMax = Vector2.one;
        initBtnTextRT.offsetMin = Vector2.zero;
        initBtnTextRT.offsetMax = Vector2.zero;
        
        RectTransform initBtnRT = initButton.GetComponent<RectTransform>();
        initBtnRT.anchorMin = new Vector2(0, 0);
        initBtnRT.anchorMax = new Vector2(0, 0);
        initBtnRT.anchoredPosition = new Vector2(240, 30);
        initBtnRT.sizeDelta = new Vector2(100, 35);
    }
}