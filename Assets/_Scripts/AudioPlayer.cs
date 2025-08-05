using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioPlayer : MonoBehaviour
{
    [Header("Audio Setup")]
    public AudioSource audioSource;
    public AudioClip audioClip;


    private void Awake()
    {
        // auto assign audio source if not assigned
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
    }
    
    public void PlayClip()
    {
        if (audioClip && audioSource)
        {
            audioSource.PlayOneShot(audioClip);
        }
    }
}
