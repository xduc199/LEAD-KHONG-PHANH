// =============================================================================
// LEAD KHÔNG PHANH - Cinematic Nitro Exhaust
// ExhaustNitroVFXSetup.cs        <-- ĐẶT TRONG THƯ MỤC "Editor"
//                                    (vd: Assets/VFX/NitroExhaust/Editor/)
//
// Menu:  Tools / LEAD KHÔNG PHANH / VFX / Create Cinematic Nitro Exhaust
//
// Tự sinh: Textures -> Materials -> Meshes -> PF_CinematicNitroExhaust.prefab
// với ĐÚNG hierarchy theo yêu cầu prompt.
//
// Sửa các lỗi của bản V5 cũ:
//   * SaveTexture() DestroyImmediate texture rồi vẫn return con trỏ đã chết
//     -> AssetDatabase.GetAssetPath() trả rỗng -> material KHÔNG CÓ texture.
//     Nay return asset đã load lại từ ổ đĩa.
//   * _Blend = 1 là "Premultiply" chứ không phải "Additive" (Additive = 2),
//     và thiếu keyword/renderQueue -> material hiển thị sai.
//   * Dùng URP/Unlit (shader opaque cho mesh) cho particle -> nên dùng
//     URP/Particles/Unlit hoặc shader NitroFlame riêng.
//   * Mesh lửa chỉ là 1 dải phẳng -> nhìn nghiêng thấy tờ giấy. Nay là mesh
//     nhiều cánh xoay quanh trục Z (cross-blade) -> có khối, có chiều sâu.
//   * playOnAwake = true -> Nitro OFF vẫn cháy. Nay tất cả đều tắt sạch.
//   * Tên "11_Burst" không khớp script runtime. Nay dùng NitroVfxModule.
// =============================================================================

#if UNITY_EDITOR

