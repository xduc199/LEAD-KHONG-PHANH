#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class MagnetVFXSetup
{
    // ============================================================
    // PATHS
    // ============================================================

    private const string RootPath =
        "Assets/VFX/Magnet";

    private const string EditorPath =
        RootPath + "/Editor";

    private const string MaterialPath =
        RootPath + "/Materials";

    private const string TexturePath =
        RootPath + "/Textures";

    private const string MeshPath =
        RootPath + "/Meshes";

    private const string PrefabFolder =
        RootPath + "/Prefabs";

    private const string PrefabPath =
        PrefabFolder + "/PF_MagnetVFX.prefab";

    // ============================================================
    // COLORS
    // ============================================================

    private static readonly Color ElectricBlue =
        new Color(
            0.04f,
            0.36f,
            1.00f,
            1.00f
        );

    private static readonly Color Cyan =
        new Color(
            0.00f,
            0.86f,
            1.00f,
            1.00f
        );

    private static readonly Color BrightCyan =
        new Color(
            0.42f,
            1.00f,
            1.00f,
            1.00f
        );

    private static readonly Color WhiteEnergy =
        new Color(
            0.90f,
            0.99f,
            1.00f,
            1.00f
        );

    private static readonly Color DeepMagneticBlue =
        new Color(
            0.015f,
            0.12f,
            0.42f,
            1.00f
        );

    // ============================================================
    // MENU
    // ============================================================

    [MenuItem(
        "Tools/LEAD KHÔNG PHANH/VFX/Magnet/Create Magnet VFX"
    )]
    private static void CreateMagnetVFX()
    {
        EnsureFolders();

        CreateTextures();
        CreateMaterials();
        CreateMeshes();

        GameObject root =
            NewChild(
                (GameObject)null,
                "MagnetVFX"
            );

        root.transform.localPosition =
            Vector3.zero;

        root.transform.localRotation =
            Quaternion.identity;

        root.transform.localScale =
            Vector3.one;

        // ========================================================
        // CORE
        // ========================================================

        Transform core =
            NewChild(
                root,
                "Core"
            ).transform;

        CreateMagnetCore(
            core
        );

        CreateCorePulse(
            core
        );

        CreateCoreGlow(
            core
        );

        // ========================================================
        // VORTEX
        // ========================================================

        Transform vortex =
            NewChild(
                root,
                "Vortex"
            ).transform;

        CreateVortexSpiral(
            vortex,
            "Spiral_01",
            0f,
            1.15f,
            0.85f,
            0.020f,
            0.85f
        );

        CreateVortexSpiral(
            vortex,
            "Spiral_02",
            Mathf.PI * 2f / 3f,
            1.05f,
            0.78f,
            0.014f,
            0.55f
        );

        CreateVortexSpiral(
            vortex,
            "Spiral_03",
            Mathf.PI * 4f / 3f,
            0.92f,
            0.70f,
            0.010f,
            0.38f
        );

        // ========================================================
        // MAGNETIC FLUX
        // ========================================================

        Transform flux =
            NewChild(
                root,
                "MagneticFlux"
            ).transform;

        CreateFluxLine(
            flux,
            "FluxLine_01",
            0f,
            1.0f,
            0.022f
        );

        CreateFluxLine(
            flux,
            "FluxLine_02",
            Mathf.PI * 0.5f,
            0.92f,
            0.016f
        );

        CreateFluxLine(
            flux,
            "FluxLine_03",
            Mathf.PI,
            1.08f,
            0.013f
        );

        CreateFluxLine(
            flux,
            "FluxLine_04",
            Mathf.PI * 1.5f,
            0.88f,
            0.010f
        );

        // ========================================================
        // ATTRACTION STREAMS
        // ========================================================

        Transform attraction =
            NewChild(
                root,
                "Attraction"
            ).transform;

        CreateAttractionStream(
            attraction,
            "Stream_01",
            new Vector3(
                1.45f,
                0.62f,
                0.35f
            ),
            new Vector3(
                0.10f,
                0.08f,
                0.05f
            ),
            0.030f
        );

        CreateAttractionStream(
            attraction,
            "Stream_02",
            new Vector3(
                -1.40f,
                0.45f,
                -0.30f
            ),
            new Vector3(
                -0.08f,
                0.10f,
                0.04f
            ),
            0.024f
        );

        CreateAttractionStream(
            attraction,
            "Stream_03",
            new Vector3(
                0.75f,
                -1.05f,
                0.25f
            ),
            new Vector3(
                0.05f,
                -0.08f,
                0.04f
            ),
            0.020f
        );

        CreateAttractionStream(
            attraction,
            "Stream_04",
            new Vector3(
                -0.72f,
                -0.82f,
                -0.20f
            ),
            new Vector3(
                -0.05f,
                -0.08f,
                0.03f
            ),
            0.016f
        );

        // ========================================================
        // POLARITY
        // ========================================================

        Transform polarity =
            NewChild(
                root,
                "Polarity"
            ).transform;

        CreatePole(
            polarity,
            "NorthPole",
            new Vector3(
                0f,
                0.90f,
                0f
            ),
            true
        );

        CreatePole(
            polarity,
            "SouthPole",
            new Vector3(
                0f,
                -0.90f,
                0f
            ),
            false
        );

        CreatePoleConnection(
            polarity
        );

        // ========================================================
        // PULSE
        // ========================================================

        Transform pulse =
            NewChild(
                root,
                "Pulse"
            ).transform;

        CreatePulseRing(
            pulse
        );

        CreatePulseParticles(
            pulse
        );

        // ========================================================
        // PARTICLES
        // ========================================================

        Transform particles =
            NewChild(
                root,
                "Particles"
            ).transform;

        CreateMagneticDust(
            particles
        );

        CreateEnergyParticles(
            particles
        );

        CreateOrbitingParticles(
            particles
        );

        // ========================================================
        // LIGHT
        // ========================================================

        Transform lightRoot =
            NewChild(
                root,
                "Light"
            ).transform;

        GameObject lightObject =
            NewChild(
                lightRoot.gameObject,
                "MagnetLight"
            );

        Light light =
            lightObject.AddComponent<Light>();

        light.type =
            LightType.Point;

        light.color =
            Cyan;

        light.intensity =
            1.25f;

        light.range =
            3.0f;

        light.shadows =
            LightShadows.None;

        // ========================================================
        // SAVE
        // ========================================================

        PrefabUtility.SaveAsPrefabAsset(
            root,
            PrefabPath
        );

        UnityEngine.Object.DestroyImmediate(
            root
        );

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                PrefabPath
            );

        Debug.Log(
            "[MagnetVFXSetup] Created Magnetic Vortex VFX:\n" +
            PrefabPath
        );
    }

    // ============================================================
    // ATTACH TO PLAYER
    // ============================================================

    [MenuItem(
        "Tools/LEAD KHÔNG PHANH/VFX/Magnet/Attach To Selection"
    )]
    private static void AttachToSelection()
    {
        GameObject selected =
            Selection.activeGameObject;

        if (selected == null)
        {
            Debug.LogWarning(
                "[MagnetVFXSetup] Select Player first."
            );

            return;
        }

        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                PrefabPath
            );

        if (prefab == null)
        {
            Debug.LogWarning(
                "[MagnetVFXSetup] " +
                "PF_MagnetVFX not found. " +
                "Create Magnet VFX first."
            );

            return;
        }

        GameObject instance =
            PrefabUtility.InstantiatePrefab(
                prefab
            ) as GameObject;

        if (instance == null)
        {
            Debug.LogError(
                "[MagnetVFXSetup] Failed to instantiate prefab."
            );

            return;
        }

        Undo.RegisterCreatedObjectUndo(
            instance,
            "Attach Magnet VFX"
        );

        instance.transform.SetParent(
            selected.transform,
            false
        );

        instance.transform.localPosition =
            new Vector3(
                0f,
                0.35f,
                -0.05f
            );

        instance.transform.localRotation =
            Quaternion.identity;

        instance.transform.localScale =
            Vector3.one;

        Selection.activeGameObject =
            instance;

        Debug.Log(
            "[MagnetVFXSetup] Attached Magnetic Vortex " +
            "to " +
            selected.name
        );
    }

    // ============================================================
    // FOLDERS
    // ============================================================

    private static void EnsureFolders()
    {
        EnsureAssetFolder(
            RootPath
        );

        EnsureAssetFolder(
            EditorPath
        );

        EnsureAssetFolder(
            MaterialPath
        );

        EnsureAssetFolder(
            TexturePath
        );

        EnsureAssetFolder(
            MeshPath
        );

        EnsureAssetFolder(
            PrefabFolder
        );
    }

    private static void EnsureAssetFolder(
        string path)
    {
        string[] parts =
            path.Split('/');

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
                    parts[i]
                );
            }

            current =
                next;
        }
    }

    // ============================================================
    // TEXTURES
    // ============================================================

    private static void CreateTextures()
    {
        CreateTextureAsset(
            TexturePath +
            "/T_Magnet_CoreGlow.png",
            CreateRadialTexture(128)
        );

        CreateTextureAsset(
            TexturePath +
            "/T_Magnet_Spark.png",
            CreateRadialTexture(64)
        );

        CreateTextureAsset(
            TexturePath +
            "/T_Magnet_Pulse.png",
            CreatePulseTexture(128)
        );
    }

    private static Texture2D CreateRadialTexture(
        int size)
    {
        Texture2D texture =
            new Texture2D(
                size,
                size,
                TextureFormat.RGBA32,
                false,
                true
            );

        texture.wrapMode =
            TextureWrapMode.Clamp;

        texture.filterMode =
            FilterMode.Bilinear;

        Color[] pixels =
            new Color[size * size];

        float center =
            (size - 1) * 0.5f;

        float radius =
            center;

        for (int y = 0;
             y < size;
             y++)
        {
            for (int x = 0;
                 x < size;
                 x++)
            {
                float dx =
                    x - center;

                float dy =
                    y - center;

                float distance =
                    Mathf.Sqrt(
                        dx * dx +
                        dy * dy
                    ) /
                    radius;

                float alpha =
                    Mathf.Clamp01(
                        1f - distance
                    );

                alpha =
                    Mathf.Pow(
                        alpha,
                        2.4f
                    );

                pixels[
                    y * size + x
                ] =
                    new Color(
                        1f,
                        1f,
                        1f,
                        alpha
                    );
            }
        }

        texture.SetPixels(
            pixels
        );

        texture.Apply(
            false,
            false
        );

        return texture;
    }

    private static Texture2D CreatePulseTexture(
        int size)
    {
        Texture2D texture =
            new Texture2D(
                size,
                size,
                TextureFormat.RGBA32,
                false,
                true
            );

        texture.wrapMode =
            TextureWrapMode.Clamp;

        texture.filterMode =
            FilterMode.Bilinear;

        Color[] pixels =
            new Color[size * size];

        float center =
            (size - 1) * 0.5f;

        float radius =
            center;

        for (int y = 0;
             y < size;
             y++)
        {
            for (int x = 0;
                 x < size;
                 x++)
            {
                float dx =
                    x - center;

                float dy =
                    y - center;

                float distance =
                    Mathf.Sqrt(
                        dx * dx +
                        dy * dy
                    ) /
                    radius;

                float ring =
                    Mathf.Abs(
                        distance - 0.62f
                    );

                float alpha =
                    Mathf.Clamp01(
                        1f -
                        ring * 28f
                    );

                alpha =
                    Mathf.Pow(
                        alpha,
                        2f
                    );

                pixels[
                    y * size + x
                ] =
                    new Color(
                        1f,
                        1f,
                        1f,
                        alpha
                    );
            }
        }

        texture.SetPixels(
            pixels
        );

        texture.Apply(
            false,
            false
        );

        return texture;
    }

    private static void CreateTextureAsset(
        string path,
        Texture2D texture)
    {
        byte[] png =
            texture.EncodeToPNG();

        File.WriteAllBytes(
            path,
            png
        );

        UnityEngine.Object.DestroyImmediate(
            texture
        );

        AssetDatabase.ImportAsset(
            path,
            ImportAssetOptions.ForceUpdate
        );

        TextureImporter importer =
            AssetImporter.GetAtPath(
                path
            ) as TextureImporter;

        if (importer != null)
        {
            importer.textureType =
                TextureImporterType.Default;

            importer.alphaIsTransparency =
                true;

            importer.mipmapEnabled =
                false;

            importer.filterMode =
                FilterMode.Bilinear;

            importer.wrapMode =
                TextureWrapMode.Clamp;

            importer.SaveAndReimport();
        }
    }

    // ============================================================
    // MATERIALS
    // ============================================================

    private static void CreateMaterials()
    {
        Texture2D glow =
            AssetDatabase.LoadAssetAtPath<Texture2D>(
                TexturePath +
                "/T_Magnet_CoreGlow.png"
            );

        Texture2D spark =
            AssetDatabase.LoadAssetAtPath<Texture2D>(
                TexturePath +
                "/T_Magnet_Spark.png"
            );

        Texture2D pulse =
            AssetDatabase.LoadAssetAtPath<Texture2D>(
                TexturePath +
                "/T_Magnet_Pulse.png"
            );

        CreateTransparentMaterial(
            "M_Magnet_Core",
            glow,
            ElectricBlue,
            0.80f
        );

        CreateAdditiveMaterial(
            "M_Magnet_CoreGlow",
            glow,
            Cyan,
            0.55f
        );

        CreateAdditiveMaterial(
            "M_Magnet_Vortex",
            glow,
            Cyan,
            0.70f
        );

        CreateAdditiveMaterial(
            "M_Magnet_Flux",
            glow,
            BrightCyan,
            0.58f
        );

        CreateAdditiveMaterial(
            "M_Magnet_Attraction",
            glow,
            WhiteEnergy,
            0.85f
        );

        CreateAdditiveMaterial(
            "M_Magnet_Pole",
            spark,
            WhiteEnergy,
            0.90f
        );

        CreateAdditiveMaterial(
            "M_Magnet_Pulse",
            pulse,
            Cyan,
            0.60f
        );

        CreateAdditiveMaterial(
            "M_Magnet_Dust",
            spark,
            Cyan,
            0.28f
        );

        CreateAdditiveMaterial(
            "M_Magnet_Spark",
            spark,
            WhiteEnergy,
            0.90f
        );

        CreateTransparentMaterial(
            "M_Magnet_DeepCore",
            glow,
            DeepMagneticBlue,
            0.95f
        );
    }

    private static Material CreateTransparentMaterial(
        string materialName,
        Texture2D texture,
        Color color,
        float alpha)
    {
        Material material =
            CreateBaseMaterial(
                materialName,
                color,
                alpha
            );

        ConfigureTransparent(
            material
        );

        AssignTexture(
            material,
            texture
        );

        return material;
    }

    private static Material CreateAdditiveMaterial(
        string materialName,
        Texture2D texture,
        Color color,
        float alpha)
    {
        Material material =
            CreateBaseMaterial(
                materialName,
                color,
                alpha
            );

        ConfigureAdditive(
            material
        );

        AssignTexture(
            material,
            texture
        );

        return material;
    }

    private static Material CreateBaseMaterial(
        string materialName,
        Color color,
        float alpha)
    {
        string path =
            MaterialPath +
            "/" +
            materialName +
            ".mat";

        Material material =
            AssetDatabase.LoadAssetAtPath<Material>(
                path
            );

        if (material == null)
        {
            Shader shader =
                Shader.Find(
                    "LEADKhongPhanh/ShieldHex"
                );

            if (shader == null)
            {
                shader =
                    Shader.Find(
                        "Universal Render Pipeline/Unlit"
                    );
            }

            material =
                new Material(
                    shader
                );

            material.name =
                materialName;

            AssetDatabase.CreateAsset(
                material,
                path
            );
        }

        Color finalColor =
            new Color(
                color.r,
                color.g,
                color.b,
                alpha
            );

        if (material.HasProperty(
                "_BaseColor"))
        {
            material.SetColor(
                "_BaseColor",
                finalColor
            );
        }

        if (material.HasProperty(
                "_Color"))
        {
            material.SetColor(
                "_Color",
                finalColor
            );
        }

        return material;
    }

    private static void ConfigureTransparent(
        Material material)
    {
        if (material.HasProperty(
                "_Surface"))
        {
            material.SetFloat(
                "_Surface",
                1f
            );
        }

        if (material.HasProperty(
                "_Blend"))
        {
            material.SetFloat(
                "_Blend",
                0f
            );
        }

        if (material.HasProperty(
                "_SrcBlend"))
        {
            material.SetFloat(
                "_SrcBlend",
                (float)BlendMode.SrcAlpha
            );
        }

        if (material.HasProperty(
                "_DstBlend"))
        {
            material.SetFloat(
                "_DstBlend",
                (float)BlendMode.OneMinusSrcAlpha
            );
        }

        if (material.HasProperty(
                "_ZWrite"))
        {
            material.SetFloat(
                "_ZWrite",
                0f
            );
        }

        material.SetOverrideTag(
            "RenderType",
            "Transparent"
        );

        material.EnableKeyword(
            "_SURFACE_TYPE_TRANSPARENT"
        );

        material.renderQueue =
            3000;
    }

    private static void ConfigureAdditive(
        Material material)
    {
        if (material.HasProperty(
                "_Surface"))
        {
            material.SetFloat(
                "_Surface",
                1f
            );
        }

        if (material.HasProperty(
                "_Blend"))
        {
            material.SetFloat(
                "_Blend",
                1f
            );
        }

        if (material.HasProperty(
                "_SrcBlend"))
        {
            material.SetFloat(
                "_SrcBlend",
                (float)BlendMode.SrcAlpha
            );
        }

        if (material.HasProperty(
                "_DstBlend"))
        {
            material.SetFloat(
                "_DstBlend",
                (float)BlendMode.One
            );
        }

        if (material.HasProperty(
                "_ZWrite"))
        {
            material.SetFloat(
                "_ZWrite",
                0f
            );
        }

        material.SetOverrideTag(
            "RenderType",
            "Transparent"
        );

        material.EnableKeyword(
            "_SURFACE_TYPE_TRANSPARENT"
        );

        material.renderQueue =
            3000;
    }

    private static void AssignTexture(
        Material material,
        Texture texture)
    {
        if (texture == null)
        {
            return;
        }

        if (material.HasProperty(
                "_BaseMap"))
        {
            material.SetTexture(
                "_BaseMap",
                texture
            );
        }

        if (material.HasProperty(
                "_MainTex"))
        {
            material.SetTexture(
                "_MainTex",
                texture
            );
        }
    }

    // ============================================================
    // CORE
    // ============================================================

    private static void CreateMagnetCore(
        Transform parent)
    {
        GameObject core =
            NewChild(
                parent,
                "MagnetCore"
            );

        MeshFilter filter =
            core.AddComponent<MeshFilter>();

        MeshRenderer renderer =
            core.AddComponent<MeshRenderer>();

        filter.sharedMesh =
            AssetDatabase.LoadAssetAtPath<Mesh>(
                MeshPath +
                "/SM_Magnet_Core.asset"
            );

        renderer.sharedMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(
                MaterialPath +
                "/M_Magnet_Core.mat"
            );

        core.transform.localScale =
            Vector3.one * 0.30f;

        DisableRendererFeatures(
            renderer
        );

        GameObject deepCore =
            NewChild(
                core,
                "DeepCore"
            );

        MeshFilter deepFilter =
            deepCore.AddComponent<MeshFilter>();

        MeshRenderer deepRenderer =
            deepCore.AddComponent<MeshRenderer>();

        deepFilter.sharedMesh =
            filter.sharedMesh;

        deepRenderer.sharedMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(
                MaterialPath +
                "/M_Magnet_DeepCore.mat"
            );

        deepCore.transform.localScale =
            Vector3.one * 0.72f;

        DisableRendererFeatures(
            deepRenderer
        );
    }

    private static void CreateCoreGlow(
        Transform parent)
    {
        GameObject glow =
            NewChild(
                parent,
                "CoreGlow"
            );

        MeshFilter filter =
            glow.AddComponent<MeshFilter>();

        MeshRenderer renderer =
            glow.AddComponent<MeshRenderer>();

        filter.sharedMesh =
            AssetDatabase.LoadAssetAtPath<Mesh>(
                MeshPath +
                "/SM_Magnet_Core.asset"
            );

        renderer.sharedMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(
                MaterialPath +
                "/M_Magnet_CoreGlow.mat"
            );

        glow.transform.localScale =
            Vector3.one * 0.52f;

        DisableRendererFeatures(
            renderer
        );
    }

    private static void CreateCorePulse(
        Transform parent)
    {
        GameObject pulse =
            NewChild(
                parent,
                "CorePulse"
            );

        MeshFilter filter =
            pulse.AddComponent<MeshFilter>();

        MeshRenderer renderer =
            pulse.AddComponent<MeshRenderer>();

        filter.sharedMesh =
            AssetDatabase.LoadAssetAtPath<Mesh>(
                MeshPath +
                "/SM_Magnet_Core.asset"
            );

        renderer.sharedMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(
                MaterialPath +
                "/M_Magnet_Vortex.mat"
            );

        pulse.transform.localScale =
            Vector3.one * 0.38f;

        DisableRendererFeatures(
            renderer
        );
    }

    // ============================================================
    // VORTEX
    // ============================================================

    private static void CreateVortexSpiral(
        Transform parent,
        string objectName,
        float phase,
        float radius,
        float height,
        float width,
        float alpha)
    {
        GameObject objectRoot =
            NewChild(
                parent,
                objectName
            );

        LineRenderer line =
            objectRoot.AddComponent<LineRenderer>();

        line.useWorldSpace =
            false;

        line.loop =
            false;

        const int points = 96;

        line.positionCount =
            points;

        line.startWidth =
            width;

        line.endWidth =
            width * 0.10f;

        line.numCapVertices =
            3;

        line.numCornerVertices =
            3;

        line.material =
            AssetDatabase.LoadAssetAtPath<Material>(
                MaterialPath +
                "/M_Magnet_Vortex.mat"
            );

        line.shadowCastingMode =
            ShadowCastingMode.Off;

        line.receiveShadows =
            false;

        Gradient gradient =
            new Gradient();

        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(
                    ElectricBlue,
                    0f
                ),

                new GradientColorKey(
                    Cyan,
                    0.45f
                ),

                new GradientColorKey(
                    WhiteEnergy,
                    0.72f
                ),

                new GradientColorKey(
                    ElectricBlue,
                    1f
                )
            },
            new[]
            {
                new GradientAlphaKey(
                    0f,
                    0f
                ),

                new GradientAlphaKey(
                    alpha,
                    0.15f
                ),

                new GradientAlphaKey(
                    alpha,
                    0.68f
                ),

                new GradientAlphaKey(
                    0f,
                    1f
                )
            }
        );

        line.colorGradient =
            gradient;

        for (int i = 0;
             i < points;
             i++)
        {
            float t =
                i /
                (float)(points - 1);

            float angle =
                phase +
                t *
                Mathf.PI *
                2.65f;

            float currentRadius =
                Mathf.Lerp(
                    radius,
                    0.18f,
                    t
                );

            float currentHeight =
                Mathf.Lerp(
                    height,
                    0.02f,
                    t
                );

            float wave =
                Mathf.Sin(
                    t *
                    Mathf.PI *
                    5f +
                    phase
                ) *
                0.035f;

            Vector3 position =
                new Vector3(
                    Mathf.Cos(angle) *
                    currentRadius,

                    currentHeight +
                    wave,

                    Mathf.Sin(angle) *
                    currentRadius
                );

            line.SetPosition(
                i,
                position
            );
        }
    }

    // ============================================================
    // FLUX
    // ============================================================

    private static void CreateFluxLine(
        Transform parent,
        string objectName,
        float phase,
        float radius,
        float width)
    {
        GameObject objectRoot =
            NewChild(
                parent,
                objectName
            );

        LineRenderer line =
            objectRoot.AddComponent<LineRenderer>();

        line.useWorldSpace =
            false;

        line.loop =
            false;

        const int points = 70;

        line.positionCount =
            points;

        line.startWidth =
            width;

        line.endWidth =
            0f;

        line.numCapVertices =
            2;

        line.numCornerVertices =
            2;

        line.material =
            AssetDatabase.LoadAssetAtPath<Material>(
                MaterialPath +
                "/M_Magnet_Flux.mat"
            );

        line.shadowCastingMode =
            ShadowCastingMode.Off;

        line.receiveShadows =
            false;

        Gradient gradient =
            new Gradient();

        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(
                    Cyan,
                    0f
                ),

                new GradientColorKey(
                    BrightCyan,
                    0.55f
                ),

                new GradientColorKey(
                    WhiteEnergy,
                    1f
                )
            },
            new[]
            {
                new GradientAlphaKey(
                    0f,
                    0f
                ),

                new GradientAlphaKey(
                    0.42f,
                    0.20f
                ),

                new GradientAlphaKey(
                    0.55f,
                    0.60f
                ),

                new GradientAlphaKey(
                    0f,
                    1f
                )
            }
        );

        line.colorGradient =
            gradient;

        for (int i = 0;
             i < points;
             i++)
        {
            float t =
                i /
                (float)(points - 1);

            float angle =
                phase +
                t *
                Mathf.PI *
                2f;

            float currentRadius =
                Mathf.Lerp(
                    radius,
                    0.16f,
                    t
                );

            float y =
                Mathf.Sin(
                    t *
                    Mathf.PI
                ) *
                0.82f;

            Vector3 position =
                new Vector3(
                    Mathf.Cos(angle) *
                    currentRadius,

                    y,

                    Mathf.Sin(angle) *
                    currentRadius
                );

            line.SetPosition(
                i,
                position
            );
        }
    }

    // ============================================================
    // ATTRACTION STREAM
    // ============================================================

    private static void CreateAttractionStream(
        Transform parent,
        string objectName,
        Vector3 start,
        Vector3 bend,
        float width)
    {
        GameObject objectRoot =
            NewChild(
                parent,
                objectName
            );

        LineRenderer line =
            objectRoot.AddComponent<LineRenderer>();

        line.useWorldSpace =
            false;

        line.loop =
            false;

        const int points = 32;

        line.positionCount =
            points;

        line.startWidth =
            width;

        line.endWidth =
            0f;

        line.numCapVertices =
            3;

        line.numCornerVertices =
            3;

        line.material =
            AssetDatabase.LoadAssetAtPath<Material>(
                MaterialPath +
                "/M_Magnet_Attraction.mat"
            );

        line.shadowCastingMode =
            ShadowCastingMode.Off;

        line.receiveShadows =
            false;

        Gradient gradient =
            new Gradient();

        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(
                    Cyan,
                    0f
                ),

                new GradientColorKey(
                    WhiteEnergy,
                    0.70f
                ),

                new GradientColorKey(
                    BrightCyan,
                    1f
                )
            },
            new[]
            {
                new GradientAlphaKey(
                    0f,
                    0f
                ),

                new GradientAlphaKey(
                    0.70f,
                    0.16f
                ),

                new GradientAlphaKey(
                    0.95f,
                    0.72f
                ),

                new GradientAlphaKey(
                    0f,
                    1f
                )
            }
        );

        line.colorGradient =
            gradient;

        Vector3 end =
            Vector3.zero;

        for (int i = 0;
             i < points;
             i++)
        {
            float t =
                i /
                (float)(points - 1);

            float inverse =
                1f - t;

            Vector3 position =
                Vector3.Lerp(
                    start,
                    end,
                    t
                );

            float curve =
                Mathf.Sin(
                    t *
                    Mathf.PI
                );

            position +=
                bend *
                curve *
                inverse;

            float spiral =
                Mathf.Sin(
                    t *
                    Mathf.PI *
                    4f
                ) *
                0.045f *
                inverse;

            position.x +=
                spiral;

            position.z +=
                spiral *
                0.7f;

            line.SetPosition(
                i,
                position
            );
        }
    }

    // ============================================================
    // POLARITY
    // ============================================================

    private static void CreatePole(
        Transform parent,
        string objectName,
        Vector3 position,
        bool north)
    {
        GameObject pole =
            NewChild(
                parent,
                objectName
            );

        pole.transform.localPosition =
            position;

        pole.transform.localScale =
            Vector3.one *
            0.075f;

        MeshFilter filter =
            pole.AddComponent<MeshFilter>();

        MeshRenderer renderer =
            pole.AddComponent<MeshRenderer>();

        filter.sharedMesh =
            AssetDatabase.LoadAssetAtPath<Mesh>(
                MeshPath +
                "/SM_Magnet_Node.asset"
            );

        renderer.sharedMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(
                MaterialPath +
                "/M_Magnet_Pole.mat"
            );

        DisableRendererFeatures(
            renderer
        );

        GameObject inner =
            NewChild(
                pole,
                north
                    ? "N_Core"
                    : "S_Core"
            );

        MeshFilter innerFilter =
            inner.AddComponent<MeshFilter>();

        MeshRenderer innerRenderer =
            inner.AddComponent<MeshRenderer>();

        innerFilter.sharedMesh =
            filter.sharedMesh;

        innerRenderer.sharedMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(
                MaterialPath +
                "/M_Magnet_Spark.mat"
            );

        inner.transform.localScale =
            Vector3.one * 1.8f;

        DisableRendererFeatures(
            innerRenderer
        );
    }

    private static void CreatePoleConnection(
        Transform parent)
    {
        GameObject objectRoot =
            NewChild(
                parent,
                "PoleConnection"
            );

        LineRenderer line =
            objectRoot.AddComponent<LineRenderer>();

        line.useWorldSpace =
            false;

        line.positionCount =
            34;

        line.startWidth =
            0.010f;

        line.endWidth =
            0.010f;

        line.material =
            AssetDatabase.LoadAssetAtPath<Material>(
                MaterialPath +
                "/M_Magnet_Flux.mat"
            );

        line.shadowCastingMode =
            ShadowCastingMode.Off;

        line.receiveShadows =
            false;

        for (int i = 0;
             i < 34;
             i++)
        {
            float t =
                i / 33f;

            float y =
                Mathf.Lerp(
                    -0.84f,
                    0.84f,
                    t
                );

            float x =
                Mathf.Sin(
                    t *
                    Mathf.PI *
                    3f
                ) *
                0.055f;

            float z =
                Mathf.Cos(
                    t *
                    Mathf.PI *
                    2f
                ) *
                0.035f;

            line.SetPosition(
                i,
                new Vector3(
                    x,
                    y,
                    z
                )
            );
        }
    }

    // ============================================================
    // PULSE
    // ============================================================

    private static void CreatePulseRing(
        Transform parent)
    {
        GameObject objectRoot =
            NewChild(
                parent,
                "PulseRing"
            );

        LineRenderer line =
            objectRoot.AddComponent<LineRenderer>();

        line.useWorldSpace =
            false;

        line.loop =
            true;

        const int points = 96;

        line.positionCount =
            points;

        line.startWidth =
            0.012f;

        line.endWidth =
            0.012f;

        line.material =
            AssetDatabase.LoadAssetAtPath<Material>(
                MaterialPath +
                "/M_Magnet_Pulse.mat"
            );

        line.shadowCastingMode =
            ShadowCastingMode.Off;

        line.receiveShadows =
            false;

        float radius =
            0.62f;

        for (int i = 0;
             i < points;
             i++)
        {
            float angle =
                i /
                (float)points *
                Mathf.PI *
                2f;

            line.SetPosition(
                i,
                new Vector3(
                    Mathf.Cos(angle) *
                    radius,

                    0f,

                    Mathf.Sin(angle) *
                    radius
                )
            );
        }
    }

    // ============================================================
    // PARTICLES
    // ============================================================

    private static ParticleSystem CreateParticleSystem(
        Transform parent,
        string objectName,
        Material material,
        int maxParticles,
        float lifetime,
        float size,
        float emissionRate)
    {
        GameObject objectRoot =
            NewChild(
                parent,
                objectName
            );

        ParticleSystem ps =
            objectRoot.AddComponent<ParticleSystem>();

        ParticleSystemRenderer renderer =
            objectRoot.AddComponent<
                ParticleSystemRenderer
            >();

        renderer.renderMode =
            ParticleSystemRenderMode.Billboard;

        renderer.material =
            material;

        ParticleSystem.MainModule main =
            ps.main;

        main.maxParticles =
            maxParticles;

        main.loop =
            true;

        main.startLifetime =
            lifetime;

        main.startSize =
            size;

        main.startSpeed =
            0f;

        main.simulationSpace =
            ParticleSystemSimulationSpace.Local;

        ParticleSystem.EmissionModule emission =
            ps.emission;

        emission.enabled =
            true;

        emission.rateOverTime =
            emissionRate;

        ParticleSystem.ShapeModule shape =
            ps.shape;

        shape.enabled =
            true;

        shape.shapeType =
            ParticleSystemShapeType.Sphere;

        shape.radius =
            1.15f;

        return ps;
    }

    private static void CreateMagneticDust(
        Transform parent)
    {
        Material material =
            AssetDatabase.LoadAssetAtPath<Material>(
                MaterialPath +
                "/M_Magnet_Dust.mat"
            );

        ParticleSystem ps =
            CreateParticleSystem(
                parent,
                "MagneticDust",
                material,
                80,
                1.2f,
                0.020f,
                24f
            );

        ParticleSystem.VelocityOverLifetimeModule velocity =
            ps.velocityOverLifetime;

        velocity.enabled =
            true;

        velocity.orbitalY =
            0.45f;

        velocity.radial =
            -0.22f;

        ParticleSystem.ColorOverLifetimeModule color =
            ps.colorOverLifetime;

        color.enabled =
            true;

        Gradient gradient =
            new Gradient();

        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(
                    Cyan,
                    0f
                ),

                new GradientColorKey(
                    BrightCyan,
                    0.55f
                )
            },
            new[]
            {
                new GradientAlphaKey(
                    0f,
                    0f
                ),

                new GradientAlphaKey(
                    0.45f,
                    0.20f
                ),

                new GradientAlphaKey(
                    0f,
                    1f
                )
            }
        );

        color.color =
            gradient;
    }

    private static void CreateEnergyParticles(
        Transform parent)
    {
        Material material =
            AssetDatabase.LoadAssetAtPath<Material>(
                MaterialPath +
                "/M_Magnet_Spark.mat"
            );

        ParticleSystem ps =
            CreateParticleSystem(
                parent,
                "EnergyParticles",
                material,
                38,
                0.60f,
                0.035f,
                12f
            );

        ParticleSystem.VelocityOverLifetimeModule velocity =
            ps.velocityOverLifetime;

        velocity.enabled =
            true;

        velocity.orbitalY =
            0.85f;

        velocity.radial =
            -0.50f;

        ParticleSystem.ColorOverLifetimeModule color =
            ps.colorOverLifetime;

        color.enabled =
            true;

        Gradient gradient =
            new Gradient();

        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(
                    WhiteEnergy,
                    0f
                ),

                new GradientColorKey(
                    Cyan,
                    1f
                )
            },
            new[]
            {
                new GradientAlphaKey(
                    0f,
                    0f
                ),

                new GradientAlphaKey(
                    0.95f,
                    0.15f
                ),

                new GradientAlphaKey(
                    0f,
                    1f
                )
            }
        );

        color.color =
            gradient;
    }

    private static void CreateOrbitingParticles(
        Transform parent)
    {
        Material material =
            AssetDatabase.LoadAssetAtPath<Material>(
                MaterialPath +
                "/M_Magnet_Spark.mat"
            );

        ParticleSystem ps =
            CreateParticleSystem(
                parent,
                "OrbitingParticles",
                material,
                24,
                1.4f,
                0.028f,
                8f
            );

        ParticleSystem.VelocityOverLifetimeModule velocity =
            ps.velocityOverLifetime;

        velocity.enabled =
            true;

        velocity.orbitalX =
            0.25f;

        velocity.orbitalY =
            0.65f;

        velocity.orbitalZ =
            0.20f;

        velocity.radial =
            -0.18f;
    }

    private static void CreatePulseParticles(
        Transform parent)
    {
        Material material =
            AssetDatabase.LoadAssetAtPath<Material>(
                MaterialPath +
                "/M_Magnet_Spark.mat"
            );

        ParticleSystem ps =
            CreateParticleSystem(
                parent,
                "PulseParticles",
                material,
                18,
                0.45f,
                0.040f,
                5f
            );

        ParticleSystem.ShapeModule shape =
            ps.shape;

        shape.shapeType =
            ParticleSystemShapeType.Sphere;

        shape.radius =
            0.25f;

        ParticleSystem.VelocityOverLifetimeModule velocity =
            ps.velocityOverLifetime;

        velocity.enabled =
            true;

        velocity.radial =
            1.2f;
    }

    // ============================================================
    // MESH
    // ============================================================

    private static void CreateMeshes()
    {
        SaveMesh(
            BuildIcosphere(2),
            MeshPath +
            "/SM_Magnet_Core.asset"
        );

        SaveMesh(
            BuildNodeMesh(),
            MeshPath +
            "/SM_Magnet_Node.asset"
        );
    }

    private static Mesh BuildNodeMesh()
    {
        Mesh mesh =
            new Mesh();

        Vector3[] vertices =
        {
            new Vector3(
                0f,
                0f,
                0.14f
            ),

            new Vector3(
                -0.08f,
                0f,
                -0.08f
            ),

            new Vector3(
                0.08f,
                0f,
                -0.08f
            ),

            new Vector3(
                0f,
                0.08f,
                -0.08f
            ),

            new Vector3(
                0f,
                -0.08f,
                -0.08f
            )
        };

        int[] triangles =
        {
            0, 1, 3,
            0, 3, 2,
            0, 4, 1,
            0, 2, 4,
            1, 4, 3,
            1, 2, 4
        };

        mesh.vertices =
            vertices;

        mesh.triangles =
            triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        mesh.name =
            "SM_Magnet_Node";

        return mesh;
    }

    private static Mesh BuildIcosphere(
        int subdivisions)
    {
        Mesh mesh =
            new Mesh();

        float t =
            (1f + Mathf.Sqrt(5f)) /
            2f;

        List<Vector3> vertices =
            new List<Vector3>
            {
                new Vector3(-1, t, 0),
                new Vector3(1, t, 0),
                new Vector3(-1, -t, 0),
                new Vector3(1, -t, 0),

                new Vector3(0, -1, t),
                new Vector3(0, 1, t),
                new Vector3(0, -1, -t),
                new Vector3(0, 1, -t),

                new Vector3(t, 0, -1),
                new Vector3(t, 0, 1),
                new Vector3(-t, 0, -1),
                new Vector3(-t, 0, 1)
            };

        for (int i = 0;
             i < vertices.Count;
             i++)
        {
            vertices[i] =
                vertices[i].normalized;
        }

        List<int> triangles =
            new List<int>
            {
                0,11,5,
                0,5,1,
                0,1,7,
                0,7,10,
                0,10,11,

                1,5,9,
                5,11,4,
                11,10,2,
                10,7,6,
                7,1,8,

                3,9,4,
                3,4,2,
                3,2,6,
                3,6,8,
                3,8,9,

                4,9,5,
                2,4,11,
                6,2,10,
                8,6,7,
                9,8,1
            };

        for (int subdivision = 0;
             subdivision < subdivisions;
             subdivision++)
        {
            Dictionary<long, int> cache =
                new Dictionary<long, int>();

            List<int> newTriangles =
                new List<int>(
                    triangles.Count * 4
                );

            for (int i = 0;
                 i < triangles.Count;
                 i += 3)
            {
                int a =
                    triangles[i];

                int b =
                    triangles[i + 1];

                int c =
                    triangles[i + 2];

                int ab =
                    GetMidpoint(
                        a,
                        b,
                        vertices,
                        cache
                    );

                int bc =
                    GetMidpoint(
                        b,
                        c,
                        vertices,
                        cache
                    );

                int ca =
                    GetMidpoint(
                        c,
                        a,
                        vertices,
                        cache
                    );

                newTriangles.Add(a);
                newTriangles.Add(ab);
                newTriangles.Add(ca);

                newTriangles.Add(b);
                newTriangles.Add(bc);
                newTriangles.Add(ab);

                newTriangles.Add(c);
                newTriangles.Add(ca);
                newTriangles.Add(bc);

                newTriangles.Add(ab);
                newTriangles.Add(bc);
                newTriangles.Add(ca);
            }

            triangles =
                newTriangles;
        }

        mesh.SetVertices(
            vertices
        );

        mesh.SetTriangles(
            triangles,
            0
        );

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        mesh.name =
            "SM_Magnet_Core";

        return mesh;
    }

    private static int GetMidpoint(
        int indexA,
        int indexB,
        List<Vector3> vertices,
        Dictionary<long, int> cache)
    {
        int smaller =
            Mathf.Min(
                indexA,
                indexB
            );

        int larger =
            Mathf.Max(
                indexA,
                indexB
            );

        long key =
            ((long)smaller << 32) |
            (uint)larger;

        if (cache.TryGetValue(
                key,
                out int existing
            ))
        {
            return existing;
        }

        Vector3 midpoint =
            (
                vertices[indexA] +
                vertices[indexB]
            ) *
            0.5f;

        midpoint.Normalize();

        int index =
            vertices.Count;

        vertices.Add(
            midpoint
        );

        cache.Add(
            key,
            index
        );

        return index;
    }

    private static void SaveMesh(
        Mesh mesh,
        string path)
    {
        Mesh oldMesh =
            AssetDatabase.LoadAssetAtPath<Mesh>(
                path
            );

        if (oldMesh != null)
        {
            AssetDatabase.DeleteAsset(
                path
            );
        }

        AssetDatabase.CreateAsset(
            mesh,
            path
        );
    }

    // ============================================================
    // HELPERS
    // ============================================================

    private static GameObject NewChild(
        GameObject parent,
        string objectName)
    {
        GameObject objectRoot =
            new GameObject(
                objectName
            );

        if (parent != null)
        {
            objectRoot.transform.SetParent(
                parent.transform,
                false
            );
        }

        return objectRoot;
    }

    private static GameObject NewChild(
        Transform parent,
        string objectName)
    {
        GameObject objectRoot =
            new GameObject(
                objectName
            );

        if (parent != null)
        {
            objectRoot.transform.SetParent(
                parent,
                false
            );
        }

        return objectRoot;
    }

    private static void DisableRendererFeatures(
        Renderer renderer)
    {
        renderer.shadowCastingMode =
            ShadowCastingMode.Off;

        renderer.receiveShadows =
            false;

        renderer.lightProbeUsage =
            LightProbeUsage.Off;

        renderer.reflectionProbeUsage =
            ReflectionProbeUsage.Off;
    }
}

#endif