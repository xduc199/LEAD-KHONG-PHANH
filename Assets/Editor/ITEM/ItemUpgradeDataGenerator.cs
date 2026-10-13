#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.IO;

public static class ItemUpgradeDataGenerator
{
    //=========================================================
    // CONFIG
    //=========================================================

    private const string RootFolder = "Assets/Data";
    private const string FolderPath = "Assets/Data/ItemUpgrades";

    private const int MaxLevel = 20;

    // Tổng LV2 -> LV20 = 100.000 Gold
    private static readonly int[] UpgradeCosts =
    {
        1200,   // LV2
        1500,   // LV3
        1800,   // LV4
        2000,   // LV5
        2300,   // LV6
        2800,   // LV7
        3300,   // LV8
        3800,   // LV9
        4300,   // LV10
        4800,   // LV11
        5400,   // LV12
        6100,   // LV13
        6700,   // LV14
        7300,   // LV15
        8000,   // LV16
        8600,   // LV17
        9300,   // LV18
        9900,   // LV19
        10900   // LV20
    };


    //=========================================================
    // MENU
    //=========================================================

    [MenuItem("Tools/Lead Khong Phanh/Item Upgrade/Generate All 4 Items")]
    private static void GenerateAll()
    {
        EnsureFolderExists();
        ValidateCostTable();

        GenerateGum();
        GeneratePhoton();
        GenerateShield();
        GenerateMagnet();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "[ItemUpgradeDataGenerator] SUCCESS - " +
            "Generated all 4 ItemUpgradeData assets."
        );

