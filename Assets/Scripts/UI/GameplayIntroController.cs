using System.Collections;
using UnityEngine;
using TMPro;

public class GameplayIntroController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CanvasGroup introGroup;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private GameObject skipButton;

    [Header("Timing")]
    [SerializeField] private float fadeInDuration = 0.25f;
    [SerializeField] private float readyDuration = 0.8f;
    [SerializeField] private float countdownDuration = 0.7f;
    [SerializeField] private float goDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.25f;

    [Header("Options")]
    [SerializeField] private bool allowSkip = true;

    private bool introFinished;
    private Coroutine introCoroutine;

    private void Awake()
    {
        Time.timeScale = 0f;

        if (introGroup != null)
        {
            introGroup.alpha = 0f;
            introGroup.interactable = false;
            introGroup.blocksRaycasts = false;
        }

        if (skipButton != null)
        {
            skipButton.SetActive(allowSkip);
        }
    }

    private void Start()
    {
        introCoroutine = StartCoroutine(PlayIntro());
    }

    private void Update()
    {
        if (!allowSkip || introFinished)
            return;

        if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
        {
            SkipIntro();
        }
    }

    private IEnumerator PlayIntro()
{
    yield return Fade(0f, 1f, fadeInDuration);

    ShowText("GET READY");
    yield return new WaitForSecondsRealtime(readyDuration);

    ShowText("3");
    yield return new WaitForSecondsRealtime(countdownDuration);

    ShowText("2");
    yield return new WaitForSecondsRealtime(countdownDuration);

    ShowText("1");
    yield return new WaitForSecondsRealtime(countdownDuration);

    // Đảm bảo số 1 biến mất hoàn toàn
    ShowText(string.Empty);
    yield return null;

    ShowText("GO!");
    yield return new WaitForSecondsRealtime(goDuration);

    FinishIntro();
}
    private IEnumerator Fade(
        float from,
        float to,
        float duration)
    {
        if (introGroup == null)
            yield break;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);

            introGroup.alpha = Mathf.Lerp(from, to, t);

            yield return null;
        }

        introGroup.alpha = to;
    }

    private void ShowText(string text)
    {
        if (messageText != null)
        {
            messageText.text = text;
        }
    }

    public void SkipIntro()
    {
        if (introFinished)
            return;

        if (introCoroutine != null)
        {
            StopCoroutine(introCoroutine);
            introCoroutine = null;
        }

        FinishIntro();
    }

    private void FinishIntro()
    {
        if (introFinished)
            return;

        introFinished = true;

        Time.timeScale = 1f;

        if (introGroup != null)
        {
            introGroup.alpha = 0f;
            introGroup.interactable = false;
            introGroup.blocksRaycasts = false;
        }

        if (skipButton != null)
        {
            skipButton.SetActive(false);
        }
    }
}