using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
public class RESTGestureManager : MonoBehaviour
{
    public static RESTGestureManager Instance { get; private set; }
    
    public string address = "";

    [Header("UI Elements for Debug")]
    public TextMeshProUGUI[] leftLabels;
    public TextMeshProUGUI[] rightLabels;
    public TextMeshProUGUI[] leftConfidences;
    public TextMeshProUGUI[] rightConfidences;
    
    [Header("Gesture Events")]
    public UnityEvent OnLeftPickStarted;
    public UnityEvent OnLeftPickStopped;
    public UnityEvent OnLeftDestroyStarted;
    public UnityEvent OnLeftDestroyStopped;
    public UnityEvent OnLeftPruneStarted;
    public UnityEvent OnLeftPruneStopped;
    public UnityEvent OnRightPickStarted;
    public UnityEvent OnRightPickStopped;
    public UnityEvent OnRightDestroyStarted;
    public UnityEvent OnRightDestroyStopped; 
    public UnityEvent OnRightPruneStarted;
    public UnityEvent OnRightPruneStopped;
    
    private string _currentLeftGesture = "idle";
    private string _currentRightGesture = "idle";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
    }

    private void Start()
    {
        address = "http://" + PlayerPrefs.GetString("IP") +":5122";
        Debug.Log("RESTGestureManager using: " + "http://" + PlayerPrefs.GetString("IP") +":5122");
    }

    public void SendFeatures(float[] features, bool isLefthand)
    {
        var url = address + "/predict";
        var jsonData = JsonConvert.SerializeObject(features);
        StartCoroutine(PostRequest(url, jsonData, isLefthand));
    }
    
    private IEnumerator PostRequest(string url, string jsonData, bool isLefthand)
    {
        var request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var jsonText = request.downloadHandler.text;
            var prediction = JsonConvert.DeserializeObject<PredictionResponse>(jsonText);
            // Debug.Log("Label: " + prediction.prediction);
            // Debug.Log("Probabilities: " + string.Join(",", prediction.probability));
            // Debug.Log("Labels: " + string.Join(",", prediction.classes));

            for (int i=0; i < prediction.classes.Length; i++)
            {
                // LEFT HAND
                if (isLefthand)
                {
                    if (_currentLeftGesture != prediction.prediction)
                    {
                        switch (_currentLeftGesture)
                        {
                            case "pick":
                                OnLeftPickStopped.Invoke();
                                break;
                            case "destroy":
                                OnLeftDestroyStopped.Invoke();
                                break;
                            case "prune":
                                OnLeftPruneStopped.Invoke();
                                break;
                        }
                        _currentLeftGesture = prediction.prediction;
                        switch (_currentLeftGesture)
                        {
                            case "pick":
                                OnLeftPickStarted.Invoke();
                                break;
                            case "destroy":
                                OnLeftDestroyStarted.Invoke();
                                break;
                            case "prune":
                                OnLeftPruneStarted.Invoke();
                                break;
                        }
                    }
                    // Debug
                    if(leftConfidences != null && leftLabels != null)
                    {
                        leftConfidences[i].text = prediction.probability[i].ToString("0.00");
                        leftLabels[i].text = prediction.classes[i];
                    }
                }
                // RIGHT HAND
                else
                {
                    if (_currentRightGesture != prediction.prediction)
                    {
                        switch (_currentRightGesture)
                        {
                            case "pick":
                                OnRightPickStopped.Invoke();
                                break;
                            case "destroy":
                                OnRightDestroyStopped.Invoke();
                                break;
                            case "prune":
                                OnRightPruneStopped.Invoke();
                                break;
                        }
                        _currentRightGesture = prediction.prediction;
                        switch (_currentRightGesture)
                        {
                            case "pick":
                                OnRightPickStarted.Invoke();
                                break;
                            case "destroy":
                                OnRightDestroyStarted.Invoke();
                                break;
                            case "prune":
                                OnRightPruneStarted.Invoke();
                                break;
                        }
                    }
                    if(rightConfidences != null && rightLabels != null)
                    {
                        rightConfidences[i].text = prediction.probability[i].ToString("0.00");
                        rightLabels[i].text = prediction.classes[i];
                    }
                }
            }
        }
        else
        {
            Debug.LogError("Error: " + request.error);
        }
    }

    public class PredictionResponse
    {
        public string prediction { get; set; }
        public float[] probability { get; set; }
        public string[] classes { get; set; }
    }
    
}
