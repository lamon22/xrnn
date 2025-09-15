using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using EditorAttributes;
using Oculus.Interaction.Input;
using Oculus.Interaction.PoseDetection;
using TMPro;
using UnityEngine;
using UnityEngine.XR;

public class HandRecorder : MonoBehaviour
{
    // Script for recording hand features and saving them to a CSV file
    // FEATURES (per frame):
    // 9 transform feature
    // 1 x 3 delta of wrist/palm position
    // 1 left/right
    // 5 x 3 SoCJ + 2 x 4 SoCJ
    
    [Header("Hand References")]
    public Oculus.Interaction.Input.Hand  leftHand;
    public Oculus.Interaction.Input.Hand rightHand;
    public TransformFeatureStateProvider leftTransformFeature;
    public TransformFeatureStateProvider rightTransformFeature;
    [Header("Text Canvas References")]
    public TextMeshProUGUI selectedLabelText, selectedHandText, recordingText;
    [Header("Recording Preferences")]
    public int frameCount = 60;
    public float frequency = 60.0f;
    public AudioSource countdownAudioSource;
    public AudioSource recordingAudioSource;
    [Header("Joint Prefab (for debug)")]
    public GameObject jointprefab;

    private List<Dictionary<HandJointId, Vector3>> _recordedFrames = new List<Dictionary<HandJointId, Vector3>>();
    private List<Vector3> _recordedHandDeltas = new List<Vector3>();
    private List<Dictionary<string, Vector3>> _recordedSoCJ = new List<Dictionary<string, Vector3>>();
    private List<Dictionary<TransformFeature, float>> _recordedTransformFeatures = new List<Dictionary<TransformFeature, float>>();
    private Vector3 _lastHandPosition;
    private GameObject[] _jointObjects = new GameObject[18];
    private TransformConfig transformConfig;

    private string selectedLabel = "-";
    private bool isLeftHandSelected = false;
    
    private static HandJointId[] jointList = new[]
    {
        HandJointId.HandStart,
        HandJointId.HandThumb0,
        HandJointId.HandThumb1,
        HandJointId.HandThumb2,
        HandJointId.HandIndex1,
        HandJointId.HandIndex2,
        HandJointId.HandMiddle1,
        HandJointId.HandMiddle2,
        HandJointId.HandRing1,
        HandJointId.HandRing2,
        HandJointId.HandPinky0,
        HandJointId.HandPinky1,
        HandJointId.HandPinky2,
        HandJointId.HandThumb3,
        HandJointId.HandIndex3,
        HandJointId.HandMiddle3,
        HandJointId.HandRing3,
        HandJointId.HandPinky3
    };

    void Start()
    {
        // InitializeHandJointPositions(leftHand, _leftHandJointPositions);
        // InitializeHandJointPositions(rightHand, _rightHandJointPositions);
        SetSelectedHand(false);
        SetSelectedLabel("prune");
        for (int i=0; i < 18; i++)
        {
            _jointObjects[i] = Instantiate(jointprefab, Vector3.zero, Quaternion.identity);
        }
        transformConfig = new TransformConfig();
        // rightTransformFeature.RegisterConfig(transformConfig);
        // leftTransformFeature.RegisterConfig(transformConfig);
    }

    [Button]
    public void StartRecording()
    {
        StartCoroutine(RecordHandJointPositions(frameCount, frequency, isLeftHandSelected, selectedLabel));
    }

    public void Update()
    {
        // Position the instantiated joints at the positions of the hand joints
        for (int i = 0; i < 18; i++)
        {
            HandJointId joint = jointList[i];
            if (isLeftHandSelected)
            {
                if (leftHand.GetJointPose(joint, out Pose jointPose))
                {
                    _jointObjects[i].transform.position = jointPose.position;
                }
            }
            else
            {
                if (rightHand.GetJointPose(joint, out Pose jointPose))
                {
                    _jointObjects[i].transform.position = jointPose.position;
                }
            }
        }
    }
    
