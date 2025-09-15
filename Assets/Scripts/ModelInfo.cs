using System.Collections.Generic;

public enum LayerType
{
    Input,
    Hidden,
    Output
}

[System.Serializable]
public class LayerData
{
    public int layerIndex;
    public LayerType layerType;
    public int inputSize;
    public int outputSize;
    public string activationFunction;
}

[System.Serializable]
public class NodeData
{
    public int layerIndex;
    public int nodeIndex;
    public LayerType layerType;
    public int inConnections;
    public int outConnections;

    public List<float> weights = new List<float>();
    public float bias;
    public bool detailsLoaded = false;
}
