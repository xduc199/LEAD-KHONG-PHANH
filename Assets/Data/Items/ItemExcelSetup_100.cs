#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// LEAD KHONG PHANH
/// Excel -> GameItemData + GameItemDatabase + GameItemVisual + GameItemVisualDatabase
///
/// Excel contract:
/// ItemId | ItemName | Description | Category | SubCategory | Rarity
/// Price | Currency | Purchasable | IsDefault
/// MaxSpeed | Acceleration | Handling | JumpForce | MagnetRange | ShieldDuration
///
/// IMPORTANT:
/// - ID bắt buộc 1..100.
/// - Không dùng array index làm ID.
/// - Chỉ import đúng 6 runtime stat đã xác nhận.
/// - Không tạo stat "Other", "AllStats" hoặc stat legacy.
/// - Không ghi đè Prefab/Icon cũ nếu Excel không có dữ liệu đó.
/// - Visual transform thuộc GameItemVisual, không thuộc GameItemData.
/// - Import có tính idempotent: chạy lại không tạo duplicate theo ItemId.
/// </summary>
public static class ItemExcelSetup
{
    // =====================================================================
    // PATHS
    // =====================================================================

    private const string DefinitionsFolder =
        "Assets/GameData/Items/Definitions";

    private const string DatabaseFolder =
        "Assets/GameData/Items/Database";

    private const string DatabasePath =
        DatabaseFolder + "/GameItemDatabase.asset";

    private const string VisualFolder =
        "Assets/Data/Items/Visuals";

    private const string VisualDatabasePath =
        DatabaseFolder + "/GameItemVisualDatabase.asset";

    // =====================================================================
    // HARD CONTRACT
    // =====================================================================

    private const int MinItemId = 1;
    private const int MaxItemId = 100;
    private const int ExpectedItemCount = 100;

    private static readonly string[] RuntimeStatKeys =
    {
        "MaxSpeed",
        "Acceleration",
        "Handling",
        "JumpForce",
        "MagnetRange",
        "ShieldDuration"
    };

    // =====================================================================
    // MENU
    // =====================================================================

