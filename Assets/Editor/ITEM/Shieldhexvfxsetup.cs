// =============================================================================
// LEAD KHÔNG PHANH
// ShieldHexVFXSetup.cs
//
// SHIELD DESIGN (revision):
// - Shield chính: cầu xanh dương nhạt, trong suốt, mỏng.
// - Hex field: hai lớp lục giác (primary + secondary) phân bố theo thuật toán
//   Fibonacci-sphere để bám sát độ cong hình cầu, không xếp thành hàng phẳng.
// - Hex field nằm bao quanh shield theo dạng spherical shell nhiều lớp depth.
// - Có các dòng năng lượng (magnetic field rings, gồm 1 vòng chéo) chạy quanh.
// - Có các energy shard / power fragments bay quanh shield.
// - Có các energy arcs ngắn, mảnh, bám theo bề mặt shell.
// - Có spark / dust năng lượng nhẹ, giới hạn quanh shell bằng shape confinement.
// - Không dùng billboard cho shield chính.
// - Không dùng VelocityOverLifetimeModule.separateAxes.
// - Editor setup tự tạo:
//      Textures
//      Materials
//      Meshes
//      Particle Systems
//      Prefab
//
// MENU:
// Tools
//   / LEAD KHÔNG PHANH
//       / VFX
//           / Create Hex Shield
//           / Attach Hex Shield To Selection
//
// OUTPUT:
// Assets/VFX/ShieldHex/
// =============================================================================

#if UNITY_EDITOR

