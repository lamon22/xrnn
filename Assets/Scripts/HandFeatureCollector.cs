using System;
using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction.Input;
using Oculus.Interaction.PoseDetection;
using TMPro;
using UnityEngine;

public class HandFeatureCollector : MonoBehaviour
{
    
    [Header("Hand References")]
    public FingerFeatureStateProvider leftFingerFeatures;
    public TransformFeatureStateProvider leftTransformFeature;
    public Hand leftHand;
    public FingerFeatureStateProvider rightFingerFeatures;
    public TransformFeatureStateProvider rightTransformFeature;
    public Hand rightHand;
    
    [Header("Buffer Settings")]
    public int sampleRate = 60; // Samples per second
    public float windowDuration = 1.0f; // Window size in seconds
    public float overlapFraction = 0.5f; // Fraction of overlap between windows
    
    [Header("Debug text")]
    public TextMeshProUGUI fingerFeaturesText;
    public TextMeshProUGUI transformFeaturesText;
    public TextMeshProUGUI jointDeltaFeaturesText;
    
    private int _bufferSize;
    private int _stepSize;
    private List<float[]> _featureBuffer;
    private float _lastSampleTime;

    private TransformConfig transformConfig;
    
    private Dictionary<HandJointId, Pose> _previousLeftHandPoses = new Dictionary<HandJointId, Pose>();
    private Dictionary<HandJointId, Pose> _previousRightHandPoses = new Dictionary<HandJointId, Pose>();
    
    
    public event Action<List<float[]>> OnWindowReady;
    
    void Start()
    {
        // Calculate buffer and step size
        _bufferSize = Mathf.CeilToInt(sampleRate * windowDuration);
        _stepSize = Mathf.CeilToInt(_bufferSize * (1 - overlapFraction));

        // Initialize buffer
        _featureBuffer = new List<float[]>();
        _lastSampleTime = 0;
        
        transformConfig = new TransformConfig();
        rightTransformFeature.RegisterConfig(transformConfig);
        
        IList<HandJointId> allTrackedJoints = new List<HandJointId>();
        foreach (HandJointId joint  in Enum.GetValues(typeof(HandJointId)))
        {
            allTrackedJoints.Add(joint);
        }

        InitializeHandPoses(leftHand, _previousLeftHandPoses);
        InitializeHandPoses(rightHand, _previousRightHandPoses);
        
    }

    void FixedUpdate()
    {
        // Check if it is time to sample based on the sample rate
        if (Time.time - _lastSampleTime >= 1f / sampleRate)
        {
            _lastSampleTime = Time.time;

            // Extract features from both hands
            float[] features = ExtractHandFeatures();

            // Add features to the buffer
            _featureBuffer.Add(features);

            // Check if the buffer size has reached the desired window size
            if (_featureBuffer.Count >= _bufferSize)
            {
                // Trigger the OnWindowReady event with a copy of the buffer
                OnWindowReady?.Invoke(new List<float[]>(_featureBuffer));

                // Slide the window forward by the step size
                _featureBuffer.RemoveRange(0, _stepSize);
            }
        }
    }
    
