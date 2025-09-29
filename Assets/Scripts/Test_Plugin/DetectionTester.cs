using System.Collections;
using System.IO;
using UnityEngine;
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

    private TensorFlowLiteDetector detector;
    private string modelPath;
    private string imagePath;

    void Start()
    {
        Debug.Log("=== Detection Tester Started ===");
        
        // Initialize detector
        detector = new TensorFlowLiteDetector();
        
        // Set up file paths
        modelPath = Path.Combine(Application.streamingAssetsPath, modelFileName);
        imagePath = Path.Combine(Application.streamingAssetsPath, testImageFileName);
        
        Debug.Log($"Model path: {modelPath}");
        Debug.Log($"Image path: {imagePath}");
        
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
        
        string testString = detector.GetTestString();
        Debug.Log($"Test string from native library: {testString}");
        
        yield return new WaitForSeconds(0.5f);
    }

    /// <summary>
    /// Initialize the TensorFlow Lite model
    /// </summary>
    private IEnumerator InitializeModel()
    {
        Debug.Log("--- Initializing Model ---");
        
        #if UNITY_ANDROID && !UNITY_EDITOR
        // On Android, we need to copy from StreamingAssets to persistent data
        string persistentModelPath = Path.Combine(Application.persistentDataPath, modelFileName);
        
        if (!File.Exists(persistentModelPath))
        {
            Debug.Log("Copying model to persistent data path...");
            yield return StartCoroutine(CopyStreamingAssetToPersistent(modelFileName, persistentModelPath));
        }
        
        bool initSuccess = detector.Initialize(persistentModelPath);
        #else
        // In editor, use StreamingAssets directly
        bool initSuccess = detector.Initialize(modelPath);
        #endif
        
        if (initSuccess)
        {
            Debug.Log("✓ Model initialized successfully!");
        }
        else
        {
            Debug.LogError("✗ Model initialization failed!");
        }
        
        yield return new WaitForSeconds(0.5f);
    }

    /// <summary>
    /// Load test image and run detection
    /// </summary>
    private IEnumerator LoadAndTestImage()
    {
        Debug.Log("--- Loading and Testing Image ---");
        
        if (!detector.IsInitialized())
        {
            Debug.LogError("Cannot test image: Model not initialized");
            yield break;
        }

        #if UNITY_ANDROID && !UNITY_EDITOR
        // Load image from StreamingAssets on Android
        string imageUrl = "file://" + imagePath;
        WWW www = new WWW(imageUrl);
        yield return www;
        
        if (!string.IsNullOrEmpty(www.error))
        {
            Debug.LogError($"Failed to load image: {www.error}");
            yield break;
        }
        
        Texture2D texture = www.texture;
        #else
        // In editor, load directly
        if (!File.Exists(imagePath))
        {
            Debug.LogError($"Test image not found: {imagePath}");
            yield break;
        }
        
        byte[] imageFileData = File.ReadAllBytes(imagePath);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(imageFileData);
        #endif
        
        if (texture == null)
        {
            Debug.LogError("Failed to create texture from image");
            yield break;
        }
        
        Debug.Log($"Image loaded: {texture.width}x{texture.height}");
        
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
        
        // Run detection
        Debug.Log("Running object detection...");
        int detectedObjects = detector.DetectObjects(imageData, texture.width, texture.height);
        
        // Display results
        if (detectedObjects >= 0)
        {
            Debug.Log($"✓ Detection completed! Found {detectedObjects} objects");
        }
        else
        {
            Debug.LogError($"✗ Detection failed with error code: {detectedObjects}");
        }
        
        // Cleanup texture
        DestroyImmediate(texture);
        
        yield return new WaitForSeconds(0.5f);
    }

    /// <summary>
    /// Copy file from StreamingAssets to persistent data path (needed on Android)
    /// </summary>
    private IEnumerator CopyStreamingAssetToPersistent(string fileName, string targetPath)
    {
        string sourcePath = Path.Combine(Application.streamingAssetsPath, fileName);
        
        #if UNITY_ANDROID && !UNITY_EDITOR
        WWW www = new WWW(sourcePath);
        yield return www;
        
        if (string.IsNullOrEmpty(www.error))
        {
            File.WriteAllBytes(targetPath, www.bytes);
            Debug.Log($"File copied to: {targetPath}");
        }
        else
        {
            Debug.LogError($"Failed to copy file: {www.error}");
        }
        #else
        if (File.Exists(sourcePath))
        {
            File.Copy(sourcePath, targetPath, true);
            Debug.Log($"File copied to: {targetPath}");
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
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && detector != null)
        {
            // Cleanup when app is paused
            detector.Cleanup();
        }
    }
}