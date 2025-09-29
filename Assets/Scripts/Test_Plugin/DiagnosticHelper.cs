using UnityEngine;
using System.IO;

/// <summary>
/// Diagnostic script to help troubleshoot model initialization issues
/// </summary>
public class DiagnosticHelper : MonoBehaviour
{
    [Header("Files to Check")]
    public string modelFileName = "yolo11n_float32.tflite";
    public string testImageFileName = "test.jpg";
    
    [Header("Manual Testing")]
    public bool runOnStart = true;

    void Start()
    {
        if (runOnStart)
        {
            RunDiagnostics();
        }
    }

    [ContextMenu("Run Diagnostics")]
    public void RunDiagnostics()
    {
        Debug.Log("=== DIAGNOSTIC REPORT ===");
        
        CheckUnityEnvironment();
        CheckStreamingAssets();
        CheckPersistentDataPath();
        CheckAAR();
        
        Debug.Log("=== END DIAGNOSTIC REPORT ===");
    }

    void CheckUnityEnvironment()
    {
        Debug.Log("--- Unity Environment ---");
        Debug.Log($"Platform: {Application.platform}");
        Debug.Log($"Unity Version: {Application.unityVersion}");
        Debug.Log($"Is Editor: {Application.isEditor}");
        Debug.Log($"StreamingAssets Path: {Application.streamingAssetsPath}");
        Debug.Log($"Persistent Data Path: {Application.persistentDataPath}");
        
        #if UNITY_ANDROID
        Debug.Log("✓ UNITY_ANDROID is defined");
        #else
        Debug.Log("✗ UNITY_ANDROID is NOT defined");
        #endif
        
        #if UNITY_EDITOR
        Debug.Log("✓ UNITY_EDITOR is defined");
        #else
        Debug.Log("✗ UNITY_EDITOR is NOT defined");
        #endif
    }

    void CheckStreamingAssets()
    {
        Debug.Log("--- StreamingAssets Check ---");
        
        string streamingAssetsPath = Application.streamingAssetsPath;
        Debug.Log($"StreamingAssets directory: {streamingAssetsPath}");
        
        // Check if StreamingAssets directory exists
        #if !UNITY_ANDROID || UNITY_EDITOR
        if (Directory.Exists(streamingAssetsPath))
        {
            Debug.Log("✓ StreamingAssets directory exists");
            
            // List all files in StreamingAssets
            string[] files = Directory.GetFiles(streamingAssetsPath);
            Debug.Log($"Files in StreamingAssets ({files.Length}):");
            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                long fileSize = new FileInfo(file).Length;
                Debug.Log($"  - {fileName} ({fileSize} bytes)");
            }
        }
        else
        {
            Debug.LogError("✗ StreamingAssets directory does not exist!");
        }
        #else
        Debug.Log("On Android device - cannot directly check StreamingAssets directory");
        #endif
        
        // Check specific files
        CheckSpecificFile(modelFileName, "Model file");
        CheckSpecificFile(testImageFileName, "Test image file");
    }

    void CheckSpecificFile(string fileName, string description)
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, fileName);
        Debug.Log($"Checking {description}: {fileName}");
        Debug.Log($"Full path: {filePath}");
        
        #if !UNITY_ANDROID || UNITY_EDITOR
        if (File.Exists(filePath))
        {
            long fileSize = new FileInfo(filePath).Length;
            Debug.Log($"✓ {description} exists ({fileSize} bytes)");
            
            // Additional checks for model file
            if (fileName.EndsWith(".tflite"))
            {
                if (fileSize > 1000000) // > 1MB
                {
                    Debug.Log($"✓ Model file size looks reasonable ({fileSize / 1024 / 1024}MB)");
                }
                else
                {
                    Debug.LogWarning($"⚠ Model file seems small ({fileSize} bytes) - verify it's complete");
                }
            }
        }
        else
        {
            Debug.LogError($"✗ {description} NOT FOUND at: {filePath}");
        }
        #else
        Debug.Log($"On Android - will check {description} during runtime copy");
        #endif
    }

    void CheckPersistentDataPath()
    {
        Debug.Log("--- Persistent Data Path Check ---");
        
        string persistentPath = Application.persistentDataPath;
        Debug.Log($"Persistent data path: {persistentPath}");
        
        try
        {
            if (Directory.Exists(persistentPath))
            {
                Debug.Log("✓ Persistent data directory exists");
                
                // Check if model file already exists in persistent data
                string persistentModelPath = Path.Combine(persistentPath, modelFileName);
                if (File.Exists(persistentModelPath))
                {
                    long fileSize = new FileInfo(persistentModelPath).Length;
                    Debug.Log($"✓ Model already copied to persistent data ({fileSize} bytes)");
                }
                else
                {
                    Debug.Log("○ Model not yet copied to persistent data (this is normal on first run)");
                }
                
                // Test write permissions
                string testFile = Path.Combine(persistentPath, "test_write.txt");
                try
                {
                    File.WriteAllText(testFile, "test");
                    File.Delete(testFile);
                    Debug.Log("✓ Persistent data path is writable");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"✗ Cannot write to persistent data path: {e.Message}");
                }
            }
            else
            {
                Debug.LogError("✗ Persistent data directory does not exist!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"✗ Error checking persistent data path: {e.Message}");
        }
    }

    void CheckAAR()
    {
        Debug.Log("--- AAR Plugin Check ---");
        
        try
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            AndroidJavaObject nativeLib = new AndroidJavaObject("com.example.mynativedetectlib.NativeLib");
            if (nativeLib != null)
            {
                Debug.Log("✓ Native library class found");
                
                // Test the basic string method
                try
                {
                    string testString = nativeLib.Call<string>("stringFromJNI");
                    Debug.Log($"✓ Native library responds: {testString}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"✗ Native library method call failed: {e.Message}");
                }
            }
            else
            {
                Debug.LogError("✗ Failed to create native library instance");
            }
            #else
            Debug.Log("○ AAR check skipped - not on Android device");
            Debug.Log("  To test AAR functionality, build and deploy to Android device");
            #endif
        }
        catch (System.Exception e)
        {
            Debug.LogError($"✗ Error checking AAR: {e.Message}");
            Debug.LogError("  Possible causes:");
            Debug.LogError("  - AAR file not in Plugins/Android folder");
            Debug.LogError("  - Wrong AAR file name or structure");
            Debug.LogError("  - Android build configuration issues");
        }
    }
}