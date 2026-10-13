using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Auth;

public sealed class FirestorePlayerDataManager : MonoBehaviour
{   
    [Header("Item Database")]
    [SerializeField]
    private GameItemDatabase itemDatabase;
    // =========================================================
    // SINGLETON
    // =========================================================

    public static FirestorePlayerDataManager Instance { get; private set; }


    // =========================================================
    // CURRENT PLAYER
    // =========================================================

    public PlayerData CurrentPlayerData { get; private set; }

    public bool IsLoading { get; private set; }

    public bool HasPlayerData =>
        CurrentPlayerData != null;

    public string CurrentPlayerUID =>
        CurrentPlayerData != null
            ? CurrentPlayerData.uid
            : null;


    // =========================================================
    // FIRESTORE
    // =========================================================

    private FirebaseFirestore db;


    // =========================================================
    // EVENTS
    // =========================================================

    public Action<PlayerData> OnPlayerDataLoaded;

    public Action OnPlayerDataCleared;

    public Action<PlayerData> OnPlayerDataSaved;


    /// <summary>
    /// Fired exactly once when this manager becomes available.
    ///
    /// EquipmentManager dùng event này để xử lý trường hợp:
    ///
    /// EquipmentManager.Awake()
    ///     ↓
    /// FirestoreManager chưa tồn tại
    ///
    /// Sau đó:
    ///
    /// FirestoreManager.Awake()
    ///     ↓
    /// OnManagerReady
    ///     ↓
    /// EquipmentManager subscribe lại
    ///
    /// Không cần Update polling.
    /// </summary>
    public static event Action OnManagerReady;


    // =========================================================
    // EQUIPMENT DEFAULTS
    // =========================================================

    private const int DefaultVehicleItemId = 1;

    private const int DefaultOutfitItemId = 41;

    private const int DefaultAccessoryItemId = 0;

    private const int DefaultCosmeticItemId = 0;


    // =========================================================
    // OWNERSHIP MIGRATION
    // =========================================================

    private const int OwnershipMigrationVersion = 3;


    // =========================================================
    // ITEM RANGE
    // =========================================================

    private const int VehicleMinId = 1;
    private const int VehicleMaxId = 40;

    private const int OutfitMinId = 41;
    private const int OutfitMaxId = 60;

    private const int CosmeticMinId = 61;
    private const int CosmeticMaxId = 80;

    private const int AccessoryMinId = 81;
    private const int AccessoryMaxId = 100;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        // ---------------------------------------------------------
        // SINGLETON
        // ---------------------------------------------------------

        if (
            Instance != null &&
            Instance != this
        )
        {
            Destroy(gameObject);
            return;
        }


        Instance = this;


        // ---------------------------------------------------------
        // PERSIST
        // ---------------------------------------------------------

        DontDestroyOnLoad(gameObject);


        // ---------------------------------------------------------
        // FIRESTORE
        // ---------------------------------------------------------

        InitializeFirestore();


        // ---------------------------------------------------------
        // MANAGER READY
        // ---------------------------------------------------------

