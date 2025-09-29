using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using DetectionPlugin;

/// <summary>
/// Simple test script to test the TensorFlow Lite detection plugin
/// </summary>
public class DetectionTester : MonoBehaviour
{
    [Header("Configuration")]
    public bool autoStart = true;
    public string modelFileName = "yolo11n_float32.tflite";
    public string testImageFileName = "test.jpg";

    [Header("UI Display")]
    public Canvas uiCanvas;
    public Text statusText;
    public Text resultsText;
    public RawImage imageDisplay;
    public GameObject labelPrefab;

    private TensorFlowLiteDetector detector;
    private string modelPath;
    private string imagePath;
    
    // UI state
    private string currentStatus = "Initializing...";
    private string currentResults = "";
    private Texture2D currentTexture;

    void Start()
    {
        Debug.Log("=== Detection Tester Started ===");
        
        // Initialize detector
        detector = new TensorFlowLiteDetector();
        if (detector == null)
        {
            Debug.LogError("Failed to initialize TensorFlowLiteDetector!");
            UpdateStatus("✗ Failed to initialize detector!");
            return;
        }
        
        // Set up file paths
        modelPath = Path.Combine(Application.streamingAssetsPath, modelFileName);
        imagePath = Path.Combine(Application.streamingAssetsPath, testImageFileName);
        
        Debug.Log($"Model path: {modelPath}");
        Debug.Log($"Image path: {imagePath}");
        
        // Create UI if not assigned
        CreateUIIfNeeded();
        
        // Update initial status
        UpdateStatus("Ready to start detection test");
        
        if (autoStart)
        {
            StartCoroutine(TestDetectionRoutine());
        }
    }

    /// <summary>
    /// Test the detection pipeline step by step
    /// </summary>
    public IEnumerator TestDetectionRoutine()
    {
        Debug.Log("=== Starting Detection Test ===");
        
        // Step 1: Test native library connection
        yield return StartCoroutine(TestNativeLibraryConnection());
        
        // Step 2: Initialize the model
        yield return StartCoroutine(InitializeModel());
        
        // Step 3: Load and test image
        yield return StartCoroutine(LoadAndTestImage());
        
        Debug.Log("=== Detection Test Completed ===");
    }

    /// <summary>
    /// Test if we can communicate with the native library
    /// </summary>
    private IEnumerator TestNativeLibraryConnection()
    {
        Debug.Log("--- Testing Native Library Connection ---");
        UpdateStatus("Testing native library connection...");
        
        string testString = detector.GetTestString();
        Debug.Log($"Test string from native library: {testString}");
        
        UpdateResults($"Native Library Test:\n{testString}");
        
        yield return new WaitForSeconds(1f);
    }

