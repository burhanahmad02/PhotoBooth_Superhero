// ============================
// Unity Script: WebcamCapture.cs
// ============================

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

    public Button manButton, womanButton, captureButton, proceedButton, retakeButton;

    private string selectedGender = "";
    public string faceCropURL = "http://localhost:5000/crop_face";
    public string enhanceURL = "http://localhost:5000/upload";

    private Texture2D capturedFace;

    void Start()
    {
        if (displayImage == null) return;

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
                StartCoroutine(CaptureAndCropFace());
            else
                Debug.LogWarning("Please select a gender.");
        });

        proceedButton.onClick.AddListener(() => StartCoroutine(SendFaceForEnhancement()));
        retakeButton.onClick.AddListener(() =>
        {
            displayImage.texture = webCamTexture;
            webCamTexture.Play();
            ToggleConfirmationButtons(false);
        });

        ToggleConfirmationButtons(false);
    }

    void SelectGender(string gender)
    {
        selectedGender = gender;
        Debug.Log("Selected Gender: " + gender);
    }

    void ToggleConfirmationButtons(bool show)
    {
        proceedButton.gameObject.SetActive(show);
        retakeButton.gameObject.SetActive(show);
        captureButton.gameObject.SetActive(!show);
    }

    IEnumerator CaptureAndCropFace()
    {
        yield return new WaitForEndOfFrame();

        Texture2D snap = new Texture2D(webCamTexture.width, webCamTexture.height);
        snap.SetPixels(webCamTexture.GetPixels());
        snap.Apply();

        byte[] bytes = snap.EncodeToPNG();

        WWWForm form = new WWWForm();
        form.AddBinaryData("image", bytes, "webcam.png", "image/png");

        using (UnityWebRequest www = UnityWebRequest.Post(faceCropURL, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Face crop failed: " + www.error);
            }
            else
            {
                capturedFace = new Texture2D(2, 2);
                capturedFace.LoadImage(www.downloadHandler.data);
                displayImage.texture = capturedFace;
                webCamTexture.Stop();

                ToggleConfirmationButtons(true);
            }
        }
    }

    IEnumerator SendFaceForEnhancement()
    {
        byte[] faceBytes = capturedFace.EncodeToPNG();

        WWWForm form = new WWWForm();
        form.AddBinaryData("image", faceBytes, "face.png", "image/png");
        form.AddField("gender", selectedGender);

        using (UnityWebRequest www = UnityWebRequest.Post(enhanceURL, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Enhancement failed: " + www.error);
            }
            else
            {
                Debug.Log("Enhancement succeeded.");
                ImageUploadResponse response = JsonUtility.FromJson<ImageUploadResponse>(www.downloadHandler.text);
                if (response.status == "success")
                {
                    string imageUrl = "http://localhost:5000/enhanced_images/" + response.enhanced_filename;
                    StartCoroutine(LoadEnhancedImage(imageUrl));
                }
            }
        }

        ToggleConfirmationButtons(false);
        selectedGender = "";
    }

    IEnumerator LoadEnhancedImage(string url)
    {
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            Texture2D enhancedTexture = DownloadHandlerTexture.GetContent(www);
            displayImage.texture = enhancedTexture;
        }
        else
        {
            Debug.LogError("Failed to load enhanced image.");
        }
    }

    [Serializable]
    public class ImageUploadResponse
    {
        public string status;
        public string message;
        public string original_filename;
        public string enhanced_filename;
    }
}
