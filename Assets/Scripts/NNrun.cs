using UnityEngine;
using Unity.Barracuda;
using System.Collections;

public class NNrun : MonoBehaviour
{
    public NNModel modelAsset;
    private Model _runtimeModel;
    private IWorker _worker;

    void Start()
    {
        // Load the ONNX model
        _runtimeModel = ModelLoader.Load(modelAsset);
        _worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, _runtimeModel);

        // Start the coroutine to run the model every second
        StartCoroutine(RunModelEverySecond());
    }

    private IEnumerator RunModelEverySecond()
    {
        while (true)
        {
            // Define the tensor shape (1, 1, 15, 54)
            TensorShape shape = new TensorShape(1, 1, 54, 15);

            // Create a tensor with random values
            Tensor inputTensor = new Tensor(shape);
            for (int i = 0; i < inputTensor.length; i++)
            {
                inputTensor[i] = Random.value;
            }

            // Measure the execution time
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            // Execute the model
            _worker.Execute(inputTensor);

            // Get the output
            Tensor outputTensor = _worker.PeekOutput();

            stopwatch.Stop();
            Debug.Log($"Model output: {outputTensor}");
            Debug.Log($"Execution time: {stopwatch.ElapsedMilliseconds} ms");

            // Dispose tensors
            inputTensor.Dispose();
            outputTensor.Dispose();

            // Wait for 1 second before the next execution
            yield return new WaitForSeconds(1);
        }
    }

    void OnDestroy()
    {
        // Dispose the worker when the script is destroyed
        _worker.Dispose();
    }
}