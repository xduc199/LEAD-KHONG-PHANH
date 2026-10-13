using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime ownership cache.
///
/// SOURCE OF TRUTH:
///     FirestorePlayerDataManager.CurrentPlayerData.ownedItemIds
///
/// CACHE:
///     HashSet<int> ownedItems
///
/// DEFAULT OWNERSHIP:
///     Vehicle = 1
///     Outfit  = 41 (NINJA LEAD)
///
/// RESPONSIBILITIES:
/// - Đồng bộ ownership từ PlayerData.
/// - Đảm bảo default item được sở hữu.
/// - Kiểm tra ownership O(1).
/// - Thêm ownership.
/// - Xóa ownership.
/// - Refresh cache.
/// - Phát event khi ownership thay đổi.
///
/// KHÔNG chịu trách nhiệm:
/// - Equipment.
/// - UI.
/// - Item stats.
/// - Visual.
///
/// AUTHORITATIVE DEFAULT:
///     ID 1  = Default Vehicle
///     ID 41 = NINJA LEAD
///
/// IMPORTANT:
/// - ID 41 luôn phải được owned.
/// - ID 1 luôn phải được owned.
/// - Không được remove ID 1 hoặc ID 41.
/// </summary>
public static class InventoryOwnership
{
    // =====================================================================
    // ITEM RANGE
    // =====================================================================

    private const int MinItemId =
        GameItemDatabase.MinItemId;

    private const int MaxItemId =
        GameItemDatabase.MaxItemId;


    // =====================================================================
    // DEFAULT ITEMS
    // =====================================================================

    /// <summary>
    /// Default Vehicle.
    /// </summary>
    public const int DefaultVehicleItemId = 1;


    /// <summary>
    /// Default Outfit.
    ///
    /// ID 41 = NINJA LEAD.
    /// </summary>
    public const int DefaultOutfitItemId = 41;


    // =====================================================================
    // RUNTIME CACHE
    // =====================================================================

    /// <summary>
    /// Runtime ownership lookup.
    ///
    /// Average:
    ///     O(1)
    /// </summary>
    private static readonly HashSet<int> ownedItems =
        new HashSet<int>();


    private static bool initialized;


    /// <summary>
    /// Firestore manager mà cache hiện tại đang subscribe.
    /// </summary>
    private static FirestorePlayerDataManager subscribedManager;


    /// <summary>
    /// PlayerData mà cache hiện tại đang mirror.
    /// </summary>
    private static PlayerData cachedPlayerData;


    // =====================================================================
    // EVENTS
    // =====================================================================

    /// <summary>
    /// Fired whenever ownership state changes.
    ///
    /// Used by:
    /// - Inventory
    /// - Shop
    /// - Garage
    /// </summary>
    public static event Action OnOwnershipChanged;


    // =====================================================================
    // INITIALIZE
    // =====================================================================

    /// <summary>
    /// Đảm bảo ownership cache đang kết nối với
    /// FirestorePlayerDataManager hiện tại.
    /// </summary>
    private static void EnsureInitialized()
    {
        FirestorePlayerDataManager manager =
            FirestorePlayerDataManager.Instance;


        // ---------------------------------------------------------------
        // FIRESTORE CHƯA TỒN TẠI
        // ---------------------------------------------------------------

        if (manager == null)
        {
            return;
        }


        // ---------------------------------------------------------------
        // ĐÃ SYNC ĐÚNG PLAYER DATA
        // ---------------------------------------------------------------

        if (
            initialized &&
            subscribedManager == manager &&
            cachedPlayerData ==
                manager.CurrentPlayerData
        )
        {
            return;
        }


        // ---------------------------------------------------------------
        // UNSUBSCRIBE MANAGER CŨ
        // ---------------------------------------------------------------

        if (subscribedManager != null)
        {
            subscribedManager.OnPlayerDataLoaded -=
                HandlePlayerDataLoaded;

            subscribedManager.OnPlayerDataCleared -=
                HandlePlayerDataCleared;
        }


        // ---------------------------------------------------------------
        // SUBSCRIBE MANAGER MỚI
        // ---------------------------------------------------------------

        subscribedManager =
            manager;

        subscribedManager.OnPlayerDataLoaded +=
            HandlePlayerDataLoaded;

        subscribedManager.OnPlayerDataCleared +=
            HandlePlayerDataCleared;

        initialized = true;


        // ---------------------------------------------------------------
        // PLAYER DATA ĐÃ CÓ
        // ---------------------------------------------------------------

        if (manager.HasPlayerData)
        {
            HandlePlayerDataLoaded(
                manager.CurrentPlayerData
            );

            return;
        }


        // ---------------------------------------------------------------
        // CHƯA CÓ PLAYER DATA
        // ---------------------------------------------------------------

        ownedItems.Clear();

        cachedPlayerData = null;
    }


