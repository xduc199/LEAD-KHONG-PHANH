using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý equipment hiện tại của Player.
///
/// Responsibilities:
/// - Load equipment từ PlayerData.
/// - Equip / Unequip.
/// - Validate ownership khi người chơi chủ động equip.
/// - Rebuild PlayerRuntimeStats.
/// - Save equipment vào Firestore.
/// - Persist xuyên Main Menu -> Gameplay.
/// - Phát event khi equipment state thay đổi.
///
/// Không chịu trách nhiệm:
/// - Shop purchase.
/// - Economy.
/// - Ownership database.
/// - UI.
/// - Vehicle / Outfit / Cosmetic / Accessory visual.
///
/// IMPORTANT:
/// - EquipmentManager persistent.
/// - Không phụ thuộc bắt buộc vào PlayerRuntimeStats.
/// - Main Menu có thể sử dụng EquipmentManager khi chưa có Player.
/// - Gameplay Player bind RuntimeStats khi Player xuất hiện.
/// - EquipmentManager là SOURCE OF TRUTH của Equipped ItemId.
///
/// LIFECYCLE:
/// FirestorePlayerDataManager
///        ↓
/// OnManagerReady
///        ↓
/// EquipmentManager
///        ↓
/// Equipment READY
///        ↓
/// OnEquipmentReady
///        ↓
/// OnEquipmentChanged
///        ↓
/// Visual Binders
/// </summary>
[DisallowMultipleComponent]
public sealed class EquipmentManager : MonoBehaviour
{
    // =====================================================================
    // SINGLETON
    // =====================================================================

    public static EquipmentManager Instance { get; private set; }


    // =====================================================================
    // DEFAULT EQUIPMENT
    // =====================================================================

    private const int DefaultVehicleItemId = 1;

    private const int DefaultOutfitItemId = 41;

    // 0 = không trang bị.
    private const int DefaultAccessoryItemId = 0;

    // 0 = không trang bị.
    private const int DefaultCosmeticItemId = 0;


    // =====================================================================
    // REFERENCES
    // =====================================================================

    [Header("Data")]
    [SerializeField]
    private GameItemDatabase itemDatabase;


    // =====================================================================
    // RUNTIME STATS
    // =====================================================================

    private PlayerRuntimeStats runtimeStats;


    // =====================================================================
    // RUNTIME EQUIPMENT
    // =====================================================================

    private int equippedVehicleId;

    private int equippedOutfitId;

    private int equippedAccessoryId;

    private int equippedCosmeticId;


    // =====================================================================
    // INITIALIZATION
    // =====================================================================

    private bool initialized;

    /*
     * TRUE chỉ khi EquipmentManager đã có một equipment state
     * hợp lệ để các visual/system khác sử dụng.
     *
     * Đây là state khác với initialized:
     *
     * initialized
     *     = EquipmentManager object đã khởi tạo.
     *
     * equipmentReady
     *     = Equipped IDs hiện tại đã sẵn sàng để consume.
     */
    private bool equipmentReady;


    // =====================================================================
    // FIRESTORE SUBSCRIPTION
    // =====================================================================

    private bool firestoreSubscribed;

    /// <summary>
    /// Instance FirestorePlayerDataManager mà EquipmentManager
    /// thực sự đang subscribe.
    ///
    /// Không dùng Instance hiện tại để unsubscribe vì Instance
    /// có thể đã chuyển sang manager mới.
    /// </summary>
    private FirestorePlayerDataManager subscribedFirestore;


    // =====================================================================
    // SAVE STATE
    // =====================================================================

    private bool saveInProgress;

    private bool saveRequested;


    // =====================================================================
    // EVENTS
    // =====================================================================

    /// <summary>
    /// Fired sau khi một item được equip thành công.
    /// </summary>
    public event Action<GameItemData> OnItemEquipped;


    /// <summary>
    /// Fired khi equipment của một category thay đổi.
    ///
    /// Sources:
    /// - TryEquip()
    /// - TryUnequip()
    /// - PlayerData load
    /// - Account clear
    ///
    /// Visual binders subscribe event này để cập nhật visual.
    /// </summary>
    public event Action<GameItemCategory> OnEquipmentChanged;


