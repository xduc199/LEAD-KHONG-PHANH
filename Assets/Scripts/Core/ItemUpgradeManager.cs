using System;
using System.Threading.Tasks;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
public sealed class ItemUpgradeManager : MonoBehaviour
{
    // =========================================================
    // SINGLETON
    // =========================================================

    public static ItemUpgradeManager Instance { get; private set; }


    // =========================================================
    // UPGRADE DATA
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
    // FIRESTORE
    // =========================================================

    private FirestorePlayerDataManager firestoreManager;


    // =========================================================
    // STATE
    // =========================================================

    private bool initialized;

    private bool playerDataReady;

    private bool upgradeOperationRunning;


    // =========================================================
    // PUBLIC STATE
    // =========================================================

    /// <summary>
    /// ItemUpgradeManager đã kết nối Firestore.
    /// </summary>
    public bool IsInitialized
    {
        get
        {
            return initialized;
        }
    }


    /// <summary>
    /// PlayerData đã load hoàn chỉnh và có thể đọc upgrade.
    ///
    /// Đây là cờ quan trọng:
    ///
    /// false = KHÔNG được coi là Level 1.
    /// true  = CurrentPlayerData hợp lệ.
    /// </summary>
    public bool IsPlayerDataReady
    {
        get
        {
            return playerDataReady;
        }
    }


    /// <summary>
    /// Có upgrade transaction đang chạy hay không.
    /// </summary>
    public bool IsUpgradeOperationRunning
    {
        get
        {
            return upgradeOperationRunning;
        }
    }


    // =========================================================
    // EVENTS
    // =========================================================

    /// <summary>
    /// itemType, currentLevel
    /// </summary>
    public event Action<UpgradeItemType, int>
        OnUpgradeLevelChanged;


    /// <summary>
    /// coins
    /// </summary>
    public event Action<int>
        OnCoinsChanged;


    /// <summary>
    /// itemType, oldLevel, newLevel
    ///
    /// IMPORTANT:
    /// Event này trước đây bị truyền:
    /// (itemType, newLevel, upgradeCost)
    ///
    /// Đã sửa semantic.
    /// </summary>
    public event Action<UpgradeItemType, int, int>
        OnUpgradeSucceeded;


    /// <summary>
    /// itemType, reason
    /// </summary>
    public event Action<UpgradeItemType, string>
        OnUpgradeFailed;


    /// <summary>
    /// PlayerData vừa ready.
    ///
    /// Controller có thể subscribe để refresh runtime.
    /// </summary>
    public event Action
        OnPlayerUpgradeDataReady;


    /// <summary>
    /// PlayerData đã bị clear/logout.
    /// </summary>
    public event Action
        OnPlayerUpgradeDataCleared;


    // =========================================================
    // UNITY - AWAKE
    // =========================================================

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;


        // =====================================================
        // FIX:
        // DontDestroyOnLoad chỉ hoạt động trên ROOT object.
        //
        // Project của bạn trước đây có warning:
        // "DontDestroyOnLoad only works for root GameObjects"
        //
        // Nếu ItemUpgradeManager nằm dưới một parent,
        // detach nó trước khi DontDestroyOnLoad.
        // =====================================================

        if (transform.parent != null)
        {
            transform.SetParent(
                null,
                true
            );
        }

        DontDestroyOnLoad(gameObject);


        // =====================================================
        // FIRESTORE MANAGER MAY ALREADY EXIST
        // =====================================================

