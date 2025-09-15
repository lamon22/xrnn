using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // Necessario per ToArray() e Select()
using UnityEngine;
using Unity.Barracuda; // Necessario per Barracuda
using Oculus.Interaction.Input;
using Oculus.Interaction.PoseDetection;
using TMPro; // Se vuoi mostrare il risultato
using EditorAttributes; // Se usi ancora i Button per test

public class HandGestureRecognizer : MonoBehaviour
{
    [Header("Hand References")]
    public Oculus.Interaction.Input.Hand leftHand;
    public Oculus.Interaction.Input.Hand rightHand;
    public TransformFeatureStateProvider leftTransformFeature;
    public TransformFeatureStateProvider rightTransformFeature;

    [Header("ONNX Model")]
    public int windowSize = 30; // Numero di frame nel buffer (deve corrispondere all'input del modello)
    public float predictionInterval= 0.5f; // Ogni quanti secondi eseguire la predizione (es. 0.1 = 10 volte al secondo)
    public float collectionFrequency = 30;
    
    [Header("Send to Python server")]
    public RESTGestureManager restManager;

    // Internal state
    private Queue<float[]> _featureBufferLeft;
    private Queue<float[]> _featureBufferRight;
    private float _timeSinceLastPredictionLeft;
    private float _timeSinceLastPredictionRight;
    private int _featureVectorSize = -1;
    private Vector3 _lastLeftPos;
    private Vector3 _lastRightPos;
    private float _timeSinceLastCollection = 0f;
    
    private TransformConfig _transformConfig;
    private GameObject[] _jointObjects = new GameObject[18]; // Per debug

    private static readonly HandJointId[] JointList = new[]
    {
        HandJointId.HandStart, HandJointId.HandThumb0, HandJointId.HandThumb1, HandJointId.HandThumb2,
        HandJointId.HandIndex1, HandJointId.HandIndex2, HandJointId.HandMiddle1, HandJointId.HandMiddle2,
        HandJointId.HandRing1, HandJointId.HandRing2, HandJointId.HandPinky0, HandJointId.HandPinky1,
        HandJointId.HandPinky2, HandJointId.HandThumb3, HandJointId.HandIndex3, HandJointId.HandMiddle3,
        HandJointId.HandRing3, HandJointId.HandPinky3
    };

    // Mappa delle feature SoCJ per mantenere l'ordine corretto
    private static readonly string[] SocjFeatureOrder = new[]
    {
        "thumb1", "thumb2", "thumb3", "index1", "index2", "index3",
        "middle1", "middle2", "middle3", "ring1", "ring2", "ring3",
        "pinky1", "pinky2", "pinky3", "second1", "second2", "second3", "second4",
        "third1", "third2", "third3", "third4"
    };

    // Mappa delle TransformFeature per mantenere l'ordine corretto
    private static readonly TransformFeature[] TransformFeatureOrder = new[]
    {
        TransformFeature.WristUp, TransformFeature.WristDown, TransformFeature.PalmDown, TransformFeature.PalmUp,
        TransformFeature.PalmTowardsFace, TransformFeature.PalmAwayFromFace, TransformFeature.FingersUp,
        TransformFeature.FingersDown, TransformFeature.PinchClear
    };


    void Start()
    {
        // Inizializza i buffer
        _featureBufferLeft = new Queue<float[]>(windowSize);
        _featureBufferRight = new Queue<float[]>(windowSize);
        
        // Inizializza TransformConfig
        _transformConfig = new TransformConfig();
        // Non è più necessario registrarlo qui se lo passi direttamente a GetFeatureValue
    }