    // =====================================================================
    // PLAYER DATA LOADED
    // =====================================================================

    private static void HandlePlayerDataLoaded(
        PlayerData data)
    {
        // ---------------------------------------------------------------
        // RESET CACHE TRƯỚC
        // ---------------------------------------------------------------

        ownedItems.Clear();

        cachedPlayerData =
            data;


        // ---------------------------------------------------------------
        // NULL DATA
        // ---------------------------------------------------------------

        if (data == null)
        {
            NotifyOwnershipChanged();
            return;
        }


        // ---------------------------------------------------------------
        // INITIALIZE OWNERSHIP LIST
        // ---------------------------------------------------------------

        if (data.ownedItemIds == null)
        {
            data.ownedItemIds =
                new List<int>();
        }


        // ---------------------------------------------------------------
        // ENSURE DEFAULT OWNERSHIP FIRST
        //
        // QUAN TRỌNG:
        //
        // Trước đây:
        //
        //     Build HashSet
        //     ↓
        //     Ensure defaults
        //     ↓
        //     Add default vào HashSet
        //
        // Hiện tại:
        //
        //     Ensure PlayerData
        //     ↓
        //     Build HashSet
        //
        // PlayerData và Cache luôn đồng bộ.
        // ---------------------------------------------------------------

        bool defaultsAdded =
            EnsureDefaultOwnership(
                data
            );


        // ---------------------------------------------------------------
        // BUILD CACHE FROM AUTHORITATIVE PLAYER DATA
        // ---------------------------------------------------------------

        for (
            int i = 0;
            i < data.ownedItemIds.Count;
            i++)
        {
            int itemId =
                data.ownedItemIds[i];


            if (!IsValidItemId(itemId))
            {
                continue;
            }


            ownedItems.Add(
                itemId
            );
        }


        // ---------------------------------------------------------------
        // HARD GUARANTEE
        //
        // Kể cả trong trường hợp dữ liệu bị biến đổi bất thường
        // trong quá trình sync, default vẫn phải nằm trong cache.
        // ---------------------------------------------------------------

        ownedItems.Add(
            DefaultVehicleItemId
        );

        ownedItems.Add(
            DefaultOutfitItemId
        );


        cachedPlayerData =
            data;


        // ---------------------------------------------------------------
        // DEBUG
        // ---------------------------------------------------------------

        Debug.Log(
            "[InventoryOwnership] " +
            "Ownership synced. " +
            "UID=" +
            data.uid +
            " | OwnedCount=" +
            ownedItems.Count +
            " | " +
            $"DefaultVehicle={ownedItems.Contains(DefaultVehicleItemId)} | " +
            $"DefaultOutfit41={ownedItems.Contains(DefaultOutfitItemId)} | " +
            $"DefaultsAdded={defaultsAdded}"
        );


        // ---------------------------------------------------------------
        // SAVE DEFAULT OWNERSHIP
        // ---------------------------------------------------------------

        if (
            defaultsAdded &&
            !string.IsNullOrEmpty(data.uid)
        )
        {
            FirestorePlayerDataManager manager =
                FirestorePlayerDataManager.Instance;


            if (manager != null)
            {
                SaveOwnershipAsync(
                    manager,
                    data.uid
                );
            }
        }


        // ---------------------------------------------------------------
        // NOTIFY
        // ---------------------------------------------------------------

        NotifyOwnershipChanged();
    }


    // =====================================================================
    // ENSURE DEFAULT OWNERSHIP
    // =====================================================================

