using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class GameplaySettingsUI : MonoBehaviour
{
    public static GameplaySettingsUI Instance { get; private set; }

    // ============================================================
    // PLAYER PREFS
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
    // PANEL
    // ============================================================

    [Header("Settings Panel")]
    [SerializeField] private GameObject settingsPanel;

    // ============================================================
    // AUDIO
    // ============================================================

    [Header("Audio")]
    [Tooltip("AudioMixer dùng chung cho toàn bộ game.")]
    [SerializeField] private AudioMixer gameAudioMixer;

    [Tooltip("Music AudioSource của Gameplay.")]
    [SerializeField] private AudioSource musicAudioSource;

    [Tooltip("Toggle bật/tắt toàn bộ âm thanh game.")]
    [SerializeField] private Toggle masterAudioToggle;

    [Tooltip("Toggle bật/tắt riêng nhạc.")]
    [SerializeField] private Toggle musicToggle;

    [Tooltip("Toggle bật/tắt riêng hiệu ứng âm thanh.")]
    [SerializeField] private Toggle sfxToggle;

    // ============================================================
    // CLOSE BUTTON
    // ============================================================

    [Header("Close")]
    [SerializeField] private Button closeButton;

    // ============================================================
    // STATE
    // ============================================================

    private bool masterAudioEnabled;
    private bool musicEnabled;
    private bool sfxEnabled;

    // ============================================================
    // PUBLIC STATE
    // ============================================================

    public bool IsOpen
    {
        get
        {
            return settingsPanel != null &&
                   settingsPanel.activeSelf;
        }
    }

    public bool IsMasterAudioEnabled => masterAudioEnabled;

    public bool IsMusicEnabled => musicEnabled;

    public bool IsSFXEnabled => sfxEnabled;

    // ============================================================
    // AWAKE
    // ============================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        Initialize();
        BindCloseButton();

        CloseImmediate();
    }

    // ============================================================
    // INITIALIZE
    // ============================================================

    private void Initialize()
    {
        // --------------------------------------------------------
        // LOAD SHARED AUDIO STATE
        // --------------------------------------------------------

        masterAudioEnabled =
            PlayerPrefs.GetInt(
                MASTER_AUDIO_ENABLED_KEY,
                1
            ) == 1;

        musicEnabled =
            PlayerPrefs.GetInt(
                MUSIC_ENABLED_KEY,
                1
            ) == 1;

        sfxEnabled =
            PlayerPrefs.GetInt(
                SFX_ENABLED_KEY,
                1
            ) == 1;

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
                "[GameplaySettingsUI] " +
                "Music AudioSource chưa được gán.",
                this
            );
        }

        // --------------------------------------------------------
        // BIND TOGGLES
        // --------------------------------------------------------

        BindMasterAudioToggle();
        BindMusicToggle();
        BindSFXToggle();

        // --------------------------------------------------------
        // APPLY AUDIO
        // --------------------------------------------------------

        ApplyAudioState();
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
    // MASTER AUDIO TOGGLE CHANGED
    // ============================================================

    private void OnMasterAudioToggleChanged(bool enabled)
    {
        masterAudioEnabled = enabled;

        PlayerPrefs.SetInt(
            MASTER_AUDIO_ENABLED_KEY,
            masterAudioEnabled ? 1 : 0
        );

        PlayerPrefs.Save();

        ApplyAudioState();

        Debug.Log(
            "[GameplaySettingsUI] Master Audio = " +
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
    // MUSIC TOGGLE CHANGED
    // ============================================================

    private void OnMusicToggleChanged(bool enabled)
    {
        musicEnabled = enabled;

        PlayerPrefs.SetInt(
            MUSIC_ENABLED_KEY,
            musicEnabled ? 1 : 0
        );

        PlayerPrefs.Save();

        ApplyAudioState();

        Debug.Log(
            "[GameplaySettingsUI] Music = " +
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
    // SFX TOGGLE CHANGED
    // ============================================================

    private void OnSFXToggleChanged(bool enabled)
    {
        sfxEnabled = enabled;

        PlayerPrefs.SetInt(
            SFX_ENABLED_KEY,
            sfxEnabled ? 1 : 0
        );

        PlayerPrefs.Save();

        ApplyAudioState();

        Debug.Log(
            "[GameplaySettingsUI] SFX = " +
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
                "[GameplaySettingsUI] Không tìm thấy exposed " +
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
                "[GameplaySettingsUI] Không tìm thấy exposed " +
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
                "[GameplaySettingsUI] Không tìm thấy exposed " +
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
            PlayerPrefs.GetInt(
                MASTER_AUDIO_ENABLED_KEY,
                1
            ) == 1;

        musicEnabled =
            PlayerPrefs.GetInt(
                MUSIC_ENABLED_KEY,
                1
            ) == 1;

        sfxEnabled =
            PlayerPrefs.GetInt(
                SFX_ENABLED_KEY,
                1
            ) == 1;
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
    // BIND CLOSE BUTTON
    // ============================================================

    private void BindCloseButton()
    {
        if (closeButton == null)
            return;

        closeButton.onClick.RemoveListener(
            Close
        );

        closeButton.onClick.AddListener(
            Close
        );
    }

    // ============================================================
    // OPEN
    // ============================================================

    public void Open()
    {
        if (settingsPanel == null)
        {
            Debug.LogWarning(
                "[GameplaySettingsUI] " +
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
    // CLOSE IMMEDIATE
    // ============================================================

    private void CloseImmediate()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    // ============================================================
    // TOGGLE PANEL
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

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(
                Close
            );
        }

        if (Instance == this)
            Instance = null;
    }
}