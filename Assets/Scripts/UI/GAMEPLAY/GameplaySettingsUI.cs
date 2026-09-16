using UnityEngine;
using UnityEngine.UI;

public class GameplaySettingsUI : MonoBehaviour
{
    public static GameplaySettingsUI Instance { get; private set; }

    // ============================================================
    // PANEL
    // ============================================================

    [Header("Settings Panel")]
    [SerializeField] private GameObject settingsPanel;


    // ============================================================
    // CLOSE BUTTON
    // ============================================================

    [Header("Close")]
    [SerializeField] private Button closeButton;


    // ============================================================
    // STATE
    // ============================================================

    public bool IsOpen
    {
        get
        {
            return settingsPanel != null &&
                   settingsPanel.activeSelf;
        }
    }


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

        BindCloseButton();

        CloseImmediate();
    }


    // ============================================================
    // BIND BUTTON
    // ============================================================

    private void BindCloseButton()
    {
        if (closeButton == null)
            return;

        closeButton.onClick.RemoveListener(Close);
        closeButton.onClick.AddListener(Close);
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
    // TOGGLE
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
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
        }

        if (Instance == this)
            Instance = null;
    }
}