    /// <summary>
    /// Fired đúng thời điểm EquipmentManager đã có equipment
    /// state hoàn chỉnh và an toàn để các visual binder consume.
    ///
    /// Đây là lifecycle event.
    ///
    /// Thứ tự:
    ///
    /// Load PlayerData
    ///      ↓
    /// Resolve Equipment
    ///      ↓
    /// InventoryOwnership.Refresh()
    ///      ↓
    /// initialized = true
    ///      ↓
    /// equipmentReady = true
    ///      ↓
    /// RebuildRuntimeStats()
    ///      ↓
    /// OnEquipmentReady
    ///      ↓
    /// OnEquipmentChanged
    /// </summary>
    public event Action OnEquipmentReady;


    // =====================================================================
    // PROPERTIES
    // =====================================================================

    public int EquippedVehicleId =>
        equippedVehicleId;


    public int EquippedOutfitId =>
        equippedOutfitId;


    public int EquippedAccessoryId =>
        equippedAccessoryId;


    public int EquippedCosmeticId =>
        equippedCosmeticId;


    public bool IsInitialized =>
        initialized;


    /// <summary>
    /// TRUE khi equipment state hiện tại đã sẵn sàng
    /// cho MainMenuCharacterShowcase / visual binders.
    /// </summary>
    public bool IsEquipmentReady =>
        equipmentReady;


    public PlayerRuntimeStats RuntimeStats =>
        runtimeStats;


    // =====================================================================
    // UNITY
    // =====================================================================

    private void Awake()
    {
        // ---------------------------------------------------------------
        // SINGLETON
        // ---------------------------------------------------------------

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;


        // ---------------------------------------------------------------
        // PERSIST
        // ---------------------------------------------------------------

        DontDestroyOnLoad(gameObject);


        // ---------------------------------------------------------------
        // INITIALIZE
        // ---------------------------------------------------------------

        Initialize();
    }


    private void OnDestroy()
    {
        UnsubscribeFromFirestore();

        if (Instance == this)
        {
            Instance = null;
        }

        runtimeStats = null;

        initialized = false;
        equipmentReady = false;
    }


    // =====================================================================
    // INITIALIZE
    // =====================================================================

    private void Initialize()
    {
        initialized = false;
        equipmentReady = false;


        // ---------------------------------------------------------------
        // DATABASE
        // ---------------------------------------------------------------

        if (itemDatabase == null)
        {
            Debug.LogError(
                "[EquipmentManager] " +
                "GameItemDatabase chưa được gán.",
                this
            );
        }
        else
        {
            itemDatabase.Initialize();
        }


        // ---------------------------------------------------------------
        // FIRESTORE
        // ---------------------------------------------------------------

        SubscribeToFirestore();


        FirestorePlayerDataManager firestore =
            FirestorePlayerDataManager.Instance;


        // ---------------------------------------------------------------
        // PLAYER DATA ĐÃ CÓ
        // ---------------------------------------------------------------

        if (firestore != null && firestore.HasPlayerData)
        {
            HandlePlayerDataLoaded(
                firestore.CurrentPlayerData
            );

            return;
        }


        // ---------------------------------------------------------------
        // CHƯA CÓ PLAYER DATA
        // ---------------------------------------------------------------

        /*
         * Main Menu vẫn có thể sử dụng EquipmentManager
         * trước khi có PlayerData.
         *
         * Canonical defaults:
         *
         * Vehicle = 1
         * Outfit  = 41
         */
        ApplyDefaultEquipment();

        initialized = true;

        RebuildRuntimeStats();

        MarkEquipmentReady();
    }


    // =====================================================================
    // MARK READY
    // =====================================================================

    private void MarkEquipmentReady()
    {
        if (equipmentReady)
        {
            return;
        }

        equipmentReady = true;

        Debug.Log(
            "[EquipmentManager] " +
            $"Equipment READY → " +
            $"Vehicle={equippedVehicleId} | " +
            $"Outfit={equippedOutfitId} | " +
            $"Accessory={equippedAccessoryId} | " +
            $"Cosmetic={equippedCosmeticId}"
        );

        /*
         * Lifecycle event trước.
         *
         * Showcase có thể gọi GetEquippedItemId()
         * ngay trong callback vì state đã hoàn chỉnh.
         */
        OnEquipmentReady?.Invoke();
    }


    // =====================================================================
    // RUNTIME STATS BINDING
    // =====================================================================

