using UnityEngine;

public class CoinSpawner : MonoBehaviour
{
    //=========================================================
    // COIN PREFAB
    //=========================================================

    [Header("Coin Prefab")]

    [SerializeField]
    private GameObject coinPrefab;


    //=========================================================
    // COIN PATTERN
    //=========================================================

    [Header("Coin Pattern")]

    [Tooltip("Số coin spawn cho mỗi nhóm.")]
    [SerializeField]
    private int coinsPerGroup = 3;

    [Tooltip("Khoảng cách giữa các coin theo trục Z.")]
    [SerializeField]
    private float coinSpacing = 2.5f;

    [Tooltip("Độ cao của coin.")]
    [SerializeField]
    private float coinHeight = 0.8f;


    //=========================================================
    // RANDOM LANE
    //=========================================================

    [Header("Random Lane")]

    [Tooltip(
        "Nếu bật, mỗi nhóm coin sẽ tự chọn ngẫu nhiên " +
        "lane trái / giữa / phải."
    )]
    [SerializeField]
    private bool randomLane = true;


    [Tooltip(
        "Nếu bật, một nhóm coin sẽ không spawn cùng lane " +
        "với nhóm trước đó."
    )]
    [SerializeField]
    private bool avoidSameLane = true;


    //=========================================================
    // SAFETY
    //=========================================================

    [Header("Spawn Safety")]

    [Tooltip("Không spawn coin quá sát mép chunk.")]
    [SerializeField]
    private float edgePadding = 2f;


    //=========================================================
    // DEBUG
    //=========================================================

    [Header("Debug")]

    [SerializeField]
    private bool debugLogs = false;


    //=========================================================
    // INTERNAL
    //=========================================================

    private int lastSpawnedLane = -1;


    //=========================================================
    // PUBLIC
    //=========================================================

    public GameObject CoinPrefab
    {
        get
        {
            return coinPrefab;
        }
    }


    public int CoinsPerGroup
    {
        get
        {
            return coinsPerGroup;
        }
    }


    //=========================================================
    // SPAWN COINS
    //=========================================================

    /// <summary>
    /// Spawn một nhóm coin theo lane.
    ///
    /// Nếu randomLane = true:
    ///     lane truyền vào sẽ được bỏ qua
    ///     và CoinSpawner tự chọn lane ngẫu nhiên.
    ///
    /// Nếu randomLane = false:
    ///     sử dụng lane được truyền vào như hệ thống cũ.
    /// </summary>
    public void SpawnCoins(
        float startZ,
        int lane,
        float chunkLength
    )
    {
        if (coinPrefab == null)
        {
            Debug.LogWarning(
                "[CoinSpawner] Chưa gán Coin Prefab."
            );

            return;
        }


        if (coinsPerGroup <= 0)
            return;


        //=====================================================
        // RANDOM LANE
        //=====================================================

        if (randomLane)
        {
            lane =
                GetRandomLane();
        }
        else
        {
            lane =
                Mathf.Clamp(
                    lane,
                    0,
                    2
                );
        }


        float laneX =
            GetLaneX(lane);


        float availableStart =
            startZ +
            edgePadding;


        float availableEnd =
            startZ +
            chunkLength -
            edgePadding;


        float totalLength =
            (coinsPerGroup - 1) *
            coinSpacing;


        //=====================================================
        // TÍNH VỊ TRÍ BẮT ĐẦU
        //=====================================================

        float minStart =
            availableStart;


        float maxStart =
            availableEnd -
            totalLength;


        if (maxStart < minStart)
        {
            maxStart =
                minStart;
        }


        float spawnStartZ =
            Random.Range(
                minStart,
                maxStart
            );


        //=====================================================
        // SPAWN
        //=====================================================

        for (
            int i = 0;
            i < coinsPerGroup;
            i++
        )
        {
            float spawnZ =
                spawnStartZ +
                i * coinSpacing;


            SpawnOneCoin(
                new Vector3(
                    laneX,
                    coinHeight,
                    spawnZ
                )
            );
        }


        //=====================================================
        // SAVE LAST LANE
        //=====================================================

        lastSpawnedLane =
            lane;


        //=====================================================
        // DEBUG
        //=====================================================

        if (debugLogs)
        {
            Debug.Log(
                "[CoinSpawner] Spawned " +
                coinsPerGroup +
                " coins | Lane = " +
                lane +
                " | X = " +
                laneX
            );
        }
    }


    //=========================================================
    // GET RANDOM LANE
    //=========================================================

    private int GetRandomLane()
    {
        //=====================================================
        // KHÔNG CẦN TRÁNH LANE CŨ
        //=====================================================

        if (
            !avoidSameLane ||
            lastSpawnedLane < 0
        )
        {
            return Random.Range(
                0,
                3
            );
        }


        //=====================================================
        // CHỌN LANE KHÁC LANE TRƯỚC
        //=====================================================

        int newLane;


        do
        {
            newLane =
                Random.Range(
                    0,
                    3
                );

        }
        while (
            newLane ==
            lastSpawnedLane
        );


        return newLane;
    }


    //=========================================================
    // SPAWN SINGLE COIN
    //=========================================================

    private void SpawnOneCoin(
        Vector3 position
    )
    {
        if (coinPrefab == null)
            return;


        GameObject coin =
            Instantiate(
                coinPrefab,
                position,
                Quaternion.identity
            );


        coin.transform.SetParent(
            transform
        );
    }


    //=========================================================
    // SPAWN COINS AT EXACT POSITION
    //=========================================================

    /// <summary>
    /// API dùng khi RoadManager muốn spawn coin
    /// tại một vị trí cụ thể.
    ///
    /// Hàm này KHÔNG random lane,
    /// vì vị trí đã được truyền trực tiếp.
    /// </summary>
    public void SpawnCoinLine(
        Vector3 startPosition,
        int count,
        Vector3 direction
    )
    {
        if (coinPrefab == null)
        {
            Debug.LogWarning(
                "[CoinSpawner] Chưa gán Coin Prefab."
            );

            return;
        }


        if (count <= 0)
            return;


        if (
            direction.sqrMagnitude <=
            0.001f
        )
        {
            direction =
                Vector3.forward;
        }


        direction.Normalize();


        for (
            int i = 0;
            i < count;
            i++
        )
        {
            Vector3 position =
                startPosition +
                direction *
                coinSpacing *
                i;


            SpawnOneCoin(
                position
            );
        }
    }


    //=========================================================
    // LANE
    //=========================================================

    private float GetLaneX(
        int lane
    )
    {
        switch (lane)
        {
            case 0:

                return -5f;


            case 1:

                return 0f;


            case 2:

                return 5f;


            default:

                return 0f;
        }
    }


    //=========================================================
    // VALIDATE
    //=========================================================

    private void OnValidate()
    {
        if (coinsPerGroup < 0)
            coinsPerGroup = 0;


        if (coinSpacing < 0.1f)
            coinSpacing = 0.1f;


        if (coinHeight < 0f)
            coinHeight = 0f;


        if (edgePadding < 0f)
            edgePadding = 0f;
    }
}