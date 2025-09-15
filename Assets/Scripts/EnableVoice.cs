using System.Collections;
using System.Collections.Generic;
using Oculus.Voice;
using UnityEngine;

public class EnableVoice : MonoBehaviour
{
    public AppVoiceExperience voiceExperience;
    public AudioSource openMicSound;
    
    // Update is called once per frame
    void Update()
    {
        if (OVRInput.GetUp(OVRInput.Button.Start, OVRInput.Controller.Active))
        {
            Debug.Log("Enabled voice");
            voiceExperience.Activate();
            openMicSound.Play();
        }
        
    }
}