    /// <summary>
    /// Đảm bảo PlayerData luôn sở hữu:
    ///
    /// ID 1:
    ///     Default Vehicle
    ///
    /// ID 41:
    ///     NINJA LEAD
    ///
    /// Complexity:
    /// O(N)
    ///
    /// N = số owned item.
    /// </summary>
    private static bool EnsureDefaultOwnership(
        PlayerData data)
    {
        if (data == null)
        {
            return false;
        }


        if (data.ownedItemIds == null)
        {
            data.ownedItemIds =
                new List<int>();
        }


        bool changed = false;


        // ---------------------------------------------------------------
        // DEFAULT VEHICLE = 1
        // ---------------------------------------------------------------

        if (!ContainsItem(
                data.ownedItemIds,
                DefaultVehicleItemId))
        {
            data.ownedItemIds.Add(
                DefaultVehicleItemId
            );

            changed = true;


            Debug.Log(
                "[InventoryOwnership] " +
                "Default Vehicle added → ID=1"
            );
        }


        // ---------------------------------------------------------------
        // DEFAULT OUTFIT = 41
        //
        // NINJA LEAD
        // ---------------------------------------------------------------

        if (!ContainsItem(
                data.ownedItemIds,
                DefaultOutfitItemId))
        {
            data.ownedItemIds.Add(
                DefaultOutfitItemId
            );

            changed = true;


            Debug.Log(
                "[InventoryOwnership] " +
                "Default Outfit added → " +
                "ID=41 | NINJA LEAD"
            );
        }


        return changed;
    }


    // =====================================================================
    // PLAYER DATA CLEARED
    // =====================================================================

    private static void HandlePlayerDataCleared()
    {
        ownedItems.Clear();

        cachedPlayerData = null;

        initialized = false;


        NotifyOwnershipChanged();


        Debug.Log(
            "[InventoryOwnership] " +
            "Account cleared. Ownership cache cleared."
        );
    }


    // =====================================================================
    // IS OWNED
    // =====================================================================

    /// <summary>
    /// Kiểm tra ownership.
    ///
    /// Average:
    /// O(1)
    /// </summary>
    public static bool IsOwned(
        int itemId)
    {
        EnsureInitialized();


        if (!IsValidItemId(itemId))
        {
            return false;
        }


        return ownedItems.Contains(
            itemId
        );
    }


    // =====================================================================
    // DIRECT PLAYER DATA CHECK
    // =====================================================================

    /// <summary>
    /// Kiểm tra trực tiếp PlayerData.
    ///
    /// Không dùng trong hot path.
    ///
    /// Complexity:
    /// O(N)
    /// </summary>
    public static bool IsOwnedInCurrentPlayerData(
        int itemId)
    {
        if (!IsValidItemId(itemId))
        {
            return false;
        }


        FirestorePlayerDataManager manager =
            FirestorePlayerDataManager.Instance;


        if (
            manager == null ||
            !manager.HasPlayerData ||
            manager.CurrentPlayerData == null
        )
        {
            return false;
        }


        List<int> ownedIds =
            manager.CurrentPlayerData.ownedItemIds;


        if (ownedIds == null)
        {
            return false;
        }


        for (
            int i = 0;
            i < ownedIds.Count;
            i++)
        {
            if (ownedIds[i] == itemId)
            {
                return true;
            }
        }


        return false;
    }


    // =====================================================================
    // SET OWNED
    // =====================================================================

    /// <summary>
    /// Thêm item vào ownership.
    /// </summary>
    public static void SetOwned(
        int itemId)
    {
        EnsureInitialized();


        if (!IsValidItemId(itemId))
        {
            Debug.LogWarning(
                "[InventoryOwnership] " +
                "Invalid item ID: " +
                itemId
            );

            return;
        }


        FirestorePlayerDataManager manager =
            FirestorePlayerDataManager.Instance;


        if (
            manager == null ||
            !manager.HasPlayerData ||
            manager.CurrentPlayerData == null
        )
        {
            Debug.LogWarning(
                "[InventoryOwnership] " +
                "Không có account đang load. " +
                "Không thể sở hữu item " +
                itemId
            );

            return;
        }


        PlayerData data =
            manager.CurrentPlayerData;


        // ---------------------------------------------------------------
        // UID SAFETY
        // ---------------------------------------------------------------

        if (string.IsNullOrEmpty(data.uid))
        {
            Debug.LogError(
                "[InventoryOwnership] " +
                "PlayerData UID rỗng."
            );

            return;
        }


        // ---------------------------------------------------------------
        // INITIALIZE LIST
        // ---------------------------------------------------------------

        if (data.ownedItemIds == null)
        {
            data.ownedItemIds =
                new List<int>();
        }


        // ---------------------------------------------------------------
        // ALREADY OWNED
        // ---------------------------------------------------------------

        if (ContainsItem(
                data.ownedItemIds,
                itemId))
        {
            ownedItems.Add(
                itemId
            );

            cachedPlayerData =
                data;

            return;
        }


        // ---------------------------------------------------------------
        // PLAYER DATA
        // ---------------------------------------------------------------

        data.ownedItemIds.Add(
            itemId
        );


        // ---------------------------------------------------------------
        // CACHE
        // ---------------------------------------------------------------

        ownedItems.Add(
            itemId
        );


        cachedPlayerData =
            data;


        // ---------------------------------------------------------------
        // SAVE
        // ---------------------------------------------------------------

        SaveOwnershipAsync(
            manager,
            data.uid
        );


        // ---------------------------------------------------------------
        // EVENT
        // ---------------------------------------------------------------

        NotifyOwnershipChanged();


        Debug.Log(
            "[InventoryOwnership] " +
            "Item owned → " +
            itemId +
            " | UID=" +
            data.uid
        );
    }


