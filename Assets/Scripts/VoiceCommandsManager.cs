using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Meta.WitAi.Json;
using Meta.WitAi;
using TMPro;

public class VoiceCommandsManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI addNode;
    [SerializeField] private TextMeshProUGUI addLayer;
    [SerializeField] private TextMeshProUGUI deleteNode;
    [SerializeField] private TextMeshProUGUI deleteLayer;
    [SerializeField] private TextMeshProUGUI startInference;

    public NNManager nnManager;

    #region Wit.ai Data Structures

    struct Data
    {
        public int layerIndex;
        public int numberOfNodes;
    }
    
    #endregion

    void Start()
    {
        if (nnManager == null)
        {
            Debug.LogError("NNManager not assigned in the VoiceCommandsManager Inspector!");
        }
    }

    #region Central Parsing Logic

    #endregion

    public void OnIntentAddNode(String[] res)
    {
        Debug.Log("Intent received: add_node");
        PrintArray(res);
        StartCoroutine(UpdateDebugTextColor(addNode));

        Data data;
        List<int> parsedData = NumberParser.ParseNumberWordsArray(res);
        data.layerIndex = parsedData[1];
        data.numberOfNodes = parsedData[0];

        nnManager.AddNodes(data.layerIndex, data.numberOfNodes);
    }

    public void OnIntentAddLayer(String[] res)
    {
        Debug.Log("Intent received: add_layer");
        StartCoroutine(UpdateDebugTextColor(addLayer));

        Data data;
        List<int> parsedData = NumberParser.ParseNumberWordsArray(res);
        data.layerIndex = parsedData[2];
        data.numberOfNodes = parsedData[1];

        for (int i = 0; i < parsedData[0]; i++)
        {
            nnManager.AddLayer(data.layerIndex, data.numberOfNodes);
        }
    }

    public void OnIntentDeleteNode(String[] res)
    {
        Debug.Log("Intent received: delete_node");
        StartCoroutine(UpdateDebugTextColor(deleteNode));

        Data data;
        List<int> parsedData = NumberParser.ParseNumberWordsArray(res);
        data.layerIndex = parsedData[1];
        data.numberOfNodes = parsedData[0];

        nnManager.RemoveNodes(data.layerIndex, data.numberOfNodes);
    }

    public void OnIntentDeleteLayer(String[] res)
    {
        Debug.Log("Intent received: delete_layer");
        StartCoroutine(UpdateDebugTextColor(deleteLayer));

        Data data;
        List<int> parsedData = NumberParser.ParseNumberWordsArray(res);
        data.layerIndex = parsedData[0];

        nnManager.RemoveLayer(data.layerIndex);
    }

    public void OnIntentStartInference(String[] res)
    {
        Debug.Log("Intent received: start_inference");
        StartCoroutine(UpdateDebugTextColor(startInference));

        nnManager.AnimateForwardPass();
    }
    
    IEnumerator UpdateDebugTextColor(TextMeshProUGUI textRef, float time = 1f)
    {
        textRef.color = Color.green;
        yield return new WaitForSeconds(time);
        textRef.color = Color.white;
    }
    
    private void PrintArray(String[] array)
    {
        string result = "Array: ";
        foreach (var item in array)
        {
            result += item + ", ";
        }
        Debug.Log(result.TrimEnd(',', ' '));
    }
}
