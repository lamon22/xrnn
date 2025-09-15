using UnityEngine;
using System.Collections;

[RequireComponent(typeof(LineRenderer))]
public class NNNeuronConnectionRenderer : MonoBehaviour
{
    public Transform startNode;
    public Transform endNode;

    private LineRenderer lineRenderer;
    private Color originalColor;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        originalColor = lineRenderer.startColor;
    }

    void LateUpdate()
    {
        if (startNode == null || endNode == null)
        {
            Destroy(gameObject);
            return;
        }

        lineRenderer.SetPosition(0, startNode.position);
        lineRenderer.SetPosition(1, endNode.position);
    }

    public IEnumerator AnimatePulse(Color pulseColor, float duration)
    {
        if (duration <= 0) yield break;

        float halfDuration = duration / 2f;
        float elapsed = 0f;

        while (elapsed < halfDuration)
        {
            float t = elapsed / halfDuration; 
            Color currentColor = Color.Lerp(originalColor, pulseColor, t);
            
            lineRenderer.startColor = currentColor;
            lineRenderer.endColor = currentColor;
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        lineRenderer.startColor = pulseColor;
        lineRenderer.endColor = pulseColor;

        elapsed = 0f;

        while (elapsed < halfDuration)
        {
            float t = elapsed / halfDuration;
            Color currentColor = Color.Lerp(pulseColor, originalColor, t);

            lineRenderer.startColor = currentColor;
            lineRenderer.endColor = currentColor;

            elapsed += Time.deltaTime;
            yield return null;
        }

        lineRenderer.startColor = originalColor;
        lineRenderer.endColor = originalColor;
    }
}