     private float[] ExtractHandFeatures()
    {
        List<float> features = new List<float>();

        // let's get finger features
        FingerFeatureStateProvider[] fingerFeatures = new[] { rightFingerFeatures }; //leftFingerFeatures
        // cycle for left and right hand
        foreach(FingerFeatureStateProvider fingerFeatureStateProvider in fingerFeatures)
        {
            // hand finger curl features
            fingerFeatureStateProvider.GetCurrentState(HandFinger.Thumb, FingerFeature.Curl, out string thumbCurl);
            fingerFeatureStateProvider.GetCurrentState(HandFinger.Index, FingerFeature.Curl, out string indexCurl);
            fingerFeatureStateProvider.GetCurrentState(HandFinger.Middle, FingerFeature.Curl, out string middleCurl);
            fingerFeatureStateProvider.GetCurrentState(HandFinger.Ring, FingerFeature.Curl, out string ringCurl);
            fingerFeatureStateProvider.GetCurrentState(HandFinger.Pinky, FingerFeature.Curl, out string pinkyCurl);
            
            // hand finger flexion features
            fingerFeatureStateProvider.GetCurrentState(HandFinger.Index, FingerFeature.Flexion, out string indexFlexion);
            fingerFeatureStateProvider.GetCurrentState(HandFinger.Middle, FingerFeature.Flexion, out string middleFlexion);
            fingerFeatureStateProvider.GetCurrentState(HandFinger.Ring, FingerFeature.Flexion, out string ringFlexion);
            fingerFeatureStateProvider.GetCurrentState(HandFinger.Pinky, FingerFeature.Flexion, out string pinkyFlexion);
            
            // hand finger abduction features
            fingerFeatureStateProvider.GetCurrentState(HandFinger.Thumb, FingerFeature.Abduction, out string thumbAbduction);
            fingerFeatureStateProvider.GetCurrentState(HandFinger.Index, FingerFeature.Abduction, out string indexAbduction);
            fingerFeatureStateProvider.GetCurrentState(HandFinger.Middle, FingerFeature.Abduction, out string middleAbduction);
            fingerFeatureStateProvider.GetCurrentState(HandFinger.Ring, FingerFeature.Abduction, out string ringAbduction);
            
            // hand finger opposition features
            fingerFeatureStateProvider.GetCurrentState(HandFinger.Index, FingerFeature.Opposition, out string indexOpposition);
            fingerFeatureStateProvider.GetCurrentState(HandFinger.Middle, FingerFeature.Opposition, out string middleOpposition);
            fingerFeatureStateProvider.GetCurrentState(HandFinger.Ring, FingerFeature.Opposition, out string ringOpposition);
            fingerFeatureStateProvider.GetCurrentState(HandFinger.Pinky, FingerFeature.Opposition, out string pinkyOpposition);
            
            // print all features in one line
            fingerFeaturesText.text = $"{thumbCurl} {indexCurl} {middleCurl} {ringCurl} {pinkyCurl} " +
                                  $"{indexFlexion} {middleFlexion} {ringFlexion} {pinkyFlexion} " +
                                  $"{thumbAbduction} {indexAbduction} {middleAbduction} {ringAbduction} " +
                                  $"{indexOpposition} {middleOpposition} {ringOpposition} {pinkyOpposition}";
            
            //TODO add to features list
        }

        try
        {
            //  let's get transform features
            TransformFeatureStateProvider[] transformFeatures = new[] { rightTransformFeature }; //leftTransformFeature
            // foreach hand
            foreach (TransformFeatureStateProvider transformFeatureStateProvider in transformFeatures)
            {
                string featuresText = "";
                // foreach feature (wrist up, wrist down, ...)
                foreach (TransformFeature tf in Enum.GetValues(typeof(TransformFeature)))
                {
                    float? state = transformFeatureStateProvider.GetFeatureValue(transformConfig, tf);

                    featuresText += $"{tf}: {state} ";

                    // TODO add to features list
                }

                transformFeaturesText.text = featuresText;
            }
        }
        catch (KeyNotFoundException ex)
        {
            Debug.LogError($"KeyNotFoundException: {ex.Message}\n{ex.StackTrace}");
        }

        // TrackHandMovement(leftHand, _previousLeftHandPoses, "Left Hand");
        // TrackHandMovement(rightHand, _previousRightHandPoses, "Right Hand");
        //
        //jointDeltaFeaturesText.text = featuresText;
        

        return features.ToArray();
    }
     
    private void InitializeHandPoses(Hand hand, Dictionary<HandJointId, Pose> previousPoses)
    {
        foreach (HandJointId joint in Enum.GetValues(typeof(HandJointId)))
        {
            if (hand.GetJointPose(joint, out Pose jointPose))
            {
                previousPoses[joint] = jointPose;
            }
        }
    }
    
    private void TrackHandMovement(Hand hand, Dictionary<HandJointId, Pose> previousPoses, string handName)
    {
        foreach (HandJointId joint in Enum.GetValues(typeof(HandJointId)))
        {
            if (hand.GetJointPose(joint, out Pose currentPose))
            {
                if (previousPoses.TryGetValue(joint, out Pose previousPose))
                {
                    Vector3 positionDelta = currentPose.position - previousPose.position;
                    Quaternion rotationDelta = Quaternion.Inverse(previousPose.rotation) * currentPose.rotation;

                    Debug.Log($"{handName} - {joint}: Position Delta: {positionDelta}, Rotation Delta: {rotationDelta}");

                    // Update the previous pose with the current pose
                    previousPoses[joint] = currentPose;
                }
                else
                {
                    // Initialize the previous pose if not already present
                    previousPoses[joint] = currentPose;
                }
            }
        }
    }
}
