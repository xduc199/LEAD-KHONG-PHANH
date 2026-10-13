using UnityEngine;

public sealed class CoinSpawner : MonoBehaviour
{
    //=========================================================
    // NORMAL COIN SETTINGS
    //=========================================================

    [Header("Coin Prefab")]
    [SerializeField] private GameObject coinPrefab;

    [Header("Coin Runtime Root")]
    [Tooltip("Root riêng để chứa toàn bộ coin runtime. Không đặt root này dưới RoadChunk.")]
    [SerializeField] private Transform coinRuntimeRoot;

    [Header("Normal Coin Group")]
    [Min(1)]
    [SerializeField] private int coinsPerGroup = 3;

    [Min(0.1f)]
    [SerializeField] private float coinSpacing = 2.5f;

    [Min(0f)]
    [SerializeField] private float coinHeight = 0.8f;

    [Header("Normal Spawn")]
    [SerializeField] private bool randomLane = true;

    [SerializeField] private bool avoidSameLane = true;

    [Min(0f)]
    [SerializeField] private float edgePadding = 2f;


    //=========================================================
    // HEAVEN ROAD REFERENCE
    //=========================================================

    [Header("Heaven Road Reference")]
    [SerializeField] private Transform heavenRoadReference;

    [SerializeField] private float fallbackHeavenRoadY = 0f;


    //=========================================================
    // HEAVEN ROAD COINS
    //=========================================================

    [Header("Heaven Road Coin")]
    [Min(1)]
    [SerializeField] private int heavenCoinsPerRoad = 8;

    [Min(0.5f)]
    [SerializeField] private float heavenCoinSpacing = 8f;

    [Min(0f)]
    [SerializeField] private float heavenCoinHeightOffset = 1.2f;

    [Tooltip(
        "Y cộng thêm riêng cho Heaven Coin. " +
        "Có thể chỉnh âm hoặc dương để nâng/hạ coin so với Heaven Road."
    )]
    [SerializeField] private float heavenCoinExtraY = 0f;

    [SerializeField] private bool heavenRandomLane = true;

    [SerializeField] private bool heavenAvoidSameLane = true;

    [Min(0f)]
    [SerializeField] private float heavenStartPadding = 8f;

    [Min(0f)]
    [SerializeField] private float heavenEndPadding = 8f;


    //=========================================================
    // RAMP -> HEAVEN
    //=========================================================

    [Header("Ramp -> Heaven Coin")]
    [Min(1)]
    [SerializeField] private int heavenCoinsPerRamp = 10;

    [Min(0.5f)]
    [SerializeField] private float heavenRampCoinSpacing = 7f;

    [Min(0f)]
    [SerializeField] private float heavenRampStartOffset = 4f;

    [Min(0f)]
    [SerializeField] private float heavenRampCoinHeightOffset = 1.2f;

    [Tooltip(
        "Y cộng thêm riêng cho coin được tạo từ Ramp -> Heaven."
    )]
    [SerializeField] private float heavenRampCoinExtraY = 0f;

    [SerializeField] private bool heavenRampFollowPlayerLane = true;

    [Range(0, 2)]
    [SerializeField] private int heavenRampDefaultLane = 1;


    //=========================================================
    // DEBUG
    //=========================================================

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;


    //=========================================================
    // RUNTIME
    //=========================================================

    private int lastSpawnedLane = -1;
    private int lastHeavenLane = -1;


    //=========================================================
    // PUBLIC API
    //=========================================================

    public GameObject CoinPrefab => coinPrefab;

    public int CoinsPerGroup => coinsPerGroup;

    public int HeavenCoinsPerRoad => heavenCoinsPerRoad;

    public int HeavenCoinsPerRamp => heavenCoinsPerRamp;


    //=========================================================
    // NORMAL COINS
    //
    // GIỮ NGUYÊN CƠ CHẾ NORMAL ROAD
    //=========================================================