    [MenuItem("LEAD KHONG PHANH/Items/Import 100 Items From XLSX")]
    private static void ImportMenu()
    {
        string xlsxPath = EditorUtility.OpenFilePanel(
            "Chọn Excel 100 ITEM",
            "",
            "xlsx");

        if (string.IsNullOrEmpty(xlsxPath))
            return;

        try
        {
            Import(xlsxPath);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "[ItemExcelSetup] IMPORT FAILED\n" +
                exception);

            EditorUtility.DisplayDialog(
                "LEAD KHONG PHANH",
                "Import thất bại.\n\n" + exception.Message,
                "OK");
        }
    }

    // =====================================================================
    // SOURCE ROW
    // =====================================================================

    private sealed class SourceRow
    {
        public int id;
        public string name;
        public string description;
        public string category;
        public string subCategory;
        public string rarity;
        public int price;
        public string currency;
        public bool purchasable;
        public bool isDefault;

        public float maxSpeed;
        public float acceleration;
        public float handling;
        public float jumpForce;
        public float magnetRange;
        public float shieldDuration;
    }

    // =====================================================================
    // MAIN
    // =====================================================================

    private static void Import(string xlsxPath)
    {
        List<SourceRow> rows = ReadXlsx(xlsxPath);

        ValidateRows(rows);

        EnsureFolder(DefinitionsFolder);
        EnsureFolder(DatabaseFolder);
        EnsureFolder(VisualFolder);

        AssetDatabase.StartAssetEditing();

        int createdItems = 0;
        int updatedItems = 0;
        int createdVisuals = 0;
        int updatedVisuals = 0;

        try
        {
            // -------------------------------------------------------------
            // PASS 1: CREATE / UPDATE ITEM DATA
            // -------------------------------------------------------------

            for (int i = 0; i < rows.Count; i++)
            {
                SourceRow row = rows[i];

                GameItemData item = FindItemById(row.id);

                if (item == null)
                {
                    string assetPath =
                        $"{DefinitionsFolder}/Item_{row.id:000}_{SanitizeFileName(row.name)}.asset";

                    item = ScriptableObject.CreateInstance<GameItemData>();
                    AssetDatabase.CreateAsset(item, assetPath);

                    createdItems++;
                }
                else
                {
                    updatedItems++;
                }

                ApplyItemRow(item, row);

                EditorUtility.SetDirty(item);
            }

            AssetDatabase.SaveAssets();
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.Refresh();

        // -------------------------------------------------------------
        // PASS 2: DATABASE
        // -------------------------------------------------------------

        GameItemDatabase database = LoadOrCreateDatabase();

        WriteItemDatabase(database, rows);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        database.RebuildLookup();

        // -------------------------------------------------------------
        // PASS 3: VISUAL DATABASE
        // -------------------------------------------------------------

        RebuildVisualDatabase(
            rows,
            ref createdVisuals,
            ref updatedVisuals);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "[ItemExcelSetup] IMPORT COMPLETE\n" +
            $"Source       : {xlsxPath}\n" +
            $"Items        : {rows.Count}\n" +
            $"Item Created : {createdItems}\n" +
            $"Item Updated : {updatedItems}\n" +
            $"Visual New   : {createdVisuals}\n" +
            $"Visual Update: {updatedVisuals}\n" +
            $"ID Range     : {MinItemId}-{MaxItemId}\n" +
            $"Database     : {DatabasePath}\n" +
            $"Visual DB    : {VisualDatabasePath}");

        EditorUtility.DisplayDialog(
            "LEAD KHONG PHANH",
            "IMPORT 100 ITEM THÀNH CÔNG.\n\n" +
            $"Items: {rows.Count}\n" +
            $"Item mới: {createdItems}\n" +
            $"Item cập nhật: {updatedItems}\n" +
            $"Visual mới: {createdVisuals}\n" +
            $"Visual cập nhật: {updatedVisuals}\n\n" +
            "GameItemDatabase và GameItemVisualDatabase đã rebuild.",
            "OK");
    }

    // =====================================================================
    // ITEM DATA
    // =====================================================================

    private static void ApplyItemRow(
        GameItemData item,
        SourceRow row)
    {
        SerializedObject so = new SerializedObject(item);

        SetInt(so, "itemId", row.id);
        SetString(so, "itemName", row.name);
        SetString(so, "description", row.description);
        SetString(so, "subCategory", row.subCategory);

        SerializedProperty visualId =
            so.FindProperty("visualId");

        if (visualId != null)
            visualId.stringValue = $"VISUAL_ITEM_{row.id:000}";

        SerializedProperty category =
            so.FindProperty("category");

        if (category != null)
            category.enumValueIndex = MapCategory(row.category);

        SerializedProperty rarity =
            so.FindProperty("rarity");

        if (rarity != null)
            rarity.enumValueIndex = MapRarity(row.rarity);

        // -------------------------------------------------------------
        // ECONOMY
        // -------------------------------------------------------------

        SerializedProperty price =
            so.FindProperty("price");

        if (price != null)
            price.intValue = Mathf.Max(0, row.price);

        SerializedProperty currency =
            so.FindProperty("currency");

        if (currency != null)
            currency.enumValueIndex = MapCurrency(row.currency);

        SerializedProperty purchasable =
            so.FindProperty("purchasable");

        if (purchasable != null)
            purchasable.boolValue = row.purchasable;

        // -------------------------------------------------------------
        // SOURCE TEXT
        // -------------------------------------------------------------

        SetString(so, "sourceBuffText", BuildSourceStatText(row));
        SetString(so, "passiveDescription", row.description);

        // -------------------------------------------------------------
        // STATS
        // -------------------------------------------------------------

        SerializedProperty stats =
            so.FindProperty("stats");

        if (stats == null)
            throw new InvalidOperationException(
                $"GameItemData ID {row.id} thiếu field 'stats'.");

        SerializedProperty modifiers =
            stats.FindPropertyRelative("modifiers");

        if (modifiers == null)
            throw new InvalidOperationException(
                $"GameItemData ID {row.id} thiếu 'stats.modifiers'.");

        List<StatValue> statValues = BuildRuntimeStats(row);

        modifiers.arraySize = statValues.Count;

        for (int i = 0; i < statValues.Count; i++)
        {
            SerializedProperty element =
                modifiers.GetArrayElementAtIndex(i);

            SetString(
                element,
                "statKey",
                statValues[i].key);

            SetFloat(
                element,
                "value",
                statValues[i].value);

            SetBool(
                element,
                "isPercent",
                statValues[i].isPercent);

            SetString(
                element,
                "condition",
                "");
        }

        SerializedProperty hasStats =
            so.FindProperty("hasStats");

        if (hasStats != null)
            hasStats.boolValue = statValues.Count > 0;

        // -------------------------------------------------------------
        // DO NOT TOUCH
        // -------------------------------------------------------------
        //
        // icon
        // prefab
        //
        // Excel không có 2 dữ liệu này.
        // Giữ reference hiện tại để không phá project.

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // =====================================================================
    // RUNTIME STATS
    // =====================================================================

    private struct StatValue
    {
        public string key;
        public float value;
        public bool isPercent;

        public StatValue(
            string key,
            float value,
            bool isPercent)
        {
            this.key = key;
            this.value = value;
            this.isPercent = isPercent;
        }
    }

    private static List<StatValue> BuildRuntimeStats(
        SourceRow row)
    {
        List<StatValue> result =
            new List<StatValue>(3);

        // MaxSpeed = absolute
        AddIfNonZero(
            result,
            "MaxSpeed",
            row.maxSpeed,
            false);

        // Acceleration = multiplier
        AddIfNonZero(
            result,
            "Acceleration",
            row.acceleration,
            true);

        // Handling = multiplier
        AddIfNonZero(
            result,
            "Handling",
            row.handling,
            true);

        // JumpForce = absolute
        AddIfNonZero(
            result,
            "JumpForce",
            row.jumpForce,
            false);

        // MagnetRange = absolute.
        // PlayerRuntimeStats baseMagnetRange = 0,
        // therefore percentage modifier would have no effect.
        AddIfNonZero(
            result,
            "MagnetRange",
            row.magnetRange,
            false);

        // ShieldDuration = absolute
        AddIfNonZero(
            result,
            "ShieldDuration",
            row.shieldDuration,
            false);

        return result;
    }

    private static void AddIfNonZero(
        List<StatValue> result,
        string key,
        float value,
        bool isPercent)
    {
        if (Mathf.Approximately(value, 0f))
            return;

        if (float.IsNaN(value) ||
            float.IsInfinity(value))
        {
            throw new InvalidDataException(
                $"Stat {key} có giá trị NaN/Infinity.");
        }

        result.Add(
            new StatValue(
                key,
                value,
                isPercent));
    }

    private static string BuildSourceStatText(
        SourceRow row)
    {
        StringBuilder builder =
            new StringBuilder(128);

        AppendSourceStat(builder, "MaxSpeed", row.maxSpeed);
        AppendSourceStat(builder, "Acceleration", row.acceleration);
        AppendSourceStat(builder, "Handling", row.handling);
        AppendSourceStat(builder, "JumpForce", row.jumpForce);
        AppendSourceStat(builder, "MagnetRange", row.magnetRange);
        AppendSourceStat(builder, "ShieldDuration", row.shieldDuration);

        return builder.ToString();
    }

    private static void AppendSourceStat(
        StringBuilder builder,
        string key,
        float value)
    {
        if (Mathf.Approximately(value, 0f))
            return;

        if (builder.Length > 0)
            builder.Append(", ");

        builder.Append(key);
        builder.Append(": ");
        builder.Append(
            value.ToString(
                "0.##",
                CultureInfo.InvariantCulture));
    }

    // =====================================================================
    // DATABASE
    // =====================================================================

    private static GameItemDatabase LoadOrCreateDatabase()
    {
        GameItemDatabase database =
            AssetDatabase.LoadAssetAtPath<GameItemDatabase>(
                DatabasePath);

        if (database != null)
            return database;

        database =
            ScriptableObject.CreateInstance<GameItemDatabase>();

        AssetDatabase.CreateAsset(
            database,
            DatabasePath);

        EditorUtility.SetDirty(database);

        return database;
    }

    private static void WriteItemDatabase(
        GameItemDatabase database,
        List<SourceRow> rows)
    {
        SerializedObject so =
            new SerializedObject(database);

        SerializedProperty items =
            so.FindProperty("items");

        if (items == null)
            throw new InvalidOperationException(
                "GameItemDatabase thiếu field 'items'.");

        items.arraySize = rows.Count;

        for (int i = 0; i < rows.Count; i++)
        {
            GameItemData item =
                FindItemById(rows[i].id);

            if (item == null)
                throw new InvalidOperationException(
                    $"Không tìm thấy GameItemData ID {rows[i].id}.");

            items
                .GetArrayElementAtIndex(i)
                .objectReferenceValue = item;
        }

        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(database);
    }

    // =====================================================================
    // VISUAL DATABASE
    // =====================================================================

    private static void RebuildVisualDatabase(
        List<SourceRow> rows,
        ref int created,
        ref int updated)
    {
        GameItemVisualDatabase database =
            AssetDatabase.LoadAssetAtPath<GameItemVisualDatabase>(
                VisualDatabasePath);

        if (database == null)
        {
            database =
                ScriptableObject.CreateInstance<GameItemVisualDatabase>();

            AssetDatabase.CreateAsset(
                database,
                VisualDatabasePath);
        }

        SerializedObject databaseSO =
            new SerializedObject(database);

        SerializedProperty visuals =
            databaseSO.FindProperty("visuals");

        if (visuals == null)
            throw new InvalidOperationException(
                "GameItemVisualDatabase thiếu field 'visuals'.");

        visuals.arraySize = rows.Count;

        for (int i = 0; i < rows.Count; i++)
        {
            SourceRow row = rows[i];

            GameItemVisual visual =
                FindVisualByItemId(row.id);

            bool isNew = visual == null;

            if (isNew)
            {
                string path =
                    $"{VisualFolder}/GameItemVisual_{row.id:000}.asset";

                visual =
                    ScriptableObject.CreateInstance<GameItemVisual>();

                AssetDatabase.CreateAsset(
                    visual,
                    path);

                created++;
            }
            else
            {
                updated++;
            }

            ApplyVisualRow(
                visual,
                row,
                isNew);

            visuals
                .GetArrayElementAtIndex(i)
                .objectReferenceValue = visual;
        }

        databaseSO.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(database);
    }

    private static void ApplyVisualRow(
        GameItemVisual visual,
        SourceRow row,
        bool isNew)
    {
        SerializedObject so =
            new SerializedObject(visual);

        SetString(
            so,
            "visualId",
            $"VISUAL_ITEM_{row.id:000}");

        SetInt(
            so,
            "itemId",
            row.id);

        SetString(
            so,
            "characterId",
            GetCharacterId(row));

        SerializedProperty anchor =
            so.FindProperty("anchor");

        if (anchor != null)
            anchor.enumValueIndex =
                MapCategory(row.category);

        // -------------------------------------------------------------
        // PREFAB
        // -------------------------------------------------------------
        //
        // Chỉ tự lấy từ GameItemData.Prefab khi visual mới/chưa có prefab.
        // Không phá prefab visual đã được gán thủ công.

        SerializedProperty prefab =
            so.FindProperty("prefab");

        if (prefab != null &&
            prefab.objectReferenceValue == null)
        {
            GameItemData item =
                FindItemById(row.id);

            if (item != null &&
                item.Prefab != null)
            {
                prefab.objectReferenceValue =
                    item.Prefab;
            }
        }

        // -------------------------------------------------------------
        // DEFAULT VISUAL TRANSFORM
        // -------------------------------------------------------------
        //
        // Chỉ set khi visual mới.
        // Visual đã chỉnh tay sẽ được giữ nguyên.

        if (isNew)
        {
            SetVector3(
                so,
                "localPosition",
                GetDefaultPosition(row.category));

            SetVector3(
                so,
                "localRotation",
                Vector3.zero);

            SetVector3(
                so,
                "localScale",
                GetDefaultScale(row.category));
        }

        SerializedProperty enabled =
            so.FindProperty("enabled");

        if (enabled != null)
            enabled.boolValue = true;

        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(visual);
    }

    private static string GetCharacterId(
        SourceRow row)
    {
        if (row.category.Equals(
                "Vehicle",
                StringComparison.OrdinalIgnoreCase))
        {
            return "PLAYER";
        }

        return "PLAYER";
    }

    private static Vector3 GetDefaultPosition(
        string category)
    {
        int index =
            MapCategory(category);

        if (index == (int)GameItemCategory.Vehicle)
            return new Vector3(0f, 0.8f, 0f);

        if (index == (int)GameItemCategory.Outfit)
            return new Vector3(0f, 2f, 0.75f);

        return Vector3.zero;
    }

    private static Vector3 GetDefaultScale(
        string category)
    {
        int index =
            MapCategory(category);

        if (index == (int)GameItemCategory.Vehicle ||
            index == (int)GameItemCategory.Outfit)
        {
            return new Vector3(0.6f, 0.6f, 0.6f);
        }

        return Vector3.one;
    }

    // =====================================================================
    // FIND EXISTING ASSETS BY STABLE ID
    // =====================================================================

    private static GameItemData FindItemById(
        int itemId)
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "t:GameItemData",
                new[] { DefinitionsFolder });

        for (int i = 0; i < guids.Length; i++)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guids[i]);

            GameItemData item =
                AssetDatabase.LoadAssetAtPath<GameItemData>(
                    path);

            if (item != null &&
                item.ItemId == itemId)
            {
                return item;
            }
        }

        return null;
    }

    private static GameItemVisual FindVisualByItemId(
        int itemId)
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "t:GameItemVisual",
                new[] { VisualFolder });

        for (int i = 0; i < guids.Length; i++)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guids[i]);

            GameItemVisual visual =
                AssetDatabase.LoadAssetAtPath<GameItemVisual>(
                    path);

            if (visual != null &&
                visual.ItemId == itemId)
            {
                return visual;
            }
        }

        return null;
    }

    // =====================================================================
    // MAPPING
    // =====================================================================

    private static int MapCategory(
        string value)
    {
        string v =
            Normalize(value);

        if (v == "vehicle" ||
            v == "xe")
        {
            return (int)GameItemCategory.Vehicle;
        }

        if (v == "outfit" ||
            v == "trang phuc")
        {
            return (int)GameItemCategory.Outfit;
        }

        if (v == "cosmetic" ||
            v == "my pham")
        {
            return (int)GameItemCategory.Cosmetic;
        }

        if (v == "accessory" ||
            v == "phu kien")
        {
            return (int)GameItemCategory.Accessory;
        }

        throw new InvalidDataException(
            $"Category không hợp lệ: '{value}'.");
    }

    private static int MapRarity(
        string value)
    {
        string v =
            Normalize(value);

        if (v == "common")
            return (int)GameItemRarity.Common;

        if (v == "uncommon")
            return (int)GameItemRarity.Uncommon;

        if (v == "rare")
            return (int)GameItemRarity.Rare;

        if (v == "epic")
            return (int)GameItemRarity.Epic;

        if (v == "legendary")
            return (int)GameItemRarity.Legendary;

        if (v == "limited")
            return (int)GameItemRarity.Limited;

        throw new InvalidDataException(
            $"Rarity không hợp lệ: '{value}'.");
    }

    private static int MapCurrency(
        string value)
    {
        string v =
            Normalize(value);

        if (v == "coin" ||
            v == "gold")
        {
            return (int)GameItemCurrency.Gold;
        }

        if (v == "gem")
            return (int)GameItemCurrency.Gem;

        if (v == "none" ||
            v == "")
        {
            return (int)GameItemCurrency.Gold;
        }

        throw new InvalidDataException(
            $"Currency không hợp lệ: '{value}'.");
    }

    private static string Normalize(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        string normalized =
            value.Trim()
                .ToLowerInvariant()
                .Normalize(NormalizationForm.FormD);

        StringBuilder builder =
            new StringBuilder(normalized.Length);

        for (int i = 0; i < normalized.Length; i++)
        {
            char c = normalized[i];

            if (CharUnicodeInfo.GetUnicodeCategory(c) ==
                UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(c);
        }

        return builder
            .ToString()
            .Normalize(NormalizationForm.FormC);
    }

    // =====================================================================
    // SERIALIZED HELPERS
    // =====================================================================

    private static void SetInt(
        SerializedObject so,
        string propertyName,
        int value)
    {
        SerializedProperty property =
            so.FindProperty(propertyName);

        if (property != null)
            property.intValue = value;
    }

    private static void SetString(
        SerializedObject so,
        string propertyName,
        string value)
    {
        SerializedProperty property =
            so.FindProperty(propertyName);

        if (property != null)
            property.stringValue = value ?? "";
    }

    private static void SetFloat(
        SerializedProperty property,
        string propertyName,
        float value)
    {
        SerializedProperty child =
            property.FindPropertyRelative(propertyName);

        if (child != null)
            child.floatValue = value;
    }

    private static void SetBool(
        SerializedProperty property,
        string propertyName,
        bool value)
    {
        SerializedProperty child =
            property.FindPropertyRelative(propertyName);

        if (child != null)
            child.boolValue = value;
    }

    private static void SetString(
        SerializedProperty property,
        string propertyName,
        string value)
    {
        SerializedProperty child =
            property.FindPropertyRelative(propertyName);

        if (child != null)
            child.stringValue = value ?? "";
    }

    private static void SetVector3(
        SerializedObject so,
        string propertyName,
        Vector3 value)
    {
        SerializedProperty property =
            so.FindProperty(propertyName);

        if (property != null)
            property.vector3Value = value;
    }

    // =====================================================================
    // VALIDATION
    // =====================================================================

    private static void ValidateRows(
        List<SourceRow> rows)
    {
        if (rows == null ||
            rows.Count != ExpectedItemCount)
        {
            throw new InvalidDataException(
                $"Excel phải có đúng {ExpectedItemCount} item. " +
                $"Hiện tại: {(rows == null ? 0 : rows.Count)}.");
        }

        HashSet<int> ids =
            new HashSet<int>();

        for (int i = 0; i < rows.Count; i++)
        {
            SourceRow row = rows[i];

            if (row.id < MinItemId ||
                row.id > MaxItemId)
            {
                throw new InvalidDataException(
                    $"Item ID ngoài range {MinItemId}-{MaxItemId}: {row.id}");
            }

            if (!ids.Add(row.id))
            {
                throw new InvalidDataException(
                    $"Duplicate Item ID: {row.id}");
            }

            if (string.IsNullOrWhiteSpace(row.name))
            {
                throw new InvalidDataException(
                    $"Item ID {row.id} thiếu ItemName.");
            }

            MapCategory(row.category);
            MapRarity(row.rarity);
            MapCurrency(row.currency);

            ValidateFloat(row.id, "MaxSpeed", row.maxSpeed);
            ValidateFloat(row.id, "Acceleration", row.acceleration);
            ValidateFloat(row.id, "Handling", row.handling);
            ValidateFloat(row.id, "JumpForce", row.jumpForce);
            ValidateFloat(row.id, "MagnetRange", row.magnetRange);
            ValidateFloat(row.id, "ShieldDuration", row.shieldDuration);
        }

        for (int id = MinItemId; id <= MaxItemId; id++)
        {
            if (!ids.Contains(id))
            {
                throw new InvalidDataException(
                    $"Excel thiếu Item ID {id}.");
            }
        }

        int defaultVehicle = 0;
        int defaultOutfit = 0;

        for (int i = 0; i < rows.Count; i++)
        {
            if (!rows[i].isDefault)
                continue;

            int category =
                MapCategory(rows[i].category);

            if (category == (int)GameItemCategory.Vehicle)
                defaultVehicle++;

            if (category == (int)GameItemCategory.Outfit)
                defaultOutfit++;
        }

        if (defaultVehicle != 1)
        {
            throw new InvalidDataException(
                $"Excel phải có đúng 1 default Vehicle. Hiện tại: {defaultVehicle}.");
        }

        if (defaultOutfit != 1)
        {
            throw new InvalidDataException(
                $"Excel phải có đúng 1 default Outfit. Hiện tại: {defaultOutfit}.");
        }
    }

    private static void ValidateFloat(
        int itemId,
        string key,
        float value)
    {
        if (float.IsNaN(value) ||
            float.IsInfinity(value))
        {
            throw new InvalidDataException(
                $"Item {itemId}: {key} = NaN/Infinity.");
        }

        if (value < 0f)
        {
            throw new InvalidDataException(
                $"Item {itemId}: {key} không được âm.");
        }
    }

    // =====================================================================
    // XLSX READER
    // =====================================================================

    private static List<SourceRow> ReadXlsx(
        string path)
    {
        using (FileStream stream =
               File.OpenRead(path))
        using (ZipArchive archive =
               new ZipArchive(
                   stream,
                   ZipArchiveMode.Read,
                   false))
        {
            List<string> sharedStrings =
                ReadSharedStrings(archive);

            XDocument workbook =
                ReadXml(
                    archive,
                    "xl/workbook.xml");

            XDocument relationships =
                ReadXml(
                    archive,
                    "xl/_rels/workbook.xml.rels");

            XNamespace main =
                "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

            XNamespace rel =
                "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

            XNamespace packageRel =
                "http://schemas.openxmlformats.org/package/2006/relationships";

            XElement firstSheet =
                workbook.Root?
                    .Element(main + "sheets")?
                    .Elements(main + "sheet")
                    .FirstOrDefault();

            if (firstSheet == null)
                throw new InvalidDataException(
                    "Không tìm thấy worksheet đầu tiên.");

            string relationshipId =
                (string)firstSheet.Attribute(rel + "id");

            XElement matchingRelationship =
                relationships.Root?
                    .Elements(packageRel + "Relationship")
                    .FirstOrDefault(
                        x =>
                            string.Equals(
                                (string)x.Attribute("Id"),
                                relationshipId,
                                StringComparison.Ordinal));

            if (matchingRelationship == null)
                throw new InvalidDataException(
                    "Không resolve được worksheet relationship.");

            string target =
                (string)matchingRelationship.Attribute("Target");

            if (string.IsNullOrEmpty(target))
                throw new InvalidDataException(
                    "Worksheet Target rỗng.");

            if (target.StartsWith("/"))
                target = target.TrimStart('/');

            if (!target.StartsWith(
                    "xl/",
                    StringComparison.OrdinalIgnoreCase))
            {
                target = "xl/" + target;
            }

            XDocument sheet =
                ReadXml(archive, target);

            XElement sheetData =
                sheet.Root?.Element(main + "sheetData");

            if (sheetData == null)
                throw new InvalidDataException(
                    "Worksheet thiếu sheetData.");

            List<SourceRow> rows =
                new List<SourceRow>(ExpectedItemCount);

            Dictionary<string, int> headers =
                null;

            foreach (XElement rowElement in
                     sheetData.Elements(main + "row"))
            {
                Dictionary<int, string> cells =
                    ReadRowCells(
                        rowElement,
                        main,
                        sharedStrings);

                if (headers == null)
                {
                    headers =
                        BuildHeaderMap(cells);

                    ValidateHeaders(headers);
                    continue;
                }

                string idText =
                    GetCellByHeader(
                        cells,
                        headers,
                        "ItemId");

                if (string.IsNullOrWhiteSpace(idText))
                    continue;

                int id;

                if (!int.TryParse(
                        idText,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out id))
                {
                    throw new InvalidDataException(
                        $"ItemId không hợp lệ: '{idText}'.");
                }

                SourceRow row =
                    new SourceRow();

                row.id = id;
                row.name = GetCellByHeader(cells, headers, "ItemName");
                row.description = GetCellByHeader(cells, headers, "Description");
                row.category = GetCellByHeader(cells, headers, "Category");
                row.subCategory = GetCellByHeader(cells, headers, "SubCategory");
                row.rarity = GetCellByHeader(cells, headers, "Rarity");
                row.price = ParseInt(GetCellByHeader(cells, headers, "Price"), id, "Price");
                row.currency = GetCellByHeader(cells, headers, "Currency");
                row.purchasable = ParseBool(GetCellByHeader(cells, headers, "Purchasable"));
                row.isDefault = ParseBool(GetCellByHeader(cells, headers, "IsDefault"));

                row.maxSpeed = ParseFloat(GetCellByHeader(cells, headers, "MaxSpeed"), id, "MaxSpeed");
                row.acceleration = ParseFloat(GetCellByHeader(cells, headers, "Acceleration"), id, "Acceleration");
                row.handling = ParseFloat(GetCellByHeader(cells, headers, "Handling"), id, "Handling");
                row.jumpForce = ParseFloat(GetCellByHeader(cells, headers, "JumpForce"), id, "JumpForce");
                row.magnetRange = ParseFloat(GetCellByHeader(cells, headers, "MagnetRange"), id, "MagnetRange");
                row.shieldDuration = ParseFloat(GetCellByHeader(cells, headers, "ShieldDuration"), id, "ShieldDuration");

                rows.Add(row);
            }

            return rows;
        }
    }

    private static Dictionary<string, int> BuildHeaderMap(
        Dictionary<int, string> cells)
    {
        Dictionary<string, int> result =
            new Dictionary<string, int>(
                StringComparer.OrdinalIgnoreCase);

        foreach (KeyValuePair<int, string> pair in cells)
        {
            string header =
                pair.Value.Trim();

            if (!string.IsNullOrEmpty(header))
                result[header] = pair.Key;
        }

        return result;
    }

    private static void ValidateHeaders(
        Dictionary<string, int> headers)
    {
        string[] required =
        {
            "ItemId",
            "ItemName",
            "Description",
            "Category",
            "SubCategory",
            "Rarity",
            "Price",
            "Currency",
            "Purchasable",
            "IsDefault",
            "MaxSpeed",
            "Acceleration",
            "Handling",
            "JumpForce",
            "MagnetRange",
            "ShieldDuration"
        };

        for (int i = 0; i < required.Length; i++)
        {
            if (!headers.ContainsKey(required[i]))
            {
                throw new InvalidDataException(
                    $"Excel thiếu column bắt buộc: {required[i]}.");
            }
        }
    }

    private static string GetCellByHeader(
        Dictionary<int, string> cells,
        Dictionary<string, int> headers,
        string header)
    {
        if (!headers.TryGetValue(
                header,
                out int column))
        {
            return "";
        }

        return GetCell(cells, column);
    }

    private static int ParseInt(
        string value,
        int itemId,
        string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;

        if (int.TryParse(
                value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int result))
        {
            return result;
        }

        if (float.TryParse(
                value.Replace(",", "."),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float floatValue))
        {
            return Mathf.RoundToInt(floatValue);
        }

        throw new InvalidDataException(
            $"Item {itemId}: {field} không hợp lệ: '{value}'.");
    }

    private static float ParseFloat(
        string value,
        int itemId,
        string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0f;

        value =
            value.Trim()
                .Replace(",", ".");

        if (float.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float result))
        {
            return result;
        }

        throw new InvalidDataException(
            $"Item {itemId}: {field} không hợp lệ: '{value}'.");
    }

    private static bool ParseBool(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        string normalized =
            Normalize(value);

        return normalized == "true" ||
               normalized == "1" ||
               normalized == "yes" ||
               normalized == "co" ||
               normalized == "x";
    }

    // =====================================================================
    // XLSX LOW-LEVEL
    // =====================================================================

    private static Dictionary<int, string> ReadRowCells(
        XElement rowElement,
        XNamespace main,
        List<string> sharedStrings)
    {
        Dictionary<int, string> result =
            new Dictionary<int, string>();

        foreach (XElement cell in
                 rowElement.Elements(main + "c"))
        {
            string reference =
                (string)cell.Attribute("r");

            int column =
                ColumnIndexFromReference(reference);

            string type =
                (string)cell.Attribute("t");

            XElement valueElement =
                cell.Element(main + "v");

            string value =
                valueElement != null
                    ? valueElement.Value
                    : "";

            if (type == "s")
            {
                if (int.TryParse(
                        value,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out int sharedIndex) &&
                    sharedIndex >= 0 &&
                    sharedIndex < sharedStrings.Count)
                {
                    value = sharedStrings[sharedIndex];
                }
            }
            else if (type == "inlineStr")
            {
                XElement text =
                    cell.Element(main + "is")?
                        .Element(main + "t");

                value =
                    text != null
                        ? text.Value
                        : "";
            }

            result[column] = value;
        }

        return result;
    }

    private static List<string> ReadSharedStrings(
        ZipArchive archive)
    {
        List<string> result =
            new List<string>();

        ZipArchiveEntry entry =
            archive.GetEntry(
                "xl/sharedStrings.xml");

        if (entry == null)
            return result;

        using Stream stream = entry.Open();

        XDocument document =
            XDocument.Load(stream);

        XNamespace main =
            "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        IEnumerable<XElement> items =
            document.Root?.Elements(main + "si");

        if (items == null)
            return result;

        foreach (XElement item in items)
        {
            StringBuilder builder =
                new StringBuilder();

            foreach (XElement text in
                     item.Descendants(main + "t"))
            {
                builder.Append(text.Value);
            }

            result.Add(builder.ToString());
        }

        return result;
    }

    private static XDocument ReadXml(
        ZipArchive archive,
        string path)
    {
        ZipArchiveEntry entry =
            archive.GetEntry(path);

        if (entry == null)
        {
            throw new InvalidDataException(
                $"Không tìm thấy XLSX entry: {path}");
        }

        using Stream stream =
            entry.Open();

        return XDocument.Load(stream);
    }

    private static string GetCell(
        Dictionary<int, string> cells,
        int index)
    {
        if (cells.TryGetValue(
                index,
                out string value))
        {
            return value.Trim();
        }

        return "";
    }

    private static int ColumnIndexFromReference(
        string reference)
    {
        if (string.IsNullOrEmpty(reference))
            return 0;

        int result = 0;

        for (int i = 0; i < reference.Length; i++)
        {
            char c = reference[i];

            if (!char.IsLetter(c))
                break;

            result =
                result * 26 +
                char.ToUpperInvariant(c) -
                'A' +
                1;
        }

        return result - 1;
    }

    // =====================================================================
    // FOLDER / FILE NAME
    // =====================================================================

    private static void EnsureFolder(
        string folder)
    {
        string[] parts =
            folder.Split('/');

        if (parts.Length == 0)
            return;

        string current =
            parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next =
                current + "/" + parts[i];

            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(
                    current,
                    parts[i]);
            }

            current = next;
        }
    }

    private static string SanitizeFileName(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Unnamed";

        StringBuilder builder =
            new StringBuilder(value.Length);

        char[] invalid =
            Path.GetInvalidFileNameChars();

        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            bool invalidChar = false;

            for (int j = 0; j < invalid.Length; j++)
            {
                if (c == invalid[j])
                {
                    invalidChar = true;
                    break;
                }
            }

            if (invalidChar ||
                c == '/' ||
                c == '\\')
            {
                builder.Append('_');
            }
            else
            {
                builder.Append(c);
            }
        }

        return builder
            .ToString()
            .Trim()
            .Replace(" ", "_");
    }
}

#endif
