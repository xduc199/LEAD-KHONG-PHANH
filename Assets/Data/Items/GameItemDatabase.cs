using System.Collections.Generic;
using System.Text;
using UnityEngine;

[CreateAssetMenu(
    fileName = "GameItemDatabase",
    menuName = "LEAD KHONG PHANH/Game Data/Item Database"
)]
public sealed class GameItemDatabase : ScriptableObject
{
    // =========================================================
    // AUTHORITATIVE DATABASE CONTRACT
    // =========================================================

    public const int MinItemId = 1;
    public const int MaxItemId = 100;
    public const int ExpectedItemCount = 100;

    // Vehicle
    public const int VehicleStartId = 1;
    public const int VehicleEndId = 40;

    // Outfit
    public const int DefaultOutfitItemId = 41;
    public const int OutfitStartId = 41;
    public const int OutfitEndId = 60;

    // Cosmetic
    public const int CosmeticStartId = 61;
    public const int CosmeticEndId = 80;

    // Accessory
    public const int AccessoryStartId = 81;
    public const int AccessoryEndId = 100;

    // =========================================================
    // SERIALIZED DATA
    // =========================================================

    [Header("Item Database")]
    [Tooltip(
        "Authoritative list of all GameItemData assets.\n" +
        "Expected IDs: 1-100."
    )]
    [SerializeField]
    private List<GameItemData> items =
        new List<GameItemData>(ExpectedItemCount);

    // =========================================================
    // RUNTIME LOOKUP
    // =========================================================

    private Dictionary<int, GameItemData> lookup;

    private int initializedItemCount = -1;

    // =========================================================
    // PUBLIC ACCESS
    // =========================================================

    public IReadOnlyList<GameItemData> Items => items;

    public int Count => items != null ? items.Count : 0;

    public bool IsInitialized => lookup != null;

    public bool IsComplete
    {
        get
        {
            return ValidateDatabaseInternal(
                false,
                false,
                out _);
        }
    }

    // =========================================================
    // INITIALIZATION
    // =========================================================

    public void Initialize()
    {
        if (items == null)
        {
            items = new List<GameItemData>(
                ExpectedItemCount);
        }

        if (lookup != null &&
            initializedItemCount == items.Count)
        {
            return;
        }

        BuildLookup();
    }

    // =========================================================
    // BUILD LOOKUP
    // =========================================================

    public void BuildLookup()
    {
        if (items == null)
        {
            items = new List<GameItemData>(
                ExpectedItemCount);
        }

        int capacity = items.Count;

        if (capacity < ExpectedItemCount)
            capacity = ExpectedItemCount;

        if (lookup == null)
        {
            lookup = new Dictionary<int, GameItemData>(
                capacity);
        }
        else
        {
            lookup.Clear();
        }

        initializedItemCount = items.Count;

        int nullCount = 0;
        int invalidIdCount = 0;
        int duplicateCount = 0;
        int categoryMismatchCount = 0;

        for (int i = 0; i < items.Count; i++)
        {
            GameItemData item = items[i];

            if (item == null)
            {
                nullCount++;
                continue;
            }

            int id = item.ItemId;

            // -------------------------------------------------
            // ID RANGE
            // -------------------------------------------------

            if (id < MinItemId || id > MaxItemId)
            {
                invalidIdCount++;

                Debug.LogError(
                    $"[GameItemDatabase] Invalid ItemId: {id}\n" +
                    $"Asset: {item.name}",
                    item);

                continue;
            }

            // -------------------------------------------------
            // CATEGORY CONTRACT
            // -------------------------------------------------

            GameItemCategory expectedCategory =
                GetExpectedCategory(id);

            if (item.Category != expectedCategory)
            {
                categoryMismatchCount++;

                Debug.LogError(
                    $"[GameItemDatabase] CATEGORY MISMATCH\n" +
                    $"ItemId: {id}\n" +
                    $"Asset: {item.name}\n" +
                    $"Current: {item.Category}\n" +
                    $"Expected: {expectedCategory}",
                    item);

                // Do NOT insert an invalid item into runtime
                // lookup. This prevents wrong-category items from
                // leaking into Inventory/Equipment systems.
                continue;
            }

            // -------------------------------------------------
            // DUPLICATE
            // -------------------------------------------------

            if (lookup.ContainsKey(id))
            {
                duplicateCount++;

                Debug.LogError(
                    $"[GameItemDatabase] DUPLICATE ITEM ID\n" +
                    $"ItemId: {id}\n" +
                    $"Asset: {item.name}",
                    item);

                continue;
            }

            // -------------------------------------------------
            // VALID ITEM
            // -------------------------------------------------

            lookup.Add(id, item);
        }

        // -----------------------------------------------------
        // REPORT MISSING IDS
        // -----------------------------------------------------

        int missingCount = 0;

        StringBuilder missingIds = null;

        for (int id = MinItemId;
             id <= MaxItemId;
             id++)
        {
            if (!lookup.ContainsKey(id))
            {
                missingCount++;

                if (missingIds == null)
                    missingIds = new StringBuilder(128);

                if (missingIds.Length > 0)
                    missingIds.Append(", ");

                missingIds.Append(id);
            }
        }

        // -----------------------------------------------------
        // SUMMARY
        // -----------------------------------------------------

        Debug.Log(
            $"[GameItemDatabase] BuildLookup completed.\n" +
            $"Serialized: {items.Count}\n" +
            $"Runtime Valid: {lookup.Count}\n" +
            $"Null: {nullCount}\n" +
            $"Invalid ID: {invalidIdCount}\n" +
            $"Duplicate: {duplicateCount}\n" +
            $"Category Mismatch: {categoryMismatchCount}\n" +
            $"Missing: {missingCount}",
            this);

        // -----------------------------------------------------
        // CRITICAL DEFAULT OUTFIT CHECK
        // -----------------------------------------------------

        if (!lookup.ContainsKey(DefaultOutfitItemId))
        {
            Debug.LogError(
                $"[GameItemDatabase] CRITICAL:\n" +
                $"Default Outfit ID {DefaultOutfitItemId} is MISSING.\n\n" +
                $"Ninja Lead cannot appear in Inventory until a " +
                $"GameItemData asset with:\n" +
                $"ItemId = 41\n" +
                $"Category = Outfit\n" +
                $"is added to this database's Items list.",
                this);
        }

        if (missingIds != null &&
            missingIds.Length > 0)
        {
            Debug.LogError(
                $"[GameItemDatabase] Missing Item IDs:\n" +
                $"{missingIds}",
                this);
        }
    }