    public void SpawnCoins(
        float startZ,
        int lane,
        float chunkLength)
    {
        if (coinPrefab == null)
        {
            Debug.LogWarning(
                "[CoinSpawner] Coin Prefab chưa được gán.",
                this);

            return;
        }

        if (coinsPerGroup <= 0)
            return;

        if (chunkLength <= 0f)
            return;

        int selectedLane;

        if (randomLane)
        {
            selectedLane = GetRandomLane();
        }
        else
        {
            selectedLane = Mathf.Clamp(lane, 0, 2);
        }

        float laneX =
            GetLaneX(selectedLane);

        float availableStart =
            startZ + edgePadding;

        float availableEnd =
            startZ +
            chunkLength -
            edgePadding;

        float totalLength =
            (coinsPerGroup - 1) *
            coinSpacing;

        float maxStart =
            availableEnd -
            totalLength;

        if (maxStart < availableStart)
            return;

        float spawnStartZ =
            Random.Range(
                availableStart,
                maxStart);

        for (int i = 0; i < coinsPerGroup; i++)
        {
            float spawnZ =
                spawnStartZ +
                coinSpacing * i;

            Vector3 position =
                new Vector3(
                    laneX,
                    coinHeight,
                    spawnZ);

            SpawnOneCoin(position);
        }

        lastSpawnedLane =
            selectedLane;

        if (debugLogs)
        {
            Debug.Log(
                "[CoinSpawner] NORMAL COINS | " +
                "Lane = " +
                selectedLane +
                " | StartZ = " +
                spawnStartZ.ToString("F1"),
                this);
        }
    }


    //=========================================================
    // HEAVEN ROAD COINS
    //
    // ĐÂY LÀ HỆ THỐNG SPAWN COIN THEO CHUNK
    //
    // RoadManager / Heaven Road system chỉ cần gọi:
    //
    // SpawnHeavenCoins(
    //     roadStartZ,
    //     roadLength,
    //     roadY);
    //
    //=========================================================

    public void SpawnHeavenCoins(
        float roadStartZ,
        float roadLength,
        float roadY)
    {
        if (!CanSpawnHeavenCoins())
            return;

        if (roadLength <= 0f)
            return;

        //=====================================================
        // CHUNK BOUNDARY
        //=====================================================

        float startZ =
            roadStartZ +
            Mathf.Max(
                0f,
                heavenStartPadding);

        float endZ =
            roadStartZ +
            roadLength -
            Mathf.Max(
                0f,
                heavenEndPadding);

        if (endZ < startZ)
            return;

        //=====================================================
        // RANDOM LANE
        //=====================================================

        int lane =
            GetHeavenLane();

        float laneX =
            GetLaneX(lane);

        //=====================================================
        // HEAVEN COIN Y
        //
        // Công thức:
        //
        // Road Y
        // + Height Offset
        // + Extra Y chỉnh tay
        //
        // Extra Y có thể âm hoặc dương.
        //=====================================================

        float coinY =
            roadY +
            heavenCoinHeightOffset +
            heavenCoinExtraY;

        //=====================================================
        // TÍNH SỐ COIN THEO CHIỀU DÀI CHUNK
        //
        // Không còn cố định 10 coin.
        //=====================================================

        int spawnCount =
            CalculateSpawnCount(
                startZ,
                endZ,
                heavenCoinSpacing,
                heavenCoinsPerRoad);

        if (spawnCount <= 0)
            return;

        //=====================================================
        // SPAWN
        //=====================================================

        int spawnedCount = 0;

        for (int i = 0; i < spawnCount; i++)
        {
            float z =
                startZ +
                heavenCoinSpacing * i;

            if (z > endZ)
                break;

            Vector3 position =
                new Vector3(
                    laneX,
                    coinY,
                    z);

            SpawnOneCoin(position);

            spawnedCount++;
        }

        //=====================================================
        // DEBUG
        //=====================================================

        if (debugLogs)
        {
            Debug.Log(
                "[CoinSpawner] HEAVEN CHUNK COINS | " +
                "Count = " +
                spawnedCount +
                " | Lane = " +
                lane +
                " | LaneX = " +
                laneX.ToString("F2") +
                " | RoadStartZ = " +
                roadStartZ.ToString("F2") +
                " | RoadLength = " +
                roadLength.ToString("F2") +
                " | RoadY = " +
                roadY.ToString("F2") +
                " | HeightOffset = " +
                heavenCoinHeightOffset.ToString("F2") +
                " | ExtraY = " +
                heavenCoinExtraY.ToString("F2") +
                " | FinalCoinY = " +
                coinY.ToString("F2") +
                " | StartZ = " +
                startZ.ToString("F2") +
                " | EndZ = " +
                endZ.ToString("F2"),
                this);
        }
    }