    /// <summary>
    /// Bind PlayerRuntimeStats của Player hiện tại.
    /// </summary>
    public void BindRuntimeStats(
        PlayerRuntimeStats stats)
    {
        runtimeStats = stats;

        if (runtimeStats == null)
        {
            Debug.LogWarning(
                "[EquipmentManager] " +
                "BindRuntimeStats nhận PlayerRuntimeStats = null."
            );

            return;
        }

        RebuildRuntimeStats();

        Debug.Log(
            "[EquipmentManager] " +
            "PlayerRuntimeStats đã được bind."
        );
    }


    /// <summary>
    /// Unbind PlayerRuntimeStats hiện tại.
    /// </summary>
    public void UnbindRuntimeStats(
        PlayerRuntimeStats stats)
    {
        if (runtimeStats != stats)
        {
            return;
        }

        runtimeStats = null;

        Debug.Log(
            "[EquipmentManager] " +
            "PlayerRuntimeStats đã được unbind."
        );
    }


    // =====================================================================
    // FIRESTORE SUBSCRIPTION
    // =====================================================================

    /// <summary>
    /// Subscribe Firestore events exactly once cho đúng instance.
    ///
    /// Nếu FirestorePlayerDataManager chưa tồn tại,
    /// EquipmentManager sẽ chờ OnManagerReady.
    ///
    /// Không sử dụng polling / Update().
    /// </summary>
    private void SubscribeToFirestore()
    {
        FirestorePlayerDataManager firestore =
            FirestorePlayerDataManager.Instance;


        // ---------------------------------------------------------------
        // FIRESTORE CHƯA READY
        // ---------------------------------------------------------------

        if (firestore == null)
        {
            FirestorePlayerDataManager.OnManagerReady -=
                HandleFirestoreManagerReady;

            FirestorePlayerDataManager.OnManagerReady +=
                HandleFirestoreManagerReady;

            return;
        }


        // ---------------------------------------------------------------
        // ĐÃ SUBSCRIBE ĐÚNG INSTANCE
        // ---------------------------------------------------------------

        if (
            firestoreSubscribed &&
            subscribedFirestore == firestore
        )
        {
            return;
        }


        // ---------------------------------------------------------------
        // INSTANCE CŨ KHÔNG CÒN HỢP LỆ
        // ---------------------------------------------------------------

        if (firestoreSubscribed)
        {
            UnsubscribeFromFirestoreInstance();
        }


        // ---------------------------------------------------------------
        // SUBSCRIBE INSTANCE MỚI
        // ---------------------------------------------------------------

        firestore.OnPlayerDataLoaded +=
            HandlePlayerDataLoaded;

        firestore.OnPlayerDataCleared +=
            HandlePlayerDataCleared;

        subscribedFirestore =
            firestore;

        firestoreSubscribed = true;


        // Không cần subscribe OnManagerReady nữa vì manager đã tồn tại.
        FirestorePlayerDataManager.OnManagerReady -=
            HandleFirestoreManagerReady;
    }


    /// <summary>
    /// FirestorePlayerDataManager vừa được tạo.
    /// </summary>
    private void HandleFirestoreManagerReady()
    {
        FirestorePlayerDataManager.OnManagerReady -=
            HandleFirestoreManagerReady;


        SubscribeToFirestore();


        FirestorePlayerDataManager firestore =
            FirestorePlayerDataManager.Instance;


        if (firestore == null)
        {
            return;
        }


        // ---------------------------------------------------------------
        // DATA ĐÃ LOAD TRƯỚC KHI EQUIPMENT SUBSCRIBE
        // ---------------------------------------------------------------

        if (firestore.HasPlayerData)
        {
            HandlePlayerDataLoaded(
                firestore.CurrentPlayerData
            );
        }
    }


    /// <summary>
    /// Unsubscribe khỏi instance Firestore hiện tại.
    /// </summary>
    private void UnsubscribeFromFirestoreInstance()
    {
        if (!firestoreSubscribed)
        {
            return;
        }


        FirestorePlayerDataManager firestore =
            subscribedFirestore;


        if (firestore != null)
        {
            firestore.OnPlayerDataLoaded -=
                HandlePlayerDataLoaded;

            firestore.OnPlayerDataCleared -=
                HandlePlayerDataCleared;
        }


        subscribedFirestore = null;

        firestoreSubscribed = false;
    }


    /// <summary>
    /// Unsubscribe toàn bộ Firestore lifecycle events.
    /// </summary>
    private void UnsubscribeFromFirestore()
    {
        FirestorePlayerDataManager.OnManagerReady -=
            HandleFirestoreManagerReady;

        UnsubscribeFromFirestoreInstance();
    }


