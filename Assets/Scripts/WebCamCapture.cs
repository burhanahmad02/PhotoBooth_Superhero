using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
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
    public string pythonServerURL = "http://localhost:5000/upload";

    public EnhancedImageLoader imageLoader;

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
    void ProcessServerResponse(string jsonResponse)
    {
        try
        {
            ImageUploadResponse response = JsonUtility.FromJson<ImageUploadResponse>(jsonResponse);

            if (response != null && response.status == "success")
            {
                Debug.Log($"Image enhanced successfully: {response.enhanced_filename}");
                if (imageLoader != null)
                {
                    imageLoader.LoadNewImage(response.enhanced_filename);
                }
            }
            else
            {
                Debug.LogError("Invalid server response");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to parse server response: {e.Message}");
        }
    }

    [System.Serializable]
    public class ImageUploadResponse
    {
        public string status;
        public string message;
        public string original_filename;
        public string enhanced_filename;
    }
    IEnumerator CaptureAndSend()
    {
        yield return new WaitForEndOfFrame();

        Texture2D texture = new Texture2D(webCamTexture.width, webCamTexture.height);
        texture.SetPixels(webCamTexture.GetPixels());
        texture.Apply();

        byte[] bytes = texture.EncodeToPNG();

        WWWForm form = new WWWForm();
        form.AddBinaryData("image", bytes, "webcam.png", "image/png");
        form.AddField("gender", selectedGender);

        using (UnityWebRequest www = UnityWebRequest.Post(pythonServerURL, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error sending image: " + www.error);
            }
            else
            {
                Debug.Log("Image sent successfully!");
                Debug.Log("Server response: " + www.downloadHandler.text);
                ProcessServerResponse(www.downloadHandler.text);

            }
        }

        selectedGender = "";
    }



}