    //=========================================================
    // RAMP -> HEAVEN
    //
    // Trail coin từ cuối ramp lên Heaven.
    //
    // Không ép X.
    // Coin bám theo vị trí thực tế của ramp.
    //=========================================================

    public void SpawnHeavenCoinsFromRamp(
        Vector3 rampEndPosition,
        Vector3 forward,
        int lane,
        float heavenWorldY)
    {
        if (!CanSpawnHeavenCoins())
        {
            Debug.LogWarning(
                "[CoinSpawner] HEAVEN RAMP COINS BLOCKED | " +
                "Coin Prefab missing.",
                this);

            return;
        }

        if (heavenCoinsPerRamp <= 0)
            return;

        //=====================================================
        // NORMALIZE FORWARD
        //=====================================================

        if (forward.sqrMagnitude <= 0.0001f)
        {
            forward =
                Vector3.forward;
        }
        else
        {
            forward.Normalize();
        }

        //=====================================================
        // SELECT LANE
        //
        // Chỉ giữ logic lane.
        // Không ép X.
        //=====================================================

        int selectedLane;

        if (heavenRampFollowPlayerLane)
        {
            selectedLane =
                Mathf.Clamp(
                    lane,
                    0,
                    2);
        }
        else
        {
            selectedLane =
                Mathf.Clamp(
                    heavenRampDefaultLane,
                    0,
                    2);
        }

        //=====================================================
        // HEAVEN RAMP Y
        //=====================================================

        float coinY =
            heavenWorldY +
            heavenRampCoinHeightOffset +
            heavenRampCoinExtraY;

        //=====================================================
        // FIRST COIN
        //=====================================================

        Vector3 firstPosition =
            rampEndPosition +
            forward *
            heavenRampStartOffset;

        firstPosition.y =
            coinY;

        //=====================================================
        // DEBUG
        //=====================================================

        if (debugLogs)
        {
            Debug.Log(
                "[CoinSpawner] HEAVEN RAMP START | " +
                "RampEnd = " +
                rampEndPosition.ToString("F2") +
                " | Forward = " +
                forward.ToString("F2") +
                " | Lane = " +
                selectedLane +
                " | HeavenWorldY = " +
                heavenWorldY.ToString("F2") +
                " | HeightOffset = " +
                heavenRampCoinHeightOffset.ToString("F2") +
                " | ExtraY = " +
                heavenRampCoinExtraY.ToString("F2") +
                " | FinalCoinY = " +
                coinY.ToString("F2") +
                " | First = " +
                firstPosition.ToString("F2"),
                this);
        }

        //=====================================================
        // SPAWN RAMP TRAIL
        //=====================================================

        for (
            int i = 0;
            i < heavenCoinsPerRamp;
            i++)
        {
            Vector3 position =
                firstPosition +
                forward *
                (heavenRampCoinSpacing * i);

            position.y =
                coinY;

            SpawnOneCoin(position);
        }

        //=====================================================
        // FINAL DEBUG
        //=====================================================

        if (debugLogs)
        {
            Vector3 lastPosition =
                firstPosition +
                forward *
                (
                    heavenRampCoinSpacing *
                    (heavenCoinsPerRamp - 1)
                );

            lastPosition.y =
                coinY;

            Debug.Log(
                "[CoinSpawner] RAMP -> HEAVEN TRAIL | " +
                "Count = " +
                heavenCoinsPerRamp +
                " | Lane = " +
                selectedLane +
                " | FinalCoinY = " +
                coinY.ToString("F2") +
                " | First = " +
                firstPosition.ToString("F2") +
                " | Last = " +
                lastPosition.ToString("F2"),
                this);
        }
    }


    //=========================================================
    // GET HEAVEN ROAD WORLD Y
    //=========================================================

