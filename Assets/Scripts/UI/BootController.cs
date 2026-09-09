using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BootController : MonoBehaviour
{
    //=============================================================
    // SCENE
    //=============================================================

    [Header("Scene")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";


    //=============================================================
    // INTRO
    //=============================================================

    [Header("Intro")]
    [SerializeField] private CanvasGroup introCanvasGroup;
    [SerializeField] private RectTransform logoTransform;

    [SerializeField] private float fadeInDuration = 1.0f;
    [SerializeField] private float holdDuration = 2.5f;
    [SerializeField] private float fadeOutDuration = 1.0f;

    [SerializeField] private float logoStartScale = 0.82f;
    [SerializeField] private float logoEndScale = 1.0f;


    //=============================================================
    // SKIP
    //=============================================================

    [Header("Skip")]
    [SerializeField] private bool allowSkip = true;
    [SerializeField] private GameObject skipButton;


    //=============================================================
    // INTERNAL
    //=============================================================

    private bool isLoadingMainMenu;
    private Coroutine introCoroutine;


    //=============================================================
    // AWAKE
    //=============================================================

    private void Awake()
    {
        Time.timeScale = 1f;

        if (introCanvasGroup == null)
        {
            Debug.LogError(
                "BootController: Intro CanvasGroup chưa được gán.",
                this
            );

            return;
        }

        if (logoTransform == null)
        {
            Debug.LogError(
                "BootController: Logo Transform chưa được gán.",
                this
            );

            return;
        }

        // Trạng thái ban đầu
        introCanvasGroup.alpha = 0f;

        logoTransform.localScale =
            Vector3.one * logoStartScale;

        // Hiện Skip
        if (skipButton != null)
        {
            skipButton.SetActive(allowSkip);
        }
    }


    //=============================================================
    // START
    //=============================================================

    private void Start()
    {
        if (introCanvasGroup == null ||
            logoTransform == null)
        {
            return;
        }

        introCoroutine =
            StartCoroutine(PlayIntro());
    }


    //=============================================================
    // UPDATE
    //=============================================================

    private void Update()
    {
        if (!allowSkip)
            return;

        if (isLoadingMainMenu)
            return;

        // Keyboard
        if (Input.anyKeyDown)
        {
            SkipIntro();
            return;
        }

        // Mouse
        if (Input.GetMouseButtonDown(0))
        {
            SkipIntro();
        }
    }


    //=============================================================
    // INTRO
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
        // 2. GIỮ LOGO
        //=========================================================

        yield return new WaitForSecondsRealtime(
            holdDuration
        );


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
        // 4. MAIN MENU
        //=========================================================

        LoadMainMenu();
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
        float elapsed = 0f;

        introCanvasGroup.alpha = startAlpha;

        logoTransform.localScale =
            Vector3.one * startScale;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / duration
                );

            // Smooth animation
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

        introCanvasGroup.alpha = endAlpha;

        logoTransform.localScale =
            Vector3.one * endScale;
    }


    //=============================================================
    // SKIP
    //=============================================================

    public void SkipIntro()
    {
        if (isLoadingMainMenu)
            return;

        // Dừng Intro
        if (introCoroutine != null)
        {
            StopCoroutine(introCoroutine);
            introCoroutine = null;
        }

        // Vào MainMenu ngay
        LoadMainMenu();
    }


    //=============================================================
    // LOAD MAIN MENU
    //=============================================================

    private void LoadMainMenu()
    {
        if (isLoadingMainMenu)
            return;

        isLoadingMainMenu = true;

        Time.timeScale = 1f;

        if (!Application.CanStreamedLevelBeLoaded(
            mainMenuSceneName))
        {
            Debug.LogError(
                $"Không thể load Scene '{mainMenuSceneName}'. " +
                "Kiểm tra Build Profiles.",
                this
            );

            isLoadingMainMenu = false;

            return;
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }
}