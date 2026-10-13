#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

public static class CurveWorldRoadBatchSetup
{
    private const string TestShaderName =
        "LEAD KHONG PHANH/Curve World/URP Lit TEST";

    private const string GeneratedFolder =
        "Assets/Materials/CurveWorldTestGenerated";

    private const string TestSuffix =
        "_CurveWorld_TEST";

    // ============================================================
    // APPLY
    // ============================================================

    [MenuItem(
        "LEAD KHONG PHANH/Curve World/ROAD/Apply TEST Shader To Entire Road",
        priority = 1000)]
    private static void ApplyTestShaderToRoad()
    {
        GameObject root = Selection.activeGameObject;

        if (root == null)
        {
            EditorUtility.DisplayDialog(
                "Curve World",
                "Hãy chọn Road root / RoadChunk root trước.",
                "OK");

            return;
        }

        Shader testShader =
            Shader.Find(TestShaderName);

        if (testShader == null)
        {
            EditorUtility.DisplayDialog(
                "Curve World",
                "Không tìm thấy shader:\n\n" +
                TestShaderName +
                "\n\nHãy kiểm tra CurveWorldLit_TEST.shader.",
                "OK");

            return;
        }

        Renderer[] renderers =
            root.GetComponentsInChildren<Renderer>(true);

        if (renderers == null ||
            renderers.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "Curve World",
                "Không tìm thấy Renderer nào bên trong Road.",
                "OK");

            return;
        }

        EnsureGeneratedFolder();

        int changedRendererCount = 0;
        int changedMaterialCount = 0;
        int skippedMaterialCount = 0;

        Undo.IncrementCurrentGroup();

        int undoGroup =
            Undo.GetCurrentGroup();

