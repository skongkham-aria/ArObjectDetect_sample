using UnityEngine;

/// <summary>
/// Simple scene setup for testing the detection plugin
/// Creates a GameObject with the DetectionTester component and sets up the scene
/// </summary>
public class SceneSetup : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreateTestObject()
    {
        // Ensure we have a camera
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject cameraGO = new GameObject("Main Camera");
            mainCamera = cameraGO.AddComponent<Camera>();
            mainCamera.tag = "MainCamera";
            cameraGO.AddComponent<AudioListener>();
            cameraGO.transform.position = new Vector3(0, 1, -10);
            
            Debug.Log("Main camera created");
        }
        
        // Create a GameObject for testing
        GameObject testObject = new GameObject("Detection Tester");
        
        // Add the detection tester component
        DetectionTester tester = testObject.AddComponent<DetectionTester>();
        
        // Configure the tester
        tester.autoStart = true;
        
        // Don't destroy on load so we can see results across scenes
        DontDestroyOnLoad(testObject);
        
        Debug.Log("Detection test object created and configured");
        Debug.Log("UI will be created automatically - check the scene for visual output!");
    }
}