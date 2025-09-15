using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;
using System.Collections.Generic;
using EditorAttributes;

public class RESTManager : MonoBehaviour
{
    public string serverAddress = "http://127.0.0.1:5001";
    
    private void Awake()
    {
        serverAddress = "http://" + PlayerPrefs.GetString("IP", "127.0.0.1") + ":5001";
        Debug.Log("RESTManager using: " + serverAddress);
    }

    [Button]
    public void SetToLocalIPl()
    {
        serverAddress = "http://127.0.0.1:5001";
        PlayerPrefs.SetString("IP", "127.0.0.1");
    }

    public void PostRequest(string endpoint, string jsonBody, Action<bool, string> onComplete)
    {
        StartCoroutine(PostRequestCoroutine(endpoint, jsonBody, onComplete));
    }

    public void GetRequest(string endpoint, Action<bool, string> onComplete)
    {
        StartCoroutine(GetRequestCoroutine(endpoint, onComplete));
    }

    private IEnumerator PostRequestCoroutine(string endpoint, string jsonBody, Action<bool, string> onComplete)
    {
        string url = serverAddress + endpoint;
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                onComplete?.Invoke(true, request.downloadHandler.text);
            }
            else
            {
                onComplete?.Invoke(false, request.error);
            }
        }
    }

    private IEnumerator GetRequestCoroutine(string endpoint, Action<bool, string> onComplete)
    {
        string url = serverAddress + endpoint;
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                onComplete?.Invoke(true, request.downloadHandler.text);
            }
            else
            {
                onComplete?.Invoke(false, request.error);
            }
        }
    }
}

[System.Serializable]
public class ServerResponse
{
    public string status;
    public string message;
    public ModelArchitecture architecture;
}

[System.Serializable]
public class ModelArchitecture
{
    public int input_nodes;
    public int[] hidden_layer_sizes;
    public int output_nodes;
    public string[] activations; 
}

[System.Serializable]
public class ForwardPassResponse
{
    public string status;
    public System.Collections.Generic.List<System.Collections.Generic.List<float>> activations;
}

[System.Serializable]
public class NodeDetailsResponse
{
    public string status;
    public string message;
    public List<float> weights;
    public float bias;
}