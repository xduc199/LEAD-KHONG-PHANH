    using System;
    using System.Collections.Generic;
    using UnityEngine;

    [DisallowMultipleComponent]
    public class RoadManager : MonoBehaviour
    {
        //=========================================================
        // BIOME ELEMENT
        //=========================================================

        /*
        * Mỗi Element đại diện cho 1 Road/Biome có thể được
        * RoadManager chọn bằng weighted random.
        *
        * Mỗi lần SpawnRoad() sẽ random độc lập theo Weight.
        * Road vừa spawn vẫn có thể được chọn lại.
        *
        * Ví dụ:
        *
        * Normal -> Normal -> Normal
        * Normal -> Alley  -> Normal
        * Alley  -> Alley  -> Normal
        *
        * Không phụ thuộc BiomeDefinition / BiomeType bên ngoài.
        */

        [Serializable]
    private class BiomeElement
    {
        [Tooltip("Tên biome để dễ nhận biết trong Inspector.")]
        public string biomeName = "Normal";

        [Tooltip("Prefab Road của biome này.")]
        public GameObject roadPrefab;

        [Tooltip(
            "Độ cao Y của Road khi được RoadManager spawn. " +
            "Dùng để căn tất cả Road về cùng một cao độ."
        )]
        public float roadY = 0f;

        [Tooltip(
            "Trọng số random tương đối. " +
            "Mỗi lần spawn random độc lập."
        )]
        [Min(0f)]
        public float weight = 1f;

        [Tooltip("Cho phép biome này xuất hiện.")]
        public bool enabled = true;
    }


        //=========================================================
        // ROAD SETTINGS
        //=========================================================

        [Header("Road Settings")]

        [Tooltip(
            "Road Prefab cũ dùng làm fallback nếu chưa cấu hình Biome Elements."
        )]
        [SerializeField] private GameObject roadPrefab;

        [Tooltip(
            "Danh sách Biome. Normal cũng được thêm vào đây như một Element."
        )]
        [SerializeField] private BiomeElement[] biomeElements;

        [Tooltip(
            "Bật weighted random Road/Biome. " +
            "Mỗi lần SpawnRoad() random độc lập theo Weight. " +
            "Road vừa spawn vẫn có thể được chọn lại. " +
            "Nếu tắt, hệ thống sẽ luôn dùng roadPrefab."
        )]
        [SerializeField] private bool randomBiome = true;

        [Header("Heaven Road")]
        [Tooltip("Prefab Road dùng riêng khi Player đang ở Heaven.")]
        [SerializeField] private GameObject heavenRoadPrefab;

        [Tooltip("Y của Heaven Road. Không thay đổi X/Z gameplay.")]
        [SerializeField] private float heavenRoadY = 0f;

        [Header("Heaven Transition")]
        [Tooltip(
            "Đã vô hiệu hóa fade; Heaven xuất hiện tức thời."
        )]
        [Min(0f)]
        [SerializeField] private float heavenFadeDistance = 0f;

        [Tooltip(
            "Khoảng cách từ Player tới START của Heaven Road khi EnterHeaven(). " +
            "Không dùng chunk boundary của Ground."
        )]
        [Min(0f)]
        [SerializeField] private float heavenEntryDistance = 0f;

        [Tooltip(
            "Khoảng Road Heaven luôn được giữ phía trước Player."
        )]
        [Min(1f)]
        [SerializeField] private float heavenForwardCoverage = 360f;

        [Header("Heaven Visual Environment")]
        [Tooltip(
            "Khoảng cách Z từ Player tới lớp visual Heaven gần nhất khi EnterHeaven. " +
            "Chỉ di chuyển root visual, KHÔNG thay đổi gameplay road/chunk."
        )]
        [Min(0f)]
        [SerializeField] private float heavenEnvironmentNearDistance = 12f;

        [Tooltip(
            "Tên root được HeavenBiomeSetup tạo ra. " +
            "Root này chỉ chứa visual Heaven."
        )]
        [SerializeField] private string heavenEnvironmentRootName =
            "HEAVEN_BIOME_GENERATED";

        private Transform heavenEnvironmentRoot;

        private bool heavenMode = false;
        private bool heavenTransitioning = false;
        private bool heavenTransitionEntering = false;
        private float heavenTransitionBoundaryZ = 0f;
        private float heavenVisualBlend01 = 0f;

        // Heaven có pipeline Z riêng.
        // Không dùng spawnZ của Ground.
        private float heavenSpawnZ = 0f;

        [Tooltip(
            "Chiều dài fallback nếu Road Prefab không có component RoadChunk."
        )]
        [Min(1f)]
        [SerializeField] private float chunkLength = 180f;

        [Min(1)]
        [SerializeField] private int numberOfChunks = 3;

        [Header("Road Streaming")]

        [Tooltip("Khoảng đường giữ lại phía sau Player trước khi despawn Road cũ.")]
        [Min(0f)]
        [SerializeField] private float despawnBehindPlayer = 45f;

        [Tooltip("Khoảng đường Road đầu tiên nằm phía sau Player khi bắt đầu hoặc revive.")]
        [Min(0f)]
        [SerializeField] private float initialRoadBehindPlayer = 25f;

        [Tooltip("Road phải luôn được spawn đủ xa phía trước Player.")]
        [Min(0f)]
        [SerializeField] private float forwardCoverage = 360f;


        //=========================================================
        // TRAFFIC PREFABS
        //=========================================================

        [Header("Traffic Prefabs")]

        [SerializeField] private GameObject carPrefab;

        [SerializeField] private GameObject motorcyclePrefab;

        [Tooltip("Prefab xe máy thứ 2 để tăng độ đa dạng traffic.")]
        [SerializeField] private GameObject motorcyclePrefab2;

        [SerializeField] private GameObject busPrefab;

        [SerializeField] private GameObject baGacPrefab;


        //=========================================================
        // COIN SPAWNER
        //=========================================================

        [Header("Coin Spawner")]

        [Tooltip(
            "CoinSpawner chịu trách nhiệm spawn Coin. " +
            "RoadManager chỉ gửi Z, lane traffic và chiều dài Road."
        )]
        [SerializeField] private CoinSpawner coinSpawner;


        //=========================================================
        // LANES
        //=========================================================

        [Header("Lane Positions")]

        [SerializeField] private float leftLaneX = -6.5f;

        [SerializeField] private float centerLaneX = 0f;

        [SerializeField] private float rightLaneX = 6.5f;


        //=========================================================
        // TRAFFIC DENSITY
        //=========================================================

        [Header("Traffic Density")]

        [Min(0)]
        [SerializeField] private int minTrafficPerChunk = 1;

        [Min(0)]
        [SerializeField] private int maxTrafficPerChunk = 1;

        [Tooltip("Khoảng cách tối thiểu giữa traffic.")]
        [Min(0f)]
        [SerializeField] private float minimumTrafficGap = 14f;


        //=========================================================
        // LANE BALANCE
        //=========================================================

        [Header("Lane Balance")]

        [SerializeField] private bool balancedLaneSpawn = true;


        //=========================================================
        // PLAYER
        //=========================================================

        [Header("Player")]

        [SerializeField] private Transform playerTransform;


        //=========================================================
        // HEIGHT
        //=========================================================

        [Header("Traffic Spawn Height")]

        [SerializeField] private float trafficSpawnY = 0.8f;


        //=========================================================
        // SPAWN
        //=========================================================

        [Header("Spawn Settings")]

        [Min(0f)]
        [SerializeField] private float spawnEdgePadding = 6f;


        //=========================================================
        // VEHICLE PROBABILITY
        //=========================================================

        [Header("Vehicle Probability")]

        [Range(0f, 1f)]
        [SerializeField] private float carProbability = 0.35f;

        [Range(0f, 1f)]
        [SerializeField] private float motorcycleProbability = 0.20f;

        [Range(0f, 1f)]
        [SerializeField] private float motorcycle2Probability = 0.20f;

        [Range(0f, 1f)]
        [SerializeField] private float busProbability = 0.10f;

        [Range(0f, 1f)]
        [SerializeField] private float baGacProbability = 0.15f;


        //=========================================================
        // SPEED
        //=========================================================

        [Header("Traffic Speed")]

        [Min(0f)]
        [SerializeField] private float minimumTrafficSpeed = 7f;

        [Min(0f)]
        [SerializeField] private float maximumTrafficSpeed = 13f;


        //=========================================================
        // DEBUG
        //=========================================================

        [Header("Debug")]

        [SerializeField] private bool debugLogs = false;


        //=========================================================
        // INTERNAL
        //=========================================================

        private readonly List<GameObject> activeRoads =
            new List<GameObject>(8);

        private readonly List<float> activeRoadLengths =
            new List<float>(8);

        // Các Heaven road đang active.
        // Dùng riêng để ExitHeaven có thể dọn đúng visual road.
        private readonly List<GameObject> activeHeavenRoads =
            new List<GameObject>(8);

        private readonly List<float> activeHeavenRoadLengths =
            new List<float>(8);

        private readonly List<TrafficCarBehavior> activeTraffic =
            new List<TrafficCarBehavior>(32);

        /*
        * Buffer dùng lại cho GenerateTrafficPositions().
        *
        * Không tạo List mới mỗi lần spawn Road.
        */
        private readonly List<float> trafficPositionBuffer =
            new List<float>(16);

        private float spawnZ;


        //=========================================================
        // BIOME STATE
        //=========================================================

        // Weighted random is independent per spawn.
        // No last-biome state is required.


        //=========================================================
        // LANE CURSOR
        //=========================================================

        /*
        * 0 = LEFT
        * 1 = CENTER
        * 2 = RIGHT
        *
        * LEFT -> CENTER -> RIGHT -> LEFT
        */

        private int nextLaneIndex = 0;


        //=========================================================
        // UNITY
        //=========================================================

        private void Awake()
        {
            ClampSettings();
        }


        //=========================================================
        // START
        //=========================================================

        private void Start()
        {
            //=====================================================
            // INITIALIZATION
            //=====================================================

            // Clamp lại tại Start để runtime luôn an toàn.
            ClampSettings();

            FindPlayer();
            FindCoinSpawner();

            if (debugLogs)
            {
                Debug.Log(
                    "[RoadManager] START | " +
                    "Player = " +
                    (
                        playerTransform != null
                            ? playerTransform.name
                            : "NULL"
                    ) +
                    " | LegacyRoadPrefab = " +
                    (
                        roadPrefab != null
                            ? roadPrefab.name
                            : "NULL"
                    ) +
                    " | BiomeCount = " +
                    (
                        biomeElements != null
                            ? biomeElements.Length
                            : 0
                    ) +
                    " | NumberOfChunks = " +
                    numberOfChunks +
                    " | ChunkLength = " +
                    chunkLength.ToString("F1") +
                    " | InitialBehind = " +
                    initialRoadBehindPlayer.ToString("F1") +
                    " | ForwardCoverage = " +
                    forwardCoverage.ToString("F1"),
                    this
                );
            }

            // Legacy Road hoặc Biome đều có thể làm nguồn spawn.
            if (
                !HasAnyValidBiome() &&
                roadPrefab == null
            )
            {
                Debug.LogError(
                    "[RoadManager] START FAILED | " +
                    "Không có Road Prefab hoặc Biome Element hợp lệ.",
                    this
                );

                return;
            }

            //=====================================================
            // INITIAL ROAD PREFAB
            //=====================================================

            GameObject firstPrefab =
                GetRoadPrefabForInitialSpawn();

            if (firstPrefab == null)
            {
                Debug.LogError(
                    "[RoadManager] START FAILED | " +
                    "GetRoadPrefabForInitialSpawn() trả về NULL.",
                    this
                );

                return;
            }

            float firstRoadLength =
                GetRoadLength(firstPrefab);

            if (firstRoadLength <= 0f)
            {
                Debug.LogError(
                    "[RoadManager] START FAILED | " +
                    "Road Length không hợp lệ | Prefab = " +
                    firstPrefab.name +
                    " | Length = " +
                    firstRoadLength.ToString("F2"),
                    firstPrefab
                );

                return;
            }

            if (debugLogs)
            {
                Debug.Log(
                    "[RoadManager] INITIAL ROAD | " +
                    "Prefab = " +
                    firstPrefab.name +
                    " | Length = " +
                    firstRoadLength.ToString("F1") +
                    " | Active = " +
                    firstPrefab.activeSelf,
                    firstPrefab
                );
            }

            //=====================================================
            // INITIAL ROAD POSITION
            //=====================================================

            if (playerTransform != null)
            {
                // spawnZ là START Z logic của chunk kế tiếp.
                // Road root nằm ở CENTER:
                // centerZ = startZ + length / 2.
                spawnZ =
                    playerTransform.position.z -
                    initialRoadBehindPlayer;
            }
            else
            {
                // Update() sẽ tự tìm Player lại khi Player chưa tồn tại.
                spawnZ =
                    -initialRoadBehindPlayer;
            }

            //=====================================================
            // INITIAL ROAD GENERATION
            //=====================================================

            for (
                int i = 0;
                i < numberOfChunks;
                i++
            )
            {
                int roadCountBefore =
                    activeRoads.Count;

                float spawnZBefore =
                    spawnZ;

                // Chunk đầu dùng đúng prefab đã chọn.
                // Các chunk sau giữ nguyên logic biome/random hiện tại.
                if (i == 0)
                {
                    SpawnRoad(firstPrefab);
                }
                else
                {
                    SpawnRoad();
                }

                // Không tiếp tục vòng lặp nếu SpawnRoad thất bại.
                if (
                    activeRoads.Count == roadCountBefore ||
                    Mathf.Approximately(
                        spawnZ,
                        spawnZBefore
                    )
                )
                {
                    Debug.LogError(
                        "[RoadManager] INITIAL ROAD GENERATION STOPPED | " +
                        "ChunkIndex = " +
                        i +
                        " | ActiveRoads = " +
                        activeRoads.Count +
                        " | SpawnZ = " +
                        spawnZ.ToString("F1"),
                        this
                    );

                    break;
                }
            }

            //=====================================================
            // VERIFY INITIAL WORLD
            //=====================================================

            if (activeRoads.Count == 0)
            {
                Debug.LogError(
                    "[RoadManager] INITIAL SPAWN FAILED | " +
                    "Không có Road nào được tạo. " +
                    "Xem log [RoadManager] ROAD SPAWN FAILED phía trên.",
                    this
                );

                return;
            }

            // Đảm bảo đủ Road phía trước nếu Player đã tồn tại.
            EnsureForwardCoverage();

            if (debugLogs)
            {
                Debug.Log(
                    "[RoadManager] INITIAL SPAWN SUCCESS | " +
                    "Roads = " +
                    activeRoads.Count +
                    " | NextSpawnZ = " +
                    spawnZ.ToString("F1"),
                    this
                );
            }
        }


        //=========================================================
        // UPDATE
        //=========================================================

        private void Update()
        {
            CleanupTraffic();

            if (playerTransform == null)
            {
                FindPlayer();

                if (playerTransform == null)
                {
                    return;
                }
            }

            if (activeRoads.Count == 0)
            {
                spawnZ =
                    playerTransform.position.z -
                    initialRoadBehindPlayer;

                SpawnRoad();

                if (activeRoads.Count == 0)
                {
                    return;
                }
            }

            //=====================================================
            // GROUND ROAD STREAMING - FORWARD
            //=====================================================

            // Ground luôn chạy bằng spawnZ riêng.
            // Heaven không được phép chiếm spawnZ của Ground.
            EnsureForwardCoverage();

            //=====================================================
            // HEAVEN ROAD STREAMING
            //=====================================================

            UpdateHeavenRoadStreaming();

            //=====================================================
            // HEAVEN VISUAL TRANSITION
            //=====================================================

            UpdateHeavenTransition();
            ApplyHeavenRoadFade();

            //=====================================================
            // GROUND ROAD STREAMING - BACKWARD DESPAWN
            //=====================================================

            DespawnRoadsBehindPlayer();
        }


        //=========================================================
        // FIND PLAYER
        //=========================================================

        private void FindPlayer()
        {
            if (playerTransform != null)
            {
                return;
            }

            GameObject player =
                GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                playerTransform =
                    player.transform;
            }
        }


        //=========================================================
        // FIND COIN SPAWNER
        //=========================================================

        private void FindCoinSpawner()
        {
            if (coinSpawner != null)
            {
                return;
            }

            coinSpawner =
                FindFirstObjectByType<CoinSpawner>();

            if (coinSpawner == null)
            {
                Debug.LogWarning(
                    "[RoadManager] Không tìm thấy CoinSpawner. " +
                    "Traffic vẫn spawn nhưng Coin sẽ không được spawn.",
                    this
                );
            }
        }


        //=========================================================
        // BIOME VALIDATION
        //=========================================================

        private bool HasAnyValidBiome()
        {
            if (biomeElements == null)
            {
                return false;
            }

            for (
                int i = 0;
                i < biomeElements.Length;
                i++
            )
            {
                BiomeElement biome =
                    biomeElements[i];

                if (biome == null)
                {
                    continue;
                }

                if (!biome.enabled)
                {
                    continue;
                }

                if (biome.roadPrefab == null)
                {
                    continue;
                }

                if (biome.weight <= 0f)
                {
                    continue;
                }

                return true;
            }

            return false;
        }


        //=========================================================
        // GET INITIAL ROAD PREFAB
        //=========================================================

        private GameObject GetRoadPrefabForInitialSpawn()
        {
            /*
            * Nếu random biome đang bật:
            *
            * chọn biome đầu tiên bằng hệ thống random.
            *
            * Nếu không:
            *
            * dùng roadPrefab cũ.
            */

            if (randomBiome && HasAnyValidBiome())
            {
                BiomeElement biome =
                    ChooseNextBiome();

                if (
                    biome != null &&
                    biome.roadPrefab != null
                )
                {
                    return biome.roadPrefab;
                }
            }

            return roadPrefab;
        }


        //=========================================================
        // GET ROAD LENGTH
        //=========================================================

        private float GetRoadLength(
            GameObject prefab
        )
        {
            if (prefab == null)
            {
                return GetFallbackChunkLength();
            }

            RoadChunk roadChunk =
                prefab.GetComponent<RoadChunk>();

            if (
                roadChunk != null &&
                roadChunk.Length > 0f
            )
            {
                return roadChunk.Length;
            }

            return GetFallbackChunkLength();
        }


        //=========================================================
        // FALLBACK LENGTH
        //=========================================================

        private float GetFallbackChunkLength()
        {
            if (chunkLength < 1f)
            {
                return 1f;
            }

            return chunkLength;
        }


        //=========================================================
        // CHOOSE NEXT BIOME
        //=========================================================

        private BiomeElement ChooseNextBiome()
        {
            if (
                biomeElements == null ||
                biomeElements.Length == 0
            )
            {
                return null;
            }

            //=====================================================
            // COUNT VALID BIOMES + TOTAL WEIGHT
            //=====================================================

            float totalWeight = 0f;
            int validBiomeCount = 0;

            for (
                int i = 0;
                i < biomeElements.Length;
                i++
            )
            {
                BiomeElement biome =
                    biomeElements[i];

                if (!IsValidBiome(biome))
                {
                    continue;
                }

                validBiomeCount++;
                totalWeight += biome.weight;
            }

            if (
                validBiomeCount == 0 ||
                totalWeight <= 0f
            )
            {
                return null;
            }

            //=====================================================
            // SINGLE BIOME
            //=====================================================

            if (validBiomeCount == 1)
            {
                for (
                    int i = 0;
                    i < biomeElements.Length;
                    i++
                )
                {
                    BiomeElement biome =
                        biomeElements[i];

                    if (IsValidBiome(biome))
                    {
                        return biome;
                    }
                }

                return null;
            }

            //=====================================================
            // INDEPENDENT WEIGHTED RANDOM
            //=====================================================
            //
            // Không loại Road/Biome vừa spawn.
            //
            // Các chuỗi sau đều hợp lệ:
            //
            // Normal -> Normal -> Normal
            // Alley  -> Alley  -> Normal
            // Normal -> Alley  -> Alley
            //
            // Xác suất mỗi lần spawn:
            //
            // probability = biome.weight / totalWeight
            //
            //=====================================================

            float roll =
                UnityEngine.Random.value *
                totalWeight;

            for (
                int i = 0;
                i < biomeElements.Length;
                i++
            )
            {
                BiomeElement biome =
                    biomeElements[i];

                if (!IsValidBiome(biome))
                {
                    continue;
                }

                if (roll < biome.weight)
                {
                    return biome;
                }

                roll -= biome.weight;
            }

            //=====================================================
            // FLOATING-POINT SAFETY FALLBACK
            //=====================================================

            for (
                int i = biomeElements.Length - 1;
                i >= 0;
                i--
            )
            {
                BiomeElement biome =
                    biomeElements[i];

                if (IsValidBiome(biome))
                {
                    return biome;
                }
            }

            return null;
        }


        //=========================================================
        // VALID BIOME CHECK
        //=========================================================

        private static bool IsValidBiome(
            BiomeElement biome
        )
        {
            return
                biome != null &&
                biome.enabled &&
                biome.roadPrefab != null &&
                biome.weight > 0f;
        }


        //=========================================================
        // GET NEXT ROAD PREFAB
        //=========================================================

        private GameObject GetNextRoadPrefab()
        {
            /*
            * Nếu random biome tắt:
            * dùng hệ thống cũ.
            */

            if (!randomBiome)
            {
                return roadPrefab;
            }


            /*
            * Random biome bật nhưng chưa có Element:
            * fallback roadPrefab.
            */

            if (!HasAnyValidBiome())
            {
                return roadPrefab;
            }


            BiomeElement biome =
                ChooseNextBiome();

            if (
                biome == null ||
                biome.roadPrefab == null
            )
            {
                return roadPrefab;
            }

            return biome.roadPrefab;
        }


        //=========================================================
        // SPAWN ROAD
        //=========================================================

        private void SpawnRoad()
        {
            SpawnRoad(null);
        }


        //=========================================================
        // SPAWN ROAD - FORCED PREFAB / CENTER PIVOT
        //=========================================================

    private void SpawnRoad(
        GameObject forcedRoadPrefab
    )
    {
        GameObject selectedRoadPrefab =
            forcedRoadPrefab != null
                ? forcedRoadPrefab
                : GetNextRoadPrefab();

        if (selectedRoadPrefab == null)
        {
            Debug.LogError(
                "[RoadManager] ROAD SPAWN FAILED | " +
                "Không có Road Prefab để spawn.",
                this
            );

            return;
        }

        float currentRoadLength =
            GetRoadLength(
                selectedRoadPrefab
            );

        if (currentRoadLength <= 0f)
        {
            Debug.LogError(
                "[RoadManager] ROAD SPAWN FAILED | " +
                "Road length không hợp lệ | Prefab = " +
                selectedRoadPrefab.name +
                " | Length = " +
                currentRoadLength.ToString("F2"),
                selectedRoadPrefab
            );

            return;
        }

        //=====================================================
        // CENTER-PIVOT PLACEMENT
        //=====================================================

        /*
        * Quy ước của RoadManager:
        *
        * spawnZ = START Z logic của chunk.
        * Road prefab có ROOT / PIVOT ở CENTER.
        * Vì vậy:
        *
        * CenterZ = StartZ + Length / 2
        * StartZ  = CenterZ - Length / 2
        * EndZ    = CenterZ + Length / 2
        *
        * Ví dụ Length = 180:
        *
        * Road 1: 0   -> 180 | Center = 90
        * Road 2: 180 -> 360 | Center = 270
        * Road 3: 360 -> 540 | Center = 450
        *
        * spawnZ vẫn chỉ là tọa độ START logic.
        * Không thay đổi kiến trúc streaming / traffic.
        */

        float roadStartZ =
            spawnZ;

        float roadEndZ =
            roadStartZ +
            currentRoadLength;

        float roadCenterZ =
            roadStartZ +
            currentRoadLength *
            0.5f;

        float roadY =
            GetRoadY(selectedRoadPrefab);

        GameObject road =
            Instantiate(
                selectedRoadPrefab,
                new Vector3(
                    0f,
                    roadY,
                    roadCenterZ
                ),
                Quaternion.identity,
                transform
            );

        if (road == null)
        {
            Debug.LogError(
                "[RoadManager] ROAD SPAWN FAILED | " +
                "Instantiate() trả về NULL | Prefab = " +
                selectedRoadPrefab.name,
                this
            );

            return;
        }

        // Instantiate giữ nguyên trạng thái active/inactive của prefab.
        // Nếu prefab bị lưu Inactive, Road đã tạo nhưng sẽ không hiển thị.
        // RoadManager là owner runtime nên kích hoạt clone tại đây.
        if (!road.activeSelf)
        {
            road.SetActive(true);

            if (debugLogs)
            {
                Debug.LogWarning(
                    "[RoadManager] ROAD WAS INACTIVE | " +
                    "Đã tự SetActive(true): " +
                    road.name,
                    road
                );
            }
        }

        if (debugLogs)
        {
            Debug.Log(
                "[RoadManager] BIOME SPAWNED | " +
                road.name +
                " | Prefab = " +
                selectedRoadPrefab.name +
                " | StartZ = " +
                roadStartZ.ToString("F1") +
                " | CenterZ = " +
                roadCenterZ.ToString("F1") +
                " | EndZ = " +
                roadEndZ.ToString("F1"),
                road
            );
        }

        //=====================================================
        // REGISTER ROAD
        //=====================================================

        activeRoads.Add(
            road
        );

        activeRoadLengths.Add(
            currentRoadLength
        );

        //=====================================================
        // ALLEY BIOME SPECIAL EVENT
        //=====================================================

        /*
        * AlleyBiomeSpawner nằm trực tiếp bên trong
        * Road_Alley prefab.
        *
        * RoadManager KHÔNG quản lý:
        *
        * - Spawn Point
        * - Direction
        * - Trigger Distance
        * - Event Chance
        * - Số lượng Ambush Vehicle
        *
        * RoadManager chỉ ARM spawner sau khi
        * Road_Alley được spawn.
        *
        * TrySpawnAmbush() là VOID:
        * nó không trả về bool.
        *
        * Spawner sẽ tự theo dõi Player và quyết định
        * thời điểm thực sự spawn Ambush.
        */

        AlleyBiomeSpawner alleySpawner =
            road.GetComponentInChildren<AlleyBiomeSpawner>();

        if (alleySpawner != null)
        {
            alleySpawner.TrySpawnAmbush();

            if (debugLogs)
            {
                Debug.Log(
                    "[RoadManager] ALLEY SPAWNER ARMED | " +
                    road.name,
                    road
                );
            }
        }

        //=====================================================
        // SPAWN NORMAL TRAFFIC
        //=====================================================

        /*
        * Traffic dùng START Z thực tế,
        * không dùng pivot CENTER.
        */

        SpawnTrafficForChunk(
            roadStartZ,
            currentRoadLength
        );

        //=====================================================
        // NEXT ROAD START Z
        //=====================================================

        /*
        * Chunk tiếp theo bắt đầu đúng tại
        * END của chunk hiện tại.
        */

        spawnZ =
            roadEndZ;

        //=====================================================
        // DEBUG
        //=====================================================

        if (debugLogs)
        {
            Debug.Log(
                "[RoadManager] ROAD SPAWN | " +
                road.name +
                " | StartZ = " +
                roadStartZ.ToString("F1") +
                " | CenterZ = " +
                roadCenterZ.ToString("F1") +
                " | Length = " +
                currentRoadLength.ToString("F1") +
                " | EndZ = " +
                roadEndZ.ToString("F1"),
                this
            );
        }
    }


        //=========================================================
        //=========================================================
        // HEAVEN ROAD CONTROL
        //=========================================================

        public bool IsHeavenMode
        {
            get { return heavenMode; }
        }

        public float HeavenVisualBlend01
        {
            get { return heavenVisualBlend01; }
        }


        //=========================================================
        // FIND ROAD CONTAINING PLAYER
        //=========================================================

        private int FindRoadContainingPlayer()
        {
            if (playerTransform == null)
            {
                return -1;
            }

            float playerZ =
                playerTransform.position.z;

            const float epsilon = 0.01f;

            for (
                int i = 0;
                i < activeRoads.Count;
                i++
            )
            {
                float startZ =
                    GetRoadStartZ(i);

                float endZ =
                    GetRoadEndZ(i);

                if (
                    playerZ >= startZ - epsilon &&
                    playerZ < endZ + epsilon
                )
                {
                    return i;
                }
            }

            return -1;
        }


        //=========================================================
        // GET NEXT SAFE ROAD BOUNDARY
        //=========================================================

        private float GetPlayerRoadBoundaryZ()
        {
            int roadIndex =
                FindRoadContainingPlayer();

            if (
                roadIndex >= 0 &&
                roadIndex < activeRoads.Count
            )
            {
                return GetRoadEndZ(
                    roadIndex
                );
            }

            return spawnZ;
        }


        //=========================================================
        // TRIM ROADS AFTER TRANSITION BOUNDARY
        //=========================================================

        private void TrimRoadsAfterBoundary(
            float boundaryZ
        )
        {
            const float epsilon = 0.01f;

            for (
                int i = activeRoads.Count - 1;
                i >= 0;
                i--
            )
            {
                float roadStartZ =
                    GetRoadStartZ(i);

                if (
                    roadStartZ <
                    boundaryZ - epsilon
                )
                {
                    continue;
                }

                GameObject road =
                    activeRoads[i];

                activeRoads.RemoveAt(i);

                if (
                    i <
                    activeRoadLengths.Count
                )
                {
                    activeRoadLengths.RemoveAt(i);
                }

                activeHeavenRoads.Remove(
                    road
                );

                if (road != null)
                {
                    Destroy(road);
                }
            }

            // Traffic nằm trên các chunk đã bỏ
            // cũng phải được xoá để không tồn tại
            // độc lập giữa hai biome.
            for (
                int i = activeTraffic.Count - 1;
                i >= 0;
                i--
            )
            {
                TrafficCarBehavior traffic =
                    activeTraffic[i];

                if (traffic == null)
                {
                    activeTraffic.RemoveAt(i);
                    continue;
                }

                if (
                    traffic.transform.position.z >=
                    boundaryZ - epsilon
                )
                {
                    Destroy(
                        traffic.gameObject
                    );

                    activeTraffic.RemoveAt(i);
                }
            }
        }


        //=========================================================
        // ALIGN HEAVEN VISUAL ENVIRONMENT TO PLAYER
        //=========================================================

        private void AlignHeavenEnvironmentToPlayer()
        {
            if (playerTransform == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(heavenEnvironmentRootName))
            {
                return;
            }

            if (heavenEnvironmentRoot == null)
            {
                GameObject rootObject =
                    GameObject.Find(
                        heavenEnvironmentRootName
                    );

                if (rootObject != null)
                {
                    heavenEnvironmentRoot =
                        rootObject.transform;
                }
            }

            if (heavenEnvironmentRoot == null)
            {
                if (debugLogs)
                {
                    Debug.LogWarning(
                        "[RoadManager] HEAVEN VISUAL ROOT NOT FOUND | " +
                        heavenEnvironmentRootName,
                        this
                    );
                }

                return;
            }

            Vector3 rootPosition =
                heavenEnvironmentRoot.position;

            // HeavenBiomeSetup tạo object theo local/world Z bắt đầu từ startZ = 0.
            // Đưa vùng Heaven đầu tiên tới gần Player.
            rootPosition.z =
                playerTransform.position.z +
                Mathf.Max(
                    0f,
                    heavenEnvironmentNearDistance
                );

            heavenEnvironmentRoot.position =
                rootPosition;

            if (debugLogs)
            {
                Debug.Log(
                    "[RoadManager] HEAVEN VISUAL ALIGNED | " +
                    "PlayerZ = " +
                    playerTransform.position.z.ToString("F1") +
                    " | RootZ = " +
                    rootPosition.z.ToString("F1") +
                    " | NearDistance = " +
                    heavenEnvironmentNearDistance.ToString("F1"),
                    heavenEnvironmentRoot
                );
            }
        }


        //=========================================================
        // HEAVEN TRANSITION BEGIN
        //=========================================================

        private void BeginHeavenTransition(
            bool entering
        )
        {
            // Heaven is an instantaneous visual state.
            // There is deliberately NO fade / blend / anticipation phase.
            heavenTransitioning = false;
            heavenTransitionEntering = entering;
            heavenVisualBlend01 = entering ? 1f : 0f;

            if (playerTransform != null)
            {
                heavenTransitionBoundaryZ = playerTransform.position.z;
            }
        }


        //=========================================================
        // SPAWN HEAVEN ROAD - INDEPENDENT STREAM
        //=========================================================

        private bool SpawnHeavenRoad()
        {
            if (heavenRoadPrefab == null)
            {
                return false;
            }

            float currentRoadLength =
                GetRoadLength(
                    heavenRoadPrefab
                );

            if (currentRoadLength <= 0f)
            {
                Debug.LogError(
                    "[RoadManager] HEAVEN ROAD SPAWN FAILED | " +
                    "Length không hợp lệ | Prefab = " +
                    heavenRoadPrefab.name,
                    heavenRoadPrefab
                );

                return false;
            }

            float roadStartZ =
                heavenSpawnZ;

            float roadEndZ =
                roadStartZ +
                currentRoadLength;

            float roadCenterZ =
                roadStartZ +
                currentRoadLength *
                0.5f;

            GameObject road =
                Instantiate(
                    heavenRoadPrefab,
                    new Vector3(
                        0f,
                        heavenRoadY,
                        roadCenterZ
                    ),
                    Quaternion.identity,
                    transform
                );

            if (road == null)
            {
                Debug.LogError(
                    "[RoadManager] HEAVEN ROAD SPAWN FAILED | " +
                    "Instantiate() trả về NULL.",
                    this
                );

                return false;
            }

            if (!road.activeSelf)
            {
                road.SetActive(true);
            }

            activeHeavenRoads.Add(
                road
            );

            activeHeavenRoadLengths.Add(
                currentRoadLength
            );

            //=====================================================
            // HEAVEN COINS - ONE ROW PER HEAVEN CHUNK
            //=====================================================
            //
            // Mỗi Heaven Road chunk phải tự tạo coin row riêng.
            // Không dùng Ground spawnZ và không phụ thuộc vào
            // ramp-trigger coin trail.
            //
            // CoinSpawner tự cộng heavenCoinHeightOffset +
            // heavenCoinExtraY, nên Y ở đây chính là Y gameplay
            // của Heaven Road.
            //=====================================================

            if (coinSpawner != null)
            {
                coinSpawner.SpawnHeavenCoins(
                    roadStartZ,
                    currentRoadLength,
                    heavenRoadY
                );
            }

            heavenSpawnZ =
                roadEndZ;

            if (debugLogs)
            {
                Debug.Log(
                    "[RoadManager] HEAVEN ROAD SPAWNED | " +
                    road.name +
                    " | StartZ = " +
                    roadStartZ.ToString("F1") +
                    " | EndZ = " +
                    roadEndZ.ToString("F1") +
                    " | Y = " +
                    heavenRoadY.ToString("F2"),
                    road
                );
            }

            return true;
        }


        //=========================================================
        // HEAVEN ROAD START Z
        //=========================================================

        private float GetHeavenRoadStartZ(
            int index
        )
        {
            if (
                index < 0 ||
                index >= activeHeavenRoads.Count ||
                index >= activeHeavenRoadLengths.Count
            )
            {
                return float.PositiveInfinity;
            }

            GameObject road =
                activeHeavenRoads[index];

            if (road == null)
            {
                return float.PositiveInfinity;
            }

            return road.transform.position.z -
                activeHeavenRoadLengths[index] *
                0.5f;
        }


        //=========================================================
        // HEAVEN ROAD END Z
        //=========================================================

        private float GetHeavenRoadEndZ(
            int index
        )
        {
            if (
                index < 0 ||
                index >= activeHeavenRoads.Count ||
                index >= activeHeavenRoadLengths.Count
            )
            {
                return float.NegativeInfinity;
            }

            GameObject road =
                activeHeavenRoads[index];

            if (road == null)
            {
                return float.NegativeInfinity;
            }

            return road.transform.position.z +
                activeHeavenRoadLengths[index] *
                0.5f;
        }


        //=========================================================
        // HEAVEN ROAD CLEANUP
        //=========================================================

        private void CleanupHeavenRoads()
        {
            for (
                int i = activeHeavenRoads.Count - 1;
                i >= 0;
                i--
            )
            {
                GameObject road =
                    activeHeavenRoads[i];

                if (road != null)
                {
                    Destroy(
                        road
                    );
                }
            }

            activeHeavenRoads.Clear();
            activeHeavenRoadLengths.Clear();
        }


        //=========================================================
        // HEAVEN ROAD STREAMING
        //=========================================================

        private void UpdateHeavenRoadStreaming()
        {
            if (playerTransform == null)
            {
                return;
            }

            if (!heavenMode)
            {
                return;
            }

            float coverage =
                Mathf.Max(
                    heavenForwardCoverage,
                    GetFallbackChunkLength()
                );

            float requiredEndZ =
                playerTransform.position.z +
                coverage;

            int safety = 0;

            while (
                heavenSpawnZ < requiredEndZ &&
                safety < 32
            )
            {
                safety++;

                float spawnZBefore =
                    heavenSpawnZ;

                bool spawned =
                    SpawnHeavenRoad();

                if (
                    !spawned ||
                    Mathf.Approximately(
                        heavenSpawnZ,
                        spawnZBefore
                    )
                )
                {
                    break;
                }
            }

            // Heaven road phía sau Player được cleanup riêng.
            // Không đụng activeRoads của Ground.
            while (
                activeHeavenRoads.Count > 1
            )
            {
                float oldestEndZ =
                    GetHeavenRoadEndZ(0);

                if (
                    playerTransform.position.z <=
                    oldestEndZ +
                    despawnBehindPlayer
                )
                {
                    break;
                }

                GameObject oldest =
                    activeHeavenRoads[0];

                activeHeavenRoads.RemoveAt(0);

                if (
                    activeHeavenRoadLengths.Count > 0
                )
                {
                    activeHeavenRoadLengths.RemoveAt(0);
                }

                if (oldest != null)
                {
                    Destroy(
                        oldest
                    );
                }
            }
        }


        //=========================================================
        // HEAVEN ROAD ENTER
        //=========================================================

        public bool EnterHeaven()
        {
            if (heavenRoadPrefab == null)
            {
                Debug.LogError(
                    "[RoadManager] ENTER HEAVEN FAILED | " +
                    "Heaven Road Prefab chưa được gán.",
                    this
                );

                return false;
            }

            if (playerTransform == null)
            {
                FindPlayer();

                if (playerTransform == null)
                {
                    Debug.LogError(
                        "[RoadManager] ENTER HEAVEN FAILED | " +
                        "Player Transform = NULL.",
                        this
                    );

                    return false;
                }
            }

            if (heavenMode)
            {
                return activeHeavenRoads.Count > 0;
            }

            if (heavenRoadY <= 0f)
            {
                heavenRoadY =
                    playerTransform.position.y;
            }

            //=====================================================
            // CRITICAL RULE
            //=====================================================
            //
            // KHÔNG:
            //
            // - reset spawnZ
            // - lấy END của Ground chunk
            // - TrimRoadsAfterBoundary()
            // - Destroy Ground Road
            //
            // Ground streaming tiếp tục độc lập.
            // Heaven có heavenSpawnZ riêng.
            //=====================================================

            // HEAVEN INSTANT ENTRY:
            // Heaven Road starts directly at the Player.
            // No fade, no anticipation distance, no waiting for a distant chunk.
            heavenSpawnZ = playerTransform.position.z;

            heavenMode = true;

            BeginHeavenTransition(
                true
            );

            // Heaven visual phải xuất hiện ngay gần Player.
            // Đây chỉ là visual environment; không đụng Ground/Traffic/Coin/Game Z.
            AlignHeavenEnvironmentToPlayer();

            bool spawned =
                SpawnHeavenRoad();

            if (!spawned)
            {
                heavenMode = false;
                heavenTransitioning = false;
                heavenVisualBlend01 = 0f;
                heavenSpawnZ = 0f;

                Debug.LogError(
                    "[RoadManager] ENTER HEAVEN FAILED | " +
                    "Không spawn được Heaven Road.",
                    this
                );

                return false;
            }

            EnsureHeavenRoadCoverage();

            if (debugLogs)
            {
                Debug.Log(
                    "[RoadManager] HEAVEN ENTER | " +
                    "PlayerZ = " +
                    playerTransform.position.z.ToString("F1") +
                    " | EntryZ = " +
                    (
                        playerTransform.position.z +
                        heavenEntryDistance
                    ).ToString("F1") +
                    " | HeavenRoads = " +
                    activeHeavenRoads.Count +
                    " | GroundRoads = " +
                    activeRoads.Count,
                    this
                );
            }

            return activeHeavenRoads.Count > 0;
        }


        //=========================================================
        // ENSURE HEAVEN ROAD COVERAGE
        //=========================================================

        private void EnsureHeavenRoadCoverage()
        {
            UpdateHeavenRoadStreaming();
        }


        //=========================================================
        // HEAVEN ROAD EXIT
        //=========================================================

        public void ExitHeaven()
        {
            if (!heavenMode)
            {
                return;
            }

            if (playerTransform == null)
            {
                FindPlayer();
            }

            if (playerTransform == null)
            {
                return;
            }

            //=====================================================
            // CRITICAL RULE
            //=====================================================
            //
            // Ground đã được stream liên tục trong lúc Player ở Heaven.
            //
            // Vì vậy ExitHeaven KHÔNG:
            //
            // - reset spawnZ
            // - rebuild Ground
            // - Trim Ground
            // - Destroy activeRoads
            //
            // Chỉ kết thúc Heaven stream và dọn Heaven visual road.
            //=====================================================

            heavenMode = false;

            BeginHeavenTransition(
                false
            );

            CleanupHeavenRoads();

            // Đảm bảo Ground vẫn có đủ coverage sau khi thoát.
            EnsureForwardCoverage();

            if (debugLogs)
            {
                Debug.Log(
                    "[RoadManager] HEAVEN EXIT | " +
                    "PlayerZ = " +
                    playerTransform.position.z.ToString("F1") +
                    " | GroundRoads = " +
                    activeRoads.Count,
                    this
                );
            }
        }


        //=========================================================
        // HEAVEN TRANSITION UPDATE
        //=========================================================

        private void UpdateHeavenTransition()
        {
            // Intentionally empty:
            // Heaven has no fade or transition animation.
            // The state is set immediately by EnterHeaven()/ExitHeaven().
        }


        //=========================================================
        // HEAVEN VISUAL HOOK
        //=========================================================

        private void ApplyHeavenRoadFade()
        {
            // No runtime fade.
            // HeavenVisualBlend01 is an immediate state:
            // 0 = Ground
            // 1 = Heaven.
        }


        //=========================================================
        // ROAD STREAMING - FORWARD COVERAGE
        //=========================================================

        private void EnsureForwardCoverage()
        {
            if (playerTransform == null)
            {
                return;
            }

            float coverage =
                Mathf.Max(
                    forwardCoverage,
                    GetFallbackChunkLength()
                );

            float requiredEndZ =
                playerTransform.position.z +
                coverage;

            int safety = 0;

            while (
                spawnZ < requiredEndZ &&
                safety < 64
            )
            {
                safety++;

                int roadCountBefore =
                    activeRoads.Count;

                float spawnZBefore =
                    spawnZ;

                SpawnRoad();

                // Bảo vệ vòng lặp nếu Instantiate / prefab lỗi.
                if (
                    activeRoads.Count == roadCountBefore ||
                    Mathf.Approximately(spawnZ, spawnZBefore)
                )
                {
                    break;
                }
            }
        }


        //=========================================================
        // ROAD STREAMING - BACKWARD DESPAWN
        //=========================================================

        private void DespawnRoadsBehindPlayer()
        {
            if (playerTransform == null)
            {
                return;
            }

            // Luôn giữ ít nhất 1 Road để tránh world rỗng.
            while (activeRoads.Count > 1)
            {
                float oldestEndZ =
                    GetRoadEndZ(0);

                if (playerTransform.position.z <=
                    oldestEndZ + despawnBehindPlayer)
                {
                    break;
                }

                DeleteOldestRoad();
            }
        }


        //=========================================================
        // ROAD START Z
        //=========================================================

        private float GetRoadStartZ(int index)
        {
            if (
                index < 0 ||
                index >= activeRoads.Count ||
                index >= activeRoadLengths.Count
            )
            {
                return float.PositiveInfinity;
            }

            GameObject road =
                activeRoads[index];

            if (road == null)
            {
                return float.PositiveInfinity;
            }

            // Road root nằm ở CENTER của prefab.
            // Chuyển từ CenterZ về START Z logic.
            return road.transform.position.z -
                activeRoadLengths[index] * 0.5f;
        }


        //=========================================================
        // ROAD END Z
        //=========================================================

        private float GetRoadEndZ(int index)
        {
            if (
                index < 0 ||
                index >= activeRoads.Count ||
                index >= activeRoadLengths.Count
            )
            {
                return float.NegativeInfinity;
            }

            GameObject road =
                activeRoads[index];

            if (road == null)
            {
                return float.NegativeInfinity;
            }

            // Road root nằm ở CENTER của prefab.
            // Chuyển từ CenterZ về END Z logic.
            return road.transform.position.z +
                activeRoadLengths[index] * 0.5f;
        }


        //=========================================================
        // SPAWN TRAFFIC FOR CHUNK
        //=========================================================

        private void SpawnTrafficForChunk(
            float chunkStartZ,
            float currentChunkLength
        )
        {
            int min =
                Mathf.Max(
                    0,
                    minTrafficPerChunk
                );

            int max =
                Mathf.Max(
                    min,
                    maxTrafficPerChunk
                );

            int count =
                UnityEngine.Random.Range(
                    min,
                    max + 1
                );

            if (count <= 0)
            {
                return;
            }


            List<float> positions =
                GenerateTrafficPositions(
                    chunkStartZ,
                    count,
                    currentChunkLength
                );

            for (
                int i = 0;
                i < positions.Count;
                i++
            )
            {
                SpawnOneTraffic(
                    positions[i],
                    currentChunkLength
                );
            }
        }


        //=========================================================
        // GENERATE TRAFFIC POSITIONS
        //=========================================================

        private List<float> GenerateTrafficPositions(
            float chunkStartZ,
            int count,
            float currentChunkLength
        )
        {
            /*
            * Reuse buffer.
            *
            * Không tạo List mới mỗi chunk.
            */

            trafficPositionBuffer.Clear();

            float minZ =
                chunkStartZ +
                spawnEdgePadding;

            float maxZ =
                chunkStartZ +
                currentChunkLength -
                spawnEdgePadding;


            if (maxZ < minZ)
            {
                return trafficPositionBuffer;
            }


            //=====================================================
            // 1 TRAFFIC / CHUNK
            //=====================================================

            if (count == 1)
            {
                float candidate =
                    Mathf.Lerp(
                        minZ,
                        maxZ,
                        0.5f
                    );

                if (IsSpawnPositionSafe(candidate))
                {
                    trafficPositionBuffer.Add(
                        candidate
                    );
                }

                return trafficPositionBuffer;
            }


            //=====================================================
            // MULTIPLE TRAFFIC
            //=====================================================

            int attempts =
                count * 100;

            while (
                trafficPositionBuffer.Count < count &&
                attempts > 0
            )
            {
                attempts--;

                float candidate =
                    UnityEngine.Random.Range(
                        minZ,
                        maxZ
                    );

                if (!IsSpawnPositionSafe(candidate))
                {
                    continue;
                }


                bool localSafe = true;

                for (
                    int i = 0;
                    i < trafficPositionBuffer.Count;
                    i++
                )
                {
                    if (
                        Mathf.Abs(
                            trafficPositionBuffer[i] -
                            candidate
                        ) <
                        minimumTrafficGap
                    )
                    {
                        localSafe = false;
                        break;
                    }
                }

                if (!localSafe)
                {
                    continue;
                }

                trafficPositionBuffer.Add(
                    candidate
                );
            }

            return trafficPositionBuffer;
        }


        //=========================================================
        // GLOBAL SPAWN SAFETY
        //=========================================================

        private bool IsSpawnPositionSafe(
            float z
        )
        {
            for (
                int i = 0;
                i < activeTraffic.Count;
                i++
            )
            {
                TrafficCarBehavior traffic =
                    activeTraffic[i];

                if (traffic == null)
                {
                    continue;
                }

                float distance =
                    Mathf.Abs(
                        traffic.transform.position.z -
                        z
                    );

                if (
                    distance <
                    minimumTrafficGap * 0.65f
                )
                {
                    return false;
                }
            }

            return true;
        }


        //=========================================================
        // SPAWN ONE TRAFFIC
        //=========================================================

        private void SpawnOneTraffic(
            float z,
            float currentChunkLength
        )
        {
            GameObject prefab =
                ChooseTrafficPrefab();

            if (prefab == null)
            {
                Debug.LogWarning(
                    "[RoadManager] Không có Traffic Prefab.",
                    this
                );

                return;
            }


            //=====================================================
            // SELECT LANE
            //=====================================================

            int selectedLane =
                SelectSpawnLane(z);

            if (selectedLane < 0)
            {
                Debug.LogWarning(
                    "[RoadManager] Không tìm được lane an toàn tại Z = " +
                    z.ToString("F1"),
                    this
                );

                return;
            }


            //=====================================================
            // POSITION
            //=====================================================

            float x =
                GetLaneX(selectedLane);


            GameObject traffic =
                Instantiate(
                    prefab,
                    new Vector3(
                        x,
                        trafficSpawnY,
                        z
                    ),
                    Quaternion.identity,
                    transform
                );

            if (traffic == null)
            {
                return;
            }


            //=====================================================
            // TRAFFIC VEHICLE
            //=====================================================

            TrafficVehicle vehicle =
                traffic.GetComponent<TrafficVehicle>();

            if (vehicle != null)
            {
                float speed =
                    UnityEngine.Random.Range(
                        minimumTrafficSpeed,
                        maximumTrafficSpeed
                    );

                vehicle.SetMoveSpeed(speed);

                vehicle.SetTravelDirection(true);
            }


            //=====================================================
            // TRAFFIC BEHAVIOR
            //=====================================================

            TrafficCarBehavior behavior =
                traffic.GetComponent<TrafficCarBehavior>();

            if (behavior != null)
            {
                behavior.SetLaneIndex(
                    selectedLane
                );

                activeTraffic.Add(
                    behavior
                );
            }
            else
            {
                Debug.LogWarning(
                    "[RoadManager] " +
                    traffic.name +
                    " thiếu TrafficCarBehavior.",
                    traffic
                );
            }


            //=====================================================
            // COIN
            //=====================================================

            /*
            * API hiện tại của CoinSpawner:
            *
            * SpawnCoins(
            *     z,
            *     selectedLane,
            *     currentChunkLength
            * );
            */

            if (coinSpawner != null)
            {
                coinSpawner.SpawnCoins(
                    z,
                    selectedLane,
                    currentChunkLength
                );
            }


            //=====================================================
            // REGISTER LANE
            //=====================================================

            RegisterSpawnedLane(
                selectedLane
            );


            //=====================================================
            // DEBUG
            //=====================================================

            if (debugLogs)
            {
                Debug.Log(
                    "[RoadManager] TRAFFIC SPAWN | " +
                    traffic.name +
                    " | Lane = " +
                    selectedLane +
                    " | X = " +
                    x.ToString("F1") +
                    " | Z = " +
                    z.ToString("F1") +
                    " | RoadLength = " +
                    currentChunkLength.ToString("F1"),
                    this
                );
            }
        }


        //=========================================================
        // SELECT SPAWN LANE
        //=========================================================

        private int SelectSpawnLane(
            float z
        )
        {
            if (!balancedLaneSpawn)
            {
                return SelectRandomSafeLane(z);
            }


            int preferredLane =
                Mathf.Clamp(
                    nextLaneIndex,
                    0,
                    2
                );


            //=====================================================
            // PREFERRED LANE
            //=====================================================

            if (
                IsLaneSpawnSafe(
                    preferredLane,
                    z
                )
            )
            {
                return preferredLane;
            }


            //=====================================================
            // FALLBACK LANES
            //=====================================================

            for (
                int offset = 1;
                offset <= 2;
                offset++
            )
            {
                int lane =
                    (preferredLane + offset) % 3;

                if (
                    IsLaneSpawnSafe(
                        lane,
                        z
                    )
                )
                {
                    return lane;
                }
            }

            return -1;
        }


        //=========================================================
        // REGISTER SPAWNED LANE
        //=========================================================

        private void RegisterSpawnedLane(
            int lane
        )
        {
            nextLaneIndex =
                (lane + 1) % 3;
        }


        //=========================================================
        // RANDOM SAFE LANE
        //=========================================================

        private int SelectRandomSafeLane(
            float z
        )
        {
            int first =
                UnityEngine.Random.Range(
                    0,
                    3
                );

            int second =
                (
                    first +
                    1 +
                    UnityEngine.Random.Range(0, 2)
                ) % 3;

            int third;

            if (
                first != second
            )
            {
                third =
                    3 -
                    first -
                    second;
            }
            else
            {
                third = 0;
            }


            if (
                IsLaneSpawnSafe(
                    first,
                    z
                )
            )
            {
                return first;
            }

            if (
                IsLaneSpawnSafe(
                    second,
                    z
                )
            )
            {
                return second;
            }

            if (
                IsLaneSpawnSafe(
                    third,
                    z
                )
            )
            {
                return third;
            }

            return -1;
        }


        //=========================================================
        // LANE SPAWN SAFETY
        //=========================================================

        private bool IsLaneSpawnSafe(
            int lane,
            float z
        )
        {
            if (
                lane < 0 ||
                lane > 2
            )
            {
                return false;
            }

            float laneX =
                GetLaneX(lane);


            for (
                int i = 0;
                i < activeTraffic.Count;
                i++
            )
            {
                TrafficCarBehavior traffic =
                    activeTraffic[i];

                if (traffic == null)
                {
                    continue;
                }

                float xDistance =
                    Mathf.Abs(
                        traffic.transform.position.x -
                        laneX
                    );

                if (xDistance > 2.5f)
                {
                    continue;
                }

                float zDistance =
                    Mathf.Abs(
                        traffic.transform.position.z -
                        z
                    );

                if (
                    zDistance <
                    minimumTrafficGap
                )
                {
                    return false;
                }
            }

            return true;
        }


        //=========================================================
        // CHOOSE TRAFFIC PREFAB
        //=========================================================

        private GameObject ChooseTrafficPrefab()
        {
            float total =
                carProbability +
                motorcycleProbability +
                motorcycle2Probability +
                busProbability +
                baGacProbability;


            //=====================================================
            // FALLBACK
            //=====================================================

            if (total <= 0f)
            {
                return GetFirstAvailableTrafficPrefab();
            }


            float roll =
                UnityEngine.Random.value *
                total;


            //=====================================================
            // CAR
            //=====================================================

            if (roll < carProbability)
            {
                if (carPrefab != null)
                {
                    return carPrefab;
                }

                return GetFirstAvailableTrafficPrefab();
            }

            roll -= carProbability;


            //=====================================================
            // MOTORCYCLE 1
            //=====================================================

            if (roll < motorcycleProbability)
            {
                if (motorcyclePrefab != null)
                {
                    return motorcyclePrefab;
                }

                return GetFirstAvailableTrafficPrefab();
            }

            roll -= motorcycleProbability;


            //=====================================================
            // MOTORCYCLE 2
            //=====================================================

            if (roll < motorcycle2Probability)
            {
                if (motorcyclePrefab2 != null)
                {
                    return motorcyclePrefab2;
                }

                /*
                * Nếu Motorcycle 2 chưa được gán,
                * fallback về Motorcycle 1.
                */

                if (motorcyclePrefab != null)
                {
                    return motorcyclePrefab;
                }

                return GetFirstAvailableTrafficPrefab();
            }

            roll -= motorcycle2Probability;


            //=====================================================
            // BUS
            //=====================================================

            if (roll < busProbability)
            {
                if (busPrefab != null)
                {
                    return busPrefab;
                }

                return GetFirstAvailableTrafficPrefab();
            }


            //=====================================================
            // BA GAC
            //=====================================================

            if (baGacPrefab != null)
            {
                return baGacPrefab;
            }

            return GetFirstAvailableTrafficPrefab();
        }


        //=========================================================
        // GET FIRST AVAILABLE TRAFFIC
        //=========================================================

        private GameObject GetFirstAvailableTrafficPrefab()
        {
            if (carPrefab != null)
            {
                return carPrefab;
            }

            if (motorcyclePrefab != null)
            {
                return motorcyclePrefab;
            }

            if (motorcyclePrefab2 != null)
            {
                return motorcyclePrefab2;
            }

            if (busPrefab != null)
            {
                return busPrefab;
            }

            if (baGacPrefab != null)
            {
                return baGacPrefab;
            }

            return null;
        }


        //=========================================================
        // GET LANE X
        //=========================================================

        private float GetLaneX(
            int lane
        )
        {
            switch (lane)
            {
                case 0:
                    return leftLaneX;

                case 1:
                    return centerLaneX;

                case 2:
                    return rightLaneX;

                default:
                    return centerLaneX;
            }
        }


        //=========================================================
        // CLEANUP TRAFFIC
        //=========================================================

        private void CleanupTraffic()
        {
            for (
                int i =
                    activeTraffic.Count - 1;
                i >= 0;
                i--
            )
            {
                TrafficCarBehavior traffic =
                    activeTraffic[i];

                if (traffic == null)
                {
                    activeTraffic.RemoveAt(i);
                }
            }
        }


        //=========================================================
        // DELETE OLDEST ROAD
        //=========================================================

        private void DeleteOldestRoad()
        {
            if (activeRoads.Count == 0)
            {
                return;
            }


            //=====================================================
            // ROAD
            //=====================================================

            GameObject oldest =
                activeRoads[0];

            activeRoads.RemoveAt(0);


            //=====================================================
            // ROAD LENGTH
            //=====================================================

            if (activeRoadLengths.Count > 0)
            {
                activeRoadLengths.RemoveAt(0);
            }


            //=====================================================
            // DESTROY
            //=====================================================

            activeHeavenRoads.Remove(
                oldest
            );

            if (oldest != null)
            {
                Destroy(oldest);
            }
        }


        //=========================================================
        // REVIVE WORLD RESET
        //=========================================================

        public bool ResetWorldForRevive(
            float deathZ,
            out float respawnZ
        )
        {
            //=====================================================
            // DEFAULT
            //=====================================================

            respawnZ = deathZ;


            //=====================================================
            // VALIDATE ROAD
            //=====================================================

            if (
                !HasAnyValidBiome() &&
                roadPrefab == null
            )
            {
                Debug.LogError(
                    "[RoadManager] " +
                    "REVIVE RESET FAILED | " +
                    "Không có Road Prefab hoặc Biome Element hợp lệ.",
                    this
                );

                return false;
            }


            //=====================================================
            // CLEAR TRAFFIC
            //=====================================================

            for (
                int i =
                    activeTraffic.Count - 1;
                i >= 0;
                i--
            )
            {
                TrafficCarBehavior traffic =
                    activeTraffic[i];

                if (traffic != null)
                {
                    Destroy(
                        traffic.gameObject
                    );
                }
            }

            activeTraffic.Clear();


            //=====================================================
            // CLEAR ROAD
            //=====================================================

            for (
                int i =
                    activeRoads.Count - 1;
                i >= 0;
                i--
            )
            {
                GameObject road =
                    activeRoads[i];

                if (road != null)
                {
                    Destroy(road);
                }
            }

            activeRoads.Clear();

            activeRoadLengths.Clear();


            //=====================================================
            // RESET STATE
            //=====================================================

            heavenMode = false;
            heavenTransitioning = false;
            heavenTransitionEntering = false;
            heavenTransitionBoundaryZ = 0f;
            heavenVisualBlend01 = 0f;
            heavenSpawnZ = 0f;

            // Heaven roads không còn nằm trong activeRoads,
            // nên Revive phải dọn riêng để không leak GameObject.
            CleanupHeavenRoads();

            nextLaneIndex = 0;

            trafficPositionBuffer.Clear();



            //=====================================================
            // FIND PLAYER
            //=====================================================

            if (playerTransform == null)
            {
                FindPlayer();
            }


            //=====================================================
            // ROAD LENGTH
            //=====================================================

            GameObject firstPrefab =
                GetRoadPrefabForInitialSpawn();

            if (firstPrefab == null)
            {
                Debug.LogError(
                    "[RoadManager] " +
                    "REVIVE RESET FAILED | " +
                    "Không tìm thấy Road Prefab.",
                    this
                );

                return false;
            }


            float firstRoadLength =
                GetRoadLength(
                    firstPrefab
                );

            if (firstRoadLength <= 0f)
            {
                Debug.LogError(
                    "[RoadManager] " +
                    "REVIVE RESET FAILED | " +
                    "Invalid road length.",
                    this
                );

                return false;
            }


            //=====================================================
            // RESET SPAWN Z
            //=====================================================

            spawnZ =
                deathZ -
                initialRoadBehindPlayer;


            //=====================================================
            // REGENERATE ROAD
            //=====================================================

            for (
                int i = 0;
                i < numberOfChunks;
                i++
            )
            {
                // Dùng đúng prefab đã chọn cho chunk đầu tiên.
                if (i == 0)
                {
                    SpawnRoad(firstPrefab);
                }
                else
                {
                    SpawnRoad();
                }
            }

            EnsureForwardCoverage();


            //=====================================================
            // RESPAWN POSITION
            //=====================================================

            respawnZ =
                deathZ;


            //=====================================================
            // DEBUG
            //=====================================================

            if (debugLogs)
            {
                Debug.Log(
                    "[RoadManager] " +
                    "REVIVE RESET SUCCESS | " +
                    "DeathZ = " +
                    deathZ.ToString("F2") +
                    " | RespawnZ = " +
                    respawnZ.ToString("F2") +
                    " | Roads = " +
                    activeRoads.Count,
                    this
                );
            }

            return true;
        }


        //=========================================================
        // GIZMOS
        //=========================================================

        private void OnDrawGizmosSelected()
        {
            for (
                int i = 0;
                i < 3;
                i++
            )
            {
                float x =
                    GetLaneX(i);

                Gizmos.DrawLine(
                    new Vector3(
                        x,
                        0f,
                        transform.position.z
                    ),
                    new Vector3(
                        x,
                        0f,
                        transform.position.z +
                        chunkLength
                    )
                );
            }
        }


        //=========================================================
        // VALIDATION
        //=========================================================

        private void OnValidate()
        {
            ClampSettings();
        }


        //=========================================================
        // CLAMP SETTINGS
        //=========================================================

        private void ClampSettings()
        {
            //=====================================================
            // ROAD
            //=====================================================

            if (chunkLength < 1f)
            {
                chunkLength = 1f;
            }

            if (numberOfChunks < 1)
            {
                numberOfChunks = 1;
            }

            if (despawnBehindPlayer < 0f)
            {
                despawnBehindPlayer = 0f;
            }

            if (initialRoadBehindPlayer < 0f)
            {
                initialRoadBehindPlayer = 0f;
            }

            if (forwardCoverage < 0f)
            {
                forwardCoverage = 0f;
            }

            if (heavenFadeDistance < 0f)
            {
                heavenFadeDistance = 0f;
            }

            if (heavenEntryDistance < 0f)
            {
                heavenEntryDistance = 0f;
            }

            if (heavenForwardCoverage < 1f)
            {
                heavenForwardCoverage = 1f;
            }


            //=====================================================
            // TRAFFIC DENSITY
            //=====================================================

            if (minTrafficPerChunk < 0)
            {
                minTrafficPerChunk = 0;
            }

            if (
                maxTrafficPerChunk <
                minTrafficPerChunk
            )
            {
                maxTrafficPerChunk =
                    minTrafficPerChunk;
            }


            //=====================================================
            // GAP
            //=====================================================

            if (minimumTrafficGap < 0f)
            {
                minimumTrafficGap = 0f;
            }


            //=====================================================
            // SPAWN PADDING
            //=====================================================

            if (spawnEdgePadding < 0f)
            {
                spawnEdgePadding = 0f;
            }


            //=====================================================
            // SPEED
            //=====================================================

            if (minimumTrafficSpeed < 0f)
            {
                minimumTrafficSpeed = 0f;
            }

            if (
                maximumTrafficSpeed <
                minimumTrafficSpeed
            )
            {
                maximumTrafficSpeed =
                    minimumTrafficSpeed;
            }


            //=====================================================
            // PROBABILITY
            //=====================================================

            float probabilityTotal =
                carProbability +
                motorcycleProbability +
                motorcycle2Probability +
                busProbability +
                baGacProbability;


            //=====================================================
            // FALLBACK
            //=====================================================

            if (probabilityTotal <= 0f)
            {
                carProbability = 1f;

                motorcycleProbability = 0f;

                motorcycle2Probability = 0f;

                busProbability = 0f;

                baGacProbability = 0f;
            }


            //=====================================================
            // BIOME
            //=====================================================

            if (biomeElements != null)
            {
                for (
                    int i = 0;
                    i < biomeElements.Length;
                    i++
                )
                {
                    BiomeElement biome =
                        biomeElements[i];

                    if (biome == null)
                    {
                        continue;
                    }

                    if (biome.weight < 0f)
                    {
                        biome.weight = 0f;
                    }

                    if (string.IsNullOrWhiteSpace(
                        biome.biomeName
                    ))
                    {
                        biome.biomeName =
                            "Biome " + i;
                    }
                }
            }
        }

        private float GetRoadY(GameObject selectedRoadPrefab)
    {
        if (selectedRoadPrefab == null)
            return 0f;

        if (biomeElements != null)
        {
            for (int i = 0; i < biomeElements.Length; i++)
            {
                BiomeElement element = biomeElements[i];

                if (element == null)
                    continue;

                if (element.roadPrefab == selectedRoadPrefab)
                    return element.roadY;
            }
        }

        return 0f;
    }
    }