    private IEnumerator RecordHandJointPositions(int frameCount, float frequency, bool isLeftHand, string label)
{
    // Select hand
    Oculus.Interaction.Input.Hand chosenHand = isLeftHand ? leftHand : rightHand;
    TransformFeatureStateProvider transformFeatureStateProvider = isLeftHand ? leftTransformFeature : rightTransformFeature;
    
    // Clear previous recordings
    _recordedFrames.Clear();
    _recordedHandDeltas.Clear();
    _recordedSoCJ.Clear();
    _recordedTransformFeatures.Clear();
    
    // Print countdown
    float interval = 1.0f / frequency;
    for(int i = 3; i>0; i--)
    {
        recordingText.text = i.ToString();
        countdownAudioSource.Play();
        yield return new WaitForSeconds(1);
    }
    recordingText.text = "Recording";
    recordingAudioSource.Play();

    chosenHand.GetJointPose(HandJointId.HandStart, out Pose pose);
    _lastHandPosition = pose.position;
    yield return new WaitForSeconds(interval);
    
    // Record hand features
    for (int i = 0; i < frameCount; i++)
    {
        // Record hand joint positions
        Dictionary<HandJointId, Vector3> frameData = new Dictionary<HandJointId, Vector3>();
        foreach (HandJointId joint in jointList)
        {
            if (chosenHand.GetJointPose(joint, out Pose jointPose))
            {
                frameData.Add(joint, jointPose.position);
            }
        }
        _recordedFrames.Add(new Dictionary<HandJointId, Vector3>(frameData));
        
        // Record hand delta
        chosenHand.GetJointPose(HandJointId.HandStart, out Pose handStartPose);
        _recordedHandDeltas.Add(handStartPose.position - _lastHandPosition);
        _lastHandPosition = handStartPose.position;
        
        // Record transform features
        Dictionary<TransformFeature, float> transformData = new Dictionary<TransformFeature, float>();
        foreach (TransformFeature tf in Enum.GetValues(typeof(TransformFeature)))
        {
            float? state = transformFeatureStateProvider.GetFeatureValue(transformConfig, tf);
            transformData.Add(tf, state ?? 0);
        }
        _recordedTransformFeatures.Add(new Dictionary<TransformFeature, float>(transformData));
        
        // Record SoCJ
        Dictionary<string, Vector3> socjData = new Dictionary<string, Vector3>();
        // Fingers
            // Thumb
            socjData.Add("thumb1", frameData[HandJointId.HandThumb1] - frameData[HandJointId.HandThumb0]);
            socjData.Add("thumb2", frameData[HandJointId.HandThumb2] - frameData[HandJointId.HandThumb1]);
            socjData.Add("thumb3", frameData[HandJointId.HandThumb3] - frameData[HandJointId.HandThumb2]);
            // Index
            socjData.Add("index1", frameData[HandJointId.HandIndex1] - frameData[HandJointId.HandThumb0]);
            socjData.Add("index2", frameData[HandJointId.HandIndex2] - frameData[HandJointId.HandIndex1]);
            socjData.Add("index3", frameData[HandJointId.HandIndex3] - frameData[HandJointId.HandIndex2]);
            // Middle
            socjData.Add("middle1", frameData[HandJointId.HandMiddle1] - frameData[HandJointId.HandThumb0]);
            socjData.Add("middle2", frameData[HandJointId.HandMiddle2] - frameData[HandJointId.HandMiddle1]);
            socjData.Add("middle3", frameData[HandJointId.HandMiddle3] - frameData[HandJointId.HandMiddle2]);
            // Ring
            socjData.Add("ring1", frameData[HandJointId.HandRing1] - frameData[HandJointId.HandThumb0]);
            socjData.Add("ring2", frameData[HandJointId.HandRing2] - frameData[HandJointId.HandRing1]);
            socjData.Add("ring3", frameData[HandJointId.HandRing3] - frameData[HandJointId.HandRing2]);
            // Pinky
            socjData.Add("pinky1", frameData[HandJointId.HandPinky1] - frameData[HandJointId.HandThumb0]);
            socjData.Add("pinky2", frameData[HandJointId.HandPinky2] - frameData[HandJointId.HandPinky1]);
            socjData.Add("pinky3", frameData[HandJointId.HandPinky3] - frameData[HandJointId.HandPinky2]);
        // Knuckles
            // 2nd
            socjData.Add("second1", frameData[HandJointId.HandThumb2] - frameData[HandJointId.HandIndex2]);
            socjData.Add("second2", frameData[HandJointId.HandIndex2] - frameData[HandJointId.HandMiddle2]);
            socjData.Add("second3", frameData[HandJointId.HandMiddle2] - frameData[HandJointId.HandRing2]);
            socjData.Add("second4", frameData[HandJointId.HandRing2] - frameData[HandJointId.HandPinky2]);
            // 3rd
            socjData.Add("third1", frameData[HandJointId.HandThumb3] - frameData[HandJointId.HandIndex3]);
            socjData.Add("third2", frameData[HandJointId.HandIndex3] - frameData[HandJointId.HandMiddle3]);
            socjData.Add("third3", frameData[HandJointId.HandMiddle3] - frameData[HandJointId.HandRing3]);
            socjData.Add("third4", frameData[HandJointId.HandRing3] - frameData[HandJointId.HandPinky3]);
        // Add frame
        _recordedSoCJ.Add(new Dictionary<string, Vector3>(socjData));
        
        Debug.Log($"Recorded frame {i + 1}/{frameCount}");
        yield return new WaitForSeconds(interval); // Wait for the next frame based on the frequency
    }
    
    recordingText.text = "Done";
    WriteToCsv(label);
    NewWriteToCsv(label);
}

private void WriteToCsv(string label)
{
    string directoryPath = Application.persistentDataPath;
    string[] existingFiles = Directory.GetFiles(directoryPath, "HandJointPositions_*.csv");
    int maxSequence = 0;

    foreach (string file in existingFiles)
    {
        string fileName = Path.GetFileNameWithoutExtension(file);
        string sequenceStr = fileName.Replace("HandJointPositions_", "");
        if (int.TryParse(sequenceStr, out int sequence))
        {
            if (sequence > maxSequence)
            {
                maxSequence = sequence;
            }
        }
    }

    string newFileName = $"HandJointPositions_{maxSequence + 1}.csv";
    string filePath = Path.Combine(directoryPath, newFileName);

    using (StreamWriter writer = new StreamWriter(filePath))
    {
        // Write header
        writer.WriteLine("Frame,Joint,PositionX,PositionY,PositionZ,Label,Hand");

        for (int frameIndex = 0; frameIndex < _recordedFrames.Count; frameIndex++)
        {
            foreach (var jointData in _recordedFrames[frameIndex])
            {
                string line = string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "{0},{1},{2},{3},{4},{5},{6}",
                    frameIndex,
                    jointData.Key,
                    jointData.Value.x,
                    jointData.Value.y,
                    jointData.Value.z,
                    label,
                    isLeftHandSelected ? "left" : "right"
                );
                //Debug.Log(line);
                writer.WriteLine(line);
            }
        }
    }
    Debug.Log($"CSV file written to: {filePath} with {_recordedFrames.Count} frames");
}

