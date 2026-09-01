using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhotonSpawner : MonoBehaviour
{
    //=============================================================
    // REFERENCES
    //=============================================================

    [Header("References")]

    [SerializeField]
    private Transform playerTransform;

    [SerializeField]
    private GameObject photonItemPrefab;


    //=============================================================
    // SPAWN SETTINGS
    //=============================================================

    [Header("Low Spawn Rate Settings")]

    [Tooltip("Thời gian tối thiểu giữa 2 lần spawn Photon.")]
    [SerializeField]
    private float minSpawnInterval = 25f;

    [Tooltip("Thời gian tối đa giữa 2 lần spawn Photon.")]
    [SerializeField]
    private float maxSpawnInterval = 45f;

    [Tooltip("Khoảng cách Photon xuất hiện phía trước Player.")]
    [SerializeField]
    private float spawnDistanceAhead = 50f;


    //=============================================================
    // HEIGHT SETTINGS
    //=============================================================

    [Header("Photon Height")]

    [Tooltip(
        "Độ cao của Photon so với mặt đường / vị trí Y của Player.\n" +
        "0 = ngang vị trí Y của Player.\n" +
        "0.5 = cao hơn 0.5.\n" +
        "1 = cao hơn 1."
    )]
    [SerializeField]
    private float itemHeight = 0.5f;


    //=============================================================
    // LANE SETTINGS
    //=============================================================

    [Header("Lane Settings")]

    [Tooltip("Các vị trí X mà Photon có thể xuất hiện.")]
    [SerializeField]
    private float[] lanes =
    {
        -2.4f,
        0f,
        2.4f
    };


    //=============================================================
    // INTERNAL
    //=============================================================

    private float nextSpawnTime;


    //=============================================================
    // START
    //=============================================================

    private void Start()
    {
        FindPlayerReference();

        ScheduleNextSpawn();
    }


    //=============================================================
    // UPDATE
    //=============================================================

    private void Update()
    {
        if (playerTransform == null)
        {
            FindPlayerReference();
            return;
        }

        if (photonItemPrefab == null)
            return;


        if (Time.time >= nextSpawnTime)
        {
            SpawnPhotonItem();

            ScheduleNextSpawn();
        }
    }


    //=============================================================
    // FIND PLAYER
    //=============================================================

    private void FindPlayerReference()
    {
        if (playerTransform != null)
            return;


        GameObject playerObj =
            GameObject.FindGameObjectWithTag("Player");


        if (playerObj != null)
        {
            playerTransform =
                playerObj.transform;
        }
    }


    //=============================================================
    // SCHEDULE NEXT SPAWN
    //=============================================================

    private void ScheduleNextSpawn()
    {
        float min =
            Mathf.Min(
                minSpawnInterval,
                maxSpawnInterval
            );

        float max =
            Mathf.Max(
                minSpawnInterval,
                maxSpawnInterval
            );


        nextSpawnTime =
            Time.time +
            Random.Range(
                min,
                max
            );
    }


    //=============================================================
    // SPAWN PHOTON
    //=============================================================

    private void SpawnPhotonItem()
    {
        if (
            playerTransform == null ||
            photonItemPrefab == null
        )
        {
            return;
        }


        //=========================================================
        // RANDOM LANE
        //=========================================================

        float randomX;


        if (
            lanes != null &&
            lanes.Length > 0
        )
        {
            randomX =
                lanes[
                    Random.Range(
                        0,
                        lanes.Length
                    )
                ];
        }
        else
        {
            randomX =
                playerTransform.position.x;
        }


        //=========================================================
        // SPAWN Z
        //=========================================================

        float spawnZ =
            playerTransform.position.z +
            spawnDistanceAhead;


        //=========================================================
        // SPAWN Y
        //=========================================================

        float spawnY =
            playerTransform.position.y +
            itemHeight;


        //=========================================================
        // FINAL POSITION
        //=========================================================

        Vector3 spawnPos =
            new Vector3(
                randomX,
                spawnY,
                spawnZ
            );


        //=========================================================
        // CREATE PHOTON
        //=========================================================

        GameObject photonObj =
            Instantiate(
                photonItemPrefab,
                spawnPos,
                Quaternion.identity
            );


        //=========================================================
        // TAG
        //=========================================================

        if (
            !photonObj.CompareTag("PhotonItem") &&
            !photonObj.CompareTag("Photon")
        )
        {
            photonObj.tag =
                "PhotonItem";
        }


        Debug.Log(
            "⚡ [HIẾM] Photon đã xuất hiện!"
        );
    }


    //=============================================================
    // GIZMOS
    //=============================================================

    private void OnDrawGizmosSelected()
    {
        if (playerTransform == null)
            return;


        float spawnZ =
            playerTransform.position.z +
            spawnDistanceAhead;


        float spawnY =
            playerTransform.position.y +
            itemHeight;


        Gizmos.DrawWireSphere(
            new Vector3(
                playerTransform.position.x,
                spawnY,
                spawnZ
            ),
            0.5f
        );
    }
}