    /// <summary>
    /// Initialize the TensorFlow Lite model
    /// </summary>
    private IEnumerator InitializeModel()
    {
        Debug.Log("--- Initializing Model ---");
        UpdateStatus("Initializing TensorFlow Lite model...");
        
        string finalModelPath = "";
        
        #if UNITY_ANDROID && !UNITY_EDITOR
        // On Android, we need to copy from StreamingAssets to persistent data
        string persistentModelPath = Path.Combine(Application.persistentDataPath, modelFileName);
        finalModelPath = persistentModelPath;
        
        Debug.Log($"Android detected. Persistent path: {persistentModelPath}");
        Debug.Log($"StreamingAssets path: {modelPath}");
        
        if (!File.Exists(persistentModelPath))
        {
            Debug.Log("Model not found in persistent data, copying from StreamingAssets...");
            UpdateStatus("Copying model file...");
            yield return StartCoroutine(CopyStreamingAssetToPersistent(modelFileName, persistentModelPath));
            
            // Verify copy was successful
            if (File.Exists(persistentModelPath))
            {
                long fileSize = new FileInfo(persistentModelPath).Length;
                Debug.Log($"✓ Model copied successfully. Size: {fileSize} bytes");
            }
            else
            {
                Debug.LogError("✗ Failed to copy model file!");
                UpdateStatus("✗ Failed to copy model file!");
                UpdateResults(currentResults + "\n\nModel Copy: FAILED");
                yield break;
            }
        }
        else
        {
            long fileSize = new FileInfo(persistentModelPath).Length;
            Debug.Log($"Model already exists in persistent data. Size: {fileSize} bytes");
        }
        
        #else
        // In editor, use StreamingAssets directly
        finalModelPath = modelPath;
        Debug.Log($"Editor detected. Using StreamingAssets path: {finalModelPath}");
        
        // Check if file exists in editor
        if (File.Exists(finalModelPath))
        {
            long fileSize = new FileInfo(finalModelPath).Length;
            Debug.Log($"✓ Model file found in StreamingAssets. Size: {fileSize} bytes");
        }
        else
        {
            Debug.LogError($"✗ Model file NOT found at: {finalModelPath}");
            UpdateStatus("✗ Model file not found!");
            UpdateResults(currentResults + "\n\nModel File: NOT FOUND");
            yield break;
        }
        #endif
        
        Debug.Log($"Attempting to initialize model with path: {finalModelPath}");
        UpdateStatus("Initializing model...");
        
        bool initSuccess = detector.Initialize(finalModelPath);
        
        if (initSuccess)
        {
            Debug.Log("✓ Model initialized successfully!");
            UpdateStatus("✓ Model initialized successfully!");
            
            // Get and display input dimensions
            int[] inputDimensions = detector.GetInputDimensions();
            if (inputDimensions != null && inputDimensions.Length >= 3)
            {
                Debug.Log($"Model input dimensions: {inputDimensions[0]}x{inputDimensions[1]}x{inputDimensions[2]}");
                UpdateResults(currentResults + $"\n\nModel Initialization: SUCCESS\nInput Dimensions: {inputDimensions[0]}x{inputDimensions[1]}x{inputDimensions[2]}");
            }
            else
            {
                Debug.LogWarning("Could not retrieve input dimensions");
                UpdateResults(currentResults + "\n\nModel Initialization: SUCCESS");
            }
        }
        else
        {
            Debug.LogError("✗ Model initialization failed!");
            UpdateStatus("✗ Model initialization failed!");
            UpdateResults(currentResults + "\n\nModel Initialization: FAILED");
        }
        
        yield return new WaitForSeconds(1f);
    }