    // =====================================================================
    // DEFAULT EQUIPMENT
    // =====================================================================

    private void ApplyDefaultEquipment()
    {
        equippedVehicleId =
            DefaultVehicleItemId;

        equippedOutfitId =
            DefaultOutfitItemId;

        equippedAccessoryId =
            DefaultAccessoryItemId;

        equippedCosmeticId =
            DefaultCosmeticItemId;
    }


    // =====================================================================
    // PLAYER DATA LOADED
    // =====================================================================

    /// <summary>
    /// Called when FirestorePlayerDataManager provides PlayerData.
    ///
    /// Firestore là owner chính của:
    /// - Migration
    /// - Normalization
    /// - Ownership sanitation
    ///
    /// EquipmentManager chỉ:
    /// - consume canonical PlayerData
    /// - validate lần cuối
    /// - rebuild runtime state
    /// - notify visual systems
    /// </summary>
    private void HandlePlayerDataLoaded(
        PlayerData data)
    {
        if (data == null)
        {
            Debug.LogError(
                "[EquipmentManager] " +
                "PlayerData = null."
            );

            return;
        }


        /*
         * Trong lúc chuyển account / load account:
         *
         * Không để Showcase consume state nửa cũ nửa mới.
         */
        equipmentReady = false;


        // ---------------------------------------------------------------
        // LOAD + FINAL SAFETY NORMALIZATION
        // ---------------------------------------------------------------

        LoadEquipmentFromPlayerData(
            data
        );


        // ---------------------------------------------------------------
        // OWNERSHIP CACHE
        // ---------------------------------------------------------------

        InventoryOwnership.Refresh();


        // ---------------------------------------------------------------
        // INITIALIZED
        // ---------------------------------------------------------------

        initialized = true;


        // ---------------------------------------------------------------
        // RUNTIME STATS
        // ---------------------------------------------------------------

        RebuildRuntimeStats();


        // ---------------------------------------------------------------
        // READY
        // ---------------------------------------------------------------

        MarkEquipmentReady();


        // ---------------------------------------------------------------
        // VISUAL SYSTEM
        // ---------------------------------------------------------------

        NotifyAllEquipmentChanged();


        Debug.Log(
            "[EquipmentManager] " +
            "Equipment loaded → " +
            $"Vehicle={equippedVehicleId} | " +
            $"Outfit={equippedOutfitId} | " +
            $"Accessory={equippedAccessoryId} | " +
            $"Cosmetic={equippedCosmeticId}"
        );
    }


    // =====================================================================
    // PLAYER DATA CLEARED
    // =====================================================================

    /// <summary>
    /// Called when current account/player data is cleared.
    ///
    /// Reset:
    /// - Equipment
    /// - Ownership cache
    /// - Runtime stats
    ///
    /// Then notify all visual categories.
    /// </summary>
    private void HandlePlayerDataCleared()
    {
        equipmentReady = false;
        initialized = false;


        // ---------------------------------------------------------------
        // RESET EQUIPMENT
        // ---------------------------------------------------------------

        ApplyDefaultEquipment();


        // ---------------------------------------------------------------
        // OWNERSHIP
        // ---------------------------------------------------------------

        InventoryOwnership.Clear();


        // ---------------------------------------------------------------
        // STATE
        // ---------------------------------------------------------------

        initialized = true;


        // ---------------------------------------------------------------
        // RUNTIME STATS
        // ---------------------------------------------------------------

        RebuildRuntimeStats();


        // ---------------------------------------------------------------
        // READY
        // ---------------------------------------------------------------

        MarkEquipmentReady();


        // ---------------------------------------------------------------
        // VISUALS
        // ---------------------------------------------------------------

        NotifyAllEquipmentChanged();


        Debug.Log(
            "[EquipmentManager] " +
            "Account cleared. Equipment reset to defaults."
        );
    }


    // =====================================================================
    // EQUIPMENT EVENT
    // =====================================================================

    private void NotifyAllEquipmentChanged()
    {
        NotifyEquipmentChanged(
            GameItemCategory.Vehicle
        );

        NotifyEquipmentChanged(
            GameItemCategory.Outfit
        );

        NotifyEquipmentChanged(
            GameItemCategory.Accessory
        );

        NotifyEquipmentChanged(
            GameItemCategory.Cosmetic
        );
    }


