using UnityEngine;

/// <summary>
/// Simple scene setup for testing the detection plugin
/// Creates a GameObject with the DetectionTester component
/// </summary>
public class SceneSetup : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreateTestObject()
    {
        // Create a GameObject for testing
        GameObject testObject = new GameObject("Detection Tester");
        
        // Add the detection tester component
        DetectionTester tester = testObject.AddComponent<DetectionTester>();
        
        // Configure the tester
        tester.autoStart = true;
        
        // Don't destroy on load so we can see results across scenes
        DontDestroyOnLoad(testObject);
        
        Debug.Log("Detection test object created and configured");
    }
}