using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class ShieldHexVFXSetup
{
    // ============================================================
    // PATHS
    // ============================================================

    private const string RootPath = "Assets/VFX/ShieldHex";
    private const string MaterialPath = RootPath + "/Materials";
    private const string TexturePath = RootPath + "/Textures";
    private const string MeshPath = RootPath + "/Meshes";
    private const string PrefabPath = RootPath + "/Prefabs/PF_ShieldHex.prefab";

    // ============================================================
    // DEPTH / RADIUS LAYOUT
    //
    // Thứ tự depth (từ Player ra ngoài):
    // Core -> InnerGlow -> Primary Hex -> Secondary Hex ->
    // Thin Blue Shell -> Outer Glow -> Magnetic Field
    // ============================================================

    private const float CoreRadius = 1.46f;
    private const float InnerGlowRadius = 1.47f;
    private const float PrimaryHexRadius = 1.50f;
    private const float SecondaryHexRadius = 1.535f;
    private const float ThinBlueShellRadius = 1.57f;
    private const float OuterGlowRadius = 1.62f;

    // Dùng làm mốc bán kính chung cho arc / particle shell / hit point.
    private const float ShellReferenceRadius = 1.58f;

    private const float MagneticRingRadiusX = 1.66f;
    private const float MagneticRingRadiusY = 1.70f;
    private const float MagneticRingRadiusZ = 1.74f;
    private const float MagneticRingRadiusDiagonal = 1.72f;

    // ============================================================
    // HEX FIELD DENSITY
    //
    // Có thể tăng/giảm 2 hằng số count bên dưới để làm hex field dày
    // hơn hoặc nhẹ hơn mà không cần đụng vào thuật toán phân bố.
    // ============================================================

    private const int PrimaryHexCount = 120;
    private const int SecondaryHexCount = 90;
    private const int PoleHexCountPerPole = 4;

    private const float PrimaryHexScale = 0.27f;
    private const float SecondaryHexScale = 0.20f;
    private const float PoleHexScale = 0.16f;

    // ============================================================
    // COLORS
    //
    // Palette chính: #8CD8FF / #A9E7FF / #D6F5FF (pale cyan blue).
    // ============================================================

    private static readonly Color ShieldBlue =
        new Color(0.549f, 0.847f, 1.00f, 1.00f); // #8CD8FF

    private static readonly Color ShieldBlueBright =
        new Color(0.663f, 0.906f, 1.00f, 1.00f); // #A9E7FF

    private static readonly Color ShieldWhiteBlue =
        new Color(0.839f, 0.961f, 1.00f, 1.00f); // #D6F5FF

    private static readonly Color EnergyBlue =
        new Color(0.42f, 0.80f, 1.00f, 1.00f);

    private static readonly Color SecondaryHexColor =
        new Color(0.58f, 0.72f, 0.85f, 1.00f); // Dim Light Blue

    // ============================================================
    // MENU
    // ============================================================

    [MenuItem("Tools/LEAD KHÔNG PHANH/VFX/Create Hex Shield")]
    private static void Create()
    {
        EnsureFolders();

        CreateTextures();
        CreateMaterials();
        BuildMeshes();

        GameObject root = NewChild((GameObject)null, "ShieldHexVFX");

        // --------------------------------------------------------
        // SHIELD CORE
        // --------------------------------------------------------

        Transform shieldCore = NewChild(root, "ShieldCore").transform;

        CreateShieldSphere(shieldCore);

        CreateGlowSphere(
            shieldCore,
            "ThinBlueLayer",
            "M_Shield_ThinBlueLayer",
            ThinBlueShellRadius
        );

        CreateGlowSphere(
            shieldCore,
            "InnerGlow",
            "M_Shield_Core",
            InnerGlowRadius
        );

        CreateGlowSphere(
            shieldCore,
            "OuterGlow",
            "M_Shield_OuterSoftGlow",
            OuterGlowRadius
        );

        // --------------------------------------------------------
        // HEX FIELD
        // --------------------------------------------------------

        Transform hexField = NewChild(root, "HexField").transform;

        CreateHexagonalShell(hexField);

        // --------------------------------------------------------
        // MAGNETIC FIELD
        // --------------------------------------------------------

        Transform magneticField =
            NewChild(root, "MagneticField").transform;

        CreateMagneticOrbit(
            magneticField,
            "FieldRing_X",
            Vector3.right,
            MagneticRingRadiusX,
            0.008f,
            0.20f
        );

        CreateMagneticOrbit(
            magneticField,
            "FieldRing_Y",
            Vector3.up,
            MagneticRingRadiusY,
            0.008f,
            0.16f
        );

        CreateMagneticOrbit(
            magneticField,
            "FieldRing_Z",
            Vector3.forward,
            MagneticRingRadiusZ,
            0.008f,
            0.13f
        );

        CreateMagneticOrbit(
            magneticField,
            "FieldRing_Diagonal",
            new Vector3(1f, 0.6f, 0.4f).normalized,
            MagneticRingRadiusDiagonal,
            0.007f,
            0.10f
        );

        // --------------------------------------------------------
        // ENERGY ARCS
        // --------------------------------------------------------

        Transform energyArcs =
            NewChild(root, "EnergyArcs").transform;

        CreateArc(
            energyArcs,
            "Arc_01",
            SphericalPoint(ShellReferenceRadius, 55f, 15f),
            SphericalPoint(ShellReferenceRadius, 48f, 50f),
            0.050f
        );

        CreateArc(
            energyArcs,
            "Arc_02",
            SphericalPoint(ShellReferenceRadius, 122f, 205f),
            SphericalPoint(ShellReferenceRadius, 130f, 240f),
            0.045f
        );

        CreateArc(
            energyArcs,
            "Arc_03",
            SphericalPoint(ShellReferenceRadius, 78f, 295f),
            SphericalPoint(ShellReferenceRadius, 64f, 322f),
            0.045f
        );

        CreateArc(
            energyArcs,
            "Arc_04",
            SphericalPoint(ShellReferenceRadius, 98f, 112f),
            SphericalPoint(ShellReferenceRadius, 90f, 142f),
            0.050f
        );

        // --------------------------------------------------------
        // ABILITY ENERGY
        // --------------------------------------------------------

        Transform abilityEnergy =
            NewChild(root, "AbilityEnergy").transform;

        CreateFlyingEnergyShard(
            abilityEnergy,
            "Ability_01",
            new Vector3(0.00f, 1.42f, 0.00f),
            new Vector3(0.00f, 1.75f, 0.08f)
        );

        CreateFlyingEnergyShard(
            abilityEnergy,
            "Ability_02",
            new Vector3(0.95f, 0.65f, 0.20f),
            new Vector3(1.25f, 0.82f, 0.10f)
        );

        CreateFlyingEnergyShard(
            abilityEnergy,
            "Ability_03",
            new Vector3(-0.95f, 0.50f, -0.15f),
            new Vector3(-1.28f, 0.72f, 0.12f)
        );

        CreateFlyingEnergyShard(
            abilityEnergy,
            "Ability_04",
            new Vector3(0.82f, -0.75f, 0.05f),
            new Vector3(1.10f, -1.02f, 0.15f)
        );

        CreateFlyingEnergyShard(
            abilityEnergy,
            "Ability_05",
            new Vector3(-0.80f, -0.70f, 0.15f),
            new Vector3(-1.12f, -0.95f, 0.10f)
        );

        CreateFlyingEnergyShard(
            abilityEnergy,
            "Ability_06",
            new Vector3(0.10f, -1.38f, -0.10f),
            new Vector3(0.15f, -1.72f, 0.08f)
        );

        // --------------------------------------------------------
        // PARTICLES
        // --------------------------------------------------------

        Transform particles =
            NewChild(root, "Particles").transform;

        CreateHexSparkles(particles);
        CreateEdgeSparkles(particles);
        CreateEnergyDust(particles);
        CreateAbilityParticles(particles);

        // --------------------------------------------------------
        // HIT POINTS
        // --------------------------------------------------------

        Transform hitPoints =
            NewChild(root, "HitPoints").transform;

        for (int i = 0; i < 4; i++)
        {
            CreateHitPoint(
                hitPoints,
                "HitSlot_" + i,
                i
            );
        }

        // --------------------------------------------------------
        // LIGHT
        // --------------------------------------------------------

        Transform lightRoot =
            NewChild(root, "Light").transform;

        GameObject lightObject =
            NewChild(lightRoot.gameObject, "ShieldLight");

        Light light =
            lightObject.AddComponent<Light>();

        light.type = LightType.Point;
        light.range = 3.2f;
        light.intensity = 0.85f;
        light.color = ShieldBlueBright;
        light.shadows = LightShadows.None;

        // --------------------------------------------------------
        // MAIN CONTROLLER
        // --------------------------------------------------------

        TryAddComponent<ShieldHexVFX>(root);

        // --------------------------------------------------------
        // SAVE PREFAB
        // --------------------------------------------------------

        string prefabDirectory =
            Path.GetDirectoryName(PrefabPath);

        if (!string.IsNullOrEmpty(prefabDirectory))
        {
            EnsureAssetFolder(prefabDirectory);
        }

        PrefabUtility.SaveAsPrefabAsset(
            root,
            PrefabPath
        );

        Selection.activeObject =
            AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

        UnityEngine.Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "[ShieldHexVFXSetup] Shield Hex VFX created successfully:\n" +
            PrefabPath
        );
    }

    // ============================================================
    // ATTACH TO SELECTION
    // ============================================================

    [MenuItem("Tools/LEAD KHÔNG PHANH/VFX/Attach Hex Shield To Selection")]
    private static void AttachToSelection()
    {
        GameObject selected = Selection.activeGameObject;

        if (selected == null)
        {
            Debug.LogWarning(
                "[ShieldHexVFXSetup] Select a Player GameObject first."
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
                "[ShieldHexVFXSetup] Prefab not found. " +
                "Create the shield first."
            );

            return;
        }

        GameObject instance =
            PrefabUtility.InstantiatePrefab(prefab) as GameObject;

        if (instance == null)
        {
            Debug.LogError(
                "[ShieldHexVFXSetup] Failed to instantiate shield prefab."
            );

            return;
        }

        Undo.RegisterCreatedObjectUndo(
            instance,
            "Attach Hex Shield"
        );

        instance.transform.SetParent(
            selected.transform,
            false
        );

        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        Selection.activeGameObject = instance;

        Debug.Log(
            "[ShieldHexVFXSetup] Shield attached to: " +
            selected.name
        );
    }

    // ============================================================
    // FOLDERS
    // ============================================================

    private static void EnsureFolders()
    {
        EnsureAssetFolder(RootPath);
        EnsureAssetFolder(MaterialPath);
        EnsureAssetFolder(TexturePath);
        EnsureAssetFolder(MeshPath);
        EnsureAssetFolder(RootPath + "/Prefabs");
    }

    private static void EnsureAssetFolder(string path)
    {
        string[] parts = path.Split('/');

        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];

            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(
                    current,
                    parts[i]
                );
            }

            current = next;
        }
    }

    // ============================================================
    // TEXTURES
    // ============================================================

    private static void CreateTextures()
    {
        CreateTextureAsset(
            TexturePath + "/T_Shield_Sparkle.png",
            CreateRadialTexture(64)
        );

        CreateTextureAsset(
            TexturePath + "/T_Shield_Hex.png",
            CreateHexTexture(128)
        );

        CreateTextureAsset(
            TexturePath + "/T_Shield_Glow.png",
            CreateRadialTexture(128)
        );
    }

    private static Texture2D CreateRadialTexture(int size)
    {
        Texture2D texture =
            new Texture2D(
                size,
                size,
                TextureFormat.RGBA32,
                false,
                true
            );

        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color[] pixels =
            new Color[size * size];

        float center =
            (size - 1) * 0.5f;

        float radius =
            center;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;

                float distance =
                    Mathf.Sqrt(dx * dx + dy * dy) /
                    radius;

                float alpha =
                    Mathf.Clamp01(1.0f - distance);

                alpha *= alpha;

                pixels[y * size + x] =
                    new Color(
                        1f,
                        1f,
                        1f,
                        alpha
                    );
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, false);

        return texture;
    }

    private static Texture2D CreateHexTexture(int size)
    {
        Texture2D texture =
            new Texture2D(
                size,
                size,
                TextureFormat.RGBA32,
                false,
                true
            );

        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;

        Color[] pixels =
            new Color[size * size];

        float center =
            size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px =
                    (x - center) /
                    size;

                float py =
                    (y - center) /
                    size;

                float angle =
                    Mathf.Atan2(py, px);

                float radius =
                    Mathf.Sqrt(
                        px * px +
                        py * py
                    );

                float hex =
                    Mathf.Cos(
                        angle * 6f
                    );

                float edge =
                    Mathf.Abs(
                        radius -
                        0.22f -
                        hex * 0.012f
                    );

                float alpha =
                    1f -
                    Mathf.Clamp01(
                        edge * 90f
                    );

                pixels[y * size + x] =
                    new Color(
                        1f,
                        1f,
                        1f,
                        alpha
                    );
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, false);

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

            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
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
        Texture2D sparkle =
            AssetDatabase.LoadAssetAtPath<Texture2D>(
                TexturePath + "/T_Shield_Sparkle.png"
            );

        Texture2D hex =
            AssetDatabase.LoadAssetAtPath<Texture2D>(
                TexturePath + "/T_Shield_Hex.png"
            );

        Texture2D glow =
            AssetDatabase.LoadAssetAtPath<Texture2D>(
                TexturePath + "/T_Shield_Glow.png"
            );

        CreateShieldCoreMaterial(
            "M_Shield_Core",
            ShieldBlue,
            0.11f
        );

        CreateAdditiveMaterial(
            "M_Shield_Hex",
            hex,
            ShieldBlueBright,
            0.42f
        );

        CreateAdditiveMaterial(
            "M_Shield_HexDim",
            hex,
            SecondaryHexColor,
            0.14f
        );

        CreateAdditiveMaterial(
            "M_Shield_Energy",
            glow,
            EnergyBlue,
            0.65f
        );

        CreateAdditiveMaterial(
            "M_Shield_Sparkle",
            sparkle,
            ShieldWhiteBlue,
            0.65f
        );

        CreateAdditiveMaterial(
            "M_Shield_Dust",
            sparkle,
            ShieldBlueBright,
            0.22f
        );

        // Thin transparent blue spherical layer.
        CreateSoftSphereMaterial(
            "M_Shield_ThinBlueLayer",
            glow,
            ShieldBlueBright,
            0.085f
        );

        CreateSoftSphereMaterial(
            "M_Shield_OuterSoftGlow",
            glow,
            ShieldBlue,
            0.035f
        );
    }

    private static Material CreateShieldCoreMaterial(
        string materialName,
        Color color,
        float alpha)
    {
        Material material =
            CreateBaseMaterial(
                materialName,
                color,
                alpha
            );

        ApplyTransparentBlend(
            material
        );

        material.SetFloat(
            "_ZWrite",
            0f
        );

        material.SetFloat(
            "_Cull",
            (float)CullMode.Back
        );

        material.renderQueue = 3000;

        return material;
    }

    private static Material CreateSoftSphereMaterial(
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

        ApplyTransparentBlend(
            material
        );

        if (texture != null)
        {
            SetTexture(
                material,
                "_BaseMap",
                texture
            );

            SetTexture(
                material,
                "_MainTex",
                texture
            );
        }

        material.SetFloat(
            "_ZWrite",
            0f
        );

        material.SetFloat(
            "_Cull",
            (float)CullMode.Back
        );

        material.renderQueue = 3000;

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

        ApplyAdditiveBlend(
            material
        );

        if (texture != null)
        {
            SetTexture(
                material,
                "_BaseMap",
                texture
            );

            SetTexture(
                material,
                "_MainTex",
                texture
            );
        }

        material.SetFloat(
            "_ZWrite",
            0f
        );

        material.SetFloat(
            "_Cull",
            (float)CullMode.Off
        );

        material.renderQueue = 3000;

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
                new Material(shader);

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

        SetColor(
            material,
            "_BaseColor",
            finalColor
        );

        SetColor(
            material,
            "_Color",
            finalColor
        );

        return material;
    }

    private static void ApplyTransparentBlend(
        Material material)
    {
        if (material.HasProperty("_Surface"))
        {
            material.SetFloat(
                "_Surface",
                1f
            );
        }

        if (material.HasProperty("_Blend"))
        {
            material.SetFloat(
                "_Blend",
                0f
            );
        }

        if (material.HasProperty("_SrcBlend"))
        {
            material.SetFloat(
                "_SrcBlend",
                (float)BlendMode.SrcAlpha
            );
        }

        if (material.HasProperty("_DstBlend"))
        {
            material.SetFloat(
                "_DstBlend",
                (float)BlendMode.OneMinusSrcAlpha
            );
        }

        if (material.HasProperty("_AlphaClip"))
        {
            material.SetFloat(
                "_AlphaClip",
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

        material.DisableKeyword(
            "_ALPHATEST_ON"
        );

        material.renderQueue = 3000;
    }

    private static void ApplyAdditiveBlend(
        Material material)
    {
        if (material.HasProperty("_Surface"))
        {
            material.SetFloat(
                "_Surface",
                1f
            );
        }

        if (material.HasProperty("_Blend"))
        {
            material.SetFloat(
                "_Blend",
                1f
            );
        }

        if (material.HasProperty("_SrcBlend"))
        {
            material.SetFloat(
                "_SrcBlend",
                (float)BlendMode.SrcAlpha
            );
        }

        if (material.HasProperty("_DstBlend"))
        {
            material.SetFloat(
                "_DstBlend",
                (float)BlendMode.One
            );
        }

        material.SetOverrideTag(
            "RenderType",
            "Transparent"
        );

        material.EnableKeyword(
            "_SURFACE_TYPE_TRANSPARENT"
        );

        material.renderQueue = 3000;
    }

    private static void SetColor(
        Material material,
        string property,
        Color color)
    {
        if (material.HasProperty(property))
        {
            material.SetColor(
                property,
                color
            );
        }
    }

    private static void SetTexture(
        Material material,
        string property,
        Texture texture)
    {
        if (material.HasProperty(property))
        {
            material.SetTexture(
                property,
                texture
            );
        }
    }

    // ============================================================
    // CORE
    // ============================================================

    private static void CreateShieldSphere(
        Transform parent)
    {
        GameObject objectRoot =
            NewChild(
                parent.gameObject,
                "ShieldSphere"
            );

        MeshFilter filter =
            objectRoot.AddComponent<MeshFilter>();

        MeshRenderer renderer =
            objectRoot.AddComponent<MeshRenderer>();

        filter.sharedMesh =
            AssetDatabase.LoadAssetAtPath<Mesh>(
                MeshPath + "/SM_Shield_Core.asset"
            );

        renderer.sharedMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(
                MaterialPath + "/M_Shield_Core.mat"
            );

        DisableRendererFeatures(
            renderer
        );

        objectRoot.transform.localScale =
            Vector3.one * CoreRadius;

        ShieldHexDome dome =
            objectRoot.AddComponent<ShieldHexDome>();

        if (dome != null)
        {
            renderer.enabled = false;
        }
    }

    private static void CreateGlowSphere(
        Transform parent,
        string objectName,
        string materialName,
        float scale)
    {
        GameObject objectRoot =
            NewChild(
                parent.gameObject,
                objectName
            );

        MeshFilter filter =
            objectRoot.AddComponent<MeshFilter>();

        MeshRenderer renderer =
            objectRoot.AddComponent<MeshRenderer>();

        filter.sharedMesh =
            AssetDatabase.LoadAssetAtPath<Mesh>(
                MeshPath + "/SM_Shield_Core.asset"
            );

        renderer.sharedMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(
                MaterialPath + "/" +
                materialName +
                ".mat"
            );

        DisableRendererFeatures(
            renderer
        );

        objectRoot.transform.localScale =
            Vector3.one * scale;

        renderer.enabled = false;
    }

    // ============================================================
    // SPHERE DISTRIBUTION HELPERS
    // ============================================================

    /// <summary>
    /// Sinh N điểm phân bố đều trên mặt cầu đơn vị theo thuật toán
    /// Fibonacci-sphere (golden angle). Kết quả bám sát độ cong hình
    /// cầu tự nhiên, không xếp thành hàng phẳng, các hàng tự lệch nhau.
    /// </summary>
    private static List<Vector3> GenerateFibonacciSpherePoints(
        int count,
        float minY,
        float maxY,
        float angleOffsetRadians)
    {
        List<Vector3> points =
            new List<Vector3>(count);

        float goldenAngle =
            Mathf.PI * (3f - Mathf.Sqrt(5f));

        for (int i = 0; i < count; i++)
        {
            float t =
                count <= 1
                    ? 0f
                    : i / (float)(count - 1);

            float y =
                Mathf.Lerp(maxY, minY, t);

            float ringRadius =
                Mathf.Sqrt(
                    Mathf.Max(
                        0f,
                        1f - y * y
                    )
                );

            float theta =
                goldenAngle * i +
                angleOffsetRadians;

            float x =
                Mathf.Cos(theta) * ringRadius;

            float z =
                Mathf.Sin(theta) * ringRadius;

            points.Add(
                new Vector3(x, y, z)
            );
        }

        return points;
    }

    /// <summary>
    /// Xoay một hex cell để mặt của nó hướng ra ngoài theo pháp tuyến
    /// mặt cầu, tránh trường hợp suy biến khi pháp tuyến gần song song
    /// với Vector3.up (ở hai cực).
    /// </summary>
    private static Quaternion HexCellRotation(
        Vector3 normal)
    {
        Vector3 upHint =
            Mathf.Abs(
                Vector3.Dot(
                    normal,
                    Vector3.up
                )
            ) > 0.98f
                ? Vector3.forward
                : Vector3.up;

        return Quaternion.LookRotation(
            normal,
            upHint
        );
    }

    /// <summary>
    /// Trả về một điểm trên mặt cầu bán kính "radius", với polarDeg là
    /// góc tính từ cực trên (+Y, 0 = đỉnh, 180 = đáy) và azimuthDeg là
    /// góc quay quanh trục Y. Dùng để đặt arc theo bề mặt shell.
    /// </summary>
    private static Vector3 SphericalPoint(
        float radius,
        float polarDeg,
        float azimuthDeg)
    {
        float polar =
            polarDeg * Mathf.Deg2Rad;

        float azimuth =
            azimuthDeg * Mathf.Deg2Rad;

        float y =
            Mathf.Cos(polar);

        float ringRadius =
            Mathf.Sin(polar);

        float x =
            Mathf.Cos(azimuth) * ringRadius;

        float z =
            Mathf.Sin(azimuth) * ringRadius;

        return new Vector3(x, y, z) * radius;
    }

    // ============================================================
    // HEXAGON FIELD
    // ============================================================

    private static void CreateHexagonalShell(
        Transform parent)
    {
        // --------------------------------------------------------
        // PRIMARY LAYER
        // --------------------------------------------------------

        GameObject primaryShell =
            NewChild(
                parent.gameObject,
                "HoneycombShell"
            );

        Material primaryMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(
                MaterialPath + "/M_Shield_Hex.mat"
            );

        CreateHexLayer(
            primaryShell.transform,
            "Hex_P",
            PrimaryHexCount,
            PrimaryHexRadius,
            -0.90f,
            0.90f,
            0f,
            PrimaryHexScale,
            primaryMaterial
        );

        CreatePoleCluster(
            primaryShell,
            "TopPole",
            Vector3.up,
            primaryMaterial
        );

        CreatePoleCluster(
            primaryShell,
            "BottomPole",
            Vector3.down,
            primaryMaterial
        );

        // --------------------------------------------------------
        // SECONDARY LAYER (sibling của HoneycombShell, không lồng
        // bên trong, đúng theo hierarchy mong muốn)
        // --------------------------------------------------------

        GameObject secondaryShell =
            NewChild(
                parent.gameObject,
                "SecondaryHexShell"
            );

        Material secondaryMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(
                MaterialPath + "/M_Shield_HexDim.mat"
            );

        // angleOffsetRadians ~ golden angle (137.5 deg) để lớp phụ
        // lệch pha với lớp chính, tránh chồng khít lên nhau.
        CreateHexLayer(
            secondaryShell.transform,
            "Hex_S",
            SecondaryHexCount,
            SecondaryHexRadius,
            -0.88f,
            0.88f,
            137.5f * Mathf.Deg2Rad,
            SecondaryHexScale,
            secondaryMaterial
        );
    }

    private static void CreateHexLayer(
        Transform parent,
        string namePrefix,
        int count,
        float radius,
        float minY,
        float maxY,
        float angleOffsetRadians,
        float cellScale,
        Material material)
    {
        List<Vector3> points =
            GenerateFibonacciSpherePoints(
                count,
                minY,
                maxY,
                angleOffsetRadians
            );

        Mesh hexMesh =
            AssetDatabase.LoadAssetAtPath<Mesh>(
                MeshPath + "/SM_Shield_HexCell.asset"
            );

        for (int i = 0; i < points.Count; i++)
        {
            Vector3 normal =
                points[i];

            Vector3 position =
                normal * radius;

            GameObject cell =
                NewChild(
                    parent.gameObject,
                    namePrefix + "_" + i
                );

            cell.transform.localPosition =
                position;

            cell.transform.localRotation =
                HexCellRotation(normal);

            cell.transform.localScale =
                Vector3.one * cellScale;

            MeshFilter filter =
                cell.AddComponent<MeshFilter>();

            MeshRenderer renderer =
                cell.AddComponent<MeshRenderer>();

            filter.sharedMesh =
                hexMesh;

            renderer.sharedMaterial =
                material;

            DisableRendererFeatures(
                renderer
            );
        }
    }

    private static void CreatePoleCluster(
        GameObject parent,
        string name,
        Vector3 direction,
        Material material)
    {
        GameObject cluster =
            NewChild(
                parent,
                name
            );

        Vector3 center =
            direction * PrimaryHexRadius;

        Mesh hexMesh =
            AssetDatabase.LoadAssetAtPath<Mesh>(
                MeshPath + "/SM_Shield_HexCell.asset"
            );

        for (int i = 0; i < PoleHexCountPerPole; i++)
        {
            float angle =
                i *
                Mathf.PI *
                2f /
                PoleHexCountPerPole;

            Vector3 offset =
                new Vector3(
                    Mathf.Cos(angle) *
                    0.11f,

                    0f,

                    Mathf.Sin(angle) *
                    0.11f
                );

            Vector3 position =
                center +
                offset;

            Vector3 normal =
                position.normalized;

            GameObject cell =
                NewChild(
                    cluster,
                    "PoleHex_" + i
                );

            cell.transform.localPosition =
                position;

            cell.transform.localRotation =
                HexCellRotation(normal);

            cell.transform.localScale =
                Vector3.one *
                PoleHexScale;

            MeshFilter filter =
                cell.AddComponent<MeshFilter>();

            MeshRenderer renderer =
                cell.AddComponent<MeshRenderer>();

            filter.sharedMesh =
                hexMesh;

            renderer.sharedMaterial =
                material;

            DisableRendererFeatures(
                renderer
            );
        }
    }

    // ============================================================
    // MAGNETIC ORBITS
    // ============================================================

    private static void CreateMagneticOrbit(
        Transform parent,
        string objectName,
        Vector3 axis,
        float radius,
        float width,
        float alpha)
    {
        GameObject objectRoot =
            NewChild(
                parent.gameObject,
                objectName
            );

        LineRenderer line =
            objectRoot.AddComponent<LineRenderer>();

        line.useWorldSpace = false;
        line.loop = true;

        line.positionCount = 96;

        line.startWidth = width;
        line.endWidth = width;

        line.numCapVertices = 2;
        line.numCornerVertices = 2;

        line.shadowCastingMode =
            ShadowCastingMode.Off;

        line.receiveShadows = false;

        line.material =
            AssetDatabase.LoadAssetAtPath<Material>(
                MaterialPath +
                "/M_Shield_Energy.mat"
            );

        Gradient gradient =
            new Gradient();

        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(
                    ShieldBlueBright,
                    0f
                ),

                new GradientColorKey(
                    ShieldWhiteBlue,
                    0.5f
                ),

                new GradientColorKey(
                    ShieldBlueBright,
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
                    0.18f
                ),

                new GradientAlphaKey(
                    alpha,
                    0.82f
                ),

                new GradientAlphaKey(
                    0f,
                    1f
                )
            }
        );

        line.colorGradient =
            gradient;

        Vector3 basisA;

        if (Mathf.Abs(
                Vector3.Dot(
                    axis.normalized,
                    Vector3.up
                )
            ) > 0.9f)
        {
            basisA =
                Vector3.right;
        }
        else
        {
            basisA =
                Vector3.up;
        }

        basisA =
            Vector3.ProjectOnPlane(
                basisA,
                axis
            ).normalized;

        Vector3 basisB =
            Vector3.Cross(
                axis.normalized,
                basisA
            ).normalized;

        for (int i = 0; i < 96; i++)
        {
            float angle =
                i /
                96f *
                Mathf.PI *
                2f;

            Vector3 position =
                (
                    basisA *
                    Mathf.Cos(angle)
                ) +
                (
                    basisB *
                    Mathf.Sin(angle)
                );

            line.SetPosition(
                i,
                position * radius
            );
        }
    }

    // ============================================================
    // ENERGY ARCS
    // ============================================================

    private static void CreateArc(
        Transform parent,
        string objectName,
        Vector3 start,
        Vector3 end,
        float scale)
    {
        GameObject objectRoot =
            NewChild(
                parent.gameObject,
                objectName
            );

        MeshFilter filter =
            objectRoot.AddComponent<MeshFilter>();

        MeshRenderer renderer =
            objectRoot.AddComponent<MeshRenderer>();

        filter.sharedMesh =
            AssetDatabase.LoadAssetAtPath<Mesh>(
                MeshPath +
                "/SM_Shield_EnergyArc.asset"
            );

        renderer.sharedMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(
                MaterialPath +
                "/M_Shield_Energy.mat"
            );

        objectRoot.transform.localPosition =
            (start + end) * 0.5f;

        Vector3 direction =
            end - start;

        objectRoot.transform.localRotation =
            Quaternion.LookRotation(
                direction.normalized
            );

        objectRoot.transform.localScale =
            new Vector3(
                scale,
                scale,
                direction.magnitude
            );

        DisableRendererFeatures(
            renderer
        );
    }

    // ============================================================
    // FLYING ENERGY
    // ============================================================

    private static void CreateFlyingEnergyShard(
        Transform parent,
        string objectName,
        Vector3 start,
        Vector3 end)
    {
        GameObject objectRoot =
            NewChild(
                parent.gameObject,
                objectName
            );

        MeshFilter filter =
            objectRoot.AddComponent<MeshFilter>();

        MeshRenderer renderer =
            objectRoot.AddComponent<MeshRenderer>();

        filter.sharedMesh =
            AssetDatabase.LoadAssetAtPath<Mesh>(
                MeshPath +
                "/SM_Shield_EnergyShard.asset"
            );

        renderer.sharedMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(
                MaterialPath +
                "/M_Shield_Energy.mat"
            );

        objectRoot.transform.localPosition =
            start;

        objectRoot.transform.localRotation =
            Quaternion.LookRotation(
                (end - start).normalized
            );

        objectRoot.transform.localScale =
            Vector3.one * 0.07f;

        TrailRenderer trail =
            objectRoot.AddComponent<TrailRenderer>();

        trail.time = 0.24f;
        trail.startWidth = 0.06f;
        trail.endWidth = 0f;

        trail.material =
            AssetDatabase.LoadAssetAtPath<Material>(
                MaterialPath +
                "/M_Shield_Energy.mat"
            );

        DisableRendererFeatures(
            renderer
        );
    }

    // ============================================================
    // PARTICLES
    // ============================================================

    private static ParticleSystem NewParticleSystem(
        Transform parent,
        string objectName,
        Material material,
        int maxParticles,
        float lifetime,
        float size,
        float emissionRate,
        float shapeRadius,
        float shapeRadiusThickness)
    {
        GameObject objectRoot =
            NewChild(
                parent.gameObject,
                objectName
            );

        ParticleSystem ps =
            objectRoot.AddComponent<ParticleSystem>();

        ParticleSystemRenderer renderer =
            objectRoot.AddComponent<ParticleSystemRenderer>();

        renderer.renderMode =
            ParticleSystemRenderMode.Billboard;

        renderer.material =
            material;

        ParticleSystem.MainModule main =
            ps.main;

        main.maxParticles =
            maxParticles;

        main.loop = true;

        main.startLifetime =
            lifetime;

        main.startSize =
            size;

        main.startSpeed = 0f;

        main.simulationSpace =
            ParticleSystemSimulationSpace.Local;

        ParticleSystem.EmissionModule emission =
            ps.emission;

        emission.enabled = true;

        emission.rateOverTime =
            emissionRate;

        ParticleSystem.ShapeModule shape =
            ps.shape;

        shape.enabled = true;

        shape.shapeType =
            ParticleSystemShapeType.Sphere;

        shape.radius = shapeRadius;

        shape.radiusThickness =
            shapeRadiusThickness;

        return ps;
    }

    private static void CreateHexSparkles(
        Transform parent)
    {
        Material material =
            AssetDatabase.LoadAssetAtPath<Material>(
                MaterialPath +
                "/M_Shield_Sparkle.mat"
            );

        ParticleSystem ps =
            NewParticleSystem(
                parent,
                "HexSparkles",
                material,
                50,
                0.9f,
                0.030f,
                14f,
                PrimaryHexRadius,
                0.15f
            );

        ParticleSystem.ColorOverLifetimeModule color =
            ps.colorOverLifetime;

        color.enabled = true;

        Gradient gradient =
            new Gradient();

        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(
                    ShieldWhiteBlue,
                    0f
                ),

                new GradientColorKey(
                    ShieldBlueBright,
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
                    0.85f,
                    0.3f
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

    private static void CreateEdgeSparkles(
        Transform parent)
    {
        Material material =
            AssetDatabase.LoadAssetAtPath<Material>(
                MaterialPath +
                "/M_Shield_Sparkle.mat"
            );

        NewParticleSystem(
            parent,
            "EdgeSparkles",
            material,
            40,
            0.6f,
            0.022f,
            10f,
            ThinBlueShellRadius,
            0.04f
        );
    }

    private static void CreateEnergyDust(
        Transform parent)
    {
        Material material =
            AssetDatabase.LoadAssetAtPath<Material>(
                MaterialPath +
                "/M_Shield_Dust.mat"
            );

        NewParticleSystem(
            parent,
            "EnergyDust",
            material,
            45,
            1.1f,
            0.016f,
            10f,
            OuterGlowRadius,
            1f
        );
    }

    private static void CreateAbilityParticles(
        Transform parent)
    {
        Material material =
            AssetDatabase.LoadAssetAtPath<Material>(
                MaterialPath +
                "/M_Shield_Energy.mat"
            );

        NewParticleSystem(
            parent,
            "AbilityEnergy",
            material,
            25,
            0.45f,
            0.040f,
            8f,
            ShellReferenceRadius,
            0.3f
        );
    }

    // ============================================================
    // HIT POINTS
    // ============================================================

    private static void CreateHitPoint(
        Transform parent,
        string objectName,
        int index)
    {
        GameObject objectRoot =
            NewChild(
                parent.gameObject,
                objectName
            );

        float angle =
            index *
            Mathf.PI *
            2f /
            4f;

        objectRoot.transform.localPosition =
            new Vector3(
                Mathf.Cos(angle) * ShellReferenceRadius,
                0.05f,
                Mathf.Sin(angle) * ShellReferenceRadius
            );

        objectRoot.transform.localRotation =
            Quaternion.LookRotation(
                objectRoot.transform.localPosition
                    .normalized
            );

        ShieldHitPoint hitPoint =
            objectRoot.AddComponent<ShieldHitPoint>();

        if (hitPoint == null)
        {
            return;
        }
    }

    // ============================================================
    // MESH BUILD
    // ============================================================

    private static void BuildMeshes()
    {
        SaveMesh(
            BuildIcosphere(3),
            MeshPath +
            "/SM_Shield_Core.asset"
        );

        SaveMesh(
            BuildHexCellMesh(),
            MeshPath +
            "/SM_Shield_HexCell.asset"
        );

        SaveMesh(
            BuildArcMesh(),
            MeshPath +
            "/SM_Shield_EnergyArc.asset"
        );

        SaveMesh(
            BuildEnergyShardMesh(),
            MeshPath +
            "/SM_Shield_EnergyShard.asset"
        );
    }

    private static Mesh BuildHexCellMesh()
    {
        Mesh mesh =
            new Mesh();

        Vector3[] vertices =
            new Vector3[12];

        int[] triangles =
            new int[36];

        float outerRadius = 0.5f;
        float innerRadius = 0.42f;

        for (int i = 0; i < 6; i++)
        {
            float angle =
                Mathf.PI /
                3f *
                i;

            vertices[i] =
                new Vector3(
                    Mathf.Cos(angle) *
                    outerRadius,

                    Mathf.Sin(angle) *
                    outerRadius,

                    0f
                );

            vertices[i + 6] =
                new Vector3(
                    Mathf.Cos(angle) *
                    innerRadius,

                    Mathf.Sin(angle) *
                    innerRadius,

                    0f
                );
        }

        int index = 0;

        for (int i = 0; i < 6; i++)
        {
            int next =
                (i + 1) % 6;

            triangles[index++] = i;
            triangles[index++] = next;
            triangles[index++] = i + 6;

            triangles[index++] = next;
            triangles[index++] = next + 6;
            triangles[index++] = i + 6;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        mesh.name =
            "SM_Shield_HexCell";

        return mesh;
    }

    private static Mesh BuildEnergyShardMesh()
    {
        Mesh mesh =
            new Mesh();

        Vector3[] vertices =
        {
            new Vector3(0f, 0f, 0.5f),
            new Vector3(-0.08f, 0f, -0.5f),
            new Vector3(0.08f, 0f, -0.5f),
            new Vector3(0f, 0.07f, -0.25f),
            new Vector3(0f, -0.07f, -0.25f)
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

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        mesh.name =
            "SM_Shield_EnergyShard";

        return mesh;
    }

    private static Mesh BuildArcMesh()
    {
        Mesh mesh =
            new Mesh();

        const int segments = 24;

        Vector3[] vertices =
            new Vector3[segments * 2];

        int[] triangles =
            new int[(segments - 1) * 6];

        for (int i = 0; i < segments; i++)
        {
            float t =
                i /
                (float)(segments - 1);

            float angle =
                t *
                Mathf.PI *
                2f;

            float radius =
                0.5f;

            float x =
                Mathf.Cos(angle) *
                radius;

            float y =
                Mathf.Sin(angle) *
                radius;

            Vector3 center =
                new Vector3(
                    x,
                    y,
                    t
                );

            Vector3 tangent =
                new Vector3(
                    -Mathf.Sin(angle),
                    Mathf.Cos(angle),
                    0f
                );

            Vector3 side =
                tangent.normalized *
                0.025f;

            vertices[i * 2] =
                center - side;

            vertices[i * 2 + 1] =
                center + side;
        }

        int index = 0;

        for (int i = 0;
             i < segments - 1;
             i++)
        {
            int a =
                i * 2;

            int b =
                a + 1;

            int c =
                a + 2;

            int d =
                a + 3;

            triangles[index++] = a;
            triangles[index++] = c;
            triangles[index++] = b;

            triangles[index++] = b;
            triangles[index++] = c;
            triangles[index++] = d;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        mesh.name =
            "SM_Shield_EnergyArc";

        return mesh;
    }

    // ============================================================
    // ICOSPHERE
    // ============================================================

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
            Dictionary<long, int> midpointCache =
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
                        midpointCache
                    );

                int bc =
                    GetMidpoint(
                        b,
                        c,
                        vertices,
                        midpointCache
                    );

                int ca =
                    GetMidpoint(
                        c,
                        a,
                        vertices,
                        midpointCache
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
            "SM_Shield_Core";

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
            ((long)smaller << 32) +
            larger;

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
            ) * 0.5f;

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

    // ============================================================
    // SAVE MESH
    // ============================================================

    private static void SaveMesh(
        Mesh mesh,
        string path)
    {
        Mesh existing =
            AssetDatabase.LoadAssetAtPath<Mesh>(
                path
            );

        if (existing != null)
        {
            UnityEngine.Object.DestroyImmediate(
                existing,
                true
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
            new GameObject(objectName);

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
        return NewChild(
            parent.gameObject,
            objectName
        );
    }

    private static T TryAddComponent<T>(
        GameObject objectRoot)
        where T : Component
    {
        T component =
            objectRoot.GetComponent<T>();

        if (component == null)
        {
            component =
                objectRoot.AddComponent<T>();
        }

        return component;
    }

    private static void DisableRendererFeatures(
        Renderer renderer)
    {
        renderer.shadowCastingMode =
            ShadowCastingMode.Off;

        renderer.receiveShadows = false;

        renderer.lightProbeUsage =
            LightProbeUsage.Off;

        renderer.reflectionProbeUsage =
            ReflectionProbeUsage.Off;
    }
}

#endif