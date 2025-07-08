// ======
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using System.Collections.Generic;

public class WebcamCapture : MonoBehaviour
{
    WebCamTexture webCamTexture;
    public RawImage displayImage;
    public AspectRatioFitter aspectRatioFitter;

    public Button manButton, womanButton, captureButton, proceedButton, retakeButton, homeButton;
    public Button sample1Button, sample2Button, sample3Button, sample4Button;
    private string[] sampleFileNames = { "Sample 1.png", "Sample 2.png", "Sample 3.png", "Sample 4.png" };
    private Dictionary<string, string> genderMap = new Dictionary<string, string>
{
    { "Sample 1.png", "woman" },
    { "Sample 2.png", "woman" },
    { "Sample 3.png", "man" },
    { "Sample 4.png", "man" }
};
    public GameObject genderWarningPopup;
    public TextMeshProUGUI countdownText;
    public CanvasGroup screenFade;
    public GameObject loadingPanel;
    public AudioSource shutterSound;

    public RawImage qrImage;

    private string selectedGender = "";
    public string faceCropURL = "http://localhost:5000/crop_face";
    public string enhanceURL = "http://localhost:5000/upload";
    public string qrBaseURL = "http://localhost:5000/qr_codes/";

    private Texture2D capturedFace;
    private Tween genderLoopTween;

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
        sample1Button.onClick.AddListener(() => StartCoroutine(LoadAndEnhanceFromStreamingAssets("Sample 1.png")));
        sample2Button.onClick.AddListener(() => StartCoroutine(LoadAndEnhanceFromStreamingAssets("Sample 2.png")));
        sample3Button.onClick.AddListener(() => StartCoroutine(LoadAndEnhanceFromStreamingAssets("Sample 3.jpg")));
        sample4Button.onClick.AddListener(() => StartCoroutine(LoadAndEnhanceFromStreamingAssets("Sample 4.png")));

        captureButton.onClick.AddListener(() =>
        {
            if (!string.IsNullOrEmpty(selectedGender))
                StartCoroutine(CountdownAndCapture());
            else
                ShowGenderWarning();
        });

        proceedButton.onClick.AddListener(() => StartCoroutine(SendFaceForEnhancement()));
        retakeButton.onClick.AddListener(() =>
        {
            displayImage.texture = webCamTexture;
            webCamTexture.Play();
            ToggleConfirmationButtons(false);
            ToggleQRDisplay(false);
            screenFade.DOFade(1f, 0.5f);
        });

        homeButton.onClick.AddListener(() =>
        {
            ToggleQRDisplay(false);
            ToggleConfirmationButtons(false);
            screenFade.DOFade(1f, 0.5f);
            webCamTexture.Play();
            displayImage.texture = webCamTexture;
        });

