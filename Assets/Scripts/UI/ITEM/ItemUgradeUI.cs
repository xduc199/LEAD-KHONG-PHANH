using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemUpgradeUI : MonoBehaviour
{
    public static ItemUpgradeUI Instance { get; private set; }

    // =========================================================
    // WINDOW CONTROL
    // =========================================================

    [Header("Window Control")]
    [SerializeField] private GameObject dimBackground;
    [SerializeField] private GameObject windowPanel;

    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;

    [SerializeField] private bool startOpen = false;


    // =========================================================
    // ITEM SELECTION
    // =========================================================

    [Header("Item Selection")]
    [SerializeField] private Button gumButton;
    [SerializeField] private Button photonButton;
    [SerializeField] private Button shieldButton;
    [SerializeField] private Button magnetButton;


    // =========================================================
    // HEADER
    // =========================================================

    [Header("Header")]
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text currentLevelText;
    [SerializeField] private TMP_Text coinsText;


    // =========================================================
    // LEVEL PROGRESS
    // =========================================================

    [Header("Level Progress")]
    [SerializeField] private TMP_Text levelProgressText;
    [SerializeField] private Slider levelProgressSlider;


    // =========================================================
    // CURRENT LEVEL
    // =========================================================

    [Header("Current Level")]
    [SerializeField] private TMP_Text currentTitleText;
    [SerializeField] private TMP_Text currentStat1Text;
    [SerializeField] private TMP_Text currentStat2Text;
    [SerializeField] private TMP_Text currentStat3Text;


    // =========================================================
    // NEXT LEVEL
    // =========================================================

    [Header("Next Level")]
    [SerializeField] private TMP_Text nextTitleText;
    [SerializeField] private TMP_Text nextLevelText;
    [SerializeField] private TMP_Text nextStat1Text;
    [SerializeField] private TMP_Text nextStat2Text;
    [SerializeField] private TMP_Text nextStat3Text;


    // =========================================================
    // UPGRADE
    // =========================================================

    [Header("Upgrade")]
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TMP_Text upgradeCostText;
    [SerializeField] private TMP_Text upgradeStatusText;


    // =========================================================
    // PANELS
    // =========================================================

    [Header("Panels")]
    [SerializeField] private GameObject nextLevelPanel;
    [SerializeField] private GameObject maxLevelPanel;


    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;


    // =========================================================
    // INTERNAL
    // =========================================================

    private ItemUpgradeManager manager;

    private UpgradeItemType selectedItem =
        UpgradeItemType.Gum;

    private bool isUpgrading;
    private bool initialized;


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DisableRootRaycast();

        BindButtons();

        initialized = true;

        SetWindowVisible(startOpen);
    }


    // =========================================================
    // ENABLE
    // =========================================================

    private void OnEnable()
    {
        SubscribeManagerEvents();

        RefreshUI();
    }


    // =========================================================
    // DISABLE
    // =========================================================

    private void OnDisable()
    {
        UnsubscribeManagerEvents();
    }


    // =========================================================
    // DESTROY
    // =========================================================

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        UnsubscribeManagerEvents();
    }


    // =========================================================
    // ROOT RAYCAST
    // =========================================================

    private void DisableRootRaycast()
    {
        Image rootImage =
            GetComponent<Image>();

        if (rootImage != null)
        {
            rootImage.raycastTarget = false;
        }
    }


    // =========================================================
    // OPEN
    // =========================================================

    public void Open()
    {
        SetWindowVisible(true);

        RefreshUI();

        if (debugLogs)
        {
            Debug.Log(
                "[ItemUpgradeUI] Window opened.",
                this);
        }
    }


    // =========================================================
    // CLOSE
    // =========================================================

    public void Close()
    {
        SetWindowVisible(false);

        if (debugLogs)
        {
            Debug.Log(
                "[ItemUpgradeUI] Window closed.",
                this);
        }
    }


    // =========================================================
    // TOGGLE
    // =========================================================

    public void Toggle()
    {
        bool currentState =
            windowPanel != null &&
            windowPanel.activeSelf;

        SetWindowVisible(!currentState);

        if (!currentState)
        {
            RefreshUI();
        }
    }


    // =========================================================
    // WINDOW VISIBILITY
    // =========================================================

    private void SetWindowVisible(bool visible)
    {
        if (dimBackground != null)
        {
            dimBackground.SetActive(visible);
        }

        if (windowPanel != null)
        {
            windowPanel.SetActive(visible);
        }
    }


    // =========================================================
    // BUTTON BINDING
    // =========================================================

    private void BindButtons()
    {
        if (openButton != null)
        {
            openButton.onClick.RemoveListener(Open);
            openButton.onClick.AddListener(Open);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.AddListener(Close);
        }

        if (gumButton != null)
        {
            gumButton.onClick.RemoveListener(SelectGum);
            gumButton.onClick.AddListener(SelectGum);
        }

        if (photonButton != null)
        {
            photonButton.onClick.RemoveListener(SelectPhoton);
            photonButton.onClick.AddListener(SelectPhoton);
        }

        if (shieldButton != null)
        {
            shieldButton.onClick.RemoveListener(SelectShield);
            shieldButton.onClick.AddListener(SelectShield);
        }

        if (magnetButton != null)
        {
            magnetButton.onClick.RemoveListener(SelectMagnet);
            magnetButton.onClick.AddListener(SelectMagnet);
        }

        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveListener(
                OnUpgradeButtonClicked);

            upgradeButton.onClick.AddListener(
                OnUpgradeButtonClicked);
        }
    }


    // =========================================================
    // ITEM SELECTION
    // =========================================================

    private void SelectGum()
    {
        SelectItem(
            UpgradeItemType.Gum);
    }


    private void SelectPhoton()
    {
        SelectItem(
            UpgradeItemType.Photon);
    }


    private void SelectShield()
    {
        SelectItem(
            UpgradeItemType.Shield);
    }


    private void SelectMagnet()
    {
        SelectItem(
            UpgradeItemType.Magnet);
    }


    public void SelectItem(
        UpgradeItemType itemType)
    {
        selectedItem = itemType;

        RefreshUI();

        if (debugLogs)
        {
            Debug.Log(
                "[ItemUpgradeUI] Selected item: " +
                itemType,
                this);
        }
    }


    // =========================================================
    // MANAGER
    // =========================================================

    private bool TryGetManager()
    {
        if (manager != null)
        {
            return true;
        }

        manager =
            ItemUpgradeManager.Instance;

        return manager != null;
    }


    // =========================================================
    // EVENTS
    // =========================================================

    private void SubscribeManagerEvents()
    {
        if (!TryGetManager())
        {
            return;
        }

        manager.OnUpgradeLevelChanged -=
            HandleUpgradeLevelChanged;

        manager.OnCoinsChanged -=
            HandleCoinsChanged;

        manager.OnUpgradeSucceeded -=
            HandleUpgradeSucceeded;

        manager.OnUpgradeFailed -=
            HandleUpgradeFailed;


        manager.OnUpgradeLevelChanged +=
            HandleUpgradeLevelChanged;

        manager.OnCoinsChanged +=
            HandleCoinsChanged;

        manager.OnUpgradeSucceeded +=
            HandleUpgradeSucceeded;

        manager.OnUpgradeFailed +=
            HandleUpgradeFailed;
    }


    private void UnsubscribeManagerEvents()
    {
        if (manager == null)
        {
            return;
        }

        manager.OnUpgradeLevelChanged -=
            HandleUpgradeLevelChanged;

        manager.OnCoinsChanged -=
            HandleCoinsChanged;

        manager.OnUpgradeSucceeded -=
            HandleUpgradeSucceeded;

        manager.OnUpgradeFailed -=
            HandleUpgradeFailed;
    }


    // =========================================================
    // REFRESH UI
    // =========================================================

    public void RefreshUI()
    {
        if (!initialized)
        {
            return;
        }

        if (!TryGetManager())
        {
            ShowUnavailableState();
            return;
        }


        ItemUpgradeData data =
            manager.GetUpgradeData(
                selectedItem);

        if (data == null)
        {
            ShowUnavailableState();
            return;
        }


        int currentLevel =
            manager.GetCurrentLevel(
                selectedItem);

        int maxLevel =
            manager.GetMaxLevel(
                selectedItem);


        ItemUpgradeLevel currentData =
            manager.GetCurrentLevelData(
                selectedItem);

        ItemUpgradeLevel nextData =
            manager.GetNextLevelData(
                selectedItem);


        UpdateHeader(
            currentLevel,
            maxLevel);


        UpdateCoins();


        UpdateLevelProgress(
            currentLevel,
            maxLevel);


        UpdateCurrentStats(
            currentData);


        UpdateNextStats(
            nextData);


        UpdateUpgradeState(
            currentLevel,
            maxLevel,
            nextData);
    }


    // =========================================================
    // HEADER
    // =========================================================

    private void UpdateHeader(
        int currentLevel,
        int maxLevel)
    {
        if (itemNameText != null)
        {
            itemNameText.text =
                GetItemDisplayName(
                    selectedItem);
        }


        if (currentLevelText != null)
        {
            currentLevelText.text =
                "CẤP " +
                currentLevel +
                " / " +
                maxLevel;
        }
    }


    // =========================================================
    // COINS
    // =========================================================

    private void UpdateCoins()
    {
        if (coinsText == null)
        {
            return;
        }


        int coins =
            manager.GetCurrentCoins();


        coinsText.text =
            coins.ToString("N0") +
            " VÀNG";
    }


    // =========================================================
    // PROGRESS
    // =========================================================

    private void UpdateLevelProgress(
        int currentLevel,
        int maxLevel)
    {
        if (maxLevel < 1)
        {
            maxLevel = 1;
        }


        int safeLevel =
            Mathf.Clamp(
                currentLevel,
                1,
                maxLevel);


        if (levelProgressText != null)
        {
            levelProgressText.text =
                "TIẾN ĐỘ  CẤP " +
                safeLevel +
                " / " +
                maxLevel;
        }


        if (levelProgressSlider != null)
        {
            levelProgressSlider.minValue = 0f;

            levelProgressSlider.maxValue =
                maxLevel;

            levelProgressSlider.value =
                safeLevel;
        }
    }


    // =========================================================
    // CURRENT STATS
    // =========================================================

    private void UpdateCurrentStats(
        ItemUpgradeLevel currentData)
    {
        if (currentTitleText != null)
        {
            currentTitleText.text =
                "HIỆN TẠI";
        }


        ClearCurrentStats();


        if (currentData == null)
        {
            return;
        }


        switch (selectedItem)
        {
            //==================================================
            // GUM
            //==================================================

            case UpgradeItemType.Gum:

                SetText(
                    currentStat1Text,
                    "Thời gian: " +
                    FormatFloat(
                        currentData.duration) +
                    "s");


                SetText(
                    currentStat2Text,
                    "Tốc độ ngang: " +
                    FormatFloat(
                        currentData.horizontalSpeed));


                break;


            //==================================================
            // PHOTON
            //==================================================

            case UpgradeItemType.Photon:

                SetText(
                    currentStat1Text,
                    "Thời gian: " +
                    FormatFloat(
                        currentData.duration) +
                    "s");


                SetText(
                    currentStat2Text,
                    "Hệ số tốc độ: x" +
                    FormatFloat(
                        currentData.speedMultiplier));


                break;


            //==================================================
            // SHIELD
            //==================================================

            case UpgradeItemType.Shield:

                SetText(
                    currentStat1Text,
                    "Thời gian: " +
                    FormatFloat(
                        currentData.duration) +
                    "s");


                SetText(
                    currentStat2Text,
                    "Số lần chịu đòn: " +
                    currentData.maxHits);


                break;


            //==================================================
            // MAGNET
            //==================================================

            case UpgradeItemType.Magnet:

                SetText(
                    currentStat1Text,
                    "Thời gian: " +
                    FormatFloat(
                        currentData.duration) +
                    "s");


                SetText(
                    currentStat2Text,
                    "Bán kính hút: " +
                    FormatFloat(
                        currentData.magnetRadius));


                SetText(
                    currentStat3Text,
                    "Tốc độ hút: " +
                    FormatFloat(
                        currentData.pullSpeed));


                break;
        }
    }


    // =========================================================
    // NEXT STATS
    // =========================================================

    private void UpdateNextStats(
        ItemUpgradeLevel nextData)
    {
        if (nextTitleText != null)
        {
            nextTitleText.text =
                "CẤP TIẾP THEO";
        }


        ClearNextStats();


        if (nextData == null)
        {
            if (nextLevelText != null)
            {
                nextLevelText.text =
                    "ĐÃ ĐẠT CẤP TỐI ĐA";
            }

            return;
        }


        if (nextLevelText != null)
        {
            nextLevelText.text =
                "CẤP " +
                nextData.level;
        }


        switch (selectedItem)
        {
            //==================================================
            // GUM
            //==================================================

            case UpgradeItemType.Gum:

                SetText(
                    nextStat1Text,
                    "Thời gian: " +
                    FormatFloat(
                        nextData.duration) +
                    "s");


                SetText(
                    nextStat2Text,
                    "Tốc độ ngang: " +
                    FormatFloat(
                        nextData.horizontalSpeed));


                break;


            //==================================================
            // PHOTON
            //==================================================

            case UpgradeItemType.Photon:

                SetText(
                    nextStat1Text,
                    "Thời gian: " +
                    FormatFloat(
                        nextData.duration) +
                    "s");


                SetText(
                    nextStat2Text,
                    "Hệ số tốc độ: x" +
                    FormatFloat(
                        nextData.speedMultiplier));


                break;


            //==================================================
            // SHIELD
            //==================================================

            case UpgradeItemType.Shield:

                SetText(
                    nextStat1Text,
                    "Thời gian: " +
                    FormatFloat(
                        nextData.duration) +
                    "s");


                SetText(
                    nextStat2Text,
                    "Số lần chịu đòn: " +
                    nextData.maxHits);


                break;


            //==================================================
            // MAGNET
            //==================================================

            case UpgradeItemType.Magnet:

                SetText(
                    nextStat1Text,
                    "Thời gian: " +
                    FormatFloat(
                        nextData.duration) +
                    "s");


                SetText(
                    nextStat2Text,
                    "Bán kính hút: " +
                    FormatFloat(
                        nextData.magnetRadius));


                SetText(
                    nextStat3Text,
                    "Tốc độ hút: " +
                    FormatFloat(
                        nextData.pullSpeed));


                break;
        }
    }


    // =========================================================
    // UPGRADE STATE
    // =========================================================

    private void UpdateUpgradeState(
        int currentLevel,
        int maxLevel,
        ItemUpgradeLevel nextData)
    {
        bool isMaxLevel =
            currentLevel >= maxLevel ||
            nextData == null;


        if (nextLevelPanel != null)
        {
            nextLevelPanel.SetActive(
                !isMaxLevel);
        }


        if (maxLevelPanel != null)
        {
            maxLevelPanel.SetActive(
                isMaxLevel);
        }


        if (isMaxLevel)
        {
            if (upgradeCostText != null)
            {
                upgradeCostText.text =
                    "ĐÃ ĐẠT CẤP TỐI ĐA";
            }


            if (upgradeStatusText != null)
            {
                upgradeStatusText.text =
                    "★ ĐÃ ĐẠT CẤP TỐI ĐA ★";
            }


            if (upgradeButton != null)
            {
                upgradeButton.interactable = false;
            }


            return;
        }


        int cost =
            manager.GetUpgradeCost(
                selectedItem);


        int coins =
            manager.GetCurrentCoins();


        bool canUpgrade =
            manager.CanUpgrade(
                selectedItem);


        bool enoughCoins =
            coins >= cost;


        if (upgradeCostText != null)
        {
            upgradeCostText.text =
                "GIÁ NÂNG CẤP: " +
                cost.ToString("N0") +
                " VÀNG";
        }


        if (upgradeStatusText != null)
        {
            if (isUpgrading)
            {
                upgradeStatusText.text =
                    "ĐANG NÂNG CẤP...";
            }
            else if (!enoughCoins)
            {
                upgradeStatusText.text =
                    "KHÔNG ĐỦ VÀNG";
            }
            else if (canUpgrade)
            {
                upgradeStatusText.text =
                    "SẴN SÀNG NÂNG CẤP";
            }
            else
            {
                upgradeStatusText.text =
                    "CHƯA THỂ NÂNG CẤP";
            }
        }


        if (upgradeButton != null)
        {
            upgradeButton.interactable =
                canUpgrade &&
                !isUpgrading;
        }
    }


    // =========================================================
    // UPGRADE
    // =========================================================

    private async void OnUpgradeButtonClicked()
    {
        if (isUpgrading)
        {
            return;
        }


        if (!TryGetManager())
        {
            if (upgradeStatusText != null)
            {
                upgradeStatusText.text =
                    "HỆ THỐNG CHƯA SẴN SÀNG";
            }

            return;
        }


        if (!manager.CanUpgrade(
                selectedItem))
        {
            RefreshUI();
            return;
        }


        isUpgrading = true;


        if (upgradeButton != null)
        {
            upgradeButton.interactable = false;
        }


        if (upgradeStatusText != null)
        {
            upgradeStatusText.text =
                "ĐANG NÂNG CẤP...";
        }


        try
        {
            await manager.UpgradeAsync(
                selectedItem);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "[ItemUpgradeUI] Upgrade exception: " +
                exception,
                this);


            if (upgradeStatusText != null)
            {
                upgradeStatusText.text =
                    "NÂNG CẤP THẤT BẠI";
            }
        }
        finally
        {
            isUpgrading = false;

            RefreshUI();
        }
    }


    // =========================================================
    // EVENTS
    // =========================================================

    private void HandleUpgradeLevelChanged(
        UpgradeItemType itemType,
        int newLevel)
    {
        if (itemType != selectedItem)
        {
            return;
        }

        RefreshUI();
    }


    private void HandleCoinsChanged(
        int newCoins)
    {
        RefreshUI();
    }


    private void HandleUpgradeSucceeded(
        UpgradeItemType itemType,
        int oldLevel,
        int newLevel)
    {
        if (itemType != selectedItem)
        {
            return;
        }


        RefreshUI();


        if (debugLogs)
        {
            Debug.Log(
                "[ItemUpgradeUI] " +
                itemType +
                " upgraded: " +
                oldLevel +
                " -> " +
                newLevel,
                this);
        }
    }


    private void HandleUpgradeFailed(
        UpgradeItemType itemType,
        string reason)
    {
        if (itemType != selectedItem)
        {
            return;
        }


        if (upgradeStatusText != null)
        {
            upgradeStatusText.text =
                string.IsNullOrEmpty(reason)
                    ? "NÂNG CẤP THẤT BẠI"
                    : reason;
        }


        RefreshUI();
    }


    // =========================================================
    // UNAVAILABLE
    // =========================================================

    private void ShowUnavailableState()
    {
        if (itemNameText != null)
        {
            itemNameText.text =
                "NÂNG CẤP VẬT PHẨM";
        }


        if (currentLevelText != null)
        {
            currentLevelText.text =
                "CHƯA SẴN SÀNG";
        }


        if (coinsText != null)
        {
            coinsText.text =
                "0 VÀNG";
        }


        if (levelProgressText != null)
        {
            levelProgressText.text =
                "TIẾN ĐỘ";
        }


        if (levelProgressSlider != null)
        {
            levelProgressSlider.value = 0f;
        }


        ClearCurrentStats();

        ClearNextStats();


        if (upgradeCostText != null)
        {
            upgradeCostText.text =
                "CHƯA SẴN SÀNG";
        }


        if (upgradeStatusText != null)
        {
            upgradeStatusText.text =
                "HỆ THỐNG CHƯA SẴN SÀNG";
        }


        if (upgradeButton != null)
        {
            upgradeButton.interactable = false;
        }
    }


    // =========================================================
    // CLEAR
    // =========================================================

    private void ClearCurrentStats()
    {
        SetText(
            currentStat1Text,
            "-");


        SetText(
            currentStat2Text,
            "-");


        SetText(
            currentStat3Text,
            "-");
    }


    private void ClearNextStats()
    {
        SetText(
            nextStat1Text,
            "-");


        SetText(
            nextStat2Text,
            "-");


        SetText(
            nextStat3Text,
            "-");


        if (nextLevelText != null)
        {
            nextLevelText.text = "-";
        }
    }


    // =========================================================
    // HELPERS
    // =========================================================

    private static string GetItemDisplayName(
        UpgradeItemType itemType)
    {
        switch (itemType)
        {
            case UpgradeItemType.Gum:
                return "GUM";

            case UpgradeItemType.Photon:
                return "PHOTON";

            case UpgradeItemType.Shield:
                return "SHIELD";

            case UpgradeItemType.Magnet:
                return "MAGNET";

            default:
                return itemType.ToString()
                    .ToUpperInvariant();
        }
    }


    private static string FormatFloat(
        float value)
    {
        return value.ToString("0.##");
    }


    private static void SetText(
        TMP_Text target,
        string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }
}