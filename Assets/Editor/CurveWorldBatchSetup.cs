#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class CurveWorldBatchSetup
{
    private const string GeneratedFolder =
        "Assets/Materials/CurveWorldGenerated";

    [MenuItem(
        "LEAD KHONG PHANH/Curve World/Restore Original URP Materials",
        priority = 1000)]
    private static void RestoreOriginalMaterials()
    {
        GameObject root = Selection.activeGameObject;

        if (root == null)
        {
            EditorUtility.DisplayDialog(
                "Curve World",
                "Hãy chọn RoadChunk root trước.",
                "OK");

            return;
        }

        Renderer[] renderers =
            root.GetComponentsInChildren<Renderer>(true);

        int restoredRendererCount = 0;
        int restoredMaterialCount = 0;

        Undo.IncrementCurrentGroup();

        int undoGroup =
            Undo.GetCurrentGroup();

        try
        {
            for (int r = 0; r < renderers.Length; r++)
            {
                Renderer renderer = renderers[r];

                if (renderer == null)
                    continue;

                Material[] materials =
                    renderer.sharedMaterials;

                if (materials == null ||
                    materials.Length == 0)
                    continue;

                Material[] restored =
                    (Material[])materials.Clone();

                bool changed = false;

                for (int slot = 0;
                     slot < restored.Length;
                     slot++)
                {
                    Material material =
                        restored[slot];

                    if (material == null)
                        continue;

                    if (!material.name.EndsWith(
                            "_CurveWorld"))
                    {
                        continue;
                    }

                    string originalName =
                        material.name.Substring(
                            0,
                            material.name.Length -
                            "_CurveWorld".Length);

                    Material original =
                        FindOriginalMaterial(
                            originalName);

                    if (original == null)
                        continue;

                    restored[slot] = original;

                    restoredMaterialCount++;
                    changed = true;
                }

                if (!changed)
                    continue;

                Undo.RecordObject(
                    renderer,
                    "Restore Original URP Materials");

                renderer.sharedMaterials = restored;

                EditorUtility.SetDirty(renderer);

                restoredRendererCount++;
            }

            Undo.CollapseUndoOperations(
                undoGroup);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Curve World",
                "Đã khôi phục material URP Lit gốc.\n\n" +
                "Renderer: " +
                restoredRendererCount +
                "\nMaterial: " +
                restoredMaterialCount,
                "OK");
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);

            Undo.CollapseUndoOperations(
                undoGroup);

            EditorUtility.DisplayDialog(
                "Curve World",
                "Có lỗi. Xem Console.",
                "OK");
        }
    }

    [MenuItem(
        "LEAD KHONG PHANH/Curve World/Restore Original URP Materials",
        true)]
    private static bool ValidateRestore()
    {
        return Selection.activeGameObject != null;
    }

    private static Material FindOriginalMaterial(
        string originalName)
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "t:Material " + originalName);

        for (int i = 0;
             i < guids.Length;
             i++)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guids[i]);

            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(
                    path);

            if (material == null)
                continue;

            if (material.name != originalName)
                continue;

            Shader shader =
                material.shader;

            if (shader == null)
                continue;

            if (shader.name ==
                    "Universal Render Pipeline/Lit" ||
                shader.name ==
                    "Universal Render Pipeline/Simple Lit")
            {
                return material;
            }
        }

        return null;
    }
}

#endif