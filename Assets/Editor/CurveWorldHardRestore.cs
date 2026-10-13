#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

public static class CurveWorldHardRestore
{
    private const string CurveWorldPrefix =
        "LEAD KHONG PHANH/Curve World/";

    private const string TargetShaderName =
        "Universal Render Pipeline/Lit";

    private const string CurveWorldSuffix =
        "_CurveWorld";


    [MenuItem(
        "LEAD KHONG PHANH/Curve World/HARD RESTORE ALL CURVEWORLD MATERIALS",
        priority = 0)]
    private static void HardRestoreAll()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Curve World",
                "Hãy Stop Play Mode trước khi restore.",
                "OK");

            return;
        }

        Shader targetShader =
            Shader.Find(TargetShaderName);

        if (targetShader == null)
        {
            EditorUtility.DisplayDialog(
                "Curve World",
                "Không tìm thấy:\n\n" +
                TargetShaderName +
                "\n\nKiểm tra URP đang được cài và cấu hình.",
                "OK");

            return;
        }

        bool confirm = EditorUtility.DisplayDialog(
            "HARD RESTORE CURVE WORLD",
            "Tool sẽ tìm TOÀN BỘ Material đang dùng Curve World Shader.\n\n" +
            "Sau đó đổi trực tiếp Shader của chúng về:\n\n" +
            "Universal Render Pipeline/Lit\n\n" +
            "Không thay:\n" +
            "• Transform\n" +
            "• Mesh\n" +
            "• Collider\n" +
            "• Rigidbody\n" +
            "• Prefab hierarchy\n" +
            "• Gameplay\n" +
            "• Spawn\n\n" +
            "Tiếp tục?",
            "RESTORE",
            "HỦY");

        if (!confirm)
            return;


        int scanned = 0;
        int restored = 0;
        int generatedRestored = 0;

        string[] guids =
            AssetDatabase.FindAssets("t:Material");

        try
        {
            for (int i = 0; i < guids.Length; i++)
            {
                string path =
                    AssetDatabase.GUIDToAssetPath(
                        guids[i]);

                if (string.IsNullOrEmpty(path))
                    continue;

                EditorUtility.DisplayProgressBar(
                    "Hard Restore Curve World",
                    path,
                    (float)i /
                    Mathf.Max(1, guids.Length));

                Material material =
                    AssetDatabase.LoadAssetAtPath<Material>(
                        path);

                if (material == null)
                    continue;

                scanned++;

                Shader shader =
                    material.shader;

                if (shader == null)
                    continue;

                bool isCurveWorld =
                    shader.name.StartsWith(
                        CurveWorldPrefix,
                        StringComparison.Ordinal);

                if (!isCurveWorld)
                    continue;

                Undo.RecordObject(
                    material,
                    "Restore Curve World Material");

                material.shader =
                    targetShader;

                EditorUtility.SetDirty(material);

                restored++;

                if (material.name.EndsWith(
                        CurveWorldSuffix,
                        StringComparison.OrdinalIgnoreCase))
                {
                    generatedRestored++;
                }

                Debug.Log(
                    "[CurveWorld HARD RESTORE] " +
                    material.name +
                    " -> " +
                    targetShader.name,
                    material);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.ClearProgressBar();

            EditorUtility.DisplayDialog(
                "HARD RESTORE COMPLETE",
                "Đã khôi phục Material.\n\n" +
                "Material đã quét: " +
                scanned +
                "\n\n" +
                "CurveWorld Material đã restore: " +
                restored +
                "\n\n" +
                "Generated _CurveWorld Material: " +
                generatedRestored +
                "\n\n" +
                "Bây giờ mở lại DUONGMOI để kiểm tra Shader.",
                "OK");
        }
        catch (Exception exception)
        {
            EditorUtility.ClearProgressBar();

            Debug.LogException(exception);

            EditorUtility.DisplayDialog(
                "HARD RESTORE ERROR",
                "Có lỗi trong quá trình restore.\n\n" +
                "Xem Console.",
                "OK");
        }
    }


    [MenuItem(
        "LEAD KHONG PHANH/Curve World/HARD RESTORE ALL CURVEWORLD MATERIALS",
        true)]
    private static bool ValidateHardRestore()
    {
        return !EditorApplication.isPlaying;
    }
}

#endif