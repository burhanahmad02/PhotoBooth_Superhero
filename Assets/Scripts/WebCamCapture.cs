using UnityEngine;
using UnityEngine.UI;

public class WebcamCapture : MonoBehaviour
{
    WebCamTexture webCamTexture;
    public RawImage displayImage;
    public AspectRatioFitter aspectRatioFitter;

    public Button manButton;
    public Button womanButton;
    public Button captureButton;

    private string selectedGender = "";

    void Start()
    {
        if (displayImage == null)
        {
            Debug.LogError("Assign RawImage to displayImage!");
            return;
        }

        webCamTexture = new WebCamTexture(WebCamTexture.devices[0].name, 1280, 720, 30);
        webCamTexture.Play();
        displayImage.texture = webCamTexture;

        if (aspectRatioFitter != null)
        {
            aspectRatioFitter.aspectRatio = (float)webCamTexture.width / webCamTexture.height;
        }
    }

}
