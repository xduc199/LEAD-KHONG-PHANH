using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string loadingSceneName = "Loading";

    [Header("Loading Target")]
    [SerializeField] private string gameplaySceneName = "Gameplay";

    public void Play()
    {
        PlayerPrefs.SetString("LoadingTargetScene", gameplaySceneName);
        PlayerPrefs.Save();

        Time.timeScale = 1f;

        if (!Application.CanStreamedLevelBeLoaded(loadingSceneName))
        {
            Debug.LogError(
                $"Không thể load Scene '{loadingSceneName}'. " +
                "Kiểm tra Build Profiles.",
                this
            );

            return;
        }

        SceneManager.LoadScene(loadingSceneName);
    }

    public void Settings()
    {
        Debug.Log("Settings chưa được triển khai.");
    }

    public void HowToPlay()
    {
        Debug.Log("How To Play chưa được triển khai.");
    }

    public void Quit()
    {
        Debug.Log("Quit Game");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}