    // =====================================================================
    // REMOVE OWNED
    // =====================================================================

    /// <summary>
    /// Remove ownership.
    ///
    /// ID 1 và ID 41 không thể remove.
    /// </summary>
    public static void RemoveOwned(
        int itemId)
    {
        EnsureInitialized();


        if (!IsValidItemId(itemId))
        {
            return;
        }


        // ---------------------------------------------------------------
        // PROTECT DEFAULT ITEMS
        // ---------------------------------------------------------------

        if (
            itemId ==
            DefaultVehicleItemId ||
            itemId ==
            DefaultOutfitItemId
        )
        {
            Debug.LogWarning(
                "[InventoryOwnership] " +
                "Không thể remove default item: " +
                itemId
            );

            return;
        }


        FirestorePlayerDataManager manager =
            FirestorePlayerDataManager.Instance;


        if (
            manager == null ||
            !manager.HasPlayerData ||
            manager.CurrentPlayerData == null
        )
        {
            Debug.LogWarning(
                "[InventoryOwnership] " +
                "Không có account đang load."
            );

            return;
        }


        PlayerData data =
            manager.CurrentPlayerData;


        // ---------------------------------------------------------------
        // REMOVE CACHE
        // ---------------------------------------------------------------

        bool removed =
            ownedItems.Remove(
                itemId
            );


        // ---------------------------------------------------------------
        // REMOVE PLAYER DATA
        // ---------------------------------------------------------------

        if (data.ownedItemIds != null)
        {
            for (
                int i =
                    data.ownedItemIds.Count - 1;
                i >= 0;
                i--)
            {
                if (
                    data.ownedItemIds[i] ==
                    itemId
                )
                {
                    data.ownedItemIds.RemoveAt(
                        i
                    );

                    removed = true;
                }
            }
        }


        if (!removed)
        {
            return;
        }


        cachedPlayerData =
            data;


        // ---------------------------------------------------------------
        // SAVE
        // ---------------------------------------------------------------

        SaveOwnershipAsync(
            manager,
            data.uid
        );


        // ---------------------------------------------------------------
        // EVENT
        // ---------------------------------------------------------------

        NotifyOwnershipChanged();


        Debug.Log(
            "[InventoryOwnership] " +
            "Item removed → " +
            itemId +
            " | UID=" +
            data.uid
        );
    }


    // =====================================================================
    // SAVE OWNERSHIP
    // =====================================================================

    /// <summary>
    /// Save ownership với UID safety.
    ///
    /// Nếu account đổi trong lúc await:
    ///     Không chấp nhận state của account cũ.
    /// </summary>
    private static async void SaveOwnershipAsync(
        FirestorePlayerDataManager manager,
        string expectedUID)
    {
        if (manager == null)
        {
            return;
        }


        if (manager.CurrentPlayerData == null)
        {
            return;
        }


        if (
            manager.CurrentPlayerData.uid !=
            expectedUID
        )
        {
            Debug.LogError(
                "[InventoryOwnership] " +
                "UID thay đổi trước khi save. " +
                "Hủy ownership save."
            );

            return;
        }


        try
        {
            bool saved =
                await manager.SavePlayerDataAsync();


            // -----------------------------------------------------------
            // ACCOUNT SAFETY
            // -----------------------------------------------------------

            if (
                manager.CurrentPlayerData == null ||
                manager.CurrentPlayerData.uid !=
                expectedUID
            )
            {
                Debug.LogWarning(
                    "[InventoryOwnership] " +
                    "Account đã thay đổi trong lúc save."
                );

                return;
            }


            // -----------------------------------------------------------
            // SAVE RESULT
            // -----------------------------------------------------------

            if (!saved)
            {
                Debug.LogError(
                    "[InventoryOwnership] " +
                    "Không thể save ownership lên Firestore."
                );

                return;
            }


            Debug.Log(
                "[InventoryOwnership] " +
                "Ownership saved → UID=" +
                expectedUID
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "[InventoryOwnership] " +
                "Ownership save exception: " +
                exception
            );
        }
    }