private void NewWriteToCsv(string label)
{
    string directoryPath = Application.persistentDataPath;
    string[] existingFiles = Directory.GetFiles(directoryPath, "HandFeatures_*.csv");
    int maxSequence = 0;

    foreach (string file in existingFiles)
    {
        string fileName = Path.GetFileNameWithoutExtension(file);
        string sequenceStr = fileName.Replace("HandFeatures_", "");
        if (int.TryParse(sequenceStr, out int sequence))
        {
            if (sequence > maxSequence)
            {
                maxSequence = sequence;
            }
        }
    }

    string newFileName = $"HandFeatures_{maxSequence + 1}.csv";
    string filePath = Path.Combine(directoryPath, newFileName);

    using (StreamWriter writer = new StreamWriter(filePath))
    {
        // Write header
        writer.WriteLine("Frame,Hand,Label," +
                         // Hand delta
                         "handDeltaX,handDeltaY,handDeltaZ," +
                         // SoCJ
                         "thumb1X,thumb1Y,thumb1Z,thumb2X,thumb2Y,thumb2Z,thumb3X,thumb3Y,thumb3Z," +
                         "index1X,index1Y,index1Z,index2X,index2Y,index2Z,index3X,index3Y,index3Z," +
                         "middle1X,middle1Y,middle1Z,middle2X,middle2Y,middle2Z,middle3X,middle3Y,middle3Z," +
                         "ring1X,ring1Y,ring1Z,ring2X,ring2Y,ring2Z,ring3X,ring3Y,ring3Z," +
                         "pinky1X,pinky1Y,pinky1Z,pinky2X,pinky2Y,pinky2Z,pinky3X,pinky3Y,pinky3Z," +
                         "second1X,second1Y,second1Z,second2X,second2Y,second2Z,second3X,second3Y,second3Z,second4X,second4Y,second4Z," +
                         "third1X,third1Y,third1Z,third2X,third2Y,third2Z,third3X,third3Y,third3Z,third4X,third4Y,third4Z," +
                         // Transform features
                         "wristUp,wristDown,palmDown,palmUp,palmTowardsFace,palmAwayFromFace,fingersUp,fingersDown,pinchClear");
        
        for (int frameIndex = 0; frameIndex < _recordedFrames.Count; frameIndex++)
        {
            Debug.Log("New csv loop " + frameIndex);
            
            var socj = _recordedSoCJ[frameIndex];
            var transformFeatures = _recordedTransformFeatures[frameIndex];
            
            string formatString = "";
            for (int i = 0; i < 84; i++)
            {
                formatString += "{" + i + "},";
            }
            formatString = formatString.TrimEnd(',');
            
            string line = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                formatString,
                frameIndex,
                isLeftHandSelected ? "left" : "right",
                label,
                _recordedHandDeltas[frameIndex].x,
                _recordedHandDeltas[frameIndex].y,
                _recordedHandDeltas[frameIndex].z,
                socj["thumb1"].x, socj["thumb1"].y, socj["thumb1"].z,
                socj["thumb2"].x, socj["thumb2"].y, socj["thumb2"].z,
                socj["thumb3"].x, socj["thumb3"].y, socj["thumb3"].z,
                socj["index1"].x, socj["index1"].y, socj["index1"].z,
                socj["index2"].x, socj["index2"].y, socj["index2"].z,
                socj["index3"].x, socj["index3"].y, socj["index3"].z,
                socj["middle1"].x, socj["middle1"].y, socj["middle1"].z,
                socj["middle2"].x, socj["middle2"].y, socj["middle2"].z,
                socj["middle3"].x, socj["middle3"].y, socj["middle3"].z,
                socj["ring1"].x, socj["ring1"].y, socj["ring1"].z,
                socj["ring2"].x, socj["ring2"].y, socj["ring2"].z,
                socj["ring3"].x, socj["ring3"].y, socj["ring3"].z,
                socj["pinky1"].x, socj["pinky1"].y, socj["pinky1"].z,
                socj["pinky2"].x, socj["pinky2"].y, socj["pinky2"].z,
                socj["pinky3"].x, socj["pinky3"].y, socj["pinky3"].z,
                socj["second1"].x, socj["second1"].y, socj["second1"].z,
                socj["second2"].x, socj["second2"].y, socj["second2"].z,
                socj["second3"].x, socj["second3"].y, socj["second3"].z,
                socj["second4"].x, socj["second4"].y, socj["second4"].z,
                socj["third1"].x, socj["third1"].y, socj["third1"].z,
                socj["third2"].x, socj["third2"].y, socj["third2"].z,
                socj["third3"].x, socj["third3"].y, socj["third3"].z,
                socj["third4"].x, socj["third4"].y, socj["third4"].z,
                transformFeatures[TransformFeature.WristUp],
                transformFeatures[TransformFeature.WristDown],
                transformFeatures[TransformFeature.PalmDown],
                transformFeatures[TransformFeature.PalmUp],
                transformFeatures[TransformFeature.PalmTowardsFace],
                transformFeatures[TransformFeature.PalmAwayFromFace],
                transformFeatures[TransformFeature.FingersUp],
                transformFeatures[TransformFeature.FingersDown],
                transformFeatures[TransformFeature.PinchClear]
            );

            Debug.Log(line);
            writer.WriteLine(line);
        }
    }
    Debug.Log($"CSV file written to: {filePath} with {_recordedFrames.Count} frames");
}
    
    #region Label/hand selection

    [Button]
    public void SetSelectedHand(bool selectLeft)
    {
        selectedHandText.text = selectLeft ? "Left" : "Right";
        isLeftHandSelected = selectLeft;
    }
    
    [Button]
    public void SetSelectedLabel(string label)
    {
        selectedLabel = label;
        selectedLabelText.text = label;
    }
    
    #endregion
    
}