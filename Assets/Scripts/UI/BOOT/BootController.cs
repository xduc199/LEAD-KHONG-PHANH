using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BootController : MonoBehaviour
{
    //=============================================================
    // SCENE
    //=============================================================

    [Header("Scene")]
    [SerializeField] private string loginSceneName = "Login";


    //=============================================================
    // INTRO
    //=============================================================

    [Header("Intro")]
    [SerializeField] private CanvasGroup introCanvasGroup;
    [SerializeField] private RectTransform logoTransform;

    [SerializeField]
    [Min(0f)]
    private float fadeInDuration = 1.0f;

    [SerializeField]
    [Min(0f)]
    private float holdDuration = 2.5f;

    [SerializeField]
    [Min(0f)]
    private float fadeOutDuration = 1.0f;

    [SerializeField]
    private float logoStartScale = 0.82f;

    [SerializeField]
    private float logoEndScale = 1.0f;


    //=============================================================
    // SKIP
    //=============================================================

    [Header("Skip")]
    [SerializeField] private bool allowSkip = true;
    [SerializeField] private GameObject skipButton;


    //=============================================================
    // INTERNAL STATE
    //=============================================================

    private Coroutine introCoroutine;
    private Coroutine loadSceneCoroutine;

    private AsyncOperation loginLoadOperation;

    private bool isBootValid;
    private bool isTransitioning;
    private bool isSceneLoading;


    //=============================================================
    // UNITY - AWAKE
    //=============================================================

    private void Awake()
    {
        // Boot luôn khởi động ở TimeScale bình thường.
        Time.timeScale = 1f;

        isBootValid = ValidateSetup();

        if (!isBootValid)
        {
            enabled = false;
            return;
        }

        InitializeIntroState();
    }


    //=============================================================
    // UNITY - START
    //=============================================================

    private void Start()
    {
        if (!isBootValid)
        {
            return;
        }

        introCoroutine = StartCoroutine(PlayIntro());
    }


    //=============================================================
    // UNITY - UPDATE
    //=============================================================

    private void Update()
    {
        if (!isBootValid)
        {
            return;
        }

        if (!allowSkip)
        {
            return;
        }

        if (isTransitioning)
        {
            return;
        }

        if (isSceneLoading)
        {
            return;
        }

        // Keyboard / controller / general input.
        if (Input.anyKeyDown)
        {
            SkipIntro();
            return;
        }

        // Mouse click.
        if (Input.GetMouseButtonDown(0))
        {
            SkipIntro();
        }
    }


    //=============================================================
    // VALIDATE
    //=============================================================

    private bool ValidateSetup()
    {
        bool valid = true;

        if (string.IsNullOrWhiteSpace(loginSceneName))
        {
            Debug.LogError(
                "[BootController] Login Scene Name chưa được cấu hình.",
                this
            );

            valid = false;
        }

        if (introCanvasGroup == null)
        {
            Debug.LogError(
                "[BootController] Intro CanvasGroup chưa được gán.",
                this
            );

            valid = false;
        }

        if (logoTransform == null)
        {
            Debug.LogError(
                "[BootController] Logo Transform chưa được gán.",
                this
            );

            valid = false;
        }

        if (!Application.CanStreamedLevelBeLoaded(loginSceneName))
        {
            Debug.LogError(
                "[BootController] Không thể load Scene '" +
                loginSceneName +
                "'. " +
                "Kiểm tra Build Profiles / Scene List.",
                this
            );

            valid = false;
        }

        return valid;
    }


    //=============================================================
    // INITIALIZE INTRO
    //=============================================================

    private void InitializeIntroState()
    {
        introCanvasGroup.alpha = 0f;

        logoTransform.localScale =
            Vector3.one * logoStartScale;

        if (skipButton != null)
        {
            skipButton.SetActive(allowSkip);
        }
    }


    //=============================================================
    // PLAY INTRO
    //=============================================================

    private IEnumerator PlayIntro()
    {
        //=========================================================
        // 1. FADE IN
        //=========================================================

        yield return FadeLogo(
            0f,
            1f,
            logoStartScale,
            logoEndScale,
            fadeInDuration
        );


        //=========================================================
        // 2. HOLD
        //=========================================================

        if (holdDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(
                holdDuration
            );
        }


        //=========================================================
        // 3. FADE OUT
        //=========================================================

        yield return FadeLogo(
            1f,
            0f,
            logoEndScale,
            logoEndScale,
            fadeOutDuration
        );


        //=========================================================
        // 4. LOAD LOGIN
        //=========================================================

        introCoroutine = null;

        LoadLogin();
    }


    //=============================================================
    // FADE LOGO
    //=============================================================

    private IEnumerator FadeLogo(
        float startAlpha,
        float endAlpha,
        float startScale,
        float endScale,
        float duration)
    {
        introCanvasGroup.alpha = startAlpha;

        logoTransform.localScale =
            Vector3.one * startScale;

        //=========================================================
        // ZERO DURATION
        //=========================================================

        if (duration <= 0f)
        {
            introCanvasGroup.alpha = endAlpha;

            logoTransform.localScale =
                Vector3.one * endScale;

            yield break;
        }


        //=========================================================
        // ANIMATION
        //=========================================================

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / duration
                );

            // SmoothStep:
            // 3t² - 2t³
            t = t * t * (3f - 2f * t);

            introCanvasGroup.alpha =
                Mathf.Lerp(
                    startAlpha,
                    endAlpha,
                    t
                );

            float scale =
                Mathf.Lerp(
                    startScale,
                    endScale,
                    t
                );

            logoTransform.localScale =
                Vector3.one * scale;

            yield return null;
        }


        //=========================================================
        // FORCE FINAL STATE
        //=========================================================

        introCanvasGroup.alpha = endAlpha;

        logoTransform.localScale =
            Vector3.one * endScale;
    }


    //=============================================================
    // SKIP INTRO
    //=============================================================

    public void SkipIntro()
    {
        if (!isBootValid)
        {
            return;
        }

        if (isTransitioning)
        {
            return;
        }

        if (isSceneLoading)
        {
            return;
        }

        LoadLogin();
    }


    //=============================================================
    // LOAD LOGIN
    //=============================================================

    private void LoadLogin()
    {
        if (!isBootValid)
        {
            return;
        }

        if (isTransitioning)
        {
            return;
        }

        if (isSceneLoading)
        {
            return;
        }

        isTransitioning = true;

        Time.timeScale = 1f;

        //=========================================================
        // STOP INTRO
        //=========================================================

        if (introCoroutine != null)
        {
            StopCoroutine(introCoroutine);
            introCoroutine = null;
        }

        //=========================================================
        // HIDE SKIP
        //=========================================================

        if (skipButton != null)
        {
            skipButton.SetActive(false);
        }

        //=========================================================
        // START ASYNC LOAD
        //=========================================================

        loadSceneCoroutine =
            StartCoroutine(
                LoadLoginSceneAsync()
            );
    }


    //=============================================================
    // ASYNC LOGIN LOAD
    //=============================================================

    private IEnumerator LoadLoginSceneAsync()
    {
        isSceneLoading = true;

        Debug.Log(
            "[BootController] Loading Login Scene: " +
            loginSceneName
        );

        loginLoadOperation =
            SceneManager.LoadSceneAsync(
                loginSceneName,
                LoadSceneMode.Single
            );

        if (loginLoadOperation == null)
        {
            Debug.LogError(
                "[BootController] Không thể tạo AsyncOperation " +
                "cho Scene '" +
                loginSceneName +
                "'.",
                this
            );

            isSceneLoading = false;
            isTransitioning = false;
            loadSceneCoroutine = null;

            if (skipButton != null &&
                allowSkip)
            {
                skipButton.SetActive(true);
            }

            yield break;
        }

        // Không cho phép activate sớm.
        // Giữ Scene Login chưa active cho đến khi
        // Unity hoàn thành quá trình load.
        loginLoadOperation.allowSceneActivation = true;

        while (!loginLoadOperation.isDone)
        {
            yield return null;
        }

        loginLoadOperation = null;

        isSceneLoading = false;

        Debug.Log(
            "[BootController] Login Scene loaded successfully."
        );

        loadSceneCoroutine = null;
    }


    //=============================================================
    // UNITY - ON DESTROY
    //=============================================================

    private void OnDestroy()
    {
        if (introCoroutine != null)
        {
            StopCoroutine(introCoroutine);
            introCoroutine = null;
        }

        if (loadSceneCoroutine != null)
        {
            StopCoroutine(loadSceneCoroutine);
            loadSceneCoroutine = null;
        }

        loginLoadOperation = null;
    }
}