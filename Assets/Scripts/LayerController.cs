using EditorAttributes;
using UnityEngine;

public class LayerController : MonoBehaviour
{
    public LayerData data;
    public Renderer visualBoxRenderer;
    public Material defaultMaterial;
    public Material selectedMaterial;
    public bool isPositionCustom = false;

    private Transform _hand;
    private bool _isNear;
    private bool _isPicked;
    
    private Transform _oldParent;


    private void OnEnable()
    {
        RESTGestureManager.Instance.OnLeftPickStarted.AddListener(OnPickStarted);
        RESTGestureManager.Instance.OnLeftPickStopped.AddListener(OnPickStopped);
        RESTGestureManager.Instance.OnRightPickStarted.AddListener(OnPickStarted);
        RESTGestureManager.Instance.OnRightPickStopped.AddListener(OnPickStopped);
        RESTGestureManager.Instance.OnLeftDestroyStarted.AddListener(OnDestroyStarted);
        RESTGestureManager.Instance.OnRightDestroyStarted.AddListener(OnDestroyStarted);
    }

    private void OnDisable()
    {
        RESTGestureManager.Instance.OnLeftPickStarted.RemoveListener(OnPickStarted);
        RESTGestureManager.Instance.OnLeftPickStopped.RemoveListener(OnPickStopped);
        RESTGestureManager.Instance.OnRightPickStarted.RemoveListener(OnPickStarted);
        RESTGestureManager.Instance.OnRightPickStopped.RemoveListener(OnPickStopped);
        RESTGestureManager.Instance.OnLeftDestroyStarted.RemoveListener(OnDestroyStarted);
        RESTGestureManager.Instance.OnRightDestroyStarted.RemoveListener(OnDestroyStarted);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("LeftHand") && !NodeInteraction.IsHandInsideNode) // Check node priority
        {
            _hand = other.transform;
            _isNear = true;
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
        }
    }

    private void OnPickStarted()
    {
        if (_isNear && !_isPicked && !NodeInteraction.IsHandInsideNode) // Check node priority
        {
            _isPicked = true;
            _oldParent = transform.parent; // Store the old parent
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

    [Button]
    private void OnDestroyStarted()
    {
        Debug.Log("DESTROY STARTED");
        if (_isNear)
        {
            NNManager.Instance.RemoveLayer(data.layerIndex);
        }
    }
}