    void FixedUpdate()
    {
        // Aggiorna timer
        _timeSinceLastPredictionLeft += Time.deltaTime;
        _timeSinceLastPredictionRight += Time.deltaTime;
        _timeSinceLastCollection += Time.deltaTime;

        if (_timeSinceLastCollection > 1f / collectionFrequency)
        {
            _timeSinceLastCollection = 0f;
            
            // --- Left hand features ---
            float[] leftFeatures = CalculateFrameFeatures(leftHand, leftTransformFeature, ref _lastLeftPos);
            if (leftFeatures != null)
            {
                if (_featureVectorSize < 0)
                {
                    _featureVectorSize = leftFeatures.Length;
                }

                _featureBufferLeft.Enqueue(leftFeatures);
                while (_featureBufferLeft.Count > windowSize)
                {
                    _featureBufferLeft.Dequeue();
                }

                if (_featureBufferLeft.Count == windowSize && _timeSinceLastPredictionLeft >= predictionInterval)
                {
                    _timeSinceLastPredictionLeft = 0f;
                    float[] inputArray = _featureBufferLeft.SelectMany(f => f).ToArray();
                    using (var inputTensor = new Tensor(1, 1, windowSize * _featureVectorSize, 1, inputArray))
                    {
                        restManager.SendFeatures(inputArray, true);
                    }
                }
            }

            // --- Right hand features ---
            float[] rightFeatures = CalculateFrameFeatures(rightHand, rightTransformFeature, ref _lastRightPos);
            if (rightFeatures != null)
            {
                if (_featureVectorSize < 0)
                {
                    _featureVectorSize = rightFeatures.Length;
                }

                _featureBufferRight.Enqueue(rightFeatures);
                while (_featureBufferRight.Count > windowSize)
                {
                    _featureBufferRight.Dequeue();
                }

                if (_featureBufferRight.Count == windowSize && _timeSinceLastPredictionRight >= predictionInterval)
                {
                    _timeSinceLastPredictionRight = 0f;
                    float[] inputArray = _featureBufferRight.SelectMany(f => f).ToArray();
                    using (var inputTensor = new Tensor(1, 1, windowSize * _featureVectorSize, 1, inputArray))
                    {
                        restManager.SendFeatures(inputArray, false);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Calcola tutte le feature richieste per un singolo frame.
    /// </summary>
    /// <returns>Un array di float contenente tutte le feature, o null se i dati non sono validi.</returns>
    private float[] CalculateFrameFeatures(Oculus.Interaction.Input.Hand hand, TransformFeatureStateProvider featureProvider, ref Vector3 lasthandPos)
    {
        // 1. Ottieni le posizioni delle articolazioni
        Dictionary<HandJointId, Vector3> jointPositions = new Dictionary<HandJointId, Vector3>();
        foreach (HandJointId jointId in JointList)
        {
            if (hand.GetJointPose(jointId, out Pose jointPose))
            {
                jointPositions.Add(jointId, jointPose.position);
            }
            else
            {
                // Se anche una sola articolazione non è valida, non possiamo calcolare le feature
                 Debug.LogWarning($"Could not get pose for joint {jointId}");
                return null;
            }
        }

        // Lista per contenere tutte le feature in ordine
        List<float> features = new List<float>();

        // 1.2 Aggiungi la mano selezionata (sinistra o destra)
        features.Add(hand == leftHand ? 1.0f : 0.0f); // 1 per sinistra, 0 per destra
        
        // 2. Calcola Hand Delta
        Vector3 currentHandPos = jointPositions[HandJointId.HandStart];
        Vector3 handDelta = currentHandPos - lasthandPos;
        features.Add(handDelta.x);
        features.Add(handDelta.y);
        features.Add(handDelta.z);
        lasthandPos = currentHandPos; // Aggiorna per il prossimo frame

        // 3. Calcola SoCJ (State of phalangeal Joints)
        // Assicurati che l'ordine qui corrisponda a quello atteso dal modello!
        // Usa la mappa SocjFeatureOrder per garantire la consistenza.
        Dictionary<string, Vector3> socjData = CalculateSoCJ(jointPositions);
        foreach (string key in SocjFeatureOrder)
        {
            if (socjData.TryGetValue(key, out Vector3 vec))
            {
                features.Add(vec.x);
                features.Add(vec.y);
                features.Add(vec.z);
            }
            else
            {
                 Debug.LogError($"SoCJ key {key} not found during feature calculation!");
                 // Potresti voler aggiungere valori placeholder (es. 0,0,0) o gestire l'errore
                 features.AddRange(new float[] { 0f, 0f, 0f });
            }
        }


        // 4. Calcola Transform Features
        // Assicurati che l'ordine corrisponda a quello atteso dal modello!
        // Usa la mappa TransformFeatureOrder.
        foreach (TransformFeature tf in TransformFeatureOrder)
        {
            // Passa _transformConfig direttamente qui
             float? state = featureProvider.GetFeatureValue(_transformConfig, tf);
             features.Add(state ?? 0f); // Usa 0 se il valore è null
        }

        // 5. Aggiungi feature mano sinistra/destra (opzionale, se il modello la usa)
        // features.Add(useLeftHand ? 1.0f : 0.0f); // Esempio: 1 per sinistra, 0 per destra

        // Verifica finale (opzionale ma utile per debug)
        if (_featureVectorSize > 0 && features.Count != _featureVectorSize)
        {
            Debug.LogError($"Feature count mismatch! Expected {_featureVectorSize}, got {features.Count}");
            return null; // Non usare dati inconsistenti
        }


        return features.ToArray();
    }

    /// <summary>
    /// Calcola i vettori State of Connected Joints (SoCJ).
    /// </summary>
    private Dictionary<string, Vector3> CalculateSoCJ(Dictionary<HandJointId, Vector3> frameData)
    {
        Dictionary<string, Vector3> socjData = new Dictionary<string, Vector3>();
        // Assicurati che tutte le chiavi necessarie esistano in frameData (gestito nel chiamante)

        // Fingers (relativo al parent o al palmo/inizio pollice)
        Vector3 thumbBase = frameData[HandJointId.HandThumb0]; // Usato come riferimento per le dita
        Vector3 indexBase = frameData[HandJointId.HandIndex1]; // A volte i modelli usano le basi delle dita
        Vector3 middleBase = frameData[HandJointId.HandMiddle1];
        Vector3 ringBase = frameData[HandJointId.HandRing1];
        Vector3 pinkyBase = frameData[HandJointId.HandPinky1];

        // Thumb
        socjData.Add("thumb1", frameData[HandJointId.HandThumb1] - thumbBase);
        socjData.Add("thumb2", frameData[HandJointId.HandThumb2] - frameData[HandJointId.HandThumb1]);
        socjData.Add("thumb3", frameData[HandJointId.HandThumb3] - frameData[HandJointId.HandThumb2]);
        // Index (relativo alla base del pollice o alla base dell'indice - scegli quello usato per l'allenamento)
        socjData.Add("index1", indexBase - thumbBase); // Esempio: relativo alla base del pollice
        //socjData.Add("index1", indexBase - frameData[HandJointId.HandStart]); // Esempio: relativo al polso
        socjData.Add("index2", frameData[HandJointId.HandIndex2] - indexBase);
        socjData.Add("index3", frameData[HandJointId.HandIndex3] - frameData[HandJointId.HandIndex2]);
        // Middle
        socjData.Add("middle1", middleBase - thumbBase);
        socjData.Add("middle2", frameData[HandJointId.HandMiddle2] - middleBase);
        socjData.Add("middle3", frameData[HandJointId.HandMiddle3] - frameData[HandJointId.HandMiddle2]);
        // Ring
        socjData.Add("ring1", ringBase - thumbBase);
        socjData.Add("ring2", frameData[HandJointId.HandRing2] - ringBase);
        socjData.Add("ring3", frameData[HandJointId.HandRing3] - frameData[HandJointId.HandRing2]);
        // Pinky
        socjData.Add("pinky1", pinkyBase - thumbBase);
        socjData.Add("pinky2", frameData[HandJointId.HandPinky2] - pinkyBase);
        socjData.Add("pinky3", frameData[HandJointId.HandPinky3] - frameData[HandJointId.HandPinky2]);

        // Knuckles (tra le dita alla stessa "altezza" falangea)
        // 2nd Knuckle Row (Falange prossimale - indice, medio, anulare, mignolo)
        // NOTA: Il pollice ha meno falangi, quindi non c'è un equivalente diretto per "second2/3" rispetto al pollice.
        // Spesso si calcola rispetto all'indice.
        //socjData.Add("second1", frameData[HandJointId.HandThumb2] - frameData[HandJointId.HandIndex2]); // Pollice vs Indice
        socjData.Add("second1", frameData[HandJointId.HandIndex2] - middleBase); // Modificato: indice vs base medio? Verifica logica
        socjData.Add("second2", frameData[HandJointId.HandIndex2] - frameData[HandJointId.HandMiddle2]);
        socjData.Add("second3", frameData[HandJointId.HandMiddle2] - frameData[HandJointId.HandRing2]);
        socjData.Add("second4", frameData[HandJointId.HandRing2] - frameData[HandJointId.HandPinky2]);
        // 3rd Knuckle Row (Falange distale - punta delle dita)
        //socjData.Add("third1", frameData[HandJointId.HandThumb3] - frameData[HandJointId.HandIndex3]); // Pollice vs Indice
        socjData.Add("third1", frameData[HandJointId.HandIndex3] - frameData[HandJointId.HandMiddle3]); // Modificato: indice vs medio? Verifica logica
        socjData.Add("third2", frameData[HandJointId.HandIndex3] - frameData[HandJointId.HandMiddle3]);
        socjData.Add("third3", frameData[HandJointId.HandMiddle3] - frameData[HandJointId.HandRing3]);
        socjData.Add("third4", frameData[HandJointId.HandRing3] - frameData[HandJointId.HandPinky3]);

        return socjData;
    }
}