        try
        {
            for (int r = 0;
                 r < renderers.Length;
                 r++)
            {
                Renderer renderer =
                    renderers[r];

                if (renderer == null)
                    continue;

                Material[] materials =
                    renderer.sharedMaterials;

                if (materials == null ||
                    materials.Length == 0)
                {
                    continue;
                }

                Material[] modified =
                    (Material[])materials.Clone();

                bool rendererChanged = false;

                for (int slot = 0;
                     slot < modified.Length;
                     slot++)
                {
                    Material source =
                        modified[slot];

                    if (source == null)
                        continue;

                    // ------------------------------------------------
                    // Đã phải TEST shader rồi -> bỏ qua
                    // ------------------------------------------------

                    if (source.shader == testShader ||
                        source.name.EndsWith(TestSuffix))
                    {
                        skippedMaterialCount++;
                        continue;
                    }

                    // ------------------------------------------------
                    // Chỉ xử lý material có shader URP Lit/Simple Lit
                    // ------------------------------------------------

                    Shader sourceShader =
                        source.shader;

                    if (sourceShader == null)
                        continue;

                    bool isSupportedShader =
                        sourceShader.name ==
                            "Universal Render Pipeline/Lit" ||
                        sourceShader.name ==
                            "Universal Render Pipeline/Simple Lit";

                    if (!isSupportedShader)
                    {
                        Debug.LogWarning(
                            "[CurveWorld Road] Bỏ qua material không phải URP Lit:\n" +
                            source.name +
                            "\nShader: " +
                            sourceShader.name,
                            source);

                        continue;
                    }

                    // ------------------------------------------------
                    // Clone material
                    // ------------------------------------------------

                    string materialName =
                        source.name + TestSuffix;

                    string assetPath =
                        GeneratedFolder +
                        "/" +
                        materialName +
                        ".mat";

                    Material testMaterial =
                        AssetDatabase.LoadAssetAtPath<Material>(
                            assetPath);

                    // ------------------------------------------------
                    // Nếu chưa tồn tại -> clone
                    // ------------------------------------------------

                    if (testMaterial == null)
                    {
                        testMaterial =
                            new Material(source);

                        testMaterial.name =
                            materialName;

                        testMaterial.shader =
                            testShader;

                        AssetDatabase.CreateAsset(
                            testMaterial,
                            assetPath);

                        changedMaterialCount++;
                    }
                    else
                    {
                        // ------------------------------------------------
                        // Asset đã tồn tại -> đảm bảo shader đúng
                        // ------------------------------------------------

                        if (testMaterial.shader != testShader)
                        {
                            testMaterial.shader =
                                testShader;

                            EditorUtility.SetDirty(
                                testMaterial);
                        }
                    }

                    modified[slot] =
                        testMaterial;

                    rendererChanged = true;
                }

                if (!rendererChanged)
                    continue;

                Undo.RecordObject(
                    renderer,
                    "Apply Curve World TEST Shader To Road");

                renderer.sharedMaterials =
                    modified;

                EditorUtility.SetDirty(
                    renderer);

                changedRendererCount++;
            }

            Undo.CollapseUndoOperations(
                undoGroup);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Curve World - ROAD",
                "Đã apply Curve World TEST shader cho Road.\n\n" +

                "Renderer xử lý: " +
                changedRendererCount +

                "\nMaterial TEST tạo mới: " +
                changedMaterialCount +

                "\nMaterial đã có / bỏ qua: " +
                skippedMaterialCount +

                "\n\nGameplay không bị thay đổi.",
                "OK");
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);

            Undo.CollapseUndoOperations(
                undoGroup);

            EditorUtility.DisplayDialog(
                "Curve World",
                "Có lỗi khi apply shader.\n\n" +
                "Xem Console để biết chi tiết.",
                "OK");
        }
    }

    // ============================================================
    // VALIDATE APPLY
    // ============================================================

    [MenuItem(
        "LEAD KHONG PHANH/Curve World/ROAD/Apply TEST Shader To Entire Road",
        true)]
    private static bool ValidateApply()
    {
        return Selection.activeGameObject != null;
    }

    // ============================================================
    // RESTORE
    // ============================================================

    [MenuItem(
        "LEAD KHONG PHANH/Curve World/ROAD/Restore Original URP Materials",
        priority = 1001)]
    private static void RestoreOriginalMaterials()
    {
        GameObject root =
            Selection.activeGameObject;

        if (root == null)
        {
            EditorUtility.DisplayDialog(
                "Curve World",
                "Hãy chọn Road root / RoadChunk root trước.",
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
            for (int r = 0;
                 r < renderers.Length;
                 r++)
            {
                Renderer renderer =
                    renderers[r];

                if (renderer == null)
                    continue;

                Material[] materials =
                    renderer.sharedMaterials;

                if (materials == null ||
                    materials.Length == 0)
                {
                    continue;
                }

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

                    if (!material.name.EndsWith(TestSuffix))
                        continue;

                    string originalName =
                        material.name.Substring(
                            0,
                            material.name.Length -
                            TestSuffix.Length);

                    Material original =
                        FindOriginalMaterial(
                            originalName);

                    if (original == null)
                    {
                        Debug.LogWarning(
                            "[CurveWorld Road] Không tìm thấy material gốc: " +
                            originalName,
                            material);

                        continue;
                    }

                    restored[slot] =
                        original;

                    restoredMaterialCount++;
                    changed = true;
                }

                if (!changed)
                    continue;

                Undo.RecordObject(
                    renderer,
                    "Restore Original Road Materials");

                renderer.sharedMaterials =
                    restored;

                EditorUtility.SetDirty(
                    renderer);

                restoredRendererCount++;
            }

            Undo.CollapseUndoOperations(
                undoGroup);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Curve World - ROAD",
                "Đã restore material URP Lit gốc.\n\n" +

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
                "Có lỗi khi restore.\n\n" +
                "Xem Console.",
                "OK");
        }
    }

    // ============================================================
    // VALIDATE RESTORE
    // ============================================================

    [MenuItem(
        "LEAD KHONG PHANH/Curve World/ROAD/Restore Original URP Materials",
        true)]
    private static bool ValidateRestore()
    {
        return Selection.activeGameObject != null;
    }

    // ============================================================
    // FIND ORIGINAL MATERIAL
    // ============================================================

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

    // ============================================================
    // CREATE GENERATED FOLDER
    // ============================================================

    private static void EnsureGeneratedFolder()
    {
        if (!AssetDatabase.IsValidFolder(
                "Assets/Materials"))
        {
            AssetDatabase.CreateFolder(
                "Assets",
                "Materials");
        }

        if (!AssetDatabase.IsValidFolder(
                GeneratedFolder))
        {
            AssetDatabase.CreateFolder(
                "Assets/Materials",
                "CurveWorldTestGenerated");
        }
    }
}

#endif