using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System.Linq;
using Newtonsoft.Json;
using System.Globalization;

public class NNManager : MonoBehaviour
{
    public static NNManager Instance { get; private set; }
    
    public RESTManager restManager;
    public ONNXModelVisualizer modelVisualizer;
    public Text statusText;

    private ModelArchitecture currentArchitecture;
    private Camera mainCamera;

    private int nodeLayerMask;
    private int layerBoxLayerMask;

    private Transform draggedObject = null;
    private Transform hoveredLayerBox = null;
    private LayerController hoveredLayerController = null;
    private Vector3 offset;
    private float zCoord;
    
    public void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    void Start()
    {
        if (restManager == null || modelVisualizer == null)
        {
            Debug.LogError("Assegna RestManager e ModelVisualizer nell'Inspector!");
            return;
        }

        mainCamera = Camera.main;
        if(mainCamera.GetComponent<PhysicsRaycaster>() == null)
        {
            mainCamera.gameObject.AddComponent<PhysicsRaycaster>();
            Debug.Log("PhysicsRaycaster aggiunto alla Main Camera.");
        }
        
        nodeLayerMask = LayerMask.GetMask("Nodes");
        layerBoxLayerMask = LayerMask.GetMask("LayerBoxes");

        RequestInitialArchitecture();
    }

    void Update()
    {
        HandleKeyboardInputs();
        HandleMouseInputs();
    }

    void HandleKeyboardInputs()
    {
        if (Input.GetKeyDown(KeyCode.A)) AddNodes(0, 1);
        if (Input.GetKeyDown(KeyCode.R)) RemoveNodes(0, 1);
        if (Input.GetKeyDown(KeyCode.L)) AddLayer(1, 8); if (Input.GetKeyDown(KeyCode.K)) RemoveLayer(0);
        if (Input.GetKeyDown(KeyCode.C)) ChangeActivation(0, "tanh");
        if (Input.GetKeyDown(KeyCode.F)) AnimateForwardPass();
    }
    
    void HandleMouseInputs()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (draggedObject == null)
        {
            if (Physics.Raycast(ray, out hit, 1000f, layerBoxLayerMask))
            {
                hoveredLayerController = hit.transform.GetComponent<LayerController>();
                if (hoveredLayerController != null && hoveredLayerController.visualBoxRenderer != null)
                {
                    hoveredLayerController.visualBoxRenderer.enabled = true;
                    hoveredLayerController.visualBoxRenderer.material = hoveredLayerController.defaultMaterial;
                }
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (Physics.Raycast(ray, out hit, 1000f, nodeLayerMask))
            {
                NodeInfo nodeInfo = hit.transform.GetComponent<NodeInfo>();
                if (nodeInfo != null && nodeInfo.data != null)
                {
                    DisplayNodeInfo(nodeInfo);
                }
            }
            else if (Physics.Raycast(ray, out hit, 1000f, layerBoxLayerMask))
            {
                LayerController layerController = hit.transform.GetComponent<LayerController>();
                if (layerController != null && layerController.data != null)
                {
                    DisplayLayerInfo(layerController.data);
                }
            }

            if (Physics.Raycast(ray, out hit, 1000f, nodeLayerMask))
            {
                draggedObject = hit.transform;
            }
            else if (Physics.Raycast(ray, out hit, 1000f, layerBoxLayerMask))
            {
                draggedObject = hit.transform;
                LayerController lc = draggedObject.GetComponent<LayerController>();
                if (lc != null && lc.visualBoxRenderer != null)
                {
                    lc.visualBoxRenderer.material = lc.selectedMaterial;
                }
            }
            
            if (draggedObject != null)
            {
                zCoord = mainCamera.WorldToScreenPoint(draggedObject.position).z;
                offset = draggedObject.position - GetMouseWorldPos();
            }
        }

        if (Input.GetMouseButton(0) && draggedObject != null)
        {
            Vector3 newPosition = GetMouseWorldPos() + offset;
            draggedObject.position = newPosition;
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (draggedObject != null)
            {
                NodeInfo nodeInfo = draggedObject.GetComponent<NodeInfo>();
                if (nodeInfo != null)
                {
                    nodeInfo.isPositionCustom = true;
                    modelVisualizer.UpdateLayerBox(draggedObject.parent.gameObject);
                }

                LayerController lc = draggedObject.GetComponent<LayerController>();
                if (lc != null)
                {
                    lc.isPositionCustom = true; 
                    
                    if(lc.visualBoxRenderer != null)
                    {
                        lc.visualBoxRenderer.material = lc.defaultMaterial;
                    }
                }
            }
            draggedObject = null;
        }
    }
    
