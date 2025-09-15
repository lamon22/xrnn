using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class ONNXModelVisualizer : MonoBehaviour
{
    public GameObject nodePrefab;
    public GameObject layerBoxPrefab;
    public LineRenderer connectionPrefab;
    
    public Transform layerContainer;
    public Transform connectionContainer;

    public float layerSpacing = 5.0f;
    public float nodeSpacing = 1.5f;
    public Vector3 boxPadding = new Vector3(1.0f, 1.0f, 0.1f);

    public Gradient activationGradient;
    public Color defaultColor = Color.white;
    
    [Header("Animation Settings")]
    public float animationNodeFadeInDuration = 0.4f;
    public float animationConnectionPulseDuration = 0.3f;
    public float animationDelayBetweenLayers = 0.2f;
    public float animationFinalFadeOutDuration = 1.0f;
    
    private List<GameObject> visualLayers = new List<GameObject>();
    private List<LineRenderer> visualConnections = new List<LineRenderer>();
    
    private Coroutine animationCoroutine;

    public void RedrawModel(ModelArchitecture arch)
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        var existingLayerPositions = new Dictionary<int, Vector3>();
        var existingNodePositions = new Dictionary<int, Dictionary<int, Vector3>>();

        for (int i = 0; i < visualLayers.Count; i++)
        {
            if (visualLayers[i] != null)
            {
                existingLayerPositions[i] = visualLayers[i].transform.localPosition;
                existingNodePositions[i] = new Dictionary<int, Vector3>();
                foreach (Transform nodeTransform in visualLayers[i].transform)
                {
                    var nodeInfo = nodeTransform.GetComponent<NodeInfo>();
                    if (nodeInfo != null)
                    {
                        existingNodePositions[i][nodeInfo.data.nodeIndex] = nodeTransform.localPosition;
                    }
                }
            }
        }

        foreach (var layer in visualLayers) Destroy(layer);
        visualLayers.Clear();
        foreach (var conn in visualConnections) if (conn != null) Destroy(conn.gameObject);
        visualConnections.Clear();

        List<int> newLayerSizes = new List<int> { arch.input_nodes };
        newLayerSizes.AddRange(arch.hidden_layer_sizes);
        newLayerSizes.Add(arch.output_nodes);

        for (int i = 0; i < newLayerSizes.Count; i++)
        {
            Vector3 layerLocalPosition;
            if (!existingLayerPositions.TryGetValue(i, out layerLocalPosition))
            {
                float defaultX = i * layerSpacing;
                layerLocalPosition = new Vector3(defaultX, 0, 0);
            }

            GameObject newLayerBox = Instantiate(layerBoxPrefab, layerContainer);
            newLayerBox.transform.localPosition = layerLocalPosition;
            newLayerBox.transform.localRotation = Quaternion.identity;
            newLayerBox.transform.localScale = Vector3.one;

            newLayerBox.name = $"Layer_{i}";
            visualLayers.Add(newLayerBox);

            LayerController newLc = newLayerBox.GetComponent<LayerController>();
            newLc.data = new LayerData();
            newLc.data.layerIndex = i;
            newLc.data.outputSize = newLayerSizes[i];
            
            if (i == 0)
                {
                    newLc.data.layerType = LayerType.Input;
                    newLc.data.inputSize = 0;
                    newLc.data.activationFunction = "None";
                }
                else if (i == newLayerSizes.Count - 1)
                {
                    newLc.data.layerType = LayerType.Output;
                    newLc.data.inputSize = newLayerSizes[i - 1];
                    newLc.data.activationFunction = "None";
                }
                else
                {
                    newLc.data.layerType = LayerType.Hidden;
                    newLc.data.inputSize = newLayerSizes[i - 1];
                    newLc.data.activationFunction = arch.activations[i - 1];
                }

            if (newLc != null && newLc.visualBoxRenderer != null)
            {
                newLc.visualBoxRenderer.enabled = true;
            }

            int nodeCount = newLayerSizes[i];
            float totalHeight = (nodeCount - 1) * nodeSpacing;
            float startY = -totalHeight / 2.0f;

            Dictionary<int, Vector3> savedNodePositions = existingNodePositions.ContainsKey(i) ? existingNodePositions[i] : null;

            for (int j = 0; j < nodeCount; j++)
            {
                GameObject newNode = Instantiate(nodePrefab, newLayerBox.transform);
                NodeInfo info = newNode.GetComponent<NodeInfo>() ?? newNode.AddComponent<NodeInfo>();

                info.data = new NodeData();
                info.data.layerIndex = i;
                info.data.nodeIndex = j;
                info.data.layerType = newLc.data.layerType;
                info.data.inConnections = (i > 0) ? newLayerSizes[i - 1] : 0;
                info.data.outConnections = (i < newLayerSizes.Count - 1) ? newLayerSizes[i + 1] : 0;

                Vector3 nodeLocalPosition;
                if (savedNodePositions != null && savedNodePositions.TryGetValue(j, out nodeLocalPosition))
                {
                    newNode.transform.localPosition = nodeLocalPosition;
                }
                else
                {
                    newNode.transform.localPosition = new Vector3(0, startY + j * nodeSpacing, 0);
                }

                newNode.transform.localRotation = Quaternion.identity;
                newNode.transform.localScale = Vector3.one;
            }

            UpdateLayerBox(newLayerBox);
        }

        DrawConnections();
    }

    public void UpdateLayerBox(GameObject layerBox)
    {
        BoxCollider collider = layerBox.GetComponent<BoxCollider>();
        LayerController lc = layerBox.GetComponent<LayerController>();
        Renderer visualRenderer = (lc != null) ? lc.visualBoxRenderer : null;

        List<Transform> layerNodes = new List<Transform>();
        foreach (Transform childNode in layerBox.transform)
        {
            if (childNode.GetComponent<NodeInfo>() != null)
            {
                layerNodes.Add(childNode);
            }
        }

        if (layerNodes.Count <= 0)
        {
            if (collider != null) collider.size = Vector3.zero;
            if (visualRenderer != null) visualRenderer.transform.parent.gameObject.SetActive(false);
            return;
        }

        if (visualRenderer != null)
        {
            visualRenderer.transform.parent.gameObject.SetActive(true);
            visualRenderer.material = lc != null ? lc.defaultMaterial : null; 
        }

        Vector3 minBoundsLocal = layerNodes[0].localPosition;
        Vector3 maxBoundsLocal = layerNodes[0].localPosition;

        for (int i = 1; i < layerNodes.Count; i++)
        {
            minBoundsLocal = Vector3.Min(minBoundsLocal, layerNodes[i].localPosition);
            maxBoundsLocal = Vector3.Max(maxBoundsLocal, layerNodes[i].localPosition);
        }

        Vector3 boxCenterLocal = (minBoundsLocal + maxBoundsLocal) / 2f;
        Vector3 boxSize = (maxBoundsLocal - minBoundsLocal) + boxPadding;

        if (collider != null)
        {
            collider.size = boxSize;
            collider.center = boxCenterLocal;
        }

        if (visualRenderer != null)
        {
            visualRenderer.transform.localPosition = boxCenterLocal;
            visualRenderer.transform.localScale = boxSize;
        }
    }

    private void DrawConnections()
    {
        foreach (var conn in visualConnections)
        {
            if (conn != null) Destroy(conn.gameObject);
        }
        visualConnections.Clear();
        
        for (int i = 0; i < visualLayers.Count - 1; i++)
        {
            foreach (Transform startNode in visualLayers[i].transform)
            {
                if(startNode.GetComponent<NodeInfo>() == null) continue;
                
                foreach (Transform endNode in visualLayers[i + 1].transform)
                {
                    if(endNode.GetComponent<NodeInfo>() == null) continue;

                    LineRenderer connectionLine = Instantiate(connectionPrefab, connectionContainer);
                    connectionLine.startWidth = 0.01f;
                    connectionLine.endWidth = 0.01f;
                    connectionLine.sortingOrder = 0;
                    NNNeuronConnectionRenderer controller = connectionLine.GetComponent<NNNeuronConnectionRenderer>();
                    controller.startNode = startNode;
                    controller.endNode = endNode;
                    visualConnections.Add(connectionLine);
                }
            }
        }
    }

    public void AnimateForwardPass(List<List<float>> allActivations)
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }

        animationCoroutine = StartCoroutine(AnimateForwardPassCoroutine(allActivations));
    }

    private IEnumerator AnimateForwardPassCoroutine(List<List<float>> allActivations)
    {
        try
        {
            if (activationGradient == null)
            {
                Debug.LogError("ERROR: Activation Gradient is not set in the Inspector!");
                yield break;
            }

            yield return StartCoroutine(FadeAllToDefaultCoroutine(0.1f));

            for (int i = 0; i < allActivations.Count; i++)
            {
                if (i >= visualLayers.Count) break;

                if (i > 0)
                {
                    yield return StartCoroutine(PulseConnectionsToLayer(i, animationConnectionPulseDuration));
                }

                List<float> layerActivations = allActivations[i];
                Transform layerTransform = visualLayers[i].transform;
                
                foreach (Transform nodeTransform in layerTransform)
                {
                    NodeInfo info = nodeTransform.GetComponent<NodeInfo>();
                    if (info == null || info.data.nodeIndex >= layerActivations.Count) continue;
                    
                    float value = layerActivations[info.data.nodeIndex];
                    float normalizedValue = ((float)System.Math.Tanh(value) + 1f) / 2f;
                    Color targetColor = activationGradient.Evaluate(normalizedValue);
                    
                    Renderer nodeRenderer = nodeTransform.GetComponent<Renderer>();
                    if (nodeRenderer != null)
                    {
                        StartCoroutine(FadeColor(nodeRenderer, targetColor, animationNodeFadeInDuration));
                    }
                }

                yield return new WaitForSeconds(animationNodeFadeInDuration + animationDelayBetweenLayers);
            }

            yield return new WaitForSeconds(animationDelayBetweenLayers * 4);
            yield return StartCoroutine(FadeAllToDefaultCoroutine(animationFinalFadeOutDuration));
        }
        finally
        {
            animationCoroutine = null;
        }
    }

    private IEnumerator FadeColor(Renderer targetRenderer, Color targetColor, float duration)
    {
        if (duration <= 0)
        {
            targetRenderer.material.color = targetColor;
            yield break;
        }

        Color startColor = targetRenderer.material.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            targetRenderer.material.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        targetRenderer.material.color = targetColor;
    }

    private IEnumerator PulseConnectionsToLayer(int targetLayerIndex, float duration)
    {
        if (targetLayerIndex <= 0 || targetLayerIndex >= visualLayers.Count) yield break;

        Transform targetLayer = visualLayers[targetLayerIndex].transform;
        foreach (var connection in visualConnections)
        {
            if(connection == null) continue;

            NNNeuronConnectionRenderer controller = connection.GetComponent<NNNeuronConnectionRenderer>();
            if (controller != null && controller.endNode != null && controller.endNode.IsChildOf(targetLayer))
            {
                Renderer startNodeRenderer = controller.startNode.GetComponent<Renderer>();
                if (startNodeRenderer != null)
                {
                    StartCoroutine(controller.AnimatePulse(startNodeRenderer.material.color, duration));
                }
            }
        }

        yield return new WaitForSeconds(duration);
    }

    private IEnumerator FadeAllToDefaultCoroutine(float duration)
    {
        List<Coroutine> runningFades = new List<Coroutine>();
        foreach (var layerBox in visualLayers)
        {
            foreach (Transform nodeTransform in layerBox.transform)
            {
                if (nodeTransform.GetComponent<NodeInfo>() == null) continue;

                Renderer r = nodeTransform.GetComponent<Renderer>();
                if (r != null)
                {
                    runningFades.Add(StartCoroutine(FadeColor(r, defaultColor, duration)));
                }
            }
        }

        foreach (var fade in runningFades)
        {
            yield return fade;
        }
    }
}

public class NodeInfo : MonoBehaviour
{
    public NodeData data;
    public bool isPositionCustom = false;
}