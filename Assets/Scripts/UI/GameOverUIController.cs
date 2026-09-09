using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameOverUIController : MonoBehaviour
{
    //=============================================================
    // UI
    //=============================================================

    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform panelRect;

    [Header("Score")]
    [SerializeField] private TMP_Text scoreValue;
    [SerializeField] private TMP_Text highScoreValue;

    [Header("Coins")]
    [SerializeField] private TMP_Text coinValue;

    [Header("Buttons")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button mainMenuButton;


    //=============================================================
    // DELAY
    //=============================================================

    [Header("Game Over Delay")]
    [Tooltip("Thời gian giữ gameplay sau khi Player chết trước khi hiện Game Over.")]
    [SerializeField] private float gameOverDelay = 5f;


    //=============================================================
    // FADE
    //=============================================================

    [Header("Fade")]
    [SerializeField] private float fadeDuration = 0.8f;


    //=============================================================
    // ANIMATION
    //=============================================================

    [Header("Panel Animation")]
    [SerializeField] private float animationSpeed = 10f;

    [SerializeField] private float hiddenScale = 0.88f;


    //=============================================================
    // AUDIO
    //=============================================================

    [Header("Audio")]
    [Tooltip("Tắt toàn bộ AudioListener khi Game Over.")]
    [SerializeField] private bool muteAllAudioOnGameOver = true;


    //=============================================================
    // SCENE
    //=============================================================

    [Header("Scene")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";


    //=============================================================
    // STATE
    //=============================================================

    private Vector3 panelBaseScale;

    private bool isShowing;

    private bool gameOverSequenceStarted;

    private Coroutine gameOverRoutine;


    //=============================================================
    // UNITY
    //=============================================================

    private void Awake()
    {
        if (gameOverPanel == null)
        {
            Debug.LogError(
                "[GameOverUIController] Chưa gán Game Over Panel.",
                this
            );
        }

        if (panelRect != null)
        {
            panelBaseScale = panelRect.localScale;
        }

        if (canvasGroup == null && gameOverPanel != null)
        {
            canvasGroup =
                gameOverPanel.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
            {
                canvasGroup =
                    gameOverPanel.AddComponent<CanvasGroup>();
            }
        }

        SetupButtons();

        HideImmediate();
    }


    private void Start()
    {
        Time.timeScale = 1f;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver += StartGameOverSequence;
        }
    }


    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver -= StartGameOverSequence;
        }
    }


    private void Update()
    {
        if (!isShowing)
            return;

        AnimatePanel();
    }


    //=============================================================
    // BUTTONS
    //=============================================================

    private void SetupButtons()
    {
        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(Retry);
            retryButton.onClick.AddListener(Retry);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(GoToMainMenu);
            mainMenuButton.onClick.AddListener(GoToMainMenu);
        }
    }


    //=============================================================
    // GAME OVER SEQUENCE
    //=============================================================

    private void StartGameOverSequence()
    {
        if (gameOverSequenceStarted)
            return;

        gameOverSequenceStarted = true;

        if (gameOverRoutine != null)
        {
            StopCoroutine(gameOverRoutine);
        }

        gameOverRoutine =
            StartCoroutine(GameOverSequence());
    }


    private IEnumerator GameOverSequence()
    {
        //---------------------------------------------------------
        // IMPORTANT
        // Game vẫn chạy trong 5 giây đầu.
        //---------------------------------------------------------

        Time.timeScale = 1f;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }


        //---------------------------------------------------------
        // 5 SECOND DEATH HOLD
        //---------------------------------------------------------

        float timer = 0f;

        while (timer < gameOverDelay)
        {
            timer += Time.unscaledDeltaTime;

            yield return null;
        }


        //---------------------------------------------------------
        // Update Result
        //---------------------------------------------------------

        UpdateResultUI();


        //---------------------------------------------------------
        // PAUSE GAME
        //---------------------------------------------------------

        Time.timeScale = 0f;


        //---------------------------------------------------------
        // MUTE AUDIO
        //---------------------------------------------------------

        if (muteAllAudioOnGameOver)
        {
            AudioListener.pause = true;
        }


        //---------------------------------------------------------
        // SHOW PANEL
        //---------------------------------------------------------

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }


        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }


        if (panelRect != null)
        {
            panelRect.localScale =
                panelBaseScale * hiddenScale;
        }


        isShowing = true;


        //---------------------------------------------------------
        // FADE IN
        //---------------------------------------------------------

        yield return StartCoroutine(FadeIn());


        //---------------------------------------------------------
        // FINISH
        //---------------------------------------------------------

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        if (panelRect != null)
        {
            panelRect.localScale =
                panelBaseScale;
        }
    }


    //=============================================================
    // UPDATE RESULT UI
    //=============================================================

    private void UpdateResultUI()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning(
                "[GameOverUIController] Không tìm thấy GameManager.",
                this
            );

            return;
        }


        //---------------------------------------------------------
        // SCORE
        //---------------------------------------------------------

        if (scoreValue != null)
        {
            scoreValue.text =
                GameManager.Instance.ScoreInt.ToString("N0");
        }


        //---------------------------------------------------------
        // HIGH SCORE
        //---------------------------------------------------------

        if (highScoreValue != null)
        {
            highScoreValue.text =
                Mathf.FloorToInt(
                    GameManager.Instance.GetHighScore()
                ).ToString("N0");
        }


        //---------------------------------------------------------
        // COINS
        //---------------------------------------------------------

        if (coinValue != null)
        {
            coinValue.text =
                GameManager.Instance.CoinCount.ToString("N0");
        }
    }


    //=============================================================
    // FADE IN
    //=============================================================

    private IEnumerator FadeIn()
    {
        float elapsed = 0f;

        float startAlpha =
            canvasGroup != null
                ? canvasGroup.alpha
                : 0f;

        Vector3 startScale =
            panelBaseScale * hiddenScale;


        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / fadeDuration
                );

            t = Mathf.SmoothStep(0f, 1f, t);


            //-----------------------------------------------------
            // Canvas Fade
            //-----------------------------------------------------

            if (canvasGroup != null)
            {
                canvasGroup.alpha =
                    Mathf.Lerp(
                        startAlpha,
                        1f,
                        t
                    );
            }


            //-----------------------------------------------------
            // Panel Scale
            //-----------------------------------------------------

            if (panelRect != null)
            {
                panelRect.localScale =
                    Vector3.Lerp(
                        startScale,
                        panelBaseScale,
                        t
                    );
            }


            yield return null;
        }
    }


    //=============================================================
    // PANEL ANIMATION
    //=============================================================

    private void AnimatePanel()
    {
        float speed =
            animationSpeed *
            Time.unscaledDeltaTime;


        if (canvasGroup != null)
        {
            canvasGroup.alpha =
                Mathf.Lerp(
                    canvasGroup.alpha,
                    1f,
                    speed
                );
        }


        if (panelRect != null)
        {
            panelRect.localScale =
                Vector3.Lerp(
                    panelRect.localScale,
                    panelBaseScale,
                    speed
                );
        }
    }


    //=============================================================
    // HIDE
    //=============================================================

    private void HideImmediate()
    {
        isShowing = false;

        gameOverSequenceStarted = false;


        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }


        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }


        if (panelRect != null)
        {
            panelRect.localScale =
                panelBaseScale;
        }
    }


    //=============================================================
    // RETRY
    //=============================================================

    public void Retry()
    {
        Time.timeScale = 1f;

        AudioListener.pause = false;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().name
        );
    }


    //=============================================================
    // MAIN MENU
    //=============================================================

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;

        AudioListener.pause = false;


        if (!Application.CanStreamedLevelBeLoaded(
                mainMenuSceneName))
        {
            Debug.LogError(
                $"[GameOverUIController] Không thể load Scene '{mainMenuSceneName}'. " +
                "Kiểm tra Build Profiles.",
                this
            );

            return;
        }


        SceneManager.LoadScene(
            mainMenuSceneName
        );
    }


    //=============================================================
    // PUBLIC
    //=============================================================

    public bool IsShowing
    {
        get
        {
            return isShowing;
        }
    }
}