    private Vector3 GetMouseWorldPos()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = zCoord;
        return mainCamera.ScreenToWorldPoint(mousePoint);
    }

    public void RequestInitialArchitecture()
    {
        UpdateStatus("Richiesta architettura iniziale...");
        restManager.GetRequest("/get_architecture", (success, jsonResponse) =>
        {
            if (success)
            {
                var response = JsonUtility.FromJson<ServerResponse>(jsonResponse);
                if (response.status == "success")
                {
                    OnArchitectureUpdated(response.architecture);
                    UpdateStatus("Architettura ricevuta. Pronto.");
                }
                else
                {
                    UpdateStatus("Errore server: " + response.message);
                }
            }
            else { UpdateStatus("Errore di connessione: " + jsonResponse); }
        });
    }

    public void AddNodes(int layerIndex, int numberOfNodes)
    {
        if (numberOfNodes <= 0) return;
        if (layerIndex == 0)
        {
            UpdateStatus($"Aggiunta di {numberOfNodes} nodi a layer {layerIndex} (Input Layer)...");
            string json = $"{{\"num_nodes\": {numberOfNodes}}}";
            restManager.PostRequest("/add_input_node", json, HandleModificationResponse);
        }
        else
        {
            int backendLayerIndex = layerIndex - 1;
            UpdateStatus($"Aggiunta di {numberOfNodes} nodi a layer {layerIndex} (Backend Index: {backendLayerIndex})...");
            string json = $"{{\"layer_index\": {backendLayerIndex}, \"num_nodes\": {numberOfNodes}}}";
            restManager.PostRequest("/add_node", json, HandleModificationResponse);
        }
    }

    public void RemoveNodes(int layerIndex, int numberOfNodes)
    {
        if (numberOfNodes <= 0) return;
        if (layerIndex == 0)
        {
            UpdateStatus($"Rimozione di {numberOfNodes} nodi dal layer {layerIndex} (Input Layer)...");
            string json = $"{{\"num_nodes\": {numberOfNodes}}}";
            restManager.PostRequest("/remove_input_node", json, HandleModificationResponse);
        }
        else
        {
            int backendLayerIndex = layerIndex - 1;
            UpdateStatus($"Rimozione di {numberOfNodes} nodi dall'alto del layer {layerIndex} (Backend Index: {backendLayerIndex})...");
            string json = $"{{\"layer_index\": {backendLayerIndex}, \"num_nodes\": {numberOfNodes}}}";
            restManager.PostRequest("/remove_node", json, HandleModificationResponse);
        }
    }

    public void AddLayer(int layerIndex, int size)
    {
        UpdateStatus($"Aggiunta layer a indice {layerIndex}...");
        string json = $"{{\"layer_index\": {layerIndex}, \"new_layer_size\": {size}}}";
        restManager.PostRequest("/add_layer", json, HandleModificationResponse);
    }

    public void RemoveLayer(int layerIndex)
    {
        UpdateStatus($"Rimozione layer {layerIndex}...");
        string json = $"{{\"layer_index\": {layerIndex-1}}}";
        restManager.PostRequest("/remove_layer", json, HandleModificationResponse);
    }

    public void ChangeActivation(int layerIndex, string newActivation)
    {
        UpdateStatus($"Cambio attivazione layer {layerIndex} in {newActivation}...");
        string json = $"{{\"layer_index\": {layerIndex}, \"new_activation_name\": \"{newActivation}\"}}";
        restManager.PostRequest("/change_activation", json, HandleModificationResponse);
    }

    public void AnimateForwardPass()
    {
        if (currentArchitecture == null) return;

        UpdateStatus($"Animazione forward pass...");
        var inputData = new System.Collections.Generic.List<float>();
        for (int i = 0; i < currentArchitecture.input_nodes; i++)
        {
            inputData.Add(Random.Range(-1f, 1f));
        }

        // Usa CultureInfo.InvariantCulture per garantire il punto come separatore decimale
        string inputJson = string.Join(",", inputData.Select(f => f.ToString(CultureInfo.InvariantCulture)));
        string json = $"{{\"input_data\": [{inputJson}]}}";
        
        Debug.Log(json);

        restManager.PostRequest("/forward_pass", json, (success, jsonResponse) =>
        {
            if (success)
            {
                var response = JsonConvert.DeserializeObject<ForwardPassResponse>(jsonResponse);

                if (response.status == "success")
                {
                    modelVisualizer.AnimateForwardPass(response.activations);
                    UpdateStatus("Animazione forward pass completata.");
                }
                else
                {
                    UpdateStatus("Errore server: " + jsonResponse);
                }
            }
            else { UpdateStatus("Errore di connessione: " + jsonResponse); }
        });
    }

    private void HandleModificationResponse(bool success, string jsonResponse)
    {
        if (success)
        {
            var response = JsonUtility.FromJson<ServerResponse>(jsonResponse);
            if (response.status == "success")
            {
                UpdateStatus("Modifica completata: " + response.message);
                OnArchitectureUpdated(response.architecture);
            }
            else
            {
                UpdateStatus("Errore server: " + response.message);
            }
        }
        else { UpdateStatus("Errore di connessione: " + jsonResponse); }
    }

    private void OnArchitectureUpdated(ModelArchitecture newArch)
    {
        currentArchitecture = newArch;
        modelVisualizer.RedrawModel(currentArchitecture);
    }

    private void UpdateStatus(string message)
    {
        Debug.Log(message);
        if (statusText != null)
        {
            statusText.text = "Status: " + message;
        }
    }

    public string PrintNodeDetails(NodeData data)
    {
        string weightsString = string.Join(", ", data.weights.Select(w => w.ToString("F4")));
        if (weightsString.Length > 200) {
             weightsString = weightsString.Substring(0, 200) + "...";
        }

        string info = "";

        if (data.layerType == LayerType.Input)
        {
            info = $"--- Node Info ---\n" +
                    $"Layer Index: {data.layerIndex} (Input Layer)\n" +
                    $"Node Index: {data.nodeIndex}\n" +
                    $"Out-Connections: {data.outConnections}\n" +
                    $"-----------------";
        }
        else
        {
            info = $"--- Node Info ---\n" +
                $"Layer Index: {data.layerIndex}\n" +
                $"Node Index: {data.nodeIndex}\n" +
                $"Layer Type: {data.layerType}\n" +
                $"In-Connections: {data.inConnections}\n" +
                $"Out-Connections: {data.outConnections}\n" +
                $"Bias: {data.bias.ToString("F4")}\n" +
                $"Input Weights ({data.weights.Count}): [{weightsString}]\n" +
                $"-----------------";
        }
        return info;
    }
    
    public NodeData DisplayNodeInfo(NodeInfo nodeInfo)
    {
        NodeData data = nodeInfo.data;

        if (data.detailsLoaded)
        {
            Debug.Log(PrintNodeDetails(data));
        }
        else
        {
            UpdateStatus($"Requesting details for Node ({data.layerIndex}, {data.nodeIndex})...");
            string json = $"{{\"layer_index\": {data.layerIndex}, \"node_index\": {data.nodeIndex}}}";
            
            restManager.PostRequest("/get_node_details", json, (success, jsonResponse) =>
            {
                if (success)
                {
                    var response = JsonUtility.FromJson<NodeDetailsResponse>(jsonResponse);
                    if (response.status == "success")
                    {
                        data.weights = response.weights;
                        data.bias = response.bias;
                        data.detailsLoaded = true;
                        
                        UpdateStatus($"Details received for Node ({data.layerIndex}, {data.nodeIndex}).");
                        Debug.Log(PrintNodeDetails(data));
                    }
                    else
                    {
                        UpdateStatus($"Server Info: {response.message}");
                        Debug.Log("Server returned a non-success status: " + response.message);
                    }
                }
                else
                {
                    UpdateStatus("Connection Error: " + jsonResponse);
                    Debug.LogError("Request FAILED. Error: " + jsonResponse);
                }
            });
        }
        return data;
    }

    private void DisplayLayerInfo(LayerData data)
    {
        string info = $"--- Layer Info ---\n" +
                      $"Layer Index: {data.layerIndex}\n" +
                      $"Layer Type: {data.layerType}\n" +
                      $"Input Size: {data.inputSize}\n" +
                      $"Output Size: {data.outputSize}\n" +
                      $"Activation: {data.activationFunction}\n" +
                      $"------------------";
        Debug.Log(info);
    }
}