using System.Collections;
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
        manButton.onClick.AddListener(() => SelectGender("man"));
        womanButton.onClick.AddListener(() => SelectGender("woman"));

        captureButton.onClick.AddListener(() =>
        {
            if (!string.IsNullOrEmpty(selectedGender))
                StartCoroutine(WaitAndCapture(3f));
            else
                Debug.LogWarning("Please select a gender before taking a picture.");
        });
    }
    void SelectGender(string gender)
    {
        selectedGender = gender;
        Debug.Log($"Gender selected: {gender}");
    }

    IEnumerator WaitAndCapture(float delaySeconds)
    {
        Debug.Log("Capture initiated. Waiting " + delaySeconds + " seconds...");
        yield return new WaitForSeconds(delaySeconds);
        yield return StartCoroutine(CaptureAndSend());
    }

    IEnumerator CaptureAndSend()
    {
        yield return new WaitForEndOfFrame();

        Texture2D texture = new Texture2D(webCamTexture.width, webCamTexture.height);
        texture.SetPixels(webCamTexture.GetPixels());
        texture.Apply();

        byte[] bytes = texture.EncodeToPNG();

        // Log to confirm
        Debug.Log($"Captured image size: {bytes.Length} bytes");
    }


}