        OnManagerReady?.Invoke();
    }


    private void OnDestroy()
    {
        if (Instance != this)
        {
            return;
        }


        Instance = null;
    }

    private GameItemDatabase FindGameItemDatabaseForMigration()
{
    return itemDatabase;
}
    // =========================================================
    // FIRESTORE INITIALIZE
    // =========================================================

    private void InitializeFirestore()
    {
        try
        {
            db =
                FirebaseFirestore.DefaultInstance;


            if (db == null)
            {
                Debug.LogError(
                    "[Firestore] Không thể khởi tạo Firestore.",
                    this
                );

                return;
            }


            Debug.Log(
                "[Firestore] Initialized successfully.",
                this
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[Firestore] Initialization failed: {exception}",
                this
            );
        }
    }


    // =========================================================
    // CREATE / LOAD PLAYER
    // =========================================================

    public async Task<PlayerData> CreateOrLoadPlayerDataAsync(
        FirebaseUser user)
    {
        if (user == null)
        {
            Debug.LogError(
                "[Firestore] FirebaseUser = null.",
                this
            );

            return null;
        }


        if (db == null)
        {
            InitializeFirestore();
        }


        if (db == null)
        {
            return null;
        }


        IsLoading = true;


        string requestedUID =
            user.UserId;


        try
        {
            DocumentReference playerRef =
                db.Collection("players")
                  .Document(requestedUID);


            DocumentSnapshot snapshot =
                await playerRef.GetSnapshotAsync();


            // =====================================================
            // ACCOUNT SAFETY
            // =====================================================

            if (!IsSameCurrentFirebaseUser(requestedUID))
            {
                Debug.LogWarning(
                    "[Firestore] Account đã thay đổi trong lúc load. " +
                    "Bỏ qua dữ liệu cũ.",
                    this
                );

                return null;
            }


            // =====================================================
            // EXISTING PLAYER
            // =====================================================

            if (snapshot.Exists)
            {
                Debug.Log(
                    $"[Firestore] Player data found: {requestedUID}",
                    this
                );


                bool requiresMigration =
                    NeedsPlayerDataMigration(
                        snapshot
                    );


                PlayerData loadedData =
                    ConvertSnapshotToPlayerData(
                        snapshot,
                        requestedUID
                    );


                CurrentPlayerData =
                    loadedData;


                // =================================================
                // EVENT
                // =================================================

                OnPlayerDataLoaded?.Invoke(
                    CurrentPlayerData
                );


                Debug.Log(
                    $"[Firestore] Account loaded: " +
                    $"{CurrentPlayerData.displayName} | " +
                    $"Level {CurrentPlayerData.level} | " +
                    $"EXP {CurrentPlayerData.exp} | " +
                    $"Coins {CurrentPlayerData.coins} | " +
                    $"Gems {CurrentPlayerData.gems} | " +
                    $"BestScore {CurrentPlayerData.bestScore} | " +
                    $"TotalRuns {CurrentPlayerData.totalRuns} | " +
                    $"GumLv {CurrentPlayerData.gumUpgradeLevel} | " +
                    $"PhotonLv {CurrentPlayerData.photonUpgradeLevel} | " +
                    $"ShieldLv {CurrentPlayerData.shieldUpgradeLevel} | " +
                    $"MagnetLv {CurrentPlayerData.magnetUpgradeLevel} | " +
                    $"OwnedItems {CurrentPlayerData.ownedItemIds.Count} | " +
                    $"Vehicle {CurrentPlayerData.equippedVehicleItemId} | " +
                    $"Outfit {CurrentPlayerData.equippedOutfitItemId} | " +
                    $"Accessory {CurrentPlayerData.equippedAccessoryItemId} | " +
                    $"Cosmetic {CurrentPlayerData.equippedCosmeticItemId} | " +
                    $"MissionDate {CurrentPlayerData.missionDate}",
                    this
                );


                // =================================================
                // PERSIST MIGRATION
                // =================================================

                if (requiresMigration)
                {
                    Debug.Log(
                        "[Firestore] Legacy player data detected. " +
                        "Running equipment migration.",
                        this
                    );


                    bool migrationSaved =
                        await SavePlayerDataAsync();


                    if (migrationSaved)
                    {
                        Debug.Log(
                            "[Firestore] Equipment migration completed. " +
                            $"Vehicle={CurrentPlayerData.equippedVehicleItemId} | " +
                            $"Outfit={CurrentPlayerData.equippedOutfitItemId} | " +
                            $"Accessory={CurrentPlayerData.equippedAccessoryItemId} | " +
                            $"Cosmetic={CurrentPlayerData.equippedCosmeticItemId}",
                            this
                        );
                    }
                    else
                    {
                        Debug.LogWarning(
                            "[Firestore] Equipment migration could not be persisted. " +
                            "Runtime data remains normalized.",
                            this
                        );
                    }
                }


                // =================================================
                // LEADERBOARD
                // =================================================

                await SyncCurrentPlayerLeaderboardAsync();


                return CurrentPlayerData;
            }


            // =====================================================
            // NEW PLAYER
            // =====================================================

            Debug.Log(
                $"[Firestore] Player data not found. " +
                $"Creating: {requestedUID}",
                this
            );


            PlayerData newPlayer =
                CreateDefaultPlayerData(
                    user
                );


            Dictionary<string, object> data =
                ConvertPlayerDataToDictionary(
                    newPlayer
                );


            await playerRef.SetAsync(data);


            // =====================================================
            // ACCOUNT SAFETY AFTER AWAIT
            // =====================================================

            if (!IsSameCurrentFirebaseUser(requestedUID))
            {
                Debug.LogWarning(
                    "[Firestore] Account đã thay đổi sau khi tạo data. " +
                    "Không set CurrentPlayerData.",
                    this
                );

                return null;
            }


            CurrentPlayerData =
                newPlayer;


            OnPlayerDataLoaded?.Invoke(
                CurrentPlayerData
            );


            Debug.Log(
                $"[Firestore] Player data created: {requestedUID} | " +
                $"Vehicle={CurrentPlayerData.equippedVehicleItemId} | " +
                $"Outfit={CurrentPlayerData.equippedOutfitItemId}",
                this
            );


            // =====================================================
            // LEADERBOARD
            // =====================================================

            await SyncCurrentPlayerLeaderboardAsync();


            return CurrentPlayerData;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[Firestore] Create/Load failed: {exception}",
                this
            );

            return null;
        }
        finally
        {
            IsLoading = false;
        }
    }


    // =========================================================
    // CHECK CURRENT FIREBASE USER
    // =========================================================

    private bool IsSameCurrentFirebaseUser(
        string uid)
    {
        if (string.IsNullOrEmpty(uid))
        {
            return false;
        }


        FirebaseUser currentUser =
            FirebaseAuthManager.Instance != null
                ? FirebaseAuthManager.Instance.CurrentUser
                : null;


        if (currentUser == null)
        {
            return false;
        }


        return currentUser.UserId == uid;
    }


    // =========================================================
    // SAVE ALL PLAYER DATA
    // =========================================================

    public async Task<bool> SavePlayerDataAsync()
    {
        PlayerData dataToSave =
            CurrentPlayerData;


        if (dataToSave == null)
        {
            Debug.LogError(
                "[Firestore] Không có CurrentPlayerData.",
                this
            );

            return false;
        }


        FirebaseUser user =
            FirebaseAuthManager.Instance != null
                ? FirebaseAuthManager.Instance.CurrentUser
                : null;


        if (user == null)
        {
            Debug.LogError(
                "[Firestore] Không có FirebaseUser đang đăng nhập.",
                this
            );

            return false;
        }


        string uid =
            user.UserId;


        // =====================================================
        // UID SAFETY
        // =====================================================

        if (dataToSave.uid != uid)
        {
            Debug.LogError(
                "[Firestore] UID mismatch. " +
                "Không save dữ liệu của account khác.",
                this
            );

            return false;
        }


        if (db == null)
        {
            InitializeFirestore();
        }


        if (db == null)
        {
            return false;
        }


        try
        {
            NormalizePlayerDataInPlace(
                dataToSave
            );


            DocumentReference playerRef =
                db.Collection("players")
                  .Document(uid);


            Dictionary<string, object> data =
                ConvertPlayerDataToDictionary(
                    dataToSave
                );


            await playerRef.SetAsync(
                data,
                SetOptions.MergeAll
            );


            // =================================================
            // ACCOUNT SAFETY
            // =================================================

            if (
                CurrentPlayerData == dataToSave &&
                CurrentPlayerUID == uid
            )
            {
                OnPlayerDataSaved?.Invoke(
                    dataToSave
                );
            }


            Debug.Log(
                $"[Firestore] Player data saved: {uid}",
                this
            );


            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[Firestore] Save failed: {exception}",
                this
            );

            return false;
        }
    }


    // =========================================================
    // TOTAL RUNS
    // =========================================================

    public async Task<bool> IncrementTotalRunsAsync()
    {
        if (CurrentPlayerData == null)
        {
            Debug.LogWarning(
                "[Firestore] Không có CurrentPlayerData " +
                "để tăng TotalRuns.",
                this
            );

            return false;
        }


        if (
            CurrentPlayerData.totalRuns <
            int.MaxValue
        )
        {
            CurrentPlayerData.totalRuns++;
        }
        else
        {
            CurrentPlayerData.totalRuns =
                int.MaxValue;
        }


        Debug.Log(
            $"[Firestore] TotalRuns = " +
            $"{CurrentPlayerData.totalRuns}",
            this
        );


        return await SavePlayerDataAsync();
    }


    public async Task<bool> SetTotalRunsAsync(
        int totalRuns)
    {
        if (CurrentPlayerData == null)
        {
            Debug.LogError(
                "[Firestore] Không có player data.",
                this
            );

            return false;
        }


        CurrentPlayerData.totalRuns =
            Mathf.Max(
                0,
                totalRuns
            );


        return await SavePlayerDataAsync();
    }


    // =========================================================
    // MISSION
    // =========================================================

    public async Task<bool> SaveMissionDataAsync()
    {
        PlayerData dataToSave =
            CurrentPlayerData;


        if (dataToSave == null)
        {
            Debug.LogWarning(
                "[Firestore] Không có CurrentPlayerData " +
                "để save Mission.",
                this
            );

            return false;
        }


        FirebaseUser user =
            FirebaseAuthManager.Instance != null
                ? FirebaseAuthManager.Instance.CurrentUser
                : null;


        if (user == null)
        {
            Debug.LogWarning(
                "[Firestore] Không có FirebaseUser " +
                "để save Mission.",
                this
            );

            return false;
        }


        string uid =
            user.UserId;


        if (dataToSave.uid != uid)
        {
            Debug.LogError(
                "[Firestore] Mission save UID mismatch.",
                this
            );

            return false;
        }


        if (db == null)
        {
            InitializeFirestore();
        }


        if (db == null)
        {
            return false;
        }


        try
        {
            DocumentReference playerRef =
                db.Collection("players")
                  .Document(uid);


            Dictionary<string, object> missionData =
                ConvertMissionDataToDictionary(
                    dataToSave
                );


            await playerRef.SetAsync(
                missionData,
                SetOptions.MergeAll
            );


            Debug.Log(
                $"[Firestore] Mission data saved: {uid}",
                this
            );


            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[Firestore] Mission save failed: {exception}",
                this
            );

            return false;
        }
    }


    // =========================================================
    // PROGRESSION
    // =========================================================

    public async Task<bool> SetProgressionAsync(
        int level,
        int exp)
    {
        if (CurrentPlayerData == null)
        {
            Debug.LogError(
                "[Firestore] Không có player data.",
                this
            );

            return false;
        }


        CurrentPlayerData.level =
            Mathf.Max(
                1,
                level
            );


        CurrentPlayerData.exp =
            Mathf.Max(
                0,
                exp
            );


        return await SavePlayerDataAsync();
    }


    // =========================================================
    // COINS
    // =========================================================

    public async Task<bool> AddCoinsAsync(
        int amount)
    {
        if (CurrentPlayerData == null)
        {
            Debug.LogError(
                "[Firestore] Không có player data.",
                this
            );

            return false;
        }


        long calculatedCoins =
            (long)CurrentPlayerData.coins +
            amount;


        if (calculatedCoins < 0)
        {
            calculatedCoins = 0;
        }


        if (calculatedCoins > int.MaxValue)
        {
            calculatedCoins = int.MaxValue;
        }


        CurrentPlayerData.coins =
            (int)calculatedCoins;


        return await SavePlayerDataAsync();
    }


    public async Task<bool> SetCoinsAsync(
        int coins)
    {
        if (CurrentPlayerData == null)
        {
            Debug.LogError(
                "[Firestore] Không có player data.",
                this
            );

            return false;
        }


        CurrentPlayerData.coins =
            Mathf.Max(
                0,
                coins
            );


        return await SavePlayerDataAsync();
    }


    // =========================================================
    // GEMS
    // =========================================================

    public async Task<bool> AddGemsAsync(
        int amount)
    {
        if (CurrentPlayerData == null)
        {
            Debug.LogError(
                "[Firestore] Không có player data.",
                this
            );

            return false;
        }


        long calculatedGems =
            (long)CurrentPlayerData.gems +
            amount;


        if (calculatedGems < 0)
        {
            calculatedGems = 0;
        }


        if (calculatedGems > int.MaxValue)
        {
            calculatedGems = int.MaxValue;
        }


        CurrentPlayerData.gems =
            (int)calculatedGems;


        return await SavePlayerDataAsync();
    }


    public async Task<bool> SetGemsAsync(
        int gems)
    {
        if (CurrentPlayerData == null)
        {
            Debug.LogError(
                "[Firestore] Không có player data.",
                this
            );

            return false;
        }


        CurrentPlayerData.gems =
            Mathf.Max(
                0,
                gems
            );


        return await SavePlayerDataAsync();
    }


    // =========================================================
    // BEST SCORE
    // =========================================================

    public async Task<bool> TrySetBestScoreAsync(
        int score)
    {
        if (CurrentPlayerData == null)
        {
            Debug.LogError(
                "[Firestore] Không có player data.",
                this
            );

            return false;
        }


        score =
            Mathf.Max(
                0,
                score
            );


        if (
            score <=
            CurrentPlayerData.bestScore
        )
        {
            return true;
        }


        CurrentPlayerData.bestScore =
            score;


        bool playerSaved =
            await SavePlayerDataAsync();


        if (!playerSaved)
        {
            return false;
        }


        return await SaveLeaderboardBestScoreAsync(
            CurrentPlayerData.bestScore
        );
    }


    // =========================================================
    // LEADERBOARD
    // =========================================================

    public async Task<bool> SaveLeaderboardBestScoreAsync(
        int bestScore)
    {
        PlayerData dataToSave =
            CurrentPlayerData;


        if (dataToSave == null)
        {
            Debug.LogError(
                "[Firestore] Không có CurrentPlayerData " +
                "để save Leaderboard.",
                this
            );

            return false;
        }


        FirebaseUser user =
            FirebaseAuthManager.Instance != null
                ? FirebaseAuthManager.Instance.CurrentUser
                : null;


        if (user == null)
        {
            Debug.LogError(
                "[Firestore] Không có FirebaseUser " +
                "đang đăng nhập để save Leaderboard.",
                this
            );

            return false;
        }


        string uid =
            user.UserId;


        if (string.IsNullOrEmpty(uid))
        {
            return false;
        }


        if (dataToSave.uid != uid)
        {
            Debug.LogError(
                "[Firestore] Leaderboard UID mismatch.",
                this
            );

            return false;
        }


        bestScore =
            Mathf.Max(
                0,
                bestScore
            );


        if (db == null)
        {
            InitializeFirestore();
        }


        if (db == null)
        {
            return false;
        }


        try
        {
            DocumentReference leaderboardRef =
                db.Collection("leaderboard")
                  .Document(uid);


            DocumentSnapshot existingSnapshot =
                await leaderboardRef.GetSnapshotAsync();


            int existingBestScore = 0;


            if (
                existingSnapshot.Exists &&
                existingSnapshot.ContainsField(
                    "bestScore"
                )
            )
            {
                existingBestScore =
                    GetInt(
                        existingSnapshot,
                        "bestScore",
                        0
                    );


                existingBestScore =
                    Mathf.Max(
                        0,
                        existingBestScore
                    );
            }


            if (
                existingBestScore >
                bestScore
            )
            {
                bestScore =
                    existingBestScore;
            }


            Dictionary<string, object>
                leaderboardData =
                    new Dictionary<string, object>
                    {
                        {
                            "uid",
                            uid
                        },

                        {
                            "displayName",
                            string.IsNullOrEmpty(
                                dataToSave.displayName
                            )
                                ? "Player"
                                : dataToSave.displayName
                        },

                        {
                            "bestScore",
                            bestScore
                        },

                        {
                            "updatedAt",
                            FieldValue.ServerTimestamp
                        }
                    };


            await leaderboardRef.SetAsync(
                leaderboardData,
                SetOptions.MergeAll
            );


            Debug.Log(
                $"[Firestore] Leaderboard updated: " +
                $"{uid} | " +
                $"{dataToSave.displayName} | " +
                $"BestScore {bestScore}",
                this
            );


            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[Firestore] Leaderboard save failed: " +
                $"{exception}",
                this
            );

            return false;
        }
    }


    // =========================================================
    // SYNC LEADERBOARD
    // =========================================================

    public async Task<bool> SyncCurrentPlayerLeaderboardAsync()
    {
        PlayerData data =
            CurrentPlayerData;


        if (data == null)
        {
            return false;
        }


        FirebaseUser user =
            FirebaseAuthManager.Instance != null
                ? FirebaseAuthManager.Instance.CurrentUser
                : null;


        if (user == null)
        {
            return false;
        }


        string uid =
            user.UserId;


        if (
            string.IsNullOrEmpty(uid) ||
            data.uid != uid
        )
        {
            return false;
        }


        return await SaveLeaderboardBestScoreAsync(
            data.bestScore
        );
    }


    // =========================================================
    // SELECTED BIKE
    // =========================================================

    public async Task<bool> SetSelectedBikeAsync(
        string bikeId)
    {
        if (CurrentPlayerData == null)
        {
            return false;
        }


        if (string.IsNullOrWhiteSpace(bikeId))
        {
            return false;
        }


        if (
            CurrentPlayerData.ownedBikes == null ||
            !CurrentPlayerData.ownedBikes.Contains(
                bikeId
            )
        )
        {
            Debug.LogWarning(
                $"[Firestore] Bike chưa sở hữu: {bikeId}",
                this
            );

            return false;
        }


        CurrentPlayerData.selectedBike =
            bikeId;


        return await SavePlayerDataAsync();
    }


    // =========================================================
    // OWNED BIKES
    // =========================================================

    public async Task<bool> AddOwnedBikeAsync(
        string bikeId)
    {
        if (CurrentPlayerData == null)
        {
            return false;
        }


        if (string.IsNullOrWhiteSpace(bikeId))
        {
            return false;
        }


        if (CurrentPlayerData.ownedBikes == null)
        {
            CurrentPlayerData.ownedBikes =
                new List<string>();
        }


        if (
            !CurrentPlayerData.ownedBikes.Contains(
                bikeId
            )
        )
        {
            CurrentPlayerData.ownedBikes.Add(
                bikeId
            );
        }


        return await SavePlayerDataAsync();
    }


    public bool OwnsBike(
        string bikeId)
    {
        if (
            CurrentPlayerData == null ||
            CurrentPlayerData.ownedBikes == null
        )
        {
            return false;
        }


        return CurrentPlayerData.ownedBikes.Contains(
            bikeId
        );
    }


    // =========================================================
    // SHOP / INVENTORY
    // =========================================================

    public bool OwnsItem(
        int itemId)
    {
        if (
            CurrentPlayerData == null ||
            CurrentPlayerData.ownedItemIds == null
        )
        {
            return false;
        }


        return CurrentPlayerData.ownedItemIds.Contains(
            itemId
        );
    }


    public async Task<bool> AddOwnedItemAsync(
        int itemId)
    {
        if (CurrentPlayerData == null)
        {
            return false;
        }


        if (!IsValidShopItemId(itemId))
        {
            Debug.LogWarning(
                $"[Firestore] Shop item ID không hợp lệ: {itemId}",
                this
            );

            return false;
        }


        if (CurrentPlayerData.ownedItemIds == null)
        {
            CurrentPlayerData.ownedItemIds =
                new List<int>();
        }


        if (
            !CurrentPlayerData.ownedItemIds.Contains(
                itemId
            )
        )
        {
            CurrentPlayerData.ownedItemIds.Add(
                itemId
            );
        }


        EnsureDefaultOutfitOwnership(
            CurrentPlayerData
        );


        return await SavePlayerDataAsync();
    }


    public async Task<bool> RemoveOwnedItemAsync(
        int itemId)
    {
        if (CurrentPlayerData == null)
        {
            return false;
        }


        if (itemId == ResolveDefaultItemId(
                GameItemCategory.Outfit,
                DefaultOutfitItemId))
        {
            Debug.LogWarning(
                $"[Firestore] Không thể xóa Outfit mặc định {DefaultOutfitItemId}.",
                this
            );

            return false;
        }


        if (CurrentPlayerData.ownedItemIds == null)
        {
            CurrentPlayerData.ownedItemIds =
                new List<int>();
        }


        bool removed =
            CurrentPlayerData.ownedItemIds.Remove(
                itemId
            );


        if (!removed)
        {
            return true;
        }


        EnsureDefaultOutfitOwnership(
            CurrentPlayerData
        );


        return await SavePlayerDataAsync();
    }


    public async Task<bool> SetOwnedItemsAsync(
        List<int> itemIds)
    {
        if (CurrentPlayerData == null)
        {
            return false;
        }


        if (CurrentPlayerData.ownedItemIds == null)
        {
            CurrentPlayerData.ownedItemIds =
                new List<int>();
        }
        else
        {
            CurrentPlayerData.ownedItemIds.Clear();
        }


        if (itemIds != null)
        {
            for (
                int i = 0;
                i < itemIds.Count;
                i++
            )
            {
                int itemId =
                    itemIds[i];


                if (!IsValidShopItemId(itemId))
                {
                    continue;
                }


                if (
                    !CurrentPlayerData.ownedItemIds.Contains(
                        itemId
                    )
                )
                {
                    CurrentPlayerData.ownedItemIds.Add(
                        itemId
                    );
                }
            }
        }


        EnsureDefaultOutfitOwnership(
            CurrentPlayerData
        );


        return await SavePlayerDataAsync();
    }


    private bool IsValidShopItemId(
        int itemId)
    {
        return itemId >= GameItemDatabase.MinItemId &&
               itemId <= GameItemDatabase.MaxItemId;
    }


    // =========================================================
    // ITEM CATEGORY VALIDATION
    // =========================================================

    private bool TryGetItemCategory(
        int itemId,
        out GameItemCategory category)
    {
        category = default;

        if (itemId < GameItemDatabase.MinItemId ||
            itemId > GameItemDatabase.MaxItemId)
        {
            return false;
        }

        if (itemDatabase == null)
        {
            return false;
        }

        GameItemData item;

        if (!itemDatabase.TryGet(
                itemId,
                out item) ||
            item == null)
        {
            return false;
        }

        category = item.Category;
        return true;
    }


    private bool IsItemInCategory(
        int itemId,
        GameItemCategory expectedCategory)
    {
        GameItemCategory actualCategory;

        if (TryGetItemCategory(
                itemId,
                out actualCategory))
        {
            return actualCategory == expectedCategory;
        }

        // Database reference is optional at runtime.
        // Preserve the established global-ID contract as a fallback.
        switch (expectedCategory)
        {
            case GameItemCategory.Vehicle:
                return itemId >= VehicleMinId &&
                       itemId <= VehicleMaxId;

            case GameItemCategory.Outfit:
                return itemId >= OutfitMinId &&
                       itemId <= OutfitMaxId;

            case GameItemCategory.Cosmetic:
                return itemId >= CosmeticMinId &&
                       itemId <= CosmeticMaxId;

            case GameItemCategory.Accessory:
                return itemId >= AccessoryMinId &&
                       itemId <= AccessoryMaxId;

            default:
                return false;
        }
    }


    private int ResolveDefaultItemId(
        GameItemCategory category,
        int configuredDefaultId)
    {
        if (IsItemInCategory(
                configuredDefaultId,
                category))
        {
            return configuredDefaultId;
        }

        // Keep the existing global-ID contract as the final fallback.
        switch (category)
        {
            case GameItemCategory.Vehicle:
                return DefaultVehicleItemId;

            case GameItemCategory.Outfit:
                return DefaultOutfitItemId;

            case GameItemCategory.Accessory:
                return DefaultAccessoryItemId;

            case GameItemCategory.Cosmetic:
                return DefaultCosmeticItemId;

            default:
                return configuredDefaultId;
        }
    }


    // =========================================================
    // DEFAULT PLAYER
    // =========================================================

    private PlayerData CreateDefaultPlayerData(
        FirebaseUser user)
    {
        string displayName =
            user.Email;


        if (!string.IsNullOrEmpty(user.Email))
        {
            int atIndex =
                user.Email.IndexOf("@");


            if (atIndex > 0)
            {
                displayName =
                    user.Email.Substring(
                        0,
                        atIndex
                    );
            }
        }


        PlayerData data =
            new PlayerData();


        data.uid =
            user.UserId;


        data.displayName =
            displayName;


        // =====================================================
        // RUN
        // =====================================================

        data.totalRuns =
            0;


        // =====================================================
        // UPGRADES
        // =====================================================

        data.gumUpgradeLevel = 1;

        data.photonUpgradeLevel = 1;

        data.shieldUpgradeLevel = 1;

        data.magnetUpgradeLevel = 1;


        // =====================================================
        // ECONOMY
        // =====================================================

        data.coins = 0;

        data.gems = 0;


        // =====================================================
        // EQUIPMENT
        // =====================================================

        data.equippedVehicleItemId =
            ResolveDefaultItemId(
                GameItemCategory.Vehicle,
                DefaultVehicleItemId);

        data.equippedOutfitItemId =
            ResolveDefaultItemId(
                GameItemCategory.Outfit,
                DefaultOutfitItemId);

        data.equippedAccessoryItemId =
            DefaultAccessoryItemId;

        data.equippedCosmeticItemId =
            DefaultCosmeticItemId;


        // =====================================================
        // OWNED ITEMS
        // =====================================================

        if (data.ownedItemIds == null)
        {
            data.ownedItemIds =
                new List<int>();
        }


        EnsureDefaultOutfitOwnership(
            data
        );


        return data;
    }


    // =========================================================
    // PLAYER DATA → FIRESTORE
    // =========================================================

    private Dictionary<string, object>
        ConvertPlayerDataToDictionary(
            PlayerData data)
    {
        if (data == null)
        {
            return new Dictionary<string, object>();
        }


        NormalizePlayerDataInPlace(
            data
        );


        Dictionary<string, object> result =
            new Dictionary<string, object>
            {
                {
                    "uid",
                    data.uid ?? string.Empty
                },

                {
                    "displayName",
                    data.displayName ?? string.Empty
                },

                {
                    "level",
                    Mathf.Max(
                        1,
                        data.level
                    )
                },

                {
                    "exp",
                    Mathf.Max(
                        0,
                        data.exp
                    )
                },

                {
                    "totalRuns",
                    Mathf.Max(
                        0,
                        data.totalRuns
                    )
                },

                {
                    "coins",
                    Mathf.Max(
                        0,
                        data.coins
                    )
                },

                {
                    "gems",
                    Mathf.Max(
                        0,
                        data.gems
                    )
                },

                {
                    "bestScore",
                    Mathf.Max(
                        0,
                        data.bestScore
                    )
                },

                {
                    "selectedBike",
                    data.selectedBike ??
                    "NinjaLead"
                },

                {
                    "ownedBikes",
                    data.ownedBikes ??
                    new List<string>()
                },

                {
                    "ownedItemIds",
                    SanitizeOwnedItemIds(
                        data.ownedItemIds
                    )
                },

                {
                    "ownershipMigrationVersion",
                    OwnershipMigrationVersion
                },

                {
                    "equippedVehicleItemId",
                    data.equippedVehicleItemId
                },

                {
                    "equippedOutfitItemId",
                    data.equippedOutfitItemId
                },

                {
                    "equippedAccessoryItemId",
                    data.equippedAccessoryItemId
                },

                {
                    "equippedCosmeticItemId",
                    data.equippedCosmeticItemId
                },

                {
                    "gumUpgradeLevel",
                    Mathf.Max(
                        1,
                        data.gumUpgradeLevel
                    )
                },

                {
                    "photonUpgradeLevel",
                    Mathf.Max(
                        1,
                        data.photonUpgradeLevel
                    )
                },

                {
                    "shieldUpgradeLevel",
                    Mathf.Max(
                        1,
                        data.shieldUpgradeLevel
                    )
                },

                {
                    "magnetUpgradeLevel",
                    Mathf.Max(
                        1,
                        data.magnetUpgradeLevel
                    )
                }
            };


        Dictionary<string, object> missionData =
            ConvertMissionDataToDictionary(
                data
            );


        foreach (
            KeyValuePair<string, object> pair
            in missionData
        )
        {
            result[pair.Key] =
                pair.Value;
        }


        return result;
    }


    // =========================================================
    // NORMALIZE PLAYER DATA
    // =========================================================

    private void NormalizePlayerDataInPlace(
        PlayerData data)
    {
        if (data == null)
        {
            return;
        }


        // -----------------------------------------------------
        // BASIC
        // -----------------------------------------------------

        data.level =
            Mathf.Max(
                1,
                data.level
            );


        data.exp =
            Mathf.Max(
                0,
                data.exp
            );


        data.totalRuns =
            Mathf.Max(
                0,
                data.totalRuns
            );


        data.coins =
            Mathf.Max(
                0,
                data.coins
            );


        data.gems =
            Mathf.Max(
                0,
                data.gems
            );


        data.bestScore =
            Mathf.Max(
                0,
                data.bestScore
            );


        // -----------------------------------------------------
        // EQUIPMENT
        // -----------------------------------------------------

        data.equippedVehicleItemId =
            NormalizeVehicleId(
                data.equippedVehicleItemId
            );


        data.equippedOutfitItemId =
            NormalizeOutfitId(
                data.equippedOutfitItemId
            );


        data.equippedAccessoryItemId =
            NormalizeAccessoryId(
                data.equippedAccessoryItemId
            );


        data.equippedCosmeticItemId =
            NormalizeCosmeticId(
                data.equippedCosmeticItemId
            );


        // -----------------------------------------------------
        // OWNERSHIP
        // -----------------------------------------------------

        NormalizeItemOwnership(
            data
        );


        // -----------------------------------------------------
        // UPGRADE
        // -----------------------------------------------------

        data.gumUpgradeLevel =
            Mathf.Max(
                1,
                data.gumUpgradeLevel
            );


        data.photonUpgradeLevel =
            Mathf.Max(
                1,
                data.photonUpgradeLevel
            );


        data.shieldUpgradeLevel =
            Mathf.Max(
                1,
                data.shieldUpgradeLevel
            );


        data.magnetUpgradeLevel =
            Mathf.Max(
                1,
                data.magnetUpgradeLevel
            );


        // -----------------------------------------------------
        // MISSION
        // -----------------------------------------------------

        data.missionDailyCoins =
            Mathf.Max(
                0,
                data.missionDailyCoins
            );


        data.missionDailyDistance =
            Mathf.Max(
                0,
                data.missionDailyDistance
            );


        data.missionDailyRuns =
            Mathf.Max(
                0,
                data.missionDailyRuns
            );


        data.missionDailyExp =
            Mathf.Max(
                0,
                data.missionDailyExp
            );


        data.missionPreviousTotalExp =
            Mathf.Max(
                0,
                data.missionPreviousTotalExp
            );


        // -----------------------------------------------------
        // BIKE
        // -----------------------------------------------------

        if (
            string.IsNullOrWhiteSpace(
                data.selectedBike
            )
        )
        {
            data.selectedBike =
                "NinjaLead";
        }
    }


    // =========================================================
    // ITEM OWNERSHIP NORMALIZATION
    // =========================================================

    private void NormalizeItemOwnership(
        PlayerData data)
    {
        if (data == null)
        {
            return;
        }


        if (data.ownedItemIds == null)
        {
            data.ownedItemIds =
                new List<int>();
        }


        data.ownedItemIds =
            SanitizeOwnedItemIds(
                data.ownedItemIds
            );


        // -----------------------------------------------------
        // EQUIPPED VEHICLE MUST BE OWNED
        // -----------------------------------------------------

        if (
            !data.ownedItemIds.Contains(
                data.equippedVehicleItemId
            )
        )
        {
            data.ownedItemIds.Add(
                data.equippedVehicleItemId
            );
        }


        // -----------------------------------------------------
        // EQUIPPED OUTFIT MUST BE OWNED
        // -----------------------------------------------------

        if (
            !data.ownedItemIds.Contains(
                data.equippedOutfitItemId
            )
        )
        {
            data.ownedItemIds.Add(
                data.equippedOutfitItemId
            );
        }


        // -----------------------------------------------------
        // DEFAULT OUTFIT ALWAYS OWNED
        // -----------------------------------------------------

        EnsureDefaultOutfitOwnership(
            data
        );
    }


    // =========================================================
    // DEFAULT OUTFIT OWNERSHIP
    // =========================================================

    private void EnsureDefaultOutfitOwnership(
        PlayerData data)
    {
        if (data == null)
        {
            return;
        }


        if (data.ownedItemIds == null)
        {
            data.ownedItemIds =
                new List<int>();
        }


        int defaultOutfitId =
            ResolveDefaultItemId(
                GameItemCategory.Outfit,
                DefaultOutfitItemId);

        if (
            !data.ownedItemIds.Contains(
                defaultOutfitId
            )
        )
        {
            data.ownedItemIds.Add(
                defaultOutfitId
            );
        }
    }


    // =========================================================
    // EQUIPMENT NORMALIZATION
    // =========================================================

    private int NormalizeVehicleId(
        int itemId)
    {
        if (IsItemInCategory(
                itemId,
                GameItemCategory.Vehicle))
        {
            return itemId;
        }

        return ResolveDefaultItemId(
            GameItemCategory.Vehicle,
            DefaultVehicleItemId);
    }


    private int NormalizeOutfitId(
        int itemId)
    {
        if (IsItemInCategory(
                itemId,
                GameItemCategory.Outfit))
        {
            return itemId;
        }

        return ResolveDefaultItemId(
            GameItemCategory.Outfit,
            DefaultOutfitItemId);
    }


    private int NormalizeAccessoryId(
        int itemId)
    {
        if (itemId == DefaultAccessoryItemId)
        {
            return DefaultAccessoryItemId;
        }

        if (IsItemInCategory(
                itemId,
                GameItemCategory.Accessory))
        {
            return itemId;
        }

        return DefaultAccessoryItemId;
    }


    private int NormalizeCosmeticId(
        int itemId)
    {
        if (itemId == DefaultCosmeticItemId)
        {
            return DefaultCosmeticItemId;
        }

        if (IsItemInCategory(
                itemId,
                GameItemCategory.Cosmetic))
        {
            return itemId;
        }

        return DefaultCosmeticItemId;
    }


    // =========================================================
    // MIGRATION DETECTION
    // =========================================================

    private bool NeedsPlayerDataMigration(
        DocumentSnapshot snapshot)
    {
        if (snapshot == null)
        {
            return false;
        }


        // -----------------------------------------------------
        // OWNERSHIP VERSION
        // -----------------------------------------------------

        if (
            !snapshot.ContainsField(
                "ownershipMigrationVersion"
            )
        )
        {
            return true;
        }


        int ownershipVersion =
            GetInt(
                snapshot,
                "ownershipMigrationVersion",
                0
            );


        if (
            ownershipVersion <
            OwnershipMigrationVersion
        )
        {
            return true;
        }


        // -----------------------------------------------------
        // VEHICLE
        // -----------------------------------------------------

        if (
            !snapshot.ContainsField(
                "equippedVehicleItemId"
            )
        )
        {
            return true;
        }


        int storedVehicle =
            GetInt(
                snapshot,
                "equippedVehicleItemId",
                DefaultVehicleItemId
            );


        if (
            storedVehicle !=
            NormalizeVehicleId(
                storedVehicle
            )
        )
        {
            return true;
        }


        // -----------------------------------------------------
        // OUTFIT
        // -----------------------------------------------------

        if (
            !snapshot.ContainsField(
                "equippedOutfitItemId"
            )
        )
        {
            return true;
        }


        int storedOutfit =
            GetInt(
                snapshot,
                "equippedOutfitItemId",
                DefaultOutfitItemId
            );


        if (
            storedOutfit !=
            NormalizeOutfitId(
                storedOutfit
            )
        )
        {
            return true;
        }


        // -----------------------------------------------------
        // ACCESSORY
        // -----------------------------------------------------

        if (
            !snapshot.ContainsField(
                "equippedAccessoryItemId"
            )
        )
        {
            return true;
        }


        int storedAccessory =
            GetInt(
                snapshot,
                "equippedAccessoryItemId",
                DefaultAccessoryItemId
            );


        if (
            storedAccessory !=
            NormalizeAccessoryId(
                storedAccessory
            )
        )
        {
            return true;
        }


        // -----------------------------------------------------
        // COSMETIC
        // -----------------------------------------------------

        if (
            !snapshot.ContainsField(
                "equippedCosmeticItemId"
            )
        )
        {
            return true;
        }


        int storedCosmetic =
            GetInt(
                snapshot,
                "equippedCosmeticItemId",
                DefaultCosmeticItemId
            );


        if (
            storedCosmetic !=
            NormalizeCosmeticId(
                storedCosmetic
            )
        )
        {
            return true;
        }


        return false;
    }


    // =========================================================
    // SANITIZE OWNED ITEMS
    // =========================================================

    private List<int> SanitizeOwnedItemIds(
        List<int> source)
    {
        int capacity =
            source != null
                ? source.Count + 2
                : 2;


        List<int> result =
            new List<int>(
                capacity
            );


        HashSet<int> seen =
            new HashSet<int>();


        if (source != null)
        {
            for (
                int i = 0;
                i < source.Count;
                i++
            )
            {
                int itemId =
                    source[i];


                if (!IsValidShopItemId(itemId))
                {
                    continue;
                }


                if (!seen.Add(itemId))
                {
                    continue;
                }


                result.Add(
                    itemId
                );
            }
        }


        // -----------------------------------------------------
        // DEFAULT VEHICLE
        // -----------------------------------------------------

        int defaultVehicleId =
            ResolveDefaultItemId(
                GameItemCategory.Vehicle,
                DefaultVehicleItemId);

        if (
            seen.Add(
                defaultVehicleId
            )
        )
        {
            result.Add(
                defaultVehicleId
            );
        }


        // -----------------------------------------------------
        // DEFAULT OUTFIT
        // -----------------------------------------------------

        int defaultOutfitId =
            ResolveDefaultItemId(
                GameItemCategory.Outfit,
                DefaultOutfitItemId);

        if (
            seen.Add(
                defaultOutfitId
            )
        )
        {
            result.Add(
                defaultOutfitId
            );
        }


        return result;
    }


    // =========================================================
    // MISSION DATA → FIRESTORE
    // =========================================================

    private Dictionary<string, object>
        ConvertMissionDataToDictionary(
            PlayerData data)
    {
        List<Dictionary<string, object>>
            missionStates =
                new List<Dictionary<string, object>>();


        if (data.missionStates != null)
        {
            for (
                int i = 0;
                i < data.missionStates.Count;
                i++
            )
            {
                MissionSaveData mission =
                    data.missionStates[i];


                if (mission == null)
                {
                    continue;
                }


                missionStates.Add(
                    new Dictionary<string, object>
                    {
                        {
                            "id",
                            mission.id ??
                            string.Empty
                        },

                        {
                            "progress",
                            Mathf.Max(
                                0,
                                mission.progress
                            )
                        },

                        {
                            "claimed",
                            mission.claimed
                        }
                    }
                );
            }
        }


        return new Dictionary<string, object>
        {
            {
                "missionDate",
                data.missionDate ??
                string.Empty
            },

            {
                "missionSelection",
                data.missionSelection ??
                new List<string>()
            },

            {
                "missionDailyCoins",
                Mathf.Max(
                    0,
                    data.missionDailyCoins
                )
            },

            {
                "missionDailyDistance",
                Mathf.Max(
                    0,
                    data.missionDailyDistance
                )
            },

            {
                "missionDailyRuns",
                Mathf.Max(
                    0,
                    data.missionDailyRuns
                )
            },

            {
                "missionDailyExp",
                Mathf.Max(
                    0,
                    data.missionDailyExp
                )
            },

            {
                "missionPreviousTotalExp",
                Mathf.Max(
                    0,
                    data.missionPreviousTotalExp
                )
            },

            {
                "missionStates",
                missionStates
            }
        };
    }


    // =========================================================
    // FIRESTORE → PLAYER DATA
    // =========================================================

    private PlayerData ConvertSnapshotToPlayerData(
    DocumentSnapshot snapshot,
    string uid)
{
    PlayerData data = new PlayerData();

    data.uid = uid;

    // =====================================================
    // ACCOUNT
    // =====================================================

    data.displayName =
        GetString(
            snapshot,
            "displayName",
            "Player"
        );

    // =====================================================
    // PROGRESSION
    // =====================================================

    data.level =
        GetInt(
            snapshot,
            "level",
            1
        );

    data.exp =
        GetInt(
            snapshot,
            "exp",
            0
        );

    // =====================================================
    // RUN
    // =====================================================

    data.totalRuns =
        GetInt(
            snapshot,
            "totalRuns",
            0
        );

    // =====================================================
    // ECONOMY
    // =====================================================

    data.coins =
        GetInt(
            snapshot,
            "coins",
            0
        );

    data.gems =
        GetInt(
            snapshot,
            "gems",
            0
        );

    data.bestScore =
        GetInt(
            snapshot,
            "bestScore",
            0
        );

    // =====================================================
    // LEGACY BIKE
    //
    // Giữ lại để tương thích dữ liệu cũ.
    // KHÔNG dùng selectedBike nếu equippedVehicleItemId
    // hiện tại đã hợp lệ.
    // =====================================================

    data.selectedBike =
        GetString(
            snapshot,
            "selectedBike",
            "NinjaLead"
        );

    data.ownedBikes =
        ReadStringList(
            snapshot,
            "ownedBikes"
        );

    if (data.ownedBikes == null ||
        data.ownedBikes.Count == 0)
    {
        data.ownedBikes =
            new List<string>
            {
                "NinjaLead"
            };
    }

    // =====================================================
    // SHOP / INVENTORY
    // =====================================================

    data.ownedItemIds =
        ReadIntList(
            snapshot,
            "ownedItemIds"
        );

    if (data.ownedItemIds == null)
    {
        data.ownedItemIds =
            new List<int>();
    }

    data.ownedItemIds =
        SanitizeOwnedItemIds(
            data.ownedItemIds
        );

    // =====================================================
    // EQUIPMENT
    // =====================================================

    // -----------------------------------------------------
    // VEHICLE
    //
    // Priority:
    //
    // 1. equippedVehicleItemId hợp lệ
    // 2. Legacy selectedBike
    // 3. Default Vehicle = 1
    //
    // QUAN TRỌNG:
    // Nếu Firestore có equippedVehicleItemId hợp lệ,
    // tuyệt đối không đọc selectedBike để override.
    // -----------------------------------------------------

    bool hasPersistedVehicleId =
        snapshot.ContainsField(
            "equippedVehicleItemId"
        );

    int persistedVehicleId =
        hasPersistedVehicleId
            ? GetInt(
                snapshot,
                "equippedVehicleItemId",
                0
            )
            : 0;

    int resolvedVehicleId;

    if (persistedVehicleId >= VehicleMinId &&
        persistedVehicleId <= VehicleMaxId)
    {
        // =================================================
        // MODERN DATA
        // =================================================

        resolvedVehicleId =
            persistedVehicleId;
    }
    else
    {
        // =================================================
        // LEGACY DATA
        // =================================================

        resolvedVehicleId =
            ResolveLegacyVehicleItemId(
                data.selectedBike
            );

        // Không resolve được legacy vehicle
        // => dùng default Vehicle 1.
        if (resolvedVehicleId < VehicleMinId ||
            resolvedVehicleId > VehicleMaxId)
        {
            resolvedVehicleId =
                DefaultVehicleItemId;
        }
    }

    data.equippedVehicleItemId =
        NormalizeVehicleId(
            resolvedVehicleId
        );

    // =====================================================
    // OUTFIT
    // =====================================================

    int persistedOutfitId =
        GetInt(
            snapshot,
            "equippedOutfitItemId",
            DefaultOutfitItemId
        );

    data.equippedOutfitItemId =
        NormalizeOutfitId(
            persistedOutfitId
        );

    // =====================================================
    // ACCESSORY
    // =====================================================

    int persistedAccessoryId =
        GetInt(
            snapshot,
            "equippedAccessoryItemId",
            DefaultAccessoryItemId
        );

    data.equippedAccessoryItemId =
        NormalizeAccessoryId(
            persistedAccessoryId
        );

    // =====================================================
    // COSMETIC
    // =====================================================

    int persistedCosmeticId =
        GetInt(
            snapshot,
            "equippedCosmeticItemId",
            DefaultCosmeticItemId
        );

    data.equippedCosmeticItemId =
        NormalizeCosmeticId(
            persistedCosmeticId
        );

    // =====================================================
    // DEFAULT OUTFIT OWNERSHIP
    // =====================================================

    EnsureDefaultOutfitOwnership(
        data
    );

    // =====================================================
    // UPGRADES
    // =====================================================

    data.gumUpgradeLevel =
        Mathf.Max(
            1,
            GetInt(
                snapshot,
                "gumUpgradeLevel",
                1
            )
        );

    data.photonUpgradeLevel =
        Mathf.Max(
            1,
            GetInt(
                snapshot,
                "photonUpgradeLevel",
                1
            )
        );

    data.shieldUpgradeLevel =
        Mathf.Max(
            1,
            GetInt(
                snapshot,
                "shieldUpgradeLevel",
                1
            )
        );

    data.magnetUpgradeLevel =
        Mathf.Max(
            1,
            GetInt(
                snapshot,
                "magnetUpgradeLevel",
                1
            )
        );

    // =====================================================
    // MISSION
    // =====================================================

    data.missionDate =
        GetString(
            snapshot,
            "missionDate",
            string.Empty
        );

    data.missionSelection =
        ReadStringList(
            snapshot,
            "missionSelection"
        );

    if (data.missionSelection == null)
    {
        data.missionSelection =
            new List<string>();
    }

    data.missionDailyCoins =
        GetInt(
            snapshot,
            "missionDailyCoins",
            0
        );

    data.missionDailyDistance =
        GetInt(
            snapshot,
            "missionDailyDistance",
            0
        );

    data.missionDailyRuns =
        GetInt(
            snapshot,
            "missionDailyRuns",
            0
        );

    data.missionDailyExp =
        GetInt(
            snapshot,
            "missionDailyExp",
            0
        );

    data.missionPreviousTotalExp =
        GetInt(
            snapshot,
            "missionPreviousTotalExp",
            data.exp
        );

    data.missionStates =
        ReadMissionStates(
            snapshot,
            "missionStates"
        );

    if (data.missionStates == null)
    {
        data.missionStates =
            new List<MissionSaveData>();
    }

    // =====================================================
    // FINAL NORMALIZATION
    // =====================================================

    NormalizePlayerDataInPlace(
        data
    );

    return data;
}

    private int ResolveLegacyVehicleItemId(
    string selectedBike)
{
    if (string.IsNullOrWhiteSpace(selectedBike))
    {
        return 0;
    }


    string normalizedLegacyName =
        NormalizeLegacyBikeName(
            selectedBike
        );


    if (string.IsNullOrEmpty(normalizedLegacyName))
    {
        return 0;
    }


    // =====================================================
    // LEGACY NAME → CURRENT ITEM ID
    // =====================================================
    //
    // Database hiện tại:
    // Vehicle = ItemId 1..40
    //
    // Không dùng array index làm ID.
    // Không hard-code từng vehicle name.
    //
    // Lookup trực tiếp từ GameItemDatabase.
    // =====================================================

    GameItemDatabase database =
        FindGameItemDatabaseForMigration();


    if (database == null)
    {
        return 0;
    }


    database.Initialize();


    IReadOnlyList<GameItemData> items =
        database.Items;


    if (items == null)
    {
        return 0;
    }


    for (
        int i = 0;
        i < items.Count;
        i++
    )
    {
        GameItemData item =
            items[i];


        if (item == null)
        {
            continue;
        }


        if (
            item.Category !=
            GameItemCategory.Vehicle
        )
        {
            continue;
        }


        string normalizedItemName =
            NormalizeLegacyBikeName(
                item.ItemName
            );


        if (
            string.Equals(
                normalizedItemName,
                normalizedLegacyName,
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            return item.ItemId;
        }
    }


    return 0;
}

    private static string NormalizeLegacyBikeName(
    string value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return string.Empty;
    }


    return value
        .Replace(" ", string.Empty)
        .Replace("_", string.Empty)
        .Replace("-", string.Empty)
        .Trim();
}
    // =========================================================
    // READ STRING
    // =========================================================

    private string GetString(
        DocumentSnapshot snapshot,
        string field,
        string defaultValue)
    {
        if (!snapshot.ContainsField(field))
        {
            return defaultValue;
        }


        try
        {
            return snapshot.GetValue<string>(
                field
            );
        }
        catch
        {
            return defaultValue;
        }
    }


    // =========================================================
    // READ INT
    // =========================================================

    private int GetInt(
        DocumentSnapshot snapshot,
        string field,
        int defaultValue)
    {
        if (!snapshot.ContainsField(field))
        {
            return defaultValue;
        }


        try
        {
            long value =
                snapshot.GetValue<long>(
                    field
                );


            if (
                value > int.MaxValue ||
                value < int.MinValue
            )
            {
                return defaultValue;
            }


            return (int)value;
        }
        catch
        {
            try
            {
                return snapshot.GetValue<int>(
                    field
                );
            }
            catch
            {
                return defaultValue;
            }
        }
    }


    // =========================================================
    // READ STRING LIST
    // =========================================================

    private List<string> ReadStringList(
        DocumentSnapshot snapshot,
        string field)
    {
        if (!snapshot.ContainsField(field))
        {
            return new List<string>();
        }


        try
        {
            List<object> rawList =
                snapshot.GetValue<List<object>>(
                    field
                );


            List<string> result =
                new List<string>();


            if (rawList == null)
            {
                return result;
            }


            for (
                int i = 0;
                i < rawList.Count;
                i++
            )
            {
                object item =
                    rawList[i];


                if (item == null)
                {
                    continue;
                }


                string value =
                    item.ToString();


                if (!string.IsNullOrEmpty(value))
                {
                    result.Add(
                        value
                    );
                }
            }


            return result;
        }
        catch
        {
            return new List<string>();
        }
    }


    // =========================================================
    // READ INT LIST
    // =========================================================

    private List<int> ReadIntList(
        DocumentSnapshot snapshot,
        string field)
    {
        List<int> result =
            new List<int>();


        if (!snapshot.ContainsField(field))
        {
            return result;
        }


        try
        {
            List<object> rawList =
                snapshot.GetValue<List<object>>(
                    field
                );


            if (rawList == null)
            {
                return result;
            }


            for (
                int i = 0;
                i < rawList.Count;
                i++
            )
            {
                object item =
                    rawList[i];


                if (item == null)
                {
                    continue;
                }


                int value =
                    ConvertFirestoreInt(
                        item,
                        -1
                    );


                if (!IsValidShopItemId(value))
                {
                    continue;
                }


                if (result.Contains(value))
                {
                    continue;
                }


                result.Add(
                    value
                );
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"[Firestore] ReadIntList failed: " +
                $"{field} | {exception}",
                this
            );
        }


        return result;
    }


    // =========================================================
    // READ MISSION STATES
    // =========================================================

    private List<MissionSaveData> ReadMissionStates(
        DocumentSnapshot snapshot,
        string field)
    {
        List<MissionSaveData> result =
            new List<MissionSaveData>();


        if (!snapshot.ContainsField(field))
        {
            return result;
        }


        try
        {
            List<object> rawList =
                snapshot.GetValue<List<object>>(
                    field
                );


            if (rawList == null)
            {
                return result;
            }


            for (
                int i = 0;
                i < rawList.Count;
                i++
            )
            {
                object rawItem =
                    rawList[i];


                if (
                    !(rawItem
                        is Dictionary<string, object> map)
                )
                {
                    continue;
                }


                MissionSaveData mission =
                    new MissionSaveData();


                if (
                    map.TryGetValue(
                        "id",
                        out object idValue
                    )
                )
                {
                    mission.id =
                        idValue != null
                            ? idValue.ToString()
                            : string.Empty;
                }


                if (
                    map.TryGetValue(
                        "progress",
                        out object progressValue
                    )
                )
                {
                    mission.progress =
                        ConvertFirestoreInt(
                            progressValue,
                            0
                        );
                }


                if (
                    map.TryGetValue(
                        "claimed",
                        out object claimedValue
                    )
                )
                {
                    if (
                        claimedValue
                        is bool boolValue
                    )
                    {
                        mission.claimed =
                            boolValue;
                    }
                    else
                    {
                        mission.claimed =
                            ConvertFirestoreInt(
                                claimedValue,
                                0
                            ) == 1;
                    }
                }


                if (
                    !string.IsNullOrEmpty(
                        mission.id
                    )
                )
                {
                    mission.progress =
                        Mathf.Max(
                            0,
                            mission.progress
                        );


                    result.Add(
                        mission
                    );
                }
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"[Firestore] ReadMissionStates failed: {exception}",
                this
            );
        }


        return result;
    }


    // =========================================================
    // CONVERT FIRESTORE NUMBER
    // =========================================================

    private int ConvertFirestoreInt(
        object value,
        int defaultValue)
    {
        if (value == null)
        {
            return defaultValue;
        }


        try
        {
            if (value is long longValue)
            {
                return ClampToInt(
                    longValue
                );
            }


            if (value is int intValue)
            {
                return intValue;
            }


            if (value is double doubleValue)
            {
                return ClampToInt(
                    (long)doubleValue
                );
            }


            if (value is float floatValue)
            {
                return ClampToInt(
                    (long)floatValue
                );
            }


            return Convert.ToInt32(
                value
            );
        }
        catch
        {
            return defaultValue;
        }
    }


    private int ClampToInt(
        long value)
    {
        if (value > int.MaxValue)
        {
            return int.MaxValue;
        }


        if (value < int.MinValue)
        {
            return int.MinValue;
        }


        return (int)value;
    }


    // =========================================================
    // CLEAR CURRENT ACCOUNT
    // =========================================================

    public void ClearCurrentPlayerData()
    {
        CurrentPlayerData = null;

        IsLoading = false;


        OnPlayerDataCleared?.Invoke();


        Debug.Log(
            "[Firestore] Current player data cleared.",
            this
        );
    }
    
}