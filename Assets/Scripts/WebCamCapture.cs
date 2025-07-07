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
        Debug.Log("WebcamCapture script initialized");
    }
}
