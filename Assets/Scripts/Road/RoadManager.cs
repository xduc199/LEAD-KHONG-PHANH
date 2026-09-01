using System.Collections.Generic;
using UnityEngine;

public class RoadManager : MonoBehaviour
{
    //=========================================================
    // ROAD
    //=========================================================

    [Header("Road Settings")]
    [SerializeField] private GameObject roadPrefab;
    [SerializeField] private float chunkLength = 20f;
    [SerializeField] private int numberOfChunks = 7;


    //=========================================================
    // TRAFFIC PREFABS
    //=========================================================

    [Header("Traffic Prefabs")]
    [SerializeField] private GameObject carPrefab;
    [SerializeField] private GameObject motorcyclePrefab;
    [SerializeField] private GameObject busPrefab;
    [SerializeField] private GameObject baGacPrefab;


    //=========================================================
    // COIN SPAWNER
    //=========================================================

    [Header("Coin Spawner")]
    [Tooltip(
        "CoinSpawner chịu trách nhiệm hoàn toàn việc sinh Coin. " +
        "RoadManager chỉ gửi lane và vị trí traffic."
    )]
    [SerializeField] private CoinSpawner coinSpawner;


    //=========================================================
    // LANES
    //=========================================================

    [Header("Lane Positions")]
    [SerializeField] private float leftLaneX = -5f;
    [SerializeField] private float centerLaneX = 0f;
    [SerializeField] private float rightLaneX = 5f;


    //=========================================================
    // TRAFFIC DENSITY
    //=========================================================

    [Header("Traffic Density")]
    [SerializeField] private int minTrafficPerChunk = 1;
    [SerializeField] private int maxTrafficPerChunk = 1;

    [Tooltip("Khoảng cách tối thiểu giữa traffic.")]
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
    [SerializeField] private float spawnEdgePadding = 6f;


    //=========================================================
    // VEHICLE PROBABILITY
    //=========================================================

    [Header("Vehicle Probability")]

    [Range(0f, 1f)]
    [SerializeField] private float carProbability = 0.45f;

    [Range(0f, 1f)]
    [SerializeField] private float motorcycleProbability = 0.30f;

    [Range(0f, 1f)]
    [SerializeField] private float busProbability = 0.10f;

    [Range(0f, 1f)]
    [SerializeField] private float baGacProbability = 0.15f;


    //=========================================================
    // SPEED
    //=========================================================

    [Header("Traffic Speed")]
    [SerializeField] private float minimumTrafficSpeed = 7f;
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
        new List<GameObject>();

    private readonly List<TrafficCarBehavior> activeTraffic =
        new List<TrafficCarBehavior>();

    private float spawnZ;


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
    // START
    //=========================================================

    private void Start()
    {
        FindPlayer();

        FindCoinSpawner();

        if (playerTransform != null)
        {
            spawnZ =
                playerTransform.position.z -
                chunkLength;
        }
        else
        {
            spawnZ = 0f;
        }

        for (int i = 0; i < numberOfChunks; i++)
        {
            SpawnRoad();
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
                return;
        }

        if (activeRoads.Count == 0)
            return;

        float playerZ =
            playerTransform.position.z;

        while (
            playerZ >
            activeRoads[0].transform.position.z +
            chunkLength
        )
        {
            SpawnRoad();
            DeleteOldestRoad();
        }
    }


    //=========================================================
    // FIND PLAYER
    //=========================================================

    private void FindPlayer()
    {
        if (playerTransform != null)
            return;

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
            return;

        coinSpawner =
            FindFirstObjectByType<CoinSpawner>();

        if (coinSpawner == null)
        {
            Debug.LogWarning(
                "[RoadManager] Không tìm thấy CoinSpawner. " +
                "Traffic vẫn spawn bình thường nhưng Coin sẽ không được spawn."
            );
        }
    }


    //=========================================================
    // SPAWN ROAD
    //=========================================================

    private void SpawnRoad()
    {
        if (roadPrefab == null)
        {
            Debug.LogError(
                "[RoadManager] Road Prefab chưa được gán."
            );

            return;
        }

        GameObject road =
            Instantiate(
                roadPrefab,
                new Vector3(
                    0f,
                    0f,
                    spawnZ
                ),
                Quaternion.identity
            );

        activeRoads.Add(road);

        SpawnTrafficForChunk(spawnZ);

        spawnZ += chunkLength;
    }


    //=========================================================
    // SPAWN TRAFFIC FOR CHUNK
    //=========================================================

    private void SpawnTrafficForChunk(
        float chunkStartZ
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
            Random.Range(
                min,
                max + 1
            );

        if (count <= 0)
            return;

        List<float> positions =
            GenerateTrafficPositions(
                chunkStartZ,
                count
            );

        for (
            int i = 0;
            i < positions.Count;
            i++
        )
        {
            SpawnOneTraffic(
                positions[i]
            );
        }
    }


