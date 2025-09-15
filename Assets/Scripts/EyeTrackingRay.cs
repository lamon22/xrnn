using System;
using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction.GrabAPI;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class EyeTrackingRay : MonoBehaviour
{

    [SerializeField] private float rayDistance = 1f;
    [SerializeField] private float minRayDistance = 0.5f; // Minimum distance for the ray
    [SerializeField] private float rayWidth = 0.01f;
    [SerializeField] private LayerMask layersToInclude;
    [SerializeField] private Color rayColorDefaultState = Color.yellow;
    [SerializeField] private Color rayColorHoverState = Color.red;
    private LineRenderer _lineRenderer;
    private List<EyeInteractable> _eyeInteractables = new List<EyeInteractable>();
    
    // Start is called before the first frame update
    void Start()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        SetupRay();
    }

    void SetupRay()
    {
        _lineRenderer.useWorldSpace = false;
        _lineRenderer.positionCount = 2;
        _lineRenderer.startWidth = rayWidth;
        _lineRenderer.endWidth = rayWidth;
        _lineRenderer.startColor = rayColorDefaultState;
        _lineRenderer.endColor = rayColorDefaultState;
        _lineRenderer.SetPosition(0, transform.position);
        _lineRenderer.SetPosition(1, new Vector3(transform.position.x, transform.position.y, transform.position.z + rayDistance));
    }

    private void FixedUpdate()
    {
        RaycastHit hit;
        Vector3 rayCastDirection = transform.TransformDirection(Vector3.forward) * rayDistance;
        if (Physics.Raycast(transform.position, rayCastDirection, out hit, rayDistance, layersToInclude))
        {
            float hitDistance = hit.distance;
            if (hitDistance >= minRayDistance && hitDistance <= rayDistance)
            {
                Unselect();
                _lineRenderer.startColor = rayColorHoverState;
                _lineRenderer.endColor = rayColorHoverState;
                var eyeInteractable = hit.transform.GetComponent<EyeInteractable>();
                if (eyeInteractable != null)
                {
                    _eyeInteractables.Add(eyeInteractable);
                    eyeInteractable.IsHovered = true;
                }
            }
            else
            {
                _lineRenderer.startColor = rayColorDefaultState;
                _lineRenderer.endColor = rayColorDefaultState;
                Unselect(true);
            }
        }
        else
        {
            _lineRenderer.startColor = rayColorDefaultState;
            _lineRenderer.endColor = rayColorDefaultState;
            Unselect(true);
        }
    }

    private void Unselect(bool clear = false)
    {
        foreach (var interactable in _eyeInteractables)
        {
            interactable.IsHovered = false;
        }
        if (clear)
        {
            _eyeInteractables.Clear();
        }
    }
}
