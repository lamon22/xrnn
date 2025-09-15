using UnityEditor;
using UnityEngine;

public enum ToolType
{
    None,
    Node,
    Layer,
    Activation,
    Loss
}

public class ToolItem : MonoBehaviour
{
    public ToolType toolType = ToolType.None;
    public bool allowDuplication = true;
    
    private Transform _hand;
    private bool _isNear;
    private bool _isPicked;

    private Transform _oldParent;

    public static bool IsHandInsideNode { get; private set; } // Static flag to track node priority

    private void OnEnable()
    {
        RESTGestureManager.Instance.OnLeftPickStarted.AddListener(OnPickStarted);
        RESTGestureManager.Instance.OnLeftPickStopped.AddListener(OnPickStopped);
        RESTGestureManager.Instance.OnRightPickStarted.AddListener(OnPickStarted);
        RESTGestureManager.Instance.OnRightPickStopped.AddListener(OnPickStopped);
    }

    private void OnDisable()
    {
        RESTGestureManager.Instance.OnLeftPickStarted.RemoveListener(OnPickStarted);
        RESTGestureManager.Instance.OnLeftPickStopped.RemoveListener(OnPickStopped);
        RESTGestureManager.Instance.OnRightPickStarted.RemoveListener(OnPickStarted);
        RESTGestureManager.Instance.OnRightPickStopped.RemoveListener(OnPickStopped);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("LeftHand"))
        {
            _hand = other.transform;
            _isNear = true;
        }
        IsHandInsideNode = true; // Set the flag when hand enters a node
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
            _oldParent = transform.parent; // Store the old parent
            if (allowDuplication)
            {
                GameObject newObj =  Instantiate(gameObject, transform.position, transform.rotation, transform.parent); // Create a copy of the tool item
                this.allowDuplication = false; // Prevent further duplication
            }
            transform.SetParent(_hand);
        }
    }

    private void OnPickStopped()
    {
        if (_isPicked)
        {
            _isPicked = false;
            transform.SetParent(_oldParent); // Restore the old parent
        }
    }
    
}