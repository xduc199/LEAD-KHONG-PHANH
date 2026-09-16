using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LoadingScreenController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider progressBar;
    [SerializeField] private TMP_Text progressText;

    [Header("Scene")]
    [SerializeField] private string fallbackSceneName = "Gameplay";

    [Header("Display")]
    [SerializeField] private float loadingDisplayTime = 2f;

    private bool isLoading;

    private void Start()
    {
        Time.timeScale = 1f;

        SetupUI();

        StartLoading();
    }

    private void SetupUI()
    {
        if (progressBar != null)
        {
            progressBar.minValue = 0f;
            progressBar.maxValue = 1f;
            progressBar.value = 0f;
        }

        UpdateUI(0f);
    }

    public void StartLoading()
    {
        if (isLoading)
            return;

        isLoading = true;

        StartCoroutine(LoadSceneRoutine());
    }

    private IEnumerator LoadSceneRoutine()
    {
        string targetSceneName =
            PlayerPrefs.GetString(
                "LoadingTargetScene",
                fallbackSceneName
            );

        PlayerPrefs.DeleteKey("LoadingTargetScene");
        PlayerPrefs.Save();

        if (!Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            Debug.LogError(
                $"Không thể load Scene '{targetSceneName}'. " +
                "Kiểm tra Build Profiles.",
                this
            );

            isLoading = false;
            yield break;
        }

        AsyncOperation operation =
            SceneManager.LoadSceneAsync(targetSceneName);

        if (operation == null)
        {
            Debug.LogError(
                $"LoadSceneAsync thất bại: {targetSceneName}",
                this
            );

            isLoading = false;
            yield break;
        }

        operation.allowSceneActivation = false;

        float elapsed = 0f;

        while (elapsed < loadingDisplayTime)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / loadingDisplayTime
                );

            progress = Mathf.SmoothStep(
                0f,
                1f,
                progress
            );

            UpdateUI(progress);

            yield return null;
        }

        UpdateUI(1f);

        yield return new WaitForSecondsRealtime(0.1f);

        operation.allowSceneActivation = true;

        while (!operation.isDone)
        {
            yield return null;
        }

        isLoading = false;
    }

    private void UpdateUI(float progress)
    {
        progress = Mathf.Clamp01(progress);

        if (progressBar != null)
        {
            progressBar.value = progress;
        }

        if (progressText != null)
        {
            int percent =
                Mathf.RoundToInt(progress * 100f);

            progressText.text =
                $"LOADING... {percent}%";
        }
    }
}