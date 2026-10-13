#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ItemSystemSetup
{
    //==============================================================
    // CONTRACT
    //==============================================================

    private const int MinItemId = GameItemDatabase.MinItemId;
    private const int MaxItemId = GameItemDatabase.MaxItemId;

    private const string DataRoot = "Assets/GameData/Items";
    private const string DatabaseFolder = DataRoot + "/Database";
    private const string DefinitionsFolder = DataRoot + "/Definitions";
    private const string VisualFolder = DataRoot + "/Visuals";

    private const string ItemDatabasePath =
        DatabaseFolder + "/GameItemDatabase.asset";

    private const string VisualDatabasePath =
        DatabaseFolder + "/GameItemVisualDatabase.asset";

    private const string VisualFilePrefix = "Visual_";

    //==============================================================
    // RUNTIME / EDITOR LOOKUPS
    //==============================================================

    private static readonly Dictionary<int, GameItemData> ItemsById =
        new Dictionary<int, GameItemData>(MaxItemId);

    private static readonly Dictionary<int, GameItemVisual> VisualsByItemId =
        new Dictionary<int, GameItemVisual>(MaxItemId);

    //==============================================================
    // REPORT
    //==============================================================

    private static int _errors;
    private static int _warnings;
    private static int _missingPrefabs;
    private static int _missingVisuals;

    //==============================================================
    // FULL SETUP
    //==============================================================

    [MenuItem(
        "LEAD KHONG PHANH/Items/Full Item System Setup",
        priority = 10)]
    public static void FullSetup()
    {
        ResetReport();

        Debug.Log(
            "[ItemSystemSetup] ===== FULL SETUP START =====");

        EnsureFolder(DataRoot);
        EnsureFolder(DefinitionsFolder);
        EnsureFolder(DatabaseFolder);
        EnsureFolder(VisualFolder);

        //----------------------------------------------------------
        // 1. SCAN ITEM DEFINITIONS
        //----------------------------------------------------------

        bool itemScanOk = ScanItemData();

        if (!itemScanOk)
        {
            Debug.LogError(
                "[ItemSystemSetup] ItemData scan failed. " +
                "Build aborted.");

            PrintReport();
            return;
        }

        //----------------------------------------------------------
        // 2. REPAIR DATABASE SCRIPT REFERENCES
        //----------------------------------------------------------

        RepairMissingDatabaseScript(
            ItemDatabasePath,
            typeof(GameItemDatabase));

        RepairMissingDatabaseScript(
            VisualDatabasePath,
            typeof(GameItemVisualDatabase));

        //----------------------------------------------------------
        // 3. LOAD / CREATE DATABASES
        //----------------------------------------------------------

        GameItemDatabase itemDatabase =
            LoadOrCreateItemDatabase();

        GameItemVisualDatabase visualDatabase =
            LoadOrCreateVisualDatabase();

        if (itemDatabase == null ||
            visualDatabase == null)
        {
            Error(
                "Cannot create/load Item or Visual database.");

            PrintReport();
            return;
        }

        //----------------------------------------------------------
        // 4. BUILD VISUAL DEFINITIONS
        //----------------------------------------------------------

        BuildVisualDefinitions(
            visualDatabase);

        //----------------------------------------------------------
        // 5. BUILD ITEM DATABASE
        //----------------------------------------------------------

        BuildItemDatabase(
            itemDatabase);

        //----------------------------------------------------------
        // 6. SAVE
        //----------------------------------------------------------

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        //----------------------------------------------------------
        // 7. ASSIGN DATABASE REFERENCES TO OPEN SCENES
        //----------------------------------------------------------

        AssignDatabasesToOpenScenes(
            itemDatabase,
            visualDatabase);

        EditorSceneManager.MarkAllScenesDirty();

        AssetDatabase.SaveAssets();

        //----------------------------------------------------------
        // COMPLETE
        //----------------------------------------------------------

        Debug.Log(
            "[ItemSystemSetup] ===== FULL SETUP COMPLETE =====");

        PrintReport();

        Selection.activeObject = itemDatabase;
        EditorGUIUtility.PingObject(itemDatabase);
    }

    //==============================================================
    // VALIDATE
    //==============================================================

    [MenuItem(
        "LEAD KHONG PHANH/Items/Validate Item System",
        priority = 11)]
    public static void Validate()
    {
        ResetReport();

        Debug.Log(
            "[ItemSystemSetup] ===== VALIDATION START =====");

        //----------------------------------------------------------
        // ITEM DEFINITIONS
        //----------------------------------------------------------

        bool itemScanOk = ScanItemData();

        if (!itemScanOk)
        {
            Debug.LogError(
                "[ItemSystemSetup] Item definition scan failed.");
        }

        //----------------------------------------------------------
        // LOAD DATABASES
        //----------------------------------------------------------

        GameItemDatabase itemDatabase =
            AssetDatabase.LoadAssetAtPath<GameItemDatabase>(
                ItemDatabasePath);

        GameItemVisualDatabase visualDatabase =
            AssetDatabase.LoadAssetAtPath<GameItemVisualDatabase>(
                VisualDatabasePath);

        if (itemDatabase == null)
        {
            Error(
                "GameItemDatabase is missing: " +
                ItemDatabasePath);
        }

        if (visualDatabase == null)
        {
            Error(
                "GameItemVisualDatabase is missing: " +
                VisualDatabasePath);
        }

        //----------------------------------------------------------
        // VALIDATE ITEM DATABASE
        //----------------------------------------------------------

        if (itemDatabase != null)
        {
            ValidateItemDatabase(
                itemDatabase);
        }

        //----------------------------------------------------------
        // VALIDATE VISUAL DATABASE
        //----------------------------------------------------------

        if (visualDatabase != null)
        {
            ValidateVisualDatabase(
                visualDatabase);
        }

        //----------------------------------------------------------
        // CROSS DATABASE VALIDATION
        //----------------------------------------------------------

        if (itemDatabase != null &&
            visualDatabase != null)
        {
            ValidateItemVisualContract(
                itemDatabase,
                visualDatabase);
        }

        //----------------------------------------------------------
        // COMPLETE
        //----------------------------------------------------------

        Debug.Log(
            "[ItemSystemSetup] ===== VALIDATION COMPLETE =====");

        PrintReport();
    }

    //==============================================================
    // REPAIR DATABASE SCRIPTS
    //==============================================================

    [MenuItem(
        "LEAD KHONG PHANH/Items/Repair Missing Database Scripts",
        priority = 12)]
    public static void RepairDatabases()
    {
        ResetReport();

        EnsureFolder(DatabaseFolder);

        bool repairedItem =
            RepairMissingDatabaseScript(
                ItemDatabasePath,
                typeof(GameItemDatabase));

        bool repairedVisual =
            RepairMissingDatabaseScript(
                VisualDatabasePath,
                typeof(GameItemVisualDatabase));

        AssetDatabase.Refresh();

        Debug.Log(
            "[ItemSystemSetup] Repair finished. " +
            "GameItemDatabase=" +
            repairedItem +
            " | GameItemVisualDatabase=" +
            repairedVisual);

        PrintReport();
    }

    //==============================================================
    // RESET
    //==============================================================

    private static void ResetReport()
    {
        _errors = 0;
        _warnings = 0;
        _missingPrefabs = 0;
        _missingVisuals = 0;

        ItemsById.Clear();
        VisualsByItemId.Clear();
    }

    //==============================================================
    // SCAN GAME ITEM DATA
    //==============================================================

    private static bool ScanItemData()
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "t:GameItemData");

        if (guids == null ||
            guids.Length == 0)
        {
            Error(
                "No GameItemData assets found.");

            return false;
        }

        for (int i = 0; i < guids.Length; i++)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guids[i]);

            GameItemData item =
                AssetDatabase.LoadAssetAtPath<GameItemData>(
                    path);

            if (item == null)
            {
                Warning(
                    "Could not load GameItemData: " +
                    path);

                continue;
            }

            int id = item.ItemId;

            //------------------------------------------------------
            // RANGE
            //------------------------------------------------------

            if (id < MinItemId ||
                id > MaxItemId)
            {
                Error(
                    "Item outside allowed range " +
                    MinItemId +
                    "-" +
                    MaxItemId +
                    ": ID=" +
                    id +
                    " | " +
                    path);

                continue;
            }

            //------------------------------------------------------
            // DUPLICATE
            //------------------------------------------------------

            if (ItemsById.ContainsKey(id))
            {
                Error(
                    "Duplicate ItemId=" +
                    id +
                    " | Existing=" +
                    AssetDatabase.GetAssetPath(
                        ItemsById[id]) +
                    " | Duplicate=" +
                    path);

                continue;
            }

            ItemsById.Add(
                id,
                item);
        }

        Debug.Log(
            "[ItemSystemSetup] GameItemData found=" +
            guids.Length +
            " | Valid IDs=" +
            ItemsById.Count);

        //----------------------------------------------------------
        // EXACT 1–100 CONTRACT
        //----------------------------------------------------------

        if (ItemsById.Count != MaxItemId)
        {
            Error(
                "Expected exactly " +
                MaxItemId +
                " valid items for ID range " +
                MinItemId +
                "-" +
                MaxItemId +
                ", but found " +
                ItemsById.Count +
                ".");
        }

        //----------------------------------------------------------
        // CHECK EVERY ID
        //----------------------------------------------------------

        for (int id = MinItemId;
             id <= MaxItemId;
             id++)
        {
            if (!ItemsById.ContainsKey(id))
            {
                Error(
                    "Missing GameItemData for ItemId=" +
                    id);
            }
        }

        return ItemsById.Count == MaxItemId;
    }

    //==============================================================
    // BUILD ITEM DATABASE
    //==============================================================

    private static void BuildItemDatabase(
        GameItemDatabase database)
    {
        SerializedObject so =
            new SerializedObject(database);

        SerializedProperty items =
            so.FindProperty("items");

        if (items == null)
        {
            Error(
                "GameItemDatabase.items serialized field " +
                "not found.");

            return;
        }

        List<int> ids =
            new List<int>(
                ItemsById.Keys);

        ids.Sort();

        items.arraySize =
            ids.Count;

        for (int i = 0;
             i < ids.Count;
             i++)
        {
            items.GetArrayElementAtIndex(i)
                .objectReferenceValue =
                ItemsById[ids[i]];
        }

        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(
            database);

        Debug.Log(
            "[ItemSystemSetup] GameItemDatabase rebuilt with " +
            ids.Count +
            " items.");
    }

    //==============================================================
    // BUILD VISUAL DEFINITIONS
    //==============================================================

    private static void BuildVisualDefinitions(
        GameItemVisualDatabase database)
    {
        VisualsByItemId.Clear();

        string[] visualGuids =
            AssetDatabase.FindAssets(
                "t:GameItemVisual");

        //----------------------------------------------------------
        // DISCOVER EXISTING VISUALS
        //----------------------------------------------------------

        for (int i = 0;
             i < visualGuids.Length;
             i++)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    visualGuids[i]);

            GameItemVisual visual =
                AssetDatabase.LoadAssetAtPath<GameItemVisual>(
                    path);

            if (visual == null)
            {
                continue;
            }

            int itemId =
                visual.ItemId;

            //------------------------------------------------------
            // OUTSIDE RANGE
            //------------------------------------------------------

            if (itemId < MinItemId ||
                itemId > MaxItemId)
            {
                Warning(
                    "Ignoring GameItemVisual outside " +
                    "allowed range: ItemId=" +
                    itemId +
                    " | " +
                    path);

                continue;
            }

            //------------------------------------------------------
            // DUPLICATE
            //------------------------------------------------------

            if (VisualsByItemId.ContainsKey(itemId))
            {
                Error(
                    "Duplicate GameItemVisual for ItemId=" +
                    itemId +
                    " | Existing=" +
                    AssetDatabase.GetAssetPath(
                        VisualsByItemId[itemId]) +
                    " | Duplicate=" +
                    path);

                continue;
            }

            VisualsByItemId.Add(
                itemId,
                visual);
        }

        //----------------------------------------------------------
        // CREATE / UPDATE EXACT 1:1 VISUALS
        //----------------------------------------------------------

        List<GameItemVisual> finalVisuals =
            new List<GameItemVisual>(
                ItemsById.Count);

        for (int id = MinItemId;
             id <= MaxItemId;
             id++)
        {
            if (!ItemsById.TryGetValue(
                    id,
                    out GameItemData item))
            {
                continue;
            }

            GameItemVisual visual;

            //------------------------------------------------------
            // EXISTING
            //------------------------------------------------------

            if (VisualsByItemId.TryGetValue(
                    id,
                    out visual) &&
                visual != null)
            {
                UpdateVisualReference(
                    visual,
                    item);
            }
            //------------------------------------------------------
            // CREATE
            //------------------------------------------------------
            else
            {
                _missingVisuals++;

                visual =
                    CreateVisualAsset(
                        item);

                if (visual != null)
                {
                    VisualsByItemId[id] =
                        visual;
                }
            }

            //------------------------------------------------------
            // FINAL DATABASE
            //------------------------------------------------------

            if (visual != null)
            {
                finalVisuals.Add(
                    visual);
            }
        }

        //----------------------------------------------------------
        // SORT
        //----------------------------------------------------------

        finalVisuals.Sort(
            CompareVisualByItemId);

        //----------------------------------------------------------
        // WRITE DATABASE
        //----------------------------------------------------------

        SerializedObject so =
            new SerializedObject(
                database);

        SerializedProperty visuals =
            so.FindProperty("visuals");

        if (visuals == null)
        {
            Error(
                "GameItemVisualDatabase.visuals " +
                "serialized field not found.");

            return;
        }

        visuals.arraySize =
            finalVisuals.Count;

        for (int i = 0;
             i < finalVisuals.Count;
             i++)
        {
            visuals.GetArrayElementAtIndex(i)
                .objectReferenceValue =
                finalVisuals[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(
            database);

        Debug.Log(
            "[ItemSystemSetup] GameItemVisualDatabase rebuilt with " +
            finalVisuals.Count +
            " visuals.");
    }

    //==============================================================
    // VALIDATE ITEM DATABASE
    //==============================================================

    private static void ValidateItemDatabase(
        GameItemDatabase database)
    {
        database.RebuildLookup();

        int itemCount =
            database.Items != null
                ? database.Items.Count
                : 0;

        if (itemCount != MaxItemId)
        {
            Error(
                "GameItemDatabase count=" +
                itemCount +
                " | Expected=" +
                MaxItemId);
        }

        HashSet<int> ids =
            new HashSet<int>();

        if (database.Items == null)
        {
            Error(
                "GameItemDatabase.Items is null.");

            return;
        }

        for (int i = 0;
             i < database.Items.Count;
             i++)
        {
            GameItemData item =
                database.Items[i];

            if (item == null)
            {
                Error(
                    "GameItemDatabase contains null " +
                    "at index " +
                    i);

                continue;
            }

            int id =
                item.ItemId;

            if (!ids.Add(id))
            {
                Error(
                    "GameItemDatabase duplicate ItemId=" +
                    id);
            }

            if (id < MinItemId ||
                id > MaxItemId)
            {
                Error(
                    "GameItemDatabase ItemId outside range: " +
                    id);
            }

            if (!ItemsById.ContainsKey(id))
            {
                Warning(
                    "Database contains ItemId=" +
                    id +
                    " which is not present in scanned definitions.");
            }
        }

        //----------------------------------------------------------
        // EXACT IDS
        //----------------------------------------------------------

        for (int id = MinItemId;
             id <= MaxItemId;
             id++)
        {
            if (!ids.Contains(id))
            {
                Error(
                    "GameItemDatabase missing ItemId=" +
                    id);
            }
        }
    }

    //==============================================================
    // VALIDATE VISUAL DATABASE
    //==============================================================

    private static void ValidateVisualDatabase(
        GameItemVisualDatabase database)
    {
        database.RebuildLookup();

        //----------------------------------------------------------
        // READ SERIALIZED VISUAL LIST
        //----------------------------------------------------------

        SerializedObject so =
            new SerializedObject(
                database);

        SerializedProperty visuals =
            so.FindProperty("visuals");

        if (visuals == null)
        {
            Error(
                "GameItemVisualDatabase.visuals " +
                "serialized field not found.");

            return;
        }

        int count =
            visuals.arraySize;

        if (count != MaxItemId)
        {
            Error(
                "GameItemVisualDatabase count=" +
                count +
                " | Expected=" +
                MaxItemId);
        }

        HashSet<int> ids =
            new HashSet<int>();

        VisualsByItemId.Clear();

        for (int i = 0;
             i < count;
             i++)
        {
            SerializedProperty element =
                visuals.GetArrayElementAtIndex(i);

            GameItemVisual visual =
                element.objectReferenceValue
                as GameItemVisual;

            if (visual == null)
            {
                Error(
                    "GameItemVisualDatabase contains null " +
                    "at index " +
                    i);

                continue;
            }

            int itemId =
                visual.ItemId;

            //------------------------------------------------------
            // RANGE
            //------------------------------------------------------

            if (itemId < MinItemId ||
                itemId > MaxItemId)
            {
                Error(
                    "Visual ItemId outside range: " +
                    itemId +
                    " | " +
                    visual.name);

                continue;
            }

            //------------------------------------------------------
            // DUPLICATE
            //------------------------------------------------------

            if (!ids.Add(itemId))
            {
                Error(
                    "Duplicate Visual ItemId=" +
                    itemId +
                    " | " +
                    visual.name);

                continue;
            }

            VisualsByItemId.Add(
                itemId,
                visual);

            //------------------------------------------------------
            // VISUAL ID
            //------------------------------------------------------

            string expectedVisualId =
                BuildVisualId(itemId);

            if (visual.VisualId != expectedVisualId)
            {
                Error(
                    "VisualId mismatch for ItemId=" +
                    itemId +
                    " | Expected=" +
                    expectedVisualId +
                    " | Actual=" +
                    visual.VisualId);
            }

            //------------------------------------------------------
            // PREFAB
            //------------------------------------------------------

            if (visual.Prefab == null)
            {
                _missingPrefabs++;

                // INTENTIONALLY INFO, NOT WARNING.
                Debug.Log(
                    "[ItemSystemSetup] INFO | " +
                    "Visual has no prefab yet " +
                    "(model not created): ItemId=" +
                    itemId +
                    " | " +
                    visual.name);
            }

            //------------------------------------------------------
            // ENABLED
            //------------------------------------------------------

            if (!visual.Enabled)
            {
                Warning(
                    "Visual disabled: ItemId=" +
                    itemId +
                    " | " +
                    visual.name);
            }
        }

        //----------------------------------------------------------
        // EXACT IDS
        //----------------------------------------------------------

        for (int id = MinItemId;
             id <= MaxItemId;
             id++)
        {
            if (!ids.Contains(id))
            {
                Error(
                    "GameItemVisualDatabase missing " +
                    "Visual for ItemId=" +
                    id);
            }
        }
    }

    //==============================================================
    // CROSS DATABASE CONTRACT
    //==============================================================

    private static void ValidateItemVisualContract(
        GameItemDatabase itemDatabase,
        GameItemVisualDatabase visualDatabase)
    {
        //----------------------------------------------------------
        // ITEM → VISUAL
        //----------------------------------------------------------

        for (int id = MinItemId;
             id <= MaxItemId;
             id++)
        {
            GameItemData item;

            if (!ItemsById.TryGetValue(
                    id,
                    out item))
            {
                Error(
                    "Cross-check missing ItemId=" +
                    id);

                continue;
            }

            GameItemVisual visual;

            if (!VisualsByItemId.TryGetValue(
                    id,
                    out visual) ||
                visual == null)
            {
                Error(
                    "Cross-check missing Visual for ItemId=" +
                    id);

                continue;
            }

            //------------------------------------------------------
            // ITEM ID
            //------------------------------------------------------

            if (visual.ItemId != item.ItemId)
            {
                Error(
                    "Item ↔ Visual ID mismatch: " +
                    "ItemId=" +
                    item.ItemId +
                    " | Visual.ItemId=" +
                    visual.ItemId);
            }

            //------------------------------------------------------
            // VISUAL ID
            //------------------------------------------------------

            string expectedVisualId =
                BuildVisualId(id);

            if (visual.VisualId != expectedVisualId)
            {
                Error(
                    "VisualId contract broken: ItemId=" +
                    id +
                    " | Expected=" +
                    expectedVisualId +
                    " | Actual=" +
                    visual.VisualId);
            }

            //------------------------------------------------------
            // ANCHOR
            //------------------------------------------------------

            GameItemVisualAnchor expectedAnchor =
                MapAnchor(
                    item.Category);

            if (visual.Anchor != expectedAnchor)
            {
                Error(
                    "Anchor mismatch: ItemId=" +
                    id +
                    " | Category=" +
                    item.Category +
                    " | ExpectedAnchor=" +
                    expectedAnchor +
                    " | ActualAnchor=" +
                    visual.Anchor);
            }
        }

        //----------------------------------------------------------
        // DATABASE OBJECTS ARE USED
        //----------------------------------------------------------

        if (itemDatabase == null)
        {
            Error(
                "Item database reference is null.");
        }

        if (visualDatabase == null)
        {
            Error(
                "Visual database reference is null.");
        }
    }

    //==============================================================
    // COMPARE
    //==============================================================

    private static int CompareVisualByItemId(
        GameItemVisual a,
        GameItemVisual b)
    {
        if (a == null)
        {
            return 1;
        }

        if (b == null)
        {
            return -1;
        }

        return a.ItemId.CompareTo(
            b.ItemId);
    }

    //==============================================================
    // CREATE VISUAL ASSET
    //==============================================================

    private static GameItemVisual CreateVisualAsset(
        GameItemData item)
    {
        string path =
            VisualFolder +
            "/" +
            VisualFilePrefix +
            item.ItemId.ToString("00") +
            ".asset";

        GameItemVisual existing =
            AssetDatabase.LoadAssetAtPath<GameItemVisual>(
                path);

        if (existing != null)
        {
            UpdateVisualReference(
                existing,
                item);

            return existing;
        }

        //----------------------------------------------------------
        // CREATE
        //----------------------------------------------------------

        GameItemVisual visual =
            ScriptableObject.CreateInstance<GameItemVisual>();

        SerializedObject so =
            new SerializedObject(
                visual);

        SetString(
            so,
            "visualId",
            BuildVisualId(
                item.ItemId));

        SetInt(
            so,
            "itemId",
            item.ItemId);

        SetString(
            so,
            "characterId",
            string.Empty);

        SetEnum(
            so,
            "anchor",
            (int)MapAnchor(
                item.Category));

        //----------------------------------------------------------
        // LEGACY PREFAB FALLBACK
        //----------------------------------------------------------

        if (item.Prefab != null)
        {
            SetObjectReference(
                so,
                "prefab",
                item.Prefab);
        }

        //----------------------------------------------------------
        // TRANSFORM
        //
        // No model exists yet, so these are neutral defaults.
        // They are NOT item-specific scale hacks.
        //----------------------------------------------------------

        SetVector3(
            so,
            "localPosition",
            Vector3.zero);

        SetVector3(
            so,
            "localRotation",
            Vector3.zero);

        SetVector3(
            so,
            "localScale",
            Vector3.one);

        SetBool(
            so,
            "enabled",
            true);

        so.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.CreateAsset(
            visual,
            path);

        EditorUtility.SetDirty(
            visual);

        if (item.Prefab == null)
        {
            _missingPrefabs++;

            Debug.Log(
                "[ItemSystemSetup] INFO | " +
                "Created Visual without prefab " +
                "(model not created yet): ItemId=" +
                item.ItemId +
                " | " +
                item.ItemName);
        }

        return visual;
    }

    //==============================================================
    // UPDATE VISUAL
    //==============================================================

    private static void UpdateVisualReference(
        GameItemVisual visual,
        GameItemData item)
    {
        if (visual == null ||
            item == null)
        {
            return;
        }

        SerializedObject so =
            new SerializedObject(
                visual);

        //----------------------------------------------------------
        // STABLE ID
        //----------------------------------------------------------

        SetString(
            so,
            "visualId",
            BuildVisualId(
                item.ItemId));

        SetInt(
            so,
            "itemId",
            item.ItemId);

        //----------------------------------------------------------
        // CATEGORY → ANCHOR
        //----------------------------------------------------------

        SetEnum(
            so,
            "anchor",
            (int)MapAnchor(
                item.Category));

        //----------------------------------------------------------
        // IMPORTANT PREFAB RULE
        //
        // NEVER:
        //
        // item.Prefab == null
        //       ↓
        // visual.Prefab = null
        //
        // because GameItemVisual is the visual source of truth.
        //----------------------------------------------------------

        if (item.Prefab != null)
        {
            SetObjectReference(
                so,
                "prefab",
                item.Prefab);
        }

        //----------------------------------------------------------
        // DO NOT TOUCH:
        //
        // localPosition
        // localRotation
        // localScale
        // characterId
        // enabled
        //
        // Existing visual configuration must survive setup.
        //----------------------------------------------------------

        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(
            visual);
    }

    //==============================================================
    // VISUAL ID
    //==============================================================

    private static string BuildVisualId(
        int itemId)
    {
        return "VISUAL_ITEM_" +
               itemId.ToString("000");
    }

    //==============================================================
    // CATEGORY → ANCHOR
    //==============================================================

    private static GameItemVisualAnchor MapAnchor(
        GameItemCategory category)
    {
        switch (category)
        {
            case GameItemCategory.Vehicle:
                return GameItemVisualAnchor.VehicleRoot;

            case GameItemCategory.Outfit:
                return GameItemVisualAnchor.OutfitRoot;

            case GameItemCategory.Accessory:
                return GameItemVisualAnchor.AccessoryRoot;

            case GameItemCategory.Cosmetic:
                return GameItemVisualAnchor.CosmeticRoot;

            default:
                return GameItemVisualAnchor.VehicleRoot;
        }
    }

    //==============================================================
    // LOAD / CREATE ITEM DATABASE
    //==============================================================

    private static GameItemDatabase LoadOrCreateItemDatabase()
    {
        GameItemDatabase database =
            AssetDatabase.LoadAssetAtPath<GameItemDatabase>(
                ItemDatabasePath);

        if (database != null)
        {
            return database;
        }

        database =
            ScriptableObject.CreateInstance<GameItemDatabase>();

        AssetDatabase.CreateAsset(
            database,
            ItemDatabasePath);

        EditorUtility.SetDirty(
            database);

        Debug.Log(
            "[ItemSystemSetup] Created " +
            ItemDatabasePath);

        return database;
    }

    //==============================================================
    // LOAD / CREATE VISUAL DATABASE
    //==============================================================

    private static GameItemVisualDatabase LoadOrCreateVisualDatabase()
    {
        GameItemVisualDatabase database =
            AssetDatabase.LoadAssetAtPath<GameItemVisualDatabase>(
                VisualDatabasePath);

        if (database != null)
        {
            return database;
        }

        database =
            ScriptableObject.CreateInstance<GameItemVisualDatabase>();

        AssetDatabase.CreateAsset(
            database,
            VisualDatabasePath);

        EditorUtility.SetDirty(
            database);

        Debug.Log(
            "[ItemSystemSetup] Created " +
            VisualDatabasePath);

        return database;
    }

    //==============================================================
    // REPAIR DATABASE SCRIPT
    //==============================================================

    private static bool RepairMissingDatabaseScript(
        string assetPath,
        Type scriptType)
    {
        if (!File.Exists(assetPath))
        {
            return false;
        }

        string fullPath =
            Path.GetFullPath(
                assetPath);

        string text =
            File.ReadAllText(
                fullPath);

        if (!text.Contains(
                "m_Script:"))
        {
            return false;
        }

        string scriptTypeName =
            scriptType.Name;

        string[] scriptGuids =
            AssetDatabase.FindAssets(
                scriptTypeName +
                " t:MonoScript");

        if (scriptGuids == null ||
            scriptGuids.Length == 0)
        {
            Error(
                "Cannot find current MonoScript for " +
                scriptTypeName +
                ". Compile the project first.");

            return false;
        }

        //----------------------------------------------------------
        // FIND EXACT SCRIPT
        //----------------------------------------------------------

        string scriptPath = null;

        for (int i = 0;
             i < scriptGuids.Length;
             i++)
        {
            string candidate =
                AssetDatabase.GUIDToAssetPath(
                    scriptGuids[i]);

            MonoScript script =
                AssetDatabase.LoadAssetAtPath<MonoScript>(
                    candidate);

            if (script == null)
            {
                continue;
            }

            Type candidateType =
                script.GetClass();

            if (candidateType == scriptType)
            {
                scriptPath =
                    candidate;

                break;
            }
        }

        if (string.IsNullOrEmpty(
                scriptPath))
        {
            Error(
                "Cannot resolve exact MonoScript for " +
                scriptTypeName);

            return false;
        }

        string currentGuid =
            AssetDatabase.AssetPathToGUID(
                scriptPath);

        if (string.IsNullOrEmpty(
                currentGuid))
        {
            Error(
                "Cannot resolve GUID for " +
                scriptTypeName +
                " at " +
                scriptPath);

            return false;
        }

        //----------------------------------------------------------
        // REPLACE GUID
        //----------------------------------------------------------

        string[] lines =
            text.Replace(
                    "\r\n",
                    "\n")
                .Split('\n');

        bool changed = false;

        for (int i = 0;
             i < lines.Length;
             i++)
        {
            string line =
                lines[i];

            if (!line.TrimStart()
                    .StartsWith(
                        "m_Script:"))
            {
                continue;
            }

            int guidIndex =
                line.IndexOf(
                    "guid:");

            if (guidIndex < 0)
            {
                continue;
            }

            int valueStart =
                guidIndex +
                "guid:".Length;

            int end =
                line.IndexOf(
                    ',',
                    valueStart);

            if (end < 0)
            {
                end =
                    line.Length;
            }

            string prefix =
                line.Substring(
                    0,
                    valueStart);

            string suffix =
                line.Substring(
                    end);

            lines[i] =
                prefix +
                " " +
                currentGuid +
                suffix;

            changed = true;
            break;
        }

        if (!changed)
        {
            return false;
        }

        File.WriteAllText(
            fullPath,
            string.Join(
                "\n",
                lines));

        AssetDatabase.ImportAsset(
            assetPath,
            ImportAssetOptions.ForceUpdate);

        Debug.Log(
            "[ItemSystemSetup] Repaired script GUID: " +
            assetPath);

        return true;
    }

    //==============================================================
    // ASSIGN DATABASES TO OPEN SCENES
    //==============================================================

    private static void AssignDatabasesToOpenScenes(
        GameItemDatabase itemDatabase,
        GameItemVisualDatabase visualDatabase)
    {
        int sceneCount =
            SceneManager.sceneCount;

        for (int sceneIndex = 0;
             sceneIndex < sceneCount;
             sceneIndex++)
        {
            Scene scene =
                SceneManager.GetSceneAt(
                    sceneIndex);

            if (!scene.isLoaded)
            {
                continue;
            }

            GameObject[] roots =
                scene.GetRootGameObjects();

            for (int rootIndex = 0;
                 rootIndex < roots.Length;
                 rootIndex++)
            {
                MonoBehaviour[] behaviours =
                    roots[rootIndex]
                        .GetComponentsInChildren<MonoBehaviour>(
                            true);

                for (int i = 0;
                     i < behaviours.Length;
                     i++)
                {
                    MonoBehaviour behaviour =
                        behaviours[i];

                    if (behaviour == null)
                    {
                        continue;
                    }

                    SerializedObject so =
                        new SerializedObject(
                            behaviour);

                    bool changed = false;

                    //--------------------------------------------------
                    // ITEM DATABASE
                    //--------------------------------------------------

                    SerializedProperty itemDb =
                        so.FindProperty(
                            "itemDatabase");

                    if (itemDb != null &&
                        itemDb.propertyType ==
                        SerializedPropertyType.ObjectReference &&
                        itemDb.objectReferenceValue == null)
                    {
                        itemDb.objectReferenceValue =
                            itemDatabase;

                        changed = true;
                    }

                    //--------------------------------------------------
                    // VISUAL DATABASE
                    //--------------------------------------------------

                    SerializedProperty visualDb =
                        so.FindProperty(
                            "visualDatabase");

                    if (visualDb != null &&
                        visualDb.propertyType ==
                        SerializedPropertyType.ObjectReference &&
                        visualDb.objectReferenceValue == null)
                    {
                        visualDb.objectReferenceValue =
                            visualDatabase;

                        changed = true;
                    }

                    //--------------------------------------------------
                    // APPLY
                    //--------------------------------------------------

                    if (changed)
                    {
                        so.ApplyModifiedPropertiesWithoutUndo();

                        EditorUtility.SetDirty(
                            behaviour);

                        Debug.Log(
                            "[ItemSystemSetup] Assigned databases → " +
                            behaviour.GetType().Name +
                            " | " +
                            behaviour.gameObject.name);
                    }
                }
            }
        }
    }

    //==============================================================
    // SERIALIZED PROPERTY HELPERS
    //==============================================================

    private static void SetString(
        SerializedObject so,
        string propertyName,
        string value)
    {
        SerializedProperty property =
            so.FindProperty(
                propertyName);

        if (property != null)
        {
            property.stringValue =
                value;
        }
    }

    private static void SetInt(
        SerializedObject so,
        string propertyName,
        int value)
    {
        SerializedProperty property =
            so.FindProperty(
                propertyName);

        if (property != null)
        {
            property.intValue =
                value;
        }
    }

    private static void SetBool(
        SerializedObject so,
        string propertyName,
        bool value)
    {
        SerializedProperty property =
            so.FindProperty(
                propertyName);

        if (property != null)
        {
            property.boolValue =
                value;
        }
    }

    private static void SetEnum(
        SerializedObject so,
        string propertyName,
        int value)
    {
        SerializedProperty property =
            so.FindProperty(
                propertyName);

        if (property != null)
        {
            property.enumValueIndex =
                value;
        }
    }

    private static void SetVector3(
        SerializedObject so,
        string propertyName,
        Vector3 value)
    {
        SerializedProperty property =
            so.FindProperty(
                propertyName);

        if (property != null)
        {
            property.vector3Value =
                value;
        }
    }

    private static void SetObjectReference(
        SerializedObject so,
        string propertyName,
        UnityEngine.Object value)
    {
        SerializedProperty property =
            so.FindProperty(
                propertyName);

        if (property != null)
        {
            property.objectReferenceValue =
                value;
        }
    }

    //==============================================================
    // FOLDER
    //==============================================================

    private static void EnsureFolder(
        string folderPath)
    {
        if (AssetDatabase.IsValidFolder(
                folderPath))
        {
            return;
        }

        string[] parts =
            folderPath.Split('/');

        string current =
            parts[0];

        for (int i = 1;
             i < parts.Length;
             i++)
        {
            string next =
                current +
                "/" +
                parts[i];

            if (!AssetDatabase.IsValidFolder(
                    next))
            {
                AssetDatabase.CreateFolder(
                    current,
                    parts[i]);
            }

            current =
                next;
        }
    }

    //==============================================================
    // REPORT HELPERS
    //==============================================================

    private static void Error(
        string message)
    {
        _errors++;

        Debug.LogError(
            "[ItemSystemSetup] " +
            message);
    }

    private static void Warning(
        string message)
    {
        _warnings++;

        Debug.LogWarning(
            "[ItemSystemSetup] " +
            message);
    }

    private static void PrintReport()
    {
        Debug.Log(
            "[ItemSystemSetup] REPORT | " +
            "Items=" +
            ItemsById.Count +
            " | Visuals=" +
            VisualsByItemId.Count +
            " | MissingPrefab=" +
            _missingPrefabs +
            " | MissingVisual=" +
            _missingVisuals +
            " | Errors=" +
            _errors +
            " | Warnings=" +
            _warnings);
    }
}

#endif