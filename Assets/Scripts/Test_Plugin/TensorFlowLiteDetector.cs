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
                return false;
            }

            try
            {
                Debug.Log($"TensorFlowLiteDetector: Initializing with model path: {modelPath}");
                
                bool result = nativeLibInstance.Call<bool>("initializeDetector", modelPath);
                
                if (result)
                {
                    isInitialized = true;
                    Debug.Log("TensorFlowLiteDetector: Initialization successful");
                }
                else
                {
                    Debug.LogError("TensorFlowLiteDetector: Initialization failed");
                }
                
                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"TensorFlowLiteDetector: Exception during initialization: {e.Message}");
                return false;
            }
            #else
            Debug.LogWarning("TensorFlowLiteDetector: Initialize - This plugin only works on Android devices");
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