using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class ExhaustNitroVFXSetup
{
    private const string Root = "Assets/VFX/NitroExhaust";
    private const string TextureRoot = Root + "/Textures";
    private const string MaterialRoot = Root + "/Materials";
    private const string MeshRoot = Root + "/Meshes";
    private const string PrefabRoot = Root + "/Prefabs";

    private const string PrefabPath = PrefabRoot + "/PF_CinematicNitroExhaust.prefab";

    private const string FlameShaderName = "LEADKhongPhanh/NitroFlame";
    private const string HeatShaderName = "LEADKhongPhanh/NitroHeatDistortion";

    // =========================================================================
    // MENU
    // =========================================================================

    [MenuItem("Tools/LEAD KHÔNG PHANH/VFX/Create Cinematic Nitro Exhaust", false, 10)]
    public static void Create()
    {
        EnsureFolders();

        // ---------------- TEXTURES ----------------
        Texture2D texFlame = CreateFlameTexture("T_Nitro_Flame", 256, 512);
        Texture2D texCore = CreateCoreTexture("T_Nitro_Core", 256, 512);
        Texture2D texGlow = CreateGlowTexture("T_Nitro_Glow", 256, 256);
        Texture2D texSpark = CreateSparkTexture("T_Nitro_Spark", 128, 128);
        Texture2D texRay = CreateRayTexture("T_Nitro_Ray", 64, 256);
        Texture2D texSmoke = CreateSmokeTexture("T_Nitro_Smoke", 256, 256);
        Texture2D texHeat = CreateHeatTexture("T_Nitro_Heat", 256, 256);

        // ---------------- MATERIALS ----------------
        Material matFlame = CreateFlameMaterial(
            "M_Nitro_Flame", texFlame,
            new Color(1.00f, 0.95f, 0.78f),   // core
            new Color(1.00f, 0.66f, 0.16f),   // mid
            new Color(1.00f, 0.15f, 0.02f),   // outer
            erosion: 0.44f, scroll: 4.6f, wobble: 0.11f);

        Material matCore = CreateFlameMaterial(
            "M_Nitro_Core", texCore,
            new Color(1.00f, 1.00f, 1.00f),
            new Color(0.55f, 0.88f, 1.00f),
            new Color(0.10f, 0.45f, 1.00f),
            erosion: 0.22f, scroll: 8.5f, wobble: 0.05f);

        Material matGlow = CreateParticleMaterial(
            "M_Nitro_Glow", texGlow, new Color(0.35f, 0.80f, 1.00f) * 2.2f, true);

        Material matSpark = CreateParticleMaterial(
            "M_Nitro_Spark", texSpark, new Color(1.00f, 0.62f, 0.18f) * 3.0f, true);

        Material matRay = CreateParticleMaterial(
            "M_Nitro_Ray", texRay, new Color(1.00f, 0.80f, 0.45f) * 2.6f, true);

        Material matSmoke = CreateParticleMaterial(
            "M_Nitro_Smoke", texSmoke, new Color(0.16f, 0.18f, 0.21f, 1f), false);

        Material matHeat = CreateHeatMaterial("M_Nitro_Heat", texHeat);

        // ---------------- MESHES ----------------
        Mesh meshMain = BuildFlameMesh("SM_Nitro_Flame_Main", 3.10f, 0.52f, 5, 16, 0.00f, 0.55f, 26f, 1.00f);
        Mesh meshSecond = BuildFlameMesh("SM_Nitro_Flame_Second", 2.45f, 0.40f, 4, 14, 1.37f, 0.62f, -34f, 1.25f);
        Mesh meshTongueL = BuildFlameMesh("SM_Nitro_Tongue_L", 1.95f, 0.22f, 3, 13, 2.11f, 0.70f, 42f, 1.80f);
        Mesh meshTongueR = BuildFlameMesh("SM_Nitro_Tongue_R", 2.20f, 0.20f, 3, 13, 3.47f, 0.72f, -48f, 1.75f);
        Mesh meshTongueC = BuildFlameMesh("SM_Nitro_Tongue_C", 2.70f, 0.16f, 3, 15, 4.93f, 0.80f, 18f, 1.45f);
        Mesh meshCore = BuildFlameMesh("SM_Nitro_Core", 1.35f, 0.17f, 4, 14, 0.73f, 0.42f, 12f, 0.35f);

        // ---------------- HIERARCHY ----------------
        GameObject root = new GameObject("ExhaustNitroVFX");
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        // ExhaustSocket
        GameObject socket = NewChild(root.transform, "ExhaustSocket");
        socket.transform.localPosition = Vector3.zero;

        // ---- FlameSystem ----
        Transform flameSystem = NewChild(root.transform, "FlameSystem").transform;

        CreateFlameLayer(flameSystem, "FlameMain", meshMain, matFlame,
            Quaternion.identity, false, 1.00f, 1.00f, 12f, 0.20f, 5.0f, 5.0f, 0f);

        CreateFlameLayer(flameSystem, "FlameSecondary", meshSecond, matFlame,
            Quaternion.Euler(0f, 0f, 58f), false, 1.00f, 1.05f, 15f, 0.26f, 6.5f, 8.0f, 9f);

        CreateFlameLayer(flameSystem, "FlameTongue_L", meshTongueL, matFlame,
            Quaternion.Euler(-6f, -9f, 118f), false, 1.00f, 1.00f, 19f, 0.34f, 8.0f, 12f, -22f);

        CreateFlameLayer(flameSystem, "FlameTongue_R", meshTongueR, matFlame,
            Quaternion.Euler(5f, 8f, -132f), false, 1.00f, 1.00f, 17f, 0.32f, 7.2f, 12f, 26f);

        CreateFlameLayer(flameSystem, "FlameTongue_Center", meshTongueC, matFlame,
            Quaternion.Euler(0f, 0f, 22f), false, 1.00f, 0.85f, 22f, 0.30f, 9.0f, 6f, -14f);

        CreateFlameLayer(flameSystem, "FlameCore", meshCore, matCore,
            Quaternion.identity, true, 1.00f, 1.00f, 26f, 0.16f, 11f, 3.5f, 55f);

        // ---- EnergySystem ----
        Transform energySystem = NewChild(root.transform, "EnergySystem").transform;
        CreateCoreGlow(energySystem, matGlow);
        CreateHotStreaks(energySystem, matRay);
        CreateEnergyParticles(energySystem, matFlame);

        // ---- BurstSystem ----
        Transform burstSystem = NewChild(root.transform, "BurstSystem").transform;
        CreateInitialBurst(burstSystem, matFlame);
        CreateBurstRays(burstSystem, matRay);
        CreateBurstSparks(burstSystem, matSpark);

        // ---- SparkSystem ----
        Transform sparkSystem = NewChild(root.transform, "SparkSystem").transform;
        CreateSparksNear(sparkSystem, matSpark);
        CreateSparksFar(sparkSystem, matSpark);

        // ---- SmokeSystem ----
        Transform smokeSystem = NewChild(root.transform, "SmokeSystem").transform;
        CreateExhaustSmoke(smokeSystem, matSmoke);

        // ---- HeatSystem ----
        Transform heatSystem = NewChild(root.transform, "HeatSystem").transform;
        CreateHeatHaze(heatSystem, matHeat);

        // ---- LightSystem ----
        Transform lightSystem = NewChild(root.transform, "LightSystem").transform;

        CreateNitroLight(lightSystem, "ExhaustLight",
            new Vector3(0f, 0f, 0.85f),
            new Color(1.00f, 0.45f, 0.12f), 7.5f, 6.0f, 0.38f, 15f);

        CreateNitroLight(lightSystem, "CoreLight",
            new Vector3(0f, 0f, 0.15f),
            new Color(0.45f, 0.78f, 1.00f), 5.5f, 2.6f, 0.28f, 22f);

        // ---- Controller ----
  
        // ---------------- SAVE PREFAB ----------------
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            AssetDatabase.DeleteAsset(PrefabPath);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);

        bool hasFlameShader = Shader.Find(FlameShaderName) != null;
        bool hasHeatShader = Shader.Find(HeatShaderName) != null;

        EditorUtility.DisplayDialog(
            "LEAD KHÔNG PHANH — Nitro Exhaust",
            "Đã tạo xong prefab:\n" + PrefabPath + "\n\n" +
            "Hướng phụt: LOCAL +Z\n\n" +
            (hasFlameShader ? "✔ NitroFlame.shader OK\n" : "✘ Thiếu NitroFlame.shader (đang dùng shader dự phòng)\n") +
            (hasHeatShader ? "✔ NitroHeatDistortion.shader OK\n" : "✘ Thiếu NitroHeatDistortion.shader (heat haze sẽ mờ nhạt)\n") +
            "\nNhớ bật Opaque Texture trong URP Asset để heat haze hoạt động.",
            "OK");
    }

    [MenuItem("Tools/LEAD KHÔNG PHANH/VFX/Attach Nitro Exhaust To Selection", false, 11)]
    public static void AttachToSelection()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

        if (prefab == null)
        {
            EditorUtility.DisplayDialog("LEAD KHÔNG PHANH",
                "Chưa có prefab. Chạy 'Create Cinematic Nitro Exhaust' trước.", "OK");
            return;
        }

        if (Selection.activeTransform == null)
        {
            EditorUtility.DisplayDialog("LEAD KHÔNG PHANH",
                "Hãy chọn GameObject ống bô trong Hierarchy trước.", "OK");
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.SetParent(Selection.activeTransform, false);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;

        Undo.RegisterCreatedObjectUndo(instance, "Attach Nitro Exhaust");
        Selection.activeGameObject = instance;
    }

    // =========================================================================
    // FOLDERS
    // =========================================================================

    private static void EnsureFolders()
    {
        CreateFolder("Assets/VFX");
        CreateFolder(Root);
        CreateFolder(TextureRoot);
        CreateFolder(MaterialRoot);
        CreateFolder(MeshRoot);
        CreateFolder(PrefabRoot);
        AssetDatabase.Refresh();
    }

    private static void CreateFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        string folder = Path.GetFileName(path);

        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            CreateFolder(parent);

        AssetDatabase.CreateFolder(parent, folder);
    }

    // =========================================================================
    // NOISE (deterministic, dùng cho texture procedural)
    // =========================================================================

    private static float Hash(int x, int y, int seed)
    {
        int n = x * 374761393 + y * 668265263 + seed * 1442695040;
        n = (n ^ (n >> 13)) * 1274126177;
        return ((n ^ (n >> 16)) & 0x7fffffff) / (float)0x7fffffff;
    }

    private static float ValueNoise(float x, float y, int seed)
    {
        int xi = Mathf.FloorToInt(x);
        int yi = Mathf.FloorToInt(y);
        float xf = x - xi;
        float yf = y - yi;

        xf = xf * xf * (3f - 2f * xf);
        yf = yf * yf * (3f - 2f * yf);

        float a = Hash(xi, yi, seed);
        float b = Hash(xi + 1, yi, seed);
        float c = Hash(xi, yi + 1, seed);
        float d = Hash(xi + 1, yi + 1, seed);

        return Mathf.Lerp(Mathf.Lerp(a, b, xf), Mathf.Lerp(c, d, xf), yf);
    }

    private static float Fbm(float x, float y, int seed, int octaves)
    {
        float sum = 0f;
        float amp = 0.5f;
        float freq = 1f;

        for (int i = 0; i < octaves; i++)
        {
            sum += ValueNoise(x * freq, y * freq, seed + i * 17) * amp;
            freq *= 2.04f;
            amp *= 0.5f;
        }

        return Mathf.Clamp01(sum * 1.35f);
    }

    // =========================================================================
    // TEXTURES
    // =========================================================================

    private static Texture2D CreateFlameTexture(string name, int w, int h)
    {
        Color[] px = new Color[w * h];

        for (int y = 0; y < h; y++)
        {
            float v = y / (h - 1f);                       // 0 = ống bô, 1 = ngọn
            float envelope = Mathf.Pow(1f - v, 0.42f);
            float halfWidth = 0.47f * envelope;
            float wander = Mathf.Sin(v * 9.3f) * 0.05f + Mathf.Sin(v * 23f) * 0.022f;
            float center = 0.5f + wander;

            for (int x = 0; x < w; x++)
            {
                float u = x / (w - 1f);
                float d = Mathf.Abs(u - center);

                float a = Mathf.SmoothStep(halfWidth, halfWidth * 0.35f, d);

                // flame tongues: xé rách dần về ngọn
                float n = Fbm(u * 5.5f, v * 11f, 7717, 3);
                float tear = Mathf.Lerp(0f, 0.72f, Mathf.Pow(v, 0.85f));
                a = Mathf.Clamp01((a - tear * n) / Mathf.Max(0.0001f, 1f - tear));

                // fade ở ngọn + fade nhẹ ở gốc để không bị cắt phẳng
                a *= Mathf.Clamp01(1f - Mathf.Pow(v, 4.5f));
                a *= Mathf.SmoothStep(0f, 0.06f, v) * 0.85f + 0.15f;

                float heat = Mathf.Clamp01((1f - v) * 1.15f);
                Color c = Color.Lerp(new Color(1f, 0.20f, 0.02f), new Color(1f, 0.93f, 0.62f), heat);
                c.a = Mathf.Clamp01(a);

                px[y * w + x] = c;
            }
        }

        return SaveTexture(name, w, h, px);
    }

    private static Texture2D CreateCoreTexture(string name, int w, int h)
    {
        Color[] px = new Color[w * h];

        for (int y = 0; y < h; y++)
        {
            float v = y / (h - 1f);
            float envelope = Mathf.Pow(1f - v, 0.30f);
            float halfWidth = 0.36f * envelope;

            for (int x = 0; x < w; x++)
            {
                float u = x / (w - 1f);
                float d = Mathf.Abs(u - 0.5f);

                float a = Mathf.SmoothStep(halfWidth, halfWidth * 0.18f, d);
                a *= Mathf.Clamp01(1f - Mathf.Pow(v, 2.2f));

                float n = Fbm01(u, v);
                a *= 0.85f + n * 0.15f;

                Color c = Color.Lerp(Color.white, new Color(0.18f, 0.58f, 1f), Mathf.Pow(v, 0.7f));
                c.a = Mathf.Clamp01(a);

                px[y * w + x] = c;
            }
        }

        return SaveTexture(name, w, h, px);
    }

    private static float Fbm01(float u, float v)
    {
        return Fbm(u * 4f, v * 9f, 3313, 2);
    }

    private static Texture2D CreateGlowTexture(string name, int w, int h)
    {
        Color[] px = new Color[w * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x / (w - 1f) - 0.5f;
                float dy = y / (h - 1f) - 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;

                float a = Mathf.Clamp01(1f - d);
                a = a * a * a;

                px[y * w + x] = new Color(0.65f, 0.88f, 1f, a);
            }
        }

        return SaveTexture(name, w, h, px);
    }

    private static Texture2D CreateSparkTexture(string name, int w, int h)
    {
        Color[] px = new Color[w * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = (x / (w - 1f) - 0.5f) * 2f;
                float dy = (y / (h - 1f) - 0.5f) * 2f;

                float core = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx * 9f + dy * dy * 1.6f));
                core = Mathf.Pow(core, 1.6f);

                Color c = Color.Lerp(new Color(1f, 0.45f, 0.08f), Color.white, core);
                c.a = Mathf.Clamp01(core);

                px[y * w + x] = c;
            }
        }

        return SaveTexture(name, w, h, px);
    }

    private static Texture2D CreateRayTexture(string name, int w, int h)
    {
        Color[] px = new Color[w * h];

        for (int y = 0; y < h; y++)
        {
            float v = y / (h - 1f);
            float along = Mathf.Sin(v * Mathf.PI);

            for (int x = 0; x < w; x++)
            {
                float u = Mathf.Abs(x / (w - 1f) - 0.5f) * 2f;
                float a = Mathf.Clamp01(1f - u);
                a = Mathf.Pow(a, 2.4f) * along;

                Color c = Color.Lerp(new Color(1f, 0.55f, 0.15f), Color.white, a);
                c.a = Mathf.Clamp01(a);

                px[y * w + x] = c;
            }
        }

        return SaveTexture(name, w, h, px);
    }

    private static Texture2D CreateSmokeTexture(string name, int w, int h)
    {
        Color[] px = new Color[w * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float u = x / (w - 1f);
                float v = y / (h - 1f);

                float dx = u - 0.5f;
                float dy = v - 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;

                float n = Fbm(u * 5.5f, v * 5.5f, 1201, 4);
                float a = Mathf.Clamp01(1f - d);
                a = Mathf.Pow(a, 1.7f) * (0.35f + n * 0.85f);

                px[y * w + x] = new Color(0.55f, 0.58f, 0.62f, Mathf.Clamp01(a));
            }
        }

        return SaveTexture(name, w, h, px);
    }

    private static Texture2D CreateHeatTexture(string name, int w, int h)
    {
        Color[] px = new Color[w * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float u = x / (w - 1f);
                float v = y / (h - 1f);

                float dx = u - 0.5f;
                float dy = v - 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;

                float n = Fbm(u * 7f, v * 7f, 9091, 3);
                float a = Mathf.Clamp01(1f - d);
                a = Mathf.Pow(a, 2.0f) * (0.45f + n * 0.75f);

                px[y * w + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(a));
            }
        }

        return SaveTexture(name, w, h, px);
    }

    /// <summary>
    /// FIX quan trọng: bản cũ Destroy texture rồi vẫn trả về con trỏ đã chết.
    /// Hàm này luôn trả về ASSET đã import lại từ ổ đĩa.
    /// </summary>
    private static Texture2D SaveTexture(string name, int w, int h, Color[] pixels)
    {
        string path = TextureRoot + "/" + name + ".png";

        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.SetPixels(pixels);
        tex.Apply();

        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // =========================================================================
    // MATERIALS
    // =========================================================================

    private static Shader FindShader(params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            Shader s = Shader.Find(names[i]);
            if (s != null)
                return s;
        }

        return Shader.Find("Sprites/Default");
    }

    private static Material CreateMaterialAsset(string name, Shader shader)
    {
        string path = MaterialRoot + "/" + name + ".mat";

        if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
            AssetDatabase.DeleteAsset(path);

        Material mat = new Material(shader) { name = name };
        AssetDatabase.CreateAsset(mat, path);

        return mat;
    }

    private static Material CreateFlameMaterial(
        string name, Texture2D tex,
        Color core, Color mid, Color outer,
        float erosion, float scroll, float wobble)
    {
        Shader shader = FindShader(
            FlameShaderName,
            "Universal Render Pipeline/Particles/Unlit",
            "Universal Render Pipeline/Unlit");

        Material mat = CreateMaterialAsset(name, shader);

        SetTexture(mat, tex);

        if (mat.shader.name == FlameShaderName)
        {
            mat.SetColor("_ColorCore", core * 2.2f);
            mat.SetColor("_ColorMid", mid * 1.9f);
            mat.SetColor("_ColorOuter", outer * 1.5f);
            mat.SetFloat("_Erosion", erosion);
            mat.SetFloat("_ScrollSpeed", scroll);
            mat.SetFloat("_WobbleAmp", wobble);
            mat.SetFloat("_Intensity", 1f);
            mat.SetFloat("_Cull", 0f);
            mat.renderQueue = (int)RenderQueue.Transparent + 100;
        }
        else
        {
            ApplyTransparentBlend(mat, true);
            SetColor(mat, mid * 2f);
        }

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material CreateParticleMaterial(string name, Texture2D tex, Color tint, bool additive)
    {
        Shader shader = FindShader(
            "Universal Render Pipeline/Particles/Unlit",
            "Universal Render Pipeline/Unlit",
            "Particles/Standard Unlit");

        Material mat = CreateMaterialAsset(name, shader);

        SetTexture(mat, tex);
        SetColor(mat, tint);
        ApplyTransparentBlend(mat, additive);

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material CreateHeatMaterial(string name, Texture2D tex)
    {
        Shader shader = Shader.Find(HeatShaderName);

        if (shader == null)
        {
            // Dự phòng: quad mờ rất nhẹ, không méo ảnh nhưng không vỡ prefab.
            Material fallback = CreateParticleMaterial(name, tex, new Color(0.5f, 0.6f, 0.7f, 0.15f), false);
            return fallback;
        }

        Material mat = CreateMaterialAsset(name, shader);
        mat.SetTexture("_MaskTex", tex);
        mat.SetFloat("_Strength", 0.035f);
        mat.SetFloat("_Speed", 2.4f);
        mat.renderQueue = (int)RenderQueue.Transparent + 200;

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void SetTexture(Material mat, Texture2D tex)
    {
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
    }

    private static void SetColor(Material mat, Color color)
    {
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", color);
    }

    /// <summary>
    /// FIX: URP _Blend -> 0=Alpha, 1=Premultiply, 2=Additive, 3=Multiply.
    /// Bản cũ set 1 cho "additive" nên ra Premultiply + thiếu keyword/queue.
    /// </summary>
    private static void ApplyTransparentBlend(Material mat, bool additive)
    {
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", additive ? 2f : 0f);
        if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
        if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);
        if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 0f);

        if (mat.HasProperty("_SrcBlend"))
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);

        if (mat.HasProperty("_DstBlend"))
            mat.SetFloat("_DstBlend", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));

        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.DisableKeyword("_ALPHAMODULATE_ON");

        mat.SetShaderPassEnabled("ShadowCaster", false);
        mat.SetShaderPassEnabled("DepthOnly", false);

        mat.renderQueue = (int)RenderQueue.Transparent;
    }

    // =========================================================================
    // MESH: lưỡi lửa nhiều cánh xoay quanh trục Z (không phải 1 tờ phẳng)
    // =========================================================================

    private static Mesh BuildFlameMesh(
        string name,
        float length,
        float maxWidth,
        int blades,
        int segments,
        float seed,
        float taperPower,
        float twistDegrees,
        float wander)
    {
        blades = Mathf.Max(1, blades);
        segments = Mathf.Max(3, segments);

        int vertsPerBlade = (segments + 1) * 2;
        int totalVerts = vertsPerBlade * blades;

        Vector3[] verts = new Vector3[totalVerts];
        Vector2[] uvs = new Vector2[totalVerts];
        Color32[] cols = new Color32[totalVerts];
        int[] tris = new int[segments * 6 * blades];

        int vi = 0;
        int ti = 0;

        for (int b = 0; b < blades; b++)
        {
            float bladeSeed = seed + b * 1.379f;
            float angle = (b / (float)blades) * 180f + seed * 37f;

            float bladeWidth = maxWidth * Mathf.Lerp(0.70f, 1f, Mathf.Abs(Mathf.Sin(bladeSeed * 2.11f)));
            float bladeLength = length * Mathf.Lerp(0.78f, 1f, Mathf.Abs(Mathf.Cos(bladeSeed * 1.73f)));

            int bladeStart = vi;

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;

                float taper = Mathf.Pow(1f - t, taperPower);
                float w = bladeWidth * taper;

                if (i == segments)
                    w = 0.004f;

                float wave =
                    Mathf.Sin(t * 9.3f + bladeSeed * 3.31f) * 0.075f +
                    Mathf.Sin(t * 21.7f + bladeSeed * 1.91f) * 0.032f;

                float offset = wave * wander * (0.15f + t);

                float rad = (angle + twistDegrees * t) * Mathf.Deg2Rad;
                float cos = Mathf.Cos(rad);
                float sin = Mathf.Sin(rad);

                float z = t * bladeLength;

                float lx = offset - w;
                float rx = offset + w;

                verts[vi] = new Vector3(lx * cos, lx * sin, z);
                verts[vi + 1] = new Vector3(rx * cos, rx * sin, z);

                uvs[vi] = new Vector2(0f, t);
                uvs[vi + 1] = new Vector2(1f, t);

                byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(1f - t * 0.12f) * 255f);
                cols[vi] = new Color32(255, 255, 255, a);
                cols[vi + 1] = cols[vi];

                vi += 2;
            }

            for (int i = 0; i < segments; i++)
            {
                int l = bladeStart + i * 2;
                int r = l + 1;

                tris[ti++] = l;
                tris[ti++] = r;
                tris[ti++] = l + 2;

                tris[ti++] = r;
                tris[ti++] = r + 2;
                tris[ti++] = l + 2;
            }
        }

        Mesh mesh = new Mesh { name = name };
        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.colors32 = cols;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.UploadMeshData(false);

        string path = MeshRoot + "/" + name + ".asset";

        if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null)
            AssetDatabase.DeleteAsset(path);

        AssetDatabase.CreateAsset(mesh, path);
        return AssetDatabase.LoadAssetAtPath<Mesh>(path);
    }

    // =========================================================================
    // HIERARCHY HELPERS
    // =========================================================================

    private static GameObject NewChild(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        return go;
    }

    private static void CreateFlameLayer(
        Transform parent, string name, Mesh mesh, Material mat,
        Quaternion rotation, bool isCore,
        float lengthScale, float widthScale,
        float flickerSpeed, float flickerAmount,
        float swaySpeed, float swayAngle, float spinSpeed)
    {
        GameObject go = NewChild(parent, name);
        go.transform.localRotation = rotation;

        MeshFilter filter = go.AddComponent<MeshFilter>();
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();

        filter.sharedMesh = mesh;
        renderer.sharedMaterial = mat;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        renderer.allowOcclusionWhenDynamic = false;
        renderer.enabled = false;              // Nitro OFF = không có lửa

        NitroFlameLayer layer = go.AddComponent<NitroFlameLayer>();
        layer.isCore = isCore;
        layer.lengthScale = lengthScale;
        layer.widthScale = widthScale;
        layer.flickerSpeed = flickerSpeed;
        layer.flickerAmount = flickerAmount;
        layer.swaySpeed = swaySpeed;
        layer.swayAngle = swayAngle;
        layer.spinSpeed = spinSpeed;
        layer.minLength = isCore ? 0.35f : 0.22f;
    }

    private static ParticleSystem NewParticleSystem(
        Transform parent, string name, Material mat,
        NitroModuleRole role, bool oneShot, bool lingers,
        ParticleSystemRenderMode renderMode)
    {
        GameObject go = NewChild(parent, name);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();

        psr.renderMode = renderMode;
        psr.sharedMaterial = mat;
        psr.shadowCastingMode = ShadowCastingMode.Off;
        psr.receiveShadows = false;
        psr.lightProbeUsage = LightProbeUsage.Off;
        psr.reflectionProbeUsage = ReflectionProbeUsage.Off;
        psr.sortMode = ParticleSystemSortMode.Distance;
        psr.alignment = ParticleSystemRenderSpace.View;

        ParticleSystem.MainModule main = ps.main;
        main.playOnAwake = false;
        main.loop = !oneShot;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Local;
        main.cullingMode = ParticleSystemCullingMode.Automatic;

        NitroVfxModule module = go.AddComponent<NitroVfxModule>();
        module.role = role;
        module.oneShot = oneShot;
        module.lingersAfterStop = lingers;

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.Clear(true);

        return ps;
    }

    private static void SetGradient(ParticleSystem ps, Gradient gradient)
    {
        ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = new ParticleSystem.MinMaxGradient(gradient);
    }

    private static Gradient MakeGradient(Color a, Color b, Color c, float alphaIn, float alphaOut)
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(a, 0f),
                new GradientColorKey(b, 0.45f),
                new GradientColorKey(c, 1f)
            },
            new[]
            {
                new GradientAlphaKey(alphaIn, 0f),
                new GradientAlphaKey(1f, 0.25f),
                new GradientAlphaKey(alphaOut, 1f)
            });
        return g;
    }

    private static void SetSizeCurve(ParticleSystem ps, float start, float mid, float end)
    {
        AnimationCurve curve = new AnimationCurve(
            new Keyframe(0f, start),
            new Keyframe(0.35f, mid),
            new Keyframe(1f, end));

        ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, curve);
    }

    private static void SetNoise(ParticleSystem ps, float strength, float frequency, float scroll)
    {
        ParticleSystem.NoiseModule noise = ps.noise;
        noise.enabled = true;
        noise.quality = ParticleSystemNoiseQuality.Medium;
        noise.strength = strength;
        noise.frequency = frequency;
        noise.scrollSpeed = scroll;
        noise.damping = true;
    }

    // =========================================================================
    // ENERGY SYSTEM
    // =========================================================================

    private static void CreateCoreGlow(Transform parent, Material mat)
    {
        ParticleSystem ps = NewParticleSystem(parent, "CoreGlow", mat,
            NitroModuleRole.Glow, false, false, ParticleSystemRenderMode.Billboard);

        ParticleSystem.MainModule main = ps.main;
        main.startLifetime = 0.12f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.85f, 1.35f);
        main.startColor = new Color(0.55f, 0.85f, 1f, 0.85f);
        main.maxParticles = 24;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 26f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.04f;

        SetSizeCurve(ps, 0.55f, 1f, 0.25f);
        SetGradient(ps, MakeGradient(
            new Color(0.85f, 0.97f, 1f),
            new Color(0.35f, 0.75f, 1f),
            new Color(1f, 0.55f, 0.15f), 0.9f, 0f));
    }

    private static void CreateHotStreaks(Transform parent, Material mat)
    {
        ParticleSystem ps = NewParticleSystem(parent, "HotStreaks", mat,
            NitroModuleRole.Energy, false, false, ParticleSystemRenderMode.Stretch);

        ParticleSystem.MainModule main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.10f, 0.22f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(9f, 17f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.13f);
        main.startColor = new Color(1f, 0.88f, 0.55f, 1f);
        main.maxParticles = 90;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 45f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 5.5f;
        shape.radius = 0.05f;

        ParticleSystemRenderer psr = ps.GetComponent<ParticleSystemRenderer>();
        psr.lengthScale = 3.4f;
        psr.velocityScale = 0.06f;

        SetGradient(ps, MakeGradient(
            Color.white,
            new Color(1f, 0.82f, 0.35f),
            new Color(1f, 0.30f, 0.05f), 1f, 0f));
    }

    private static void CreateEnergyParticles(Transform parent, Material mat)
    {
        ParticleSystem ps = NewParticleSystem(parent, "EnergyParticles", mat,
            NitroModuleRole.OuterFlame, false, false, ParticleSystemRenderMode.Billboard);

        ParticleSystem.MainModule main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.16f, 0.36f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(4.5f, 11f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.55f);
        main.startRotation = new ParticleSystem.MinMaxCurve(-3.14f, 3.14f);
        main.startColor = new Color(1f, 0.75f, 0.35f, 1f);
        main.maxParticles = 140;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 70f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 11f;
        shape.radius = 0.07f;

        SetSizeCurve(ps, 0.45f, 1f, 0.15f);
        SetNoise(ps, 0.55f, 1.8f, 1.4f);
        SetGradient(ps, MakeGradient(
            new Color(0.7f, 0.9f, 1f),
            new Color(1f, 0.70f, 0.22f),
            new Color(0.9f, 0.14f, 0.02f), 0.85f, 0f));
    }

    // =========================================================================
    // BURST SYSTEM  (one-shot khi bật nitro)
    // =========================================================================

    private static void CreateInitialBurst(Transform parent, Material mat)
    {
        ParticleSystem ps = NewParticleSystem(parent, "InitialBurst", mat,
            NitroModuleRole.Burst, true, false, ParticleSystemRenderMode.Billboard);

        ParticleSystem.MainModule main = ps.main;
        main.duration = 0.30f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.30f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(6f, 15f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.30f, 0.95f);
        main.startRotation = new ParticleSystem.MinMaxCurve(-3.14f, 3.14f);
        main.maxParticles = 80;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 26, 34, 1, 0.01f) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 26f;
        shape.radius = 0.05f;

        SetSizeCurve(ps, 0.35f, 1.15f, 0.2f);
        SetGradient(ps, MakeGradient(
            Color.white,
            new Color(1f, 0.72f, 0.25f),
            new Color(0.85f, 0.12f, 0.02f), 1f, 0f));
    }

    private static void CreateBurstRays(Transform parent, Material mat)
    {
        ParticleSystem ps = NewParticleSystem(parent, "BurstRays", mat,
            NitroModuleRole.Burst, true, false, ParticleSystemRenderMode.Stretch);

        ParticleSystem.MainModule main = ps.main;
        main.duration = 0.22f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(16f, 30f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.16f);
        main.maxParticles = 48;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 14, 20, 1, 0.01f) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 13f;
        shape.radius = 0.03f;

        ParticleSystemRenderer psr = ps.GetComponent<ParticleSystemRenderer>();
        psr.lengthScale = 6.5f;
        psr.velocityScale = 0.05f;

        SetGradient(ps, MakeGradient(
            Color.white,
            new Color(1f, 0.85f, 0.45f),
            new Color(1f, 0.35f, 0.05f), 1f, 0f));
    }

    private static void CreateBurstSparks(Transform parent, Material mat)
    {
        ParticleSystem ps = NewParticleSystem(parent, "BurstSparks", mat,
            NitroModuleRole.Burst, true, false, ParticleSystemRenderMode.Stretch);

        ParticleSystem.MainModule main = ps.main;
        main.duration = 0.35f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.30f, 0.75f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(8f, 22f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
        main.gravityModifier = 0.55f;
        main.maxParticles = 70;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 24, 38, 1, 0.01f) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 34f;
        shape.radius = 0.05f;

        ParticleSystemRenderer psr = ps.GetComponent<ParticleSystemRenderer>();
        psr.lengthScale = 2.6f;
        psr.velocityScale = 0.05f;

        SetGradient(ps, MakeGradient(
            Color.white,
            new Color(1f, 0.62f, 0.15f),
            new Color(0.7f, 0.10f, 0.01f), 1f, 0f));
    }

    // =========================================================================
    // SPARK SYSTEM
    // =========================================================================

    private static void CreateSparksNear(Transform parent, Material mat)
    {
        ParticleSystem ps = NewParticleSystem(parent, "SparksNear", mat,
            NitroModuleRole.Sparks, false, true, ParticleSystemRenderMode.Stretch);

        ParticleSystem.MainModule main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.20f, 0.45f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(6f, 14f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.06f);
        main.gravityModifier = 0.30f;
        main.maxParticles = 120;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 40f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 22f;
        shape.radius = 0.06f;

        ParticleSystemRenderer psr = ps.GetComponent<ParticleSystemRenderer>();
        psr.lengthScale = 2.2f;
        psr.velocityScale = 0.05f;

        SetNoise(ps, 0.35f, 2.4f, 1.2f);
        SetGradient(ps, MakeGradient(
            Color.white,
            new Color(1f, 0.60f, 0.15f),
            new Color(0.75f, 0.08f, 0.01f), 1f, 0f));
    }

    private static void CreateSparksFar(Transform parent, Material mat)
    {
        ParticleSystem ps = NewParticleSystem(parent, "SparksFar", mat,
            NitroModuleRole.Sparks, false, true, ParticleSystemRenderMode.Stretch);

        ParticleSystem.MainModule main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 1.25f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(14f, 26f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.045f);
        main.gravityModifier = 0.75f;
        main.maxParticles = 90;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 22f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 9f;
        shape.radius = 0.05f;

        ParticleSystemRenderer psr = ps.GetComponent<ParticleSystemRenderer>();
        psr.lengthScale = 4.5f;
        psr.velocityScale = 0.05f;

        SetGradient(ps, MakeGradient(
            new Color(1f, 0.92f, 0.70f),
            new Color(1f, 0.48f, 0.10f),
            new Color(0.55f, 0.06f, 0.01f), 1f, 0f));
    }

    // =========================================================================
    // SMOKE / HEAT / LIGHT
    // =========================================================================

    private static void CreateExhaustSmoke(Transform parent, Material mat)
    {
        ParticleSystem ps = NewParticleSystem(parent, "ExhaustSmoke", mat,
            NitroModuleRole.Smoke, false, true, ParticleSystemRenderMode.Billboard);

        ParticleSystem.MainModule main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.65f);
        main.startRotation = new ParticleSystem.MinMaxCurve(-3.14f, 3.14f);
        main.startColor = new Color(0.30f, 0.33f, 0.36f, 0.55f);
        main.gravityModifier = -0.04f;
        main.maxParticles = 90;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 26f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 15f;
        shape.radius = 0.08f;

        ParticleSystem.RotationOverLifetimeModule rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-1.1f, 1.1f);

        SetSizeCurve(ps, 0.35f, 1f, 2.3f);
        SetNoise(ps, 0.45f, 0.8f, 0.6f);
        SetGradient(ps, MakeGradient(
            new Color(0.55f, 0.45f, 0.38f),
            new Color(0.30f, 0.32f, 0.34f),
            new Color(0.18f, 0.19f, 0.21f), 0f, 0f));
    }

    private static void CreateHeatHaze(Transform parent, Material mat)
    {
        ParticleSystem ps = NewParticleSystem(parent, "HeatHaze", mat,
            NitroModuleRole.Heat, false, false, ParticleSystemRenderMode.Billboard);

        ParticleSystem.MainModule main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
        main.startRotation = new ParticleSystem.MinMaxCurve(-3.14f, 3.14f);
        main.startColor = new Color(1f, 1f, 1f, 0.8f);
        main.maxParticles = 30;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 14f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 9f;
        shape.radius = 0.06f;

        SetSizeCurve(ps, 0.5f, 1f, 1.6f);
        SetGradient(ps, MakeGradient(Color.white, Color.white, Color.white, 0f, 0f));

        ParticleSystemRenderer psr = ps.GetComponent<ParticleSystemRenderer>();
        psr.sortingFudge = -5f;
    }

    private static void CreateNitroLight(
        Transform parent, string name, Vector3 localPos,
        Color color, float intensity, float range,
        float flickerAmount, float flickerSpeed)
    {
        GameObject go = NewChild(parent, name);
        go.transform.localPosition = localPos;

        Light light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
        light.renderMode = LightRenderMode.Auto;
        light.enabled = false;               // Nitro OFF = tối hoàn toàn

        NitroLightFlicker flicker = go.AddComponent<NitroLightFlicker>();
        flicker.baseIntensity = intensity;
        flicker.baseRange = range;
        flicker.flickerAmount = flickerAmount;
        flicker.flickerSpeed = flickerSpeed;
    }

    private class ExhaustNitroVFX
    {
    }
}

#endif