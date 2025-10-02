using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioSwapperTest : MonoBehaviour
{
    public AudioClip[] clips;
    public AudioSource audioSource;
    private int currIdx = 0;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            audioSource.clip = clips[currIdx];
            audioSource.Play();
            currIdx = (currIdx + 1) % clips.Length;
        }
    }
}
