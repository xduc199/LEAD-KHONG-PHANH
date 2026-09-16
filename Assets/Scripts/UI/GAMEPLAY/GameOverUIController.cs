using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class GameOverUIController : MonoBehaviour
{
    //=============================================================
    // UI
    //=============================================================

    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform panelRect;


    //=============================================================
    // SCORE
    //=============================================================

    [Header("Score")]
    [SerializeField] private TMP_Text scoreValue;
    [SerializeField] private TMP_Text highScoreValue;


    //=============================================================
    // COINS
    //=============================================================

    [Header("Coins")]
    [SerializeField] private TMP_Text coinValue;


    //=============================================================
    // EXP UI
    //=============================================================

    [Header("EXP UI")]
    [Tooltip("Hiển thị EXP nhận được trong run. Ví dụ: +200 EXP")]
    [SerializeField] private TMP_Text expRewardValue;

    [Tooltip("Hiển thị Level hiện tại. Ví dụ: LEVEL 3")]
    [SerializeField] private TMP_Text levelValue;

    [Tooltip("Hiển thị khi người chơi vừa lên Level.")]
    [SerializeField] private TMP_Text levelUpValue;


    //=============================================================
    // BUTTONS
    //=============================================================

    [Header("Buttons")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button mainMenuButton;


    //=============================================================
    // GAME OVER DELAY
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
    // EXP SETTINGS
    //=============================================================

    [Header("EXP Reward")]
    [Tooltip("Mỗi bao nhiêu mét sẽ nhận EXP.")]
    [SerializeField] private float distancePerExpStep = 100f;

    [Tooltip("EXP nhận được cho mỗi mốc khoảng cách.")]
    [SerializeField] private int expPerDistanceStep = 10;

    [Tooltip("Mỗi bao nhiêu Coin sẽ nhận EXP.")]
    [SerializeField] private int coinsPerExpStep = 10;

    [Tooltip("EXP nhận được cho mỗi mốc Coin.")]
    [SerializeField] private int expPerCoinStep = 5;

    [Tooltip("EXP thưởng cố định khi kết thúc một run.")]
    [SerializeField] private int runCompletionExp = 20;


    //=============================================================
    // INPUT SAFETY
    //=============================================================

    [Header("Input Safety")]
    [Tooltip("Thời gian khóa Button sau khi Game Over Panel hoàn tất Fade.")]
    [SerializeField] private float buttonUnlockDelay = 0.2f;


    //=============================================================
    // STATE
    //=============================================================

    private Vector3 panelBaseScale;

    private bool isShowing;
    private bool gameOverSequenceStarted;
    private bool expRewardGranted;
    private bool buttonsUnlocked;
    private bool transitionStarted;

    private Coroutine gameOverRoutine;


    //=============================================================
    // EVENT SYSTEM STATE
    //=============================================================

    private bool previousSendNavigationEvents;
    private bool eventSystemStateCaptured;


    //=============================================================
    // POINTER INPUT STATE
    //=============================================================

    private bool retryPointerArmed;
    private bool mainMenuPointerArmed;
    private bool pointerInputEnabled;


    //=============================================================
    // EXP RESULT STATE
    //=============================================================

    private int lastRunExpReward;
    private int levelBeforeRun;
    private int levelAfterRun;
    private bool leveledUpThisRun;


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
        SetupPointerGuards();

        HideImmediate();
    }


    private void Start()
    {
        Time.timeScale = 1f;

        expRewardGranted = false;
        buttonsUnlocked = false;
        transitionStarted = false;

        pointerInputEnabled = false;
        retryPointerArmed = false;
        mainMenuPointerArmed = false;

        RestoreEventSystemNavigation();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver +=
                StartGameOverSequence;
        }
    }


    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver -=
                StartGameOverSequence;
        }

        RestoreEventSystemNavigation();
    }


    private void Update()
    {
        if (!isShowing)
            return;

        AnimatePanel();

        if (!buttonsUnlocked)
        {
            ClearEventSystemSelection();
        }
    }


    //=============================================================
    // BUTTON SETUP
    //=============================================================

    private void SetupButtons()
    {
        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(Retry);
            retryButton.onClick.RemoveListener(
                HandleRetryButtonClick
            );

            retryButton.onClick.AddListener(Retry);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(
                GoToMainMenu
            );

            mainMenuButton.onClick.RemoveListener(
                HandleMainMenuButtonClick
            );

            mainMenuButton.onClick.AddListener(
                GoToMainMenu
            );
        }
    }


    //=============================================================
    // POINTER GUARD SETUP
    //=============================================================

    private void SetupPointerGuards()
    {
        if (retryButton != null)
        {
            GameOverButtonPointerGuard guard =
                retryButton.GetComponent<GameOverButtonPointerGuard>();

            if (guard == null)
            {
                guard =
                    retryButton.gameObject.AddComponent<
                        GameOverButtonPointerGuard
                    >();
            }

            guard.Initialize(
                this,
                GameOverButtonPointerGuard.ButtonType.Retry
            );
        }


        if (mainMenuButton != null)
        {
            GameOverButtonPointerGuard guard =
                mainMenuButton.GetComponent<GameOverButtonPointerGuard>();

            if (guard == null)
            {
                guard =
                    mainMenuButton.gameObject.AddComponent<
                        GameOverButtonPointerGuard
                    >();
            }

            guard.Initialize(
                this,
                GameOverButtonPointerGuard.ButtonType.MainMenu
            );
        }
    }


    //=============================================================
    // POINTER DOWN
    //=============================================================
    //
    // FIX CS0051:
    // Method này là PRIVATE vì ButtonType nằm trong private class.
    //
    // Nested class bên dưới vẫn gọi được method private này.
    //=============================================================

    private void NotifyPointerDown(
        GameOverButtonPointerGuard.ButtonType buttonType
    )
    {
        if (!pointerInputEnabled)
        {
            Debug.Log(
                "[GameOverUIController] " +
                $"PointerDown bị bỏ qua | Button={buttonType} | " +
                "Pointer input chưa được ENABLE.",
                this
            );

            return;
        }


        if (buttonType ==
            GameOverButtonPointerGuard.ButtonType.Retry)
        {
            retryPointerArmed = true;

            Debug.Log(
                "[GameOverUIController] " +
                "RETRY PointerDown hợp lệ -> ARMED.",
                this
            );
        }


        if (buttonType ==
            GameOverButtonPointerGuard.ButtonType.MainMenu)
        {
            mainMenuPointerArmed = true;

            Debug.Log(
                "[GameOverUIController] " +
                "MAIN MENU PointerDown hợp lệ -> ARMED.",
                this
            );
        }
    }


    //=============================================================
    // BUTTON WRAPPERS
    //=============================================================

    private void HandleRetryButtonClick()
    {
        Debug.Log(
            "[GameOverUIController] " +
            "WARNING: HandleRetryButtonClick() được gọi.",
            this
        );

        if (!buttonsUnlocked)
        {
            Debug.Log(
                "[GameOverUIController] " +
                "Retry input bị chặn vì Button chưa unlock.",
                this
            );

            return;
        }

        if (!retryPointerArmed)
        {
            Debug.Log(
                "[GameOverUIController] " +
                "Retry bị chặn: không có PointerDown mới.",
                this
            );

            return;
        }

        Retry();
    }


    private void HandleMainMenuButtonClick()
    {
        Debug.Log(
            "[GameOverUIController] " +
            "WARNING: HandleMainMenuButtonClick() được gọi.",
            this
        );

        if (!buttonsUnlocked)
        {
            Debug.Log(
                "[GameOverUIController] " +
                "Main Menu input bị chặn vì Button chưa unlock.",
                this
            );

            return;
        }

        if (!mainMenuPointerArmed)
        {
            Debug.Log(
                "[GameOverUIController] " +
                "Main Menu bị chặn: không có PointerDown mới.",
                this
            );

            return;
        }

        GoToMainMenu();
    }


    //=============================================================
    // GAME OVER SEQUENCE
    //=============================================================

    private void StartGameOverSequence()
    {
        if (gameOverSequenceStarted)
            return;

        gameOverSequenceStarted = true;

        pointerInputEnabled = false;
        retryPointerArmed = false;
        mainMenuPointerArmed = false;

        DisableEventSystemNavigation();

        if (gameOverRoutine != null)
        {
            StopCoroutine(gameOverRoutine);
        }

        gameOverRoutine =
            StartCoroutine(GameOverSequence());
    }


    private IEnumerator GameOverSequence()
    {
        Time.timeScale = 1f;

        buttonsUnlocked = false;

        transitionStarted = false;

        pointerInputEnabled = false;
        retryPointerArmed = false;
        mainMenuPointerArmed = false;

        LockButtons();

        DisableEventSystemNavigation();
        ClearEventSystemSelection();


        //---------------------------------------------------------
        // HIDE PANEL
        //---------------------------------------------------------

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }


        //---------------------------------------------------------
        // DEATH HOLD
        //---------------------------------------------------------

        float timer = 0f;

        while (timer < gameOverDelay)
        {
            timer += Time.unscaledDeltaTime;

            DisableEventSystemNavigation();
            ClearEventSystemSelection();

            yield return null;
        }


        //---------------------------------------------------------
        // UPDATE RESULT UI
        //---------------------------------------------------------

        UpdateResultUI();


        //---------------------------------------------------------
        // EXP
        //---------------------------------------------------------

        GrantRunExp();

        UpdateExpUI();


        //---------------------------------------------------------
        // PAUSE
        //---------------------------------------------------------

        Time.timeScale = 0f;


        //---------------------------------------------------------
        // AUDIO
        //---------------------------------------------------------

        if (muteAllAudioOnGameOver)
        {
            AudioListener.pause = true;
        }


        //---------------------------------------------------------
        // LOCK
        //---------------------------------------------------------

        LockButtons();

        DisableEventSystemNavigation();
        ClearEventSystemSelection();


        //---------------------------------------------------------
        // SHOW PANEL
        //---------------------------------------------------------

        if (gameOverPanel != null)
        {
            Debug.Log(
                "[GameOverUIController] SHOW PANEL BEFORE | " +
                $"Name={gameOverPanel.name} | " +
                $"ActiveSelf={gameOverPanel.activeSelf} | " +
                $"ActiveInHierarchy={gameOverPanel.activeInHierarchy} | " +
                $"Parent={(gameOverPanel.transform.parent != null ? gameOverPanel.transform.parent.name : "NULL")}",
                this
            );

            gameOverPanel.SetActive(true);

            Debug.Log(
                "[GameOverUIController] SHOW PANEL AFTER | " +
                $"ActiveSelf={gameOverPanel.activeSelf} | " +
                $"ActiveInHierarchy={gameOverPanel.activeInHierarchy}",
                this
            );
        }
        else
        {
            Debug.LogError(
                "[GameOverUIController] " +
                "GAME OVER PANEL = NULL KHI SHOW!",
                this
            );
        }


        //---------------------------------------------------------
        // INITIAL VISUAL STATE
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

        isShowing = true;


        //---------------------------------------------------------
        // FADE
        //---------------------------------------------------------

        yield return StartCoroutine(FadeIn());


        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        if (panelRect != null)
        {
            panelRect.localScale =
                panelBaseScale;
        }


        //---------------------------------------------------------
        // INPUT SAFETY DELAY
        //---------------------------------------------------------

        Debug.Log(
            "[GameOverUIController] " +
            "FadeIn hoàn tất. Bắt đầu Input Safety Delay.",
            this
        );


        float unlockTimer = 0f;

        while (unlockTimer < buttonUnlockDelay)
        {
            unlockTimer +=
                Time.unscaledDeltaTime;

            pointerInputEnabled = false;

            retryPointerArmed = false;
            mainMenuPointerArmed = false;

            DisableEventSystemNavigation();
            ClearEventSystemSelection();

            yield return null;
        }


        //---------------------------------------------------------
        // CLEAR EVENT SYSTEM
        //---------------------------------------------------------

        DisableEventSystemNavigation();
        ClearEventSystemSelection();


        //---------------------------------------------------------
        // UNLOCK BUTTON
        //---------------------------------------------------------

        UnlockButtons();


        //---------------------------------------------------------
        // IMPORTANT:
        // Button được unlock nhưng chưa được ARM.
        //---------------------------------------------------------

        pointerInputEnabled = true;

        retryPointerArmed = false;
        mainMenuPointerArmed = false;


        //---------------------------------------------------------
        // KEEP NAVIGATION OFF
        //---------------------------------------------------------

        DisableEventSystemNavigation();
        ClearEventSystemSelection();


        //---------------------------------------------------------
        // WAIT ONE FRAME
        //---------------------------------------------------------

        yield return null;


        retryPointerArmed = false;
        mainMenuPointerArmed = false;

        ClearEventSystemSelection();


        //---------------------------------------------------------
        // READY
        //---------------------------------------------------------

        transitionStarted = false;


        Debug.Log(
            "[GameOverUIController] " +
            "GAME OVER PANEL READY | Buttons unlocked.",
            this
        );


        if (EventSystem.current != null)
        {
            Debug.Log(
                "[GameOverUIController] " +
                $"EVENT SYSTEM STATE | " +
                $"sendNavigationEvents={EventSystem.current.sendNavigationEvents} | " +
                $"Selected={(EventSystem.current.currentSelectedGameObject != null ? EventSystem.current.currentSelectedGameObject.name : "NULL")} | " +
                $"PointerInputEnabled={pointerInputEnabled} | " +
                $"RetryArmed={retryPointerArmed} | " +
                $"MainMenuArmed={mainMenuPointerArmed}",
                this
            );
        }
    }


    //=============================================================
    // BUTTON LOCK / UNLOCK
    //=============================================================

    private void LockButtons()
    {
        buttonsUnlocked = false;

        if (retryButton != null)
        {
            retryButton.interactable = false;
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.interactable = false;
        }
    }


    private void UnlockButtons()
    {
        buttonsUnlocked = true;

        if (retryButton != null)
        {
            retryButton.interactable = true;
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.interactable = true;
        }
    }


    //=============================================================
    // EVENT SYSTEM
    //=============================================================

    private void DisableEventSystemNavigation()
    {
        if (EventSystem.current == null)
            return;


        if (!eventSystemStateCaptured)
        {
            previousSendNavigationEvents =
                EventSystem.current.sendNavigationEvents;

            eventSystemStateCaptured = true;
        }


        EventSystem.current.sendNavigationEvents = false;

        EventSystem.current.SetSelectedGameObject(null);
    }


    private void RestoreEventSystemNavigation()
    {
        if (EventSystem.current == null)
            return;


        if (eventSystemStateCaptured)
        {
            EventSystem.current.sendNavigationEvents =
                previousSendNavigationEvents;

            eventSystemStateCaptured = false;
        }


        EventSystem.current.SetSelectedGameObject(null);
    }


    private void ClearEventSystemSelection()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }


    //=============================================================
    // EXP
    //=============================================================

    private void GrantRunExp()
    {
        if (expRewardGranted)
        {
            return;
        }


        PlayerProgression progression =
            FindFirstObjectByType<PlayerProgression>();


        if (progression == null)
        {
            Debug.LogWarning(
                "[GameOverUIController] " +
                "Không tìm thấy PlayerProgression. " +
                "Không thể cộng EXP.",
                this
            );

            return;
        }


        if (GameManager.Instance == null)
        {
            Debug.LogWarning(
                "[GameOverUIController] " +
                "Không tìm thấy GameManager. " +
                "Không thể tính EXP.",
                this
            );

            return;
        }


        levelBeforeRun =
            progression.Level;


        float distance =
            GetRunDistance();


        int distanceExp = 0;


        if (distancePerExpStep > 0f)
        {
            int distanceSteps =
                Mathf.FloorToInt(
                    distance /
                    distancePerExpStep
                );


            distanceExp =
                distanceSteps *
                expPerDistanceStep;
        }


        int coins =
            Mathf.Max(
                0,
                GameManager.Instance.CoinCount
            );


        int coinExp = 0;


        if (coinsPerExpStep > 0)
        {
            int coinSteps =
                coins /
                coinsPerExpStep;


            coinExp =
                coinSteps *
                expPerCoinStep;
        }


        int bonusExp =
            Mathf.Max(
                0,
                runCompletionExp
            );


        int totalExp =
            distanceExp +
            coinExp +
            bonusExp;


        lastRunExpReward =
            Mathf.Max(
                0,
                totalExp
            );


        progression.AddExp(
            lastRunExpReward
        );


        levelAfterRun =
            progression.Level;


        leveledUpThisRun =
            levelAfterRun >
            levelBeforeRun;


        expRewardGranted = true;


        Debug.Log(
            "[GameOverUIController] " +
            "RUN EXP REWARD\n" +
            $"Distance: {distance:0}m\n" +
            $"Distance EXP: +{distanceExp}\n" +
            $"Coins: {coins}\n" +
            $"Coin EXP: +{coinExp}\n" +
            $"Run Bonus: +{bonusExp}\n" +
            $"TOTAL EXP: +{lastRunExpReward}\n" +
            $"Level Before: {levelBeforeRun}\n" +
            $"Level After: {levelAfterRun}\n" +
            $"Level Up: {leveledUpThisRun}\n" +
            $"Total EXP Saved: {progression.TotalExp}",
            this
        );
    }


    //=============================================================
    // EXP UI
    //=============================================================

    private void UpdateExpUI()
    {
        if (expRewardValue != null)
        {
            expRewardValue.text =
                $"+{lastRunExpReward:N0} EXP";
        }


        if (levelValue != null)
        {
            levelValue.text =
                $"LEVEL {levelAfterRun}";
        }


        if (levelUpValue != null)
        {
            levelUpValue.gameObject.SetActive(
                leveledUpThisRun
            );


            if (leveledUpThisRun)
            {
                levelUpValue.text =
                    "LEVEL UP!";
            }
        }
    }


    //=============================================================
    // GET RUN DISTANCE
    //=============================================================

    private float GetRunDistance()
    {
        PlayerController player =
            FindFirstObjectByType<PlayerController>();


        if (player != null)
        {
            return Mathf.Max(
                0f,
                player.transform.position.z
            );
        }


        if (GameManager.Instance != null)
        {
            return Mathf.Max(
                0f,
                GameManager.Instance.Score
            );
        }


        return 0f;
    }


    //=============================================================
    // UPDATE RESULT UI
    //=============================================================

    private void UpdateResultUI()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning(
                "[GameOverUIController] " +
                "Không tìm thấy GameManager.",
                this
            );

            return;
        }


        if (scoreValue != null)
        {
            scoreValue.text =
                GameManager.Instance.ScoreInt
                    .ToString("N0");
        }


        if (highScoreValue != null)
        {
            highScoreValue.text =
                Mathf.FloorToInt(
                    GameManager.Instance.GetHighScore()
                )
                .ToString("N0");
        }


        if (coinValue != null)
        {
            coinValue.text =
                GameManager.Instance.CoinCount
                    .ToString("N0");
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
            panelBaseScale *
            hiddenScale;


        while (elapsed < fadeDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed /
                    fadeDuration
                );


            t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );


            if (canvasGroup != null)
            {
                canvasGroup.alpha =
                    Mathf.Lerp(
                        startAlpha,
                        1f,
                        t
                    );
            }


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

        expRewardGranted = false;

        buttonsUnlocked = false;

        transitionStarted = false;


        pointerInputEnabled = false;

        retryPointerArmed = false;

        mainMenuPointerArmed = false;


        lastRunExpReward = 0;

        levelBeforeRun = 0;

        levelAfterRun = 0;

        leveledUpThisRun = false;


        LockButtons();


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


        if (levelUpValue != null)
        {
            levelUpValue.gameObject.SetActive(false);
        }
    }


    //=============================================================
    // RETRY
    //=============================================================

    public void Retry()
    {
        if (!buttonsUnlocked)
        {
            Debug.Log(
                "[GameOverUIController] " +
                "Retry() bị chặn vì Button chưa được unlock.",
                this
            );

            return;
        }


        if (!retryPointerArmed)
        {
            Debug.Log(
                "[GameOverUIController] " +
                "Retry() bị CHẶN: chưa có PointerDown mới " +
                "sau khi Game Over Panel READY.",
                this
            );

            return;
        }


        if (transitionStarted)
        {
            return;
        }


        transitionStarted = true;


        retryPointerArmed = false;

        pointerInputEnabled = false;


        Debug.Log(
            "[GameOverUIController] " +
            "RETRY() ĐÃ ĐƯỢC GỌI " +
            "BỞI POINTER INPUT HỢP LỆ!",
            this
        );


        Debug.Log(
            "[GameOverUIController] Retry Button = " +
            (retryButton != null
                ? retryButton.name
                : "NULL"),
            this
        );


        if (EventSystem.current != null)
        {
            Debug.Log(
                "[GameOverUIController] Retry EventSystem | " +
                $"sendNavigationEvents={EventSystem.current.sendNavigationEvents} | " +
                $"Selected={(EventSystem.current.currentSelectedGameObject != null ? EventSystem.current.currentSelectedGameObject.name : "NULL")}",
                this
            );
        }


        RestoreEventSystemNavigation();


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
        if (!buttonsUnlocked)
        {
            Debug.Log(
                "[GameOverUIController] " +
                "GoToMainMenu() bị chặn vì Button chưa được unlock.",
                this
            );

            return;
        }


        if (!mainMenuPointerArmed)
        {
            Debug.Log(
                "[GameOverUIController] " +
                "GoToMainMenu() bị CHẶN: chưa có PointerDown mới " +
                "sau khi Game Over Panel READY.",
                this
            );

            return;
        }


        if (transitionStarted)
        {
            return;
        }


        transitionStarted = true;


        mainMenuPointerArmed = false;

        pointerInputEnabled = false;


        RestoreEventSystemNavigation();


        Time.timeScale = 1f;

        AudioListener.pause = false;


        if (!Application.CanStreamedLevelBeLoaded(
            mainMenuSceneName))
        {
            transitionStarted = false;

            pointerInputEnabled = true;


            Debug.LogError(
                $"[GameOverUIController] " +
                $"Không thể load Scene '{mainMenuSceneName}'. " +
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
    // PUBLIC STATE
    //=============================================================

    public bool IsShowing
    {
        get
        {
            return isShowing;
        }
    }


    //=============================================================
    // POINTER GUARD COMPONENT
    //=============================================================

    private class GameOverButtonPointerGuard :
        MonoBehaviour,
        IPointerDownHandler
    {
        public enum ButtonType
        {
            Retry,
            MainMenu
        }


        private GameOverUIController controller;

        private ButtonType buttonType;


        public void Initialize(
            GameOverUIController target,
            ButtonType type
        )
        {
            controller = target;

            buttonType = type;
        }


        public void OnPointerDown(
            PointerEventData eventData
        )
        {
            if (controller == null)
                return;


            controller.NotifyPointerDown(
                buttonType
            );
        }
    }
}