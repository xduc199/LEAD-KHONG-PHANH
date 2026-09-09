using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    //=============================================================
    // PAUSE UI
    //=============================================================

    [Header("Pause UI")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform panelRect;


    //=============================================================
    // PAUSE BUTTONS
    //=============================================================

    [Header("Pause Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button mainMenuButton;


    //=============================================================
    // SETTINGS
    //=============================================================

    [Header("Settings")]
    [Tooltip("Panel Settings được mở từ Pause Menu.")]
    [SerializeField] private GameObject settingsPanel;

    [Tooltip("Nút đóng Settings Panel.")]
    [SerializeField] private Button closeSettingsButton;


    //=============================================================
    // ANIMATION
    //=============================================================

    [Header("Animation")]
    [SerializeField] private float animationSpeed = 12f;

    [Tooltip("Scale ban đầu của Panel khi mở.")]
    [SerializeField] private float hiddenScale = 0.88f;


    //=============================================================
    // SCENE
    //=============================================================

    [Header("Scene")]
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


        //---------------------------------------------------------
        // Auto Find / Create CanvasGroup
        //---------------------------------------------------------

        if (canvasGroup == null && pausePanel != null)
        {
            canvasGroup =
                pausePanel.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
            {
                canvasGroup =
                    pausePanel.AddComponent<CanvasGroup>();
            }
        }


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

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
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
        // Close Settings
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
            return;


        //---------------------------------------------------------
        // State
        //---------------------------------------------------------

        isPaused = true;


        //---------------------------------------------------------
        // Pause Unity
        //---------------------------------------------------------

        Time.timeScale = 0f;


        //---------------------------------------------------------
        // Close Settings If Open
        //---------------------------------------------------------

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }


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
            return;


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

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }


        //---------------------------------------------------------
        // Hide Pause Panel
        //---------------------------------------------------------

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }


    //=============================================================
    // SETTINGS
    //=============================================================

    public void OpenSettings()
    {
        if (settingsPanel == null)
        {
            Debug.LogWarning(
                "[PauseMenuController] Settings Panel chưa được gán.",
                this
            );

            return;
        }


        //---------------------------------------------------------
        // Open Settings
        //---------------------------------------------------------

        settingsPanel.SetActive(true);
    }


    //=============================================================
    // CLOSE SETTINGS
    //=============================================================

    public void CloseSettings()
    {
        if (settingsPanel == null)
            return;


        settingsPanel.SetActive(false);
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
            canvasGroup.alpha =
                Mathf.Lerp(
                    canvasGroup.alpha,
                    1f,
                    animationSpeed *
                    Time.unscaledDeltaTime
                );
        }


        //---------------------------------------------------------
        // Panel Scale
        //---------------------------------------------------------

        if (panelRect != null)
        {
            panelRect.localScale =
                Vector3.Lerp(
                    panelRect.localScale,
                    panelBaseScale,
                    animationSpeed *
                    Time.unscaledDeltaTime
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