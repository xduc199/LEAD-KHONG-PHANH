using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameplayHUDAnimator : MonoBehaviour
{
    //==============================================================
    // SCORE
    //==============================================================

    [Header("Score")]
    [SerializeField] private TMP_Text scoreValue;


    //==============================================================
    // COIN
    //==============================================================

    [Header("Coin")]
    [SerializeField] private TMP_Text coinValue;
    [SerializeField] private Image coinIcon;


    //==============================================================
    // DISTANCE
    //==============================================================

    [Header("Distance")]
    [SerializeField] private TMP_Text distanceValue;


    //==============================================================
    // SPEED
    //==============================================================

    [Header("Speed")]
    [SerializeField] private TMP_Text speedValue;


    //==============================================================
    // PHOTON
    //==============================================================

    [Header("Photon")]
    [SerializeField] private GameObject photonHUD;
    [SerializeField] private CanvasGroup photonCanvasGroup;
    [SerializeField] private RectTransform photonRect;

    [SerializeField] private TMP_Text photonTitle;
    [SerializeField] private Image photonFill;


    //==============================================================
    // PAUSE
    //==============================================================

    [Header("Pause")]
    [SerializeField] private Button pauseButton;


    //==============================================================
    // SCORE POP
    //==============================================================

    [Header("Score Pop")]
    [SerializeField] private float scorePopScale = 1.08f;
    [SerializeField] private float scorePopDuration = 0.12f;


    //==============================================================
    // COIN POP
    //==============================================================

    [Header("Coin Pop")]
    [SerializeField] private float coinPopScale = 1.16f;
    [SerializeField] private float coinPopDuration = 0.14f;


    //==============================================================
    // COIN ICON POP
    //==============================================================

    [Header("Coin Icon Pop")]
    [SerializeField] private float coinIconPopScale = 1.22f;


    //==============================================================
    // PAUSE POP
    //==============================================================

    [Header("Pause Pop")]
    [SerializeField] private float pausePressedScale = 0.90f;
    [SerializeField] private float pausePopDuration = 0.08f;


    //==============================================================
    // SPEED
    //==============================================================

    [Header("Speed Animation")]
    [SerializeField] private float speedMinScale = 1f;
    [SerializeField] private float speedMaxScale = 1.04f;
    [SerializeField] private float speedSmooth = 8f;


    //==============================================================
    // PHOTON ANIMATION
    //==============================================================

    [Header("Photon Animation")]
    [SerializeField] private float photonShowScale = 1f;
    [SerializeField] private float photonHiddenScale = 0.88f;
    [SerializeField] private float photonFadeSpeed = 10f;


    //==============================================================
    // PHOTON TIMER
    //==============================================================

    [Header("Photon Timer")]
    [SerializeField] private float photonMaxDuration = 6f;
    [SerializeField] private float photonWarningTime = 2f;
    [SerializeField] private float photonWarningSpeed = 8f;


    //==============================================================
    // RUNTIME REFERENCES
    //==============================================================

    private GameManager gameManager;
    private PlayerController playerController;


    //==============================================================
    // BASE SCALES
    //==============================================================

    private Vector3 scoreBaseScale;
    private Vector3 coinBaseScale;
    private Vector3 coinIconBaseScale;
    private Vector3 speedBaseScale;

    private Vector3 photonBaseScale;

    // IMPORTANT:
    // Scale gốc của thanh Photon Fill.
    private Vector3 photonFillBaseScale;

    private Vector3 pauseBaseScale;


    //==============================================================
    // DATA CACHE
    //==============================================================

    private int lastScore = int.MinValue;
    private int lastCoins = int.MinValue;

    private float currentSpeedScale = 1f;


    //==============================================================
    // COROUTINES
    //==============================================================

    private Coroutine scorePopRoutine;
    private Coroutine coinPopRoutine;
    private Coroutine coinIconPopRoutine;
    private Coroutine pausePopRoutine;


    //==============================================================
    // PHOTON STATE
    //==============================================================

    private bool photonWasActive;


    //==============================================================
    // AWAKE
    //==============================================================

    private void Awake()
    {
        CacheBaseScales();

        SetupPhoton();

        SetupPause();
    }


    //==============================================================
    // START
    //==============================================================

    private void Start()
    {
        FindGameplaySystems();

        SyncGameplayData(true);

        UpdateDistanceUI();

        UpdateSpeedUI();

        UpdatePhotonUI();
    }


    //==============================================================
    // UPDATE
    //==============================================================

    private void Update()
    {
        FindGameplaySystemsIfNeeded();

        SyncGameplayData(false);

        UpdateDistanceUI();

        UpdateSpeedUI();

        UpdatePhotonUI();
    }


    //==============================================================
    // FIND GAMEPLAY SYSTEMS
    //==============================================================

    private void FindGameplaySystems()
    {
        if (gameManager == null)
        {
            gameManager = GameManager.Instance;

            if (gameManager == null)
            {
                gameManager =
                    FindFirstObjectByType<GameManager>();
            }
        }

        if (playerController == null)
        {
            playerController =
                FindFirstObjectByType<PlayerController>();
        }
    }


    private void FindGameplaySystemsIfNeeded()
    {
        if (gameManager == null ||
            playerController == null)
        {
            FindGameplaySystems();
        }
    }


    //==============================================================
    // CACHE BASE SCALES
    //==============================================================

    private void CacheBaseScales()
    {
        if (scoreValue != null)
        {
            scoreBaseScale =
                scoreValue.transform.localScale;
        }

        if (coinValue != null)
        {
            coinBaseScale =
                coinValue.transform.localScale;
        }

        if (coinIcon != null)
        {
            coinIconBaseScale =
                coinIcon.transform.localScale;
        }

        if (speedValue != null)
        {
            speedBaseScale =
                speedValue.transform.localScale;
        }

        if (photonRect != null)
        {
            photonBaseScale =
                photonRect.localScale;
        }

        if (photonFill != null)
        {
            photonFillBaseScale =
                photonFill.rectTransform.localScale;
        }

        if (pauseButton != null)
        {
            pauseBaseScale =
                pauseButton.transform.localScale;
        }
    }


    //==============================================================
    // SCORE + COIN
    //==============================================================

    private void SyncGameplayData(bool force)
    {
        if (gameManager == null)
            return;


        //==========================================================
        // SCORE
        //==========================================================

        int currentScore =
            gameManager.ScoreInt;


        if (force || currentScore != lastScore)
        {
            lastScore = currentScore;

            if (scoreValue != null)
            {
                scoreValue.text =
                    currentScore.ToString("N0");
            }

            if (!force)
            {
                PlayScorePop();
            }
        }


        //==========================================================
        // COIN
        //==========================================================

        int currentCoins =
            gameManager.CoinCount;


        if (force || currentCoins != lastCoins)
        {
            lastCoins = currentCoins;

            if (coinValue != null)
            {
                coinValue.text =
                    currentCoins.ToString();
            }

            if (!force)
            {
                PlayCoinPop();
            }
        }
    }


    //==============================================================
    // DISTANCE
    //==============================================================

    private void UpdateDistanceUI()
    {
        if (distanceValue == null)
            return;

        if (playerController == null)
            return;


        float distance =
            Mathf.Max(
                0f,
                playerController.transform.position.z
            );


        int displayDistance =
            Mathf.FloorToInt(distance);


        distanceValue.text =
            displayDistance.ToString("N0") + " M";
    }


    //==============================================================
    // SPEED
    //==============================================================

    private void UpdateSpeedUI()
    {
        if (speedValue == null)
            return;

        if (playerController == null)
            return;


        float speed =
            Mathf.Max(
                0f,
                playerController.CurrentSpeed
            );


        int displaySpeed =
            Mathf.RoundToInt(speed);


        speedValue.text =
            displaySpeed.ToString();


        float maxSpeed =
            Mathf.Max(
                1f,
                playerController.MaxSpeed
            );


        float speed01 =
            Mathf.Clamp01(
                speed / maxSpeed
            );


        float targetScale =
            Mathf.Lerp(
                speedMinScale,
                speedMaxScale,
                speed01
            );


        currentSpeedScale =
            Mathf.Lerp(
                currentSpeedScale,
                targetScale,
                speedSmooth *
                Time.unscaledDeltaTime
            );


        speedValue.transform.localScale =
            speedBaseScale *
            currentSpeedScale;
    }


    //==============================================================
    // SCORE POP
    //==============================================================

    public void PlayScorePop()
    {
        if (scoreValue == null)
            return;


        if (scorePopRoutine != null)
        {
            StopCoroutine(scorePopRoutine);
        }


        scorePopRoutine =
            StartCoroutine(
                ScalePop(
                    scoreValue.transform,
                    scoreBaseScale,
                    scorePopScale,
                    scorePopDuration
                )
            );
    }


    //==============================================================
    // COIN POP
    //==============================================================

    public void PlayCoinPop()
    {
        //==========================================================
        // COIN TEXT
        //==========================================================

        if (coinValue != null)
        {
            if (coinPopRoutine != null)
            {
                StopCoroutine(coinPopRoutine);
            }


            coinPopRoutine =
                StartCoroutine(
                    ScalePop(
                        coinValue.transform,
                        coinBaseScale,
                        coinPopScale,
                        coinPopDuration
                    )
                );
        }


        //==========================================================
        // COIN ICON
        //==========================================================

        if (coinIcon != null)
        {
            if (coinIconPopRoutine != null)
            {
                StopCoroutine(coinIconPopRoutine);
            }


            coinIconPopRoutine =
                StartCoroutine(
                    ScalePop(
                        coinIcon.transform,
                        coinIconBaseScale,
                        coinIconPopScale,
                        coinPopDuration
                    )
                );
        }
    }


    //==============================================================
    // GENERIC SCALE POP
    //==============================================================

    private IEnumerator ScalePop(
        Transform target,
        Vector3 baseScale,
        float popScale,
        float duration)
    {
        if (target == null)
            yield break;


        float halfDuration =
            duration * 0.5f;


        if (halfDuration <= 0f)
        {
            target.localScale =
                baseScale;

            yield break;
        }


        float timer = 0f;


        //==========================================================
        // SCALE UP
        //==========================================================

        while (timer < halfDuration)
        {
            timer +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    timer / halfDuration
                );


            t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );


            float scale =
                Mathf.Lerp(
                    1f,
                    popScale,
                    t
                );


            target.localScale =
                baseScale * scale;


            yield return null;
        }


        //==========================================================
        // SCALE DOWN
        //==========================================================

        timer = 0f;


        while (timer < halfDuration)
        {
            timer +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    timer / halfDuration
                );


            t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );


            float scale =
                Mathf.Lerp(
                    popScale,
                    1f,
                    t
                );


            target.localScale =
                baseScale * scale;


            yield return null;
        }


        target.localScale =
            baseScale;
    }


    //==============================================================
    // PHOTON SETUP
    //==============================================================

    private void SetupPhoton()
    {
        if (photonHUD == null)
            return;


        //==========================================================
        // CANVAS GROUP
        //==========================================================

        if (photonCanvasGroup == null)
        {
            photonCanvasGroup =
                photonHUD.GetComponent<CanvasGroup>();


            if (photonCanvasGroup == null)
            {
                photonCanvasGroup =
                    photonHUD.AddComponent<CanvasGroup>();
            }
        }


        //==========================================================
        // PHOTON RECT
        //==========================================================

        if (photonRect == null)
        {
            photonRect =
                photonHUD.GetComponent<RectTransform>();
        }


        if (photonRect != null)
        {
            photonBaseScale =
                photonRect.localScale;
        }


        //==========================================================
        // PHOTON FILL
        //==========================================================

        SetupPhotonFill();


        //==========================================================
        // INITIAL PHOTON STATE
        //==========================================================

        photonCanvasGroup.alpha = 0f;


        if (photonRect != null)
        {
            photonRect.localScale =
                photonBaseScale *
                photonHiddenScale;
        }


        if (photonFill != null)
        {
            RectTransform fillRect =
                photonFill.rectTransform;


            fillRect.localScale =
                photonFillBaseScale;
        }


        photonHUD.SetActive(false);

        photonWasActive = false;
    }


    //==============================================================
    // PHOTON FILL SETUP
    //==============================================================

    private void SetupPhotonFill()
    {
        if (photonFill == null)
        {
            Debug.LogWarning(
                "[GameplayHUDAnimator] Photon Fill chưa được gán.",
                this
            );

            return;
        }


        RectTransform fillRect =
            photonFill.rectTransform;


        //==========================================================
        // PIVOT
        //==========================================================
        //
        // Giữ cạnh trái cố định.
        // Khi scale X giảm, thanh co từ phải sang trái.
        //

        fillRect.pivot =
            new Vector2(
                0f,
                0.5f
            );


        //==========================================================
        // CACHE BASE SCALE
        //==========================================================

        photonFillBaseScale =
            fillRect.localScale;


        //==========================================================
        // RESET FULL
        //==========================================================

        fillRect.localScale =
            photonFillBaseScale;


        //==========================================================
        // UI SETTINGS
        //==========================================================

        photonFill.raycastTarget = false;
    }


    //==============================================================
    // PHOTON UI
    //==============================================================

    private void UpdatePhotonUI()
    {
        if (gameManager == null)
            return;

        if (photonHUD == null)
            return;


        bool active =
            gameManager.IsPhotonUIActive;


        float timeRemaining =
            Mathf.Max(
                0f,
                gameManager.PhotonTimeRemaining
            );


        //==========================================================
        // PHOTON JUST ACTIVATED
        //==========================================================

        if (active && !photonWasActive)
        {
            photonWasActive = true;


            if (!photonHUD.activeSelf)
            {
                photonHUD.SetActive(true);
            }


            if (photonCanvasGroup != null)
            {
                photonCanvasGroup.alpha = 0f;
            }


            if (photonRect != null)
            {
                photonRect.localScale =
                    photonBaseScale *
                    photonHiddenScale;
            }


            // RESET PHOTON FILL
            if (photonFill != null)
            {
                photonFill.rectTransform.localScale =
                    photonFillBaseScale;
            }
        }


        //==========================================================
        // PHOTON ACTIVE
        //==========================================================

        if (active)
        {
            if (!photonHUD.activeSelf)
            {
                photonHUD.SetActive(true);
            }


            //======================================================
            // FADE IN
            //======================================================

            if (photonCanvasGroup != null)
            {
                photonCanvasGroup.alpha =
                    Mathf.Lerp(
                        photonCanvasGroup.alpha,
                        1f,
                        photonFadeSpeed *
                        Time.unscaledDeltaTime
                    );
            }


            //======================================================
            // SCALE IN
            //======================================================

            if (photonRect != null)
            {
                photonRect.localScale =
                    Vector3.Lerp(
                        photonRect.localScale,
                        photonBaseScale *
                        photonShowScale,
                        photonFadeSpeed *
                        Time.unscaledDeltaTime
                    );
            }


            //======================================================
            // TIMER NORMALIZED
            //======================================================

            float normalized =
                Mathf.Clamp01(
                    timeRemaining /
                    Mathf.Max(
                        0.01f,
                        photonMaxDuration
                    )
                );


            //======================================================
            // PHOTON FILL
            //======================================================
            //
            // KHÔNG dùng Image.fillAmount.
            //
            // Scale X:
            //
            // 1.0 = 100%
            // 0.75 = 75%
            // 0.50 = 50%
            // 0.25 = 25%
            // 0.00 = hết
            //

            if (photonFill != null)
            {
                RectTransform fillRect =
                    photonFill.rectTransform;


                Vector3 targetScale =
                    photonFillBaseScale;


                targetScale.x *= normalized;


                fillRect.localScale =
                    targetScale;
            }


            //======================================================
            // TITLE
            //======================================================

            if (photonTitle != null)
            {
                photonTitle.text =
                    "PHOTON  " +
                    timeRemaining.ToString("0.0") +
                    "s";
            }


            //======================================================
            // WARNING
            //======================================================

            if (timeRemaining <= photonWarningTime)
            {
                AnimatePhotonWarning();
            }
        }


        //==========================================================
        // PHOTON ENDED
        //==========================================================

        else
        {
            if (photonWasActive)
            {
                photonWasActive = false;
            }


            if (!photonHUD.activeSelf)
                return;


            //======================================================
            // FADE OUT
            //======================================================

            if (photonCanvasGroup != null)
            {
                photonCanvasGroup.alpha =
                    Mathf.Lerp(
                        photonCanvasGroup.alpha,
                        0f,
                        photonFadeSpeed *
                        Time.unscaledDeltaTime
                    );


                if (photonCanvasGroup.alpha <= 0.02f)
                {
                    photonCanvasGroup.alpha = 0f;

                    photonHUD.SetActive(false);
                }
            }
            else
            {
                photonHUD.SetActive(false);
            }
        }
    }


    //==============================================================
    // PHOTON WARNING
    //==============================================================

    private void AnimatePhotonWarning()
    {
        if (photonFill == null)
            return;


        float pulse =
            Mathf.PingPong(
                Time.unscaledTime *
                photonWarningSpeed,
                1f
            );


        float scale =
            Mathf.Lerp(
                1f,
                1.03f,
                pulse
            );


        // Chỉ pulse nhẹ.
        // Không phá scale X của thanh timer.

        Vector3 currentScale =
            photonFill.rectTransform.localScale;


        currentScale.y =
            photonFillBaseScale.y *
            scale;


        photonFill.rectTransform.localScale =
            currentScale;
    }


    //==============================================================
    // PUBLIC PHOTON API
    //==============================================================

    public void ShowPhoton()
    {
        if (photonHUD == null)
            return;


        photonHUD.SetActive(true);


        if (photonCanvasGroup != null)
        {
            photonCanvasGroup.alpha = 1f;
        }


        if (photonRect != null)
        {
            photonRect.localScale =
                photonBaseScale;
        }


        if (photonFill != null)
        {
            photonFill.rectTransform.localScale =
                photonFillBaseScale;
        }


        photonWasActive = true;
    }


    public void HidePhoton()
    {
        if (photonHUD == null)
            return;


        if (photonCanvasGroup != null)
        {
            photonCanvasGroup.alpha = 0f;
        }


        photonHUD.SetActive(false);

        photonWasActive = false;


        if (photonFill != null)
        {
            photonFill.rectTransform.localScale =
                photonFillBaseScale;
        }
    }


    public void SetPhotonVisible(bool visible)
    {
        if (visible)
        {
            ShowPhoton();
        }
        else
        {
            HidePhoton();
        }
    }


    //==============================================================
    // PAUSE
    //==============================================================

    private void SetupPause()
    {
        if (pauseButton == null)
            return;


        pauseBaseScale =
            pauseButton.transform.localScale;


        pauseButton.onClick.RemoveListener(
            HandlePausePressed
        );


        pauseButton.onClick.AddListener(
            HandlePausePressed
        );
    }


    private void HandlePausePressed()
    {
        PlayPausePop();
    }


    public void PlayPausePop()
    {
        if (pauseButton == null)
            return;


        if (pausePopRoutine != null)
        {
            StopCoroutine(pausePopRoutine);
        }


        pausePopRoutine =
            StartCoroutine(
                PausePopRoutine()
            );
    }


    private IEnumerator PausePopRoutine()
    {
        Transform target =
            pauseButton.transform;


        float half =
            pausePopDuration;


        if (half <= 0f)
        {
            target.localScale =
                pauseBaseScale;

            yield break;
        }


        float timer = 0f;


        //==========================================================
        // DOWN
        //==========================================================

        while (timer < half)
        {
            timer +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    timer / half
                );


            t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );


            target.localScale =
                Vector3.Lerp(
                    pauseBaseScale,
                    pauseBaseScale *
                    pausePressedScale,
                    t
                );


            yield return null;
        }


        //==========================================================
        // UP
        //==========================================================

        timer = 0f;


        while (timer < half)
        {
            timer +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    timer / half
                );


            t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );


            target.localScale =
                Vector3.Lerp(
                    pauseBaseScale *
                    pausePressedScale,
                    pauseBaseScale,
                    t
                );


            yield return null;
        }


        target.localScale =
            pauseBaseScale;
    }
}