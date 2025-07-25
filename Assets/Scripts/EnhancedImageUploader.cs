using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class EnhancedImageLoader : MonoBehaviour
{
    public GameObject imageItemPrefab;
    public Transform content;
    public ScrollRect scrollRect;
    public string imageListAPI = "http://localhost:5000/enhanced_images/list";
    public string baseImageURL = "http://localhost:5000/enhanced_images/";
    private List<string> loadedImages = new List<string>();

    void Start()
    {
        StartCoroutine(LoadExistingImages());
    }

    public void LoadNewImage(string imageName)
    {
        if (!loadedImages.Contains(imageName))
        {
            Debug.Log($"Loading new image: {imageName}");
            StartCoroutine(DownloadAndAddImage(imageName));
            StartCoroutine(ScrollToNewImage());
        }
    }

    IEnumerator LoadExistingImages()
    {
        UnityWebRequest www = UnityWebRequest.Get(imageListAPI);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error fetching image list: " + www.error);
        }
        else
        {
            string[] images = JsonHelper.GetJsonArray<string>(www.downloadHandler.text);
            foreach (string imageName in images)
            {
                if (!loadedImages.Contains(imageName))
                {
                    StartCoroutine(DownloadAndAddImage(imageName));
                }
            }
        }
    }

    IEnumerator DownloadAndAddImage(string imageName)
    {
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(baseImageURL + imageName);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error downloading image: " + www.error);
        }
        else
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(www);
            GameObject newItem = Instantiate(imageItemPrefab, content);
            newItem.GetComponent<RawImage>().texture = texture;
            loadedImages.Add(imageName);
            Canvas.ForceUpdateCanvases();
        }
    }

    IEnumerator ScrollToNewImage()
    {
        yield return new WaitForEndOfFrame();
        scrollRect.horizontalNormalizedPosition = 0f;
    }
}
public static class JsonHelper
{
    public static T[] GetJsonArray<T>(string json)
    {
        string newJson = "{\"array\":" + json + "}";
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
        return wrapper.array;
    }

    [System.Serializable]
    private class Wrapper<T>
    {
        public T[] array;
    }
}
