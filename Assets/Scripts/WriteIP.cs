using System;
using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction;
using UnityEngine;
using TMPro;

public class WriteIP : MonoBehaviour
{
    public TextMeshProUGUI ipAddress;

    private void Start()
    {
        ipAddress.text = PlayerPrefs.GetString("IP", "");
        
        GameObject[] keys = GameObject.FindGameObjectsWithTag("keyButton");
        foreach (GameObject g in keys)
        {
            string s = g.GetComponentInChildren<TextMeshProUGUI>().text;
            g.GetComponent<InteractableUnityEventWrapper>().WhenSelect.AddListener(() => AddValueToIP(s));
        }
    }

    public void SaveIP()
    {
        PlayerPrefs.SetString("IP", ipAddress. text);
        Debug.Log("IP address saved: " + ipAddress.text);
    }
    
    public void AddValueToIP(string value)
    {
        if(value == "DEL")
        {
            if (ipAddress.text.Length > 0)
            {
                ipAddress.text = ipAddress.text.Substring(0, ipAddress.text.Length - 1);
            }
            SaveIP();
            return;
        }
        ipAddress.text += value;
        SaveIP();
    }

    public void Reset()
    {
        ipAddress.text = "";
    }
}
