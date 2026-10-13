#if UNITY_EDITOR

using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-click Editor setup for the LEAD KHÔNG PHANH Player Lane Smoke VFX.
///
/// Creates/reuses:
/// PlayerBike
/// └── VFX
///     ├── LeftSmoke
///     └── RightSmoke
///
/// Also creates/configures:
/// - PlayerLaneSmoke component
/// - Particle Systems
/// - Cartoon puffy smoke texture
/// - URP particle material
/// - Particle modules
/// - Serialized references
///
/// Runtime code is NOT contained in this file.
/// This file must remain inside an Editor folder.
/// </summary>
public static class PlayerLaneSmokeSetup
{
    // =====================================================================
    // PATHS
    // =====================================================================

    private const string MenuRoot =
        "Tools/LEAD KHÔNG PHANH/Player Lane Smoke/";

    private const string VfxFolder =
        "Assets/VFX/PlayerLaneSmoke";

    private const string MaterialPath =
        VfxFolder + "/M_PlayerLaneSmoke.mat";

    private const string TexturePath =
        VfxFolder + "/T_PlayerLaneSmoke_Puffy.png";

    // =====================================================================
    // HIERARCHY
    // =====================================================================

    private const string VfxRootName = "VFX";
    private const string LeftSmokeName = "LeftSmoke";
    private const string RightSmokeName = "RightSmoke";

    // =====================================================================
    // PARTICLE SETTINGS
    // =====================================================================

    private const int MaxParticlesPerWheel = 120;

    private const float LifetimeMin = 0.48f;
    private const float LifetimeMax = 0.82f;

    private const float SpeedMin = 0.08f;
    private const float SpeedMax = 0.42f;

    private const float SizeMin = 0.34f;
    private const float SizeMax = 0.82f;

    private const float ShapeRadius = 0.055f;

    // =====================================================================
    // MENU
    // =====================================================================

    [MenuItem(MenuRoot + "Setup Selected Player", priority = 0)]
    private static void SetupSelectedPlayerMenuItem()
    {
        RunSetup(false);
    }

    [MenuItem(MenuRoot + "Rebuild Selected Player", priority = 1)]
    private static void RebuildSelectedPlayerMenuItem()
    {
        RunSetup(true);
    }

    [MenuItem(MenuRoot + "Remove Generated VFX", priority = 20)]
    private static void RemoveGeneratedVfxMenuItem()
    {
        RemoveGeneratedVfx();
    }

    // =====================================================================
    // CORE SETUP
    // =====================================================================

