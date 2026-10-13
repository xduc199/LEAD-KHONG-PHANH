using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PauseMenuController : MonoBehaviour
{
    //=============================================================
    // PAUSE UI
    //=============================================================

    [Header("PAUSE UI")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform panelRect;


    //=============================================================
    // PAUSE BUTTONS
    //=============================================================

    [Header("PAUSE BUTTONS")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button mainMenuButton;


    //=============================================================
    // GAMEPLAY SETTINGS
    //=============================================================

    [Header("GAMEPLAY SETTINGS")]
    [Tooltip(
        "GameplaySettingsUI dùng chung cho Settings của Gameplay. " +
        "Nếu để trống, hệ thống sẽ tự lấy GameplaySettingsUI.Instance."
    )]
    [SerializeField] private GameplaySettingsUI gameplaySettingsUI;

    [Tooltip(
        "Fallback cho scene cũ. Không cần dùng nếu GameplaySettingsUI đã được Setup."
    )]
    [SerializeField] private GameObject settingsPanel;

    [Tooltip(
        "Nút đóng Settings của scene cũ. Có thể để trống khi dùng GameplaySettingsUI mới."
    )]
    [SerializeField] private Button closeSettingsButton;


    //=============================================================
    // ANIMATION
    //=============================================================

    [Header("ANIMATION")]
    [SerializeField] private float animationSpeed = 12f;

    [Tooltip("Scale ban đầu của Pause Panel khi mở.")]
    [SerializeField] private float hiddenScale = 0.88f;


    //=============================================================
    // SCENE
    //=============================================================

    [Header("SCENE")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";


    //=============================================================
    // STATE
    //=============================================================

    private bool isPaused;

    private Vector3 panelBaseScale;


    //=============================================================
    // UNITY - AWAKE
    //=============================================================

    private void Awake()
    {
        //---------------------------------------------------------
        // Validate Pause Panel
        //---------------------------------------------------------

        if (pausePanel == null)
        {
            Debug.LogError(
                "[PauseMenuController] Chưa gán Pause Panel.",
                this
            );
        }


        //---------------------------------------------------------
        // Cache Panel Scale
        //---------------------------------------------------------

        if (panelRect != null)
        {
            panelBaseScale = panelRect.localScale;
        }
        else if (pausePanel != null)
        {
            panelRect = pausePanel.GetComponent<RectTransform>();

            if (panelRect != null)
            {
                panelBaseScale = panelRect.localScale;
            }
        }


        //---------------------------------------------------------
        // Auto Find / Create CanvasGroup
        //---------------------------------------------------------

        if (canvasGroup == null && pausePanel != null)
        {
            canvasGroup = pausePanel.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
            {
                canvasGroup = pausePanel.AddComponent<CanvasGroup>();
            }
        }


        //---------------------------------------------------------
        // Resolve Gameplay Settings
        //---------------------------------------------------------

        ResolveGameplaySettings();


        //---------------------------------------------------------
        // Setup Buttons
        //---------------------------------------------------------

        SetupButtons();


        //---------------------------------------------------------
        // Hide UI At Start
        //---------------------------------------------------------

        HideImmediate();
    }


    //=============================================================
    // UNITY - START
    //=============================================================

    private void Start()
    {
        Time.timeScale = 1f;

        isPaused = false;

        //---------------------------------------------------------
        // Settings phải đóng lúc bắt đầu
        //---------------------------------------------------------

        CloseSettingsImmediate();
    }


    //=============================================================
    // UNITY - UPDATE
    //=============================================================

    private void Update()
    {
        //---------------------------------------------------------
        // ESC = Toggle Pause
        //---------------------------------------------------------

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }


        //---------------------------------------------------------
        // Pause Animation
        //---------------------------------------------------------

        if (isPaused)
        {
            AnimatePauseIn();
        }
    }


    //=============================================================
    // RESOLVE GAMEPLAY SETTINGS
    //=============================================================

    private void ResolveGameplaySettings()
    {
        //---------------------------------------------------------
        // Nếu đã gán Inspector thì giữ nguyên.
        //---------------------------------------------------------

        if (gameplaySettingsUI != null)
        {
            return;
        }


        //---------------------------------------------------------
        // Lấy Singleton nếu đã tồn tại.
        //---------------------------------------------------------

        gameplaySettingsUI = GameplaySettingsUI.Instance;
    }


    //=============================================================
    // BUTTON SETUP
    //=============================================================

    private void SetupButtons()
    {
        //---------------------------------------------------------
        // Resume
        //---------------------------------------------------------

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(Resume);
            resumeButton.onClick.AddListener(Resume);
        }


        //---------------------------------------------------------
        // Settings
        //---------------------------------------------------------

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveListener(OpenSettings);
            settingsButton.onClick.AddListener(OpenSettings);
        }


        //---------------------------------------------------------
        // Main Menu
        //---------------------------------------------------------

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(GoToMainMenu);
            mainMenuButton.onClick.AddListener(GoToMainMenu);
        }


        //---------------------------------------------------------
        // Close Settings - Fallback
        //---------------------------------------------------------

        if (closeSettingsButton != null)
        {
            closeSettingsButton.onClick.RemoveListener(CloseSettings);
            closeSettingsButton.onClick.AddListener(CloseSettings);
        }
    }


    //=============================================================
    // PAUSE
    //=============================================================

    public void TogglePause()
    {
        if (isPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }


    //=============================================================
    // PAUSE GAME
    //=============================================================

    public void Pause()
    {
        if (isPaused)
        {
            return;
        }


        //---------------------------------------------------------
        // State
        //---------------------------------------------------------

        isPaused = true;


        //---------------------------------------------------------
        // Pause Unity
        //---------------------------------------------------------

        Time.timeScale = 0f;


        //---------------------------------------------------------
        // Close Settings
        //---------------------------------------------------------

        CloseSettingsImmediate();


        //---------------------------------------------------------
        // Show Pause Panel
        //---------------------------------------------------------

        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }


        //---------------------------------------------------------
        // Animation Start
        //---------------------------------------------------------

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }


        if (panelRect != null)
        {
            panelRect.localScale =
                panelBaseScale * hiddenScale;
        }
    }


    //=============================================================
    // RESUME GAME
    //=============================================================

    public void Resume()
    {
        if (!isPaused)
        {
            return;
        }


        //---------------------------------------------------------
        // State
        //---------------------------------------------------------

        isPaused = false;


        //---------------------------------------------------------
        // Resume Unity
        //---------------------------------------------------------

        Time.timeScale = 1f;


        //---------------------------------------------------------
        // Close Settings
        //---------------------------------------------------------

        CloseSettingsImmediate();


        //---------------------------------------------------------
        // Hide Pause Panel
        //---------------------------------------------------------

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }


    //=============================================================
    // OPEN SETTINGS
    //=============================================================

    public void OpenSettings()
    {
        //---------------------------------------------------------
        // Nếu GameplaySettingsUI chưa được resolve
        //---------------------------------------------------------

        ResolveGameplaySettings();


        //---------------------------------------------------------
        // Ưu tiên hệ thống Settings mới
        //---------------------------------------------------------

        if (gameplaySettingsUI != null)
        {
            gameplaySettingsUI.Open();
            return;
        }


        //---------------------------------------------------------
        // Fallback hệ thống cũ
        //---------------------------------------------------------

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            return;
        }


        //---------------------------------------------------------
        // Không tìm thấy Settings
        //---------------------------------------------------------

        Debug.LogWarning(
            "[PauseMenuController] Không tìm thấy GameplaySettingsUI " +
            "và Settings Panel fallback cũng chưa được gán.",
            this
        );
    }


    //=============================================================
    // CLOSE SETTINGS
    //=============================================================

    public void CloseSettings()
    {
        //---------------------------------------------------------
        // Ưu tiên hệ thống Settings mới
        //---------------------------------------------------------

        if (gameplaySettingsUI != null)
        {
            gameplaySettingsUI.Close();
            return;
        }


        //---------------------------------------------------------
        // Fallback hệ thống cũ
        //---------------------------------------------------------

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }


    //=============================================================
    // CLOSE SETTINGS IMMEDIATE
    //=============================================================

    private void CloseSettingsImmediate()
    {
        //---------------------------------------------------------
        // GameplaySettingsUI
        //---------------------------------------------------------

        if (gameplaySettingsUI != null &&
            gameplaySettingsUI.IsOpen)
        {
            gameplaySettingsUI.Close();
        }


        //---------------------------------------------------------
        // Fallback Panel
        //---------------------------------------------------------

        if (settingsPanel != null &&
            settingsPanel.activeSelf)
        {
            settingsPanel.SetActive(false);
        }
    }


    //=============================================================
    // MAIN MENU
    //=============================================================

    public void GoToMainMenu()
    {
        //---------------------------------------------------------
        // Always resume before changing scene
        //---------------------------------------------------------

        Time.timeScale = 1f;

        isPaused = false;


        //---------------------------------------------------------
        // Close Settings
        //---------------------------------------------------------

        CloseSettingsImmediate();


        //---------------------------------------------------------
        // Hide Pause Panel
        //---------------------------------------------------------

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }


        //---------------------------------------------------------
        // Validate Scene Name
        //---------------------------------------------------------

        if (string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.LogError(
                "[PauseMenuController] Main Menu Scene Name đang trống.",
                this
            );

            return;
        }


        //---------------------------------------------------------
        // Check Scene
        //---------------------------------------------------------

        if (!Application.CanStreamedLevelBeLoaded(
                mainMenuSceneName))
        {
            Debug.LogError(
                $"[PauseMenuController] Không thể load Scene '{mainMenuSceneName}'. " +
                "Kiểm tra Build Profiles.",
                this
            );

            return;
        }


        //---------------------------------------------------------
        // Load Main Menu
        //---------------------------------------------------------

        SceneManager.LoadScene(mainMenuSceneName);
    }


    //=============================================================
    // PAUSE OPEN ANIMATION
    //=============================================================

    private void AnimatePauseIn()
    {
        //---------------------------------------------------------
        // CanvasGroup Fade
        //---------------------------------------------------------

        if (canvasGroup != null)
        {
            float t =
                animationSpeed *
                Time.unscaledDeltaTime;

            canvasGroup.alpha =
                Mathf.Lerp(
                    canvasGroup.alpha,
                    1f,
                    t
                );
        }


        //---------------------------------------------------------
        // Panel Scale
        //---------------------------------------------------------

        if (panelRect != null)
        {
            float t =
                animationSpeed *
                Time.unscaledDeltaTime;

            panelRect.localScale =
                Vector3.Lerp(
                    panelRect.localScale,
                    panelBaseScale,
                    t
                );
        }
    }


    //=============================================================
    // HIDE IMMEDIATELY
    //=============================================================

    private void HideImmediate()
    {
        //---------------------------------------------------------
        // Hide Pause Panel
        //---------------------------------------------------------

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }


        //---------------------------------------------------------
        // Reset Alpha
        //---------------------------------------------------------

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }


        //---------------------------------------------------------
        // Reset Scale
        //---------------------------------------------------------

        if (panelRect != null)
        {
            panelRect.localScale = panelBaseScale;
        }
    }


    //=============================================================
    // PUBLIC SETTER
    //=============================================================

    public void SetGameplaySettingsUI(
        GameplaySettingsUI target)
    {
        gameplaySettingsUI = target;
    }


    //=============================================================
    // PUBLIC STATE
    //=============================================================

    public bool IsPaused
    {
        get
        {
            return isPaused;
        }
    }
}