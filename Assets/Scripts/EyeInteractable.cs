using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class EyeInteractable : MonoBehaviour
{
    public bool IsHovered { get; set; }

    public UnityEvent<GameObject> OnObjectHoverStart; 
    public UnityEvent<GameObject> OnObjectHoverEnd;
    // [SerializeField] private Material OnHoverActiveMaterial;
    // [SerializeField] private Material OnHoverInactiveMaterial;

    private MeshRenderer _meshRenderer;
    public bool _lastHoveredState;
    
    // Start is called before the first frame update
    private void Start()
    {
        IsHovered = false;
        _lastHoveredState = !IsHovered;
        // _meshRenderer = GetComponent<MeshRenderer>();
    }

    // Update is called once per frame
    private void Update()
    {
        if (IsHovered && !_lastHoveredState)
        {
            // if(_meshRenderer != null)
            //     _meshRenderer.material = OnHoverActiveMaterial;
            OnObjectHoverStart?.Invoke(gameObject);
            _lastHoveredState = true;
        }
        if(!IsHovered && _lastHoveredState)
        {
            // if(_meshRenderer != null)
            //     _meshRenderer.material = OnHoverInactiveMaterial;
            OnObjectHoverEnd?.Invoke(gameObject);
            _lastHoveredState = false;
        }
    }
}