    private void NotifyEquipmentChanged(
        GameItemCategory category)
    {
        OnEquipmentChanged?.Invoke(
            category
        );
    }


    // =====================================================================
    // LOAD EQUIPMENT
    // =====================================================================

    /// <summary>
    /// Load equipment từ PlayerData.
    ///
    /// Firestore đã normalize trước khi phát OnPlayerDataLoaded.
    /// Đây là final safety layer.
    ///
    /// Vehicle:
    /// - ưu tiên equippedVehicleItemId
    /// - fallback legacy selectedBike
    /// - cuối cùng default Vehicle 1
    ///
    /// Outfit:
    /// - ưu tiên equippedOutfitItemId
    /// - fallback default Outfit 41
    ///
    /// Accessory/Cosmetic:
    /// - invalid -> 0
    /// </summary>
    private void LoadEquipmentFromPlayerData(
        PlayerData data)
    {
        if (data == null)
        {
            return;
        }


        // ---------------------------------------------------------------
        // VEHICLE
        // ---------------------------------------------------------------

        int vehicleId =
            data.equippedVehicleItemId;


        if (!IsValidCategoryItem(
                vehicleId,
                GameItemCategory.Vehicle))
        {
            /*
             * Legacy migration fallback.
             *
             * Ví dụ PlayerData cũ chỉ có:
             *
             * selectedBike = "LeadNinja"
             *
             * nhưng equippedVehicleItemId = 0.
             */
            int legacyVehicleId =
                ResolveLegacyVehicle(data);


            if (IsValidCategoryItem(
                    legacyVehicleId,
                    GameItemCategory.Vehicle))
            {
                vehicleId =
                    legacyVehicleId;
            }
            else
            {
                vehicleId =
                    DefaultVehicleItemId;
            }
        }


        equippedVehicleId =
            vehicleId;


        // ---------------------------------------------------------------
        // OUTFIT
        // ---------------------------------------------------------------

        int outfitId =
            ValidateEquippedId(
                data.equippedOutfitItemId,
                GameItemCategory.Outfit
            );


        if (outfitId == 0)
        {
            outfitId =
                ResolveDefaultOutfit();
        }


        equippedOutfitId =
            outfitId;


        // ---------------------------------------------------------------
        // ACCESSORY
        // ---------------------------------------------------------------

        equippedAccessoryId =
            ValidateEquippedId(
                data.equippedAccessoryItemId,
                GameItemCategory.Accessory
            );


        // ---------------------------------------------------------------
        // COSMETIC
        // ---------------------------------------------------------------

        equippedCosmeticId =
            ValidateEquippedId(
                data.equippedCosmeticItemId,
                GameItemCategory.Cosmetic
            );
    }


    // =====================================================================
    // DEFAULT OUTFIT RESOLUTION
    // =====================================================================

    private int ResolveDefaultOutfit()
    {
        if (IsValidCategoryItem(
                DefaultOutfitItemId,
                GameItemCategory.Outfit))
        {
            return DefaultOutfitItemId;
        }


        Debug.LogError(
            "[EquipmentManager] " +
            $"Default Outfit ID {DefaultOutfitItemId} " +
            "không tồn tại hoặc không thuộc Outfit."
        );


        return 0;
    }


    // =====================================================================
    // LEGACY VEHICLE RESOLUTION
    // =====================================================================