    //=========================================================
    // GENERATE TRAFFIC POSITIONS
    //=========================================================

    private List<float> GenerateTrafficPositions(
        float chunkStartZ,
        int count
    )
    {
        List<float> result =
            new List<float>();

        float minZ =
            chunkStartZ +
            spawnEdgePadding;

        float maxZ =
            chunkStartZ +
            chunkLength -
            spawnEdgePadding;

        if (maxZ < minZ)
            return result;


        //=====================================================
        // 1 XE / CHUNK
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
                result.Add(candidate);
            }

            return result;
        }


        //=====================================================
        // NHIỀU XE
        //=====================================================

        int attempts =
            count * 100;

        while (
            result.Count < count &&
            attempts > 0
        )
        {
            attempts--;

            float candidate =
                Random.Range(
                    minZ,
                    maxZ
                );

            if (!IsSpawnPositionSafe(candidate))
                continue;

            bool localSafe = true;

            for (
                int i = 0;
                i < result.Count;
                i++
            )
            {
                if (
                    Mathf.Abs(
                        result[i] -
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
                continue;

            result.Add(candidate);
        }

        return result;
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
                continue;

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
        float z
    )
    {
        GameObject prefab =
            ChooseTrafficPrefab();

        if (prefab == null)
        {
            Debug.LogWarning(
                "[RoadManager] Không có Traffic Prefab."
            );

            return;
        }

        int selectedLane =
            SelectSpawnLane(z);

        if (selectedLane < 0)
        {
            Debug.LogWarning(
                "[RoadManager] Không tìm được lane an toàn tại Z = " +
                z
            );

            return;
        }

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
                Quaternion.identity
            );

        if (traffic == null)
            return;


        //=====================================================
        // TRAFFIC VEHICLE
        //=====================================================

        TrafficVehicle vehicle =
            traffic.GetComponent<TrafficVehicle>();

        if (vehicle != null)
        {
            float speed =
                Random.Range(
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
                " thiếu TrafficCarBehavior."
            );
        }


        //=====================================================
        // COIN
        //=====================================================

        /*
         * RoadManager KHÔNG còn tự spawn Coin.
         *
         * Chỉ gửi thông tin:
         * - lane traffic đang chiếm
         * - Z của traffic
         *
         * CoinSpawner tự quyết định:
         * - lane coin
         * - số lượng coin
         * - khoảng cách
         * - độ cao
         * - pattern
         */

        if (coinSpawner != null)
{
    coinSpawner.SpawnCoins(
        z,
        selectedLane,
        chunkLength
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
                "[RoadManager] SPAWN | " +
                traffic.name +
                " | Lane " +
                selectedLane +
                " | X " +
                x +
                " | Z " +
                z
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
        // ƯU TIÊN LANE THEO VÒNG
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
        // FALLBACK
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
        List<int> lanes =
            new List<int>
            {
                0,
                1,
                2
            };

        Shuffle(lanes);

        for (
            int i = 0;
            i < lanes.Count;
            i++
        )
        {
            if (
                IsLaneSpawnSafe(
                    lanes[i],
                    z
                )
            )
            {
                return lanes[i];
            }
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
        if (lane < 0 || lane > 2)
            return false;

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
                continue;

            float xDistance =
                Mathf.Abs(
                    traffic.transform.position.x -
                    laneX
                );

            if (xDistance > 2.5f)
                continue;

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
            busProbability +
            baGacProbability;

        if (total <= 0f)
            return carPrefab;

        float roll =
            Random.value * total;


        //=====================================================
        // CAR
        //=====================================================

        if (
            roll <
            carProbability
        )
        {
            return carPrefab;
        }

        roll -=
            carProbability;


        //=====================================================
        // MOTORCYCLE
        //=====================================================

        if (
            roll <
            motorcycleProbability
        )
        {
            return motorcyclePrefab;
        }

        roll -=
            motorcycleProbability;


        //=====================================================
        // BUS
        //=====================================================

        if (
            roll <
            busProbability
        )
        {
            return busPrefab;
        }


        //=====================================================
        // BA GÁC
        //=====================================================

        return baGacPrefab;
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
            return;

        GameObject oldest =
            activeRoads[0];

        activeRoads.RemoveAt(0);

        if (oldest != null)
        {
            Destroy(oldest);
        }
    }


    //=========================================================
    // SHUFFLE
    //=========================================================

    private void Shuffle(
        List<int> list
    )
    {
        for (
            int i = 0;
            i < list.Count;
            i++
        )
        {
            int index =
                Random.Range(
                    i,
                    list.Count
                );

            int temp =
                list[i];

            list[i] =
                list[index];

            list[index] =
                temp;
        }
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
}