using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MainMenuController : MonoBehaviour
{
    // ============================================================
    // SCENES
    // ============================================================

    [Header("Scenes")]
    [SerializeField] private string loadingSceneName = "Loading";
    [SerializeField] private string gameplaySceneName = "Gameplay";
    [SerializeField] private string loginSceneName = "Login";

    // ============================================================
    // MAIN MENU
    // ============================================================

    [Header("Main Menu")]
    [SerializeField] private GameObject mainMenuScreen;

    // ============================================================
    // MAIN MENU PANELS / OVERLAYS
    // ============================================================

    [Header("Main Menu Panels / Overlays")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private GameObject wardrobePanel;
    [SerializeField] private GameObject garagePanel;
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private GameObject missionPanel;

    // ============================================================
    // WARDROBE
    // ============================================================

    [Header("Wardrobe")]
    [SerializeField] private InventoryUI inventoryUI;

    // ============================================================
    // GARAGE
    // ============================================================

    [Header("Garage")]
    [SerializeField] private MainMenuGarageUI garageUI;

    // ============================================================
    // BUTTONS
    // ============================================================

    [Header("Buttons")]
    [SerializeField] private Button playButton;

    // ============================================================
    // STATE
    // ============================================================

    private bool playButtonBound;
    private bool isStartingGame;
    private bool isLoggingOut;
    private bool isExitingGame;

    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        Debug.Log(
            "[MainMenuController] AWAKE | Object=" +
            gameObject.name,
            this
        );

        Time.timeScale = 1f;

        isStartingGame = false;
        isLoggingOut = false;
        isExitingGame = false;

        if (mainMenuScreen != null)
            mainMenuScreen.SetActive(true);

        CloseAllPanels();

        ResolveInventoryUI();
        ResolveGarageUI();

        BindPlayButton();
    }

    private void OnEnable()
    {
        Time.timeScale = 1f;

        isStartingGame = false;
        isLoggingOut = false;
        isExitingGame = false;
    }

    private void Start()
    {
        Time.timeScale = 1f;

        isStartingGame = false;
        isLoggingOut = false;
        isExitingGame = false;

        if (mainMenuScreen != null)
            mainMenuScreen.SetActive(true);

        CloseAllPanels();

        ResolveInventoryUI();
        ResolveGarageUI();

        BindPlayButton();

        Debug.Log(
            "[MainMenuController] START | " +
            $"Object={gameObject.name} | " +
            $"PlayButton={(playButton != null ? playButton.gameObject.name : "NULL")} | " +
            $"Bound={playButtonBound} | " +
            $"StartingGame={isStartingGame} | " +
            $"InventoryUI={(inventoryUI != null ? inventoryUI.gameObject.name : "NULL")} | " +
            $"GarageUI={(garageUI != null ? garageUI.gameObject.name : "NULL")}",
            this
        );
    }

    // ============================================================
    // INVENTORY / WARDROBE RESOLVE
    // ============================================================

    private void ResolveInventoryUI()
    {
        if (inventoryUI != null)
            return;

        if (wardrobePanel != null)
        {
            inventoryUI =
                wardrobePanel.GetComponentInChildren<InventoryUI>(true);

            if (inventoryUI != null)
            {
                Debug.Log(
                    "[MainMenuController] ĐÃ TÌM THẤY InventoryUI trong WardrobePanel -> " +
                    inventoryUI.gameObject.name,
                    inventoryUI
                );

                return;
            }
        }

        InventoryUI[] inventoryUIs =
            FindObjectsByType<InventoryUI>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        if (inventoryUIs == null ||
            inventoryUIs.Length == 0)
        {
            Debug.LogWarning(
                "[MainMenuController] KHÔNG TÌM THẤY InventoryUI trong Scene.",
                this
            );

            return;
        }

        inventoryUI = inventoryUIs[0];

        Debug.Log(
            "[MainMenuController] FALLBACK -> Tìm thấy InventoryUI: " +
            inventoryUI.gameObject.name,
            inventoryUI
        );
    }

    // ============================================================
    // GARAGE RESOLVE
    // ============================================================

    private void ResolveGarageUI()
    {
        if (garageUI != null)
            return;

        if (garagePanel != null)
        {
            garageUI =
                garagePanel.GetComponentInChildren<MainMenuGarageUI>(true);

            if (garageUI != null)
            {
                Debug.Log(
                    "[MainMenuController] ĐÃ TÌM THẤY MainMenuGarageUI trong GaragePanel -> " +
                    garageUI.gameObject.name,
                    garageUI
                );

                return;
            }
        }

        MainMenuGarageUI[] garageUIs =
            FindObjectsByType<MainMenuGarageUI>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        if (garageUIs == null ||
            garageUIs.Length == 0)
        {
            Debug.LogWarning(
                "[MainMenuController] KHÔNG TÌM THẤY MainMenuGarageUI trong Scene.",
                this
            );

            return;
        }

        garageUI = garageUIs[0];

        Debug.Log(
            "[MainMenuController] FALLBACK -> Tìm thấy MainMenuGarageUI: " +
            garageUI.gameObject.name,
            garageUI
        );
    }

    // ============================================================
    // PLAY BUTTON
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

        playButton.onClick.RemoveListener(Play);
        playButton.onClick.AddListener(Play);

        playButtonBound = true;

        Debug.Log(
            "[MainMenuController] ĐÃ BIND PLAY BUTTON -> Play()",
            playButton
        );
    }

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

            string buttonName =
                button.gameObject.name.ToLowerInvariant();

            if (buttonName.Contains("play"))
                return button;
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
                "[MainMenuController] Play() bị bỏ qua: Gameplay đang được khởi động.",
                this
            );

            return;
        }

        if (isLoggingOut)
        {
            Debug.Log(
                "[MainMenuController] Play() bị bỏ qua: Đang đăng xuất.",
                this
            );

            return;
        }

        if (isExitingGame)
        {
            Debug.Log(
                "[MainMenuController] Play() bị bỏ qua: Game đang thoát.",
                this
            );

            return;
        }

        isStartingGame = true;

        if (string.IsNullOrWhiteSpace(gameplaySceneName))
        {
            isStartingGame = false;

            Debug.LogError(
                "[MainMenuController] Gameplay Scene Name đang trống.",
                this
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(loadingSceneName))
        {
            isStartingGame = false;

            Debug.LogError(
                "[MainMenuController] Loading Scene Name đang trống.",
                this
            );

            return;
        }

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
                $"Không thể load Scene '{loadingSceneName}'. Kiểm tra Build Profiles.",
                this
            );

            return;
        }

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

        Time.timeScale = 1f;

        Debug.Log(
            "[MainMenuController] SCENE LOAD STARTED -> " +
            loadingSceneName,
            this
        );

        SceneManager.LoadScene(loadingSceneName);
    }

    // ============================================================
    // MAIN MENU NAVIGATION
    // ============================================================

    public void OpenMainMenu()
    {
        CloseAllPanels();

        if (mainMenuScreen != null)
            mainMenuScreen.SetActive(true);

        Debug.Log(
            "[MainMenuController] OPEN MAIN MENU / CLOSE ALL OVERLAYS",
            this
        );
    }

    // ============================================================
    // SHOP
    // ============================================================

    public void OpenShop()
    {
        OpenPanel(shopPanel, "Shop");
    }

    public void CloseShop()
    {
        ClosePanel(shopPanel, "Shop");
    }

    // ============================================================
    // WARDROBE
    // ============================================================

    public void OpenWardrobe()
    {
        ResolveInventoryUI();

        if (!OpenPanel(wardrobePanel, "Wardrobe"))
            return;

        if (inventoryUI == null)
        {
            Debug.LogError(
                "[MainMenuController] KHÔNG CÓ InventoryUI -> " +
                "Không thể refresh Tủ đồ.",
                this
            );

            return;
        }

        Debug.Log(
            "[MainMenuController] WARDROBE -> GỌI InventoryUI.Open()",
            inventoryUI
        );

        inventoryUI.Open();
    }

    public void CloseWardrobe()
    {
        ClosePanel(wardrobePanel, "Wardrobe");
    }

    // ============================================================
    // GARAGE
    // ============================================================

    public void OpenGarage()
    {
        Debug.Log(
            "[MainMenuController] OpenGarage() START.",
            this
        );

        ResolveGarageUI();

        if (!OpenPanel(garagePanel, "Garage"))
            return;

        if (garageUI == null)
        {
            Debug.LogError(
                "[MainMenuController] GARAGE PANEL đã mở nhưng " +
                "không tìm thấy MainMenuGarageUI.",
                this
            );

            return;
        }

        Debug.Log(
            "[MainMenuController] GARAGE -> GỌI MainMenuGarageUI.Open()",
            garageUI
        );

        garageUI.Open();

        Debug.Log(
            "[MainMenuController] OpenGarage() COMPLETE.",
            this
        );
    }

    public void CloseGarage()
    {
        ClosePanel(garagePanel, "Garage");
    }

    // ============================================================
    // UPGRADE
    // ============================================================

    public void OpenUpgrade()
    {
        OpenPanel(upgradePanel, "Upgrade");
    }

    public void CloseUpgrade()
    {
        ClosePanel(upgradePanel, "Upgrade");
    }

    // ============================================================
    // MISSION
    // ============================================================

    public void OpenMission()
    {
        OpenPanel(missionPanel, "Mission");
    }

    public void CloseMission()
    {
        ClosePanel(missionPanel, "Mission");
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

        if (missionPanel.activeSelf)
            CloseMission();
        else
            OpenMission();
    }

    // ============================================================
    // PANEL / OVERLAY CORE
    // ============================================================

    private bool OpenPanel(
        GameObject targetPanel,
        string panelName)
    {
        if (targetPanel == null)
        {
            Debug.LogError(
                $"[MainMenuController] {panelName}Panel chưa được gán.",
                this
            );

            return false;
        }

        if (mainMenuScreen != null &&
            !mainMenuScreen.activeSelf)
        {
            mainMenuScreen.SetActive(true);
        }

        CloseAllPanels();

        targetPanel.SetActive(true);

        Debug.Log(
            $"[MainMenuController] OPEN OVERLAY -> {panelName} | " +
            $"PanelActive={targetPanel.activeSelf} | " +
            $"MainMenuActive={(mainMenuScreen != null && mainMenuScreen.activeSelf)}",
            this
        );

        return true;
    }

    private void ClosePanel(
        GameObject panel,
        string panelName)
    {
        if (panel == null)
        {
            Debug.LogError(
                $"[MainMenuController] {panelName}Panel chưa được gán.",
                this
            );

            return;
        }

        panel.SetActive(false);

        if (mainMenuScreen != null &&
            !mainMenuScreen.activeSelf)
        {
            mainMenuScreen.SetActive(true);
        }

        Debug.Log(
            $"[MainMenuController] CLOSE OVERLAY -> {panelName} | " +
            "MainMenuScreen vẫn ACTIVE",
            this
        );
    }

    private void CloseAllPanels()
    {
        if (shopPanel != null)
            shopPanel.SetActive(false);

        if (wardrobePanel != null)
            wardrobePanel.SetActive(false);

        if (garagePanel != null)
            garagePanel.SetActive(false);

        if (upgradePanel != null)
            upgradePanel.SetActive(false);

        if (missionPanel != null)
            missionPanel.SetActive(false);
    }

    // ============================================================
    // CHARACTER PROFILE
    // ============================================================

    public void OpenCharacterProfile()
    {
        if (CharacterProfileUI.Instance == null)
        {
            Debug.LogError(
                "[MainMenuController] Không tìm thấy CharacterProfileUI. " +
                "Kiểm tra CharacterProfileCanvas đã có component CharacterProfileUI chưa.",
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
                "[MainMenuController] Không tìm thấy CharacterProfileUI.",
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
                "[MainMenuController] Không tìm thấy CharacterProfileUI.",
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
            "Settings được quản lý bởi hệ thống riêng.",
            this
        );
    }

    // ============================================================
    // HOW TO PLAY
    // ============================================================

    public void HowToPlay()
    {
        Debug.Log(
            "How To Play chưa được triển khai.",
            this
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
                "[MainMenuController] Logout đã được thực hiện."
            );

            return;
        }

        if (isStartingGame)
        {
            Debug.Log(
                "[MainMenuController] Không thể Logout: đang khởi động Gameplay.",
                this
            );

            return;
        }

        if (isExitingGame)
        {
            Debug.Log(
                "[MainMenuController] Không thể Logout: game đang thoát.",
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

        if (FirebaseAuthManager.Instance != null &&
            FirebaseAuthManager.Instance.Auth != null)
        {
            FirebaseAuthManager.Instance.Auth.SignOut();

            Debug.Log(
                "[MainMenuController] Firebase SignOut thành công.",
                this
            );
        }
        else
        {
            Debug.LogWarning(
                "[MainMenuController] Không tìm thấy FirebaseAuthManager.",
                this
            );
        }

        if (FirestorePlayerDataManager.Instance != null)
        {
            FirestorePlayerDataManager.Instance.ClearCurrentPlayerData();

            Debug.Log(
                "[MainMenuController] CurrentPlayerData đã được clear.",
                this
            );
        }

        if (string.IsNullOrWhiteSpace(loginSceneName))
        {
            isLoggingOut = false;

            Debug.LogError(
                "[MainMenuController] Login Scene Name đang trống.",
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
                "[MainMenuController] " +
                $"Không thể load Scene '{loginSceneName}'. " +
                "Kiểm tra Build Profiles.",
                this
            );

            return;
        }

        Debug.Log(
            "[MainMenuController] LOGOUT SUCCESS → LOGIN",
            this
        );

        SceneManager.LoadScene(loginSceneName);
    }

    // ============================================================
    // EXIT GAME
    // ============================================================

    public void ExitGame()
    {
        if (isExitingGame)
        {
            Debug.Log(
                "[MainMenuController] ExitGame() bị bỏ qua: " +
                "game đang trong quá trình thoát.",
                this
            );

            return;
        }

        if (isStartingGame)
        {
            Debug.Log(
                "[MainMenuController] Không thể thoát game: " +
                "đang khởi động Gameplay.",
                this
            );

            return;
        }

        if (isLoggingOut)
        {
            Debug.Log(
                "[MainMenuController] Không thể thoát game: " +
                "đang đăng xuất.",
                this
            );

            return;
        }

        isExitingGame = true;

        Time.timeScale = 1f;

        Debug.Log(
            "[MainMenuController] EXIT GAME -> " +
            "Đang đóng ứng dụng.",
            this
        );

#if UNITY_EDITOR
        // --------------------------------------------------------
        // UNITY EDITOR
        // --------------------------------------------------------
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // --------------------------------------------------------
        // WINDOWS / ANDROID / BUILD
        // --------------------------------------------------------
        Application.Quit();
#endif
    }
}