    private int ResolveLegacyVehicle(
        PlayerData data)
    {
        if (
            data == null ||
            string.IsNullOrEmpty(data.selectedBike)
        )
        {
            return 0;
        }


        if (itemDatabase == null)
        {
            return 0;
        }


        string legacyName =
            NormalizeLegacyBikeName(
                data.selectedBike
            );


        if (string.IsNullOrEmpty(legacyName))
        {
            return 0;
        }


        IReadOnlyList<GameItemData> items =
            itemDatabase.Items;


        if (items == null)
        {
            return 0;
        }


        for (
            int i = 0;
            i < items.Count;
            i++)
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


            string itemName =
                NormalizeLegacyBikeName(
                    item.ItemName
                );


            if (
                string.Equals(
                    itemName,
                    legacyName,
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
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }


        return value
            .Replace(" ", string.Empty)
            .Replace("_", string.Empty)
            .Replace("-", string.Empty)
            .Trim();
    }


    // =====================================================================
    // EQUIP
    // =====================================================================

    /// <summary>
    /// Equip item.
    ///
    /// Ownership được kiểm tra ở đây.
    /// Không kiểm tra ownership trong quá trình load equipment.
    /// </summary>
    public bool TryEquip(
        int itemId)
    {
        if (!initialized)
        {
            Debug.LogWarning(
                "[EquipmentManager] " +
                "EquipmentManager chưa initialize."
            );

            return false;
        }


        if (!equipmentReady)
        {
            Debug.LogWarning(
                "[EquipmentManager] " +
                "Equipment state chưa READY."
            );

            return false;
        }


        if (itemDatabase == null)
        {
            return false;
        }


        // ---------------------------------------------------------------
        // ITEM LOOKUP
        // ---------------------------------------------------------------

        if (!itemDatabase.TryGet(
                itemId,
                out GameItemData item))
        {
            Debug.LogWarning(
                "[EquipmentManager] " +
                $"Không tìm thấy Item ID {itemId}."
            );

            return false;
        }


        if (item == null)
        {
            return false;
        }


        // ---------------------------------------------------------------
        // OWNERSHIP
        // ---------------------------------------------------------------

        if (!InventoryOwnership.IsOwned(itemId))
        {
            Debug.LogWarning(
                "[EquipmentManager] " +
                $"Chưa sở hữu Item ID {itemId}."
            );

            return false;
        }


        // ---------------------------------------------------------------
        // ALREADY EQUIPPED
        // ---------------------------------------------------------------

        if (
            GetEquippedItemId(item.Category) ==
            itemId
        )
        {
            return true;
        }


        // ---------------------------------------------------------------
        // SET EQUIPMENT
        // ---------------------------------------------------------------

        switch (item.Category)
        {
            case GameItemCategory.Vehicle:

                equippedVehicleId =
                    itemId;

                break;


            case GameItemCategory.Outfit:

                equippedOutfitId =
                    itemId;

                break;


            case GameItemCategory.Accessory:

                equippedAccessoryId =
                    itemId;

                break;


            case GameItemCategory.Cosmetic:

                equippedCosmeticId =
                    itemId;

                break;


            default:

                return false;
        }


        // ---------------------------------------------------------------
        // PLAYER DATA
        // ---------------------------------------------------------------

        FirestorePlayerDataManager firestore =
            FirestorePlayerDataManager.Instance;


        if (
            firestore != null &&
            firestore.HasPlayerData &&
            firestore.CurrentPlayerData != null
        )
        {
            ApplyEquipmentToPlayerData(
                firestore.CurrentPlayerData
            );

            RequestSave();
        }


        // ---------------------------------------------------------------
        // RUNTIME STATS
        // ---------------------------------------------------------------

        RebuildRuntimeStats();


        // ---------------------------------------------------------------
        // EVENTS
        // ---------------------------------------------------------------

        OnItemEquipped?.Invoke(
            item
        );

        NotifyEquipmentChanged(
            item.Category
        );


        Debug.Log(
            "[EquipmentManager] " +
            $"Equipped → {item.ItemName} ({itemId})"
        );


        return true;
    }


    // =====================================================================
    // UNEQUIP
    // =====================================================================

    /// <summary>
    /// Unequip category.
    ///
    /// Vehicle:
    ///     Không thể unequip.
    ///
    /// Outfit:
    ///     Không được về 0.
    ///     Sẽ trở về Outfit mặc định 41.
    ///
    /// Accessory:
    ///     0 = không trang bị.
    ///
    /// Cosmetic:
    ///     0 = không trang bị.
    /// </summary>
    public bool TryUnequip(
        GameItemCategory category)
    {
        if (!initialized ||
            !equipmentReady)
        {
            return false;
        }


        switch (category)
        {
            // -----------------------------------------------------------
            // VEHICLE
            // -----------------------------------------------------------

            case GameItemCategory.Vehicle:

                return false;


            // -----------------------------------------------------------
            // OUTFIT
            // -----------------------------------------------------------

            case GameItemCategory.Outfit:

                if (equippedOutfitId ==
                    DefaultOutfitItemId)
                {
                    return false;
                }

                equippedOutfitId =
                    DefaultOutfitItemId;

                break;


            // -----------------------------------------------------------
            // ACCESSORY
            // -----------------------------------------------------------

            case GameItemCategory.Accessory:

                if (equippedAccessoryId == 0)
                {
                    return false;
                }

                equippedAccessoryId = 0;

                break;


            // -----------------------------------------------------------
            // COSMETIC
            // -----------------------------------------------------------

            case GameItemCategory.Cosmetic:

                if (equippedCosmeticId == 0)
                {
                    return false;
                }

                equippedCosmeticId = 0;

                break;


            default:

                return false;
        }


        // ---------------------------------------------------------------
        // PLAYER DATA
        // ---------------------------------------------------------------

        FirestorePlayerDataManager firestore =
            FirestorePlayerDataManager.Instance;


        if (
            firestore != null &&
            firestore.HasPlayerData &&
            firestore.CurrentPlayerData != null
        )
        {
            ApplyEquipmentToPlayerData(
                firestore.CurrentPlayerData
            );

            RequestSave();
        }


        // ---------------------------------------------------------------
        // RUNTIME STATS
        // ---------------------------------------------------------------

        RebuildRuntimeStats();


        // ---------------------------------------------------------------
        // VISUAL / SYSTEM EVENT
        // ---------------------------------------------------------------

        NotifyEquipmentChanged(
            category
        );


        return true;
    }


    // =====================================================================
    // GET EQUIPPED ITEM
    // =====================================================================

    public GameItemData GetEquippedItem(
        GameItemCategory category)
    {
        int itemId =
            GetEquippedItemId(category);


        if (itemId <= 0)
        {
            return null;
        }


        if (itemDatabase == null)
        {
            return null;
        }


        if (itemDatabase.TryGet(
                itemId,
                out GameItemData item))
        {
            return item;
        }


        return null;
    }


    // =====================================================================
    // GET EQUIPPED ID
    // =====================================================================

    public int GetEquippedItemId(
        GameItemCategory category)
    {
        switch (category)
        {
            case GameItemCategory.Vehicle:

                return equippedVehicleId;


            case GameItemCategory.Outfit:

                return equippedOutfitId;


            case GameItemCategory.Accessory:

                return equippedAccessoryId;


            case GameItemCategory.Cosmetic:

                return equippedCosmeticId;


            default:

                return 0;
        }
    }


    // =====================================================================
    // TRY GET EQUIPPED ID
    // =====================================================================

    /// <summary>
    /// Safe API cho visual binder.
    ///
    /// false = Equipment state chưa READY hoặc ID invalid.
    /// </summary>
    public bool TryGetEquippedItemId(
        GameItemCategory category,
        out int itemId)
    {
        itemId = 0;

        if (!equipmentReady)
        {
            return false;
        }

        itemId =
            GetEquippedItemId(category);

        return itemId > 0;
    }


    // =====================================================================
    // VALIDATION
    // =====================================================================

    private int ValidateEquippedId(
        int itemId,
        GameItemCategory category)
    {
        if (itemId <= 0)
        {
            return 0;
        }


        if (!IsValidCategoryItem(
                itemId,
                category))
        {
            return 0;
        }


        /*
         * IMPORTANT:
         *
         * Không kiểm tra ownership khi LOAD.
         *
         * PlayerData là persisted equipment state.
         * Ownership cache có thể initialize sau EquipmentManager.
         *
         * Ownership chỉ bắt buộc khi TryEquip().
         */

        return itemId;
    }


    private bool IsValidCategoryItem(
        int itemId,
        GameItemCategory category)
    {
        if (itemId <= 0)
        {
            return false;
        }


        if (itemDatabase == null)
        {
            return false;
        }


        if (!itemDatabase.TryGet(
                itemId,
                out GameItemData item))
        {
            return false;
        }


        if (item == null)
        {
            return false;
        }


        return item.Category == category;
    }


    // =====================================================================
    // PLAYER DATA SYNC
    // =====================================================================

    private void ApplyEquipmentToPlayerData(
        PlayerData data)
    {
        if (data == null)
        {
            return;
        }


        data.equippedVehicleItemId =
            equippedVehicleId;

        data.equippedOutfitItemId =
            equippedOutfitId;

        data.equippedAccessoryItemId =
            equippedAccessoryId;

        data.equippedCosmeticItemId =
            equippedCosmeticId;


        UpdateLegacyBikeField(
            data,
            equippedVehicleId
        );
    }


    private void UpdateLegacyBikeField(
        PlayerData data,
        int vehicleId)
    {
        if (data == null)
        {
            return;
        }


        if (itemDatabase == null)
        {
            return;
        }


        if (!itemDatabase.TryGet(
                vehicleId,
                out GameItemData item))
        {
            return;
        }


        if (
            item == null ||
            item.Category != GameItemCategory.Vehicle
        )
        {
            return;
        }


        data.selectedBike =
            item.ItemName
                .Replace(" ", string.Empty);
    }


    // =====================================================================
    // SAVE
    // =====================================================================

    /// <summary>
    /// Request save theo dạng coalescing queue.
    ///
    /// Nếu đang save:
    ///     chỉ đánh dấu saveRequested.
    ///
    /// Khi save hiện tại hoàn thành:
    ///     nếu có thay đổi mới -> save tiếp.
    /// </summary>
    private async void RequestSave()
    {
        if (saveInProgress)
        {
            saveRequested = true;
            return;
        }


        FirestorePlayerDataManager firestore =
            FirestorePlayerDataManager.Instance;


        if (
            firestore == null ||
            !firestore.HasPlayerData ||
            firestore.CurrentPlayerData == null
        )
        {
            return;
        }


        saveInProgress = true;


        try
        {
            do
            {
                saveRequested = false;


                bool saved =
                    await firestore.SavePlayerDataAsync();


                if (!saved)
                {
                    Debug.LogError(
                        "[EquipmentManager] " +
                        "Không thể save Equipment."
                    );
                }

            }
            while (saveRequested);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "[EquipmentManager] " +
                $"Equipment save exception: {exception}"
            );
        }
        finally
        {
            saveInProgress = false;
        }
    }


