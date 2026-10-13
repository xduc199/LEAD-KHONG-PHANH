#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class ItemVisualMigrationTool
{
    [MenuItem("LEAD KHONG PHANH/Items/Validate Visual Database")]
    public static void Validate()
    {
        GameItemDatabase items =
            AssetDatabase.LoadAssetAtPath<GameItemDatabase>(
                "Assets/GameData/Items/Database/GameItemDatabase.asset");

        GameItemVisualDatabase visuals =
            AssetDatabase.LoadAssetAtPath<GameItemVisualDatabase>(
                "Assets/GameData/Items/Database/GameItemVisualDatabase.asset");

        if (items == null || visuals == null)
        {
            Debug.LogError(
                "[ItemVisualMigration] Missing Item or Visual database.");

            return;
        }

        items.Initialize();
        visuals.Initialize();

        int errors = 0;
        int warnings = 0;

        for (int id = GameItemDatabase.MinItemId;
             id <= GameItemDatabase.MaxItemId;
             id++)
        {
            if (!items.TryGet(id, out GameItemData item) || item == null)
            {
                Debug.LogError(
                    "[ItemVisualMigration] Missing ItemId " + id);

                errors++;
                continue;
            }

            if (!visuals.TryGet(id, out GameItemVisual visual) ||
                visual == null)
            {
                Debug.LogError(
                    "[ItemVisualMigration] Missing Visual for ItemId " + id);

                errors++;
                continue;
            }

            if (visual.Prefab == null)
            {
                Debug.LogWarning(
                    "[ItemVisualMigration] ItemId " +
                    id +
                    " has no visual prefab.");

                warnings++;
            }
        }

        Debug.Log(
            "[ItemVisualMigration] Validation complete. " +
            "Range=" +
            GameItemDatabase.MinItemId +
            "-" +
            GameItemDatabase.MaxItemId +
            " | Errors=" +
            errors +
            " | Warnings=" +
            warnings);
    }
}
#endif