    // =========================================================
    // TRY GET
    // =========================================================

    public bool TryGet(
        int itemId,
        out GameItemData item)
    {
        item = null;

        if (itemId < MinItemId ||
            itemId > MaxItemId)
        {
            return false;
        }

        Initialize();

        return lookup != null &&
               lookup.TryGetValue(
                   itemId,
                   out item);
    }

    // =========================================================
    // DEFAULT OUTFIT
    // =========================================================

    public bool TryGetDefaultOutfit(
        out GameItemData item)
    {
        return TryGet(
            DefaultOutfitItemId,
            out item);
    }

    public bool HasDefaultOutfit()
    {
        return TryGetDefaultOutfit(
            out GameItemData item) &&
               item != null &&
               item.ItemId == DefaultOutfitItemId &&
               item.Category == GameItemCategory.Outfit;
    }

    // =========================================================
    // CATEGORY
    // =========================================================

    public static bool IsVehicleId(int itemId)
    {
        return itemId >= VehicleStartId &&
               itemId <= VehicleEndId;
    }

    public static bool IsOutfitId(int itemId)
    {
        return itemId >= OutfitStartId &&
               itemId <= OutfitEndId;
    }

    public static bool IsCosmeticId(int itemId)
    {
        return itemId >= CosmeticStartId &&
               itemId <= CosmeticEndId;
    }

    public static bool IsAccessoryId(int itemId)
    {
        return itemId >= AccessoryStartId &&
               itemId <= AccessoryEndId;
    }

    public static GameItemCategory GetExpectedCategory(
        int itemId)
    {
        if (IsVehicleId(itemId))
            return GameItemCategory.Vehicle;

        if (IsOutfitId(itemId))
            return GameItemCategory.Outfit;

        if (IsCosmeticId(itemId))
            return GameItemCategory.Cosmetic;

        if (IsAccessoryId(itemId))
            return GameItemCategory.Accessory;

        return GameItemCategory.Vehicle;
    }

    // =========================================================
    // CATEGORY COUNT
    // =========================================================

    public int GetCategoryCount(
        GameItemCategory category)
    {
        Initialize();

        if (lookup == null)
            return 0;

        int count = 0;

        foreach (KeyValuePair<int, GameItemData> pair
                 in lookup)
        {
            GameItemData item = pair.Value;

            if (item != null &&
                item.Category == category)
            {
                count++;
            }
        }

        return count;
    }

    // =========================================================
    // FIRST ITEM
    // =========================================================