    // =====================================================================
    // RUNTIME STATS
    // =====================================================================

    private void RebuildRuntimeStats()
    {
        if (runtimeStats == null)
        {
            return;
        }


        runtimeStats.RebuildFromBaseStats();


        ApplyItemStats(
            equippedVehicleId
        );


        ApplyItemStats(
            equippedOutfitId
        );


        ApplyItemStats(
            equippedAccessoryId
        );


        ApplyItemStats(
            equippedCosmeticId
        );


        runtimeStats.FinalizeStats();
    }


    private void ApplyItemStats(
        int itemId)
    {
        if (itemId <= 0)
        {
            return;
        }


        if (itemDatabase == null)
        {
            return;
        }


        if (!itemDatabase.TryGet(
                itemId,
                out GameItemData item))
        {
            return;
        }


        if (item == null || !item.HasStats)
        {
            return;
        }


        ItemStats stats =
            item.Stats;


        if (
            stats == null ||
            stats.modifiers == null
        )
        {
            return;
        }


        for (
            int i = 0;
            i < stats.modifiers.Count;
            i++)
        {
            ItemStatModifier modifier =
                stats.modifiers[i];


            runtimeStats.ApplyModifier(
                in modifier
            );
        }
    }


    // =====================================================================
    // FORCE REBUILD
    // =====================================================================

