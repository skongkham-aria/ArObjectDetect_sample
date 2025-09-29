using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ARObjectDetector.NativeDetection;
using System.Collections;

namespace ARObjectDetector
{
    public class ARObjectDetectionManager : MonoBehaviour
    {
        [Header("AR Camera")]
        public Camera arCamera;
        
        [Header("UI Elements")]
        public TextMeshProUGUI statusText;
        public TextMeshProUGUI detectionResultText;
        public Button testConnectionButton;
        public Button startDetectionButton;
        public Button stopDetectionButton;
        
        [Header("Detection Settings")]
        public float detectionInterval = 0.1f; // Detection every 100ms
        public bool autoStartDetection = true;
        public string modelFileName = "yolo11n_float32.tflite"; // Place in StreamingAssets
        
        [Header("Debug")]
        public RawImage debugImageDisplay;
        public bool showDebugImage = false;
        
        private NativeDetectWrapper nativeWrapper;
        private bool isDetecting = false;
        private Coroutine detectionCoroutine;
        private RenderTexture renderTexture;
        private Texture2D captureTexture;
        
        // Events for other AR components to subscribe to
        public System.Action<DetectionResult> OnObjectsDetected;
        
        void Start()
        {
            UpdateStatusText("Starting AR Object Detection...");
            
            // Check camera permissions first
            StartCoroutine(CheckPermissionsAndInitialize());
        }
        
        IEnumerator CheckPermissionsAndInitialize()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // Check for camera permission
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
            {
                UpdateStatusText("Requesting camera permission...");
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);
                
                // Wait for user response
                yield return new WaitUntil(() => UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera));
            }
            
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
            {
                UpdateStatusText("Camera permission denied. AR features will not work.");
                yield break;
            }
            
            UpdateStatusText("Camera permission granted");