    public bool TryGetFirstItem(
        GameItemCategory category,
        out GameItemData result)
    {
        result = null;

        Initialize();

        if (lookup == null)
            return false;

        int startId;
        int endId;

        GetCategoryRange(
            category,
            out startId,
            out endId);

        for (int id = startId;
             id <= endId;
             id++)
        {
            GameItemData item;

            if (!lookup.TryGetValue(
                    id,
                    out item))
            {
                continue;
            }

            if (item == null)
                continue;

            if (item.Category != category)
                continue;

            result = item;
            return true;
        }

        return false;
    }

    // =========================================================
    // CATEGORY RANGE
    // =========================================================

    public static void GetCategoryRange(
        GameItemCategory category,
        out int startId,
        out int endId)
    {
        switch (category)
        {
            case GameItemCategory.Vehicle:
                startId = VehicleStartId;
                endId = VehicleEndId;
                return;

            case GameItemCategory.Outfit:
                startId = OutfitStartId;
                endId = OutfitEndId;
                return;

            case GameItemCategory.Cosmetic:
                startId = CosmeticStartId;
                endId = CosmeticEndId;
                return;

            case GameItemCategory.Accessory:
                startId = AccessoryStartId;
                endId = AccessoryEndId;
                return;

            default:
                startId = 0;
                endId = -1;
                return;
        }
    }

    // =========================================================
    // VALIDATE DATABASE
    // =========================================================

    public bool ValidateDatabase()
    {
        bool valid = ValidateDatabaseInternal(
            true,
            true,
            out string report);

        if (valid)
        {
            Debug.Log(
                $"[GameItemDatabase] DATABASE VALID.\n" +
                $"{report}",
                this);
        }
        else
        {
            Debug.LogError(
                $"[GameItemDatabase] DATABASE INVALID.\n" +
                $"{report}",
                this);
        }

        return valid;
    }

    private bool ValidateDatabaseInternal(
        bool logDetails,
        bool includeReport,
        out string report)
    {
        StringBuilder sb = includeReport
            ? new StringBuilder(512)
            : null;

        bool valid = true;

        if (items == null)
        {
            valid = false;

            if (sb != null)
                sb.AppendLine("Items list is NULL.");

            report = sb != null
                ? sb.ToString()
                : string.Empty;

            return false;
        }

        if (items.Count != ExpectedItemCount)
        {
            valid = false;

            if (sb != null)
            {
                sb.AppendLine(
                    $"Expected {ExpectedItemCount} items, " +
                    $"found {items.Count}.");
            }
        }

        bool[] found =
            new bool[MaxItemId + 1];

        for (int i = 0;
             i < items.Count;
             i++)
        {
            GameItemData item = items[i];

            if (item == null)
            {
                valid = false;

                if (sb != null)
                {
                    sb.AppendLine(
                        $"Null item at list index {i}.");
                }

                continue;
            }

            int id = item.ItemId;

            if (id < MinItemId ||
                id > MaxItemId)
            {
                valid = false;

                if (sb != null)
                {
                    sb.AppendLine(
                        $"Invalid ID {id}: {item.name}");
                }

                continue;
            }

            if (found[id])
            {
                valid = false;

                if (sb != null)
                {
                    sb.AppendLine(
                        $"Duplicate ID {id}: {item.name}");
                }

                continue;
            }

            found[id] = true;

            GameItemCategory expected =
                GetExpectedCategory(id);

            if (item.Category != expected)
            {
                valid = false;

                if (sb != null)
                {
                    sb.AppendLine(
                        $"Category mismatch ID {id}: " +
                        $"Current={item.Category}, " +
                        $"Expected={expected}");
                }
            }
        }

        // -----------------------------------------------------
        // CHECK ALL IDS
        // -----------------------------------------------------

        for (int id = MinItemId;
             id <= MaxItemId;
             id++)
        {
            if (!found[id])
            {
                valid = false;

                if (sb != null)
                {
                    sb.AppendLine(
                        $"Missing Item ID: {id}");
                }
            }
        }

        // -----------------------------------------------------
        // CRITICAL ID 41
        // -----------------------------------------------------

        if (!found[DefaultOutfitItemId])
        {
            valid = false;

            if (sb != null)
            {
                sb.AppendLine(
                    "CRITICAL: Default Outfit ID 41 " +
                    "(Ninja Lead) is missing.");
            }
        }
        else
        {
            GameItemData defaultOutfit =
                FindSerializedItem(
                    DefaultOutfitItemId);

            if (defaultOutfit == null ||
                defaultOutfit.Category !=
                    GameItemCategory.Outfit)
            {
                valid = false;

                if (sb != null)
                {
                    sb.AppendLine(
                        "CRITICAL: Item 41 exists but " +
                        "is not a valid Outfit.");
                }
            }
        }

        report = sb != null
            ? sb.ToString()
            : string.Empty;

        if (logDetails && !valid)
        {
            Debug.LogError(
                $"[GameItemDatabase] Validation failed.\n" +
                report,
                this);
        }

        return valid;
    }

