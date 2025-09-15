using EditorAttributes;
using UnityEngine;
using UnityEngine.Events;

public class NodeInteraction : MonoBehaviour
{
    private Transform _hand;
    private bool _isNear;
    private bool _isPicked;
    public static bool IsHandInsideNode { get; private set; } // Static flag to track node priority

    private Transform _oldParent;


    private void OnEnable()
    {
        RESTGestureManager.Instance.OnLeftPickStarted.AddListener(OnPickStarted);
        RESTGestureManager.Instance.OnLeftPickStopped.AddListener(OnPickStopped);
        RESTGestureManager.Instance.OnRightPickStarted.AddListener(OnPickStarted);
        RESTGestureManager.Instance.OnRightPickStopped.AddListener(OnPickStopped);
        RESTGestureManager.Instance.OnLeftDestroyStarted.AddListener(OnDestroyStarted);
        RESTGestureManager.Instance.OnRightDestroyStarted.AddListener(OnDestroyStarted);
        RESTGestureManager.Instance.OnLeftPruneStarted.AddListener(OnPruneStarted);
        RESTGestureManager.Instance.OnRightPruneStarted.AddListener(OnPruneStarted);
    }

    private void OnDisable()
    {
        RESTGestureManager.Instance.OnLeftPickStarted.RemoveListener(OnPickStarted);
        RESTGestureManager.Instance.OnLeftPickStopped.RemoveListener(OnPickStopped);
        RESTGestureManager.Instance.OnRightPickStarted.RemoveListener(OnPickStarted);
        RESTGestureManager.Instance.OnRightPickStopped.RemoveListener(OnPickStopped);
        RESTGestureManager.Instance.OnLeftDestroyStarted.RemoveListener(OnDestroyStarted);
        RESTGestureManager.Instance.OnRightDestroyStarted.RemoveListener(OnDestroyStarted);
        RESTGestureManager.Instance.OnLeftPruneStarted.RemoveListener(OnPruneStarted);
        RESTGestureManager.Instance.OnRightPruneStarted.RemoveListener(OnPruneStarted);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("LeftHand"))
        {
            _hand = other.transform;
            _isNear = true;
            IsHandInsideNode = true; // Set the flag when hand enters a node
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("LeftHand"))
        {
            _isNear = false;
            if (!_isPicked)
            {
                _hand = null;
            }
            IsHandInsideNode = false; // Clear the flag when hand exits a node
        }
    }

    private void OnPickStarted()
    {
        if (_isNear && !_isPicked)
        {
            _isPicked = true;
            _oldParent = transform.parent;
            transform.SetParent(_hand);
        }
    }

    private void OnPickStopped()
    {
        if (_isPicked)
        {
            _isPicked = false;
            transform.SetParent(_oldParent);
            NNManager.Instance.modelVisualizer.UpdateLayerBox(_oldParent.gameObject);
        }
    }

    private void OnPruneStarted()
    {
        Debug.Log("PRUNE STARTED");
        if(_isNear)
        {
            NodeInfo nodeInfo = GetComponent<NodeInfo>();
            NNManager.Instance.RemoveNodes(nodeInfo.data.layerIndex,1);
        }
    }

    [Button]
    private void OnDestroyStarted()
    {
        Debug.Log("DESTROY STARTED");
        if (_isNear)
        {
            NodeInfo nodeInfo = GetComponent<NodeInfo>();
            NNManager.Instance.RemoveLayer(nodeInfo.data.layerIndex);
        }
    }
}