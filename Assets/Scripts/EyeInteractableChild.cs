using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class EyeInteractableChild : EyeInteractable
{
    private EyeInteractable _parentInteractable;
    private Transform _mainCamera;
    private Vector3 _hoverOffset = new Vector3(0, 0.1f, 0); // Adjust the Y offset as needed
    private RectTransform _rectTransform;
    private BoxCollider _boxCollider;

    private void Start()
    {
        IsHovered = false;
        _lastHoveredState = !IsHovered;
        _parentInteractable = transform.parent.GetComponent<EyeInteractable>();
        _mainCamera = Camera.main.transform; // Get the main camera
        Debug.Log("Parent name: " + _parentInteractable.gameObject.name);

        _rectTransform = GetComponent<RectTransform>();
        _boxCollider = GetComponent<BoxCollider>();
    }

    private void Update()
    {
        // Make the panel face the main camera
        transform.LookAt(_mainCamera);
        transform.Rotate(0, 180, 0); // Adjust rotation to face the camera correctly

        // Hover over the sphere (parent object)
        Vector3 cameraDirection = (Camera.main.transform.position - transform.parent.position ).normalized;
        transform.position = transform.parent.position + cameraDirection * 0.1f + _hoverOffset; // Adjust distance as needed

        if (IsHovered && !_lastHoveredState)
        {
            OnObjectHoverStart?.Invoke(gameObject);
            _parentInteractable.OnObjectHoverStart.Invoke(gameObject);
            _lastHoveredState = true;
            
            // Adjust the BoxCollider size to match the Canvas
            if (_rectTransform != null && _boxCollider != null)
            {
                _boxCollider.size = new Vector3(_rectTransform.rect.width, _rectTransform.rect.height, 0.1f); // Adjust depth as needed
            }
        }
        if (!IsHovered && _lastHoveredState)
        {
            OnObjectHoverEnd?.Invoke(gameObject);
            _parentInteractable.OnObjectHoverEnd.Invoke(gameObject);
            _lastHoveredState = false;
        }
    }
}