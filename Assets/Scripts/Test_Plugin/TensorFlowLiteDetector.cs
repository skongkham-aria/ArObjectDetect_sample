using System;
using UnityEngine;

namespace DetectionPlugin
{
    /// <summary>
    /// Unity C# wrapper for the Android TensorFlow Lite detection library
    /// </summary>
    public class TensorFlowLiteDetector
    {
        private AndroidJavaObject nativeLibInstance;
        private bool isInitialized = false;

        private const string JAVA_CLASS_NAME = "com.example.mynativedetectlib.NativeLib";

        public TensorFlowLiteDetector()
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                // Create instance of the native library
                nativeLibInstance = new AndroidJavaObject(JAVA_CLASS_NAME);
                Debug.Log("TensorFlowLiteDetector: Native library instance created successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"TensorFlowLiteDetector: Failed to create native library instance: {e.Message}");
                nativeLibInstance = null;
            }
            #else
            Debug.LogWarning("TensorFlowLiteDetector: This plugin only works on Android devices");
            #endif
        }

        /// <summary>
        /// Initialize the TensorFlow Lite model
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
                
                bool result = nativeLibInstance.Call<bool>("initializeDetector", modelPath);
                
                if (result)
                {
                    isInitialized = true;
                    Debug.Log("TensorFlowLiteDetector: ✓ Initialization successful");
                }
                else
                {
                    Debug.LogError("TensorFlowLiteDetector: ✗ Initialization failed - Native method returned false");
                    Debug.LogError("TensorFlowLiteDetector: Check Android logcat for detailed native error messages");
                    Debug.LogError("TensorFlowLiteDetector: Possible causes:");
                    Debug.LogError("TensorFlowLiteDetector: - Invalid model file format");
                    Debug.LogError("TensorFlowLiteDetector: - Insufficient device memory");
                    Debug.LogError("TensorFlowLiteDetector: - Model incompatible with TensorFlow Lite version");
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
        /// <returns>Number of detected objects, negative value indicates error</returns>
        public int DetectObjects(byte[] imageData, int width, int height)
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            if (!isInitialized || nativeLibInstance == null)
            {
                Debug.LogError("TensorFlowLiteDetector: Detector not initialized");
                return -1;
            }

            try
            {
                Debug.Log($"TensorFlowLiteDetector: Running detection on {width}x{height} image ({imageData.Length} bytes)");
                
                int result = nativeLibInstance.Call<int>("detectObjects", imageData, width, height);
                
                Debug.Log($"TensorFlowLiteDetector: Detection completed, found {result} objects");
                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"TensorFlowLiteDetector: Exception during detection: {e.Message}");
                return -2;
            }
            #else
            Debug.LogWarning("TensorFlowLiteDetector: DetectObjects - This plugin only works on Android devices");
            return -1;
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
        /// Check if the detector is initialized
        /// </summary>
        /// <returns>True if initialized, false otherwise</returns>
        public bool IsInitialized()
        {
            return isInitialized;
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
                    // If there's a cleanup method in the native library, call it
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
    }
}