    /// <summary>
    /// Load test image and run detection
    /// </summary>
    private IEnumerator LoadAndTestImage()
    {
        Debug.Log("--- Loading and Testing Image ---");
        UpdateStatus("Loading test image...");
        
        if (!detector.IsInitialized())
        {
            Debug.LogError("Cannot test image: Model not initialized");
            UpdateStatus("✗ Cannot test: Model not initialized");
            yield break;
        }

        #if UNITY_ANDROID && !UNITY_EDITOR
        // Load image from StreamingAssets on Android
        string imageUrl = imagePath;
        // On Android, Application.streamingAssetsPath already includes the proper scheme (jar:file://)
        if (!imagePath.StartsWith("jar:") && !imagePath.StartsWith("file://"))
        {
            imageUrl = "file://" + imagePath;
        }
        WWW www = new WWW(imageUrl);
        yield return www;
        
        if (!string.IsNullOrEmpty(www.error))
        {
            Debug.LogError($"Failed to load image: {www.error}");
            UpdateStatus($"✗ Failed to load image: {www.error}");
            yield break;
        }
        
        Texture2D texture = www.texture;
        #else
        // In editor, load directly
        if (!File.Exists(imagePath))
        {
            Debug.LogError($"Test image not found: {imagePath}");
            UpdateStatus($"✗ Test image not found: {imagePath}");
            yield break;
        }
        
        byte[] imageFileData = File.ReadAllBytes(imagePath);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(imageFileData);
        #endif
        
        if (texture == null)
        {
            Debug.LogError("Failed to create texture from image");
            UpdateStatus("✗ Failed to create texture from image");
            yield break;
        }
        
        Debug.Log($"Image loaded: {texture.width}x{texture.height}");
        UpdateStatus($"Image loaded: {texture.width}x{texture.height}");
        
        // Display the image in UI
        currentTexture = texture;
        if (imageDisplay != null)
        {
            imageDisplay.texture = texture;
        }
        
        // Convert texture to byte array (RGB24 format)
        Color32[] pixels = texture.GetPixels32();
        byte[] imageData = new byte[pixels.Length * 3]; // RGB format
        
        for (int i = 0; i < pixels.Length; i++)
        {
            imageData[i * 3] = pixels[i].r;     // R
            imageData[i * 3 + 1] = pixels[i].g; // G  
            imageData[i * 3 + 2] = pixels[i].b; // B
        }
        
        Debug.Log($"Image data prepared: {imageData.Length} bytes");
        UpdateStatus("Running object detection...");
        
        // Run detection
        Debug.Log("Running object detection...");
        int channels = 3; // RGB format
        string detectionResult = detector.DetectObjects(imageData, texture.width, texture.height, channels);
        
        // Parse the JSON result
        int detectedObjects = 0;
        string detectionResults = "";
        
        if (!string.IsNullOrEmpty(detectionResult))
        {
            try
            {
                Debug.Log($"DetectionTester: Parsing JSON result: {detectionResult}");
                
                // Simple JSON parsing to extract total_detections
                if (detectionResult.Contains("total_detections"))
                {
                    // Look for the pattern with space after colon
                    string pattern = "\"total_detections\": ";
                    int startIndex = detectionResult.IndexOf(pattern);
                    if (startIndex == -1)
                    {
                        // Try without space as fallback
                        pattern = "\"total_detections\":";
                        startIndex = detectionResult.IndexOf(pattern);
                    }
                    
                    if (startIndex != -1)
                    {
                        startIndex += pattern.Length;
                        int endIndex = detectionResult.IndexOf(",", startIndex);
                        if (endIndex == -1) endIndex = detectionResult.IndexOf("}", startIndex);
                        
                        if (endIndex > startIndex)
                        {
                            string countStr = detectionResult.Substring(startIndex, endIndex - startIndex).Trim();
                            Debug.Log($"DetectionTester: Extracted count string: '{countStr}'");
                            
                            if (int.TryParse(countStr, out detectedObjects))
                            {
                                Debug.Log($"✓ Detection completed! Found {detectedObjects} objects");
                                Debug.Log($"Full detection JSON: {detectionResult}");
                                
                                UpdateStatus($"✓ Detection completed!");
                                detectionResults = $"DETECTION RESULTS:\n" +
                                                 $"Objects Found: {detectedObjects}\n" +
                                                 $"Image Size: {texture.width}x{texture.height}\n" +
                                                 $"Data Size: {imageData.Length} bytes\n" +
                                                 $"Model: {modelFileName}\n\n" +
                                                 $"Raw JSON Result:\n{detectionResult}";
                            }
                            else
                            {
                                Debug.LogError($"Failed to parse detection count from string: '{countStr}'");
                                detectedObjects = -1;
                            }
                        }
                        else
                        {
                            Debug.LogError("Failed to find end of total_detections value in JSON");
                            detectedObjects = -1;
                        }
                    }
                    else
                    {
                        Debug.LogError("Failed to find total_detections pattern in JSON");
                        detectedObjects = -1;
                    }
                }
                else
                {
                    Debug.LogError("Invalid JSON format - missing total_detections field");
                    Debug.LogError($"Received JSON: {detectionResult}");
                    detectedObjects = -1;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error parsing detection JSON: {e.Message}");
                Debug.LogError($"JSON that failed to parse: {detectionResult}");
                detectedObjects = -1;
            }
        }
        else
        {
            Debug.LogError("DetectObjects returned null or empty result");
            detectedObjects = -1;
        }
        
        // Display results
        if (detectedObjects >= 0)
        {
            Debug.Log($"✓ Detection completed! Found {detectedObjects} objects");
            UpdateStatus($"✓ Detection completed!");
            detectionResults = $"DETECTION RESULTS:\n" +
                             $"Objects Found: {detectedObjects}\n" +
                             $"Image Size: {texture.width}x{texture.height}\n" +
                             $"Data Size: {imageData.Length} bytes\n" +
                             $"Model: {modelFileName}";
            
            // Create detection summary labels on the image
            if (imageDisplay == null)
            {
                Debug.LogWarning("imageDisplay is null before calling CreateDetectionLabels, attempting to create UI");
                CreateUIIfNeeded();
            }
            
            if (imageDisplay != null)
            {
                CreateDetectionLabels(detectedObjects, texture.width, texture.height);
            }
            else
            {
                Debug.LogError("imageDisplay is still null after UI creation, cannot create detection labels");
            }
        }
        else
        {
            Debug.LogError($"✗ Detection failed with error code: {detectedObjects}");
            UpdateStatus($"✗ Detection failed (Error: {detectedObjects})");
            detectionResults = $"DETECTION FAILED:\n" +
                             $"Error Code: {detectedObjects}\n" +
                             $"Check console for details";
        }
        
        UpdateResults(currentResults + "\n\n" + detectionResults);
        
        yield return new WaitForSeconds(2f);
    }

    /// <summary>
    /// Copy file from StreamingAssets to persistent data path (needed on Android)
    /// </summary>
    private IEnumerator CopyStreamingAssetToPersistent(string fileName, string targetPath)
    {
        string sourcePath = Path.Combine(Application.streamingAssetsPath, fileName);
        Debug.Log($"Copying from: {sourcePath}");
        Debug.Log($"Copying to: {targetPath}");
        
        #if UNITY_ANDROID && !UNITY_EDITOR
        string sourceUrl = sourcePath;
        // On Android, Application.streamingAssetsPath already includes the proper scheme for APK files
        if (!sourcePath.StartsWith("jar:") && !sourcePath.StartsWith("file://"))
        {
            sourceUrl = "file://" + sourcePath;
        }
        
        Debug.Log($"Using URL: {sourceUrl}");
        WWW www = new WWW(sourceUrl);
        yield return www;
        
        if (string.IsNullOrEmpty(www.error))
        {
            try
            {
                // Ensure target directory exists
                string targetDirectory = Path.GetDirectoryName(targetPath);
                if (!Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                    Debug.Log($"Created directory: {targetDirectory}");
                }
                
                File.WriteAllBytes(targetPath, www.bytes);
                Debug.Log($"✓ File copied successfully to: {targetPath}");
                Debug.Log($"✓ File size: {www.bytes.Length} bytes");
                
                // Verify the file was written correctly
                if (File.Exists(targetPath))
                {
                    long writtenSize = new FileInfo(targetPath).Length;
                    Debug.Log($"✓ Verification: File exists with size {writtenSize} bytes");
                }
                else
                {
                    Debug.LogError("✗ Verification failed: File was not written");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"✗ Exception while writing file: {e.Message}");
            }
        }
        else
        {
            Debug.LogError($"✗ Failed to copy file: {www.error}");
            Debug.LogError($"✗ Source URL was: {sourceUrl}");
        }
        
        www.Dispose();
        #else
        if (File.Exists(sourcePath))
        {
            try
            {
                // Ensure target directory exists
                string targetDirectory = Path.GetDirectoryName(targetPath);
                if (!Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                    Debug.Log($"Created directory: {targetDirectory}");
                }
                
                File.Copy(sourcePath, targetPath, true);
                Debug.Log($"✓ File copied to: {targetPath}");
                
                // Verify copy
                if (File.Exists(targetPath))
                {
                    long sourceSize = new FileInfo(sourcePath).Length;
                    long targetSize = new FileInfo(targetPath).Length;
                    Debug.Log($"✓ Copy verified: {sourceSize} -> {targetSize} bytes");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"✗ Exception while copying file: {e.Message}");
            }
        }
        else
        {
            Debug.LogError($"✗ Source file not found: {sourcePath}");
        }
        yield return null;
        #endif
    }

    /// <summary>
    /// Manual test trigger (can be called from inspector or other scripts)
    /// </summary>
    [ContextMenu("Run Detection Test")]
    public void RunTest()
    {
        if (Application.isPlaying)
        {
            StartCoroutine(TestDetectionRoutine());
        }
        else
        {
            Debug.LogWarning("Test can only be run in play mode");
        }
    }

    /// <summary>
    /// Manual initialization trigger
    /// </summary>
    [ContextMenu("Initialize Only")]
    public void InitializeOnly()
    {
        if (Application.isPlaying)
        {
            StartCoroutine(InitializeModel());
        }
        else
        {
            Debug.LogWarning("Initialize can only be run in play mode");
        }
    }

    void OnDestroy()
    {
        // Cleanup detector when object is destroyed
        if (detector != null)
        {
            detector.Cleanup();
        }
        
        // Cleanup texture
        if (currentTexture != null)
        {
            DestroyImmediate(currentTexture);
        }
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && detector != null)
        {
            // Cleanup when app is paused
            detector.Cleanup();
        }
    }
    
