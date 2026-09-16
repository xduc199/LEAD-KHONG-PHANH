using UnityEngine;
using UnityEngine.UI;

public class MainMenuSettingsUI : MonoBehaviour
{
    public static MainMenuSettingsUI Instance { get; private set; }

    // ============================================================
    // AUDIO SOURCE
    // ============================================================

    [Header("Audio Source")]
    [SerializeField] private AudioSource musicAudioSource;


    // ============================================================
    // PANEL
    // ============================================================

    [Header("Panel")]
    [SerializeField] private GameObject settingsPanel;


    // ============================================================
    // AUDIO TOGGLES
    // ============================================================

    [Header("Audio")]
    [SerializeField] private Toggle musicToggle;

    [SerializeField] private Toggle sfxToggle;


    // ============================================================
    // STATE
    // ============================================================

    private bool musicEnabled = true;
    private bool sfxEnabled = true;


    // ============================================================
    // PUBLIC STATE
    // ============================================================

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
    }


    // ============================================================
    // INITIALIZE
    // ============================================================

    private void Initialize()
    {
        // --------------------------------------------------------
        // CLOSE SETTINGS PANEL
        // --------------------------------------------------------

        if (settingsPanel != null)
            settingsPanel.SetActive(false);


        // --------------------------------------------------------
        // DEFAULT STATE
        // --------------------------------------------------------

        musicEnabled = true;
        sfxEnabled = true;


        // --------------------------------------------------------
        // MUSIC TOGGLE
        // --------------------------------------------------------

        if (musicToggle != null)
        {
            musicToggle.SetIsOnWithoutNotify(musicEnabled);

            musicToggle.onValueChanged.RemoveListener(
                OnMusicToggleChanged
            );

            musicToggle.onValueChanged.AddListener(
                OnMusicToggleChanged
            );
        }


        // --------------------------------------------------------
        // SFX TOGGLE
        // --------------------------------------------------------

        if (sfxToggle != null)
        {
            sfxToggle.SetIsOnWithoutNotify(sfxEnabled);

            sfxToggle.onValueChanged.RemoveListener(
                OnSFXToggleChanged
            );

            sfxToggle.onValueChanged.AddListener(
                OnSFXToggleChanged
            );
        }


        // --------------------------------------------------------
        // APPLY INITIAL MUSIC STATE
        // --------------------------------------------------------

        ApplyMusicState();
    }


    // ============================================================
    // MUSIC
    // ============================================================

    private void OnMusicToggleChanged(bool enabled)
    {
        musicEnabled = enabled;

        ApplyMusicState();

        Debug.Log(
            "[MainMenuSettings] Music = " +
            (musicEnabled ? "ON" : "OFF"),
            this
        );
    }


    private void ApplyMusicState()
    {
        if (musicAudioSource == null)
        {
            Debug.LogWarning(
                "[MainMenuSettings] " +
                "Music AudioSource chưa được gán.",
                this
            );

            return;
        }

        musicAudioSource.mute = !musicEnabled;
    }


    // ============================================================
    // SFX
    // ============================================================

    private void OnSFXToggleChanged(bool enabled)
    {
        sfxEnabled = enabled;

        Debug.Log(
            "[MainMenuSettings] SFX = " +
            (sfxEnabled ? "ON" : "OFF"),
            this
        );

        // SFX chưa kết nối vì hiện tại
        // chưa xác định AudioSource SFX thực tế.
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
    // TOGGLE PANEL
    // ============================================================

    public void Toggle()
    {
        if (settingsPanel == null)
            return;

        settingsPanel.SetActive(
            !settingsPanel.activeSelf
        );
    }


    // ============================================================
    // DESTROY
    // ============================================================

    private void OnDestroy()
    {
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

        if (Instance == this)
            Instance = null;
    }
}
