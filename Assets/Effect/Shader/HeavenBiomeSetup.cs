#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LeadKhongPhanh.EditorTools
{
    /// <summary>
    /// HEAVEN ROAD — PROCEDURAL ENVIRONMENT COMPOSITION SYSTEM
    /// Unity 6 / Editor-only generator.
    ///
    /// IMPORTANT:
    /// - Visual environment only.
    /// - Does not modify PlayerController, RoadManager, Traffic, Ramp, lanes,
    ///   gameplay Z, colliders, rigidbodies or gameplay positions.
    /// - All generated objects live under HEAVEN_BIOME_GENERATED.
    /// - Deterministic layout from seed.
    /// - CREATE HEAVEN BIOME is idempotent: previous generated hierarchy is cleared.
    /// </summary>
    [ExecuteAlways]
    public sealed class HeavenBiomeSetup : MonoBehaviour
    {
        private const string RootName = "HEAVEN_BIOME_GENERATED";
        private const string Version = "HEAVEN CINEMATIC CLOUD CITY v4.3 PREFAB SAFE MATERIAL ASSETS";

        public enum Quality
        {
            Preview,
            Standard,
            High,
            Cinematic
        }

        [Header("WORLD")]
        [SerializeField] private int seed = 20260924;
        [SerializeField] private Quality quality = Quality.High;
        [SerializeField] private bool clearBeforeGenerate = true;
        [SerializeField] private bool markStatic = true;
        [SerializeField] private float startZ = 0f;
        [SerializeField] private float endZ = 520f;
        [SerializeField] private float roadHalfWidth = 10f;

        [Header("CINEMATIC COMPOSITION")]
        [SerializeField] private float sideArchitectureMinX = 18f;
        [SerializeField] private float sideArchitectureMaxX = 115f;
        [SerializeField] private float cloudOceanWidth = 360f;
        [SerializeField] private float cloudOceanDepth = 560f;
        [SerializeField] private float architectureDensity = 1f;
        [SerializeField] private bool createCloudFloor = true;
        [SerializeField] private bool createHorizonFog = true;

        [Header("DEPTH")]
        [SerializeField] private float nearEnd = 40f;
        [SerializeField] private float midEnd = 150f;
        [SerializeField] private float farEnd = 500f;

        [Header("SCALE")]
        [SerializeField] private float worldScale = 1f;
        [SerializeField] private float landmarkScale = 1f;

        [Header("PREFABS - OPTIONAL AUTO DISCOVERY")]
        [SerializeField] private bool useProjectPrefabs = true;
        [HideInInspector] private GameObject treePrefab;
        [HideInInspector] private GameObject buildingPrefab;
        [HideInInspector] private GameObject towerPrefab;
        [HideInInspector] private GameObject cloudPrefab;
        [HideInInspector] private GameObject statuePrefab;

        [Header("MATERIAL")]
        [SerializeField] private bool createMaterials = true;
        [SerializeField] private bool useEmission = true;

        [Header("LIGHTING")]
        [SerializeField] private bool createLighting = true;
        [SerializeField] private bool createSkyBackdrop = true;

        [Header("OPTIONAL PLACEMENT")]
        [Tooltip("Tự đặt Heaven bắt đầu ngay tại Z của Player trong Editor.")]
        [SerializeField] private bool anchorToPlayer = true;

        [Tooltip("Editor placement only. Nếu không tìm thấy Player thì dùng Z này.")]
        [SerializeField] private float compositionOriginZ = 0f;

        [Tooltip("Nếu bật, Generator sẽ tự đưa Transform của nó về World Origin để tránh lệch toàn bộ Heaven.")]
        [SerializeField] private bool forceGeneratorWorldOrigin = true;

        private Transform root;
        private Transform roadEnvironment;
        private Transform left;
        private Transform leftForeground;
        private Transform leftMidground;
        private Transform leftBackground;
        private Transform right;
        private Transform rightForeground;
        private Transform rightMidground;
        private Transform rightBackground;
        private Transform floatingIslands;
        private Transform celestialArchitecture;
        private Transform landmarks;
        private Transform waterfalls;
        private Transform cloudOcean;
        private Transform farBackground;
        private Transform atmosphere;
        private Transform lightingRoot;

        private readonly Dictionary<string, Material> materials =
            new Dictionary<string, Material>(StringComparer.Ordinal);

        private readonly List<GameObject> generatedObjects =
            new List<GameObject>(2048);

        private System.Random rng;

        [MenuItem("LEAD KHÔNG PHANH/Heaven Biome/CREATE HEAVEN BIOME - AUTO SETUP")]
        private static void CreateAndGenerate()
        {
            HeavenBiomeSetup setup =
                FindFirstObjectByType<HeavenBiomeSetup>();

            if (setup == null)
            {
                GameObject go =
                    new GameObject("HeavenBiomeGenerator");

                Undo.RegisterCreatedObjectUndo(
                    go,
                    "Create Heaven Biome Generator"
                );

                setup =
                    go.AddComponent<HeavenBiomeSetup>();
            }

            // Không yêu cầu kéo/thả bất kỳ reference nào.
            // Generator tự resolve Player, prefab, material, hierarchy,
            // lighting và toàn bộ environment.
            setup.useProjectPrefabs = true;
            setup.createMaterials = true;
            setup.createLighting = true;
            setup.createSkyBackdrop = true;
            setup.anchorToPlayer = true;
            setup.Generate();

            Selection.activeGameObject =
                setup.gameObject;

            EditorGUIUtility.PingObject(
                setup.gameObject
            );
        }

        [MenuItem("LEAD KHÔNG PHANH/Heaven Biome/Create Generator")]
        private static void CreateGenerator()
        {
            HeavenBiomeSetup existing =
                FindFirstObjectByType<HeavenBiomeSetup>();

            if (existing != null)
            {
                Selection.activeGameObject =
                    existing.gameObject;

                EditorGUIUtility.PingObject(
                    existing.gameObject
                );

                return;
            }

            GameObject go =
                new GameObject("HeavenBiomeGenerator");

            Undo.RegisterCreatedObjectUndo(
                go,
                "Create Heaven Biome Generator"
            );

            HeavenBiomeSetup setup =
                go.AddComponent<HeavenBiomeSetup>();

            Selection.activeGameObject = go;

            EditorGUIUtility.PingObject(go);
        }

        [MenuItem("LEAD KHÔNG PHANH/Heaven Biome/Create Heaven Biome")]
        private static void GenerateSelected()
        {
            HeavenBiomeSetup setup = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponent<HeavenBiomeSetup>()
                : FindFirstObjectByType<HeavenBiomeSetup>();

            if (setup == null)
            {
                GameObject go = new GameObject("HeavenBiomeGenerator");
                Undo.RegisterCreatedObjectUndo(go, "Create Heaven Biome Generator");
                setup = go.AddComponent<HeavenBiomeSetup>();
            }

            setup.Generate();
            Selection.activeGameObject = setup.gameObject;
        }

        [MenuItem("LEAD KHÔNG PHANH/Heaven Biome/Clear Heaven Biome")]
        private static void ClearSelected()
        {
            HeavenBiomeSetup setup = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponent<HeavenBiomeSetup>()
                : FindFirstObjectByType<HeavenBiomeSetup>();

            if (setup != null)
                setup.ClearGenerated();
        }

        public void Generate()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[HeavenBiomeSetup] Generate is Editor-only. Stop Play Mode first.", this);
                return;
            }

            // Generator là editor-only helper: luôn đặt ở world origin
            // để Transform của Generator không làm lệch toàn bộ composition.
            if (forceGeneratorWorldOrigin)
            {
                transform.position = Vector3.zero;
                transform.rotation = Quaternion.identity;
                transform.localScale = Vector3.one;
            }

            // FULL AUTO MODE:
            // Người dùng không cần gán bất kỳ thành phần nào.
            // Mọi dependency visual đều được tạo/tìm tự động.
            useProjectPrefabs = true;
            createMaterials = true;
            createLighting = true;
            createSkyBackdrop = true;

            ResolveCompositionOrigin();

            NormalizeSettings();
            rng = new System.Random(seed);

            if (clearBeforeGenerate)
                ClearGeneratedInternal();

            CreateHierarchy();
            ResolveOptionalPrefabs();

            if (createMaterials)
                BuildMaterialPalette();

            // Material là Asset thật trong Project, không phải object tạm.
            // Điều này giúp Save As Prefab giữ shader/material reference.
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            GenerateRoadEnvironment();
            GenerateLeftComposition();
            GenerateRightComposition();
            GenerateFloatingIslands();
            GenerateCelestialArchitecture();
            GenerateLandmarks();
            GenerateWaterfalls();
            GenerateCloudOcean();
            GenerateFarBackground();
            GenerateAtmosphere();

            if (createLighting)
                GenerateLighting();

            if (createSkyBackdrop)
                GenerateSkyBackdrop();

            MarkHierarchyStatic();
            SaveScene();

            root.gameObject.SetActive(true);
            Selection.activeGameObject = root.gameObject;
            EditorGUIUtility.PingObject(root.gameObject);

            // Đưa Scene View tới đúng Heaven vừa tạo.
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.LookAt(
                    root.position + new Vector3(0f, 8f, 35f),
                    Quaternion.Euler(12f, 0f, 0f),
                    90f
                );

                SceneView.lastActiveSceneView.Repaint();
            }

            EditorApplication.QueuePlayerLoopUpdate();

            Debug.Log(
                "[HeavenBiomeSetup] " + Version +
                " generated. Seed=" + seed +
                " Objects=" + generatedObjects.Count +
                " | RootWorld=" + root.position +
                " | LocalSpace=ON",
                root
            );
        }

        public void ClearGenerated()
        {
            if (Application.isPlaying)
                return;

            ClearGeneratedInternal();
            Debug.Log("[HeavenBiomeSetup] HEAVEN_BIOME_GENERATED cleared.", this);
        }

        private void NormalizeSettings()
        {
            if (worldScale <= 0.01f) worldScale = 1f;
            if (landmarkScale <= 0.01f) landmarkScale = 1f;
            if (endZ < startZ + 100f) endZ = startZ + 520f;
            if (nearEnd < 20f) nearEnd = 40f;
            if (midEnd < nearEnd + 30f) midEnd = 150f;
            if (farEnd < midEnd + 100f) farEnd = 500f;
            // Không được ghi đè compositionOriginZ tại đây.
            // Heaven phải giữ đúng vị trí đã resolve từ Player/fallback.
        }

        private void ResolveCompositionOrigin()
        {
            if (!anchorToPlayer)
                return;

            Transform player = null;

            // Ưu tiên tag Player.
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;

            // Fallback theo tên để không phụ thuộc Tag.
            if (player == null)
            {
                string[] names =
                {
                    "Player",
                    "PlayerController",
                    "LEAD",
                    "Lead",
                    "Bike"
                };

                for (int i = 0; i < names.Length; ++i)
                {
                    GameObject candidate = GameObject.Find(names[i]);
                    if (candidate != null)
                    {
                        player = candidate.transform;
                        break;
                    }
                }
            }

            if (player != null)
            {
                compositionOriginZ = player.position.z;

                Debug.Log(
                    "[HeavenBiomeSetup] Heaven anchored to Player Z = " +
                    compositionOriginZ.ToString("F2"),
                    player
                );
            }
            else
            {
                Debug.LogWarning(
                    "[HeavenBiomeSetup] Không tìm thấy Player trong Editor. " +
                    "Heaven dùng compositionOriginZ = " +
                    compositionOriginZ.ToString("F2") +
                    ".",
                    this
                );
            }
        }

        private void ClearGeneratedInternal()
        {
            GameObject old = GameObject.Find(RootName);
            if (old != null)
            {
                if (Application.isPlaying)
                    Destroy(old);
                else
                    DestroyImmediate(old);
            }

            root = null;
            generatedObjects.Clear();
            materials.Clear();
        }

        private void CreateHierarchy()
        {
            root = NewRoot(RootName, transform);
            root.localRotation = Quaternion.identity;
            root.localScale = Vector3.one;
            root.position = new Vector3(0f, 0f, compositionOriginZ);

            roadEnvironment = NewRoot("ROAD_ENVIRONMENT", root);
            left = NewRoot("LEFT_SIDE", root);
            leftForeground = NewRoot("FOREGROUND", left);
            leftForeground.name = "FOREGROUND";
            leftMidground = NewRoot("MIDGROUND", left);
            leftBackground = NewRoot("BACKGROUND", left);

            right = NewRoot("RIGHT_SIDE", root);
            rightForeground = NewRoot("FOREGROUND", right);
            rightMidground = NewRoot("MIDGROUND", right);
            rightBackground = NewRoot("BACKGROUND", right);

            floatingIslands = NewRoot("FLOATING_ISLANDS", root);
            celestialArchitecture = NewRoot("CELESTIAL_ARCHITECTURE", root);
            landmarks = NewRoot("LANDMARKS", root);
            waterfalls = NewRoot("WATERFALLS", root);
            cloudOcean = NewRoot("CLOUD_OCEAN", root);
            farBackground = NewRoot("FAR_BACKGROUND", root);
            atmosphere = NewRoot("ATMOSPHERE", root);
            lightingRoot = NewRoot("LIGHTING", root);
        }

        private Transform NewRoot(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private void ResolveOptionalPrefabs()
        {
            if (!useProjectPrefabs)
                return;

            if (treePrefab == null)
                treePrefab = FindPrefab("Tree", "FantasyTree", "Tree_Palm", "GlowingTree");
            if (buildingPrefab == null)
                buildingPrefab = FindPrefab("Temple", "Building", "Palace", "Celestial");
            if (towerPrefab == null)
                towerPrefab = FindPrefab("Tower", "Pagoda", "TempleTower");
            if (cloudPrefab == null)
                cloudPrefab = FindPrefab("Cloud", "Clouds", "CloudVolume");
            if (statuePrefab == null)
                statuePrefab = FindPrefab("Statue", "GiantStatue");
        }

        private GameObject FindPrefab(params string[] keywords)
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab");
            for (int i = 0; i < guids.Length; ++i)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                string lower = path.ToLowerInvariant();

                for (int k = 0; k < keywords.Length; ++k)
                {
                    if (lower.Contains(keywords[k].ToLowerInvariant()))
                    {
                        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (prefab != null)
                            return prefab;
                    }
                }
            }

            return null;
        }

        private void BuildMaterialPalette()
        {
            materials.Clear();

            materials["MARBLE"] = CreateMaterial(
                "HEAVEN_MARBLE",
                new Color(0.93f, 0.96f, 1f, 1f),
                new Color(0.015f, 0.02f, 0.025f, 1f),
                0.18f
            );

            materials["IVORY"] = CreateMaterial(
                "HEAVEN_IVORY",
                new Color(1f, 0.93f, 0.72f, 1f),
                new Color(0.01f, 0.006f, 0.002f, 1f),
                0.2f
            );

            materials["GOLD"] = CreateMaterial(
                "HEAVEN_GOLD",
                new Color(1f, 0.58f, 0.08f, 1f),
                new Color(0.32f, 0.12f, 0.015f, 1f),
                0.22f
            );

            materials["CYAN"] = CreateMaterial(
                "HEAVEN_CYAN",
                new Color(0.18f, 0.85f, 1f, 1f),
                new Color(0.02f, 0.28f, 0.45f, 1f),
                0.08f
            );

            materials["GLOW"] = CreateMaterial(
                "HEAVEN_GLOW_WHITE",
                new Color(1f, 1f, 1f, 1f),
                new Color(0.65f, 0.82f, 1f, 1f),
                0.05f
            );

            materials["CRYSTAL"] = CreateMaterial(
                "HEAVEN_CRYSTAL",
                new Color(0.35f, 0.9f, 1f, 0.72f),
                new Color(0.05f, 0.35f, 0.55f, 1f),
                0.05f
            );

            materials["CLOUD"] = CreateMaterial(
                "HEAVEN_CLOUD",
                new Color(0.96f, 0.99f, 1f, 1f),
                new Color(0.04f, 0.06f, 0.09f, 1f),
                0.35f
            );

            materials["HORIZON"] = CreateMaterial(
                "HEAVEN_HORIZON",
                new Color(0.74f, 0.88f, 1f, 1f),
                new Color(0.22f, 0.42f, 0.75f, 1f),
                0.02f
            );

            materials["DARK_ROCK"] = CreateMaterial(
                "HEAVEN_FLOATING_ROCK",
                new Color(0.24f, 0.30f, 0.38f, 1f),
                new Color(0.005f, 0.006f, 0.01f, 1f),
                0.95f
            );

            materials["VEGETATION"] = CreateMaterial(
                "HEAVEN_VEGETATION",
                new Color(0.32f, 0.85f, 0.56f, 1f),
                new Color(0.015f, 0.12f, 0.04f, 1f),
                0.3f
            );

            materials["WATER"] = CreateMaterial(
                "HEAVEN_WATER",
                new Color(0.25f, 0.82f, 1f, 0.72f),
                new Color(0.02f, 0.2f, 0.35f, 1f),
                0.08f
            );
        }

        private const string HeavenMaterialFolder =
            "Assets/Generated/HeavenBiome/Materials";

        private void EnsureHeavenMaterialFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Generated"))
                AssetDatabase.CreateFolder("Assets", "Generated");

            if (!AssetDatabase.IsValidFolder("Assets/Generated/HeavenBiome"))
                AssetDatabase.CreateFolder("Assets/Generated", "HeavenBiome");

            if (!AssetDatabase.IsValidFolder(HeavenMaterialFolder))
                AssetDatabase.CreateFolder(
                    "Assets/Generated/HeavenBiome",
                    "Materials"
                );
        }

        private Shader ResolvePrefabSafeShader()
        {
            // URP first. These shader assets survive Prefab serialization.
            Shader shader = Shader.Find(
                "Universal Render Pipeline/Lit"
            );

            if (shader != null)
                return shader;

            shader = Shader.Find(
                "Universal Render Pipeline/Simple Lit"
            );

            if (shader != null)
                return shader;

            shader = Shader.Find(
                "Universal Render Pipeline/Unlit"
            );

            if (shader != null)
                return shader;

            // Built-in Render Pipeline fallback.
            shader = Shader.Find("Standard");

            if (shader != null)
                return shader;

            shader = Shader.Find("Sprites/Default");

            return shader;
        }

        private Material CreateMaterial(
            string materialName,
            Color baseColor,
            Color emissionColor,
            float metallic)
        {
            EnsureHeavenMaterialFolder();

            Shader shader = ResolvePrefabSafeShader();

            if (shader == null)
            {
                Debug.LogError(
                    "[HeavenBiomeSetup] Không tìm thấy shader hợp lệ. " +
                    "Material Heaven không thể tạo."
                );

                return null;
            }

            string assetPath =
                HeavenMaterialFolder +
                "/" +
                materialName +
                ".mat";

            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(
                    assetPath
                );

            if (material == null)
            {
                material = new Material(shader);
                material.name = materialName;

                AssetDatabase.CreateAsset(
                    material,
                    assetPath
                );
            }
            else
            {
                // Quan trọng: ép lại shader hiện tại để asset cũ
                // không giữ Hidden/InternalErrorShader / shader cũ.
                material.shader = shader;
            }

            if (material.shader == null ||
                material.shader.name == "Hidden/InternalErrorShader")
            {
                Debug.LogError(
                    "[HeavenBiomeSetup] INVALID SHADER | " +
                    materialName +
                    " | Shader=" +
                    (
                        material.shader != null
                            ? material.shader.name
                            : "NULL"
                    )
                );

                return material;
            }

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", baseColor);
            else if (material.HasProperty("_Color"))
                material.SetColor("_Color", baseColor);

            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", metallic);

            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", 0.55f);

            if (useEmission &&
                material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor(
                    "_EmissionColor",
                    emissionColor
                );
            }

            if ((materialName.Contains("CLOUD") ||
                 materialName.Contains("WATER")) &&
                material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);

                if (material.HasProperty("_Blend"))
                    material.SetFloat("_Blend", 0f);

                if (material.HasProperty("_AlphaClip"))
                    material.SetFloat("_AlphaClip", 0f);

                if (material.HasProperty("_SrcBlend"))
                {
                    material.SetFloat(
                        "_SrcBlend",
                        (float)UnityEngine.Rendering.BlendMode.SrcAlpha
                    );
                }

                if (material.HasProperty("_DstBlend"))
                {
                    material.SetFloat(
                        "_DstBlend",
                        (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha
                    );
                }

                if (material.HasProperty("_ZWrite"))
                    material.SetFloat("_ZWrite", 0f);

                material.renderQueue =
                    (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }

            EditorUtility.SetDirty(material);

            return material;
        }

        private Material Mat(string key)
        {
            Material material;
            if (materials.TryGetValue(key, out material))
                return material;

            return null;
        }

        // ============================================================
        // COMPOSITION
        // ============================================================

        private void GenerateRoadEnvironment()
        {
            Transform edge = NewRoot("CELESTIAL_ROAD_EDGES", roadEnvironment);
            Transform stones = NewRoot("MAGICAL_STONES", roadEnvironment);
            Transform flowers = NewRoot("FLOWER_STRIPS", roadEnvironment);
            Transform pillars = NewRoot("LIGHT_PILLARS", roadEnvironment);

            // Purely visual decoration. Road width/Z/lane logic is untouched.
            int count = 34;
            for (int i = 0; i < count; ++i)
            {
                float z = startZ + 5f + i * 15f;
                float side = (i & 1) == 0 ? -1f : 1f;

                CreateCube(
                    "GoldenRoadEdge_" + i,
                    edge,
                    new Vector3(side * (roadHalfWidth + 0.45f), 0.08f, z),
                    new Vector3(0.22f, 0.16f, 12f),
                    Mat("GOLD")
                );

                CreateCrystal(
                    "RoadCrystal_" + i,
                    stones,
                    new Vector3(
                        side * (roadHalfWidth + 2.0f + RandomRange(0f, 1.4f)),
                        0.35f,
                        z + RandomRange(-3f, 3f)
                    ),
                    RandomRange(0.28f, 0.62f),
                    RandomRange(0.8f, 1.4f)
                );

                CreateFlowerPatch(
                    "RoadFlowers_" + i,
                    flowers,
                    side,
                    z,
                    2.2f
                );

                if (i % 2 == 0)
                {
                    CreateLightPillar(
                        "RoadLight_" + i,
                        pillars,
                        new Vector3(side * (roadHalfWidth + 4.8f), 2.5f, z),
                        4.2f
                    );
                }
            }
        }

        private void GenerateLeftComposition()
        {
            // LEFT = CELESTIAL GARDEN / PALACE PROMENADE
            int foregroundCount = Mathf.RoundToInt(24f * architectureDensity);
            for (int i = 0; i < foregroundCount; ++i)
            {
                float z = 4f + i * 11.5f;
                float x = 18f + (i % 4) * 5f + RandomRange(0f, 5f);

                CreateGardenCluster(
                    leftForeground,
                    -1f,
                    z,
                    x,
                    i
                );

                // Architectural rhythm: gate / pavilion / tower / garden.
                int pattern = i & 3;
                Vector3 basePos = new Vector3(
                    -(x + 6f + RandomRange(0f, 5f)),
                    0f,
                    z + RandomRange(2f, 6f)
                );

                if (pattern == 0)
                {
                    CreateArch("LeftGardenGate_" + i, leftForeground,
                        basePos + Vector3.up * 1.5f, 10f, 12f, 0f);
                }
                else if (pattern == 1)
                {
                    CreateSmallCelestialBuilding(leftForeground, basePos, 0.72f);
                }
                else if (pattern == 2)
                {
                    CreateTower(leftForeground, basePos + Vector3.up * 0.5f, 0.62f);
                }
                else
                {
                    CreateSmallArch("LeftGardenArch_" + i, leftForeground,
                        basePos + Vector3.up * 1.5f, 7f);
                }
            }

            // Midground floating gardens form a continuous visual belt.
            int midCount = Mathf.RoundToInt(12f * architectureDensity);
            for (int i = 0; i < midCount; ++i)
            {
                float z = 45f + i * 17f;
                float x = 32f + RandomRange(0f, 18f);
                float y = 10f + RandomRange(0f, 7f);

                CreateFloatingGarden(
                    leftMidground,
                    new Vector3(-x, y, z),
                    7f + RandomRange(0f, 6f),
                    i
                );

                if ((i & 1) == 0)
                {
                    CreateArch("LeftMidArch_" + i, leftMidground,
                        new Vector3(-x, y + 4f, z + 5f), 12f, 15f, 0f);
                }
            }

            // Long background city-garden silhouettes.
            CreateTempleCluster(
                leftBackground,
                new Vector3(-55f, 12f, 105f),
                1.45f,
                "LeftGardenTemple_A"
            );

            CreateTempleCluster(
                leftBackground,
                new Vector3(-82f, 23f, 205f),
                2.1f,
                "LeftGardenTemple_B"
            );

            CreatePalace(
                "LeftFarPalace",
                leftBackground,
                new Vector3(-112f, 42f, 330f),
                3.2f
            );
        }

        private void GenerateRightComposition()
        {
            // RIGHT = CELESTIAL CITY / CONTINUOUS ARCHITECTURAL FRONTAGE
            int foregroundCount = Mathf.RoundToInt(22f * architectureDensity);
            for (int i = 0; i < foregroundCount; ++i)
            {
                float z = 8f + i * 12.5f;
                float x = 20f + (i % 4) * 5f + RandomRange(0f, 5f);
                Vector3 p = new Vector3(x, 0f, z);

                CreateSmallCelestialBuilding(
                    rightForeground,
                    p,
                    0.68f + RandomRange(0f, 0.22f)
                );

                if ((i & 1) == 0)
                {
                    CreateTower(
                        rightForeground,
                        p + new Vector3(9f, 1f, 4f),
                        0.55f + RandomRange(0f, 0.22f)
                    );
                }

                if ((i % 3) == 0)
                {
                    CreateArch(
                        "RightCityGate_" + i,
                        rightForeground,
                        p + new Vector3(6f, 2f, 7f),
                        11f,
                        13f,
                        0f
                    );
                }

                CreateCrystalCluster(
                    rightForeground,
                    1f,
                    z + 3f,
                    x + 10f,
                    i
                );
            }

            int midCount = Mathf.RoundToInt(11f * architectureDensity);
            for (int i = 0; i < midCount; ++i)
            {
                float z = 48f + i * 20f;
                float x = 35f + RandomRange(0f, 22f);
                float y = 11f + RandomRange(0f, 8f);

                CreateCityIslandCluster(
                    rightMidground,
                    new Vector3(x, y, z),
                    9f + RandomRange(0f, 8f),
                    i
                );

                CreateCelestialBridge(
                    "RightMidBridge_" + i,
                    rightMidground,
                    new Vector3(x + 7f, y + 7f, z + 9f),
                    9f + RandomRange(0f, 5f),
                    20f + RandomRange(0f, 12f)
                );
            }

            // Giant background frontage.
            CreateCityTowerCluster(
                rightBackground,
                new Vector3(62f, 8f, 118f),
                1.55f,
                "CelestialCityCluster_A"
            );

            CreateCityTowerCluster(
                rightBackground,
                new Vector3(88f, 23f, 245f),
                2.2f,
                "CelestialCityCluster_B"
            );

            CreatePalace(
                "RightFarPalace",
                rightBackground,
                new Vector3(112f, 45f, 355f),
                3.4f
            );
        }

        private void GenerateFloatingIslands()
        {
            // The image language is: islands are part of the skyline, not isolated props.
            CreateIslandWithFeature("FloatingGarden_SMALL_A",
                new Vector3(-24f, 16f, 62f), 6f, 0);
            CreateIslandWithFeature("FloatingCity_SMALL_B",
                new Vector3(31f, 23f, 88f), 8f, 1);
            CreateIslandWithFeature("FloatingWaterfall_MEDIUM_A",
                new Vector3(-46f, 30f, 128f), 16f, 2);
            CreateIslandWithFeature("FloatingTemple_MEDIUM_B",
                new Vector3(48f, 36f, 158f), 18f, 4);
            CreateIslandWithFeature("FloatingCrystal_LARGE_A",
                new Vector3(-70f, 48f, 215f), 30f, 3);
            CreateIslandWithFeature("FloatingTemple_LARGE_B",
                new Vector3(80f, 58f, 285f), 38f, 4);
            CreateIslandWithFeature("FloatingCelestial_GIANT",
                new Vector3(-110f, 78f, 400f), 55f, 1);

            for (int i = 0; i < 16; ++i)
            {
                float z = 105f + i * 25f;
                float side = (i & 1) == 0 ? -1f : 1f;
                float x = side * RandomRange(70f, 145f);
                float y = RandomRange(25f, 82f);
                float radius = RandomRange(7f, 24f);

                CreateIslandWithFeature(
                    "SkylineIsland_" + i,
                    new Vector3(x, y, z),
                    radius,
                    i + 2
                );
            }
        }

        private void GenerateCelestialArchitecture()
        {
            // Repeated arches make the sides read as one designed city rather than random objects.
            for (int i = 0; i < 12; ++i)
            {
                float z = 18f + i * 23f;
                float x = 24f + (i % 3) * 7f;

                CreateArch(
                    "LeftProcessionalArch_" + i,
                    celestialArchitecture,
                    new Vector3(-x, 4f + (i % 2) * 1.5f, z),
                    10f + (i % 3) * 2f,
                    13f + (i % 4) * 2f,
                    0f
                );

                CreateArch(
                    "RightProcessionalArch_" + i,
                    celestialArchitecture,
                    new Vector3(x, 4.5f + (i % 2) * 1.5f, z + 8f),
                    11f + (i % 3) * 2f,
                    14f + (i % 4) * 2f,
                    0f
                );
            }

            CreateGiantStaircase(
                "SkyStaircase_Left",
                celestialArchitecture,
                new Vector3(-52f, 11f, 138f),
                22f,
                42f
            );

            CreateGiantStaircase(
                "SkyStaircase_Right",
                celestialArchitecture,
                new Vector3(58f, 14f, 178f),
                25f,
                48f
            );

            CreateCelestialBridge(
                "CelestialBridge_Left",
                celestialArchitecture,
                new Vector3(-68f, 30f, 225f),
                28f,
                65f
            );

            CreateCelestialBridge(
                "CelestialBridge_Right",
                celestialArchitecture,
                new Vector3(72f, 34f, 260f),
                30f,
                72f
            );

            CreatePalace(
                "FarCelestialPalace",
                celestialArchitecture,
                new Vector3(0f, 17f, 360f),
                3.5f
            );
        }

        private void GenerateLandmarks()
        {
            CreateGoldenHeavenGate(
                landmarks,
                new Vector3(0f, 17f, 92f),
                1.35f * landmarkScale
            );

            CreateCelestialTemple(
                landmarks,
                new Vector3(-52f, 24f, 210f),
                2.0f * landmarkScale
            );

            CreateGiantCelestialStatue(
                landmarks,
                new Vector3(75f, 22f, 315f),
                2.4f * landmarkScale
            );

            CreateGiantCelestialStatue(
                landmarks,
                new Vector3(-105f, 36f, 445f),
                3.2f * landmarkScale
            );

            CreateCelestialOrb(
                "GIANT_SUN",
                farBackground,
                new Vector3(125f, 150f, 455f),
                42f * landmarkScale,
                true
            );

            CreateCelestialOrb(
                "GIANT_MOON",
                farBackground,
                new Vector3(-150f, 110f, 510f),
                30f * landmarkScale,
                false
            );
        }

        private void GenerateWaterfalls()
        {
            CreateWaterfall(
                "Waterfall_LeftNear",
                waterfalls,
                new Vector3(-34f, 14f, 105f),
                8f,
                18f
            );

            CreateWaterfall(
                "Waterfall_RightMid",
                waterfalls,
                new Vector3(43f, 24f, 190f),
                11f,
                28f
            );

            CreateWaterfall(
                "Waterfall_LeftFar",
                waterfalls,
                new Vector3(-72f, 45f, 318f),
                16f,
                44f
            );

            CreateWaterfall(
                "Waterfall_GiantFar",
                waterfalls,
                new Vector3(100f, 58f, 430f),
                22f,
                60f
            );
        }

        private void GenerateCloudOcean()
        {
            Transform floor = NewRoot("CLOUD_OCEAN_FLOOR", cloudOcean);
            Transform near = NewRoot("NEAR_CLOUDS", cloudOcean);
            Transform mid = NewRoot("MID_CLOUDS", cloudOcean);
            Transform far = NewRoot("FAR_CLOUDS", cloudOcean);

            if (createCloudFloor)
            {
                // Broad cloud carpet below the road. It intentionally extends far beyond
                // the camera frustum so the player reads as travelling above an endless ocean.
                int rows = 10;
                int columns = 15;
                for (int rz = 0; rz < rows; ++rz)
                {
                    float z = startZ - 25f + rz * (cloudOceanDepth / (rows - 1));
                    for (int cx = 0; cx < columns; ++cx)
                    {
                        float x = -cloudOceanWidth * 0.5f +
                                  cx * (cloudOceanWidth / (columns - 1));

                        float y = -6.5f + RandomRange(-1.5f, 1.2f);
                        float radius = RandomRange(15f, 28f);

                        CreateCloudCluster(
                            floor,
                            new Vector3(x + RandomRange(-9f, 9f), y, z + RandomRange(-8f, 8f)),
                            radius
                        );
                    }
                }
            }

            // Dense foreground cloud banks on both sides of the road.
            for (int i = 0; i < 32; ++i)
            {
                float z = startZ + i * 16f;
                float side = (i & 1) == 0 ? -1f : 1f;

                CreateCloudCluster(
                    near,
                    new Vector3(
                        side * RandomRange(18f, 55f),
                        -4f + RandomRange(-1f, 2f),
                        z
                    ),
                    RandomRange(12f, 24f)
                );
            }

            // Middle cloud layers create the soft, foggy separation visible in the reference.
            for (int i = 0; i < 28; ++i)
            {
                float z = 25f + i * 19f;
                CreateCloudCluster(
                    mid,
                    new Vector3(
                        RandomRange(-105f, 105f),
                        RandomRange(0f, 8f),
                        z
                    ),
                    RandomRange(18f, 34f)
                );
            }

            // Far clouds become larger and lower-contrast, forming the horizon base.
            for (int i = 0; i < 22; ++i)
            {
                float z = 90f + i * 23f;
                CreateCloudCluster(
                    far,
                    new Vector3(
                        RandomRange(-180f, 180f),
                        RandomRange(10f, 42f),
                        z
                    ),
                    RandomRange(28f, 55f)
                );
            }
        }

        private void GenerateFarBackground()
        {
            Transform silhouettes = NewRoot("DISTANT_CELESTIAL_CITY", farBackground);
            Transform haze = NewRoot("HORIZON_HAZE", farBackground);

            // Strong central vanishing-point city, matching the reference composition.
            CreateFarCitySilhouette(
                silhouettes,
                new Vector3(0f, 18f, 170f),
                3.6f
            );

            CreateFarCitySilhouette(
                silhouettes,
                new Vector3(-88f, 30f, 275f),
                5.0f
            );

            CreateFarCitySilhouette(
                silhouettes,
                new Vector3(96f, 36f, 330f),
                5.4f
            );

            for (int i = 0; i < 16; ++i)
            {
                float z = 145f + i * 24f;
                float side = (i & 1) == 0 ? -1f : 1f;
                float x = side * RandomRange(95f, 190f);
                float y = RandomRange(24f, 76f);

                CreateFarTower(
                    silhouettes,
                    new Vector3(x, y, z),
                    RandomRange(2.8f, 7.5f)
                );
            }

            if (createHorizonFog)
            {
                // Wide soft volumes are deliberately placed behind the architecture.
                for (int i = 0; i < 18; ++i)
                {
                    float z = 115f + i * 22f;
                    CreateMistVolume(
                        haze,
                        new Vector3(
                            RandomRange(-160f, 160f),
                            RandomRange(12f, 45f),
                            z
                        ),
                        RandomRange(14f, 30f)
                    );
                }
            }
        }

        private void GenerateAtmosphere()
        {
            Transform mist = NewRoot("MIST", atmosphere);
            Transform dust = NewRoot("GOLD_DUST", atmosphere);
            Transform rays = NewRoot("LIGHT_RAYS", atmosphere);

            for (int i = 0; i < 26; ++i)
            {
                float z = 15f + i * 19f;
                float side = i % 2 == 0 ? -1f : 1f;

                CreateMistVolume(
                    mist,
                    new Vector3(
                        side * RandomRange(12f, 52f),
                        RandomRange(1f, 10f),
                        z
                    ),
                    RandomRange(4f, 11f)
                );
            }

            for (int i = 0; i < 65; ++i)
            {
                float z = RandomRange(0f, 500f);
                float x = RandomRange(-70f, 70f);
                float y = RandomRange(3f, 65f);

                CreateGlowParticle(
                    "HeavenDust_" + i,
                    dust,
                    new Vector3(x, y, z),
                    RandomRange(0.05f, 0.16f)
                );
            }

            CreateLightRay(
                "DivineRay_Left",
                rays,
                new Vector3(-48f, 30f, 175f),
                new Vector3(18f, 55f, 12f),
                28f
            );

            CreateLightRay(
                "DivineRay_Right",
                rays,
                new Vector3(62f, 40f, 300f),
                new Vector3(22f, 70f, 14f),
                -24f
            );
        }

        private void GenerateLighting()
        {
            Light key = CreateDirectionalLight(
                "HEAVEN_SUN_KEY",
                new Vector3(42f, -28f, 20f),
                1.25f,
                new Color(1f, 0.84f, 0.62f)
            );

            key.transform.SetParent(lightingRoot, true);

            Light fill = CreateDirectionalLight(
                "HEAVEN_COOL_FILL",
                new Vector3(-30f, 20f, -15f),
                0.45f,
                new Color(0.45f, 0.75f, 1f)
            );

            fill.transform.SetParent(lightingRoot, true);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.46f, 0.58f, 0.72f);
            RenderSettings.reflectionIntensity = 0.7f;
        }

        private void GenerateSkyBackdrop()
        {
            Transform sky = NewRoot("DIVINE_SKY_BACKDROP", farBackground);

            // Layered panels: bright upper sky + pale horizon. No gameplay geometry is touched.
            CreateSkyGradientPanel(
                sky,
                new Vector3(0f, 120f, 390f),
                new Vector3(420f, 180f, 0.5f)
            );

            CreateCube(
                "HorizonLightLayer",
                sky,
                new Vector3(0f, 42f, 485f),
                new Vector3(420f, 70f, 0.35f),
                Mat("HORIZON")
            );

            CreateCelestialHalo(
                "SunHalo",
                sky,
                new Vector3(105f, 155f, 485f),
                68f
            );

            CreateCelestialHalo(
                "MoonHalo",
                sky,
                new Vector3(-155f, 118f, 510f),
                46f
            );
        }

        // ============================================================
        // CLUSTERS
        // ============================================================

        private void CreateGardenCluster(
            Transform parent,
            float side,
            float z,
            float x,
            int index)
        {
            Transform cluster = NewRoot("GardenCluster_" + index, parent);

            CreateTree(
                cluster,
                new Vector3(side * x, 2.8f, z),
                2.0f + RandomRange(0f, 1.2f),
                index % 3
            );

            CreateTree(
                cluster,
                new Vector3(
                    side * (x + RandomRange(2f, 5f)),
                    2.2f,
                    z + RandomRange(2f, 6f)
                ),
                1.2f + RandomRange(0f, 0.8f),
                (index + 1) % 3
            );

            CreateFlowerPatch(
                "FlowerField_" + index,
                cluster,
                side,
                z + 2f,
                4.5f
            );

            CreateCrystal(
                "GardenCrystal_" + index,
                cluster,
                new Vector3(
                    side * (x - RandomRange(1f, 4f)),
                    1f,
                    z - 3f
                ),
                RandomRange(0.7f, 1.2f),
                RandomRange(1.2f, 2.3f)
            );

            CreateStatue(
                "GardenStatue_" + index,
                cluster,
                new Vector3(
                    side * (x + RandomRange(-3f, 4f)),
                    0f,
                    z + RandomRange(3f, 6f)
                ),
                RandomRange(0.8f, 1.2f)
            );
        }

        private void CreateCrystalCluster(
            Transform parent,
            float side,
            float z,
            float x,
            int index)
        {
            Transform cluster = NewRoot("CrystalCluster_" + index, parent);

            for (int i = 0; i < 5; ++i)
            {
                CreateCrystal(
                    "Crystal_" + index + "_" + i,
                    cluster,
                    new Vector3(
                        side * (x + RandomRange(-3f, 4f)),
                        RandomRange(0.2f, 1.5f),
                        z + RandomRange(-4f, 4f)
                    ),
                    RandomRange(0.35f, 1.1f),
                    RandomRange(1.0f, 2.8f)
                );
            }

            CreateFlowerPatch(
                "CrystalFlowers_" + index,
                cluster,
                side,
                z,
                3f
            );
        }

        private void CreateFloatingGarden(
            Transform parent,
            Vector3 position,
            float radius,
            int index)
        {
            Transform group = NewRoot("FloatingGarden_" + index, parent);

            CreateIslandMesh(
                "GardenIsland",
                group,
                position,
                radius,
                4.5f,
                12,
                2f
            );

            CreateTree(
                group,
                position + new Vector3(-radius * 0.25f, 4.6f, 0f),
                1.7f,
                index % 3
            );

            CreateFlowerPatch(
                "GardenFlowers",
                group,
                0f,
                position.z,
                radius * 0.65f
            );

            CreateSmallArch(
                "GardenArch",
                group,
                position + new Vector3(radius * 0.25f, 4.2f, 0f),
                radius * 0.55f
            );
        }

        private void CreateCityIslandCluster(
            Transform parent,
            Vector3 position,
            float radius,
            int index)
        {
            Transform group = NewRoot("CityIsland_" + index, parent);

            CreateIslandMesh(
                "CityPlatform",
                group,
                position,
                radius,
                radius * 0.45f,
                14,
                2.5f
            );

            CreateSmallCelestialBuilding(
                group,
                position + new Vector3(-radius * 0.25f, radius * 0.48f, 0f),
                0.8f
            );

            CreateTower(
                group,
                position + new Vector3(radius * 0.25f, radius * 0.48f, 1f),
                0.75f
            );

            if (index % 2 == 0)
            {
                CreateWaterfall(
                    "IslandWaterfall_" + index,
                    group,
                    position + new Vector3(radius * 0.55f, radius * 0.35f, 0f),
                    radius * 0.18f,
                    radius * 1.3f
                );
            }
        }

        private void CreateTempleCluster(
            Transform parent,
            Vector3 position,
            float scale,
            string name)
        {
            Transform group = NewRoot(name, parent);

            CreateIslandMesh(
                "TempleIsland",
                group,
                position,
                16f * scale,
                5f * scale,
                18,
                3f * scale
            );

            CreateCelestialTemple(
                group,
                position + new Vector3(0f, 5f * scale, 0f),
                scale
            );

            CreateStatue(
                "TempleStatue_L",
                group,
                position + new Vector3(-10f * scale, 5f * scale, -3f),
                1.5f * scale
            );

            CreateStatue(
                "TempleStatue_R",
                group,
                position + new Vector3(10f * scale, 5f * scale, 3f),
                1.5f * scale
            );
        }

        private void CreateCityTowerCluster(
            Transform parent,
            Vector3 position,
            float scale,
            string name)
        {
            Transform group = NewRoot(name, parent);

            CreateTower(
                group,
                position,
                2.8f * scale
            );

            CreateTower(
                group,
                position + new Vector3(16f * scale, 2f, 10f * scale),
                1.7f * scale
            );

            CreateTower(
                group,
                position + new Vector3(-14f * scale, -1f, 18f * scale),
                1.25f * scale
            );

            CreateArch(
                "CityGate",
                group,
                position + new Vector3(0f, 8f * scale, 12f * scale),
                14f * scale,
                18f * scale,
                0f
            );

            CreateCelestialBridge(
                "CityBridge",
                group,
                position + new Vector3(0f, 12f * scale, -4f * scale),
                12f * scale,
                25f * scale
            );
        }

        private void CreateIslandWithFeature(
            string name,
            Vector3 position,
            float radius,
            int featureType)
        {
            Transform group = NewRoot(name, floatingIslands);

            CreateIslandMesh(
                "IslandBody",
                group,
                position,
                radius,
                Mathf.Max(3f, radius * 0.32f),
                18,
                Mathf.Max(2f, radius * 0.14f)
            );

            Vector3 top = position +
                Vector3.up * Mathf.Max(3f, radius * 0.32f);

            switch (featureType % 6)
            {
                case 0:
                    CreateTree(group, top + new Vector3(-2f, 0f, 0f), radius * 0.12f, 0);
                    CreateFlowerPatch("IslandGarden", group, 0f, top.z, radius * 0.5f);
                    break;

                case 1:
                    CreateSmallCelestialBuilding(group, top, 0.8f + radius / 25f);
                    break;

                case 2:
                    CreateWaterfall(
                        "IslandFall",
                        group,
                        top + new Vector3(radius * 0.4f, 0f, 0f),
                        Mathf.Max(1f, radius * 0.09f),
                        radius * 1.5f
                    );
                    break;

                case 3:
                    for (int i = 0; i < 7; ++i)
                    {
                        CreateCrystal(
                            "IslandCrystal_" + i,
                            group,
                            top + new Vector3(
                                RandomRange(-radius * 0.4f, radius * 0.4f),
                                RandomRange(0f, 3f),
                                RandomRange(-radius * 0.4f, radius * 0.4f)
                            ),
                            RandomRange(0.7f, 1.8f),
                            RandomRange(2f, 5f)
                        );
                    }
                    break;

                case 4:
                    CreateCelestialTemple(group, top, 0.7f + radius / 60f);
                    break;

                default:
                    CreateStatue("IslandStatue", group, top, radius / 12f);
                    break;
            }
        }

        // ============================================================
        // LANDMARKS
        // ============================================================

        private void CreateGoldenHeavenGate(
            Transform parent,
            Vector3 position,
            float scale)
        {
            Transform group = NewRoot("GOLDEN_HEAVEN_GATE", parent);

            float width = 24f * scale;
            float height = 35f * scale;
            float depth = 5f * scale;

            CreateCube(
                "GateLeftPillar",
                group,
                position + new Vector3(-width * 0.5f, height * 0.5f, 0f),
                new Vector3(4f, height, depth),
                Mat("MARBLE")
            );

            CreateCube(
                "GateRightPillar",
                group,
                position + new Vector3(width * 0.5f, height * 0.5f, 0f),
                new Vector3(4f, height, depth),
                Mat("MARBLE")
            );

            CreateArch(
                "GateArch",
                group,
                position + new Vector3(0f, height * 0.54f, 0f),
                width,
                height * 0.62f,
                0f
            );

            CreateCube(
                "GateGoldCrown",
                group,
                position + new Vector3(0f, height * 0.93f, 0f),
                new Vector3(width * 0.72f, 2.2f * scale, depth * 1.2f),
                Mat("GOLD")
            );

            CreateCube(
                "GateGlowCenter",
                group,
                position + new Vector3(0f, height * 0.45f, -0.25f),
                new Vector3(width * 0.65f, height * 0.7f, 0.25f),
                Mat("GLOW")
            );

            for (int i = 0; i < 5; ++i)
            {
                CreateLightRay(
                    "GateRay_" + i,
                    group,
                    position + new Vector3(
                        (i - 2) * width * 0.16f,
                        height * 0.56f,
                        -1f
                    ),
                    new Vector3(0.7f * scale, height * 0.7f, 0.7f * scale),
                    0f
                );
            }
        }

        private void CreateCelestialTemple(
            Transform parent,
            Vector3 position,
            float scale)
        {
            Transform group = NewRoot("CELESTIAL_TEMPLE", parent);

            CreateGiantStaircase(
                "TempleStairs",
                group,
                position + new Vector3(0f, 0f, -11f * scale),
                13f * scale,
                20f * scale
            );

            CreateCube(
                "TempleBase",
                group,
                position + new Vector3(0f, 5f * scale, 0f),
                new Vector3(25f * scale, 5f * scale, 18f * scale),
                Mat("MARBLE")
            );

            for (int i = -2; i <= 2; ++i)
            {
                CreatePillar(
                    "TemplePillar_" + i,
                    group,
                    position + new Vector3(
                        i * 5f * scale,
                        13f * scale,
                        -5f * scale
                    ),
                    1.1f * scale,
                    16f * scale
                );
            }

            CreateArch(
                "TempleFrontArch",
                group,
                position + new Vector3(0f, 14f * scale, -7f * scale),
                16f * scale,
                15f * scale,
                0f
            );

            CreatePalaceRoof(
                "TempleRoof",
                group,
                position + new Vector3(0f, 18f * scale, 0f),
                22f * scale,
                15f * scale
            );

            CreateGlowOrb(
                "TempleCore",
                group,
                position + new Vector3(0f, 12f * scale, -7.8f * scale),
                2.5f * scale
            );
        }

        private void CreateGiantCelestialStatue(
            Transform parent,
            Vector3 position,
            float scale)
        {
            Transform group = NewRoot("GIANT_CELESTIAL_STATUE", parent);

            float h = 28f * scale;

            CreatePillar(
                "StatueBody",
                group,
                position + Vector3.up * (h * 0.38f),
                4.5f * scale,
                h * 0.72f
            );

            CreateSphere(
                "StatueHead",
                group,
                position + Vector3.up * (h * 0.82f),
                new Vector3(5f, 5f, 5f) * scale,
                Mat("MARBLE")
            );

            CreateCube(
                "StatueArmLeft",
                group,
                position + new Vector3(-8f, h * 0.58f, 0f),
                new Vector3(12f, 2.4f, 2.6f) * scale,
                Mat("MARBLE")
            );

            CreateCube(
                "StatueArmRight",
                group,
                position + new Vector3(8f, h * 0.58f, 0f),
                new Vector3(12f, 2.4f, 2.6f) * scale,
                Mat("MARBLE")
            );

            CreateHalo(
                "StatueHalo",
                group,
                position + Vector3.up * (h * 0.82f),
                8f * scale
            );

            CreateGlowOrb(
                "StatueHeart",
                group,
                position + new Vector3(0f, h * 0.58f, -4.2f * scale),
                1.6f * scale
            );
        }

        private void CreateCelestialOrb(
            string name,
            Transform parent,
            Vector3 position,
            float radius,
            bool sun)
        {
            Transform group = NewRoot(name, parent);

            CreateSphere(
                "Orb",
                group,
                position,
                Vector3.one * radius,
                sun ? Mat("GOLD") : Mat("GLOW")
            );

            CreateHalo(
                "OrbHalo_A",
                group,
                position,
                radius * 1.18f
            );

            CreateHalo(
                "OrbHalo_B",
                group,
                position,
                radius * 1.42f
            );

            for (int i = 0; i < 8; ++i)
            {
                float angle = i * 45f;
                float r = radius * 1.55f;

                CreateLightRay(
                    "OrbRay_" + i,
                    group,
                    position + new Vector3(
                        Mathf.Cos(angle * Mathf.Deg2Rad) * r,
                        Mathf.Sin(angle * Mathf.Deg2Rad) * r,
                        0f
                    ),
                    new Vector3(
                        radius * 0.06f,
                        radius * 0.55f,
                        radius * 0.06f
                    ),
                    angle
                );
            }
        }

        // ============================================================
        // PROCEDURAL PRIMITIVE BUILDING BLOCKS
        // ============================================================

        private GameObject CreateCube(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale * worldScale;
            ApplyVisualOnly(go, material);
            return Register(go);
        }

        private GameObject CreateSphere(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale * worldScale;
            ApplyVisualOnly(go, material);
            return Register(go);
        }

        private void CreateTree(
            Transform parent,
            Vector3 position,
            float scale,
            int style)
        {
            GameObject tree;

            if (treePrefab != null)
            {
                tree = InstantiatePrefab(
                    treePrefab,
                    "TreePrefab",
                    parent,
                    position,
                    Quaternion.identity,
                    Vector3.one * scale
                );
            }
            else
            {
                Transform group = NewRoot("FantasyTree", parent);
                CreatePillar(
                    "Trunk",
                    group,
                    position + Vector3.up * (2.2f * scale),
                    0.75f * scale,
                    4.4f * scale
                );

                CreateSphere(
                    "Crown_A",
                    group,
                    position + Vector3.up * (5.2f * scale),
                    new Vector3(4.4f, 5.2f, 4.4f) * scale,
                    style == 1 ? Mat("GOLD") : Mat("VEGETATION")
                );

                CreateSphere(
                    "Crown_B",
                    group,
                    position + new Vector3(1.5f, 6.8f, 0.5f) * scale,
                    new Vector3(3.2f, 3.8f, 3.2f) * scale,
                    Mat("GLOW")
                );
            }
        }

        private void CreateFlowerPatch(
            string name,
            Transform parent,
            float side,
            float z,
            float radius)
        {
            Transform group = NewRoot(name, parent);

            for (int i = 0; i < 11; ++i)
            {
                float angle = i * 32.7f;
                float r = radius * Mathf.Sqrt((i + 1f) / 11f);

                Vector3 p = new Vector3(
                    side * (roadHalfWidth + 2f + r * 0.7f),
                    0.35f + RandomRange(0f, 0.45f),
                    z + Mathf.Sin(angle * Mathf.Deg2Rad) * r
                );

                CreateGlowOrb(
                    "Flower_" + i,
                    group,
                    p,
                    RandomRange(0.12f, 0.28f)
                );
            }
        }

        private void CreateCrystal(
            string name,
            Transform parent,
            Vector3 position,
            float radius,
            float height)
        {
            GameObject go = CreateCrystalMesh(
                name,
                parent,
                position,
                radius,
                height
            );

            go.transform.Rotate(
                RandomRange(-8f, 8f),
                RandomRange(0f, 360f),
                RandomRange(-8f, 8f)
            );
        }

        private GameObject CreateCrystalMesh(
            string name,
            Transform parent,
            Vector3 position,
            float radius,
            float height)
        {
            Mesh mesh = new Mesh();
            mesh.name = name + "_Mesh";

            const int sides = 6;
            Vector3[] vertices = new Vector3[sides * 2 + 2];
            // 4 triangles / side × 3 indices = 12 indices per side.
            // Bản cũ chỉ cấp sides * 6 -> IndexOutOfRangeException
            // ngay crystal đầu tiên, khiến Generate() dừng sau road edge.
            int[] triangles = new int[sides * 12];

            vertices[0] = Vector3.zero;
            vertices[1] = Vector3.up * height;

            for (int i = 0; i < sides; ++i)
            {
                float a = i * Mathf.PI * 2f / sides;
                float r = radius * (0.85f + (float)rng.NextDouble() * 0.25f);

                vertices[2 + i] = new Vector3(
                    Mathf.Cos(a) * r,
                    0f,
                    Mathf.Sin(a) * r
                );

                vertices[2 + sides + i] = new Vector3(
                    Mathf.Cos(a) * r * 0.45f,
                    height * 0.62f,
                    Mathf.Sin(a) * r * 0.45f
                );
            }

            int t = 0;
            for (int i = 0; i < sides; ++i)
            {
                int n = (i + 1) % sides;
                int b0 = 2 + i;
                int b1 = 2 + n;
                int m0 = 2 + sides + i;
                int m1 = 2 + sides + n;

                triangles[t++] = 0;
                triangles[t++] = b1;
                triangles[t++] = b0;

                triangles[t++] = 1;
                triangles[t++] = m0;
                triangles[t++] = m1;

                triangles[t++] = b0;
                triangles[t++] = b1;
                triangles[t++] = m1;

                triangles[t++] = b0;
                triangles[t++] = m1;
                triangles[t++] = m0;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;

            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = Mat("CRYSTAL");

            return Register(go);
        }

        private void CreatePillar(
            string name,
            Transform parent,
            Vector3 position,
            float radius,
            float height)
        {
            GameObject go = CreateCylinderMesh(
                name,
                parent,
                position,
                radius,
                height,
                12
            );

            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.sharedMaterial = Mat("MARBLE");
        }

        private GameObject CreateCylinderMesh(
            string name,
            Transform parent,
            Vector3 position,
            float radius,
            float height,
            int sides)
        {
            Mesh mesh = new Mesh();
            mesh.name = name + "_Mesh";

            Vector3[] vertices = new Vector3[sides * 2];
            int[] triangles = new int[sides * 6];

            for (int i = 0; i < sides; ++i)
            {
                float a = i * Mathf.PI * 2f / sides;
                float x = Mathf.Cos(a) * radius;
                float z = Mathf.Sin(a) * radius;

                vertices[i] = new Vector3(x, 0f, z);
                vertices[sides + i] = new Vector3(x, height, z);
            }

            int t = 0;
            for (int i = 0; i < sides; ++i)
            {
                int n = (i + 1) % sides;

                triangles[t++] = i;
                triangles[t++] = n;
                triangles[t++] = sides + n;

                triangles[t++] = i;
                triangles[t++] = sides + n;
                triangles[t++] = sides + i;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;

            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = Mat("MARBLE");

            return Register(go);
        }

        private void CreateArch(
            string name,
            Transform parent,
            Vector3 position,
            float width,
            float height,
            float yaw)
        {
            // Arch coordinates are local to the Heaven composition root.
            // This keeps the complete biome anchored to Player Z.
            Transform group = NewRoot(name, parent);
            group.localPosition = Vector3.zero;
            group.localRotation = Quaternion.identity;

            float pillarHeight = height * 0.62f;
            float pillarWidth = Mathf.Max(1f, width * 0.12f);

            CreateCube(
                "ArchPillar_L",
                group,
                position + new Vector3(-width * 0.5f, pillarHeight * 0.5f, 0f),
                new Vector3(pillarWidth, pillarHeight, pillarWidth),
                Mat("MARBLE")
            );

            CreateCube(
                "ArchPillar_R",
                group,
                position + new Vector3(width * 0.5f, pillarHeight * 0.5f, 0f),
                new Vector3(pillarWidth, pillarHeight, pillarWidth),
                Mat("MARBLE")
            );

            const int segments = 14;
            float outerRadius = width * 0.5f;
            float centerY = pillarHeight;

            for (int i = 0; i < segments; ++i)
            {
                float a0 = Mathf.PI - Mathf.PI * i / segments;
                float a1 = Mathf.PI - Mathf.PI * (i + 1) / segments;

                Vector3 p0 = position + new Vector3(
                    Mathf.Cos(a0) * outerRadius,
                    centerY + Mathf.Sin(a0) * outerRadius * 0.62f,
                    0f
                );

                Vector3 p1 = position + new Vector3(
                    Mathf.Cos(a1) * outerRadius,
                    centerY + Mathf.Sin(a1) * outerRadius * 0.62f,
                    0f
                );

                Vector3 mid = (p0 + p1) * 0.5f;
                float length = Vector3.Distance(p0, p1);

                GameObject block = CreateCube(
                    "ArchBlock_" + i,
                    group,
                    mid,
                    new Vector3(
                        Mathf.Max(0.6f, width * 0.07f),
                        0.9f,
                        length
                    ),
                    Mat("GOLD")
                );

                Vector3 direction = p1 - p0;
                if (direction.sqrMagnitude > 0.0001f)
                    block.transform.rotation =
                        Quaternion.LookRotation(direction, Vector3.up);
            }

            CreateCube(
                "ArchInnerGlow",
                group,
                position + new Vector3(0f, centerY + height * 0.08f, -0.1f),
                new Vector3(
                    width * 0.62f,
                    Mathf.Max(0.5f, height * 0.12f),
                    0.18f
                ),
                Mat("GLOW")
            );
        }

        private void CreateLightPillar(
            string name,
            Transform parent,
            Vector3 position,
            float height)
        {
            const float radius = 0.18f;

            CreatePillar(
                name,
                parent,
                position,
                radius,
                Mathf.Max(0.5f, height)
            );

            CreateSphere(
                name + "_TopGlow",
                parent,
                position + Vector3.up * Mathf.Max(0.5f, height),
                Vector3.one * 0.42f,
                Mat("GLOW")
            );
        }

        private void CreateCelestialHalo(
            string name,
            Transform parent,
            Vector3 position,
            float radius)
        {
            CreateHalo(
                name,
                parent,
                position,
                Mathf.Max(0.5f, radius)
            );

            CreateSphere(
                name + "_Core",
                parent,
                position,
                Vector3.one * Mathf.Max(0.5f, radius * 0.12f),
                Mat("GLOW")
            );
        }

        private void CreateSmallArch(
            string name,
            Transform parent,
            Vector3 position,
            float size)
        {
            CreateArch(name, parent, position, size, size * 1.3f, 0f);
        }

        private void CreateIslandMesh(
            string name,
            Transform parent,
            Vector3 position,
            float radius,
            float topThickness,
            int sides,
            float bottomDepth)
        {
            Mesh mesh = new Mesh();
            mesh.name = name + "_IrregularMesh";

            int ringCount = 3;
            int vertexCount = sides * ringCount;
            Vector3[] vertices = new Vector3[vertexCount];
            int[] triangles = new int[(ringCount - 1) * sides * 6];

            for (int ring = 0; ring < ringCount; ++ring)
            {
                float ringT = ring / (float)(ringCount - 1);

                for (int i = 0; i < sides; ++i)
                {
                    float a = i * Mathf.PI * 2f / sides;
                    float irregular =
                        0.78f +
                        (float)rng.NextDouble() * 0.42f;

                    float r = radius * irregular * (1f - ringT * 0.34f);

                    float y;
                    if (ring == 0)
                        y = topThickness;
                    else
                        y = topThickness - bottomDepth * ringT;

                    vertices[ring * sides + i] =
                        new Vector3(
                            Mathf.Cos(a) * r,
                            y,
                            Mathf.Sin(a) * r
                        );
                }
            }

            int t = 0;

            for (int ring = 0; ring < ringCount - 1; ++ring)
            {
                int baseA = ring * sides;
                int baseB = (ring + 1) * sides;

                for (int i = 0; i < sides; ++i)
                {
                    int n = (i + 1) % sides;

                    triangles[t++] = baseA + i;
                    triangles[t++] = baseA + n;
                    triangles[t++] = baseB + n;

                    triangles[t++] = baseA + i;
                    triangles[t++] = baseB + n;
                    triangles[t++] = baseB + i;
                }
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;

            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = Mat("DARK_ROCK");

            Register(go);
        }

        private void CreateTower(
            Transform parent,
            Vector3 position,
            float scale)
        {
            Transform group = NewRoot("CelestialTower", parent);

            CreatePillar(
                "TowerBody",
                group,
                position,
                3f * scale,
                18f * scale
            );

            CreateArch(
                "TowerCrown",
                group,
                position + Vector3.up * (17f * scale),
                8f * scale,
                8f * scale,
                0f
            );

            CreateGlowOrb(
                "TowerLight",
                group,
                position + Vector3.up * (18.5f * scale),
                1.1f * scale
            );
        }

        private void CreateSmallCelestialBuilding(
            Transform parent,
            Vector3 position,
            float scale)
        {
            Transform group = NewRoot("CelestialBuilding", parent);

            CreateCube(
                "BuildingBody",
                group,
                position + Vector3.up * (4f * scale),
                new Vector3(10f, 8f, 8f) * scale,
                Mat("MARBLE")
            );

            CreatePalaceRoof(
                "BuildingRoof",
                group,
                position + Vector3.up * (8.5f * scale),
                11f * scale,
                8f * scale
            );

            CreateGlowOrb(
                "BuildingGlow",
                group,
                position + new Vector3(0f, 4.5f * scale, -4.2f * scale),
                0.8f * scale
            );
        }

        private void CreatePalace(
            string name,
            Transform parent,
            Vector3 position,
            float scale)
        {
            Transform group = NewRoot(name, parent);

            CreateCube(
                "PalaceBase",
                group,
                position + Vector3.up * (8f * scale),
                new Vector3(40f, 16f, 28f) * scale,
                Mat("MARBLE")
            );

            for (int i = -3; i <= 3; ++i)
            {
                CreatePillar(
                    "PalacePillar_" + i,
                    group,
                    position + new Vector3(
                        i * 5f * scale,
                        16f * scale,
                        -11f * scale
                    ),
                    1.2f * scale,
                    18f * scale
                );
            }

            CreatePalaceRoof(
                "PalaceRoof",
                group,
                position + Vector3.up * (18f * scale),
                34f * scale,
                25f * scale
            );
        }

        private void CreatePalaceRoof(
            string name,
            Transform parent,
            Vector3 position,
            float width,
            float depth)
        {
            CreateCube(
                name + "_Base",
                parent,
                position,
                new Vector3(width, 2f, depth),
                Mat("GOLD")
            );

            CreateCube(
                name + "_Crown",
                parent,
                position + Vector3.up * 2.5f,
                new Vector3(width * 0.68f, 2f, depth * 0.62f),
                Mat("IVORY")
            );

            CreateGlowOrb(
                name + "_Glow",
                parent,
                position + Vector3.up * 4.2f,
                Mathf.Max(0.5f, width * 0.035f)
            );
        }

        private void CreateGiantStaircase(
            string name,
            Transform parent,
            Vector3 position,
            float width,
            float length)
        {
            Transform group = NewRoot(name, parent);

            const int steps = 14;

            for (int i = 0; i < steps; ++i)
            {
                float t = i / (float)(steps - 1);

                CreateCube(
                    "Step_" + i,
                    group,
                    position + new Vector3(
                        0f,
                        t * length * 0.28f,
                        i * 2.2f
                    ),
                    new Vector3(
                        width * (1f - t * 0.25f),
                        0.8f,
                        2.6f
                    ),
                    Mat("MARBLE")
                );
            }

            CreateCube(
                "GoldRail_L",
                group,
                position + new Vector3(-width * 0.48f, length * 0.14f, length * 0.45f),
                new Vector3(0.35f, 0.35f, length),
                Mat("GOLD")
            );

            CreateCube(
                "GoldRail_R",
                group,
                position + new Vector3(width * 0.48f, length * 0.14f, length * 0.45f),
                new Vector3(0.35f, 0.35f, length),
                Mat("GOLD")
            );
        }

        private void CreateCelestialBridge(
            string name,
            Transform parent,
            Vector3 position,
            float width,
            float length)
        {
            Transform group = NewRoot(name, parent);

            CreateCube(
                "BridgeDeck",
                group,
                position,
                new Vector3(width, 1.2f, length),
                Mat("MARBLE")
            );

            CreateCube(
                "BridgeGoldEdge_L",
                group,
                position + new Vector3(-width * 0.48f, 0.8f, 0f),
                new Vector3(0.35f, 0.5f, length),
                Mat("GOLD")
            );

            CreateCube(
                "BridgeGoldEdge_R",
                group,
                position + new Vector3(width * 0.48f, 0.8f, 0f),
                new Vector3(0.35f, 0.5f, length),
                Mat("GOLD")
            );

            for (int i = 0; i < 5; ++i)
            {
                float z = position.z - length * 0.5f + i * length / 4f;

                CreatePillar(
                    "BridgePillar_" + i,
                    group,
                    new Vector3(position.x, position.y - 8f, z),
                    1.2f,
                    16f
                );
            }
        }

        private void CreateStatue(
            string name,
            Transform parent,
            Vector3 position,
            float scale)
        {
            if (statuePrefab != null)
            {
                InstantiatePrefab(
                    statuePrefab,
                    name,
                    parent,
                    position,
                    Quaternion.identity,
                    Vector3.one * scale
                );
                return;
            }

            Transform group = NewRoot(name, parent);

            CreatePillar(
                "Body",
                group,
                position,
                0.9f * scale,
                3.5f * scale
            );

            CreateSphere(
                "Head",
                group,
                position + Vector3.up * (4.2f * scale),
                Vector3.one * (1.25f * scale),
                Mat("MARBLE")
            );

            CreateHalo(
                "Halo",
                group,
                position + Vector3.up * (4.8f * scale),
                1.8f * scale
            );
        }

        private void CreateWaterfall(
            string name,
            Transform parent,
            Vector3 position,
            float width,
            float height)
        {
            Transform group = NewRoot(name, parent);

            CreateCube(
                "WaterSheet",
                group,
                position + Vector3.down * (height * 0.5f),
                new Vector3(width, height, 0.35f),
                Mat("WATER")
            );

            for (int i = 0; i < 6; ++i)
            {
                CreateGlowOrb(
                    "WaterMist_" + i,
                    group,
                    position + new Vector3(
                        RandomRange(-width, width),
                        -height + RandomRange(-1f, 2f),
                        RandomRange(-1f, 1f)
                    ),
                    RandomRange(0.25f, 0.7f)
                );
            }

            for (int i = 0; i < 10; ++i)
            {
                CreateGlowParticle(
                    "WaterParticle_" + i,
                    group,
                    position + new Vector3(
                        RandomRange(-width, width),
                        -RandomRange(0f, height),
                        RandomRange(-0.6f, 0.6f)
                    ),
                    RandomRange(0.06f, 0.16f)
                );
            }
        }

        private void CreateCloudCluster(
            Transform parent,
            Vector3 position,
            float radius)
        {
            Transform group = NewRoot("CloudCluster", parent);

            for (int i = 0; i < 7; ++i)
            {
                Vector3 p = position + new Vector3(
                    RandomRange(-radius, radius),
                    RandomRange(-radius * 0.25f, radius * 0.25f),
                    RandomRange(-radius * 0.7f, radius * 0.7f)
                );

                CreateSphere(
                    "CloudPuff_" + i,
                    group,
                    p,
                    new Vector3(
                        RandomRange(0.8f, 1.6f),
                        RandomRange(0.45f, 0.9f),
                        RandomRange(0.8f, 1.6f)
                    ) * radius,
                    Mat("CLOUD")
                );
            }
        }

        private void CreateMistVolume(
            Transform parent,
            Vector3 position,
            float radius)
        {
            CreateSphere(
                "MistVolume",
                parent,
                position,
                new Vector3(radius * 1.8f, radius * 0.35f, radius * 1.2f),
                Mat("CLOUD")
            );
        }

        private void CreateGlowOrb(
            string name,
            Transform parent,
            Vector3 position,
            float radius)
        {
            CreateSphere(
                name,
                parent,
                position,
                Vector3.one * radius,
                Mat("GLOW")
            );
        }

        private void CreateGlowParticle(
            string name,
            Transform parent,
            Vector3 position,
            float radius)
        {
            CreateSphere(
                name,
                parent,
                position,
                Vector3.one * radius,
                Mat("GLOW")
            );
        }

        private void CreateHalo(
            string name,
            Transform parent,
            Vector3 position,
            float radius)
        {
            const int segments = 32;
            float tube = Mathf.Max(0.15f, radius * 0.045f);

            for (int i = 0; i < segments; ++i)
            {
                float a = i * 360f / segments;
                float rad = a * Mathf.Deg2Rad;

                CreateCube(
                    name + "_" + i,
                    parent,
                    position + new Vector3(
                        Mathf.Cos(rad) * radius,
                        Mathf.Sin(rad) * radius,
                        0f
                    ),
                    new Vector3(
                        tube,
                        tube,
                        tube * 2f
                    ),
                    Mat("GOLD")
                );
            }
        }

        private void CreateLightRay(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            float yaw)
        {
            GameObject go = CreateCube(
                name,
                parent,
                position,
                scale,
                Mat("GLOW")
            );

            go.transform.rotation =
                Quaternion.Euler(0f, yaw, 0f);
        }

        private void CreateSkyGradientPanel(
            Transform parent,
            Vector3 position,
            Vector3 scale)
        {
            GameObject go = CreateCube(
                "SkyGradientBackdrop",
                parent,
                position,
                scale,
                Mat("GLOW")
            );

            go.transform.rotation =
                Quaternion.Euler(0f, 180f, 0f);
        }

        private void CreateFarCitySilhouette(
            Transform parent,
            Vector3 position,
            float scale)
        {
            Transform group = NewRoot("FarCity", parent);

            float[] widths = { 12f, 8f, 15f, 10f, 18f, 7f, 13f };

            for (int i = 0; i < widths.Length; ++i)
            {
                float h = (10f + (i % 4) * 7f) * scale;

                CreateCube(
                    "FarBuilding_" + i,
                    group,
                    position + new Vector3(
                        (i - 3) * 10f * scale,
                        h * 0.5f,
                        (i % 2) * 5f * scale
                    ),
                    new Vector3(
                        widths[i] * scale,
                        h,
                        8f * scale
                    ),
                    Mat("MARBLE")
                );
            }
        }

        private void CreateFarTower(
            Transform parent,
            Vector3 position,
            float scale)
        {
            CreateTower(parent, position, scale);
        }

        private Light CreateDirectionalLight(
            string name,
            Vector3 euler,
            float intensity,
            Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.rotation = Quaternion.Euler(euler);
            go.transform.SetParent(lightingRoot, true);

            Light light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.color = color;
            light.shadows = LightShadows.Soft;

            Register(go);
            return light;
        }

        private GameObject InstantiatePrefab(
            GameObject prefab,
            string name,
            Transform parent,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale)
        {
            GameObject go =
                (GameObject)PrefabUtility.InstantiatePrefab(prefab);

            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = rotation;
            go.transform.localScale = Vector3.Scale(
                go.transform.localScale,
                scale
            );

            StripGameplayComponents(go);
            return Register(go);
        }

        private void StripGameplayComponents(GameObject rootObject)
        {
            Collider[] colliders =
                rootObject.GetComponentsInChildren<Collider>(true);

            for (int i = 0; i < colliders.Length; ++i)
                DestroyImmediate(colliders[i]);

            Rigidbody[] bodies =
                rootObject.GetComponentsInChildren<Rigidbody>(true);

            for (int i = 0; i < bodies.Length; ++i)
                DestroyImmediate(bodies[i]);

            MonoBehaviour[] behaviours =
                rootObject.GetComponentsInChildren<MonoBehaviour>(true);

            for (int i = 0; i < behaviours.Length; ++i)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour != null && behaviour != this)
                    DestroyImmediate(behaviour);
            }
        }

        private void ApplyVisualOnly(
            GameObject go,
            Material material)
        {
            Collider[] colliders =
                go.GetComponentsInChildren<Collider>(true);

            for (int i = 0; i < colliders.Length; ++i)
                DestroyImmediate(colliders[i]);

            MeshRenderer renderer =
                go.GetComponent<MeshRenderer>();

            if (renderer != null && material != null)
                renderer.sharedMaterial = material;
        }

        private GameObject Register(GameObject go)
        {
            if (go == null)
                return null;

            generatedObjects.Add(go);
            return go;
        }

        private Transform NewRootRegistered(
            string name,
            Transform parent)
        {
            return NewRoot(name, parent);
        }

        private void MarkHierarchyStatic()
        {
            if (!markStatic || root == null)
                return;

            Transform[] all =
                root.GetComponentsInChildren<Transform>(true);

            for (int i = 0; i < all.Length; ++i)
            {
                GameObject go = all[i].gameObject;
                if (go != null)
                    GameObjectUtility.SetStaticEditorFlags(
                        go,
                        StaticEditorFlags.BatchingStatic |
                        StaticEditorFlags.OccluderStatic |
                        StaticEditorFlags.OccludeeStatic
                    );
            }
        }

        private void SaveScene()
        {
            EditorUtility.SetDirty(gameObject);
            if (root != null)
                EditorUtility.SetDirty(root.gameObject);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                gameObject.scene
            );
        }

        // Every generated position is expressed relative to HEAVEN_BIOME_GENERATED.
        // The root alone carries the Player-Z anchor.
        private Vector3 CompositionLocal(Vector3 position)
        {
            return position;
        }

        private float RandomRange(float min, float max)
        {
            return min +
                (float)rng.NextDouble() *
                (max - min);
        }

        private void OnValidate()
        {
            if (nearEnd < 20f)
                nearEnd = 20f;

            if (midEnd < nearEnd + 20f)
                midEnd = nearEnd + 20f;

            if (farEnd < midEnd + 100f)
                farEnd = midEnd + 100f;

            if (endZ < farEnd)
                endZ = farEnd;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin =
                new Vector3(
                    transform.position.x,
                    transform.position.y,
                    compositionOriginZ
                );

            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.22f);
            DrawDepthBox(origin, 40f, 0f, nearEnd);

            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.16f);
            DrawDepthBox(origin, 70f, nearEnd, midEnd);

            Gizmos.color = new Color(0.7f, 0.4f, 1f, 0.1f);
            DrawDepthBox(origin, 110f, midEnd, farEnd);
        }

        private void DrawDepthBox(
            Vector3 origin,
            float halfWidth,
            float z0,
            float z1)
        {
            Vector3 center =
                origin +
                new Vector3(
                    0f,
                    10f,
                    (z0 + z1) * 0.5f
                );

            Vector3 size =
                new Vector3(
                    halfWidth * 2f,
                    20f,
                    z1 - z0
                );

            Gizmos.DrawWireCube(center, size);
        }

        [CustomEditor(typeof(HeavenBiomeSetup))]
        private sealed class HeavenBiomeSetupEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                HeavenBiomeSetup setup =
                    (HeavenBiomeSetup)target;

                serializedObject.Update();

                DrawProperties(
                    "seed",
                    "quality",
                    "clearBeforeGenerate",
                    "markStatic",
                    "startZ",
                    "endZ",
                    "roadHalfWidth",
                    "nearEnd",
                    "midEnd",
                    "farEnd",
                    "worldScale",
                    "landmarkScale",
                    "useProjectPrefabs",
                    "createMaterials",
                    "useEmission",
                    "createLighting",
                    "createSkyBackdrop",
                    "compositionOriginZ"
                );

                serializedObject.ApplyModifiedProperties();

                EditorGUILayout.Space(10f);

                EditorGUILayout.HelpBox(
                    "Generated Heaven local-space anchor Z = " +
                    setup.compositionOriginZ.ToString("F1") +
                    (setup.anchorToPlayer ? " (Player Anchor)" : " (Manual)") +
                    ".",
                    MessageType.None
                );

                if (GUILayout.Button(
                    "CREATE HEAVEN BIOME — AUTO SETUP EVERYTHING",
                    GUILayout.Height(50f)))
                {
                    setup.useProjectPrefabs = true;
                    setup.createMaterials = true;
                    setup.createLighting = true;
                    setup.createSkyBackdrop = true;
                    setup.anchorToPlayer = true;
                    setup.Generate();
                }

                EditorGUILayout.HelpBox(
                    "Không cần kéo/thả Player, Prefab, Material, Light, Cloud hoặc Object nào. " +
                    "Bấm nút trên là hệ thống tự tạo toàn bộ.",
                    MessageType.Info
                );

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button(
                    "CLEAR",
                    GUILayout.Height(30f)))
                {
                    setup.ClearGenerated();
                }

                if (GUILayout.Button(
                    "CREATE HIERARCHY",
                    GUILayout.Height(30f)))
                {
                    setup.CreateHierarchy();
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.HelpBox(
                    "Visual-only environment. " +
                    "Không sửa RoadManager / PlayerController / Traffic / Ramp / Lane / Gameplay Z.",
                    MessageType.Info
                );
            }

            private void DrawProperties(
                params string[] propertyNames)
            {
                for (int i = 0; i < propertyNames.Length; ++i)
                {
                    SerializedProperty property =
                        serializedObject.FindProperty(
                            propertyNames[i]
                        );

                    if (property != null)
                        EditorGUILayout.PropertyField(
                            property,
                            true
                        );
                }
            }
        }
    }
}
#endif
