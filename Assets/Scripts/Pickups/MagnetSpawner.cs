using UnityEngine;

public class MagnetSpawner : MonoBehaviour
{
    [Header("Magnet Settings")]
    [SerializeField] private GameObject magnetPrefab;

    [SerializeField] private float spawnInterval = 12f;

    [SerializeField] private float spawnDistanceAhead = 40f;

    [SerializeField] private float spawnHeight = 0.5f;

    [Header("Lane Settings")]
    [SerializeField] private float[] lanes = { -2f, 0f, 2f };

    [Header("Spawn Safety")]
    [SerializeField] private bool avoidSameLaneAsPlayer = false;

    private Transform playerTransform;
    private float timer;

    //=============================================================
    // START
    //=============================================================

    private void Start()
    {
        FindPlayer();

        timer = spawnInterval;
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

        if (timer <= 0f)
        {
            timer = spawnInterval;

            SpawnMagnet();
        }
    }

    //=============================================================
    // SPAWN MAGNET
    //=============================================================

    private void SpawnMagnet()
    {
        if (magnetPrefab == null)
            return;

        if (lanes == null || lanes.Length == 0)
            return;

        int laneIndex;

        if (avoidSameLaneAsPlayer)
        {
            laneIndex = GetSafeLaneIndex();
        }
        else
        {
            laneIndex = Random.Range(
                0,
                lanes.Length
            );
        }

        float spawnX = lanes[laneIndex];

        Vector3 spawnPosition = new Vector3(
            spawnX,
            spawnHeight,
            playerTransform.position.z + spawnDistanceAhead
        );

        Instantiate(
            magnetPrefab,
            spawnPosition,
            Quaternion.identity
        );
    }

    //=============================================================
    // SAFE LANE
    //=============================================================

    private int GetSafeLaneIndex()
    {
        float playerX = playerTransform.position.x;

        int nearestLane = 0;

        float nearestDistance = Mathf.Abs(
            playerX - lanes[0]
        );

        for (int i = 1; i < lanes.Length; i++)
        {
            float distance = Mathf.Abs(
                playerX - lanes[i]
            );

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestLane = i;
            }
        }

        if (lanes.Length <= 1)
            return nearestLane;

        int selectedLane = Random.Range(
            0,
            lanes.Length - 1
        );

        if (selectedLane >= nearestLane)
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
        GameObject playerObj =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
    }

    //=============================================================
    // VALIDATE
    //=============================================================

    private void OnValidate()
    {
        if (spawnInterval < 0.5f)
            spawnInterval = 0.5f;

        if (spawnDistanceAhead < 10f)
            spawnDistanceAhead = 10f;

        if (spawnHeight < 0f)
            spawnHeight = 0f;

        if (lanes == null || lanes.Length == 0)
        {
            lanes = new float[] { -2f, 0f, 2f };
        }
    }
}