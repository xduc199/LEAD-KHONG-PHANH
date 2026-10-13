#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class PlayerVisualRootSetup
{
    [MenuItem("LEAD KHONG PHANH/Items/Create Player Visual Roots")]
    public static void CreateRoots()
    {
        GameObject[] selected = Selection.gameObjects;

        if (selected == null || selected.Length == 0)
        {
            Debug.LogError(
                "[PlayerVisualRootSetup] Hãy chọn PLAYER GameObject trước.");
            return;
        }

        for (int i = 0; i < selected.Length; i++)
        {
            GameObject player = selected[i];

            if (player == null)
                continue;

            CreateRoot(player.transform, "VehicleVisualRoot");
            CreateRoot(player.transform, "OutfitVisualRoot");
            CreateRoot(player.transform, "CosmeticVisualRoot");
            CreateRoot(player.transform, "AccessoryVisualRoot");

            EditorUtility.SetDirty(player);
        }

        Debug.Log(
            "[PlayerVisualRootSetup] Created/verified 4 visual roots.");
    }

    private static void CreateRoot(Transform player, string rootName)
    {
        Transform existing = player.Find(rootName);

        if (existing != null)
        {
            existing.localPosition = Vector3.zero;
            existing.localRotation = Quaternion.identity;
            existing.localScale = Vector3.one;
            return;
        }

        GameObject root = new GameObject(rootName);
        Transform rootTransform = root.transform;

        rootTransform.SetParent(player, false);
        rootTransform.localPosition = Vector3.zero;
        rootTransform.localRotation = Quaternion.identity;
        rootTransform.localScale = Vector3.one;

        Undo.RegisterCreatedObjectUndo(root, "Create " + rootName);
    }
}
#endif
