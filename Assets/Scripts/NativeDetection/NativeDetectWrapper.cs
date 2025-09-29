using System;
using UnityEngine;

namespace ARObjectDetector.NativeDetection
{
    [Serializable]
    public class DetectedObject
    {
        public int classId;
        public float score;
        public float x;
        public float y;
        public float width;
        public float height;
        
        public override string ToString()
        {
            return $"Object[Class:{classId}, Score:{score:F2}, Bounds:({x:F1},{y:F1},{width:F1},{height:F1})]";
        }
    }

    [Serializable]
    public class DetectionResult
    {
        public DetectedObject[] objects;
        public int count;
        
        public override string ToString()
        {
            return $"DetectionResult[Count:{count}, Objects:{objects?.Length ?? 0}]";
        }
    }

    public class NativeDetectWrapper : MonoBehaviour
    {
        [Header("Debug Settings")]
        public bool enableDebugLogs = true;
        
        private AndroidJavaObject nativeLib;
        private AndroidJavaClass unityPlayerClass;
        private AndroidJavaObject currentActivity;
        
        private static NativeDetectWrapper instance;
        public static NativeDetectWrapper Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("NativeDetectWrapper");
                    instance = go.AddComponent<NativeDetectWrapper>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        // Events for AR integration
        public event System.Action<DetectionResult> OnObjectsDetected;
        public event System.Action<string> OnDetectionError;
        public event System.Action<bool> OnDetectorInitialized;

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializePlugin();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void InitializePlugin()
        {
            try
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                // Get Unity Player and current activity
                unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                currentActivity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");
                
                // Create instance of your NativeLib
                nativeLib = new AndroidJavaObject("com.example.mynativedetectlib.NativeLib");
                
                if (enableDebugLogs)
                    Debug.Log("Native detection library initialized successfully");
                    
                OnDetectorInitialized?.Invoke(true);
#else
                if (enableDebugLogs)
                    Debug.Log("Native detection library only works on Android devices");
                OnDetectorInitialized?.Invoke(false);
#endif
            }
            catch (System.Exception e)
            {
                string errorMsg = "Failed to initialize native library: " + e.Message;
                if (enableDebugLogs)
                    Debug.LogError(errorMsg);
                OnDetectionError?.Invoke(errorMsg);
                OnDetectorInitialized?.Invoke(false);
            }
        }

        /// <summary>
        /// Initialize the object detector with a model file
        /// </summary>
        /// <param name="modelPath">Path to the ML model file</param>
        /// <returns>True if initialization was successful</returns>
        public bool InitializeDetector(string modelPath)
        {
            try
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                if (nativeLib != null)
                {
                    bool result = nativeLib.Call<bool>("initializeDetector", modelPath);
                    if (enableDebugLogs)
                        Debug.Log($"Detector initialization result: {result}");
                    return result;
                }
#endif
            }
            catch (System.Exception e)
            {
                string errorMsg = "Failed to initialize detector: " + e.Message;
                if (enableDebugLogs)
                    Debug.LogError(errorMsg);
                OnDetectionError?.Invoke(errorMsg);
            }
            return false;
        }

        /// <summary>
        /// Detect objects in the provided image data
        /// </summary>
        /// <param name="imageData">Raw image byte array</param>
        /// <param name="width">Image width</param>
        /// <param name="height">Image height</param>
        /// <returns>Number of detected objects</returns>
        public int DetectObjects(byte[] imageData, int width, int height)
        {
            try
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                if (nativeLib != null)
                {
                    int detectedCount = nativeLib.Call<int>("detectObjects", imageData, width, height);
                    
                    if (enableDebugLogs)
                        Debug.Log($"Detected {detectedCount} objects in {width}x{height} image");
                    
                    // Create a simple detection result for now
                    DetectionResult result = new DetectionResult
                    {
                        count = detectedCount,
                        objects = new DetectedObject[0] // Your native library would need to return actual object data
                    };
                    
                    OnObjectsDetected?.Invoke(result);
                    return detectedCount;
                }
#endif
            }
            catch (System.Exception e)
            {
                string errorMsg = "Failed to detect objects: " + e.Message;
                if (enableDebugLogs)
                    Debug.LogError(errorMsg);
                OnDetectionError?.Invoke(errorMsg);
            }
            return 0;
        }

        /// <summary>
        /// Detect objects in a Unity Texture2D
        /// </summary>
        /// <param name="texture">Input texture</param>
        /// <returns>Number of detected objects</returns>
        public int DetectObjects(Texture2D texture)
        {
            if (texture == null)
            {
                if (enableDebugLogs)
                    Debug.LogError("Input texture is null");
                return 0;
            }

            // Convert texture to byte array
            byte[] imageData = texture.GetRawTextureData();
            return DetectObjects(imageData, texture.width, texture.height);
        }

        /// <summary>
        /// Get detailed information about an image for validation
        /// </summary>
        /// <param name="imageData">Raw image data</param>
        /// <param name="width">Image width</param>
        /// <param name="height">Image height</param>
        /// <returns>Image information array [dataSize, channels, isValid]</returns>
        public int[] GetImageInfo(byte[] imageData, int width, int height)
        {
            try
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                if (nativeLib != null)
                {
                    int[] info = nativeLib.Call<int[]>("getImageInfo", imageData, width, height);
                    if (enableDebugLogs && info != null)
                        Debug.Log($"Image info: Size={info[0]}, Channels={info[1]}, Valid={info[2]}");
                    return info ?? new int[0];
                }
#endif
            }
            catch (System.Exception e)
            {
                string errorMsg = "Failed to get image info: " + e.Message;
                if (enableDebugLogs)
                    Debug.LogError(errorMsg);
                OnDetectionError?.Invoke(errorMsg);
            }
            return new int[0];
        }

        /// <summary>
        /// Test the native library connection
        /// </summary>
        /// <returns>Test string from native library</returns>
        public string GetTestString()
        {
            try
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                if (nativeLib != null)
                {
                    string result = nativeLib.Call<string>("stringFromJNI");
                    if (enableDebugLogs)
                        Debug.Log("Native test string: " + result);
                    return result;
                }
#endif
            }
            catch (System.Exception e)
            {
                string errorMsg = "Failed to get test string: " + e.Message;
                if (enableDebugLogs)
                    Debug.LogError(errorMsg);
                OnDetectionError?.Invoke(errorMsg);
            }
            return "Native library not available";
        }

        /// <summary>
        /// Clean up native resources
        /// </summary>
        public void Cleanup()
        {
            try
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                if (nativeLib != null)
                {
                    nativeLib.Call("cleanup");
                    if (enableDebugLogs)
                        Debug.Log("Native resources cleaned up");
                }
#endif
            }
            catch (System.Exception e)
            {
                string errorMsg = "Failed to cleanup: " + e.Message;
                if (enableDebugLogs)
                    Debug.LogError(errorMsg);
                OnDetectionError?.Invoke(errorMsg);
            }
        }

        // Unity lifecycle events
        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                Cleanup();
            }
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                Cleanup();
            }
        }

        void OnDestroy()
        {
            Cleanup();
        }
    }
}