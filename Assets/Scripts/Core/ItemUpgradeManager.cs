using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ItemUpgradeManager : MonoBehaviour
{
    // =========================================================
    // SINGLETON
    // =========================================================

    public static ItemUpgradeManager Instance { get; private set; }


    // =========================================================
    // DATA
    // =========================================================

    [Header("Upgrade Data")]
    [SerializeField]
    private ItemUpgradeData gumData;

    [SerializeField]
    private ItemUpgradeData photonData;

    [SerializeField]
    private ItemUpgradeData shieldData;

    [SerializeField]
    private ItemUpgradeData magnetData;


    // =========================================================
    // REFERENCES
    // =========================================================

    private FirestorePlayerDataManager firestoreManager;


    // =========================================================
    // STATE
    // =========================================================

    private bool initialized;


    // =========================================================
    // EVENTS
    // =========================================================

    public event Action<UpgradeItemType, int> OnUpgradeLevelChanged;

    public event Action<int> OnCoinsChanged;

    public event Action<UpgradeItemType, int, int> OnUpgradeSucceeded;

    public event Action<UpgradeItemType, string> OnUpgradeFailed;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }


    private void Start()
    {
        TryInitialize();
    }


    private void OnDestroy()
    {
        UnbindFirestoreEvents();
    }


    // =========================================================
    // INITIALIZE
    // =========================================================

    private void TryInitialize()
    {
        if (initialized)
            return;

        firestoreManager =
            FirestorePlayerDataManager.Instance;

        if (firestoreManager == null)
        {
            return;
        }

        BindFirestoreEvents();

        initialized = true;
    }


    private void BindFirestoreEvents()
    {
        if (firestoreManager == null)
            return;

        firestoreManager.OnPlayerDataLoaded -=
            HandlePlayerDataLoaded;

        firestoreManager.OnPlayerDataLoaded +=
            HandlePlayerDataLoaded;

        firestoreManager.OnPlayerDataCleared -=
            HandlePlayerDataCleared;

        firestoreManager.OnPlayerDataCleared +=
            HandlePlayerDataCleared;
    }


    private void UnbindFirestoreEvents()
    {
        if (firestoreManager == null)
            return;

        firestoreManager.OnPlayerDataLoaded -=
            HandlePlayerDataLoaded;

        firestoreManager.OnPlayerDataCleared -=
            HandlePlayerDataCleared;
    }


    // =========================================================
    // FIRESTORE EVENTS
    // =========================================================

    private void HandlePlayerDataLoaded(
        PlayerData playerData)
    {
        if (playerData == null)
            return;

        Debug.Log(
            "[ItemUpgradeManager] Player upgrade data loaded."
        );
    }


    private void HandlePlayerDataCleared()
    {
        Debug.Log(
            "[ItemUpgradeManager] Player upgrade data cleared."
        );
    }


    // =========================================================
    // GET UPGRADE DATA
    // =========================================================

    public ItemUpgradeData GetUpgradeData(
        UpgradeItemType itemType)
    {
        switch (itemType)
        {
            case UpgradeItemType.Gum:
                return gumData;

            case UpgradeItemType.Photon:
                return photonData;

            case UpgradeItemType.Shield:
                return shieldData;

            case UpgradeItemType.Magnet:
                return magnetData;

            default:
                return null;
        }
    }


    // =========================================================
    // GET CURRENT LEVEL
    // =========================================================

    public int GetCurrentLevel(
        UpgradeItemType itemType)
    {
        PlayerData data =
            GetCurrentPlayerData();

        if (data == null)
            return 1;

        switch (itemType)
        {
            case UpgradeItemType.Gum:
                return Mathf.Max(
                    1,
                    data.gumUpgradeLevel
                );

            case UpgradeItemType.Photon:
                return Mathf.Max(
                    1,
                    data.photonUpgradeLevel
                );

            case UpgradeItemType.Shield:
                return Mathf.Max(
                    1,
                    data.shieldUpgradeLevel
                );

            case UpgradeItemType.Magnet:
                return Mathf.Max(
                    1,
                    data.magnetUpgradeLevel
                );

            default:
                return 1;
        }
    }


    // =========================================================
    // GET NEXT LEVEL
    // =========================================================

    public int GetNextLevel(
        UpgradeItemType itemType)
    {
        int currentLevel =
            GetCurrentLevel(itemType);

        return currentLevel + 1;
    }


    // =========================================================
    // GET MAX LEVEL
    // =========================================================

    public int GetMaxLevel(
        UpgradeItemType itemType)
    {
        ItemUpgradeData data =
            GetUpgradeData(itemType);

        if (data == null)
            return 1;

        return Mathf.Max(
            1,
            data.maxLevel
        );
    }


    // =========================================================
    // IS MAX LEVEL
    // =========================================================

    public bool IsMaxLevel(
        UpgradeItemType itemType)
    {
        return
            GetCurrentLevel(itemType) >=
            GetMaxLevel(itemType);
    }


    // =========================================================
    // GET LEVEL DATA
    // =========================================================

    public ItemUpgradeLevel GetLevelData(
        UpgradeItemType itemType,
        int level)
    {
        ItemUpgradeData data =
            GetUpgradeData(itemType);

        if (data == null)
            return null;

        if (data.levels == null ||
            data.levels.Length == 0)
        {
            return null;
        }

        for (
            int i = 0;
            i < data.levels.Length;
            i++)
        {
            ItemUpgradeLevel levelData =
                data.levels[i];

            if (levelData == null)
                continue;

            if (levelData.level == level)
            {
                return levelData;
            }
        }

        return null;
    }


    // =========================================================
    // GET CURRENT LEVEL DATA
    // =========================================================

    public ItemUpgradeLevel GetCurrentLevelData(
        UpgradeItemType itemType)
    {
        int level =
            GetCurrentLevel(itemType);

        return GetLevelData(
            itemType,
            level
        );
    }


    // =========================================================
    // GET NEXT LEVEL DATA
    // =========================================================

    public ItemUpgradeLevel GetNextLevelData(
        UpgradeItemType itemType)
    {
        if (IsMaxLevel(itemType))
            return null;

        int nextLevel =
            GetNextLevel(itemType);

        return GetLevelData(
            itemType,
            nextLevel
        );
    }


    // =========================================================
    // GET UPGRADE COST
    // =========================================================

    public int GetUpgradeCost(
        UpgradeItemType itemType)
    {
        ItemUpgradeLevel nextLevel =
            GetNextLevelData(itemType);

        if (nextLevel == null)
            return 0;

        return Mathf.Max(
            0,
            nextLevel.upgradeCost
        );
    }


    // =========================================================
    // GET COINS
    // =========================================================

    public int GetCurrentCoins()
    {
        PlayerData data =
            GetCurrentPlayerData();

        if (data == null)
            return 0;

        return Mathf.Max(
            0,
            data.coins
        );
    }


    // =========================================================
    // CAN AFFORD
    // =========================================================

    public bool CanAffordUpgrade(
        UpgradeItemType itemType)
    {
        int cost =
            GetUpgradeCost(itemType);

        return GetCurrentCoins() >= cost;
    }


    // =========================================================
    // CAN UPGRADE
    // =========================================================

    public bool CanUpgrade(
        UpgradeItemType itemType)
    {
        if (!TryGetReadyManager())
            return false;

        if (GetUpgradeData(itemType) == null)
            return false;

        if (IsMaxLevel(itemType))
            return false;

        ItemUpgradeLevel nextLevel =
            GetNextLevelData(itemType);

        if (nextLevel == null)
            return false;

        return CanAffordUpgrade(itemType);
    }


    // =========================================================
    // UPGRADE
    // =========================================================

    public async Task<bool> UpgradeAsync(
        UpgradeItemType itemType)
    {
        if (!TryGetReadyManager())
        {
            NotifyUpgradeFailed(
                itemType,
                "Hệ thống nâng cấp chưa sẵn sàng."
            );

            return false;
        }


        PlayerData playerData =
            firestoreManager.CurrentPlayerData;

        if (playerData == null)
        {
            NotifyUpgradeFailed(
                itemType,
                "Không có dữ liệu người chơi."
            );

            return false;
        }


        ItemUpgradeData upgradeData =
            GetUpgradeData(itemType);

        if (upgradeData == null)
        {
            NotifyUpgradeFailed(
                itemType,
                "Chưa gán ItemUpgradeData."
            );

            return false;
        }


        int currentLevel =
            GetCurrentLevel(itemType);


        int maxLevel =
            GetMaxLevel(itemType);


        if (currentLevel >= maxLevel)
        {
            NotifyUpgradeFailed(
                itemType,
                "Item đã đạt cấp tối đa."
            );

            return false;
        }


        ItemUpgradeLevel nextLevelData =
            GetLevelData(
                itemType,
                currentLevel + 1
            );


        if (nextLevelData == null)
        {
            NotifyUpgradeFailed(
                itemType,
                $"Không tìm thấy dữ liệu Level {currentLevel + 1}."
            );

            return false;
        }


        int upgradeCost =
            Mathf.Max(
                0,
                nextLevelData.upgradeCost
            );


        int currentCoins =
            Mathf.Max(
                0,
                playerData.coins
            );


        if (currentCoins < upgradeCost)
        {
            NotifyUpgradeFailed(
                itemType,
                "Không đủ vàng."
            );

            return false;
        }


        // =====================================================
        // BACKUP
        // =====================================================

        int oldLevel =
            currentLevel;

        int oldCoins =
            currentCoins;


        // =====================================================
        // APPLY LOCALLY
        // =====================================================

        int newLevel =
            currentLevel + 1;

        int newCoins =
            currentCoins - upgradeCost;


        SetUpgradeLevel(
            itemType,
            newLevel
        );

        playerData.coins =
            newCoins;


        // =====================================================
        // SAVE FIRESTORE
        // =====================================================

        bool saveSuccess = false;

        try
        {
            saveSuccess =
                await firestoreManager
                    .SavePlayerDataAsync();
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[ItemUpgradeManager] Upgrade save exception: {exception}"
            );

            saveSuccess = false;
        }


        // =====================================================
        // SAVE FAILED → ROLLBACK
        // =====================================================

        if (!saveSuccess)
        {
            SetUpgradeLevel(
                itemType,
                oldLevel
            );

            playerData.coins =
                oldCoins;


            NotifyUpgradeFailed(
                itemType,
                "Không thể lưu nâng cấp."
            );

            return false;
        }


        // =====================================================
        // SUCCESS
        // =====================================================

        Debug.Log(
            $"[ItemUpgradeManager] " +
            $"{itemType}: Level {oldLevel} → {newLevel} | " +
            $"Coins {oldCoins} → {newCoins}"
        );


        OnUpgradeLevelChanged?.Invoke(
            itemType,
            newLevel
        );


        OnCoinsChanged?.Invoke(
            newCoins
        );


        OnUpgradeSucceeded?.Invoke(
            itemType,
            newLevel,
            upgradeCost
        );


        return true;
    }


    // =========================================================
    // SET LEVEL
    // =========================================================

    private void SetUpgradeLevel(
        UpgradeItemType itemType,
        int level)
    {
        if (firestoreManager == null ||
            firestoreManager.CurrentPlayerData == null)
        {
            return;
        }

        level =
            Mathf.Max(
                1,
                level
            );


        switch (itemType)
        {
            case UpgradeItemType.Gum:

                firestoreManager
                    .CurrentPlayerData
                    .gumUpgradeLevel =
                    level;

                break;


            case UpgradeItemType.Photon:

                firestoreManager
                    .CurrentPlayerData
                    .photonUpgradeLevel =
                    level;

                break;


            case UpgradeItemType.Shield:

                firestoreManager
                    .CurrentPlayerData
                    .shieldUpgradeLevel =
                    level;

                break;


            case UpgradeItemType.Magnet:

                firestoreManager
                    .CurrentPlayerData
                    .magnetUpgradeLevel =
                    level;

                break;
        }
    }


    // =========================================================
    // GET PLAYER DATA
    // =========================================================

    private PlayerData GetCurrentPlayerData()
    {
        if (!TryGetReadyManager())
            return null;

        return firestoreManager.CurrentPlayerData;
    }


    // =========================================================
    // READY CHECK
    // =========================================================

    private bool TryGetReadyManager()
    {
        if (firestoreManager == null)
        {
            firestoreManager =
                FirestorePlayerDataManager.Instance;

            if (firestoreManager != null)
            {
                BindFirestoreEvents();
            }
        }

        if (firestoreManager == null)
        {
            return false;
        }

        if (!firestoreManager.HasPlayerData)
        {
            return false;
        }

        return true;
    }


    // =========================================================
    // NOTIFY FAILED
    // =========================================================

    private void NotifyUpgradeFailed(
        UpgradeItemType itemType,
        string reason)
    {
        Debug.LogWarning(
            $"[ItemUpgradeManager] " +
            $"Upgrade {itemType} failed: {reason}"
        );

        OnUpgradeFailed?.Invoke(
            itemType,
            reason
        );
    }


    // =========================================================
    // DEBUG
    // =========================================================

    [ContextMenu("Debug/Log Upgrade Status")]
    private void DebugLogUpgradeStatus()
    {
        if (!TryGetReadyManager())
        {
            Debug.Log(
                "[ItemUpgradeManager] " +
                "Player data chưa sẵn sàng."
            );

            return;
        }


        Debug.Log(
            "========== ITEM UPGRADE STATUS =========="
        );


        LogItemStatus(
            UpgradeItemType.Gum
        );

        LogItemStatus(
            UpgradeItemType.Photon
        );

        LogItemStatus(
            UpgradeItemType.Shield
        );

        LogItemStatus(
            UpgradeItemType.Magnet
        );


        Debug.Log(
            $"Coins: {GetCurrentCoins()}"
        );


        Debug.Log(
            "========================================="
        );
    }


    private void LogItemStatus(
        UpgradeItemType itemType)
    {
        int level =
            GetCurrentLevel(itemType);

        int maxLevel =
            GetMaxLevel(itemType);

        int cost =
            GetUpgradeCost(itemType);


        Debug.Log(
            $"{itemType} | " +
            $"Level {level}/{maxLevel} | " +
            $"Next Cost: {cost}"
        );
    }
}