    /// <summary>
    /// Create UI elements if not assigned in inspector
    /// </summary>
    private void CreateUIIfNeeded()
    {
        if (uiCanvas == null)
        {
            // Create Canvas
            GameObject canvasGO = new GameObject("DetectionUI");
            uiCanvas = canvasGO.AddComponent<Canvas>();
            uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        if (statusText == null)
        {
            // Create Status Text
            GameObject statusGO = new GameObject("StatusText");
            statusGO.transform.SetParent(uiCanvas.transform, false);
            statusText = statusGO.AddComponent<Text>();
            if (statusText == null)
            {
                Debug.LogError("CreateUIIfNeeded: Failed to add Text component to statusText");
                DestroyImmediate(statusGO);
                return;
            }
            
            statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf") ?? Resources.Load<Font>("Arial");
            if (statusText.font == null)
            {
                Debug.LogWarning("CreateUIIfNeeded: No font could be loaded for statusText, using default font");
            }
            statusText.fontSize = 24;
            statusText.color = Color.white;
            statusText.text = "Initializing...";
            
            RectTransform statusRT = statusText.GetComponent<RectTransform>();
            statusRT.anchorMin = new Vector2(0, 1);
            statusRT.anchorMax = new Vector2(1, 1);
            statusRT.anchoredPosition = new Vector2(0, -30);
            statusRT.sizeDelta = new Vector2(-20, 50);
        }

        if (resultsText == null)
        {
            // Create Results Text
            GameObject resultsGO = new GameObject("ResultsText");
            resultsGO.transform.SetParent(uiCanvas.transform, false);
            resultsText = resultsGO.AddComponent<Text>();
            if (resultsText == null)
            {
                Debug.LogError("CreateUIIfNeeded: Failed to add Text component to resultsText");
                DestroyImmediate(resultsGO);
                return;
            }
            
            resultsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf") ?? Resources.Load<Font>("Arial");
            if (resultsText.font == null)
            {
                Debug.LogWarning("CreateUIIfNeeded: No font could be loaded for resultsText, using default font");
            }
            resultsText.fontSize = 16;
            resultsText.color = Color.yellow;
            resultsText.text = "";
            resultsText.alignment = TextAnchor.UpperLeft;
            
            RectTransform resultsRT = resultsText.GetComponent<RectTransform>();
            resultsRT.anchorMin = new Vector2(0, 0);
            resultsRT.anchorMax = new Vector2(0.5f, 1);
            resultsRT.offsetMin = new Vector2(10, 10);
            resultsRT.offsetMax = new Vector2(-10, -80);
        }

        if (imageDisplay == null)
        {
            // Create Image Display
            GameObject imageGO = new GameObject("ImageDisplay");
            imageGO.transform.SetParent(uiCanvas.transform, false);
            imageDisplay = imageGO.AddComponent<RawImage>();
            imageDisplay.color = Color.white;
            
            RectTransform imageRT = imageDisplay.GetComponent<RectTransform>();
            imageRT.anchorMin = new Vector2(0.5f, 0);
            imageRT.anchorMax = new Vector2(1, 1);
            imageRT.offsetMin = new Vector2(10, 10);
            imageRT.offsetMax = new Vector2(-10, -80);
            
            // Add a background
            GameObject bgGO = new GameObject("ImageBackground");
            bgGO.transform.SetParent(imageGO.transform, false);
            bgGO.transform.SetSiblingIndex(0);
            Image bg = bgGO.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            RectTransform bgRT = bg.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;
        }
    }
    
    /// <summary>
    /// Update status text
    /// </summary>
    private void UpdateStatus(string status)
    {
        currentStatus = status;
        if (statusText != null)
        {
            statusText.text = status;
        }
        Debug.Log($"Status: {status}");
    }
    
    /// <summary>
    /// Update results text
    /// </summary>
    private void UpdateResults(string results)
    {
        currentResults = results;
        if (resultsText != null)
        {
            resultsText.text = results;
        }
    }
    
    /// <summary>
    /// Create detection result labels on the image
    /// </summary>
    private void CreateDetectionLabels(int objectCount, int imageWidth, int imageHeight)
    {
        if (imageDisplay == null) 
        {
            Debug.LogWarning("CreateDetectionLabels: imageDisplay is null, cannot create labels");
            return;
        }
        
        // Clear existing labels
        try
        {
            // Collect labels to destroy first to avoid collection modification during iteration
            System.Collections.Generic.List<GameObject> labelsToDestroy = new System.Collections.Generic.List<GameObject>();
            
            foreach (Transform child in imageDisplay.transform)
            {
                if (child != null && child.name != null && child.name.StartsWith("DetectionLabel"))
                {
                    labelsToDestroy.Add(child.gameObject);
                }
            }
            
            // Now destroy them safely
            foreach (GameObject labelObj in labelsToDestroy)
            {
                if (labelObj != null)
                {
                    DestroyImmediate(labelObj);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"CreateDetectionLabels: Error clearing existing labels: {e.Message}");
        }
        
        // Create a summary label
        GameObject summaryLabel = new GameObject("DetectionLabel_Summary");
        if (summaryLabel == null)
        {
            Debug.LogError("CreateDetectionLabels: Failed to create summaryLabel GameObject");
            return;
        }
        
        summaryLabel.transform.SetParent(imageDisplay.transform, false);
        
        Text labelText = summaryLabel.AddComponent<Text>();
        if (labelText == null)
        {
            Debug.LogError("CreateDetectionLabels: Failed to add Text component to summaryLabel");
            DestroyImmediate(summaryLabel);
            return;
        }
        
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf") ?? Resources.Load<Font>("Arial");
        if (labelText.font == null)
        {
            Debug.LogWarning("CreateDetectionLabels: No font could be loaded, using default font");
        }
        labelText.fontSize = 20;
        labelText.color = Color.green;
        labelText.fontStyle = FontStyle.Bold;
        labelText.text = $"🎯 {objectCount} Objects Detected";
        labelText.alignment = TextAnchor.MiddleCenter;
        
        // Add background
        Image labelBg = summaryLabel.AddComponent<Image>();
        if (labelBg == null)
        {
            Debug.LogError("CreateDetectionLabels: Failed to add Image component to summaryLabel");
        }
        else
        {
            labelBg.color = new Color(0, 0, 0, 0.7f);
        }
        
        RectTransform labelRT = labelText.GetComponent<RectTransform>();
        if (labelRT == null)
        {
            Debug.LogError("CreateDetectionLabels: Failed to get RectTransform from labelText");
            return;
        }
        
        labelRT.anchorMin = new Vector2(0, 1);
        labelRT.anchorMax = new Vector2(1, 1);
        labelRT.anchoredPosition = new Vector2(0, -25);
        labelRT.sizeDelta = new Vector2(-10, 40);
        
        // Create additional info labels
        if (objectCount > 0)
        {
            CreateInfoLabel("Image Size", $"{imageWidth} × {imageHeight}", new Vector2(0, 0.85f));
            CreateInfoLabel("Model", "YOLO11n", new Vector2(0, 0.75f));
            CreateInfoLabel("Status", "Detection Complete ✓", new Vector2(0, 0.65f));
        }
        else
        {
            CreateInfoLabel("Status", "No objects detected", new Vector2(0, 0.85f));
        }
    }
    
    /// <summary>
    /// Create an info label on the image
    /// </summary>
    private void CreateInfoLabel(string title, string value, Vector2 position)
    {
        if (imageDisplay == null) 
        {
            Debug.LogWarning($"CreateInfoLabel: imageDisplay is null, cannot create label for {title}");
            return;
        }
        
        if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(value))
        {
            Debug.LogWarning("CreateInfoLabel: title or value is null/empty");
            return;
        }
        
        GameObject infoLabel = new GameObject($"DetectionLabel_{title}");
        if (infoLabel == null)
        {
            Debug.LogError($"CreateInfoLabel: Failed to create GameObject for {title}");
            return;
        }
        
        infoLabel.transform.SetParent(imageDisplay.transform, false);
        
        Text labelText = infoLabel.AddComponent<Text>();
        if (labelText == null)
        {
            Debug.LogError($"CreateInfoLabel: Failed to add Text component for {title}");
            DestroyImmediate(infoLabel);
            return;
        }
        
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf") ?? Resources.Load<Font>("Arial");
        if (labelText.font == null)
        {
            Debug.LogWarning($"CreateInfoLabel: No font could be loaded for {title}, using default font");
        }
        labelText.fontSize = 14;
        labelText.color = Color.cyan;
        labelText.text = $"{title}: {value}";
        labelText.alignment = TextAnchor.MiddleLeft;
        
        // Add background
        Image labelBg = infoLabel.AddComponent<Image>();
        if (labelBg == null)
        {
            Debug.LogError($"CreateInfoLabel: Failed to add Image component for {title}");
        }
        else
        {
            labelBg.color = new Color(0, 0, 0, 0.5f);
        }
        
        RectTransform labelRT = labelText.GetComponent<RectTransform>();
        if (labelRT == null)
        {
            Debug.LogError($"CreateInfoLabel: Failed to get RectTransform from labelText for {title}");
            return;
        }
        
        labelRT.anchorMin = position;
        labelRT.anchorMax = position;
        labelRT.anchoredPosition = Vector2.zero;
        labelRT.sizeDelta = new Vector2(200, 25);
    }
}