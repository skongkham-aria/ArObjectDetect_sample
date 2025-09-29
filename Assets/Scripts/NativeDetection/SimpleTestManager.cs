using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ARObjectDetector.NativeDetection;

namespace ARObjectDetector
{
    public class SimpleTestManager : MonoBehaviour
    {
        [Header("UI Elements")]
        public TextMeshProUGUI statusText;
        public Button testConnectionButton;
        public Button testDetectionButton;
        
        [Header("Test Settings")]
        public Texture2D testImage;
        
        private NativeDetectWrapper nativeWrapper;
        
        void Start()
        {
            // Get the wrapper instance
            nativeWrapper = NativeDetectWrapper.Instance;
            
            // Subscribe to events
            nativeWrapper.OnDetectionError += HandleDetectionError;
            nativeWrapper.OnDetectorInitialized += HandleDetectorInitialized;
            
            // Setup UI
            SetupButtons();
            
            UpdateStatusText("Simple Test Manager Ready");
        }
        
        void SetupButtons()
        {
            if (testConnectionButton != null)
                testConnectionButton.onClick.AddListener(TestNativeConnection);
                
            if (testDetectionButton != null)
                testDetectionButton.onClick.AddListener(TestDetectionWithImage);
        }
        
        public void TestNativeConnection()
        {
            UpdateStatusText("Testing native connection...");
            string result = nativeWrapper.GetTestString();
            UpdateStatusText($"Native result: {result}");
            Debug.Log("Native connection test: " + result);
        }
        
        public void TestDetectionWithImage()
        {
            if (testImage == null)
            {
                UpdateStatusText("No test image assigned!");
                return;
            }
            
            UpdateStatusText("Testing object detection...");
            
            try
            {
                // Test with a simple texture
                byte[] imageData = testImage.GetRawTextureData();
                int width = testImage.width;
                int height = testImage.height;
                
                UpdateStatusText($"Processing {width}x{height} image...");
                
                // Get image info
                int[] imageInfo = nativeWrapper.GetImageInfo(imageData, width, height);
                Debug.Log($"Image info: [{string.Join(", ", imageInfo)}]");
                
                // Try detection
                int detectedCount = nativeWrapper.DetectObjects(imageData, width, height);
                
                UpdateStatusText($"Detection complete! Found {detectedCount} objects");
                Debug.Log($"Detection result: {detectedCount} objects");
            }
            catch (System.Exception e)
            {
                UpdateStatusText($"Detection failed: {e.Message}");
                Debug.LogError("Detection error: " + e.Message);
            }
        }
        
        void HandleDetectionError(string error)
        {
            UpdateStatusText($"Error: {error}");
            Debug.LogError("Detection error: " + error);
        }
        
        void HandleDetectorInitialized(bool success)
        {
            string message = success ? "Native detector ready" : "Native detector failed";
            UpdateStatusText(message);
            Debug.Log("Detector initialized: " + success);
        }
        
        void UpdateStatusText(string text)
        {
            if (statusText != null)
            {
                statusText.text = text;
            }
            Debug.Log("Status: " + text);
        }
        
        void OnDestroy()
        {
            // Cleanup
            if (nativeWrapper != null)
            {
                nativeWrapper.OnDetectionError -= HandleDetectionError;
                nativeWrapper.OnDetectorInitialized -= HandleDetectorInitialized;
            }
        }
    }
}