        EditorUtility.DisplayDialog(
            "Item Upgrade Generator",
            "Đã tạo/cập nhật đầy đủ 4 Item Upgrade Data:\n\n" +
            "• Gum\n" +
            "• Photon\n" +
            "• Shield\n" +
            "• Magnet\n\n" +
            "Mỗi item có 20 Level.\n" +
            "Tổng chi phí LV1 → LV20: 100.000 Gold/item.",
            "OK"
        );
    }


    [MenuItem("Tools/Lead Khong Phanh/Item Upgrade/Generate Gum")]
    private static void GenerateGumOnly()
    {
        EnsureFolderExists();

        ItemUpgradeData data = GenerateGum();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "[ItemUpgradeDataGenerator] Gum generated: " +
            AssetDatabase.GetAssetPath(data)
        );
    }


    [MenuItem("Tools/Lead Khong Phanh/Item Upgrade/Generate Photon")]
    private static void GeneratePhotonOnly()
    {
        EnsureFolderExists();

        ItemUpgradeData data = GeneratePhoton();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "[ItemUpgradeDataGenerator] Photon generated: " +
            AssetDatabase.GetAssetPath(data)
        );
    }


    [MenuItem("Tools/Lead Khong Phanh/Item Upgrade/Generate Shield")]
    private static void GenerateShieldOnly()
    {
        EnsureFolderExists();

        ItemUpgradeData data = GenerateShield();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "[ItemUpgradeDataGenerator] Shield generated: " +
            AssetDatabase.GetAssetPath(data)
        );
    }


    [MenuItem("Tools/Lead Khong Phanh/Item Upgrade/Generate Magnet")]
    private static void GenerateMagnetOnly()
    {
        EnsureFolderExists();

        ItemUpgradeData data = GenerateMagnet();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "[ItemUpgradeDataGenerator] Magnet generated: " +
            AssetDatabase.GetAssetPath(data)
        );
    }


    //=========================================================
    // GUM
    //=========================================================

    private static ItemUpgradeData GenerateGum()
    {
        ItemUpgradeData data = GetOrCreateAsset(
            "Gum_ItemUpgradeData.asset"
        );

        data.itemType = UpgradeItemType.Gum;
        data.maxLevel = MaxLevel;

        data.levels = new ItemUpgradeLevel[MaxLevel];

        for (int level = 1; level <= MaxLevel; level++)
        {
            float duration =
                5.0f +
                ((level - 1) * 0.2f);

            float horizontalSpeed =
                30.0f +
                ((level - 1) * 0.5f);

            data.levels[level - 1] = CreateLevel(
                level,
                duration,
                horizontalSpeed,
                0f,
                1,
                0f,
                0f,
                0f,
                GetUpgradeCost(level)
            );
        }

        EditorUtility.SetDirty(data);

        ValidateGeneratedData(
            data,
            UpgradeItemType.Gum
        );

        return data;
    }


    //=========================================================
    // PHOTON
    //=========================================================

    private static ItemUpgradeData GeneratePhoton()
    {
        ItemUpgradeData data = GetOrCreateAsset(
            "Photon_ItemUpgradeData.asset"
        );

        data.itemType = UpgradeItemType.Photon;
        data.maxLevel = MaxLevel;

        data.levels = new ItemUpgradeLevel[MaxLevel];

        for (int level = 1; level <= MaxLevel; level++)
        {
            // Duration cố định 11 giây.
            float duration = 11.0f;

            // LV1 = 2.50x
            // LV20 = 3.45x
            float speedMultiplier =
                2.50f +
                ((level - 1) * 0.05f);

            data.levels[level - 1] = CreateLevel(
                level,
                duration,
                0f,
                speedMultiplier,
                1,
                0f,
                0f,
                0f,
                GetUpgradeCost(level)
            );
        }

        EditorUtility.SetDirty(data);

        ValidateGeneratedData(
            data,
            UpgradeItemType.Photon
        );

        return data;
    }


    //=========================================================
    // SHIELD
    //=========================================================

    private static ItemUpgradeData GenerateShield()
    {
        ItemUpgradeData data = GetOrCreateAsset(
            "Shield_ItemUpgradeData.asset"
        );

        data.itemType = UpgradeItemType.Shield;
        data.maxLevel = MaxLevel;

        data.levels = new ItemUpgradeLevel[MaxLevel];

        for (int level = 1; level <= MaxLevel; level++)
        {
            // LV1 = 10s
            // LV20 = 13.8s
            float duration =
                10.0f +
                ((level - 1) * 0.2f);

            int maxHits;

            if (level <= 6)
            {
                maxHits = 1;
            }
            else if (level <= 12)
            {
                maxHits = 2;
            }
            else if (level <= 18)
            {
                maxHits = 3;
            }
            else
            {
                maxHits = 4;
            }

            data.levels[level - 1] = CreateLevel(
                level,
                duration,
                0f,
                0f,
                maxHits,
                0f,
                0f,
                0f,
                GetUpgradeCost(level)
            );
        }

        EditorUtility.SetDirty(data);

        ValidateGeneratedData(
            data,
            UpgradeItemType.Shield
        );

        return data;
    }


    //=========================================================
    // MAGNET
    //=========================================================

    private static ItemUpgradeData GenerateMagnet()
    {
        ItemUpgradeData data = GetOrCreateAsset(
            "Magnet_ItemUpgradeData.asset"
        );

        data.itemType = UpgradeItemType.Magnet;
        data.maxLevel = MaxLevel;

        data.levels = new ItemUpgradeLevel[MaxLevel];

        for (int level = 1; level <= MaxLevel; level++)
        {
            // LV1 = 8.0
            // LV20 = 11.8
            float duration =
                8.0f +
                ((level - 1) * 0.2f);

            // LV1 = 8.00
            // LV20 = 12.75
            float magnetRadius =
                8.0f +
                ((level - 1) * 0.25f);

            // LV1 = 30
            // LV20 = 39.5
            float pullSpeed =
                30.0f +
                ((level - 1) * 0.5f);

            // LV1 = 1.00
            // LV20 = 1.95
            float collectDistance =
                1.0f +
                ((level - 1) * 0.05f);

            data.levels[level - 1] = CreateLevel(
                level,
                duration,
                0f,
                0f,
                1,
                magnetRadius,
                pullSpeed,
                collectDistance,
                GetUpgradeCost(level)
            );
        }

        EditorUtility.SetDirty(data);

        ValidateGeneratedData(
            data,
            UpgradeItemType.Magnet
        );

        return data;
    }


    //=========================================================
    // CREATE LEVEL
    //=========================================================

    private static ItemUpgradeLevel CreateLevel(
        int level,
        float duration,
        float horizontalSpeed,
        float speedMultiplier,
        int maxHits,
        float magnetRadius,
        float pullSpeed,
        float collectDistance,
        int upgradeCost
    )
    {
        ItemUpgradeLevel data = new ItemUpgradeLevel();

        data.level = level;

        // Common
        data.duration = duration;

        // Gum
        data.horizontalSpeed = horizontalSpeed;

        // Photon
        data.speedMultiplier = speedMultiplier;

        // Shield
        data.maxHits = maxHits;

        // Magnet
        data.magnetRadius = magnetRadius;
        data.pullSpeed = pullSpeed;
        data.collectDistance = collectDistance;

        // Cost
        data.upgradeCost = upgradeCost;

        return data;
    }


    //=========================================================
    // GET / CREATE ASSET
    //=========================================================

    private static ItemUpgradeData GetOrCreateAsset(
        string fileName
    )
    {
        string path =
            FolderPath +
            "/" +
            fileName;

        ItemUpgradeData data =
            AssetDatabase.LoadAssetAtPath<ItemUpgradeData>(
                path
            );

        if (data == null)
        {
            data =
                ScriptableObject.CreateInstance<ItemUpgradeData>();

            AssetDatabase.CreateAsset(
                data,
                path
            );

            Debug.Log(
                "[ItemUpgradeDataGenerator] Created: " +
                path
            );
        }
        else
        {
            Debug.Log(
                "[ItemUpgradeDataGenerator] Updated: " +
                path
            );
        }

        return data;
    }


    //=========================================================
    // COST
    //=========================================================

    private static int GetUpgradeCost(int level)
    {
        // LV1 không mua.
        if (level <= 1)
        {
            return 0;
        }

        int index = level - 2;

        if (index < 0 ||
            index >= UpgradeCosts.Length)
        {
            return 0;
        }

        return UpgradeCosts[index];
    }


    private static void ValidateCostTable()
    {
        if (UpgradeCosts.Length != MaxLevel - 1)
        {
            Debug.LogError(
                "[ItemUpgradeDataGenerator] " +
                "UpgradeCosts không đủ dữ liệu."
            );

            return;
        }

        int totalCost = 0;

        for (int i = 0; i < UpgradeCosts.Length; i++)
        {
            if (UpgradeCosts[i] < 0)
            {
                Debug.LogError(
                    "[ItemUpgradeDataGenerator] " +
                    "Upgrade cost âm tại index " +
                    i
                );
            }

            totalCost += UpgradeCosts[i];
        }

        Debug.Log(
            "[ItemUpgradeDataGenerator] " +
            "Total upgrade cost per item = " +
            totalCost +
            " Gold."
        );

        if (totalCost != 100000)
        {
            Debug.LogWarning(
                "[ItemUpgradeDataGenerator] " +
                "Tổng cost hiện tại = " +
                totalCost +
                ", không phải 100.000 Gold."
            );
        }
    }


    //=========================================================
    // VALIDATE GENERATED DATA
    //=========================================================

    private static void ValidateGeneratedData(
        ItemUpgradeData data,
        UpgradeItemType expectedType
    )
    {
        if (data == null)
        {
            Debug.LogError(
                "[ItemUpgradeDataGenerator] " +
                "Generated data is NULL."
            );

            return;
        }

        if (data.itemType != expectedType)
        {
            Debug.LogError(
                "[ItemUpgradeDataGenerator] " +
                "Wrong item type. Expected " +
                expectedType +
                " but got " +
                data.itemType
            );
        }

        if (data.maxLevel != MaxLevel)
        {
            Debug.LogError(
                "[ItemUpgradeDataGenerator] " +
                expectedType +
                " maxLevel = " +
                data.maxLevel +
                " instead of " +
                MaxLevel
            );
        }

        if (data.levels == null)
        {
            Debug.LogError(
                "[ItemUpgradeDataGenerator] " +
                expectedType +
                " levels is NULL."
            );

            return;
        }

        if (data.levels.Length != MaxLevel)
        {
            Debug.LogError(
                "[ItemUpgradeDataGenerator] " +
                expectedType +
                " has " +
                data.levels.Length +
                " levels instead of " +
                MaxLevel
            );

            return;
        }

        int totalCost = 0;

        for (int i = 0; i < data.levels.Length; i++)
        {
            ItemUpgradeLevel level =
                data.levels[i];

            if (level == null)
            {
                Debug.LogError(
                    "[ItemUpgradeDataGenerator] " +
                    expectedType +
                    " Level " +
                    (i + 1) +
                    " is NULL."
                );

                continue;
            }

            int expectedLevel = i + 1;

            if (level.level != expectedLevel)
            {
                Debug.LogError(
                    "[ItemUpgradeDataGenerator] " +
                    expectedType +
                    " expected Level " +
                    expectedLevel +
                    " but got " +
                    level.level
                );
            }

            if (level.upgradeCost < 0)
            {
                Debug.LogError(
                    "[ItemUpgradeDataGenerator] " +
                    expectedType +
                    " Level " +
                    expectedLevel +
                    " has negative cost."
                );
            }

            totalCost += level.upgradeCost;
        }

        if (totalCost != 100000)
        {
            Debug.LogWarning(
                "[ItemUpgradeDataGenerator] " +
                expectedType +
                " total cost = " +
                totalCost +
                " Gold."
            );
        }
        else
        {
            Debug.Log(
                "[ItemUpgradeDataGenerator] VALIDATED | " +
                expectedType +
                " | 20 Levels | " +
                "Total Cost = 100,000 Gold"
            );
        }
    }


    //=========================================================
    // FOLDER
    //=========================================================

    private static void EnsureFolderExists()
    {
        if (!AssetDatabase.IsValidFolder(RootFolder))
        {
            AssetDatabase.CreateFolder(
                "Assets",
                "Data"
            );
        }

        if (!AssetDatabase.IsValidFolder(FolderPath))
        {
            AssetDatabase.CreateFolder(
                RootFolder,
                "ItemUpgrades"
            );
        }
    }
}

#endif