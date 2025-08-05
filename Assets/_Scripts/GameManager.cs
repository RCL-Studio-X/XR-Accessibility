using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using HurricaneVR.Framework.Core.Player;
using Oculus.Platform;
using UnityEngine.Rendering.Universal;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("== Scene References ==")]
    [Tooltip("Fog controller for transitions.")]
    public FogTransitionController fogController;

    [Tooltip("Receiver object for beeswax.")]
    public GameObject beeswaxReceiver;

    [Tooltip("Rock spawning systems.")]
    public GameObject rockSpawners;

    [Tooltip("Island spawning systems.")]
    public GameObject islandSpawners;

    [Tooltip("Swimming Siren Spawners.")]
    public GameObject swimmingSirenSpawners;
    
    [Tooltip("Handles fade transitions.")]
    public ScreenFadeController fadeController;

    [Tooltip("Player XR rig root.")]
    public GameObject playerController;

    public GameObject PhysicsLeftHand;
    public GameObject PhysicsRightHand;
    
    [Tooltip("Teleport destination in tied-up state.")]
    public Transform tiedUpPos;

    [Header("== Task Objects ==")]
    [Tooltip("The letter GameObject the player reads.")]
    public GameObject letter;

    [Tooltip("The sword object used to cut wax.")]
    public GameObject sword;

    [Tooltip("The wax object.")]
    public GameObject wax;

    [Tooltip("Manages wax state logic.")]
    public WaxStateManager waxStateManager;
    
    [Tooltip("Audio source for crew dialogue.")]
    public AudioSource dialogueAS;

    [Header("==Audio Settings==")]
    [Tooltip("Sound effect for task completion and objective UI change")]
    public AudioSource taskCompleteAS;
    public AudioClip taskCompleteClip;
    //TODO: Add VFX to the task completion and UI Change 

    [Header("== Fog Timing ==")]
    [Tooltip("Delay before fog transition in State 1.")]
    public float fogDelay = 5f;

    [Header("== Debug/State Info ==")]
    [Tooltip("Current game state (debug/testing).")]
    [SerializeField] private int _currentState = -1;

    // runtime created references
    private Camera uiCamera;

    private Vector3 _letterInitialPosition;
    private Quaternion _letterInitialRotation;

    private Vector3 _swordInitialPosition;
    private Quaternion _swordInitialRotation;

    private Vector3 _waxInitialPosition;
    private Quaternion _waxInitialRotation;

    private int _currentTask = 0;

    private readonly string[] _taskName =
    {
        "Find and read the letter",
        "Cut the wax with the sword",
        "Soften the wax",
        "Give the wax to the crew"
    };

    private HVRPlayerController _playerController;
    private HVRTeleporter _teleporter;

    // Store player movement settings to restore between states
    private bool _canJump, _canSprint, _canCrouch, _instantAccel;
    private float _accel, _turnSpeed, _snapAmount;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        CreateUICamera();
    }

    private void Start()
    {
        if (!ValidateSceneReferences())
        {
            Debug.LogError("GameManager missing required components. Check console for details.");
            return;
        }

        RecordInitialValues();
        SetGameState(0);
        UpdateCurrentObjective();
    }

    // UI layer setup
    //void CreateUICamera()
    //{
    //    // Find main camera
    //    Camera mainCamera = Camera.main;

    //    // Create UI camera as child of main camera
    //    GameObject uiCameraObj = new GameObject("UI Camera");
    //    uiCameraObj.transform.SetParent(mainCamera.transform);
    //    uiCameraObj.transform.localPosition = Vector3.zero;
    //    uiCameraObj.transform.localRotation = Quaternion.identity;

    //    uiCamera = uiCameraObj.AddComponent<Camera>();

    //    // Configure UI camera
    //    uiCamera.cullingMask = 1 << LayerMask.NameToLayer("UI"); // Only render UI layer
    //    uiCamera.depth = mainCamera.depth + 10; // Render after main camera
    //    uiCamera.clearFlags = CameraClearFlags.Nothing; // Don't clear color
    //    uiCamera.orthographic = false; // Keep perspective
    //}

    void CreateUICamera()
    {
        Camera mainCamera = Camera.main;

        // Configure main camera as base
        var mainCameraData = mainCamera.GetUniversalAdditionalCameraData();
        mainCameraData.renderType = CameraRenderType.Base;

        // Create overlay camera
        GameObject uiCameraObj = new GameObject("UI Camera");
        uiCameraObj.transform.SetParent(mainCamera.transform);
        uiCameraObj.transform.localPosition = Vector3.zero;
        uiCameraObj.transform.localRotation = Quaternion.identity;

        uiCamera = uiCameraObj.AddComponent<Camera>();
        uiCamera.cullingMask = 1 << LayerMask.NameToLayer("UI");

        // Configure as overlay camera
        var uiCameraData = uiCamera.GetUniversalAdditionalCameraData();
        uiCameraData.renderType = CameraRenderType.Overlay;

        // Add to camera stack
        mainCameraData.cameraStack.Add(uiCamera);
    }

    private bool ValidateSceneReferences()
    {
        bool valid = true;

        if (!letter) { Debug.LogError("Letter GameObject not assigned."); valid = false; }
        if (!fogController) { Debug.LogError("FogController not assigned."); valid = false; }
        if (!beeswaxReceiver) { Debug.LogError("BeeswaxReceiver not assigned."); valid = false; }
        if (!fadeController) { Debug.LogError("FadeController not assigned."); valid = false; }
        if (!playerController) { Debug.LogError("PlayerController not assigned."); valid = false; }
        if (!PhysicsLeftHand) { Debug.LogError("PhysicsLeftHand not assigned."); valid = false; }
        if (!PhysicsRightHand) { Debug.LogError("PhysicsRightHand not assigned."); valid = false; }
        if (!tiedUpPos) { Debug.LogError("TiedUpPos transform not assigned."); valid = false; }
        if (!sword) { Debug.LogError("Sword object not assigned."); valid = false; }
        if (!wax) { Debug.LogError("Wax object not assigned."); valid = false; }

        return valid;
    }

    private void RecordInitialValues()
    {
        if (letter)
        {
            _letterInitialPosition = letter.transform.position;
            _letterInitialRotation = letter.transform.rotation;
        }

        if (sword)
        {
            _swordInitialPosition = sword.transform.position;
            _swordInitialRotation = sword.transform.rotation;
        }

        if (wax)
        {
            _waxInitialPosition = wax.transform.position;
            _waxInitialRotation = wax.transform.rotation;
        }

        _playerController = playerController.GetComponent<HVRPlayerController>();
        _teleporter = playerController.GetComponent<HVRTeleporter>();

        if (!_playerController)
        {
            Debug.LogError("Missing HVRPlayerController on playerController.");
        }

        if (!_teleporter)
        {
            Debug.LogError("Missing HVRTeleporter on playerController.");
        }

    }

    private void PlaySFX(AudioSource audioSource, AudioClip audioClip)
    {
        if (audioSource != null && audioClip != null)
        {
            audioSource.PlayOneShot(audioClip);
        }
    }

    #region Game State Management

    public void SetGameState(int state)
    {
        if (state == _currentState) return;

        StopAllCoroutines();
        _currentState = state;

        switch (state)
        {
            case 0: EnterBaseState(); break;
            case 1: StartCoroutine(EnterState1()); break;
            case 2: StartCoroutine(EnterState2()); break;
            default: Debug.LogWarning("Unknown game state: " + state); break;
        }
    }

    private void EnterBaseState()
    {

        PhysicsLeftHand.SetActive(true);
        PhysicsRightHand.SetActive(true);
        // Reset object transforms
        if (letter)
        {
            letter.SetActive(true);
            letter.transform.SetPositionAndRotation(_letterInitialPosition, _letterInitialRotation);
        }

        if (sword)
        {
            sword.transform.SetPositionAndRotation(_swordInitialPosition, _swordInitialRotation);
        }

        if (wax)
        {
            wax.transform.SetPositionAndRotation(_waxInitialPosition, _waxInitialRotation);
        }

        fogController.fogAmount = 0f;
        waxStateManager.stabbable.enabled = false;
        beeswaxReceiver.SetActive(false);
        rockSpawners.SetActive(true);
        islandSpawners.SetActive(false);
        swimmingSirenSpawners.SetActive(false);


        // Restore movement
        if (_playerController)
        {
            _playerController.enabled = true;
        }

        if (_teleporter)
            _teleporter.enabled = true;
    }

    private IEnumerator EnterState1()
    {
        PhysicsLeftHand.SetActive(true);
        PhysicsRightHand.SetActive(true);
        waxStateManager.stabbable.enabled = true;
        fogController.SetFogClear();
        rockSpawners.SetActive(true);
        islandSpawners.SetActive(false);
        swimmingSirenSpawners.SetActive(false);
        if (letter) letter.SetActive(false);
        beeswaxReceiver.SetActive(true);
        if (_playerController)
        {
            _playerController.enabled = true;
        }

        yield return new WaitForSeconds(fogDelay);

        fogController.ToggleFog();
    }

    private IEnumerator EnterState2()
    {
        PhysicsLeftHand.SetActive(false);
        PhysicsRightHand.SetActive(false);
        CentralUIManager.Instance?.HideUI(CentralUIManager.Instance.GetObjectiveUI());
        StartCoroutine(StartConversation());
        rockSpawners.SetActive(false);
        islandSpawners.SetActive(true);
        swimmingSirenSpawners.SetActive(true);

        fogController.SetFogFoggy();
        fadeController.FadeTo(1f, fadeController.fadeDuration);
        yield return new WaitForSeconds(fadeController.fadeDuration);

        if (_teleporter)
        {
            _teleporter.Teleport(tiedUpPos.position, Vector3.right);
            _teleporter.enabled = false;
        }

        if (_playerController)
        {
            _playerController.enabled = false;
        }

        fadeController.FadeTo(0f, fadeController.fadeDuration);
    }

    #endregion

    #region Task Flow

    private void ToNextTask()
    {
        _currentTask++;
        UpdateCurrentObjective();
    }

    private void UpdateCurrentObjective()
    {
        if (_currentTask >= _taskName.Length) return;

        if (CentralUIManager.Instance != null)
            CentralUIManager.Instance.UpdateObjectiveText(_taskName[_currentTask]);
        
        if (taskCompleteAS != null && taskCompleteClip != null)
        {
            PlaySFX(taskCompleteAS, taskCompleteClip);
        }
    }

    public void OnLetterPickedUp()
    {
        if (_currentState != 0)
            return;
        SetGameState(1);
        ToNextTask();
        if (letter) letter.SetActive(false);
    }

    public void OnWaxCut()
    {
        if (_currentState == 1)
            ToNextTask();
    }

    public void OnWaxSoften()
    {
        if (_currentState == 1)
            ToNextTask();
    }

    public void OnWaxToCrew(GameObject other)
    {
        if(_currentState != 1)
            return;

        if (!waxStateManager.IsAtFinalWaxModel())
            return;

        other.SetActive(false);
        SetGameState(2);
    }

    private IEnumerator StartConversation()
    {
        yield return new WaitForSeconds(fadeController.fadeDuration - 0.1f);
        if (dialogueAS) dialogueAS.Play();

        //Wait for the dialogue completion
        StartCoroutine(WaitForAudioEnd());
    }

    private IEnumerator WaitForAudioEnd()
    {
        while (dialogueAS.isPlaying)
        {
            yield return new WaitForSeconds(0.5f);
        }

        StartCoroutine(ShowEndingSequence());
    }

    private IEnumerator ShowEndingSequence()
    {
        // Fade scene to black
        if (fadeController)
        {
            fadeController.FadeTo(1f, fadeController.fadeDuration);
            yield return new WaitForSeconds(fadeController.fadeDuration);
        }

        // Switch UI camera to black background (invisible switch since already black)
        //if (uiCamera)
        //{
        //    uiCamera.clearFlags = CameraClearFlags.SolidColor;
        //    uiCamera.backgroundColor = Color.black;
        //    uiCamera.cullingMask = 1 << LayerMask.NameToLayer("UI");
        //}

        // Show ending UI
        if (CentralUIManager.Instance != null)
        {
            CentralUIManager.Instance.ShowUI(CentralUIManager.Instance.endingUI);
        }

        // Fade screen overlay back to transparent (reveals black UI camera background)
        //if (fadeController)
        //{
        //    fadeController.FadeTo(0f, fadeController.fadeDuration);
        //}

        Debug.Log("Ending sequence started");
    }

    #endregion
}
