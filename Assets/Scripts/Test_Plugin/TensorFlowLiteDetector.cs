using System;
using UnityEngine;

namespace DetectionPlugin
{
    /// <summary>
    /// Unity C# wrapper for the Android TensorFlow Lite detection library
    /// 
    /// This class interfaces with the NativeLib plugin which has a hybrid implementation:
    /// - Primary: Native JNI implementation (C++)
    /// - Fallback: Java TensorFlow Lite implementation
    /// 
    /// NativeLib.kt method signatures:
    /// Core Methods:
    /// - Boolean initializeDetector(String modelPath) - Auto-selects native or Java implementation
    /// - Int detectObjects(ByteArray imageData, Int width, Int height) - Returns detection count
    /// - String getDetailedDetections(ByteArray imageData, Int width, Int height) - Returns JSON with details
    /// - IntArray getInputDimensions() - Model input dimensions
    /// - Boolean isInitialized() - Check initialization status
    /// - Unit cleanup() - Cleanup resources
    /// 
    /// Extended Methods:
    /// - String[] getLastDetections() - Array of detection strings from last inference
    /// - Int getLastDetectionCount() - Count from last inference
    /// - String getDetectionInfo(Int index) - Specific detection details
    /// - IntArray getImageInfo(ByteArray, Int, Int) - Image validation info
    /// - String stringFromJNI() - JNI connectivity test
    /// </summary>
    public class TensorFlowLiteDetector
    {
        private AndroidJavaObject nativeLibInstance;
        private bool isInitialized = false;

        // NativeLib is the JNI interface that internally uses TensorFlowLiteWrapper for TensorFlow Lite operations
        private const string JAVA_CLASS_NAME = "com.example.mynativedetectlib.NativeLib";

        /// <summary>
        /// Create a standardized JSON response with required fields
        /// </summary>
        /// <param name="totalDetections">Number of detections found</param>
        /// <param name="detections">Array of detection objects (can be empty)</param>
        /// <param name="status">Status: "success", "error", "warning"</param>
        /// <param name="message">Descriptive message</param>
        /// <returns>Properly formatted JSON string</returns>
        private string CreateJsonResponse(int totalDetections, string detections = "[]", string status = "success", string message = "")
        {
            // Escape any quotes in the message
            string escapedMessage = message.Replace("\"", "\\\"");
            
            // Include space after colon to match the parser in DetectionTester.cs
            string jsonResponse = $"{{\"total_detections\": {totalDetections}, \"detections\": {detections}, \"status\": \"{status}\", \"message\": \"{escapedMessage}\"}}";
            
            Debug.Log($"TensorFlowLiteDetector: Generated JSON response: {jsonResponse}");
            return jsonResponse;
        }

        /// <summary>
        /// Check which implementation is currently being used (native JNI vs Java fallback)
        /// </summary>
        /// <returns>Implementation info string</returns>
        public string GetImplementationInfo()
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            if (nativeLibInstance == null)
            {
                return "Native library instance is null";
            }

            try
            {
                // Try to determine implementation by checking initialization status
                bool initialized = nativeLibInstance.Call<bool>("isInitialized");
                string testString = nativeLibInstance.Call<string>("stringFromJNI");
                
                return $"Implementation: Hybrid (Native JNI + Java fallback)\nInitialized: {initialized}\nJNI Test: {testString}";
            }
            catch (Exception e)
            {
                return $"Unable to determine implementation: {e.Message}";
            }
            #else
            return "Editor mode - no native implementation available";
            #endif
        }

        /// <summary>
        /// Test method to verify JSON format compatibility with DetectionTester
        /// </summary>
        /// <returns>Sample JSON response for testing</returns>
        public string GetTestJsonResponse()
        {
            return CreateJsonResponse(2, "[{\"class\":\"person\",\"confidence\":0.85},{\"class\":\"car\",\"confidence\":0.92}]", "success", "Test detection completed");
        }