    private GameItemData FindSerializedItem(
        int itemId)
    {
        if (items == null)
            return null;

        for (int i = 0;
             i < items.Count;
             i++)
        {
            GameItemData item = items[i];

            if (item == null)
                continue;

            if (item.ItemId == itemId)
                return item;
        }

        return null;
    }

    // =========================================================
    // REBUILD
    // =========================================================

    public void RebuildLookup()
    {
        BuildLookup();
    }

    // =========================================================
    // EDITOR TOOLS
    // =========================================================

#if UNITY_EDITOR

    [ContextMenu("Validate Database")]
    private void ContextValidateDatabase()
    {
        ValidateDatabase();
    }

    [ContextMenu("Rebuild Runtime Lookup")]
    private void ContextRebuildRuntimeLookup()
    {
        BuildLookup();
    }

    [ContextMenu("Validate Default Items")]
    private void ContextValidateDefaultItems()
    {
        GameItemData vehicle;
        GameItemData outfit;

        bool vehicleValid =
            TryGet(1, out vehicle) &&
            vehicle != null &&
            vehicle.Category ==
                GameItemCategory.Vehicle;

        bool outfitValid =
            TryGet(41, out outfit) &&
            outfit != null &&
            outfit.Category ==
                GameItemCategory.Outfit;

        if (!vehicleValid)
        {
            Debug.LogError(
                "[GameItemDatabase] Default Vehicle ID 1 " +
                "is missing or has wrong category.",
                this);
        }
        else
        {
            Debug.Log(
                $"[GameItemDatabase] Default Vehicle OK: " +
                $"{vehicle.name}",
                this);
        }

        if (!outfitValid)
        {
            Debug.LogError(
                "[GameItemDatabase] Default Outfit ID 41 " +
                "(Ninja Lead) is missing or has wrong category.",
                this);
        }
        else
        {
            Debug.Log(
                $"[GameItemDatabase] Default Outfit OK: " +
                $"{outfit.name}",
                this);
        }
    }

    [ContextMenu("Print Category Report")]
    private void ContextPrintCategoryReport()
    {
        Initialize();

        int vehicleCount =
            GetCategoryCount(
                GameItemCategory.Vehicle);

        int outfitCount =
            GetCategoryCount(
                GameItemCategory.Outfit);

        int cosmeticCount =
            GetCategoryCount(
                GameItemCategory.Cosmetic);

        int accessoryCount =
            GetCategoryCount(
                GameItemCategory.Accessory);

        Debug.Log(
            $"[GameItemDatabase] CATEGORY REPORT\n" +
            $"Vehicle   : {vehicleCount} / 40\n" +
            $"Outfit    : {outfitCount} / 20\n" +
            $"Cosmetic  : {cosmeticCount} / 20\n" +
            $"Accessory : {accessoryCount} / 20\n" +
            $"Total     : {Count} / 100",
            this);
    }

    [ContextMenu("Print Item ID Report")]
    private void ContextPrintItemIdReport()
    {
        Initialize();

        StringBuilder sb =
            new StringBuilder(1024);

        sb.AppendLine(
            "[GameItemDatabase] ITEM ID REPORT");

        for (int id = MinItemId;
             id <= MaxItemId;
             id++)
        {
            GameItemData item;

            if (lookup.TryGetValue(
                    id,
                    out item))
            {
                sb.Append(id);
                sb.Append(" -> ");
                sb.Append(item.name);
                sb.Append(" | ");
                sb.Append(item.Category);
                sb.AppendLine();
            }
            else
            {
                sb.Append(id);
                sb.AppendLine(" -> MISSING");
            }
        }

        Debug.Log(
            sb.ToString(),
            this);
    }

    [ContextMenu("Sort Items By ItemId")]
    private void ContextSortItemsByItemId()
    {
        if (items == null)
            return;

        items.Sort(
            CompareItemsById);

        UnityEditor.EditorUtility.SetDirty(this);

        BuildLookup();

        Debug.Log(
            "[GameItemDatabase] Items sorted by ItemId.",
            this);
    }

    private static int CompareItemsById(
        GameItemData a,
        GameItemData b)
    {
        if (ReferenceEquals(a, b))
            return 0;

        if (a == null)
            return 1;

        if (b == null)
            return -1;

        return a.ItemId.CompareTo(
            b.ItemId);
    }

#endif
}