    // =====================================================================
    // REFRESH
    // =====================================================================

    /// <summary>
    /// Rebuild toàn bộ runtime cache từ PlayerData.
    ///
    /// Đồng thời đảm bảo:
    ///     ID 1  = owned
    ///     ID 41 = owned
    ///
    /// Complexity:
    /// O(N)
    /// </summary>
    public static void Refresh()
    {
        FirestorePlayerDataManager manager =
            FirestorePlayerDataManager.Instance;


        // ---------------------------------------------------------------
        // NO PLAYER DATA
        // ---------------------------------------------------------------

        if (
            manager == null ||
            !manager.HasPlayerData ||
            manager.CurrentPlayerData == null
        )
        {
            ownedItems.Clear();

            cachedPlayerData = null;

            initialized = false;


            NotifyOwnershipChanged();

            return;
        }


        // ---------------------------------------------------------------
        // ENSURE SUBSCRIPTION
        // ---------------------------------------------------------------

        EnsureInitialized();


        // ---------------------------------------------------------------
        // FORCE REBUILD
        // ---------------------------------------------------------------

        HandlePlayerDataLoaded(
            manager.CurrentPlayerData
        );
    }


    // =====================================================================
    // CLEAR
    // =====================================================================

    public static void Clear()
    {
        ownedItems.Clear();

        cachedPlayerData = null;

        initialized = false;


        NotifyOwnershipChanged();


        Debug.Log(
            "[InventoryOwnership] " +
            "Ownership cache manually cleared."
        );
    }


    // =====================================================================
    // DEBUG
    // =====================================================================

    public static void LogCurrentOwnership()
    {
        FirestorePlayerDataManager manager =
            FirestorePlayerDataManager.Instance;


        if (
            manager == null ||
            !manager.HasPlayerData ||
            manager.CurrentPlayerData == null
        )
        {
            Debug.Log(
                "[InventoryOwnership] " +
                "NO PLAYER DATA."
            );

            return;
        }


        PlayerData data =
            manager.CurrentPlayerData;


        if (data.ownedItemIds == null)
        {
            Debug.Log(
                "[InventoryOwnership] " +
                "ownedItemIds = NULL"
            );

            return;
        }


        string output =
            "[InventoryOwnership] " +
            "PlayerData ownedItemIds: ";


        for (
            int i = 0;
            i < data.ownedItemIds.Count;
            i++)
        {
            if (i > 0)
            {
                output += ", ";
            }


            output +=
                data.ownedItemIds[i];
        }


        Debug.Log(output);


        // ---------------------------------------------------------------
        // DEFAULT ITEM VERIFICATION
        // ---------------------------------------------------------------

        bool hasVehicleDefault =
            ContainsItem(
                data.ownedItemIds,
                DefaultVehicleItemId
            );


        bool hasNinjaLead =
            ContainsItem(
                data.ownedItemIds,
                DefaultOutfitItemId
            );


        Debug.Log(
            "[InventoryOwnership] " +
            $"Default Vehicle (1) = {hasVehicleDefault} | " +
            $"Ninja Lead (41) = {hasNinjaLead}"
        );
    }


    // =====================================================================
    // HELPERS
    // =====================================================================

    /// <summary>
    /// Linear lookup trong PlayerData list.
    ///
    /// Chỉ dùng ở synchronization/save path.
    ///
    /// Complexity:
    /// O(N)
    /// </summary>
    private static bool ContainsItem(
        List<int> list,
        int itemId)
    {
        if (list == null)
        {
            return false;
        }


        for (
            int i = 0;
            i < list.Count;
            i++)
        {
            if (list[i] == itemId)
            {
                return true;
            }
        }


        return false;
    }


    /// <summary>
    /// Kiểm tra ID có nằm trong authoritative range không.
    /// </summary>
    private static bool IsValidItemId(
        int itemId)
    {
        return
            itemId >= MinItemId &&
            itemId <= MaxItemId;
    }


    /// <summary>
    /// Notify ownership listeners.
    /// </summary>
    private static void NotifyOwnershipChanged()
    {
        Action callback =
            OnOwnershipChanged;


        if (callback != null)
        {
            callback.Invoke();
        }
    }
}