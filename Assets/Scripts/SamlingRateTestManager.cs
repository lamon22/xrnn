using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SamlingRateTestManager : MonoBehaviour
{
    public int samplesInOneSecond = 10;
    
    [SerializeField] private TextMeshProUGUI frameRate;
    [SerializeField] private TextMeshProUGUI frameCount;
    
    int frameCounter = 0;
    
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(CountFrames());
        frameRate.text = "Frame Rate: 1/" + samplesInOneSecond;
    }


    IEnumerator CountFrames()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f / samplesInOneSecond);
            frameCounter++;
            frameCounter = frameCounter % samplesInOneSecond;

            frameCount.text = frameCounter + "";
        }
    }
}
