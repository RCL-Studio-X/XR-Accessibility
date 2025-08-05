/*// TransitionOverlay.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class Transition : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject overlayPanel;
    public Image backgroundImage;
    public Image gameIcon;
    public TextMeshProUGUI tipsText;
    public Image loadingSpinner; // Optional

    [Header("Game Tips")]
    public string[] gameTips = {
        "Use teleportation to move around efficiently",
        "Look around to find hidden items",
        "Grab objects with your controllers",
        "Check your inventory regularly",
        "Save your progress frequently"
    };

    [Header("Settings")]
    public float tipDisplayDuration = 2.5f;
    public float fadeInSpeed = 3f;
    public float fadeOutSpeed = 2f;

    private static Transition instance;
    private Coroutine currentTransition;
    private int currentTipIndex = 0;

    void Awake()
    {
        // Singleton pattern - persist across scenes
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            // Initially hidden
            overlayPanel.SetActive(false);

            // Set up for VR
            SetupForVR();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void SetupForVR()
    {
        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // Ensure it's on top

        // Make sure it covers everything
        backgroundImage.color = Color.black;
    }

    public static void StartTransition(string targetScene)
    {
        if (instance != null)
        {
            if (instance.currentTransition != null)
                instance.StopCoroutine(instance.currentTransition);

            instance.currentTransition = instance.StartCoroutine(instance.TransitionToScene(targetScene));
        }
    }

    IEnumerator TransitionToScene(string targetScene)
    {
        // Step 1: Show overlay immediately
        overlayPanel.SetActive(true);
        yield return StartCoroutine(FadeInOverlay());

        // Step 2: Start loading target scene in background
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetScene);
        asyncLoad.allowSceneActivation = false;

        // Step 3: Show tips while loading
        Coroutine tipsRoutine = StartCoroutine(ShowTipsWhileLoading(asyncLoad));

        // Step 4: Wait for loading to complete
        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        // Step 5: Ensure minimum display time for smooth experience
        yield return new WaitForSeconds(0.5f);

        // Step 6: Stop showing tips
        StopCoroutine(tipsRoutine);

        // Step 7: Activate the new scene
        asyncLoad.allowSceneActivation = true;

        // Step 8: Wait for scene to be fully loaded
        yield return new WaitUntil(() => asyncLoad.isDone);

        // Step 9: Small delay to ensure scene is ready
        yield return new WaitForSeconds(0.2f);

        // Step 10: Fade out overlay
        yield return StartCoroutine(FadeOutOverlay());

        // Step 11: Hide overlay
        overlayPanel.SetActive(false);

        currentTransition = null;
    }

    IEnumerator FadeInOverlay()
    {
        // Instant show with fade in
        backgroundImage.color = new Color(0, 0, 0, 0);
        gameIcon.color = new Color(1, 1, 1, 0);
        tipsText.color = new Color(1, 1, 1, 0);

        float timer = 0;
        while (timer < 1f)
        {
            timer += Time.unscaledDeltaTime * fadeInSpeed;
            float alpha = Mathf.Clamp01(timer);

            backgroundImage.color = new Color(0, 0, 0, alpha);
            gameIcon.color = new Color(1, 1, 1, alpha);

            yield return null;
        }

        // Ensure fully opaque
        backgroundImage.color = Color.black;
        gameIcon.color = Color.white;
    }

    IEnumerator FadeOutOverlay()
    {
        float timer = 0;
        while (timer < 1f)
        {
            timer += Time.unscaledDeltaTime * fadeOutSpeed;
            float alpha = 1f - Mathf.Clamp01(timer);

            backgroundImage.color = new Color(0, 0, 0, alpha);
            gameIcon.color = new Color(1, 1, 1, alpha);
            tipsText.color = new Color(1, 1, 1, alpha);

            yield return null;
        }
    }

    IEnumerator ShowTipsWhileLoading(AsyncOperation asyncLoad)
    {
        while (!asyncLoad.isDone && asyncLoad.progress < 0.9f)
        {
            // Show current tip
            yield return StartCoroutine(ShowSingleTip());

            // Move to next tip
            currentTipIndex = (currentTipIndex + 1) % gameTips.Length;
        }
    }

    IEnumerator ShowSingleTip()
    {
        // Set text
        tipsText.text = gameTips[currentTipIndex];

        // Fade in
        float timer = 0;
        while (timer < 1f)
        {
            timer += Time.unscaledDeltaTime * 3f;
            tipsText.color = new Color(1, 1, 1, Mathf.Clamp01(timer));
            yield return null;
        }

        // Stay visible
        yield return new WaitForSeconds(tipDisplayDuration);

        // Fade out
        timer = 0;
        while (timer < 1f)
        {
            timer += Time.unscaledDeltaTime * 3f;
            tipsText.color = new Color(1, 1, 1, 1f - Mathf.Clamp01(timer));
            yield return null;
        }
    }
}*//*


// TransitionOverlay.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.XR;

public class Transition : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject overlayPanel;
    public Image backgroundImage;
    public Image gameIcon;
    public TextMeshProUGUI tipsText;
    public Image loadingSpinner;

    [Header("VR Settings")]
    public float canvasDistance = 1.5f;
    public float canvasScale = 0.01f;

    [Header("Game Tips")]
    public string[] gameTips = {
        "Use teleportation to move around efficiently",
        "Look around to find hidden items",
        "Grab objects with your controllers",
        "Check your inventory regularly",
        "Save your progress frequently"
    };

    [Header("Settings")]
    public float tipDisplayDuration = 2.5f;
    public float fadeInSpeed = 3f;
    public float fadeOutSpeed = 2f;

    private static Transition instance;
    private Coroutine currentTransition;
    private int currentTipIndex = 0;
    private Camera vrCamera;
    private Canvas canvas;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            canvas = GetComponent<Canvas>();
            overlayPanel.SetActive(false);

            // Setup VR after a short delay to ensure XR is initialized
            StartCoroutine(DelayedVRSetup());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    IEnumerator DelayedVRSetup()
    {
        // Wait a frame for XR to initialize
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        SetupForVR();
    }

    void SetupForVR()
    {
        // Check if we're in VR
        bool isVR = XRSettings.enabled && XRSettings.loadedDeviceName != "";

        if (isVR)
        {
            Debug.Log("Setting up for VR");
            SetupVRCanvas();
        }
        else
        {
            Debug.Log("Setting up for non-VR");
            SetupScreenSpaceCanvas();
        }
    }

    void SetupVRCanvas()
    {
        // Find VR camera
        vrCamera = Camera.main;
        if (vrCamera == null)
        {
            // Try to find XR camera
            vrCamera = FindObjectOfType<Camera>();
        }

        if (vrCamera == null)
        {
            Debug.LogError("No camera found for VR canvas!");
            return;
        }

        // Set canvas to World Space for VR
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = vrCamera;

        // Position the canvas in front of the camera
        RectTransform rectTransform = canvas.GetComponent<RectTransform>();

        // Reset transform
        rectTransform.localPosition = Vector3.zero;
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.localScale = Vector3.one * canvasScale;

        // Set size for world space
        rectTransform.sizeDelta = new Vector2(1920, 1080);

        // Position it in front of camera
        transform.SetParent(vrCamera.transform, false);
        transform.localPosition = new Vector3(0, 0, canvasDistance);
        transform.localRotation = Quaternion.identity;

        Debug.Log($"VR Canvas setup complete. Position: {transform.position}, Local Position: {transform.localPosition}");
    }

    void SetupScreenSpaceCanvas()
    {
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
    }

    void Update()
    {
        // Keep canvas in front of VR camera if in world space mode
        if (canvas.renderMode == RenderMode.WorldSpace && vrCamera != null)
        {
            // Optional: Make canvas always face the camera
            // transform.LookAt(transform.position + vrCamera.transform.rotation * Vector3.forward,
            //                 vrCamera.transform.rotation * Vector3.up);
        }
    }

    public static void StartTransition(string targetScene)
    {
        if (instance != null)
        {
            if (instance.currentTransition != null)
                instance.StopCoroutine(instance.currentTransition);

            instance.currentTransition = instance.StartCoroutine(instance.TransitionToScene(targetScene));
        }
        else
        {
            Debug.LogError("TransitionOverlay instance not found!");
        }
    }

    IEnumerator TransitionToScene(string targetScene)
    {
        Debug.Log($"Starting transition to {targetScene}");

        // Ensure canvas is properly positioned for VR
        if (canvas.renderMode == RenderMode.WorldSpace && vrCamera != null)
        {
            transform.position = vrCamera.transform.position + vrCamera.transform.forward * canvasDistance;
            transform.LookAt(transform.position + vrCamera.transform.rotation * Vector3.forward,
                           vrCamera.transform.rotation * Vector3.up);
        }

        // Step 1: Show overlay immediately
        overlayPanel.SetActive(true);
        yield return StartCoroutine(FadeInOverlay());

        // Step 2: Start loading target scene in background
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetScene);
        asyncLoad.allowSceneActivation = false;

        // Step 3: Show tips while loading
        Coroutine tipsRoutine = StartCoroutine(ShowTipsWhileLoading(asyncLoad));

        // Step 4: Wait for loading to complete
        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        // Step 5: Ensure minimum display time
        yield return new WaitForSeconds(0.5f);

        // Step 6: Stop showing tips
        StopCoroutine(tipsRoutine);

        // Step 7: Activate the new scene
        asyncLoad.allowSceneActivation = true;

        // Step 8: Wait for scene to be fully loaded
        yield return new WaitUntil(() => asyncLoad.isDone);

        // Step 9: Re-setup VR after scene change (camera might have changed)
        yield return new WaitForSeconds(0.1f);
        SetupForVR();

        // Step 10: Fade out overlay
        yield return StartCoroutine(FadeOutOverlay());

        // Step 11: Hide overlay
        overlayPanel.SetActive(false);

        currentTransition = null;
        Debug.Log("Transition complete");
    }

    IEnumerator FadeInOverlay()
    {
        backgroundImage.color = new Color(0, 0, 0, 0);
        gameIcon.color = new Color(1, 1, 1, 0);
        tipsText.color = new Color(1, 1, 1, 0);

        float timer = 0;
        while (timer < 1f)
        {
            timer += Time.unscaledDeltaTime * fadeInSpeed;
            float alpha = Mathf.Clamp01(timer);

            backgroundImage.color = new Color(0, 0, 0, alpha);
            gameIcon.color = new Color(1, 1, 1, alpha);

            yield return null;
        }

        backgroundImage.color = Color.black;
        gameIcon.color = Color.white;
    }

    IEnumerator FadeOutOverlay()
    {
        float timer = 0;
        while (timer < 1f)
        {
            timer += Time.unscaledDeltaTime * fadeOutSpeed;
            float alpha = 1f - Mathf.Clamp01(timer);

            backgroundImage.color = new Color(0, 0, 0, alpha);
            gameIcon.color = new Color(1, 1, 1, alpha);
            tipsText.color = new Color(1, 1, 1, alpha);

            yield return null;
        }
    }

    IEnumerator ShowTipsWhileLoading(AsyncOperation asyncLoad)
    {
        while (!asyncLoad.isDone && asyncLoad.progress < 0.9f)
        {
            yield return StartCoroutine(ShowSingleTip());
            currentTipIndex = (currentTipIndex + 1) % gameTips.Length;
        }
    }

    IEnumerator ShowSingleTip()
    {
        tipsText.text = gameTips[currentTipIndex];

        // Fade in
        float timer = 0;
        while (timer < 1f)
        {
            timer += Time.unscaledDeltaTime * 3f;
            tipsText.color = new Color(1, 1, 1, Mathf.Clamp01(timer));
            yield return null;
        }

        yield return new WaitForSeconds(tipDisplayDuration);

        // Fade out
        timer = 0;
        while (timer < 1f)
        {
            timer += Time.unscaledDeltaTime * 3f;
            tipsText.color = new Color(1, 1, 1, 1f - Mathf.Clamp01(timer));
            yield return null;
        }
    }
}*/


