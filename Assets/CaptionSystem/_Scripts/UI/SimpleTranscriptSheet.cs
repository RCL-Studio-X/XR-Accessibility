using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Simple transcript display - no auto-scroll, no highlighting, just shows all captions
/// </summary>
[RequireComponent(typeof(Canvas))]
public class SimpleTranscriptSheet : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform contentContainer;
    [SerializeField] private GameObject transcriptEntryPrefab;

    [Header("Display Settings")]
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.3f);

    [Header("Auto-Detection")]
    [SerializeField] private bool autoDetectPlayingAudio = true;
    [SerializeField] private AudioSource manualAudioSource;
    [SerializeField] private float detectionCheckInterval = 0.5f;

    private Canvas canvas;
    private List<CaptionEntry> captions = new List<CaptionEntry>();
    private List<TranscriptEntryUI> entryUIElements = new List<TranscriptEntryUI>();

    private AudioSource trackedAudioSource;
    private AudioClip lastTrackedClip;
    private float nextDetectionCheckTime = 0f;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();

        if (scrollRect == null)
        {
            scrollRect = GetComponentInChildren<ScrollRect>();
        }

        if (contentContainer == null && scrollRect != null)
        {
            contentContainer = scrollRect.content;
        }
    }

    private void Start()
    {
        if (canvas.gameObject.activeInHierarchy)
        {
            if (manualAudioSource != null)
            {
                LoadTranscriptForAudioSource(manualAudioSource);
            }
            else if (autoDetectPlayingAudio)
            {
                TryAutoDetectAudioSource();
            }
        }
    }

    private void Update()
    {
        if (autoDetectPlayingAudio && Time.time >= nextDetectionCheckTime)
        {
            CheckForAudioChanges();
            nextDetectionCheckTime = Time.time + detectionCheckInterval;
        }
    }

    #region Auto-Detection

    private void CheckForAudioChanges()
    {
        if (manualAudioSource != null)
        {
            if (trackedAudioSource != manualAudioSource || lastTrackedClip != manualAudioSource.clip)
            {
                LoadTranscriptForAudioSource(manualAudioSource);
            }
            return;
        }

        if (trackedAudioSource != null)
        {
            if (!trackedAudioSource.isPlaying || trackedAudioSource.clip != lastTrackedClip)
            {
                TryAutoDetectAudioSource();
            }
        }
        else
        {
            TryAutoDetectAudioSource();
        }
    }

    private void TryAutoDetectAudioSource()
    {
        if (GlobalCaptionManager.Instance == null) return;

        var database = GlobalCaptionManager.Instance.GetCaptionDatabase();
        if (database == null) return;

        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();

        foreach (var audioSource in allAudioSources)
        {
            if (audioSource.isPlaying && audioSource.clip != null)
            {
                if (database.HasCaptionForClip(audioSource.clip))
                {
                    LoadTranscriptForAudioSource(audioSource);
                    return;
                }
            }
        }
    }

    #endregion

    #region Transcript Loading

    public void LoadTranscriptForAudioSource(AudioSource audioSource)
    {
        if (audioSource == null) return;

        if (trackedAudioSource == audioSource && lastTrackedClip == audioSource.clip)
        {
            return;
        }

        trackedAudioSource = audioSource;
        lastTrackedClip = audioSource.clip;

        if (GlobalCaptionManager.Instance != null)
        {
            var database = GlobalCaptionManager.Instance.GetCaptionDatabase();
            if (database != null)
            {
                var entry = database.GetEntryForClip(audioSource.clip);
                if (entry != null && entry.srtFile != null)
                {
                    LoadTranscript(entry.srtFile);
                    return;
                }
            }
        }
    }

    public void LoadTranscript(TextAsset srtFile)
    {
        if (srtFile == null) return;

        captions = SRTParser.ParseSRT(srtFile.text);
        ClearTranscriptUI();
        CreateTranscriptUI();
    }

    #endregion

    #region UI Creation

    private void CreateTranscriptUI()
    {
        if (contentContainer == null || transcriptEntryPrefab == null) return;

        for (int i = 0; i < captions.Count; i++)
        {
            GameObject entryObj = Instantiate(transcriptEntryPrefab, contentContainer);
            TranscriptEntryUI entryUI = entryObj.GetComponent<TranscriptEntryUI>();

            if (entryUI == null)
            {
                entryUI = entryObj.AddComponent<TranscriptEntryUI>();
            }

            entryUI.Setup(captions[i], i, textColor, backgroundColor);
            entryUIElements.Add(entryUI);
        }
    }

    private void ClearTranscriptUI()
    {
        foreach (var entry in entryUIElements)
        {
            if (entry != null)
            {
                Destroy(entry.gameObject);
            }
        }

        entryUIElements.Clear();
    }

    #endregion

    #region Public API

    public void SetManualAudioSource(AudioSource audioSource)
    {
        manualAudioSource = audioSource;
        LoadTranscriptForAudioSource(audioSource);
    }

    public void ClearTranscript()
    {
        ClearTranscriptUI();
        captions.Clear();
        trackedAudioSource = null;
        lastTrackedClip = null;
    }

    #endregion
}