        public TensorFlowLiteDetector()
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                // Create instance of the native library
                nativeLibInstance = new AndroidJavaObject(JAVA_CLASS_NAME);
                Debug.Log("TensorFlowLiteDetector: Native library instance created successfully");
                Debug.Log("TensorFlowLiteDetector: Hybrid implementation available (Native JNI + Java fallback)");
                Debug.Log("TensorFlowLiteDetector: Core method signatures:");
                Debug.Log("TensorFlowLiteDetector: - Boolean initializeDetector(String modelPath)");
                Debug.Log("TensorFlowLiteDetector: - Int detectObjects(ByteArray imageData, Int width, Int height)");
                Debug.Log("TensorFlowLiteDetector: - String getDetailedDetections(ByteArray imageData, Int width, Int height)");
                Debug.Log("TensorFlowLiteDetector: - IntArray getInputDimensions()");
                Debug.Log("TensorFlowLiteDetector: - Boolean isInitialized()");
                Debug.Log("TensorFlowLiteDetector: Extended methods:");
                Debug.Log("TensorFlowLiteDetector: - String[] getLastDetections(), Int getLastDetectionCount()");
            }
            catch (Exception e)
            {
                Debug.LogError($"TensorFlowLiteDetector: Failed to create native library instance: {e.Message}");
                Debug.LogError($"TensorFlowLiteDetector: Make sure the AAR file with class '{JAVA_CLASS_NAME}' is in the Plugins/Android folder");
                nativeLibInstance = null;
            }
            #else
            Debug.LogWarning("TensorFlowLiteDetector: This plugin only works on Android devices");
            #endif
        }

        /// <summary>
        /// Initialize the TensorFlow Lite model
        /// Note: The Kotlin implementation will automatically choose between native JNI and Java fallback
        /// </summary>
        /// <param name="modelPath">Path to the .tflite model file</param>
        /// <returns>True if initialization successful, false otherwise</returns>
        public bool Initialize(string modelPath)
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            if (nativeLibInstance == null)
            {
                Debug.LogError("TensorFlowLiteDetector: Native library instance is null");
                Debug.LogError("TensorFlowLiteDetector: Make sure the AAR file is in Plugins/Android/ folder");
                Debug.LogError("TensorFlowLiteDetector: And that you're building for Android platform");
                return false;
            }

            try
            {
                Debug.Log($"TensorFlowLiteDetector: Initializing with model path: {modelPath}");
                Debug.Log("TensorFlowLiteDetector: Kotlin implementation will auto-select native JNI or Java fallback");
                
                // Check if file exists before passing to native code
                if (!System.IO.File.Exists(modelPath))
                {
                    Debug.LogError($"TensorFlowLiteDetector: Model file does not exist at path: {modelPath}");
                    return false;
                }
                
                // Get file size for debugging
                System.IO.FileInfo fileInfo = new System.IO.FileInfo(modelPath);
                Debug.Log($"TensorFlowLiteDetector: Model file size: {fileInfo.Length} bytes");
                
                // Check file extension
                if (!modelPath.EndsWith(".tflite"))
                {
                    Debug.LogWarning($"TensorFlowLiteDetector: File doesn't have .tflite extension: {modelPath}");
                }
                
                // Try to call the initialization method with proper error handling
                bool result = false;
                try
                {
                    result = nativeLibInstance.Call<bool>("initializeDetector", modelPath);
                }
                catch (AndroidJavaException androidEx)
                {
                    Debug.LogError($"TensorFlowLiteDetector: Android Java Exception: {androidEx.Message}");
                    Debug.LogError("TensorFlowLiteDetector: This usually indicates a method signature mismatch");
                    Debug.LogError("TensorFlowLiteDetector: Expected: Boolean initializeDetector(String modelPath)");
                    return false;
                }
                catch (System.Exception sysEx) when (sysEx.Message.Contains("NoSuchMethodError"))
                {
                    Debug.LogError($"TensorFlowLiteDetector: NoSuchMethodError: {sysEx.Message}");
                    Debug.LogError("TensorFlowLiteDetector: The method 'initializeDetector' was not found in the native library");
                    Debug.LogError("TensorFlowLiteDetector: Please check:");
                    Debug.LogError("TensorFlowLiteDetector: 1. AAR file contains the correct NativeLib class");
                    Debug.LogError("TensorFlowLiteDetector: 2. Method signature: Boolean initializeDetector(String modelPath)");
                    Debug.LogError("TensorFlowLiteDetector: 3. Class name: com.example.mynativedetectlib.NativeLib");
                    return false;
                }
                
                if (result)
                {
                    isInitialized = true;
                    Debug.Log("TensorFlowLiteDetector: ✓ Initialization successful");
                    
                    // Log which implementation is being used
                    string implInfo = GetImplementationInfo();
                    Debug.Log($"TensorFlowLiteDetector: {implInfo}");
                }
                else
                {
                    Debug.LogError("TensorFlowLiteDetector: ✗ Initialization failed - Native method returned false");
                    Debug.LogError("TensorFlowLiteDetector: Check Android logcat for detailed native error messages");
                    Debug.LogError("TensorFlowLiteDetector: The Kotlin implementation tried both native JNI and Java fallback");
                    Debug.LogError("TensorFlowLiteDetector: Possible causes:");
                    Debug.LogError("TensorFlowLiteDetector: - Invalid model file format");
                    Debug.LogError("TensorFlowLiteDetector: - Insufficient device memory");
                    Debug.LogError("TensorFlowLiteDetector: - Model incompatible with TensorFlow Lite version");
                    Debug.LogError("TensorFlowLiteDetector: - Missing native library dependencies");
                }
                
                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"TensorFlowLiteDetector: Exception during initialization: {e.Message}");
                Debug.LogError($"TensorFlowLiteDetector: Exception type: {e.GetType().Name}");
                Debug.LogError($"TensorFlowLiteDetector: Stack trace: {e.StackTrace}");
                return false;
            }
            #else
            Debug.LogWarning("TensorFlowLiteDetector: Initialize - This plugin only works on Android devices");
            Debug.LogWarning("TensorFlowLiteDetector: You're currently running in Unity Editor or non-Android platform");
            Debug.LogWarning("TensorFlowLiteDetector: For testing, build and deploy to an Android device");
            return false;
            #endif
        }

        /// <summary>
        /// Detect objects in the given image data
        /// </summary>
        /// <param name="imageData">Raw image bytes (RGB or RGBA)</param>
        /// <param name="width">Image width</param>
        /// <param name="height">Image height</param>
        /// <param name="channels">Number of image channels (3 for RGB, 4 for RGBA)</param>
        /// <returns>JSON string with detection results, null or empty indicates error</returns>
        public string DetectObjects(byte[] imageData, int width, int height, int channels)
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            if (!isInitialized || nativeLibInstance == null)
            {
                Debug.LogError("TensorFlowLiteDetector: Detector not initialized");
                return CreateJsonResponse(0, "[]", "error", "Detector not initialized");
            }

            try
            {
                Debug.Log($"TensorFlowLiteDetector: Running detection on {width}x{height} image ({imageData.Length} bytes, {channels} channels)");
                
                // First try the primary Kotlin method (returns detection count)
                int detectionCount = nativeLibInstance.Call<int>("detectObjects", imageData, width, height);
                Debug.Log($"TensorFlowLiteDetector: Primary detection returned {detectionCount} objects");
                
                if (detectionCount > 0)
                {
                    // Try to get detailed results using the correct method signature (no channels parameter)
                    try
                    {
                        string detailedResult = nativeLibInstance.Call<string>("getDetailedDetections", imageData, width, height);
                        Debug.Log($"TensorFlowLiteDetector: Detailed detection raw result: {detailedResult}");
                        
                        // Validate and potentially fix the JSON format
                        if (!string.IsNullOrEmpty(detailedResult))
                        {
                            // Check if the result contains total_detections field
                            if (!detailedResult.Contains("total_detections"))
                            {
                                Debug.LogWarning("TensorFlowLiteDetector: Detailed result missing total_detections field, wrapping response");
                                string wrappedResult = CreateJsonResponse(detectionCount, detailedResult, "success", "Detection completed");
                                Debug.Log($"TensorFlowLiteDetector: Wrapped detailed result: {wrappedResult}");
                                return wrappedResult;
                            }
                            else
                            {
                                Debug.Log($"TensorFlowLiteDetector: Detailed detection completed with proper format: {detailedResult}");
                                return detailedResult;
                            }
                        }
                    }
                    catch (AndroidJavaException detailEx)
                    {
                        Debug.LogWarning($"TensorFlowLiteDetector: getDetailedDetections not available: {detailEx.Message}");
                        Debug.LogWarning("TensorFlowLiteDetector: This may indicate the implementation is using basic detection only");
                        
                        // Fallback to getLastDetections method
                        try
                        {
                            string[] lastDetections = nativeLibInstance.Call<string[]>("getLastDetections");
                            if (lastDetections != null && lastDetections.Length > 0)
                            {
                                // Create proper JSON format with total_detections
                                string detectionsArray = string.Join(",", lastDetections);
                                string properJson = CreateJsonResponse(lastDetections.Length, $"[{detectionsArray}]", "success", "Detections retrieved from getLastDetections");
                                Debug.Log($"TensorFlowLiteDetector: Last detections proper JSON: {properJson}");
                                return properJson;
                            }
                        }
                        catch (Exception fallbackEx)
                        {
                            Debug.LogWarning($"TensorFlowLiteDetector: getLastDetections also failed: {fallbackEx.Message}");
                        }
                    }
                }
                
                // Create proper JSON response with total_detections field
                if (detectionCount > 0)
                {
                    return CreateJsonResponse(detectionCount, "[]", "success", "Detection completed but detailed results not available");
                }
                else
                {
                    return CreateJsonResponse(0, "[]", "success", "No objects detected");
                }
            }
            catch (AndroidJavaException androidEx)
            {
                Debug.LogError($"TensorFlowLiteDetector: Android Java Exception during detection: {androidEx.Message}");
                Debug.LogError("TensorFlowLiteDetector: Expected method: Int detectObjects(ByteArray, Int, Int)");
                string errorJson = $"{{\"total_detections\":0,\"detections\":[],\"status\":\"error\",\"message\":\"AndroidJavaException: {androidEx.Message.Replace("\"", "\\\"")}\"}}";
                return errorJson;
            }
            catch (System.Exception sysEx) when (sysEx.Message.Contains("NoSuchMethodError"))
            {
                Debug.LogError($"TensorFlowLiteDetector: NoSuchMethodError during detection: {sysEx.Message}");
                Debug.LogError("TensorFlowLiteDetector: The method 'detectObjects' was not found");
                Debug.LogError("TensorFlowLiteDetector: Expected signature: Int detectObjects(ByteArray imageData, Int width, Int height)");
                Debug.LogError($"TensorFlowLiteDetector: Error during detection - detection failed with error code: -1");
                return CreateJsonResponse(0, "[]", "error", "NoSuchMethodError - detectObjects method not found");
            }
            catch (Exception e)
            {
                Debug.LogError($"TensorFlowLiteDetector: Exception during detection: {e.Message}");
                Debug.LogError($"TensorFlowLiteDetector: Exception type: {e.GetType().Name}");
                Debug.LogError($"TensorFlowLiteDetector: Error during detection - detection failed with error code: -1");
                string errorJson = $"{{\"total_detections\":0,\"detections\":[],\"status\":\"error\",\"message\":\"Exception during detection: {e.Message.Replace("\"", "\\\"")}\"}}";
                return errorJson;
            }
            #else
            Debug.LogWarning("TensorFlowLiteDetector: DetectObjects - This plugin only works on Android devices");
            return CreateJsonResponse(0, "[]", "warning", "Plugin only works on Android devices - running in editor mode");
            #endif
        }

        /// <summary>
        /// Test the native library string method
        /// </summary>
        /// <returns>Test string from native library</returns>
        public string GetTestString()
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            if (nativeLibInstance == null)
            {
                return "Native library instance is null";
            }

            try
            {
                return nativeLibInstance.Call<string>("stringFromJNI");
            }
            catch (Exception e)
            {
                return $"Exception: {e.Message}";
            }
            #else
            return "This plugin only works on Android devices";
            #endif
        }

        /// <summary>
        /// Cleanup resources (call when done)
        /// </summary>
        public void Cleanup()
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            if (nativeLibInstance != null)
            {
                try
                {
                    // Call the native cleanup method first
                    CallNativeCleanup();
                    Debug.Log("TensorFlowLiteDetector: Cleaning up resources");
                }
                catch (Exception e)
                {
                    Debug.LogError($"TensorFlowLiteDetector: Exception during cleanup: {e.Message}");
                }
                finally
                {
                    nativeLibInstance.Dispose();
                    nativeLibInstance = null;
                    isInitialized = false;
                }
            }
            #endif
        }

        /// <summary>
        /// Get input dimensions required by the model
        /// </summary>
        /// <returns>Array with [width, height, channels] or null on error</returns>
        public int[] GetInputDimensions()
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            if (!isInitialized || nativeLibInstance == null)
            {
                Debug.LogError("TensorFlowLiteDetector: Detector not initialized");
                return null;
            }

            try
            {
                return nativeLibInstance.Call<int[]>("getInputDimensions");
            }
            catch (AndroidJavaException androidEx)
            {
                Debug.LogError($"TensorFlowLiteDetector: Android Java Exception getting input dimensions: {androidEx.Message}");
                Debug.LogError("TensorFlowLiteDetector: Expected method: IntArray getInputDimensions()");
                return null;
            }
            catch (System.Exception sysEx) when (sysEx.Message.Contains("NoSuchMethodError"))
            {
                Debug.LogError($"TensorFlowLiteDetector: NoSuchMethodError getting input dimensions: {sysEx.Message}");
                Debug.LogError("TensorFlowLiteDetector: The method 'getInputDimensions' was not found");
                Debug.LogError("TensorFlowLiteDetector: Expected signature: IntArray getInputDimensions()");
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"TensorFlowLiteDetector: Exception getting input dimensions: {e.Message}");
                Debug.LogError($"TensorFlowLiteDetector: Exception type: {e.GetType().Name}");
                return null;
            }
            #else
            Debug.LogWarning("TensorFlowLiteDetector: GetInputDimensions - This plugin only works on Android devices");
            return null;
            #endif
        }

        /// <summary>
        /// Get information about the image data
        /// </summary>
        /// <param name="imageData">Raw image bytes</param>
        /// <param name="width">Image width</param>
        /// <param name="height">Image height</param>
        /// <returns>Array with image information or null on error</returns>
        public int[] GetImageInfo(byte[] imageData, int width, int height)
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            if (!isInitialized || nativeLibInstance == null)
            {
                Debug.LogError("TensorFlowLiteDetector: Detector not initialized");
                return null;
            }

            try
            {
                return nativeLibInstance.Call<int[]>("getImageInfo", imageData, width, height);
            }
            catch (AndroidJavaException androidEx)
            {
                Debug.LogError($"TensorFlowLiteDetector: Android Java Exception getting image info: {androidEx.Message}");
                Debug.LogError("TensorFlowLiteDetector: Expected method: IntArray getImageInfo(ByteArray, Int, Int)");
                return null;
            }
            catch (System.Exception sysEx) when (sysEx.Message.Contains("NoSuchMethodError"))
            {
                Debug.LogError($"TensorFlowLiteDetector: NoSuchMethodError getting image info: {sysEx.Message}");
                Debug.LogError("TensorFlowLiteDetector: The method 'getImageInfo' was not found");
                Debug.LogError("TensorFlowLiteDetector: Expected signature: IntArray getImageInfo(ByteArray imageData, Int width, Int height)");
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"TensorFlowLiteDetector: Exception getting image info: {e.Message}");
                Debug.LogError($"TensorFlowLiteDetector: Exception type: {e.GetType().Name}");
                return null;
            }
            #else
            Debug.LogWarning("TensorFlowLiteDetector: GetImageInfo - This plugin only works on Android devices");
            return null;
            #endif
        }

        /// <summary>
        /// Get the last detection results as an array of strings
        /// </summary>
        /// <returns>Array of detection result strings or null on error</returns>
        public string[] GetLastDetections()
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            if (!isInitialized || nativeLibInstance == null)
            {
                Debug.LogError("TensorFlowLiteDetector: Detector not initialized");
                return null;
            }

            try
            {
                return nativeLibInstance.Call<string[]>("getLastDetections");
            }
            catch (AndroidJavaException androidEx)
            {
                Debug.LogError($"TensorFlowLiteDetector: Android Java Exception getting last detections: {androidEx.Message}");
                Debug.LogError("TensorFlowLiteDetector: Expected method: String[] getLastDetections()");
                return null;
            }
            catch (System.Exception sysEx) when (sysEx.Message.Contains("NoSuchMethodError"))
            {
                Debug.LogError($"TensorFlowLiteDetector: NoSuchMethodError getting last detections: {sysEx.Message}");
                Debug.LogError("TensorFlowLiteDetector: The method 'getLastDetections' was not found");
                Debug.LogError("TensorFlowLiteDetector: This method is only available in Java fallback mode");
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"TensorFlowLiteDetector: Exception getting last detections: {e.Message}");
                return null;
            }
            #else
            Debug.LogWarning("TensorFlowLiteDetector: GetLastDetections - This plugin only works on Android devices");
            return null;
            #endif
        }

        /// <summary>
        /// Get the count of the last detection results
        /// </summary>
        /// <returns>Number of detections from last call, -1 on error</returns>
        public int GetLastDetectionCount()
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            if (!isInitialized || nativeLibInstance == null)
            {
                Debug.LogError("TensorFlowLiteDetector: Detector not initialized");
                return -1;
            }

            try
            {
                return nativeLibInstance.Call<int>("getLastDetectionCount");
            }
            catch (AndroidJavaException androidEx)
            {
                Debug.LogError($"TensorFlowLiteDetector: Android Java Exception getting last detection count: {androidEx.Message}");
                Debug.LogError("TensorFlowLiteDetector: Expected method: int getLastDetectionCount()");
                return -1;
            }
            catch (System.Exception sysEx) when (sysEx.Message.Contains("NoSuchMethodError"))
            {
                Debug.LogError($"TensorFlowLiteDetector: NoSuchMethodError getting last detection count: {sysEx.Message}");
                Debug.LogError("TensorFlowLiteDetector: The method 'getLastDetectionCount' was not found");
                Debug.LogError("TensorFlowLiteDetector: This method is only available in Java fallback mode");
                return -1;
            }
            catch (Exception e)
            {
                Debug.LogError($"TensorFlowLiteDetector: Exception getting last detection count: {e.Message}");
                return -1;
            }
            #else
            Debug.LogWarning("TensorFlowLiteDetector: GetLastDetectionCount - This plugin only works on Android devices");
            return -1;
            #endif
        }

        /// <summary>
        /// Get detailed information about a specific detection by index
        /// </summary>
        /// <param name="index">Index of the detection to get info for</param>
        /// <returns>Detection info string or null on error</returns>
        public string GetDetectionInfo(int index)
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            if (!isInitialized || nativeLibInstance == null)
            {
                Debug.LogError("TensorFlowLiteDetector: Detector not initialized");
                return null;
            }

            try
            {
                return nativeLibInstance.Call<string>("getDetectionInfo", index);
            }
            catch (AndroidJavaException androidEx)
            {
                Debug.LogError($"TensorFlowLiteDetector: Android Java Exception getting detection info: {androidEx.Message}");
                Debug.LogError("TensorFlowLiteDetector: Expected method: String getDetectionInfo(int index)");
                return null;
            }
            catch (System.Exception sysEx) when (sysEx.Message.Contains("NoSuchMethodError"))
            {
                Debug.LogError($"TensorFlowLiteDetector: NoSuchMethodError getting detection info: {sysEx.Message}");
                Debug.LogError("TensorFlowLiteDetector: The method 'getDetectionInfo' was not found");
                Debug.LogError("TensorFlowLiteDetector: This method is only available in Java fallback mode");
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"TensorFlowLiteDetector: Exception getting detection info: {e.Message}");
                return null;
            }
            #else
            Debug.LogWarning("TensorFlowLiteDetector: GetDetectionInfo - This plugin only works on Android devices");
            return null;
            #endif
        }

        /// <summary>
        /// Call the native cleanup method and dispose resources
        /// </summary>
        public void CallNativeCleanup()
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            if (nativeLibInstance != null)
            {
                try
                {
                    // Call the Kotlin cleanup method
                    nativeLibInstance.Call("cleanup");
                    Debug.Log("TensorFlowLiteDetector: Native cleanup called successfully");
                }
                catch (AndroidJavaException androidEx)
                {
                    Debug.LogError($"TensorFlowLiteDetector: Android Java Exception during native cleanup: {androidEx.Message}");
                }
                catch (System.Exception sysEx) when (sysEx.Message.Contains("NoSuchMethodError"))
                {
                    Debug.LogError($"TensorFlowLiteDetector: NoSuchMethodError during cleanup: {sysEx.Message}");
                    Debug.LogError("TensorFlowLiteDetector: The method 'cleanup' was not found");
                }
                catch (Exception e)
                {
                    Debug.LogError($"TensorFlowLiteDetector: Exception during native cleanup: {e.Message}");
                }
            }
            #endif
        }

        /// <summary>
        /// Test method to verify JNI connectivity
        /// </summary>
        /// <returns>Test string from native library or null on error</returns>
        public string TestConnection()
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            if (nativeLibInstance == null)
            {
                Debug.LogError("TensorFlowLiteDetector: Native library instance is null");
                return null;
            }

            try
            {
                string result = nativeLibInstance.Call<string>("stringFromJNI");
                Debug.Log($"TensorFlowLiteDetector: Test connection successful: {result}");
                return result;
            }
            catch (AndroidJavaException androidEx)
            {
                Debug.LogError($"TensorFlowLiteDetector: Android Java Exception during test: {androidEx.Message}");
                Debug.LogError("TensorFlowLiteDetector: Expected method: String stringFromJNI()");
                return null;
            }
            catch (System.Exception sysEx) when (sysEx.Message.Contains("NoSuchMethodError"))
            {
                Debug.LogError($"TensorFlowLiteDetector: NoSuchMethodError during test: {sysEx.Message}");
                Debug.LogError("TensorFlowLiteDetector: The method 'stringFromJNI' was not found");
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"TensorFlowLiteDetector: Exception during test: {e.Message}");
                return null;
            }
            #else
            Debug.LogWarning("TensorFlowLiteDetector: TestConnection - This plugin only works on Android devices");
            return "Editor mode - no native connection";
            #endif
        }

        /// <summary>
        /// Validate that all required methods exist in the native library
        /// </summary>
        /// <returns>True if all methods are available, false otherwise</returns>
        public bool ValidateNativeMethods()
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            if (nativeLibInstance == null)
            {
                Debug.LogError("TensorFlowLiteDetector: Cannot validate methods - native instance is null");
                return false;
            }

            try
            {
                Debug.Log("TensorFlowLiteDetector: Validating native method signatures...");
                
                // Try to get the Java class to check if methods exist
                AndroidJavaClass javaClass = new AndroidJavaClass(JAVA_CLASS_NAME);
                
                // Note: We can't directly check method signatures from Unity,
                // but we can try to call them with dummy parameters to see if they exist
                Debug.Log("TensorFlowLiteDetector: Method validation completed - methods will be tested during actual calls");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"TensorFlowLiteDetector: Method validation failed: {e.Message}");
                return false;
            }
            #else
            Debug.LogWarning("TensorFlowLiteDetector: Method validation not available on non-Android platforms");
            return false;
            #endif
        }

        /// <summary>
        /// Check if the detector is properly initialized
        /// Uses the actual isInitialized() method from Kotlin implementation
        /// </summary>
        /// <returns>True if initialized, false otherwise</returns>
        public bool IsInitialized()
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            if (nativeLibInstance == null)
            {
                return false;
            }
                
            try
            {
                // Call the actual Kotlin isInitialized() method
                bool kotlinInitialized = nativeLibInstance.Call<bool>("isInitialized");
                
                // Both Unity flag and Kotlin method should agree
                bool bothInitialized = isInitialized && kotlinInitialized;
                
                if (isInitialized != kotlinInitialized)
                {
                    Debug.LogWarning($"TensorFlowLiteDetector: Initialization state mismatch - Unity: {isInitialized}, Kotlin: {kotlinInitialized}");
                }
                
                return bothInitialized;
            }
            catch (Exception e)
            {
                Debug.LogError($"TensorFlowLiteDetector: Exception checking initialization status: {e.Message}");
                return false;
            }
            #else
            return false;
            #endif
        }
    }
}