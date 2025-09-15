using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Oculus.Interaction.Input;
using Oculus.Interaction.PoseDetection;
using TMPro;
using UnityEngine;
using UnityEngine.XR;

public class HandJointRecorder : MonoBehaviour
{
    [Header("Hand References")]
    public Oculus.Interaction.Input.Hand  leftHand;
    public Oculus.Interaction.Input.Hand rightHand;
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
    private GameObject[] _jointObjects = new GameObject[18];

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
    }

    public void StartRecording()
    {
        //StartCoroutine(RecordHandJointPositions(frameCount, frequency, isLeftHandSelected, selectedLabel));
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
    // Dictionary<HandJointId, Vector3> jointPositions = isLeftHand ? _leftHandJointPositions : _rightHandJointPositions;
    Oculus.Interaction.Input.Hand chosenHand = isLeftHand ? leftHand : rightHand;
    _recordedFrames.Clear();
    float interval = 1.0f / frequency;

    for(int i = 3; i>0; i--)
    {
        recordingText.text = i.ToString();
        countdownAudioSource.Play();
        yield return new WaitForSeconds(1);
    }
    recordingText.text = "Recording";
    recordingAudioSource.Play();
    
    for (int i = 0; i < frameCount; i++)
    {
        Dictionary<HandJointId, Vector3> frameData = new Dictionary<HandJointId, Vector3>();
        
        foreach (HandJointId joint in jointList)
        {
            if (chosenHand.GetJointPose(joint, out Pose jointPose))
            {
                Debug.Log("Writing: " + joint + " " + jointPose.position);
                frameData.Add(joint, jointPose.position);
                //frameData[joint] = jointPose.position;
            }
        }

        _recordedFrames.Add(new Dictionary<HandJointId, Vector3>(frameData));
        Debug.Log($"Recorded frame {i + 1}/{frameCount}");
        yield return new WaitForSeconds(interval); // Wait for the next frame based on the frequency
    }
    recordingText.text = "Done";
    WriteToCsv(label);
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
        writer.WriteLine("Frame,Joint,PositionX,PositionY,PositionZ,Label");

        for (int frameIndex = 0; frameIndex < _recordedFrames.Count; frameIndex++)
        {
            foreach (var jointData in _recordedFrames[frameIndex])
            {
                string line = string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "{0},{1},{2},{3},{4},{5}",
                    frameIndex,
                    jointData.Key,
                    jointData.Value.x,
                    jointData.Value.y,
                    jointData.Value.z,
                    label
                );
                Debug.Log(line);
                writer.WriteLine(line);
            }
        }
    }
    Debug.Log($"CSV file written to: {filePath} with {_recordedFrames.Count} frames");
}
    
    
    #region Label/hand selection

    public void SetSelectedHand(bool selectLeft)
    {
        selectedHandText.text = selectLeft ? "Left" : "Right";
        isLeftHandSelected = selectLeft;
    }
    
    public void SetSelectedLabel(string label)
    {
        selectedLabel = label;
        selectedLabelText.text = label;
    }
    
    #endregion
    
}