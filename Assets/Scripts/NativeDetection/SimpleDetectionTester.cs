using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ARObjectDetector.NativeDetection;

namespace ARObjectDetector
{
    public class SimpleDetectionTester : MonoBehaviour
    {
        [Header("UI Elements")]
        public Button testConnectionButton;
        public Button testDetectionButton;
        public Button cleanupButton;
        public TextMeshProUGUI resultText;
        public TextMeshProUGUI logText;
        
        [Header("Test Settings")]
        public Texture2D testImage;
        public string modelFileName = "your_model.tflite";
        
        private NativeDetectWrapper nativeWrapper;
        private string logMessages = "";
        
        void Start()
        {
            // Get the wrapper instance
            nativeWrapper = NativeDetectWrapper.Instance;
            
            // Subscribe to events
            nativeWrapper.OnObjectsDetected += OnObjectsDetected;
            nativeWrapper.OnDetectionError += OnDetectionError;
            nativeWrapper.OnDetectorInitialized += OnDetectorInitialized;
            
            // Setup UI
            SetupButtons();
            
            LogMessage("Simple Detection Tester initialized");
            UpdateResultText("Ready for testing");
        }
        
        void SetupButtons()
        {
            if (testConnectionButton != null)
                testConnectionButton.onClick.AddListener(TestConnection);
                
            if (testDetectionButton != null)
                testDetectionButton.onClick.AddListener(TestDetection);
                
            if (cleanupButton != null)
                cleanupButton.onClick.AddListener(CleanupResources);
        }
        
        public void TestConnection()
        {
            LogMessage("Testing native connection...");
            string result = nativeWrapper.GetTestString();
            LogMessage($"Connection result: {result}");
            UpdateResultText($"Connection: {result}");
        }
        
        public void TestDetection()
        {
            if (testImage == null)
            {
                LogMessage("ERROR: No test image assigned!");
                UpdateResultText("Error: No test image");
                return;
            }
            
            LogMessage("Starting detection test...");
            
            // First, test image info
            byte[] imageData = testImage.GetRawTextureData();
            int[] imageInfo = nativeWrapper.GetImageInfo(imageData, testImage.width, testImage.height);
            LogMessage($"Image info: [{string.Join(", ", imageInfo)}]");
            
            // Initialize detector with model
            string modelPath = System.IO.Path.Combine(Application.persistentDataPath, modelFileName);
            LogMessage($"Initializing detector with model: {modelPath}");
            
            bool initialized = nativeWrapper.InitializeDetector(modelPath);
            LogMessage($"Detector initialized: {initialized}");
            
            if (!initialized)
            {
                UpdateResultText("Failed to initialize detector");
                return;
            }
            
            // Perform detection
            LogMessage($"Detecting objects in {testImage.width}x{testImage.height} image...");
            int detectedCount = nativeWrapper.DetectObjects(imageData, testImage.width, testImage.height);
            
            string result = $"Detection complete!\nObjects: {detectedCount}\nImage: {testImage.width}x{testImage.height}";
            LogMessage($"Detection result: {detectedCount} objects found");
            UpdateResultText(result);
        }
        
        public void CleanupResources()
        {
            LogMessage("Cleaning up resources...");
            nativeWrapper.Cleanup();
            UpdateResultText("Resources cleaned up");
        }
        
        // Event handlers
        void OnObjectsDetected(DetectionResult result)
        {
            LogMessage($"Objects detected event: {result}");
        }
        
        void OnDetectionError(string error)
        {
            LogMessage($"Detection error: {error}");
            UpdateResultText($"Error: {error}");
        }
        
        void OnDetectorInitialized(bool success)
        {
            LogMessage($"Detector initialization event: {success}");
        }
        
        void LogMessage(string message)
        {
            string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
            string logEntry = $"[{timestamp}] {message}";
            
            Debug.Log(logEntry);
            
            logMessages += logEntry + "\n";
            
            // Keep only last 20 lines
            string[] lines = logMessages.Split('\n');
            if (lines.Length > 20)
            {
                logMessages = string.Join("\n", lines, lines.Length - 20, 20);
            }
            
            if (logText != null)
            {
                logText.text = logMessages;
            }
        }
        
        void UpdateResultText(string text)
        {
            if (resultText != null)
            {
                resultText.text = text;
            }
        }
        
        void OnDestroy()
        {
            // Unsubscribe from events
            if (nativeWrapper != null)
            {
                nativeWrapper.OnObjectsDetected -= OnObjectsDetected;
                nativeWrapper.OnDetectionError -= OnDetectionError;
                nativeWrapper.OnDetectorInitialized -= OnDetectorInitialized;
            }
        }
    }
}