using UnityEngine;

public class GumSpawner : MonoBehaviour
{
    //=========================================================
    // GUM
    //=========================================================

    [Header("Gum Settings")]
    [SerializeField] private GameObject gumPrefab;

    [SerializeField] private float spawnInterval = 6f;

    [SerializeField] private float spawnDistanceAhead = 40f;

    [SerializeField] private float spawnHeight = 0.5f;

    //=========================================================
    // LANES
    //=========================================================

    [Header("Lane Settings")]
    [SerializeField] private float[] lanes =
    {
        -5f,
        0f,
        5f
    };

    //=========================================================
    // SPAWN SAFETY
    //=========================================================

    [Header("Spawn Safety")]
    [SerializeField] private bool avoidSameLaneAsPlayer = false;

    //=========================================================
    // INTERNAL
    //=========================================================

    private Transform playerTransform;

    private float timer;

    //=========================================================
    // START
    //=========================================================

    private void Start()
    {
        FindPlayer();

        timer =
            spawnInterval;
    }

    //=========================================================
    // UPDATE
    //=========================================================

    private void Update()
    {
        if (playerTransform == null)
        {
            FindPlayer();

            return;
        }

        timer -=
            Time.deltaTime;

        if (timer <= 0f)
        {
            timer =
                spawnInterval;

            SpawnGum();
        }
    }

    //=========================================================
    // SPAWN GUM
    //=========================================================

    private void SpawnGum()
    {
        if (gumPrefab == null)
        {
            Debug.LogWarning(
                "[GumSpawner] Gum Prefab chưa được gán."
            );

            return;
        }

        if (
            lanes == null ||
            lanes.Length == 0
        )
        {
            Debug.LogWarning(
                "[GumSpawner] Chưa có Lane."
            );

            return;
        }

        int laneIndex;

        //=====================================================
        // SELECT LANE
        //=====================================================

        if (avoidSameLaneAsPlayer)
        {
            laneIndex =
                GetSafeLaneIndex();
        }
        else
        {
            laneIndex =
                Random.Range(
                    0,
                    lanes.Length
                );
        }

        float spawnX =
            lanes[laneIndex];

        //=====================================================
        // POSITION
        //=====================================================

        Vector3 spawnPosition =
            new Vector3(
                spawnX,
                spawnHeight,
                playerTransform.position.z +
                spawnDistanceAhead
            );

        //=====================================================
        // SPAWN
        //=====================================================

        GameObject gum =
            Instantiate(
                gumPrefab,
                spawnPosition,
                Quaternion.identity
            );

        if (gum == null)
            return;

        if (gum.GetComponent<Gum>() == null)
        {
            Debug.LogWarning(
                "[GumSpawner] Gum Prefab thiếu component Gum."
            );
        }
    }

    //=========================================================
    // GET SAFE LANE
    //=========================================================

    private int GetSafeLaneIndex()
    {
        if (lanes.Length <= 1)
            return 0;

        float playerX =
            playerTransform.position.x;

        int nearestLane =
            0;

        float nearestDistance =
            Mathf.Abs(
                playerX -
                lanes[0]
            );

        for (
            int i = 1;
            i < lanes.Length;
            i++
        )
        {
            float distance =
                Mathf.Abs(
                    playerX -
                    lanes[i]
                );

            if (
                distance <
                nearestDistance
            )
            {
                nearestDistance =
                    distance;

                nearestLane =
                    i;
            }
        }

        //=====================================================
        // TẠO DANH SÁCH LANE KHÁC PLAYER
        //=====================================================

        int[] availableLanes =
            new int[lanes.Length - 1];

        int index = 0;

        for (
            int i = 0;
            i < lanes.Length;
            i++
        )
        {
            if (i == nearestLane)
                continue;

            availableLanes[index] =
                i;

            index++;
        }

        return availableLanes[
            Random.Range(
                0,
                availableLanes.Length
            )
        ];
    }

    //=========================================================
    // FIND PLAYER
    //=========================================================

    private void FindPlayer()
    {
        GameObject playerObj =
            GameObject.FindGameObjectWithTag(
                "Player"
            );

        if (playerObj != null)
        {
            playerTransform =
                playerObj.transform;
        }
    }

    //=========================================================
    // VALIDATE
    //=========================================================

    private void OnValidate()
    {
        if (spawnInterval < 0.5f)
            spawnInterval = 0.5f;

        if (spawnDistanceAhead < 10f)
            spawnDistanceAhead = 10f;

        if (spawnHeight < 0f)
            spawnHeight = 0f;

        if (
            lanes == null ||
            lanes.Length == 0
        )
        {
            lanes =
                new float[]
                {
                    -5f,
                    0f,
                    5f
                };
        }
    }
}