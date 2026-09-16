using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    // ============================================================
    // SCENES
    // ============================================================

    [Header("Scenes")]
    [SerializeField] private string loadingSceneName = "Loading";

    [SerializeField] private string gameplaySceneName = "Gameplay";

    [SerializeField] private string loginSceneName = "Login";


    // ============================================================
    // MISSION UI
    // ============================================================

    [Header("Mission UI")]
    [SerializeField] private GameObject missionPanel;


    // ============================================================
    // BUTTONS
    // ============================================================

    [Header("Buttons")]
    [SerializeField] private Button playButton;


    // ============================================================
    // INTERNAL
    // ============================================================

    private bool playButtonBound;
    private bool isStartingGame;
    private bool isLoggingOut;


    // ============================================================
    // AWAKE
    // ============================================================

    private void Awake()
    {
        Debug.Log(
            "[MainMenuController] AWAKE | " +
            $"Object={gameObject.name}",
            this
        );

        Time.timeScale = 1f;

        isStartingGame = false;
        isLoggingOut = false;

        if (missionPanel != null)
            missionPanel.SetActive(false);

        BindPlayButton();
    }


    // ============================================================
    // ON ENABLE
    // ============================================================

    private void OnEnable()
    {
        Time.timeScale = 1f;

        isStartingGame = false;
        isLoggingOut = false;
    }


    // ============================================================
    // START
    // ============================================================

    private void Start()
    {
        Time.timeScale = 1f;

        isStartingGame = false;
        isLoggingOut = false;

        BindPlayButton();

        Debug.Log(
            "[MainMenuController] START | " +
            $"Object={gameObject.name} | " +
            $"PlayButton={(playButton != null ? playButton.gameObject.name : "NULL")} | " +
            $"Bound={playButtonBound} | " +
            $"StartingGame={isStartingGame}",
            this
        );
    }


    // ============================================================
    // BIND PLAY BUTTON
    // ============================================================

    private void BindPlayButton()
    {
        Debug.Log(
            "[MainMenuController] Bắt đầu BindPlayButton().",
            this
        );

        if (playButton == null)
            playButton = FindPlayButton();

        if (playButton == null)
        {
            Debug.LogError(
                "[MainMenuController] KHÔNG TÌM THẤY PLAY BUTTON.",
                this
            );

            playButtonBound = false;

            return;
        }

        Debug.Log(
            "[MainMenuController] Tìm thấy PlayButton: " +
            playButton.gameObject.name,
            playButton
        );

        /*
         * CHỈ DÙNG MỘT ĐƯỜNG CLICK:
         *
         * Button.onClick -> Play()
         *
         * Không dùng Relay.
         * Không dùng IPointerClickHandler.
         * Không tự tìm MainMenuController khác.
         *
         * RemoveListener chỉ xóa runtime listener Play().
         * Persistent OnClick trong Inspector vẫn tồn tại.
         */

        playButton.onClick.RemoveListener(Play);
        playButton.onClick.AddListener(Play);

        playButtonBound = true;

        Debug.Log(
            "[MainMenuController] ĐÃ BIND PLAY BUTTON -> Play()",
            playButton
        );
    }


    // ============================================================
    // FIND PLAY BUTTON
    // ============================================================

    private Button FindPlayButton()
    {
        Button[] buttons =
            FindObjectsByType<Button>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (Button button in buttons)
        {
            if (button == null)
                continue;

            if (button.gameObject.name == "PlayButton")
                return button;
        }

        foreach (Button button in buttons)
        {
            if (button == null)
                continue;

            if (
                button.gameObject.name
                    .ToLowerInvariant()
                    .Contains("play")
            )
            {
                return button;
            }
        }

        return null;
    }


    // ============================================================
    // PLAY
    // ============================================================

    public void Play()
    {
        Debug.Log(
            "[MainMenuController] PLAY() ĐƯỢC GỌI.",
            this
        );

        if (isStartingGame)
        {
            Debug.Log(
                "[MainMenuController] Play() bị bỏ qua: " +
                "Gameplay đang được khởi động.",
                this
            );

            return;
        }

        if (isLoggingOut)
        {
            Debug.Log(
                "[MainMenuController] Play() bị bỏ qua: " +
                "Đang đăng xuất.",
                this
            );

            return;
        }

        isStartingGame = true;


        // --------------------------------------------------------
        // CHECK GAMEPLAY SCENE
        // --------------------------------------------------------

        if (string.IsNullOrWhiteSpace(
                gameplaySceneName))
        {
            isStartingGame = false;

            Debug.LogError(
                "[MainMenuController] " +
                "Gameplay Scene Name đang trống.",
                this
            );

            return;
        }


        // --------------------------------------------------------
        // CHECK LOADING SCENE
        // --------------------------------------------------------

        if (string.IsNullOrWhiteSpace(
                loadingSceneName))
        {
            isStartingGame = false;

            Debug.LogError(
                "[MainMenuController] " +
                "Loading Scene Name đang trống.",
                this
            );

            return;
        }


        // --------------------------------------------------------
        // CHECK LOADING SCENE
        // --------------------------------------------------------

        bool canLoad =
            Application.CanStreamedLevelBeLoaded(
                loadingSceneName
            );

        Debug.Log(
            "[MainMenuController] " +
            $"CanLoad Loading Scene = {canLoad} | " +
            $"Loading='{loadingSceneName}' | " +
            $"Gameplay='{gameplaySceneName}'",
            this
        );


        if (!canLoad)
        {
            isStartingGame = false;

            Debug.LogError(
                $"Không thể load Scene '{loadingSceneName}'. " +
                "Kiểm tra Build Profiles.",
                this
            );

            return;
        }


        // --------------------------------------------------------
        // SAVE LOADING TARGET
        // --------------------------------------------------------

        PlayerPrefs.SetString(
            "LoadingTargetScene",
            gameplaySceneName
        );

        PlayerPrefs.Save();


        Debug.Log(
            "[MainMenuController] LoadingTargetScene = '" +
            gameplaySceneName +
            "'",
            this
        );


        // --------------------------------------------------------
        // LOAD LOADING SCENE
        // --------------------------------------------------------

        Time.timeScale = 1f;

        Debug.Log(
            "[MainMenuController] SCENE LOAD STARTED -> " +
            loadingSceneName,
            this
        );

        SceneManager.LoadScene(
            loadingSceneName
        );
    }


    // ============================================================
    // MISSION
    // ============================================================

    public void OpenMission()
    {
        if (missionPanel == null)
        {
            Debug.LogError(
                "[MainMenuController] Chưa gán MissionPanel.",
                this
            );

            return;
        }

        missionPanel.SetActive(true);
    }


    public void CloseMission()
    {
        if (missionPanel == null)
        {
            Debug.LogError(
                "[MainMenuController] Chưa gán MissionPanel.",
                this
            );

            return;
        }

        missionPanel.SetActive(false);
    }


    public void ToggleMission()
    {
        if (missionPanel == null)
        {
            Debug.LogError(
                "[MainMenuController] Chưa gán MissionPanel.",
                this
            );

            return;
        }

        missionPanel.SetActive(
            !missionPanel.activeSelf
        );
    }


    // ============================================================
    // CHARACTER PROFILE
    // ============================================================

    public void OpenCharacterProfile()
    {
        if (CharacterProfileUI.Instance == null)
        {
            Debug.LogError(
                "[MainMenuController] " +
                "Không tìm thấy CharacterProfileUI. " +
                "Kiểm tra CharacterProfileCanvas đã có component " +
                "CharacterProfileUI chưa.",
                this
            );

            return;
        }

        CharacterProfileUI.Instance.Open();
    }


    public void CloseCharacterProfile()
    {
        if (CharacterProfileUI.Instance == null)
        {
            Debug.LogError(
                "[MainMenuController] " +
                "Không tìm thấy CharacterProfileUI.",
                this
            );

            return;
        }

        CharacterProfileUI.Instance.Close();
    }


    public void ToggleCharacterProfile()
    {
        if (CharacterProfileUI.Instance == null)
        {
            Debug.LogError(
                "[MainMenuController] " +
                "Không tìm thấy CharacterProfileUI.",
                this
            );

            return;
        }

        CharacterProfileUI.Instance.Toggle();
    }


    // ============================================================
    // SETTINGS
    // ============================================================

    public void Settings()
    {
        Debug.Log(
            "Settings chưa được triển khai."
        );
    }


    // ============================================================
    // HOW TO PLAY
    // ============================================================

    public void HowToPlay()
    {
        Debug.Log(
            "How To Play chưa được triển khai."
        );
    }


    // ============================================================
    // LOGOUT
    // ============================================================

    public void Quit()
    {
        if (isLoggingOut)
        {
            Debug.Log(
                "[MainMenuController] " +
                "Logout đã được thực hiện."
            );

            return;
        }

        if (isStartingGame)
        {
            Debug.Log(
                "[MainMenuController] " +
                "Không thể Logout: đang khởi động Gameplay.",
                this
            );

            return;
        }

        isLoggingOut = true;

        Time.timeScale = 1f;


        Debug.Log(
            "[MainMenuController] ĐANG ĐĂNG XUẤT...",
            this
        );


        // --------------------------------------------------------
        // FIREBASE SIGN OUT
        // --------------------------------------------------------

        if (
            FirebaseAuthManager.Instance != null &&
            FirebaseAuthManager.Instance.Auth != null
        )
        {
            FirebaseAuthManager.Instance.Auth.SignOut();

            Debug.Log(
                "[MainMenuController] " +
                "Firebase SignOut thành công.",
                this
            );
        }
        else
        {
            Debug.LogWarning(
                "[MainMenuController] " +
                "Không tìm thấy FirebaseAuthManager.",
                this
            );
        }


        // --------------------------------------------------------
        // CLEAR CURRENT PLAYER DATA
        // --------------------------------------------------------

        if (
            FirestorePlayerDataManager.Instance != null
        )
        {
            FirestorePlayerDataManager.Instance
                .ClearCurrentPlayerData();

            Debug.Log(
                "[MainMenuController] " +
                "CurrentPlayerData đã được clear.",
                this
            );
        }


        // --------------------------------------------------------
        // CHECK LOGIN SCENE
        // --------------------------------------------------------

        if (string.IsNullOrWhiteSpace(
                loginSceneName))
        {
            isLoggingOut = false;

            Debug.LogError(
                "[MainMenuController] " +
                "Login Scene Name đang trống.",
                this
            );

            return;
        }


        bool canLoad =
            Application.CanStreamedLevelBeLoaded(
                loginSceneName
            );


        Debug.Log(
            "[MainMenuController] " +
            $"CanLoad Login Scene = {canLoad} | " +
            $"Login='{loginSceneName}'",
            this
        );


        if (!canLoad)
        {
            isLoggingOut = false;

            Debug.LogError(
                $"[MainMenuController] " +
                $"Không thể load Scene '{loginSceneName}'. " +
                "Kiểm tra Build Profiles.",
                this
            );

            return;
        }


        // --------------------------------------------------------
        // LOAD LOGIN
        // --------------------------------------------------------

        Debug.Log(
            "[MainMenuController] " +
            "LOGOUT SUCCESS → LOGIN",
            this
        );

        SceneManager.LoadScene(
            loginSceneName
        );
    }
}