    private float GetHeavenRoadWorldY()
    {
        if (heavenRoadReference != null)
        {
            return heavenRoadReference.position.y;
        }

        return fallbackHeavenRoadY;
    }


    //=========================================================
    // HEAVEN COIN LINE
    //=========================================================

    public void SpawnHeavenCoinLine(
        Vector3 startPosition,
        int lane,
        int count,
        float spacing)
    {
        float heavenRoadY =
            GetHeavenRoadWorldY();

        SpawnHeavenCoinLine(
            startPosition,
            lane,
            count,
            spacing,
            heavenRoadY);
    }


    public void SpawnHeavenCoinLine(
        Vector3 startPosition,
        int lane,
        int count,
        float spacing,
        float heavenWorldY)
    {
        if (coinPrefab == null)
            return;

        if (count <= 0)
            return;

        if (spacing <= 0f)
            spacing =
                heavenCoinSpacing;

        lane =
            Mathf.Clamp(
                lane,
                0,
                2);

        float laneX =
            GetLaneX(lane);

        float coinY =
            heavenWorldY +
            heavenCoinHeightOffset +
            heavenCoinExtraY;

        for (int i = 0; i < count; i++)
        {
            Vector3 position =
                startPosition;

            position.x =
                laneX;

            position.y =
                coinY;

            position.z +=
                spacing * i;

            SpawnOneCoin(position);
        }

        if (debugLogs)
        {
            Debug.Log(
                "[CoinSpawner] HEAVEN COIN LINE | " +
                "Count = " +
                count +
                " | Lane = " +
                lane +
                " | HeavenWorldY = " +
                heavenWorldY.ToString("F2") +
                " | HeightOffset = " +
                heavenCoinHeightOffset.ToString("F2") +
                " | ExtraY = " +
                heavenCoinExtraY.ToString("F2") +
                " | FinalCoinY = " +
                coinY.ToString("F2"),
                this);
        }
    }


    //=========================================================
    // GENERIC LINE
    //=========================================================

    public void SpawnCoinLine(
        Vector3 startPosition,
        int count,
        Vector3 direction)
    {
        if (coinPrefab == null)
            return;

        if (count <= 0)
            return;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction =
                Vector3.forward;
        }
        else
        {
            direction.Normalize();
        }

