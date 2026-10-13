using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class GameOverUIController : MonoBehaviour
{
    // =====================================================================
    // GAME OVER PANEL
    // =====================================================================

    [Header("Game Over Panel")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private CanvasGroup gameOverCanvasGroup;
    [SerializeField] private RectTransform gameOverPanelRect;

    // =====================================================================
    // REVIVE PANEL
    // =====================================================================

    [Header("Revive Panel")]
    [SerializeField] private GameObject revivePanel;
    [SerializeField] private CanvasGroup reviveCanvasGroup;
    [SerializeField] private RectTransform revivePanelRect;

    // =====================================================================
    // BACKDROP
    // =====================================================================

    [Header("Backdrop")]
    [SerializeField] private GameObject backdrop;
    [SerializeField] private CanvasGroup backdropCanvasGroup;

    // =====================================================================
    // RESULT UI
    // =====================================================================

    [Header("Result")]
    [SerializeField] private TMP_Text scoreValue;
    [SerializeField] private TMP_Text highScoreValue;
    [SerializeField] private TMP_Text coinValue;

    // =====================================================================
    // EXP UI
    // =====================================================================

    [Header("EXP")]
    [SerializeField] private TMP_Text expRewardValue;
    [SerializeField] private TMP_Text levelValue;
    [SerializeField] private TMP_Text levelUpValue;

    // =====================================================================
    // GAME OVER BUTTONS
    // =====================================================================

    [Header("Game Over Buttons")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button mainMenuButton;

    // =====================================================================
    // REVIVE UI
    // =====================================================================

    [Header("Revive UI")]
    [SerializeField] private Button reviveButton;
    [SerializeField] private TMP_Text reviveCostValue;
    [SerializeField] private TMP_Text reviveGemValue;
    [SerializeField] private TMP_Text reviveStatusValue;
    [SerializeField] private TMP_Text reviveTimerValue;
    [SerializeField] private Image reviveTimerFill;

    // =====================================================================
    // TRANSITION SETTINGS
    // =====================================================================

    [Header("Transition")]
    [SerializeField] private float reviveShowDelay = 1.5f;
    [SerializeField] private float fadeDuration = 0.55f;
    [SerializeField] private float hiddenScale = 0.94f;

    // =====================================================================
    // AUDIO
    // =====================================================================

    [Header("Audio")]
    [SerializeField] private bool muteAllAudioOnGameOver = true;

    [Tooltip(
        "AudioSource dùng riêng cho âm thanh khi Game Over Panel xuất hiện."
    )]
    [SerializeField] private AudioSource gameOverAudioSource;

    [Tooltip(
        "Âm thanh được phát sau khi Game Over Panel fade-in hoàn tất."
    )]
    [SerializeField] private AudioClip gameOverPanelOpenClip;

    [SerializeField, Range(0f, 1f)]
    private float gameOverPanelOpenVolume = 1f;

    // =====================================================================
    // SCENE
    // =====================================================================

    [Header("Scene")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    // =====================================================================
    // EXP SETTINGS
    // =====================================================================

    [Header("EXP Reward")]
    [SerializeField] private int distancePerExpStep = 100;
    [SerializeField] private int expPerDistanceStep = 10;
    [SerializeField] private int coinsPerExpStep = 10;
    [SerializeField] private int expPerCoinStep = 5;
    [SerializeField] private int runCompletionExp = 20;

    // =====================================================================
    // BUTTON SAFETY
    // =====================================================================

    [Header("Button Safety")]
    [SerializeField] private float buttonUnlockDelay = 0.15f;

    // =====================================================================
    // INTERNAL STATE
    // =====================================================================

    private Vector3 gameOverBaseScale;
    private Vector3 reviveBaseScale;

    private Coroutine transitionRoutine;
    private Coroutine reviveUnlockRoutine;

    private bool gameOverSequenceStarted;
    private bool revivePanelShowing;
    private bool reviveButtonUnlocked;
    private bool reviveTransitionStarted;

    private bool buttonsUnlocked;
    private bool pointerInputEnabled;
    private bool transitionStarted;

    private bool retryPointerArmed;
    private bool mainMenuPointerArmed;

    private float reviveTimerInitialDuration;

    // =====================================================================
    // AUDIO STATE
    // =====================================================================

    private bool gameOverPanelOpenSoundPlayed;

    // =====================================================================
    // EXP STATE
    // =====================================================================

    private bool expRewardGranted;

    private int lastRunExpReward;
    private int levelBeforeRun;
    private int levelAfterRun;
    private bool leveledUpThisRun;

    // =====================================================================
    // EVENT SYSTEM
    // =====================================================================

    private bool previousSendNavigationEvents;
    private bool eventSystemStateCaptured;

    // =====================================================================
    // UNITY
    // =====================================================================

    private void Awake()
    {
        CacheReferences();
        SetupButtons();
        SetupPointerGuards();
        SetupAudio();

        gameOverPanelOpenSoundPlayed = false;

        HideEverythingImmediate();
    }

    private void Start()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        CaptureEventSystemState();

        GameManager gameManager = GameManager.Instance;

        if (gameManager == null)
        {
            Debug.LogWarning(
                "[GameOverUIController] GameManager.Instance == NULL."
            );

            return;
        }

        gameManager.OnDeathPending += HandleDeathPending;
        gameManager.OnRevive += HandleRevive;
        gameManager.OnGameOver += StartGameOverSequence;
    }

    private void Update()
    {
        if (revivePanelShowing)
        {
            UpdateReviveUI();

            if (!reviveButtonUnlocked)
            {
                EventSystem eventSystem = EventSystem.current;

                if (eventSystem != null)
                    eventSystem.SetSelectedGameObject(null);
            }
        }
    }

    private void OnDestroy()
    {
        StopTransitionRoutine();
        StopReviveUnlockRoutine();

        GameManager gameManager = GameManager.Instance;

        if (gameManager != null)
        {
            gameManager.OnDeathPending -= HandleDeathPending;
            gameManager.OnRevive -= HandleRevive;
            gameManager.OnGameOver -= StartGameOverSequence;
        }

        RestoreEventSystemState();
    }

    // =====================================================================
    // CACHE
    // =====================================================================

    private void CacheReferences()
    {
        if (gameOverPanel != null)
        {
            if (gameOverCanvasGroup == null)
                gameOverCanvasGroup =
                    gameOverPanel.GetComponent<CanvasGroup>();

            if (gameOverCanvasGroup == null)
                gameOverCanvasGroup =
                    gameOverPanel.AddComponent<CanvasGroup>();
        }

        if (revivePanel != null)
        {
            if (reviveCanvasGroup == null)
                reviveCanvasGroup =
                    revivePanel.GetComponent<CanvasGroup>();

            if (reviveCanvasGroup == null)
                reviveCanvasGroup =
                    revivePanel.AddComponent<CanvasGroup>();
        }

        if (backdrop != null)
        {
            if (backdropCanvasGroup == null)
                backdropCanvasGroup =
                    backdrop.GetComponent<CanvasGroup>();

            if (backdropCanvasGroup == null)
                backdropCanvasGroup =
                    backdrop.AddComponent<CanvasGroup>();
        }

        if (gameOverPanelRect != null)
            gameOverBaseScale =
                gameOverPanelRect.localScale;

        if (revivePanelRect != null)
            reviveBaseScale =
                revivePanelRect.localScale;
    }

    // =====================================================================
    // AUDIO SETUP
    // =====================================================================

    private void SetupAudio()
    {
        if (gameOverAudioSource == null)
            return;

        gameOverAudioSource.playOnAwake = false;
        gameOverAudioSource.loop = false;

        // Cho phép âm thanh UI Game Over phát ngay cả khi
        // AudioListener.pause = true.
        gameOverAudioSource.ignoreListenerPause = true;
    }

    private void PlayGameOverPanelOpenSound()
    {
        if (gameOverPanelOpenSoundPlayed)
            return;

        if (gameOverPanelOpenClip == null)
            return;

        if (gameOverAudioSource == null)
            return;

        gameOverPanelOpenSoundPlayed = true;

        gameOverAudioSource.PlayOneShot(
            gameOverPanelOpenClip,
            Mathf.Clamp01(gameOverPanelOpenVolume)
        );
    }

    // =====================================================================
    // BUTTON SETUP
    // =====================================================================

    private void SetupButtons()
    {
        if (retryButton != null)
        {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(Retry);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(GoToMainMenu);
        }

        if (reviveButton != null)
        {
            reviveButton.onClick.RemoveAllListeners();
            reviveButton.onClick.AddListener(
                HandleReviveButtonClick
            );
        }
    }

    // =====================================================================
    // POINTER GUARDS
    // =====================================================================

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
                ButtonType.Retry
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
                ButtonType.MainMenu
            );
        }
    }

    private enum ButtonType
    {
        Retry,
        MainMenu
    }

    private void NotifyPointerDown(ButtonType buttonType)
    {
        if (!pointerInputEnabled)
            return;

        if (!buttonsUnlocked)
            return;

        if (transitionStarted)
            return;

        if (buttonType == ButtonType.Retry)
            retryPointerArmed = true;

        if (buttonType == ButtonType.MainMenu)
            mainMenuPointerArmed = true;
    }

    // =====================================================================
    // DEATH PENDING
    // =====================================================================

    private void HandleDeathPending()
    {
        if (gameOverSequenceStarted)
            return;

        GameManager gameManager = GameManager.Instance;

        if (gameManager == null)
            return;

        StartReviveSequence();

        Debug.Log(
            "[GameOverUIController] DEATH PENDING | " +
            "Revive timer = " +
            gameManager.ReviveDecisionTimeRemaining.ToString("F2") +
            "s"
        );
    }

    // =====================================================================
    // REVIVE SEQUENCE
    // =====================================================================

    private void StartReviveSequence()
    {
        StopTransitionRoutine();
        StopReviveUnlockRoutine();

        gameOverSequenceStarted = false;

        revivePanelShowing = false;
        reviveButtonUnlocked = false;
        reviveTransitionStarted = false;

        buttonsUnlocked = false;
        pointerInputEnabled = false;
        transitionStarted = false;

        retryPointerArmed = false;
        mainMenuPointerArmed = false;

        if (retryButton != null)
            retryButton.interactable = false;

        if (mainMenuButton != null)
            mainMenuButton.interactable = false;

        GameManager gameManager = GameManager.Instance;

        if (gameManager == null)
            return;

        reviveTimerInitialDuration =
            Mathf.Max(
                gameManager.ReviveDecisionTimeRemaining,
                0.0001f
            );

        transitionRoutine =
            StartCoroutine(
                ReviveSequence()
            );
    }

    private IEnumerator ReviveSequence()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        HideGameOverImmediate();

        PrepareReviveVisual();

        float delay = Mathf.Max(
            reviveShowDelay,
            0f
        );

        float timer = 0f;

        while (timer < delay)
        {
            GameManager gameManager =
                GameManager.Instance;

            if (gameManager == null)
                yield break;

            if (!gameManager.IsDeathPending)
                yield break;

            timer += Time.unscaledDeltaTime;

            yield return null;
        }

        GameManager currentManager =
            GameManager.Instance;

        if (currentManager == null)
            yield break;

        if (!currentManager.IsDeathPending)
            yield break;

        revivePanelShowing = true;

        yield return StartCoroutine(
            FadeReviveIn()
        );

        if (!revivePanelShowing)
            yield break;

        UpdateReviveUI();

        StopReviveUnlockRoutine();

        reviveUnlockRoutine =
            StartCoroutine(
                UnlockReviveButtonNextFrame()
            );

        transitionRoutine = null;

        Debug.Log(
            "[GameOverUIController] REVIVE PANEL FADED IN."
        );
    }

    // =====================================================================
    // REVIVE VISUAL
    // =====================================================================

    private void PrepareReviveVisual()
    {
        if (backdrop != null)
        {
            backdrop.SetActive(true);

            if (backdropCanvasGroup != null)
                backdropCanvasGroup.alpha = 0f;
        }

        if (revivePanel != null)
        {
            revivePanel.SetActive(true);

            if (reviveCanvasGroup != null)
                reviveCanvasGroup.alpha = 0f;

            if (revivePanelRect != null)
            {
                revivePanelRect.localScale =
                    reviveBaseScale *
                    hiddenScale;
            }
        }

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    private IEnumerator FadeReviveIn()
    {
        float duration =
            Mathf.Max(
                fadeDuration,
                0f
            );

        if (duration <= 0f)
        {
            if (backdropCanvasGroup != null)
                backdropCanvasGroup.alpha = 1f;

            if (reviveCanvasGroup != null)
                reviveCanvasGroup.alpha = 1f;

            if (revivePanelRect != null)
                revivePanelRect.localScale =
                    reviveBaseScale;

            yield break;
        }

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    timer / duration
                );

            float eased =
                1f -
                Mathf.Pow(
                    1f - t,
                    3f
                );

            if (backdropCanvasGroup != null)
                backdropCanvasGroup.alpha =
                    eased;

            if (reviveCanvasGroup != null)
                reviveCanvasGroup.alpha =
                    eased;

            if (revivePanelRect != null)
            {
                revivePanelRect.localScale =
                    Vector3.LerpUnclamped(
                        reviveBaseScale *
                        hiddenScale,
                        reviveBaseScale,
                        eased
                    );
            }

            yield return null;
        }

        if (backdropCanvasGroup != null)
            backdropCanvasGroup.alpha = 1f;

        if (reviveCanvasGroup != null)
            reviveCanvasGroup.alpha = 1f;

        if (revivePanelRect != null)
            revivePanelRect.localScale =
                reviveBaseScale;
    }

    // =====================================================================
    // REVIVE BUTTON
    // =====================================================================

    private IEnumerator UnlockReviveButtonNextFrame()
    {
        yield return null;

        reviveUnlockRoutine = null;

        if (!revivePanelShowing)
            yield break;

        GameManager gameManager =
            GameManager.Instance;

        if (gameManager == null)
            yield break;

        reviveButtonUnlocked =
            gameManager.CanRevive;

        if (reviveButton != null)
            reviveButton.interactable =
                reviveButtonUnlocked;

        UpdateReviveUI();
    }

    private void HandleReviveButtonClick()
    {
        if (!revivePanelShowing)
            return;

        if (!reviveButtonUnlocked)
            return;

        if (reviveTransitionStarted)
            return;

        GameManager gameManager =
            GameManager.Instance;

        if (gameManager == null)
            return;

        if (!gameManager.CanRevive)
        {
            reviveButtonUnlocked = false;

            if (reviveButton != null)
                reviveButton.interactable = false;

            UpdateReviveUI();

            return;
        }

        reviveTransitionStarted = true;
        reviveButtonUnlocked = false;

        if (reviveButton != null)
            reviveButton.interactable = false;

        bool revived =
            gameManager.TryRevive();

        if (!revived)
        {
            reviveTransitionStarted = false;

            reviveButtonUnlocked =
                gameManager.CanRevive;

            if (reviveButton != null)
                reviveButton.interactable =
                    reviveButtonUnlocked;

            UpdateReviveUI();

            return;
        }

        Debug.Log(
            "[GameOverUIController] REVIVE REQUEST ACCEPTED."
        );
    }

    // =====================================================================
    // REVIVE SUCCESS
    // =====================================================================

    private void HandleRevive()
    {
        StopTransitionRoutine();
        StopReviveUnlockRoutine();

        reviveTransitionStarted = true;
        reviveButtonUnlocked = false;

        buttonsUnlocked = false;
        pointerInputEnabled = false;

        retryPointerArmed = false;
        mainMenuPointerArmed = false;

        if (reviveButton != null)
            reviveButton.interactable = false;

        StartCoroutine(
            ReviveExitSequence()
        );
    }

    private IEnumerator ReviveExitSequence()
    {
        yield return StartCoroutine(
            FadeReviveOut()
        );

        HideEverythingImmediate();

        revivePanelShowing = false;
        reviveTransitionStarted = false;

        Debug.Log(
            "[GameOverUIController] REVIVE UI CLOSED."
        );
    }

    // =====================================================================
    // REVIVE FADE OUT
    // =====================================================================

    private IEnumerator FadeReviveOut()
    {
        if (revivePanel == null)
            yield break;

        float duration =
            Mathf.Max(
                fadeDuration,
                0f
            );

        float startAlpha =
            reviveCanvasGroup != null
                ? reviveCanvasGroup.alpha
                : 1f;

        float startBackdropAlpha =
            backdropCanvasGroup != null
                ? backdropCanvasGroup.alpha
                : 1f;

        Vector3 startScale =
            revivePanelRect != null
                ? revivePanelRect.localScale
                : reviveBaseScale;

        if (duration <= 0f)
        {
            if (reviveCanvasGroup != null)
                reviveCanvasGroup.alpha = 0f;

            if (backdropCanvasGroup != null)
                backdropCanvasGroup.alpha = 0f;

            yield break;
        }

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    timer / duration
                );

            float eased =
                t * t;

            if (reviveCanvasGroup != null)
                reviveCanvasGroup.alpha =
                    Mathf.Lerp(
                        startAlpha,
                        0f,
                        eased
                    );

            if (backdropCanvasGroup != null)
                backdropCanvasGroup.alpha =
                    Mathf.Lerp(
                        startBackdropAlpha,
                        0f,
                        eased
                    );

            if (revivePanelRect != null)
            {
                revivePanelRect.localScale =
                    Vector3.Lerp(
                        startScale,
                        reviveBaseScale *
                        hiddenScale,
                        eased
                    );
            }

            yield return null;
        }

        if (reviveCanvasGroup != null)
            reviveCanvasGroup.alpha = 0f;

        if (backdropCanvasGroup != null)
            backdropCanvasGroup.alpha = 0f;

        if (revivePanelRect != null)
            revivePanelRect.localScale =
                reviveBaseScale *
                hiddenScale;
    }

    // =====================================================================
    // REVIVE UI
    // =====================================================================

    private void UpdateReviveUI()
    {
        if (!revivePanelShowing)
            return;

        GameManager gameManager =
            GameManager.Instance;

        if (gameManager == null)
            return;

        int cost =
            gameManager.ReviveGemCost;

        int currentGem =
            gameManager.GetGem();

        float remaining =
            Mathf.Max(
                gameManager.ReviveDecisionTimeRemaining,
                0f
            );

        bool isPending =
            gameManager.IsDeathPending;

        bool hasUsedRevive =
            gameManager.HasUsedRevive;

        bool timerAvailable =
            remaining > 0f;

        bool hasEnoughGem =
            currentGem >= cost;

        bool canRevive =
            gameManager.CanRevive;

        if (reviveCostValue != null)
        {
            reviveCostValue.SetText(
                "{0} GEM",
                cost
            );
        }

        if (reviveGemValue != null)
        {
            reviveGemValue.SetText(
                "{0}",
                currentGem
            );
        }

        if (reviveTimerValue != null)
        {
            reviveTimerValue.SetText(
                "{0:0.0}",
                remaining
            );
        }

        if (reviveTimerFill != null)
        {
            float normalized =
                reviveTimerInitialDuration > 0f
                    ? remaining /
                      reviveTimerInitialDuration
                    : 0f;

            normalized =
                Mathf.Clamp01(
                    normalized
                );

            reviveTimerFill.fillAmount =
                normalized;

            if (normalized <= 0.33f)
            {
                reviveTimerFill.color =
                    new Color(
                        1f,
                        0.30f,
                        0.30f,
                        1f
                    );
            }
            else if (normalized <= 0.66f)
            {
                reviveTimerFill.color =
                    new Color(
                        1f,
                        0.70f,
                        0.15f,
                        1f
                    );
            }
            else
            {
                reviveTimerFill.color =
                    new Color(
                        0.10f,
                        0.90f,
                        0.70f,
                        1f
                    );
            }
        }

        if (reviveStatusValue != null)
        {
            if (!isPending ||
                !timerAvailable)
            {
                reviveStatusValue.SetText(
                    "ĐÃ HẾT THỜI GIAN HỒI SINH"
                );
            }
            else if (hasUsedRevive)
            {
                reviveStatusValue.SetText(
                    "ĐÃ SỬ DỤNG HỒI SINH TRONG VÁN NÀY"
                );
            }
            else if (!hasEnoughGem)
            {
                reviveStatusValue.SetText(
                    "KHÔNG ĐỦ GEM"
                );
            }
            else if (canRevive)
            {
                reviveStatusValue.SetText(
                    "SẴN SÀNG HỒI SINH"
                );
            }
            else
            {
                reviveStatusValue.SetText(
                    "KHÔNG THỂ HỒI SINH"
                );
            }
        }

        if (reviveButton != null)
        {
            reviveButton.interactable =
                reviveButtonUnlocked &&
                canRevive &&
                !reviveTransitionStarted;
        }
    }

    // =====================================================================
    // FINAL GAME OVER
    // =====================================================================

    private void StartGameOverSequence()
    {
        if (gameOverSequenceStarted)
            return;

        gameOverSequenceStarted = true;

        StopTransitionRoutine();
        StopReviveUnlockRoutine();

        revivePanelShowing = false;
        reviveButtonUnlocked = false;
        reviveTransitionStarted = false;

        buttonsUnlocked = false;
        pointerInputEnabled = false;
        transitionStarted = false;

        retryPointerArmed = false;
        mainMenuPointerArmed = false;

        gameOverPanelOpenSoundPlayed = false;

        if (reviveButton != null)
            reviveButton.interactable = false;

        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);

        transitionRoutine =
            StartCoroutine(
                FinalGameOverSequence()
            );
    }

    private IEnumerator FinalGameOverSequence()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        // ================================================================
        // 1. FADE REVIVE OUT
        // ================================================================

        if (revivePanel != null &&
            revivePanel.activeSelf)
        {
            yield return StartCoroutine(
                FadeReviveOut()
            );
        }

        if (revivePanel != null)
            revivePanel.SetActive(false);

        // ================================================================
        // 2. UPDATE RESULT
        // ================================================================

        UpdateResultUI();

        GrantRunExp();
        UpdateExpUI();

        // ================================================================
        // 3. PAUSE GAME
        // ================================================================

        Time.timeScale = 0f;

        if (muteAllAudioOnGameOver)
            AudioListener.pause = true;

        // ================================================================
        // 4. SHOW GAME OVER
        // ================================================================

        PrepareGameOverVisual();

        yield return StartCoroutine(
            FadeGameOverIn()
        );

        // ================================================================
        // 4.5. GAME OVER PANEL AUDIO
        // ================================================================

        PlayGameOverPanelOpenSound();

        // ================================================================
        // 5. UNLOCK BUTTONS
        // ================================================================

        float unlockDelay =
            Mathf.Max(
                buttonUnlockDelay,
                0f
            );

        float timer = 0f;

        while (timer < unlockDelay)
        {
            timer +=
                Time.unscaledDeltaTime;

            yield return null;
        }

        UnlockFinalButtons();

        transitionRoutine = null;

        Debug.Log(
            "[GameOverUIController] FINAL GAME OVER READY."
        );
    }

    // =====================================================================
    // GAME OVER VISUAL
    // =====================================================================

    private void PrepareGameOverVisual()
    {
        if (backdrop != null)
            backdrop.SetActive(true);

        if (backdropCanvasGroup != null)
            backdropCanvasGroup.alpha = 1f;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);

            if (gameOverCanvasGroup != null)
                gameOverCanvasGroup.alpha = 0f;

            if (gameOverPanelRect != null)
            {
                gameOverPanelRect.localScale =
                    gameOverBaseScale *
                    hiddenScale;
            }
        }
    }

    private IEnumerator FadeGameOverIn()
    {
        float duration =
            Mathf.Max(
                fadeDuration,
                0f
            );

        if (duration <= 0f)
        {
            if (gameOverCanvasGroup != null)
                gameOverCanvasGroup.alpha = 1f;

            if (gameOverPanelRect != null)
                gameOverPanelRect.localScale =
                    gameOverBaseScale;

            yield break;
        }

        float timer = 0f;

        while (timer < duration)
        {
            timer +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    timer / duration
                );

            float eased =
                1f -
                Mathf.Pow(
                    1f - t,
                    3f
                );

            if (gameOverCanvasGroup != null)
            {
                gameOverCanvasGroup.alpha =
                    eased;
            }

            if (gameOverPanelRect != null)
            {
                gameOverPanelRect.localScale =
                    Vector3.LerpUnclamped(
                        gameOverBaseScale *
                        hiddenScale,
                        gameOverBaseScale,
                        eased
                    );
            }

            yield return null;
        }

        if (gameOverCanvasGroup != null)
            gameOverCanvasGroup.alpha = 1f;

        if (gameOverPanelRect != null)
            gameOverPanelRect.localScale =
                gameOverBaseScale;
    }

    // =====================================================================
    // FINAL BUTTONS
    // =====================================================================

    private void UnlockFinalButtons()
    {
        buttonsUnlocked = true;
        pointerInputEnabled = true;

        if (retryButton != null)
            retryButton.interactable = true;

        if (mainMenuButton != null)
            mainMenuButton.interactable = true;

        EventSystem eventSystem =
            EventSystem.current;

        if (eventSystem != null)
            eventSystem.SetSelectedGameObject(null);
    }

    // =====================================================================
    // RESULT
    // =====================================================================

    private void UpdateResultUI()
    {
        GameManager gameManager =
            GameManager.Instance;

        if (gameManager == null)
            return;

        int score =
            gameManager.ScoreInt;

        int highScore = 0;

        int coins =
            gameManager.CoinCount;

        FirestorePlayerDataManager firestoreManager =
            FirestorePlayerDataManager.Instance;

        if (firestoreManager != null &&
            firestoreManager.CurrentPlayerData != null)
        {
            highScore =
                firestoreManager.CurrentPlayerData.bestScore;
        }

        if (scoreValue != null)
            scoreValue.SetText(
                "{0}",
                score
            );

        if (highScoreValue != null)
            highScoreValue.SetText(
                "{0}",
                highScore
            );

        if (coinValue != null)
            coinValue.SetText(
                "{0}",
                coins
            );
    }

    // =====================================================================
    // EXP
    // =====================================================================

    private void GrantRunExp()
    {
        if (expRewardGranted)
            return;

        PlayerProgression progression =
            FindFirstObjectByType<PlayerProgression>();

        if (progression == null)
        {
            Debug.LogWarning(
                "[GameOverUIController] PlayerProgression not found."
            );

            return;
        }

        GameManager gameManager =
            GameManager.Instance;

        if (gameManager == null)
            return;

        levelBeforeRun =
            progression.Level;

        int distance =
            GetRunDistance();

        int coins =
            gameManager.CoinCount;

        int distanceExp = 0;

        if (distancePerExpStep > 0 &&
            expPerDistanceStep > 0)
        {
            distanceExp =
                (distance /
                 distancePerExpStep) *
                expPerDistanceStep;
        }

        int coinExp = 0;

        if (coinsPerExpStep > 0 &&
            expPerCoinStep > 0)
        {
            coinExp =
                (coins /
                 coinsPerExpStep) *
                expPerCoinStep;
        }

        int totalExp =
            distanceExp +
            coinExp +
            runCompletionExp;

        if (totalExp < 0)
            totalExp = 0;

        lastRunExpReward =
            totalExp;

        progression.AddExp(
            totalExp
        );

        levelAfterRun =
            progression.Level;

        leveledUpThisRun =
            levelAfterRun >
            levelBeforeRun;

        expRewardGranted = true;
    }

    private void UpdateExpUI()
    {
        if (expRewardValue != null)
        {
            expRewardValue.SetText(
                "+{0} EXP",
                lastRunExpReward
            );
        }

        if (levelValue != null)
        {
            levelValue.SetText(
                "CẤP {0}",
                levelAfterRun
            );
        }

        if (levelUpValue != null)
        {
            levelUpValue.gameObject.SetActive(
                leveledUpThisRun
            );

            if (leveledUpThisRun)
                levelUpValue.SetText(
                    "LÊN CẤP!"
                );
        }
    }

    // =====================================================================
    // DISTANCE
    // =====================================================================

    private int GetRunDistance()
    {
        PlayerController player =
            FindFirstObjectByType<PlayerController>();

        if (player != null)
        {
            float z =
                player.transform.position.z;

            if (z > 0f)
            {
                if (z >= int.MaxValue)
                    return int.MaxValue;

                return Mathf.FloorToInt(z);
            }
        }

        GameManager gameManager =
            GameManager.Instance;

        if (gameManager != null)
            return gameManager.ScoreInt;

        return 0;
    }

    // =====================================================================
    // RETRY
    // =====================================================================

    private void Retry()
    {
        if (!buttonsUnlocked)
            return;

        if (!retryPointerArmed)
            return;

        if (transitionStarted)
            return;

        transitionStarted = true;

        buttonsUnlocked = false;
        pointerInputEnabled = false;

        retryPointerArmed = false;
        mainMenuPointerArmed = false;

        Time.timeScale = 1f;
        AudioListener.pause = false;

        RestoreEventSystemState();

        Scene activeScene =
            SceneManager.GetActiveScene();

        SceneManager.LoadScene(
            activeScene.buildIndex
        );
    }

    // =====================================================================
    // MAIN MENU
    // =====================================================================

    private void GoToMainMenu()
    {
        if (!buttonsUnlocked)
            return;

        if (!mainMenuPointerArmed)
            return;

        if (transitionStarted)
            return;

        transitionStarted = true;

        buttonsUnlocked = false;
        pointerInputEnabled = false;

        Time.timeScale = 1f;
        AudioListener.pause = false;

        mainMenuPointerArmed = false;
        retryPointerArmed = false;

        RestoreEventSystemState();

        if (!Application.CanStreamedLevelBeLoaded(
                mainMenuSceneName))
        {
            Debug.LogError(
                "[GameOverUIController] Không thể load Scene Main Menu: " +
                mainMenuSceneName
            );

            transitionStarted = false;
            buttonsUnlocked = true;
            pointerInputEnabled = true;

            return;
        }

        SceneManager.LoadScene(
            mainMenuSceneName
        );
    }

    // =====================================================================
    // HIDE
    // =====================================================================

    private void HideGameOverImmediate()
    {
        if (gameOverCanvasGroup != null)
            gameOverCanvasGroup.alpha = 0f;

        if (gameOverPanelRect != null)
            gameOverPanelRect.localScale =
                gameOverBaseScale *
                hiddenScale;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    private void HideEverythingImmediate()
    {
        if (gameOverCanvasGroup != null)
            gameOverCanvasGroup.alpha = 0f;

        if (reviveCanvasGroup != null)
            reviveCanvasGroup.alpha = 0f;

        if (backdropCanvasGroup != null)
            backdropCanvasGroup.alpha = 0f;

        if (gameOverPanelRect != null)
            gameOverPanelRect.localScale =
                gameOverBaseScale *
                hiddenScale;

        if (revivePanelRect != null)
            revivePanelRect.localScale =
                reviveBaseScale *
                hiddenScale;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (revivePanel != null)
            revivePanel.SetActive(false);

        if (backdrop != null)
            backdrop.SetActive(false);

        revivePanelShowing = false;
        reviveButtonUnlocked = false;
        reviveTransitionStarted = false;

        buttonsUnlocked = false;
        pointerInputEnabled = false;
    }

    // =====================================================================
    // COROUTINE CONTROL
    // =====================================================================

    private void StopTransitionRoutine()
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(
                transitionRoutine
            );

            transitionRoutine = null;
        }
    }

    private void StopReviveUnlockRoutine()
    {
        if (reviveUnlockRoutine != null)
        {
            StopCoroutine(
                reviveUnlockRoutine
            );

            reviveUnlockRoutine = null;
        }
    }

    // =====================================================================
    // EVENT SYSTEM
    // =====================================================================

    private void CaptureEventSystemState()
    {
        if (eventSystemStateCaptured)
            return;

        EventSystem eventSystem =
            EventSystem.current;

        if (eventSystem == null)
            return;

        previousSendNavigationEvents =
            eventSystem.sendNavigationEvents;

        eventSystemStateCaptured = true;
    }

    private void RestoreEventSystemState()
    {
        EventSystem eventSystem =
            EventSystem.current;

        if (eventSystem == null)
            return;

        if (eventSystemStateCaptured)
        {
            eventSystem.sendNavigationEvents =
                previousSendNavigationEvents;

            eventSystemStateCaptured = false;
        }
        else
        {
            eventSystem.sendNavigationEvents = true;
        }

        eventSystem.SetSelectedGameObject(null);
    }

    // =====================================================================
    // POINTER GUARD
    // =====================================================================

    private sealed class GameOverButtonPointerGuard :
        MonoBehaviour,
        IPointerDownHandler
    {
        private GameOverUIController controller;
        private ButtonType buttonType;

        public void Initialize(
            GameOverUIController owner,
            ButtonType type)
        {
            controller = owner;
            buttonType = type;
        }

        public void OnPointerDown(
            PointerEventData eventData)
        {
            if (controller == null)
                return;

            controller.NotifyPointerDown(
                buttonType
            );
        }
    }
}
