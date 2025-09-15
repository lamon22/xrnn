using System;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EditorAttributes;

[RequireComponent(typeof(EyeInteractable))]
public class EyeTooltip : MonoBehaviour
{
    [SerializeField] private float hoverTimeRequired = 1f;
    [SerializeField] private float unhoverDelay = 1f;
    [SerializeField] private GameObject toolTip;
    
    private float _hoverTimer;
    private float _unhoverTimer;
    // private Transform _currentTarget;
    private bool _isHovered;

    private void Awake()
    {
        _hoverTimer = 0f;
        _unhoverTimer = 0f;
        // _currentTarget = null;
        _isHovered = false;
        toolTip.SetActive(false);

        var eyeInteractable = GetComponent<EyeInteractable>();
        eyeInteractable.OnObjectHoverStart.AddListener(OnHoverStart);
        eyeInteractable.OnObjectHoverEnd.AddListener(OnHoverEnd);
    }

    private void OnHoverStart(GameObject go)
    {
        Debug.Log("OnHoverStart");
        _isHovered = true;
        _unhoverTimer = 0f;
    }

    private void OnHoverEnd(GameObject go)
    {
        Debug.Log("OnHoverEnd");
        _isHovered = false;
        _hoverTimer = 0f;
    }

    private void Update()
    {
        if (_isHovered)
        {
            _hoverTimer += Time.deltaTime;
            if (_hoverTimer >= hoverTimeRequired && !toolTip.activeSelf)
            {
                ShowTooltip();
            }
        }
        else
        {
            if (toolTip && toolTip.activeSelf)
            {
                _unhoverTimer += Time.deltaTime;
                if (_unhoverTimer >= unhoverDelay)
                {
                    toolTip.SetActive(false);
                }
            }
        }
    }

    [Button]
    private void ShowTooltip()
    {
        NodeInfo nodeInfo = GetComponent<NodeInfo>();
        string text;
        if (nodeInfo == null)
        {
            Debug.LogWarning("NodeInfo component not found on the GameObject.");
            text = "No NodeInfo available.";
        }
        else
        {
            NodeData nodeData = NNManager.Instance.DisplayNodeInfo(nodeInfo);
            text = NNManager.Instance.PrintNodeDetails(nodeData);
        }

        toolTip.GetComponentInChildren<TextMeshProUGUI>().text = text;

        //MoveTooltipUpAndRotate(nodePosition);
        
        toolTip.SetActive(true);
        
        // float distance = Vector3.Distance(toolTip.transform.position, Camera.main.transform.position);
        // float scaleFactor = 0.02f * distance;
        // toolTip.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
    }

    public void MoveTooltipUpAndRotate(Vector3 nodePosition)
    {
        // Calcola la posizione con un offset verso l'alto e verso l'utente
        Vector3 cameraDirection = (Camera.main.transform.position - nodePosition).normalized;
        Vector3 offset = cameraDirection * -1f; // Aggiungi un offset verso l'utente  Vector3.up * 0.1f 
        toolTip.transform.position = nodePosition + offset;

        // Ruota la UI verso la fotocamera
        toolTip.transform.LookAt(Camera.main.transform);
        toolTip.transform.Rotate(0, 180, 0);
    }
}