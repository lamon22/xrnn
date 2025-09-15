using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuButtonTest : MonoBehaviour
{
    void Update()
    {
        if (OVRInput.GetUp(OVRInput.Button.Start, OVRInput.Controller.Active))
        {
            Debug.Log("Enabled voice");
        }
    }
}