        TryInitialize();
    }


    // =========================================================
    // UNITY - START
    // =========================================================

    private void Start()
    {
        TryInitialize();
    }


    // =========================================================
    // ENABLE
    // =========================================================

    private void OnEnable()
    {
        FirestorePlayerDataManager.OnManagerReady +=
            HandleFirestoreManagerReady;

        TryInitialize();
    }


    // =========================================================
    // DISABLE
    // =========================================================

    private void OnDisable()
    {
        FirestorePlayerDataManager.OnManagerReady -=
            HandleFirestoreManagerReady;

        UnbindFirestoreEvents();
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

        FirestorePlayerDataManager.OnManagerReady -=
            HandleFirestoreManagerReady;

        UnbindFirestoreEvents();
    }


    // =========================================================
    // INITIALIZE
    // =========================================================

    private void TryInitialize()
    {
        // -----------------------------------------------------
        // FIND FIRESTORE MANAGER
        // -----------------------------------------------------

        if (firestoreManager == null)
        {
            firestoreManager =
                FirestorePlayerDataManager.Instance;
        }


        // -----------------------------------------------------
        // FIRESTORE CHƯA CÓ
        // -----------------------------------------------------

        if (firestoreManager == null)
        {
            return;
        }


        // -----------------------------------------------------
        // BIND EVENT
        // -----------------------------------------------------

        BindFirestoreEvents();


        // -----------------------------------------------------
        // MARK INITIALIZED
        // -----------------------------------------------------

        initialized = true;


        // -----------------------------------------------------
        // CRITICAL FIX:
        //
        // Firestore có thể đã load PlayerData trước khi
        // ItemUpgradeManager subscribe event.
        //
        // Vì vậy KHÔNG được chỉ dựa vào OnPlayerDataLoaded.
        //
        // Nếu CurrentPlayerData đã tồn tại:
        // xử lý ngay lập tức.
        // -----------------------------------------------------

        if (firestoreManager.HasPlayerData &&
            firestoreManager.CurrentPlayerData != null)
        {
            HandlePlayerDataLoaded(
                firestoreManager.CurrentPlayerData
            );
        }
    }


    // =========================================================
    // FIRESTORE MANAGER READY
    // =========================================================

    private void HandleFirestoreManagerReady()
    {
        TryInitialize();
    }


    // =========================================================
    // BIND FIRESTORE EVENTS
    // =========================================================

    private void BindFirestoreEvents()
    {
        if (firestoreManager == null)
        {
            return;
        }


        firestoreManager.OnPlayerDataLoaded -=
            HandlePlayerDataLoaded;

        firestoreManager.OnPlayerDataLoaded +=
            HandlePlayerDataLoaded;


        firestoreManager.OnPlayerDataCleared -=
            HandlePlayerDataCleared;

        firestoreManager.OnPlayerDataCleared +=
            HandlePlayerDataCleared;
    }


    // =========================================================
    // UNBIND
    // =========================================================

    private void UnbindFirestoreEvents()
    {
        if (firestoreManager == null)
        {
            return;
        }


        firestoreManager.OnPlayerDataLoaded -=
            HandlePlayerDataLoaded;

        firestoreManager.OnPlayerDataCleared -=
            HandlePlayerDataCleared;
    }


    // =========================================================
    // PLAYER DATA LOADED
    // =========================================================

    private void HandlePlayerDataLoaded(
        PlayerData playerData)
    {
        if (playerData == null)
        {
            playerDataReady = false;
            return;
        }


        // =====================================================
        // NORMALIZE UPGRADE LEVELS
        // =====================================================

        ClampAllUpgradeLevels(
            playerData
        );


        // =====================================================
        // MARK READY
        // =====================================================

        playerDataReady = true;


        // =====================================================
        // DEBUG
        // =====================================================

        Debug.Log(
            "[ItemUpgradeManager] " +
            "PLAYER UPGRADE DATA READY | " +
            $"Gum={playerData.gumUpgradeLevel} | " +
            $"Photon={playerData.photonUpgradeLevel} | " +
            $"Shield={playerData.shieldUpgradeLevel} | " +
            $"Magnet={playerData.magnetUpgradeLevel}",
            this
        );


        // =====================================================
        // NOTIFY CONTROLLERS
        // =====================================================

        OnPlayerUpgradeDataReady?.Invoke();
    }


    // =========================================================
    // PLAYER DATA CLEARED
    // =========================================================

    private void HandlePlayerDataCleared()
    {
        playerDataReady = false;


        Debug.Log(
            "[ItemUpgradeManager] " +
            "PLAYER UPGRADE DATA CLEARED.",
            this
        );


        OnPlayerUpgradeDataCleared?.Invoke();
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

    /// <summary>
    /// Lấy Level hiện tại.
    ///
    /// Compatibility:
    /// Nếu chưa ready -> trả 1.
    ///
    /// KHÔNG dùng hàm này trong runtime controller để
    /// quyết định upgrade đã sẵn sàng hay chưa.
    ///
    /// Controller phải dùng:
    /// TryGetCurrentLevelData().
    /// </summary>
    public int GetCurrentLevel(
        UpgradeItemType itemType)
    {
        PlayerData data =
            GetCurrentPlayerData();

        if (data == null)
        {
            return 1;
        }


        int level;


        switch (itemType)
        {
            case UpgradeItemType.Gum:

                level =
                    data.gumUpgradeLevel;

                break;


            case UpgradeItemType.Photon:

                level =
                    data.photonUpgradeLevel;

                break;


            case UpgradeItemType.Shield:

                level =
                    data.shieldUpgradeLevel;

                break;


            case UpgradeItemType.Magnet:

                level =
                    data.magnetUpgradeLevel;

                break;


            default:

                return 1;
        }


        return ClampLevel(
            itemType,
            level
        );
    }


    // =========================================================
    // TRY GET CURRENT LEVEL
    // =========================================================

    /// <summary>
    /// API AUTHORITATIVE cho runtime.
    ///
    /// Nếu data chưa ready:
    /// false + levelData = null.
    ///
    /// Không bao giờ giả mạo Level 1.
    /// </summary>
    public bool TryGetCurrentLevelData(
        UpgradeItemType itemType,
        out ItemUpgradeLevel levelData)
    {
        levelData = null;


        if (!IsPlayerUpgradeDataReady())
        {
            return false;
        }


        ItemUpgradeData upgradeData =
            GetUpgradeData(itemType);

        if (upgradeData == null)
        {
            return false;
        }


        PlayerData playerData =
            firestoreManager.CurrentPlayerData;

        if (playerData == null)
        {
            return false;
        }


        int level =
            GetCurrentLevel(
                itemType
            );


        if (level < 1)
        {
            return false;
        }


        // =====================================================
        // O(1)
        // =====================================================

        if (upgradeData.levels == null)
        {
            return false;
        }


        int index =
            level - 1;


        if (index < 0 ||
            index >= upgradeData.levels.Length)
        {
            return false;
        }


        levelData =
            upgradeData.levels[index];


        if (levelData == null)
        {
            return false;
        }


        return true;
    }


    // =========================================================
    // GET CURRENT LEVEL DATA
    // =========================================================

    public ItemUpgradeLevel GetCurrentLevelData(
        UpgradeItemType itemType)
    {
        ItemUpgradeLevel levelData;


        if (!TryGetCurrentLevelData(
                itemType,
                out levelData))
        {
            return null;
        }


        return levelData;
    }


    // =========================================================
    // GET NEXT LEVEL
    // =========================================================

    public int GetNextLevel(
        UpgradeItemType itemType)
    {
        int currentLevel =
            GetCurrentLevel(
                itemType
            );


        int maxLevel =
            GetMaxLevel(
                itemType
            );


        if (currentLevel >= maxLevel)
        {
            return maxLevel;
        }


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
        {
            return 1;
        }


        int configuredMax =
            Mathf.Max(
                1,
                data.maxLevel
            );


        int actualArrayLength =
            data.levels != null
                ? data.levels.Length
                : 0;


        if (actualArrayLength <= 0)
        {
            return 1;
        }


        // Không cho maxLevel vượt quá số level thực tế.
        return Mathf.Min(
            configuredMax,
            actualArrayLength
        );
    }


    // =========================================================
    // IS MAX LEVEL
    // =========================================================

    public bool IsMaxLevel(
        UpgradeItemType itemType)
    {
        if (!IsPlayerUpgradeDataReady())
        {
            return false;
        }


        return GetCurrentLevel(
            itemType
        ) >= GetMaxLevel(
            itemType
        );
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
        {
            return null;
        }


        if (data.levels == null ||
            data.levels.Length == 0)
        {
            return null;
        }


        if (level < 1)
        {
            return null;
        }


        int maxLevel =
            GetMaxLevel(
                itemType
            );


        if (level > maxLevel)
        {
            return null;
        }


        // =====================================================
        // O(1) DIRECT INDEX
        // =====================================================

        int index =
            level - 1;


        if (index < 0 ||
            index >= data.levels.Length)
        {
            return null;
        }


        ItemUpgradeLevel result =
            data.levels[index];


        if (result == null)
        {
            return null;
        }


        return result;
    }


    // =========================================================
    // GET NEXT LEVEL DATA
    // =========================================================

    public ItemUpgradeLevel GetNextLevelData(
        UpgradeItemType itemType)
    {
        if (!IsPlayerUpgradeDataReady())
        {
            return null;
        }


        if (IsMaxLevel(itemType))
        {
            return null;
        }


        return GetLevelData(
            itemType,
            GetNextLevel(itemType)
        );
    }


    // =========================================================
    // GET UPGRADE COST
    // =========================================================

    public int GetUpgradeCost(
        UpgradeItemType itemType)
    {
        ItemUpgradeLevel nextData =
            GetNextLevelData(
                itemType
            );


        if (nextData == null)
        {
            return 0;
        }


        return Mathf.Max(
            0,
            nextData.upgradeCost
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
        {
            return 0;
        }


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
        if (!IsPlayerUpgradeDataReady())
        {
            return false;
        }


        int cost =
            GetUpgradeCost(
                itemType
            );


        return GetCurrentCoins() >= cost;
    }


    // =========================================================
    // CAN UPGRADE
    // =========================================================

    public bool CanUpgrade(
        UpgradeItemType itemType)
    {
        if (!IsPlayerUpgradeDataReady())
        {
            return false;
        }


        if (GetUpgradeData(itemType) == null)
        {
            return false;
        }


        if (IsMaxLevel(itemType))
        {
            return false;
        }


        ItemUpgradeLevel nextData =
            GetNextLevelData(
                itemType
            );


        if (nextData == null)
        {
            return false;
        }


        return CanAffordUpgrade(
            itemType
        );
    }


    // =========================================================
    // UPGRADE
    // =========================================================

    public async Task<bool> UpgradeAsync(
        UpgradeItemType itemType)
    {
        // -----------------------------------------------------
        // PREVENT DOUBLE CLICK / DOUBLE TRANSACTION
        // -----------------------------------------------------

        if (upgradeOperationRunning)
        {
            NotifyUpgradeFailed(
                itemType,
                "Đang xử lý một nâng cấp khác."
            );

            return false;
        }


        // -----------------------------------------------------
        // READY
        // -----------------------------------------------------

        if (!IsPlayerUpgradeDataReady())
        {
            NotifyUpgradeFailed(
                itemType,
                "Dữ liệu người chơi chưa sẵn sàng."
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
            GetUpgradeData(
                itemType
            );


        if (upgradeData == null)
        {
            NotifyUpgradeFailed(
                itemType,
                "Chưa gán ItemUpgradeData."
            );

            return false;
        }


        // -----------------------------------------------------
        // CURRENT LEVEL
        // -----------------------------------------------------

        int oldLevel =
            GetCurrentLevel(
                itemType
            );


        int maxLevel =
            GetMaxLevel(
                itemType
            );


        if (oldLevel >= maxLevel)
        {
            NotifyUpgradeFailed(
                itemType,
                "Item đã đạt cấp tối đa."
            );

            return false;
        }


        // -----------------------------------------------------
        // NEXT DATA
        // -----------------------------------------------------

        int newLevel =
            oldLevel + 1;


        ItemUpgradeLevel nextLevelData =
            GetLevelData(
                itemType,
                newLevel
            );


        if (nextLevelData == null)
        {
            NotifyUpgradeFailed(
                itemType,
                $"Không tìm thấy dữ liệu Level {newLevel}."
            );

            return false;
        }


        // -----------------------------------------------------
        // COST
        // -----------------------------------------------------

        int upgradeCost =
            Mathf.Max(
                0,
                nextLevelData.upgradeCost
            );


        int oldCoins =
            Mathf.Max(
                0,
                playerData.coins
            );


        if (oldCoins < upgradeCost)
        {
            NotifyUpgradeFailed(
                itemType,
                "Không đủ vàng."
            );

            return false;
        }


        int newCoins =
            oldCoins - upgradeCost;


        // -----------------------------------------------------
        // LOCK
        // -----------------------------------------------------

        upgradeOperationRunning = true;


        // -----------------------------------------------------
        // APPLY LOCAL
        // -----------------------------------------------------

        SetUpgradeLevel(
            itemType,
            newLevel
        );


        playerData.coins =
            newCoins;


        // -----------------------------------------------------
        // SAVE
        // -----------------------------------------------------

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
                "[ItemUpgradeManager] " +
                $"Upgrade save exception: {exception}",
                this
            );

            saveSuccess = false;
        }


        // -----------------------------------------------------
        // SAVE FAILED → ROLLBACK
        // -----------------------------------------------------

        if (!saveSuccess)
        {
            SetUpgradeLevel(
                itemType,
                oldLevel
            );


            playerData.coins =
                oldCoins;


            upgradeOperationRunning = false;


            NotifyUpgradeFailed(
                itemType,
                "Không thể lưu nâng cấp."
            );


            return false;
        }


        // -----------------------------------------------------
        // SUCCESS
        // -----------------------------------------------------

        upgradeOperationRunning = false;


        Debug.Log(
            "[ItemUpgradeManager] " +
            $"UPGRADE SUCCESS | " +
            $"{itemType} | " +
            $"Level {oldLevel} → {newLevel} | " +
            $"Coins {oldCoins} → {newCoins}",
            this
        );


        // =====================================================
        // IMPORTANT:
        //
        // CurrentPlayerData đã chứa newLevel.
        //
        // Controllers nhận event và refresh runtime.
        // =====================================================

        OnUpgradeLevelChanged?.Invoke(
            itemType,
            newLevel
        );


        OnCoinsChanged?.Invoke(
            newCoins
        );


        OnUpgradeSucceeded?.Invoke(
            itemType,
            oldLevel,
            newLevel
        );


        return true;
    }


    // =========================================================
    // SET UPGRADE LEVEL
    // =========================================================

    private void SetUpgradeLevel(
        UpgradeItemType itemType,
        int level)
    {
        if (firestoreManager == null)
        {
            return;
        }


        PlayerData data =
            firestoreManager.CurrentPlayerData;


        if (data == null)
        {
            return;
        }


        level =
            ClampLevel(
                itemType,
                level
            );


        switch (itemType)
        {
            case UpgradeItemType.Gum:

                data.gumUpgradeLevel =
                    level;

                break;


            case UpgradeItemType.Photon:

                data.photonUpgradeLevel =
                    level;

                break;


            case UpgradeItemType.Shield:

                data.shieldUpgradeLevel =
                    level;

                break;


            case UpgradeItemType.Magnet:

                data.magnetUpgradeLevel =
                    level;

                break;
        }
    }


    // =========================================================
    // CLAMP LEVEL
    // =========================================================

    private int ClampLevel(
        UpgradeItemType itemType,
        int level)
    {
        int maxLevel =
            GetMaxLevel(
                itemType
            );


        if (maxLevel < 1)
        {
            maxLevel = 1;
        }


        return Mathf.Clamp(
            level,
            1,
            maxLevel
        );
    }


    // =========================================================
    // CLAMP ALL
    // =========================================================

    private void ClampAllUpgradeLevels(
        PlayerData data)
    {
        if (data == null)
        {
            return;
        }


        data.gumUpgradeLevel =
            ClampLevel(
                UpgradeItemType.Gum,
                data.gumUpgradeLevel
            );


        data.photonUpgradeLevel =
            ClampLevel(
                UpgradeItemType.Photon,
                data.photonUpgradeLevel
            );


        data.shieldUpgradeLevel =
            ClampLevel(
                UpgradeItemType.Shield,
                data.shieldUpgradeLevel
            );


        data.magnetUpgradeLevel =
            ClampLevel(
                UpgradeItemType.Magnet,
                data.magnetUpgradeLevel
            );
    }


    // =========================================================
    // READY CHECK
    // =========================================================

    private bool IsPlayerUpgradeDataReady()
    {
        if (!initialized)
        {
            TryInitialize();
        }


        if (!initialized)
        {
            return false;
        }


        if (!playerDataReady)
        {
            if (firestoreManager != null &&
                firestoreManager.HasPlayerData &&
                firestoreManager.CurrentPlayerData != null)
            {
                HandlePlayerDataLoaded(
                    firestoreManager.CurrentPlayerData
                );
            }
        }


        return playerDataReady &&
               firestoreManager != null &&
               firestoreManager.CurrentPlayerData != null;
    }


    // =========================================================
    // GET CURRENT PLAYER DATA
    // =========================================================

    private PlayerData GetCurrentPlayerData()
    {
        if (!IsPlayerUpgradeDataReady())
        {
            return null;
        }


        return firestoreManager.CurrentPlayerData;
    }


    // =========================================================
    // DEBUG
    // =========================================================

    [ContextMenu("Debug/Log Upgrade Status")]
    private void DebugLogUpgradeStatus()
    {
        Debug.Log(
            "========================================"
        );

        Debug.Log(
            "[ItemUpgradeManager] " +
            $"Initialized={initialized} | " +
            $"PlayerReady={playerDataReady}"
        );


        if (!IsPlayerUpgradeDataReady())
        {
            Debug.LogWarning(
                "[ItemUpgradeManager] " +
                "PlayerData chưa sẵn sàng."
            );

            return;
        }


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
            "========================================"
        );
    }

    // =========================================================
// NOTIFY UPGRADE FAILED
// =========================================================

private void NotifyUpgradeFailed(
    UpgradeItemType itemType,
    string reason)
{
    Debug.LogWarning(
        "[ItemUpgradeManager] " +
        $"Upgrade {itemType} failed: {reason}",
        this
    );

    OnUpgradeFailed?.Invoke(
        itemType,
        reason
    );
}
    // =========================================================
    // DEBUG ITEM
    // =========================================================

    private void LogItemStatus(
        UpgradeItemType itemType)
    {
        int level =
            GetCurrentLevel(
                itemType
            );


        int max =
            GetMaxLevel(
                itemType
            );


        ItemUpgradeLevel data =
            GetCurrentLevelData(
                itemType
            );


        if (data == null)
        {
            Debug.Log(
                $"[ItemUpgradeManager] " +
                $"{itemType} | " +
                $"Level={level}/{max} | " +
                "DATA=NULL"
            );

            return;
        }


        Debug.Log(
            $"[ItemUpgradeManager] " +
            $"{itemType} | " +
            $"Level={level}/{max} | " +
            $"DataLevel={data.level} | " +
            $"Duration={data.duration} | " +
            $"HorizontalSpeed={data.horizontalSpeed} | " +
            $"SpeedMultiplier={data.speedMultiplier} | " +
            $"MaxHits={data.maxHits} | " +
            $"Radius={data.magnetRadius} | " +
            $"PullSpeed={data.pullSpeed} | " +
            $"CollectDistance={data.collectDistance}"
        );
    }
}