    public void Rebuild()
    {
        RebuildRuntimeStats();
    }


    // =====================================================================
    // DEBUG
    // =====================================================================

#if UNITY_EDITOR

    [ContextMenu("Debug Equipment")]
    private void DebugEquipment()
    {
        Debug.Log(
            "[EquipmentManager] " +
            $"Initialized={initialized} | " +
            $"Ready={equipmentReady} | " +
            $"Vehicle={equippedVehicleId} | " +
            $"Outfit={equippedOutfitId} | " +
            $"Accessory={equippedAccessoryId} | " +
            $"Cosmetic={equippedCosmeticId}"
        );


        if (runtimeStats == null)
        {
            Debug.Log(
                "[EquipmentManager] " +
                "RuntimeStats = null " +
                "(bình thường nếu đang ở Main Menu)."
            );

            return;
        }


        Debug.Log(
            "[EquipmentManager] Stats | " +
            $"MaxSpeed={runtimeStats.MaxSpeed} | " +
            $"Acceleration={runtimeStats.Acceleration} | " +
            $"Handling={runtimeStats.Handling} | " +
            $"JumpForce={runtimeStats.JumpForce} | " +
            $"MagnetRange={runtimeStats.MagnetRange} | " +
            $"ShieldDuration={runtimeStats.ShieldDuration}"
        );
    }


    [ContextMenu("DEBUG / Equip Item 38")]
    private void DebugEquipItem38()
    {
        TryEquip(38);
    }

#endif
}