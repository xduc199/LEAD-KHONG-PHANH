#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class CurveWorldTestSetup
{
    private const string ShaderName =
        "LEAD KHONG PHANH/Curve World/URP Lit TEST";

    private const string GeneratedFolder =
        "Assets/Materials/CurveWorldTestGenerated";

    [MenuItem(
        "LEAD KHONG PHANH/Curve World/TEST 2 - Apply To Selected Renderer",
        priority = 10)]
    private static void ApplyToSelectedRenderer()
    {
        GameObject selected =
            Selection.activeGameObject;

        if (selected == null)
        {
            EditorUtility.DisplayDialog(
                "Curve World TEST",
                "Hãy chọn Renderer/Traffic prefab trước.",
                "OK");
            return;
        }

        Shader shader =
            Shader.Find(ShaderName);

        if (shader == null)
        {
            EditorUtility.DisplayDialog(
                "Curve World TEST",
                "Không tìm thấy shader:\n" + ShaderName,
                "OK");
            return;
        }

        EnsureFolder(GeneratedFolder);

        Renderer[] renderers =
            selected.GetComponentsInChildren<Renderer>(true);

        Dictionary<int, Material> cache =
            new Dictionary<int, Material>(32);

        int changed = 0;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null)
                continue;

            Material[] source =
                renderer.sharedMaterials;

            if (source == null ||
                source.Length == 0)
                continue;

            Material[] target = null;
            bool rendererChanged = false;

            for (int slot = 0;
                 slot < source.Length;
                 slot++)
            {
                Material material = source[slot];

                if (material == null)
                    continue;

                Shader sourceShader =
                    material.shader;

                if (sourceShader == null ||
                    sourceShader.name ==
                    ShaderName)
                    continue;

                int id =
                    material.GetInstanceID();

                Material converted;

                if (!cache.TryGetValue(
                        id,
                        out converted))
                {
                    converted =
                        new Material(material);

                    converted.name =
                        material.name +
                        "_CurveWorld_TEST";

                    converted.shader =
                        shader;

                    string path =
                        AssetDatabase.GenerateUniqueAssetPath(
                            GeneratedFolder +
                            "/" +
                            Sanitize(converted.name) +
                            ".mat");

                    AssetDatabase.CreateAsset(
                        converted,
                        path);

                    cache.Add(
                        id,
                        converted);
                }

                if (target == null)
                {
                    target =
                        (Material[])source.Clone();
                }

                target[slot] =
                    converted;

                rendererChanged = true;
            }

            if (!rendererChanged)
                continue;

            Undo.RecordObject(
                renderer,
                "Curve World TEST Material");

            renderer.sharedMaterials =
                target;

            EditorUtility.SetDirty(renderer);
            changed++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Curve World TEST",
            "Đã apply TEST shader cho " +
            changed +
            " Renderer.\n\n" +
            "Không thay Transform/Collider/Rigidbody.",
            "OK");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string[] parts =
            path.Split('/');

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

    private static string Sanitize(string value)
    {
        foreach (char c in
                 System.IO.Path.GetInvalidFileNameChars())
        {
            value = value.Replace(c, '_');
        }

        return value;
    }
}

#endif