using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class Transition : MonoBehaviour
{
    [Header("UI Elements")]
    public Canvas uiCanvas;
    public Image backgroundImage;
    public Image gameIcon;
    public TextMeshProUGUI tipsText;

    [Header("Game Tips")]
    public string[] gameTips = {
        "Pull back the thumbstick to teleport and move around",
        "Use the left controller to open the menu and check your current task",
        "Force grab from a distance",
        "There might be some hidden hints in the environment",
        "I heard there's something interesting in the flyer"
    };

    [Header("Settings")]
    public float tipDisplayDuration = 2.5f;
    public float fadeSpeed = 3f;
    public float minimumTransitionTime = 1f;

    private static Transition instance;
    private int currentTipIndex = 0;
    private Camera vrCamera;
    private Camera overlayCamera;
    private bool isTransitioning = false;

    void Awake()
    {
        // Singleton pattern with DontDestroyOnLoad
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            StartCoroutine(InitializeTransition());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    IEnumerator InitializeTransition()
    {
        // Wait for scene to be ready
        yield return new WaitForEndOfFrame();

        SetupVRTransition();

        // Initially hide the transition UI
        HideTransitionUI();

        Debug.Log("VR Transition Manager initialized");
    }

    void SetupVRTransition()
    {
        // Find VR camera
        vrCamera = Camera.main;
        if (vrCamera == null)
        {
            vrCamera = FindObjectOfType<Camera>();
        }

        if (vrCamera == null)
        {
            Debug.LogError("No camera found for VR transition!");
            return;
        }

        // Create overlay camera if it doesn't exist
        if (overlayCamera == null)
        {
            CreateOverlayCamera();
        }

        // Setup UI
        SetupUI();

        Debug.Log($"VR Transition setup with camera: {vrCamera.name}");
    }

    void CreateOverlayCamera()
    {
        GameObject cameraObj = new GameObject("VR_OverlayCamera");
        cameraObj.transform.SetParent(transform);

        overlayCamera = cameraObj.AddComponent<Camera>();
        overlayCamera.clearFlags = CameraClearFlags.SolidColor;
        overlayCamera.backgroundColor = Color.black;
        overlayCamera.cullingMask = 1 << LayerMask.NameToLayer("UI");
        overlayCamera.depth = 100; // Render on top of everything
        overlayCamera.orthographic = false;
        overlayCamera.enabled = false; // Initially disabled

        if (vrCamera != null)
        {
            overlayCamera.fieldOfView = vrCamera.fieldOfView;
            overlayCamera.nearClipPlane = 0.01f;
            overlayCamera.farClipPlane = 10f;
        }

        Debug.Log("Overlay camera created");
    }

    void SetupUI()
    {
        if (uiCanvas == null)
        {
            Debug.LogError("UI Canvas not assigned!");
            return;
        }

        // Set UI to UI layer
        SetLayerRecursively(uiCanvas.gameObject, LayerMask.NameToLayer("UI"));

        // Setup canvas for overlay camera
        uiCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        uiCanvas.worldCamera = overlayCamera;
        uiCanvas.planeDistance = 1f;
        uiCanvas.sortingOrder = 1000;

        // Ensure background covers full screen
        if (backgroundImage != null)
        {
            RectTransform bgRect = backgroundImage.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
        }
    }

    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    void Update()
    {
        // Keep overlay camera synced with VR camera
        if (overlayCamera != null && vrCamera != null && overlayCamera.enabled)
        {
            overlayCamera.transform.position = vrCamera.transform.position;
            overlayCamera.transform.rotation = vrCamera.transform.rotation;

            // Update FOV in case it changed
            overlayCamera.fieldOfView = vrCamera.fieldOfView;
        }
    }

    void OnEnable()
    {
        // Subscribe to scene loaded event
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Re-setup after scene change
        if (isTransitioning)
        {
            StartCoroutine(ResetupAfterSceneChange());
        }
    }

    IEnumerator ResetupAfterSceneChange()
    {
        // Wait a frame for the scene to be fully loaded
        yield return new WaitForEndOfFrame();

        // Find new camera
        vrCamera = Camera.main;
        if (vrCamera == null)
        {
            vrCamera = FindObjectOfType<Camera>();
        }

        // Update overlay camera settings
        if (overlayCamera != null && vrCamera != null)
        {
            overlayCamera.fieldOfView = vrCamera.fieldOfView;
            Debug.Log($"Camera updated after scene change: {vrCamera.name}");
        }
    }

    public static void StartTransition(string targetScene)
    {
        if (instance != null && !instance.isTransitioning)
        {
            instance.StartCoroutine(instance.PerformTransition(targetScene));
        }
        else if (instance == null)
        {
            Debug.LogError("VRTransitionManager instance not found!");
        }
        else
        {
            Debug.LogWarning("Transition already in progress!");
        }
    }

    IEnumerator PerformTransition(string targetScene)
    {
        isTransitioning = true;
        Debug.Log($"Starting transition to {targetScene}");

        // Show transition UI
        ShowTransitionUI();

        // Fade in
        yield return StartCoroutine(FadeIn());

        // Start loading the target scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetScene);
        asyncLoad.allowSceneActivation = false;

        // Show tips while loading
        Coroutine tipsCoroutine = StartCoroutine(ShowTipsWhileLoading(asyncLoad));

        // Wait for loading to complete
        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        // Ensure minimum transition time for smooth experience
        float startTime = Time.unscaledTime;
        while (Time.unscaledTime - startTime < minimumTransitionTime)
        {
            yield return null;
        }

        // Stop showing tips
        if (tipsCoroutine != null)
        {
            StopCoroutine(tipsCoroutine);
        }

        // Clear current tip
        if (tipsText != null)
        {
            tipsText.color = new Color(1, 1, 1, 0);
        }

        // Activate the new scene
        asyncLoad.allowSceneActivation = true;

        // Wait for scene to be fully loaded
        yield return new WaitUntil(() => asyncLoad.isDone);

        // Give the new scene a moment to initialize
        yield return new WaitForSeconds(0.2f);

        // Fade out transition
        yield return StartCoroutine(FadeOut());

        // Hide transition UI
        HideTransitionUI();

        isTransitioning = false;
        Debug.Log("Transition completed");
    }

    void ShowTransitionUI()
    {
        if (overlayCamera != null)
        {
            overlayCamera.enabled = true;
        }

        if (uiCanvas != null)
        {
            uiCanvas.gameObject.SetActive(true);
        }
    }

    void HideTransitionUI()
    {
        if (overlayCamera != null)
        {
            overlayCamera.enabled = false;
        }

        if (uiCanvas != null)
        {
            uiCanvas.gameObject.SetActive(false);
        }
    }

    IEnumerator FadeIn()
    {
        float timer = 0;

        while (timer < 1f)
        {
            timer += Time.unscaledDeltaTime * fadeSpeed;
            float alpha = Mathf.Clamp01(timer);

            if (backgroundImage != null)
            {
                backgroundImage.color = new Color(0, 0, 0, alpha);
            }

            if (gameIcon != null)
            {
                gameIcon.color = new Color(1, 1, 1, alpha);
            }

            yield return null;
        }

        // Ensure fully visible
        if (backgroundImage != null)
        {
            backgroundImage.color = Color.black;
        }

        if (gameIcon != null)
        {
            gameIcon.color = Color.white;
        }
    }

    IEnumerator FadeOut()
    {
        float timer = 0;

        while (timer < 1f)
        {
            timer += Time.unscaledDeltaTime * fadeSpeed;
            float alpha = 1f - Mathf.Clamp01(timer);

            if (backgroundImage != null)
            {
                backgroundImage.color = new Color(0, 0, 0, alpha);
            }

            if (gameIcon != null)
            {
                gameIcon.color = new Color(1, 1, 1, alpha);
            }

            if (tipsText != null)
            {
                Color textColor = tipsText.color;
                textColor.a = alpha;
                tipsText.color = textColor;
            }

            yield return null;
        }
    }

    IEnumerator ShowTipsWhileLoading(AsyncOperation asyncLoad)
    {
        while (!asyncLoad.isDone && asyncLoad.progress < 0.9f)
        {
            yield return StartCoroutine(ShowSingleTip());
            currentTipIndex = (currentTipIndex + 1) % gameTips.Length;
        }
    }

    IEnumerator ShowSingleTip()
    {
        if (tipsText == null) yield break;

        // Set text
        tipsText.text = gameTips[currentTipIndex];

        // Fade in
        float timer = 0;
        while (timer < 1f)
        {
            timer += Time.unscaledDeltaTime * 4f;
            Color textColor = tipsText.color;
            textColor.a = Mathf.Clamp01(timer);
            tipsText.color = textColor;
            yield return null;
        }

        // Stay visible
        yield return new WaitForSeconds(tipDisplayDuration);

        // Fade out
        timer = 0;
        while (timer < 1f)
        {
            timer += Time.unscaledDeltaTime * 4f;
            Color textColor = tipsText.color;
            textColor.a = 1f - Mathf.Clamp01(timer);
            tipsText.color = textColor;
            yield return null;
        }
    }
}