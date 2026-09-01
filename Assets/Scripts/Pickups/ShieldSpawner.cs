using UnityEngine;

public class ShieldSpawner : MonoBehaviour
{
    [Header("Shield Settings")]

    [SerializeField]
    private GameObject shieldPrefab;

    [SerializeField]
    private float spawnInterval = 18f;

    [SerializeField]
    private float spawnDistanceAhead = 40f;

    [SerializeField]
    private float spawnHeight = 1.2f;


    [Header("Lane Settings")]

    [SerializeField]
    private float[] lanes =
    {
        -2f,
        0f,
        2f
    };


    [Header("Spawn Safety")]

    [SerializeField]
    private bool avoidSameLaneAsPlayer = false;


    private Transform playerTransform;

    private float timer;


    //=============================================================
    // START
    //=============================================================

    private void Start()
    {
        FindPlayer();

        timer =
            spawnInterval;
    }


    //=============================================================
    // UPDATE
    //=============================================================

    private void Update()
    {
        if (playerTransform == null)
        {
            FindPlayer();

            return;
        }


        timer -= Time.deltaTime;


        if (timer > 0f)
            return;


        timer =
            spawnInterval;

        SpawnShield();
    }


    //=============================================================
    // SPAWN
    //=============================================================

    private void SpawnShield()
    {
        if (shieldPrefab == null)
            return;

        if (
            lanes == null ||
            lanes.Length == 0
        )
        {
            return;
        }


        int laneIndex;


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


        Vector3 spawnPosition =
            new Vector3(
                lanes[laneIndex],
                spawnHeight,
                playerTransform.position.z +
                spawnDistanceAhead
            );


        Instantiate(
            shieldPrefab,
            spawnPosition,
            Quaternion.identity
        );
    }


    //=============================================================
    // SAFE LANE
    //=============================================================

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


            if (distance < nearestDistance)
            {
                nearestDistance =
                    distance;

                nearestLane =
                    i;
            }
        }


        int selectedLane =
            Random.Range(
                0,
                lanes.Length - 1
            );


        if (
            selectedLane >=
            nearestLane
        )
        {
            selectedLane++;
        }


        return selectedLane;
    }


    //=============================================================
    // FIND PLAYER
    //=============================================================

    private void FindPlayer()
    {
        GameObject playerObject =
            GameObject.FindGameObjectWithTag(
                "Player"
            );


        if (playerObject != null)
        {
            playerTransform =
                playerObject.transform;
        }
    }


    //=============================================================
    // VALIDATE
    //=============================================================

    private void OnValidate()
    {
        spawnInterval =
            Mathf.Max(
                0.5f,
                spawnInterval
            );

        spawnDistanceAhead =
            Mathf.Max(
                10f,
                spawnDistanceAhead
            );

        spawnHeight =
            Mathf.Max(
                0.1f,
                spawnHeight
            );


        if (
            lanes == null ||
            lanes.Length == 0
        )
        {
            lanes =
                new float[]
                {
                    -2f,
                    0f,
                    2f
                };
        }
    }
}