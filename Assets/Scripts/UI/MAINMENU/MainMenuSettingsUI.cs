using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MainMenuSettingsUI : MonoBehaviour
{
    public static MainMenuSettingsUI Instance { get; private set; }

    // ============================================================
    // PLAYER PREFS KEYS
    // ============================================================

    private const string MASTER_AUDIO_ENABLED_KEY = "MasterAudioEnabled";
    private const string MUSIC_ENABLED_KEY = "MusicEnabled";
    private const string SFX_ENABLED_KEY = "SfxEnabled";

    // ============================================================
    // AUDIO MIXER PARAMETERS
    // ============================================================

    private const string MASTER_VOLUME_PARAMETER = "MasterVolume";
    private const string MUSIC_VOLUME_PARAMETER = "MusicVolume";
    private const string SFX_VOLUME_PARAMETER = "SFXVolume";

    private const float AUDIO_ON_VOLUME = 0f;
    private const float AUDIO_OFF_VOLUME = -80f;

    // ============================================================
    // AUDIO SOURCE
    // ============================================================

    [Header("Audio")]
    [Tooltip("AudioMixer dùng chung cho toàn bộ game.")]
    [SerializeField] private AudioMixer gameAudioMixer;

    [Tooltip("Music AudioSource của Main Menu.")]
    [SerializeField] private AudioSource musicAudioSource;

    // ============================================================
    // PANEL
    // ============================================================

    [Header("Panel")]
    [SerializeField] private GameObject settingsPanel;

    // ============================================================
    // AUDIO TOGGLES
    // ============================================================

    [Header("Audio Toggles")]
    [Tooltip("Bật/tắt toàn bộ âm thanh game.")]
    [SerializeField] private Toggle masterAudioToggle;

    [Tooltip("Bật/tắt riêng nhạc.")]
    [SerializeField] private Toggle musicToggle;

    [Tooltip("Bật/tắt riêng hiệu ứng âm thanh.")]
    [SerializeField] private Toggle sfxToggle;

    // ============================================================
    // ACCOUNT / GAME BUTTONS
    // ============================================================

    [Header("Account / Game")]
    [SerializeField] private Button logoutButton;

    [SerializeField] private Button exitGameButton;

    // ============================================================
    // MAIN MENU CONTROLLER
    // ============================================================

    [Header("Main Menu Controller")]
    [SerializeField] private MainMenuController mainMenuController;

    // ============================================================
    // STATE
    // ============================================================

    private bool masterAudioEnabled;
    private bool musicEnabled;
    private bool sfxEnabled;

    // ============================================================
    // PUBLIC STATE
    // ============================================================

    public bool IsMasterAudioEnabled => masterAudioEnabled;

    public bool IsMusicEnabled => musicEnabled;

    public bool IsSFXEnabled => sfxEnabled;

    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        ResolveMainMenuController();
        Initialize();
    }

    // ============================================================
    // INITIALIZE
    // ============================================================

    private void Initialize()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        // --------------------------------------------------------
        // LOAD SHARED AUDIO STATE
        // --------------------------------------------------------

        masterAudioEnabled =
            LoadMasterAudioState();

        musicEnabled =
            LoadMusicState();

        sfxEnabled =
            LoadSFXState();

        // --------------------------------------------------------
        // AUDIO SOURCE
        // --------------------------------------------------------

        if (musicAudioSource != null)
        {
            musicAudioSource.loop = true;
        }
        else
        {
            Debug.LogWarning(
                "[MainMenuSettings] " +
                "Music AudioSource chưa được gán.",
                this
            );
        }

        // --------------------------------------------------------
        // BIND UI
        // --------------------------------------------------------

        BindMasterAudioToggle();
        BindMusicToggle();
        BindSFXToggle();

        BindLogoutButton();
        BindExitGameButton();

        // --------------------------------------------------------
        // APPLY AUDIO
        // --------------------------------------------------------

        ApplyAudioState();
    }

    // ============================================================
    // LOAD MASTER AUDIO STATE
    // ============================================================

    private static bool LoadMasterAudioState()
    {
        return PlayerPrefs.GetInt(
            MASTER_AUDIO_ENABLED_KEY,
            1
        ) == 1;
    }

    // ============================================================
    // LOAD MUSIC STATE
    // ============================================================

    private static bool LoadMusicState()
    {
        return PlayerPrefs.GetInt(
            MUSIC_ENABLED_KEY,
            1
        ) == 1;
    }

    // ============================================================
    // LOAD SFX STATE
    // ============================================================

    private static bool LoadSFXState()
    {
        return PlayerPrefs.GetInt(
            SFX_ENABLED_KEY,
            1
        ) == 1;
    }

    // ============================================================
    // SAVE MASTER AUDIO STATE
    // ============================================================

    private static void SaveMasterAudioState(
        bool enabled
    )
    {
        PlayerPrefs.SetInt(
            MASTER_AUDIO_ENABLED_KEY,
            enabled ? 1 : 0
        );

        PlayerPrefs.Save();
    }

    // ============================================================
    // SAVE MUSIC STATE
    // ============================================================

    private static void SaveMusicState(
        bool enabled
    )
    {
        PlayerPrefs.SetInt(
            MUSIC_ENABLED_KEY,
            enabled ? 1 : 0
        );

        PlayerPrefs.Save();
    }

    // ============================================================
    // SAVE SFX STATE
    // ============================================================

    private static void SaveSFXState(
        bool enabled
    )
    {
        PlayerPrefs.SetInt(
            SFX_ENABLED_KEY,
            enabled ? 1 : 0
        );

        PlayerPrefs.Save();
    }

    // ============================================================
    // RESOLVE MAIN MENU CONTROLLER
    // ============================================================

    private void ResolveMainMenuController()
    {
        if (mainMenuController != null)
            return;

        mainMenuController =
            FindFirstObjectByType<MainMenuController>();

        if (mainMenuController == null)
        {
            Debug.LogError(
                "[MainMenuSettings] Không tìm thấy " +
                "MainMenuController trong Scene.",
                this
            );
        }
    }

    // ============================================================
    // MASTER AUDIO TOGGLE
    // ============================================================

    private void BindMasterAudioToggle()
    {
        if (masterAudioToggle == null)
            return;

        masterAudioToggle.onValueChanged.RemoveListener(
            OnMasterAudioToggleChanged
        );

        masterAudioToggle.SetIsOnWithoutNotify(
            masterAudioEnabled
        );

        masterAudioToggle.onValueChanged.AddListener(
            OnMasterAudioToggleChanged
        );
    }

    // ============================================================
    // MASTER AUDIO CHANGED
    // ============================================================

    private void OnMasterAudioToggleChanged(
        bool enabled
    )
    {
        masterAudioEnabled = enabled;

        SaveMasterAudioState(
            masterAudioEnabled
        );

        ApplyAudioState();

        Debug.Log(
            "[MainMenuSettings] Master Audio = " +
            (masterAudioEnabled ? "ON" : "OFF"),
            this
        );
    }

    // ============================================================
    // MUSIC TOGGLE
    // ============================================================

    private void BindMusicToggle()
    {
        if (musicToggle == null)
            return;

        musicToggle.onValueChanged.RemoveListener(
            OnMusicToggleChanged
        );

        musicToggle.SetIsOnWithoutNotify(
            musicEnabled
        );

        musicToggle.onValueChanged.AddListener(
            OnMusicToggleChanged
        );
    }

    // ============================================================
    // MUSIC CHANGED
    // ============================================================

    private void OnMusicToggleChanged(
        bool enabled
    )
    {
        musicEnabled = enabled;

        SaveMusicState(
            musicEnabled
        );

        ApplyAudioState();

        Debug.Log(
            "[MainMenuSettings] Music = " +
            (musicEnabled ? "ON" : "OFF"),
            this
        );
    }

    // ============================================================
    // SFX TOGGLE
    // ============================================================

    private void BindSFXToggle()
    {
        if (sfxToggle == null)
            return;

        sfxToggle.onValueChanged.RemoveListener(
            OnSFXToggleChanged
        );

        sfxToggle.SetIsOnWithoutNotify(
            sfxEnabled
        );

        sfxToggle.onValueChanged.AddListener(
            OnSFXToggleChanged
        );
    }

    // ============================================================
    // SFX CHANGED
    // ============================================================

    private void OnSFXToggleChanged(
        bool enabled
    )
    {
        sfxEnabled = enabled;

        SaveSFXState(
            sfxEnabled
        );

        ApplyAudioState();

        Debug.Log(
            "[MainMenuSettings] SFX = " +
            (sfxEnabled ? "ON" : "OFF"),
            this
        );
    }

    // ============================================================
    // APPLY AUDIO STATE
    // ============================================================

    private void ApplyAudioState()
    {
        // --------------------------------------------------------
        // AUDIO MIXER
        // --------------------------------------------------------

        if (gameAudioMixer != null)
        {
            ApplyMixerState();
        }
        else
        {
            // ----------------------------------------------------
            // FALLBACK
            // ----------------------------------------------------

            ApplyFallbackMusicState();
        }
    }

    // ============================================================
    // APPLY MIXER STATE
    // ============================================================

    private void ApplyMixerState()
    {
        // --------------------------------------------------------
        // MASTER
        // --------------------------------------------------------

        bool masterApplied =
            gameAudioMixer.SetFloat(
                MASTER_VOLUME_PARAMETER,
                masterAudioEnabled
                    ? AUDIO_ON_VOLUME
                    : AUDIO_OFF_VOLUME
            );

        if (!masterApplied)
        {
            Debug.LogWarning(
                "[MainMenuSettings] Không tìm thấy exposed " +
                "parameter: " +
                MASTER_VOLUME_PARAMETER,
                this
            );
        }

        // --------------------------------------------------------
        // MUSIC
        // --------------------------------------------------------

        bool musicApplied =
            gameAudioMixer.SetFloat(
                MUSIC_VOLUME_PARAMETER,
                musicEnabled
                    ? AUDIO_ON_VOLUME
                    : AUDIO_OFF_VOLUME
            );

        if (!musicApplied)
        {
            Debug.LogWarning(
                "[MainMenuSettings] Không tìm thấy exposed " +
                "parameter: " +
                MUSIC_VOLUME_PARAMETER,
                this
            );
        }

        // --------------------------------------------------------
        // SFX
        // --------------------------------------------------------

        bool sfxApplied =
            gameAudioMixer.SetFloat(
                SFX_VOLUME_PARAMETER,
                sfxEnabled
                    ? AUDIO_ON_VOLUME
                    : AUDIO_OFF_VOLUME
            );

        if (!sfxApplied)
        {
            Debug.LogWarning(
                "[MainMenuSettings] Không tìm thấy exposed " +
                "parameter: " +
                SFX_VOLUME_PARAMETER,
                this
            );
        }
    }

    // ============================================================
    // FALLBACK MUSIC STATE
    // ============================================================

    private void ApplyFallbackMusicState()
    {
        if (musicAudioSource == null)
            return;

        bool finalMusicState =
            masterAudioEnabled &&
            musicEnabled;

        musicAudioSource.mute =
            !finalMusicState;
    }

    // ============================================================
    // REFRESH SHARED STATE
    // ============================================================

    private void RefreshSharedAudioState()
    {
        masterAudioEnabled =
            LoadMasterAudioState();

        musicEnabled =
            LoadMusicState();

        sfxEnabled =
            LoadSFXState();
    }

    // ============================================================
    // REFRESH TOGGLES
    // ============================================================

    private void RefreshToggleStates()
    {
        if (masterAudioToggle != null)
        {
            masterAudioToggle.SetIsOnWithoutNotify(
                masterAudioEnabled
            );
        }

        if (musicToggle != null)
        {
            musicToggle.SetIsOnWithoutNotify(
                musicEnabled
            );
        }

        if (sfxToggle != null)
        {
            sfxToggle.SetIsOnWithoutNotify(
                sfxEnabled
            );
        }
    }

    // ============================================================
    // OPEN
    // ============================================================

    public void Open()
    {
        if (settingsPanel == null)
        {
            Debug.LogWarning(
                "[MainMenuSettings] " +
                "Settings Panel chưa được gán.",
                this
            );

            return;
        }

        settingsPanel.SetActive(true);

        RefreshSharedAudioState();
        RefreshToggleStates();
        ApplyAudioState();
    }

    // ============================================================
    // CLOSE
    // ============================================================

    public void Close()
    {
        if (settingsPanel == null)
            return;

        settingsPanel.SetActive(false);
    }

    // ============================================================
    // TOGGLE
    // ============================================================

    public void Toggle()
    {
        if (settingsPanel == null)
            return;

        bool newState =
            !settingsPanel.activeSelf;

        settingsPanel.SetActive(newState);

        if (!newState)
            return;

        RefreshSharedAudioState();
        RefreshToggleStates();
        ApplyAudioState();
    }

    // ============================================================
    // LOGOUT
    // ============================================================

    private void BindLogoutButton()
    {
        if (logoutButton == null)
            return;

        logoutButton.onClick.RemoveListener(
            OnLogoutButtonClicked
        );

        logoutButton.onClick.AddListener(
            OnLogoutButtonClicked
        );
    }

    private void OnLogoutButtonClicked()
    {
        ResolveMainMenuController();

        if (mainMenuController == null)
        {
            Debug.LogError(
                "[MainMenuSettings] Không thể Logout: " +
                "MainMenuController không tồn tại.",
                this
            );

            return;
        }

        Debug.Log(
            "[MainMenuSettings] " +
            "Logout Button -> MainMenuController.Quit()",
            this
        );

        mainMenuController.Quit();
    }

    // ============================================================
    // EXIT GAME
    // ============================================================

    private void BindExitGameButton()
    {
        if (exitGameButton == null)
            return;

        exitGameButton.onClick.RemoveListener(
            OnExitGameButtonClicked
        );

        exitGameButton.onClick.AddListener(
            OnExitGameButtonClicked
        );
    }

    private void OnExitGameButtonClicked()
    {
        ResolveMainMenuController();

        if (mainMenuController == null)
        {
            Debug.LogError(
                "[MainMenuSettings] Không thể thoát game: " +
                "MainMenuController không tồn tại.",
                this
            );

            return;
        }

        Debug.Log(
            "[MainMenuSettings] " +
            "Exit Game Button -> MainMenuController.ExitGame()",
            this
        );

        mainMenuController.ExitGame();
    }

    // ============================================================
    // SET MAIN MENU CONTROLLER
    // ============================================================

    public void SetMainMenuController(
        MainMenuController controller
    )
    {
        mainMenuController = controller;
    }

    // ============================================================
    // SET LOGOUT BUTTON
    // ============================================================

    public void SetLogoutButton(
        Button button
    )
    {
        if (logoutButton != null)
        {
            logoutButton.onClick.RemoveListener(
                OnLogoutButtonClicked
            );
        }

        logoutButton = button;

        BindLogoutButton();
    }

    // ============================================================
    // SET EXIT GAME BUTTON
    // ============================================================

    public void SetExitGameButton(
        Button button
    )
    {
        if (exitGameButton != null)
        {
            exitGameButton.onClick.RemoveListener(
                OnExitGameButtonClicked
            );
        }

        exitGameButton = button;

        BindExitGameButton();
    }

    // ============================================================
    // DESTROY
    // ============================================================

    private void OnDestroy()
    {
        if (masterAudioToggle != null)
        {
            masterAudioToggle.onValueChanged.RemoveListener(
                OnMasterAudioToggleChanged
            );
        }

        if (musicToggle != null)
        {
            musicToggle.onValueChanged.RemoveListener(
                OnMusicToggleChanged
            );
        }

        if (sfxToggle != null)
        {
            sfxToggle.onValueChanged.RemoveListener(
                OnSFXToggleChanged
            );
        }

        if (logoutButton != null)
        {
            logoutButton.onClick.RemoveListener(
                OnLogoutButtonClicked
            );
        }

        if (exitGameButton != null)
        {
            exitGameButton.onClick.RemoveListener(
                OnExitGameButtonClicked
            );
        }

        if (Instance == this)
            Instance = null;
    }
}