    private static void RunSetup(bool rebuild)
    {
        GameObject playerRoot =
            ResolvePlayerRoot(out PlayerController controller);

        if (playerRoot == null || controller == null)
        {
            return;
        }

        Undo.SetCurrentGroupName(
            rebuild
                ? "Rebuild Player Lane Smoke"
                : "Setup Player Lane Smoke");

        int undoGroup = Undo.GetCurrentGroup();

        EnsureVfxFolder();

        // -------------------------------------------------------------
        // Rebuild only the generated VFX root.
        // PlayerController and PlayerLaneSmoke are preserved.
        // -------------------------------------------------------------

        if (rebuild)
        {
            Transform oldVfx =
                playerRoot.transform.Find(VfxRootName);

            if (oldVfx != null)
            {
                Undo.DestroyObjectImmediate(
                    oldVfx.gameObject);
            }
        }

        // -------------------------------------------------------------
        // Runtime controller
        // -------------------------------------------------------------

        PlayerLaneSmoke smoke =
            playerRoot.GetComponent<PlayerLaneSmoke>();

        if (smoke == null)
        {
            smoke =
                Undo.AddComponent<PlayerLaneSmoke>(
                    playerRoot);
        }

        // -------------------------------------------------------------
        // Hierarchy
        // -------------------------------------------------------------

        Transform vfxRoot =
            FindOrCreateChild(
                playerRoot.transform,
                VfxRootName,
                Vector3.zero);

        Transform leftTransform =
            FindOrCreateChild(
                vfxRoot,
                LeftSmokeName,
                new Vector3(-0.30f, 0.045f, -0.20f));

        Transform rightTransform =
            FindOrCreateChild(
                vfxRoot,
                RightSmokeName,
                new Vector3(0.30f, 0.045f, -0.20f));

        // -------------------------------------------------------------
        // Particle Systems
        // -------------------------------------------------------------

        ParticleSystem leftParticle =
            FindOrCreateParticleSystem(
                leftTransform);

        ParticleSystem rightParticle =
            FindOrCreateParticleSystem(
                rightTransform);

        // -------------------------------------------------------------
        // Material + texture
        // -------------------------------------------------------------

        Texture2D smokeTexture =
            CreateOrUpdatePuffySmokeTexture();

        Material smokeMaterial =
            CreateOrUpdateSmokeMaterial(
                smokeTexture);

        // -------------------------------------------------------------
        // Configure particles
        // -------------------------------------------------------------

        ConfigureSmokeParticleSystem(
            leftParticle,
            smokeMaterial,
            false);

        ConfigureSmokeParticleSystem(
            rightParticle,
            smokeMaterial,
            true);

        // -------------------------------------------------------------
        // Wire runtime references
        // -------------------------------------------------------------

        AssignReferences(
            smoke,
            controller,
            leftParticle,
            rightParticle);

        // -------------------------------------------------------------
        // Save / selection
        // -------------------------------------------------------------

        EditorUtility.SetDirty(smoke);

        if (leftParticle != null)
        {
            EditorUtility.SetDirty(leftParticle);
        }

        if (rightParticle != null)
        {
            EditorUtility.SetDirty(rightParticle);
        }

        Undo.CollapseUndoOperations(undoGroup);

        Selection.activeGameObject = playerRoot;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "[PlayerLaneSmokeSetup] " +
            "One-click Player Lane Smoke VFX setup completed on '" +
            playerRoot.name +
            "'.");
    }

    // =====================================================================
    // PLAYER RESOLUTION
    // =====================================================================

    private static GameObject ResolvePlayerRoot(
        out PlayerController controller)
    {
        controller = null;

        GameObject selected =
            Selection.activeGameObject;

        // -------------------------------------------------------------
        // Explicit selection has priority.
        // -------------------------------------------------------------

        if (selected != null)
        {
            controller =
                selected.GetComponent<PlayerController>();

            if (controller == null)
            {
                controller =
                    selected.GetComponentInParent<PlayerController>();
            }

            if (controller == null)
            {
                controller =
                    selected.GetComponentInChildren<PlayerController>();
            }
        }

        // -------------------------------------------------------------
        // No explicit player selected.
        // Only allow exactly one PlayerController.
        // -------------------------------------------------------------

        if (controller == null)
        {
            PlayerController[] all =
                Object.FindObjectsByType<PlayerController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            if (all.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "Player Lane Smoke",
                    "No PlayerController was found in the scene.\n\n" +
                    "Please select PlayerBike first.",
                    "OK");

                return null;
            }

            if (all.Length > 1)
            {
                EditorUtility.DisplayDialog(
                    "Player Lane Smoke",
                    "Multiple PlayerController objects were found.\n\n" +
                    "Please select the intended PlayerBike before running setup.",
                    "OK");

                return null;
            }

            controller = all[0];
        }

        return controller.gameObject;
    }

    // =====================================================================
    // HIERARCHY
    // =====================================================================

    private static Transform FindOrCreateChild(
        Transform parent,
        string childName,
        Vector3 localPosition)
    {
        Transform existing =
            parent.Find(childName);

        if (existing != null)
        {
            existing.localPosition = localPosition;
            existing.localRotation = Quaternion.identity;
            existing.localScale = Vector3.one;

            return existing;
        }

        GameObject child =
            new GameObject(childName);

        Undo.RegisterCreatedObjectUndo(
            child,
            "Create " + childName);

        Undo.SetTransformParent(
            child.transform,
            parent,
            "Parent " + childName);

        child.transform.localPosition =
            localPosition;

        child.transform.localRotation =
            Quaternion.identity;

        child.transform.localScale =
            Vector3.one;

        return child.transform;
    }

    // =====================================================================
    // PARTICLE SYSTEM
    // =====================================================================

    private static ParticleSystem FindOrCreateParticleSystem(
        Transform target)
    {
        ParticleSystem particleSystem =
            target.GetComponent<ParticleSystem>();

        if (particleSystem == null)
        {
            particleSystem =
                Undo.AddComponent<ParticleSystem>(
                    target.gameObject);
        }

        return particleSystem;
    }

    // =====================================================================
    // PARTICLE CONFIGURATION
    // =====================================================================

    private static void ConfigureSmokeParticleSystem(
        ParticleSystem ps,
        Material material,
        bool isRightWheel)
    {
        if (ps == null)
        {
            return;
        }

        // -------------------------------------------------------------
        // Main
        // -------------------------------------------------------------

        ParticleSystem.MainModule main =
            ps.main;

        main.duration = 5f;

        main.loop = true;

        main.prewarm = false;

        main.playOnAwake = false;

        main.simulationSpace =
            ParticleSystemSimulationSpace.World;

        main.scalingMode =
            ParticleSystemScalingMode.Local;

        main.startLifetime =
            new ParticleSystem.MinMaxCurve(
                LifetimeMin,
                LifetimeMax);

        main.startSpeed =
            new ParticleSystem.MinMaxCurve(
                SpeedMin,
                SpeedMax);

        main.startSize =
            new ParticleSystem.MinMaxCurve(
                SizeMin,
                SizeMax);

        main.startRotation =
            new ParticleSystem.MinMaxCurve(
                0f,
                360f * Mathf.Deg2Rad);

        main.startColor =
            new ParticleSystem.MinMaxGradient(
                new Color(
                    0.92f,
                    0.92f,
                    0.90f,
                    1f));

        main.gravityModifier = 0f;

        main.maxParticles =
            MaxParticlesPerWheel;

        // -------------------------------------------------------------
        // Emission
        // -------------------------------------------------------------

        ParticleSystem.EmissionModule emission =
            ps.emission;

        emission.enabled = true;

        // PlayerLaneSmoke controls this at runtime.
        emission.rateOverTime = 0f;

        emission.rateOverDistance = 0f;

        emission.SetBursts(
            new ParticleSystem.Burst[0]);

        // -------------------------------------------------------------
        // Shape
        //
        // Tiny sphere gives a compact puff origin.
        // The actual cloud expands through SizeOverLifetime.
        // -------------------------------------------------------------

        ParticleSystem.ShapeModule shape =
            ps.shape;

        shape.enabled = true;

        shape.shapeType =
            ParticleSystemShapeType.Sphere;

        shape.radius =
            ShapeRadius;

        shape.radiusThickness = 1f;

        // -------------------------------------------------------------
        // Velocity
        //
        // World-space particle movement:
        // - upward
        // - slight lateral spread
        // - slight backward movement
        // -------------------------------------------------------------

        ParticleSystem.VelocityOverLifetimeModule velocity =
            ps.velocityOverLifetime;

        velocity.enabled = true;

        velocity.space =
            ParticleSystemSimulationSpace.World;

        velocity.x =
            new ParticleSystem.MinMaxCurve(
                -0.22f,
                0.22f);

        velocity.y =
            new ParticleSystem.MinMaxCurve(
                0.22f,
                0.58f);

        velocity.z =
            new ParticleSystem.MinMaxCurve(
                -0.85f,
                -0.25f);

        // -------------------------------------------------------------
        // Noise
        //
        // Deliberately restrained.
        // The target is puffy cartoon smoke, not fog.
        // -------------------------------------------------------------

        ParticleSystem.NoiseModule noise =
            ps.noise;

        noise.enabled = true;

        noise.strength =
            new ParticleSystem.MinMaxCurve(
                0.08f,
                0.22f);

        noise.frequency = 0.55f;

        noise.scrollSpeed = 0.18f;

        noise.quality =
            ParticleSystemNoiseQuality.Medium;

        noise.damping = true;

        // -------------------------------------------------------------
        // Size over Lifetime
        //
        // Small → big puff → hold → fade.
        // -------------------------------------------------------------

        ParticleSystem.SizeOverLifetimeModule size =
            ps.sizeOverLifetime;

        size.enabled = true;

        AnimationCurve sizeCurve =
            new AnimationCurve(
                new Keyframe(0.00f, 0.34f),
                new Keyframe(0.16f, 0.72f),
                new Keyframe(0.42f, 1.00f),
                new Keyframe(0.72f, 1.12f),
                new Keyframe(1.00f, 0.62f));

        for (int i = 0; i < sizeCurve.length; i++)
        {
            sizeCurve.SmoothTangents(i, 0f);
        }

        size.size =
            new ParticleSystem.MinMaxCurve(
                1f,
                sizeCurve);

        // -------------------------------------------------------------
        // Color over Lifetime
        //
        // Cartoon white/gray puffy smoke.
        // Alpha starts visible very quickly.
        // -------------------------------------------------------------

        ParticleSystem.ColorOverLifetimeModule color =
            ps.colorOverLifetime;

        color.enabled = true;

        Gradient gradient =
            new Gradient();

        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(
                    new Color(
                        0.96f,
                        0.96f,
                        0.94f),
                    0f),

                new GradientColorKey(
                    new Color(
                        0.88f,
                        0.88f,
                        0.85f),
                    0.32f),

                new GradientColorKey(
                    new Color(
                        0.70f,
                        0.70f,
                        0.67f),
                    0.72f),

                new GradientColorKey(
                    new Color(
                        0.56f,
                        0.56f,
                        0.53f),
                    1f)
            },
            new[]
            {
                new GradientAlphaKey(
                    0f,
                    0f),

                new GradientAlphaKey(
                    0.72f,
                    0.07f),

                new GradientAlphaKey(
                    0.62f,
                    0.28f),

                new GradientAlphaKey(
                    0.42f,
                    0.68f),

                new GradientAlphaKey(
                    0f,
                    1f)
            });

        color.color = gradient;

        // -------------------------------------------------------------
        // Rotation over Lifetime
        //
        // Very small angular movement prevents identical sprites
        // from looking stamped.
        // -------------------------------------------------------------

        ParticleSystem.RotationOverLifetimeModule rotation =
            ps.rotationOverLifetime;

        rotation.enabled = true;

        rotation.z =
            new ParticleSystem.MinMaxCurve(
                -0.35f,
                0.35f);

        // -------------------------------------------------------------
        // Renderer
        // -------------------------------------------------------------

        ParticleSystemRenderer renderer =
            ps.GetComponent<ParticleSystemRenderer>();

        if (renderer != null)
        {
            renderer.renderMode =
                ParticleSystemRenderMode.Billboard;

            renderer.alignment =
                ParticleSystemRenderSpace.View;

            renderer.sortingFudge = 0f;

            renderer.sharedMaterial =
                material;

            renderer.enableGPUInstancing = true;

            renderer.allowRoll = true;
        }

        // -------------------------------------------------------------
        // Clear current particles
        // -------------------------------------------------------------

        ps.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    // =====================================================================
    // TEXTURE
    // =====================================================================

    private static Texture2D CreateOrUpdatePuffySmokeTexture()
    {
        EnsureVfxFolder();

        Texture2D existing =
            AssetDatabase.LoadAssetAtPath<Texture2D>(
                TexturePath);

        if (existing != null)
        {
            return existing;
        }

        const int size = 128;

        Texture2D texture =
            new Texture2D(
                size,
                size,
                TextureFormat.RGBA32,
                false);

        texture.name =
            "T_PlayerLaneSmoke_Puffy";

        texture.wrapMode =
            TextureWrapMode.Clamp;

        texture.filterMode =
            FilterMode.Bilinear;

        Color32[] pixels =
            new Color32[size * size];

        // -------------------------------------------------------------
        // Puffy cartoon silhouette.
        //
        // Multiple overlapping soft blobs:
        // - central mass
        // - upper left puff
        // - upper right puff
        // - lower side puffs
        //
        // This intentionally does NOT generate a perfect circle.
        // -------------------------------------------------------------

        Vector2 center =
            new Vector2(64f, 65f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx =
                    x / (float)(size - 1);

                float ny =
                    y / (float)(size - 1);

                float alpha = 0f;

                // Main central puff.
                alpha +=
                    SoftBlob(
                        x,
                        y,
                        center.x,
                        center.y,
                        44f,
                        1.00f);

                // Upper left puff.
                alpha +=
                    SoftBlob(
                        x,
                        y,
                        39f,
                        39f,
                        27f,
                        0.82f);

                // Upper right puff.
                alpha +=
                    SoftBlob(
                        x,
                        y,
                        79f,
                        40f,
                        29f,
                        0.86f);

                // Left lower puff.
                alpha +=
                    SoftBlob(
                        x,
                        y,
                        27f,
                        72f,
                        25f,
                        0.66f);

                // Right lower puff.
                alpha +=
                    SoftBlob(
                        x,
                        y,
                        100f,
                        73f,
                        24f,
                        0.62f);

                // Small central upper puff.
                alpha +=
                    SoftBlob(
                        x,
                        y,
                        61f,
                        23f,
                        18f,
                        0.50f);

                // Slightly irregular edge deformation.
                float distortion =
                    Mathf.Sin(
                        nx * 19.0f +
                        ny * 8.0f) *
                    0.035f;

                alpha += distortion;

                alpha =
                    Mathf.Clamp01(alpha);

                // Soft contrast curve.
                alpha =
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        alpha);

                pixels[
                    y * size + x] =
                    new Color32(
                        255,
                        255,
                        255,
                        (byte)(alpha * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, false);

        byte[] png =
            texture.EncodeToPNG();

        File.WriteAllBytes(
            TexturePath,
            png);

        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(
            TexturePath,
            ImportAssetOptions.ForceUpdate);

        ConfigureTextureImporter(
            TexturePath);

        AssetDatabase.SaveAssets();

        return AssetDatabase.LoadAssetAtPath<Texture2D>(
            TexturePath);
    }

    private static float SoftBlob(
        float x,
        float y,
        float centerX,
        float centerY,
        float radius,
        float strength)
    {
        float dx =
            x - centerX;

        float dy =
            y - centerY;

        float distance =
            Mathf.Sqrt(
                dx * dx +
                dy * dy);

        float normalized =
            distance / radius;

        if (normalized >= 1f)
        {
            return 0f;
        }

        float value =
            1f - normalized;

        value =
            value * value *
            (3f - 2f * value);

        return value * strength;
    }

    private static void ConfigureTextureImporter(
        string path)
    {
        TextureImporter importer =
            AssetImporter.GetAtPath(path)
            as TextureImporter;

        if (importer == null)
        {
            return;
        }

        importer.textureType =
            TextureImporterType.Default;

        importer.alphaIsTransparency = true;

        importer.mipmapEnabled = false;

        importer.wrapMode =
            TextureWrapMode.Clamp;

        importer.filterMode =
            FilterMode.Bilinear;

        importer.textureCompression =
            TextureImporterCompression.CompressedHQ;

        importer.SaveAndReimport();
    }

    // =====================================================================
    // MATERIAL
    // =====================================================================

    private static Material CreateOrUpdateSmokeMaterial(
        Texture2D texture)
    {
        EnsureVfxFolder();

        Material material =
            AssetDatabase.LoadAssetAtPath<Material>(
                MaterialPath);

        Shader shader =
            Shader.Find(
                "Universal Render Pipeline/Particles/Unlit");

        if (shader == null)
        {
            shader =
                Shader.Find(
                    "Universal Render Pipeline/Particles/Lit");
        }

        if (shader == null)
        {
            shader =
                Shader.Find("Sprites/Default");
        }

        if (material == null)
        {
            material =
                new Material(shader);

            material.name =
                "M_PlayerLaneSmoke";

            AssetDatabase.CreateAsset(
                material,
                MaterialPath);
        }
        else if (shader != null && material.shader != shader)
        {
            material.shader = shader;
        }

        // -------------------------------------------------------------
        // URP transparent configuration
        // -------------------------------------------------------------

        if (material.HasProperty("_Surface"))
        {
            material.SetFloat(
                "_Surface",
                1f);
        }

        if (material.HasProperty("_Blend"))
        {
            material.SetFloat(
                "_Blend",
                0f);
        }

        if (material.HasProperty("_AlphaClip"))
        {
            material.SetFloat(
                "_AlphaClip",
                0f);
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor(
                "_BaseColor",
                Color.white);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor(
                "_Color",
                Color.white);
        }

        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture(
                "_BaseMap",
                texture);
        }

        if (material.HasProperty("_MainTex"))
        {
            material.SetTexture(
                "_MainTex",
                texture);
        }

        material.EnableKeyword(
            "_SURFACE_TYPE_TRANSPARENT");

        material.DisableKeyword(
            "_ALPHATEST_ON");

        material.renderQueue = 3000;

        EditorUtility.SetDirty(material);

        AssetDatabase.SaveAssets();

        return material;
    }

    // =====================================================================
    // REFERENCE WIRING
    // =====================================================================

    private static void AssignReferences(
        PlayerLaneSmoke smoke,
        PlayerController controller,
        ParticleSystem leftSmoke,
        ParticleSystem rightSmoke)
    {
        SerializedObject serializedObject =
            new SerializedObject(smoke);

        SerializedProperty controllerProperty =
            serializedObject.FindProperty(
                "playerController");

        SerializedProperty leftProperty =
            serializedObject.FindProperty(
                "leftSmoke");

        SerializedProperty rightProperty =
            serializedObject.FindProperty(
                "rightSmoke");

        if (controllerProperty != null)
        {
            controllerProperty.objectReferenceValue =
                controller;
        }

        if (leftProperty != null)
        {
            leftProperty.objectReferenceValue =
                leftSmoke;
        }

        if (rightProperty != null)
        {
            rightProperty.objectReferenceValue =
                rightSmoke;
        }

        serializedObject.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(smoke);
    }

    // =====================================================================
    // REMOVE
    // =====================================================================

    private static void RemoveGeneratedVfx()
    {
        GameObject playerRoot =
            ResolvePlayerRoot(out _);

        if (playerRoot == null)
        {
            return;
        }

        Transform vfxRoot =
            playerRoot.transform.Find(
                VfxRootName);

        if (vfxRoot == null)
        {
            EditorUtility.DisplayDialog(
                "Player Lane Smoke",
                "No generated Player Lane Smoke VFX was found under '" +
                playerRoot.name +
                "'.",
                "OK");

            return;
        }

        PlayerLaneSmoke smoke =
            playerRoot.GetComponent<PlayerLaneSmoke>();

        if (smoke != null)
        {
            ClearSmokeReferences(smoke);
        }

        Undo.DestroyObjectImmediate(
            vfxRoot.gameObject);

        Debug.Log(
            "[PlayerLaneSmokeSetup] " +
            "Removed generated VFX under '" +
            playerRoot.name +
            "'.");
    }

    private static void ClearSmokeReferences(
        PlayerLaneSmoke smoke)
    {
        SerializedObject so =
            new SerializedObject(smoke);

        SerializedProperty leftProperty =
            so.FindProperty("leftSmoke");

        SerializedProperty rightProperty =
            so.FindProperty("rightSmoke");

        if (leftProperty != null)
        {
            leftProperty.objectReferenceValue =
                null;
        }

        if (rightProperty != null)
        {
            rightProperty.objectReferenceValue =
                null;
        }

        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(smoke);
    }

    // =====================================================================
    // FOLDER
    // =====================================================================

    private static void EnsureVfxFolder()
    {
        if (AssetDatabase.IsValidFolder(VfxFolder))
        {
            return;
        }

        string[] parts =
            VfxFolder.Split('/');

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
}

#endif