#endif
            
            // Wait a moment for AR to initialize
            yield return new WaitForSeconds(1f);
            
            InitializeComponents();
            SetupUI();
            
            if (autoStartDetection)
            {
                StartCoroutine(InitializeAfterDelay(2f));
            }
        }
        
        void InitializeComponents()
        {
            // Get or create the native wrapper
            nativeWrapper = NativeDetectWrapper.Instance;
            
            // Subscribe to events
            nativeWrapper.OnObjectsDetected += HandleObjectsDetected;
            nativeWrapper.OnDetectionError += HandleDetectionError;
            nativeWrapper.OnDetectorInitialized += HandleDetectorInitialized;
            
            // Setup AR camera if not assigned
            if (arCamera == null)
            {
                // Try to find AR Camera first (AR Foundation)
                GameObject xrOrigin = GameObject.Find("XR Origin");
                if (xrOrigin == null)
                {
                    xrOrigin = GameObject.Find("AR Session Origin");
                }
                
                if (xrOrigin != null)
                {
                    arCamera = xrOrigin.GetComponentInChildren<Camera>();
                    Debug.Log("Found AR Camera in XR Origin: " + (arCamera != null ? arCamera.name : "null"));
                }
                
                // Fallback to main camera
                if (arCamera == null)
                {
                    arCamera = Camera.main;
                    if (arCamera == null)
                    {
                        arCamera = FindObjectOfType<Camera>();
                    }
                    Debug.Log("Using fallback camera: " + (arCamera != null ? arCamera.name : "null"));
                }
            }
            
            // Create render texture for capturing camera feed
            if (arCamera != null)
            {
                renderTexture = new RenderTexture(640, 480, 24);
                captureTexture = new Texture2D(640, 480, TextureFormat.RGB24, false);
            }
        }
        
        void SetupUI()
        {
            // Setup buttons
            if (testConnectionButton != null)
                testConnectionButton.onClick.AddListener(TestNativeConnection);
                
            if (startDetectionButton != null)
                startDetectionButton.onClick.AddListener(StartDetection);
                
            if (stopDetectionButton != null)
                stopDetectionButton.onClick.AddListener(StopDetection);
            
            // Initial UI state
            UpdateStatusText("Checking AR session...");
            
            // Monitor AR session status
            StartCoroutine(MonitorARSession());
        }
        
        IEnumerator MonitorARSession()
        {
            int attempts = 0;
            while (attempts < 10)
            {
                // Check if AR camera is active
                if (arCamera != null && arCamera.enabled)
                {
                    UpdateStatusText("AR camera active - Ready for detection");
                    break;
                }
                
                UpdateStatusText($"Waiting for AR session... ({attempts + 1}/10)");
                yield return new WaitForSeconds(1f);
                attempts++;
            }
            
            if (attempts >= 10)
            {
                UpdateStatusText("AR session failed to start. Check device compatibility.");
            }
        }
        
        IEnumerator InitializeAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            InitializeDetector();
        }
        
        void InitializeDetector()
        {
            UpdateStatusText("Initializing detector...");
            
            // Copy model from StreamingAssets to persistent data path
            string modelPath = System.IO.Path.Combine(Application.persistentDataPath, modelFileName);
            
            StartCoroutine(CopyModelFile(modelPath));
        }
        
        IEnumerator CopyModelFile(string targetPath)
        {
            string sourcePath = System.IO.Path.Combine(Application.streamingAssetsPath, modelFileName);
            
            // Check if model already exists
            if (System.IO.File.Exists(targetPath))
            {
                Debug.Log("Model file already exists at: " + targetPath);
                InitializeWithModel(targetPath);
                yield break;
            }
            
            // Copy from StreamingAssets
            if (Application.platform == RuntimePlatform.Android)
            {
                // On Android, StreamingAssets are in APK, use UnityWebRequest
                using (var request = UnityEngine.Networking.UnityWebRequest.Get(sourcePath))
                {
                    yield return request.SendWebRequest();
                    
                    if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                    {
                        System.IO.File.WriteAllBytes(targetPath, request.downloadHandler.data);
                        Debug.Log("Model copied to: " + targetPath);
                        InitializeWithModel(targetPath);
                    }
                    else
                    {
                        Debug.LogError("Failed to load model from StreamingAssets: " + request.error);
                        UpdateStatusText("Error: Model file not found");
                    }
                }
            }
            else
            {
                // On other platforms, direct file copy
                if (System.IO.File.Exists(sourcePath))
                {
                    System.IO.File.Copy(sourcePath, targetPath);
                    Debug.Log("Model copied to: " + targetPath);
                    InitializeWithModel(targetPath);
                }
                else
                {
                    Debug.LogError("Model file not found in StreamingAssets: " + sourcePath);
                    UpdateStatusText("Error: Model file not found in StreamingAssets");
                }
            }
        }
        
        void InitializeWithModel(string modelPath)
        {
            bool initialized = nativeWrapper.InitializeDetector(modelPath);
            if (initialized)
            {
                UpdateStatusText("Detector initialized successfully");
                if (autoStartDetection)
                {
                    StartDetection();
                }
            }
            else
            {
                UpdateStatusText("Failed to initialize detector");
            }
        }
        
        public void TestNativeConnection()
        {
            string result = nativeWrapper.GetTestString();
            UpdateStatusText("Connection test: " + result);
            Debug.Log("Native connection test result: " + result);
        }
        
        public void StartDetection()
        {
            if (isDetecting)
            {
                Debug.Log("Detection already running");
                return;
            }
            
            if (arCamera == null)
            {
                UpdateStatusText("Error: No AR camera assigned");
                return;
            }
            
            isDetecting = true;
            detectionCoroutine = StartCoroutine(DetectionLoop());
            UpdateStatusText("Detection started");
            
            // Update UI
            if (startDetectionButton != null)
                startDetectionButton.interactable = false;
            if (stopDetectionButton != null)
                stopDetectionButton.interactable = true;
        }
        
        public void StopDetection()
        {
            if (!isDetecting)
            {
                Debug.Log("Detection not running");
                return;
            }
            
            isDetecting = false;
            if (detectionCoroutine != null)
            {
                StopCoroutine(detectionCoroutine);
                detectionCoroutine = null;
            }
            
            UpdateStatusText("Detection stopped");
            
            // Update UI
            if (startDetectionButton != null)
                startDetectionButton.interactable = true;
            if (stopDetectionButton != null)
                stopDetectionButton.interactable = false;
        }
        
        IEnumerator DetectionLoop()
        {
            while (isDetecting)
            {
                CaptureAndAnalyze();
                yield return new WaitForSeconds(detectionInterval);
            }
        }
        
        void CaptureAndAnalyze()
        {
            if (arCamera == null || renderTexture == null || captureTexture == null)
                return;
            
            // Capture camera feed
            RenderTexture previousRT = RenderTexture.active;
            arCamera.targetTexture = renderTexture;
            arCamera.Render();
            
            RenderTexture.active = renderTexture;
            captureTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            captureTexture.Apply();
            
            // Restore render targets
            arCamera.targetTexture = null;
            RenderTexture.active = previousRT;
            
            // Show debug image if enabled
            if (showDebugImage && debugImageDisplay != null)
            {
                debugImageDisplay.texture = captureTexture;
            }
            
            // Perform detection
            int detectedCount = nativeWrapper.DetectObjects(captureTexture);
            
            if (detectionResultText != null)
            {
                detectionResultText.text = $"Objects detected: {detectedCount}";
            }
        }
        
        // Event handlers
        void HandleObjectsDetected(DetectionResult result)
        {
            Debug.Log($"Objects detected: {result}");
            OnObjectsDetected?.Invoke(result);
        }
        
        void HandleDetectionError(string error)
        {
            Debug.LogError("Detection error: " + error);
            UpdateStatusText("Detection error: " + error);
        }
        
        void HandleDetectorInitialized(bool success)
        {
            if (success)
            {
                Debug.Log("Native detector initialized successfully");
            }
            else
            {
                Debug.LogError("Failed to initialize native detector");
                UpdateStatusText("Failed to initialize native detector");
            }
        }
        
        void UpdateStatusText(string status)
        {
            if (statusText != null)
            {
                statusText.text = status;
            }
            Debug.Log("Status: " + status);
        }
        
        void OnDestroy()
        {
            // Cleanup
            StopDetection();
            
            if (nativeWrapper != null)
            {
                nativeWrapper.OnObjectsDetected -= HandleObjectsDetected;
                nativeWrapper.OnDetectionError -= HandleDetectionError;
                nativeWrapper.OnDetectorInitialized -= HandleDetectorInitialized;
            }
            
            if (renderTexture != null)
            {
                renderTexture.Release();
                DestroyImmediate(renderTexture);
            }
            
            if (captureTexture != null)
            {
                DestroyImmediate(captureTexture);
            }
        }
        
        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                StopDetection();
            }
        }
    }
}