        for (int i = 0; i < count; i++)
        {
            Vector3 position =
                startPosition +
                direction *
                coinSpacing *
                i;

            SpawnOneCoin(position);
        }
    }


    //=========================================================
    // SPAWN ONE COIN
    //=========================================================

    public void SpawnOneCoin(Vector3 position)
    {
        if (coinPrefab == null)
            return;

        GameObject coin =
            Instantiate(
                coinPrefab,
                position,
                Quaternion.identity);

        if (coin == null)
            return;

        //=====================================================
        // PARENT
        //=====================================================

        if (coinRuntimeRoot != null)
        {
            coin.transform.SetParent(
                coinRuntimeRoot,
                true);
        }

        //=====================================================
        // FORCE WORLD POSITION
        //=====================================================

        coin.transform.SetPositionAndRotation(
            position,
            Quaternion.identity);

        //=====================================================
        // FORCE ACTIVE
        //=====================================================

        if (!coin.activeSelf)
        {
            coin.SetActive(true);
        }

        //=====================================================
        // FORCE RENDERERS
        //
        // Giữ lại để bảo vệ prefab hiện tại.
        //=====================================================

        Renderer[] renderers =
            coin.GetComponentsInChildren<Renderer>(
                true);

        for (
            int i = 0;
            i < renderers.Length;
            i++)
        {
            Renderer renderer =
                renderers[i];

            if (
                renderer != null &&
                !renderer.enabled)
            {
                renderer.enabled = true;
            }
        }

        //=====================================================
        // DEBUG
        //=====================================================

        if (debugLogs)
        {
            Debug.Log(
                "[CoinSpawner] COIN SPAWNED | " +
                "WorldPosition = " +
                coin.transform.position.ToString("F2") +
                " | ActiveSelf = " +
                coin.activeSelf +
                " | ActiveHierarchy = " +
                coin.activeInHierarchy +
                " | Layer = " +
                LayerMask.LayerToName(
                    coin.layer) +
                " | Renderers = " +
                renderers.Length,
                coin);
        }
    }


    //=========================================================
    // RESET
    //=========================================================

    public void ResetCoinsForRevive()
    {
        if (coinRuntimeRoot != null)
        {
            for (
                int i =
                    coinRuntimeRoot.childCount - 1;
                i >= 0;
                i--)
            {
                Transform child =
                    coinRuntimeRoot.GetChild(i);

                if (child != null)
                {
                    Destroy(
                        child.gameObject);
                }
            }
        }

        lastSpawnedLane = -1;
        lastHeavenLane = -1;
    }


    //=========================================================
    // VALIDATION
    //=========================================================

    private bool CanSpawnHeavenCoins()
    {
        if (coinPrefab == null)
        {
            Debug.LogWarning(
                "[CoinSpawner] HEAVEN COINS FAILED | " +
                "Coin Prefab chưa được gán.",
                this);

            return false;
        }

        return true;
    }


    //=========================================================
    // HEAVEN LANE
    //=========================================================

    private int GetHeavenLane()
    {
        if (!heavenRandomLane)
        {
            lastHeavenLane = 1;
            return 1;
        }

        int lane =
            Random.Range(
                0,
                3);

        if (
            heavenAvoidSameLane &&
            lastHeavenLane >= 0 &&
            lane == lastHeavenLane)
        {
            lane++;

            if (lane >= 3)
                lane = 0;
        }

        lastHeavenLane =
            lane;

        return lane;
    }


    //=========================================================
    // SPAWN COUNT
    //=========================================================

    private static int CalculateSpawnCount(
        float startZ,
        float endZ,
        float spacing,
        int maximumCount)
    {
        if (maximumCount <= 0)
            return 0;

        if (endZ < startZ)
            return 0;

        if (spacing <= 0f)
            return 1;

        float availableLength =
            endZ -
            startZ;

        int count =
            Mathf.FloorToInt(
                availableLength /
                spacing) +
            1;

        return Mathf.Clamp(
            count,
            1,
            maximumCount);
    }


    //=========================================================
    // LANE
    //=========================================================

    private float GetLaneX(int lane)
    {
        switch (lane)
        {
            case 0:
                return -6.5f;

            case 1:
                return 0f;

            case 2:
                return 6.5f;

            default:
                return 0f;
        }
    }


    //=========================================================
    // RANDOM LANE
    //=========================================================

    private int GetRandomLane()
    {
        int lane =
            Random.Range(
                0,
                3);

        if (
            avoidSameLane &&
            lastSpawnedLane >= 0 &&
            lane == lastSpawnedLane)
        {
            lane++;

            if (lane >= 3)
                lane = 0;
        }

        return lane;
    }


    //=========================================================
    // VALIDATION
    //=========================================================

    private void OnValidate()
    {
        if (coinsPerGroup < 1)
            coinsPerGroup = 1;

        if (coinSpacing < 0.1f)
            coinSpacing = 0.1f;

        if (coinHeight < 0f)
            coinHeight = 0f;

        if (edgePadding < 0f)
            edgePadding = 0f;

        if (heavenCoinsPerRoad < 1)
            heavenCoinsPerRoad = 1;

        if (heavenCoinSpacing < 0.5f)
            heavenCoinSpacing = 0.5f;

        if (heavenCoinHeightOffset < 0f)
            heavenCoinHeightOffset = 0f;

        if (heavenStartPadding < 0f)
            heavenStartPadding = 0f;

        if (heavenEndPadding < 0f)
            heavenEndPadding = 0f;

        if (heavenCoinsPerRamp < 1)
            heavenCoinsPerRamp = 1;

        if (heavenRampCoinSpacing < 0.5f)
            heavenRampCoinSpacing = 0.5f;

        if (heavenRampStartOffset < 0f)
            heavenRampStartOffset = 0f;

        if (heavenRampCoinHeightOffset < 0f)
            heavenRampCoinHeightOffset = 0f;

        if (fallbackHeavenRoadY < -1000f)
            fallbackHeavenRoadY = -1000f;

        heavenRampDefaultLane =
            Mathf.Clamp(
                heavenRampDefaultLane,
                0,
                2);
    }
}