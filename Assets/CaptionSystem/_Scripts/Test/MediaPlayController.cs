using UnityEngine;
using UnityEngine.Video;  // only needed if you use VideoPlayer

public class MediaPlayController: MonoBehaviour
{
    [Header("Player")]
    public string playerTag = "Player";
    private Transform player;

    [Header("Media")]
    public AudioSource audioSource;
    public VideoPlayer videoPlayer;

    private Collider zoneCollider;
    private bool isPlayerInside = false;

    void Awake()
    {
        zoneCollider = GetComponent<Collider>();
        if (zoneCollider == null)
        {
            Debug.LogError("MediaPlayer needs a Collider on the same GameObject.");
        }
    }

    void Start()
    {
        // optional: try to find player by tag at start
        GameObject p = GameObject.FindGameObjectWithTag(playerTag);
        if (p != null) player = p.transform;
    }

    void Update()
    {
        // This part is for teleports:
        // if we have a player, check if they're inside the bounds even if OnTriggerEnter didn't fire.
        if (player != null && zoneCollider != null)
        {
            bool currentlyInside = zoneCollider.bounds.Contains(player.position);

            if (currentlyInside && !isPlayerInside)
            {
                // teleported in
                isPlayerInside = true;
                PlayMedia();
            }
            else if (!currentlyInside && isPlayerInside)
            {
                // teleported out
                isPlayerInside = false;
                StopMedia();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            player = other.transform;
            isPlayerInside = true;
            PlayMedia();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerInside = false;
            StopMedia();
        }
    }

    private void PlayMedia()
    {
        if (audioSource != null)
        {
            if (!audioSource.isPlaying)
                audioSource.Play();
        }

        if (videoPlayer != null)
        {
            // if you want resume instead of restart:
            if (videoPlayer.isPaused)
                videoPlayer.Play();
            else if (!videoPlayer.isPlaying)
                videoPlayer.Play();
        }
    }

    private void StopMedia()
    {
        if (audioSource != null)
        {
            // choose Stop vs Pause
            audioSource.Pause();  // or audioSource.Stop();
        }

        if (videoPlayer != null)
        {
            videoPlayer.Pause();  // or videoPlayer.Stop();
        }
    }
}
