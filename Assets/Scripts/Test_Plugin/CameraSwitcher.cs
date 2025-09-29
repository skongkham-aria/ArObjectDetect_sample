using UnityEngine;
using UnityEngine.UI;

public class CameraSwitcher : MonoBehaviour
{
    public RawImage cameraDisplay; // Assign the RawImage in the Inspector
    public AspectRatioFitter aspectRatioFitter; // Assign the AspectRatioFitter in the Inspector
    private WebCamTexture webCamTexture;
    private WebCamDevice[] devices;
    private int currentCameraIndex = 0;

    void Start()
    {
        devices = WebCamTexture.devices;

        if (devices.Length > 0)
        {
            StartCamera(currentCameraIndex);
        }
        else
        {
            Debug.LogError("No cameras found on this device.");
        }
    }

    public void SwitchCamera()
    {
        if (devices.Length > 1)
        {
            currentCameraIndex = (currentCameraIndex + 1) % devices.Length;
            StartCamera(currentCameraIndex);
        }
        else
        {
            Debug.LogWarning("Only one camera available.");
        }
    }

    private void StartCamera(int cameraIndex)
    {
        if (webCamTexture != null)
        {
            webCamTexture.Stop();
        }

        webCamTexture = new WebCamTexture(devices[cameraIndex].name);
        cameraDisplay.texture = webCamTexture;
        webCamTexture.Play();

        // Adjust the aspect ratio
        aspectRatioFitter.aspectRatio = (float)webCamTexture.width / webCamTexture.height;
    }

    void OnDestroy()
    {
        if (webCamTexture != null)
        {
            webCamTexture.Stop();
        }
    }
}