        ToggleConfirmationButtons(false);
        ToggleQRDisplay(false);
        genderWarningPopup.SetActive(false);
        countdownText.gameObject.SetActive(false);
        loadingPanel.SetActive(false);
        screenFade.alpha = 1f;
    }
    IEnumerator LoadAndEnhanceFromStreamingAssets(string fileName)
    {
        // Fade screen to black (same as in Capture flow)
        yield return screenFade.DOFade(0f, 0.5f).WaitForCompletion();

        // Hide initial buttons
        captureButton.gameObject.SetActive(false);
        manButton.gameObject.SetActive(false);
        womanButton.gameObject.SetActive(false);

        // Load image from StreamingAssets
        string filePath = System.IO.Path.Combine(Application.streamingAssetsPath + "/Input Sample Faces", fileName);

#if UNITY_ANDROID && !UNITY_EDITOR
    UnityWebRequest request = UnityWebRequest.Get(filePath);
    yield return request.SendWebRequest();
    byte[] imageData = request.downloadHandler.data;
#else
        byte[] imageData = System.IO.File.ReadAllBytes(filePath);
#endif

        Texture2D loadedTexture = new Texture2D(2, 2);
        loadedTexture.LoadImage(imageData);
        displayImage.texture = loadedTexture;
        capturedFace = loadedTexture;

        // Get gender from map
        if (genderMap.ContainsKey(fileName))
            selectedGender = genderMap[fileName];
        else
            selectedGender = "man";

        // Fade screen back in
        yield return screenFade.DOFade(1f, 0.5f).WaitForCompletion();

        // Show Proceed/Retake
        ToggleConfirmationButtons(true);
    }


    void SelectGender(string gender)
    {
        selectedGender = gender;
        Debug.Log("Selected Gender: " + gender);

        genderLoopTween?.Kill();

        if (gender == "man")
        {
            genderLoopTween = manButton.transform.DOScale(1.1f, 0.5f).SetLoops(-1, LoopType.Yoyo);
            womanButton.transform.DOScale(1f, 0.2f);
        }
        else
        {
            genderLoopTween = womanButton.transform.DOScale(1.1f, 0.5f).SetLoops(-1, LoopType.Yoyo);
            manButton.transform.DOScale(1f, 0.2f);
        }
    }

    void ShowGenderWarning()
    {
        genderWarningPopup.SetActive(true);
        genderWarningPopup.transform.localScale = Vector3.zero;
        genderWarningPopup.transform.DOScale(1f, 0.3f).OnComplete(() =>
        {
            DOVirtual.DelayedCall(2f, () => genderWarningPopup.SetActive(false));
        });
    }

    void ToggleConfirmationButtons(bool show)
    {
        proceedButton.gameObject.SetActive(show);
        retakeButton.gameObject.SetActive(show);
        captureButton.gameObject.SetActive(!show);
        manButton.gameObject.SetActive(!show);
        womanButton.gameObject.SetActive(!show);
    }

    void ToggleQRDisplay(bool show)
    {
        qrImage.gameObject.SetActive(show);
        homeButton.gameObject.SetActive(show);
        captureButton.gameObject.SetActive(!show);
        manButton.gameObject.SetActive(!show);
        womanButton.gameObject.SetActive(!show);
    }
    

    IEnumerator CountdownAndCapture()
    {
        countdownText.gameObject.SetActive(true);

        for (int i = 3; i > 0; i--)
        {
            countdownText.text = i.ToString();
            yield return new WaitForSeconds(1);
        }

        countdownText.gameObject.SetActive(false);
        shutterSound?.Play();

        yield return screenFade.DOFade(0f, 0.5f).WaitForCompletion();

        manButton.gameObject.SetActive(false);
        womanButton.gameObject.SetActive(false);
        captureButton.gameObject.SetActive(false);

        yield return StartCoroutine(CaptureAndCropFace());

        yield return screenFade.DOFade(1f, 0.5f).WaitForCompletion();
        ToggleConfirmationButtons(true);
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
            }
        }
    }

    IEnumerator SendFaceForEnhancement()
    {
        loadingPanel.SetActive(true);

        byte[] faceBytes = capturedFace.EncodeToPNG();

        WWWForm form = new WWWForm();
        form.AddBinaryData("image", faceBytes, "face.png", "image/png");
        form.AddField("gender", selectedGender);

        using (UnityWebRequest www = UnityWebRequest.Post(enhanceURL, form))
        {
            yield return www.SendWebRequest();

            loadingPanel.SetActive(false);

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
                    string qrUrl = qrBaseURL + response.qr_code_filename;

                    StartCoroutine(LoadEnhancedImage(imageUrl));
                    StartCoroutine(LoadQRImage(qrUrl));
                }
                else
                {
                    Debug.LogError("Enhancement failed: " + response.message);
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

    IEnumerator LoadQRImage(string url)
    {
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            Texture2D qrTexture = DownloadHandlerTexture.GetContent(www);
            qrImage.texture = qrTexture;
            ToggleQRDisplay(true);
        }
        else
        {
            Debug.LogError("Failed to load QR code image.");
        }
    }

    [Serializable]
    public class ImageUploadResponse
    {
        public string status;
        public string message;
        public string original_filename;
        public string enhanced_filename;
        public string qr_code_filename;
    }
}
