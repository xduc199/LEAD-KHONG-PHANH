using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhotonSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private GameObject photonItemPrefab; // Kéo Prefab Photon Item vào đây

    [Header("Low Spawn Rate Settings (Tỉ lệ thấp)")]
    [SerializeField] private float minSpawnInterval = 25f; // Thời gian tối thiểu giữa các lần xuất hiện (tăng lên để hiếm hơn)
    [SerializeField] private float maxSpawnInterval = 45f; // Thời gian tối đa giữa các lần xuất hiện (rất lâu mới ra 1 cái)
    [SerializeField] private float spawnDistanceAhead = 50f;// Khoảng cách phía trước mặt Player để xuất hiện
    [SerializeField] private float itemHeight = 0.5f;       // Độ cao Y của vật phẩm so với mặt đường

    private float nextSpawnTime;

    private void Start()
    {
        FindPlayerReference();
        ScheduleNextSpawn();
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            FindPlayerReference();
            return;
        }

        if (photonItemPrefab == null) return;

        // Kiểm tra thời gian để spawn vật phẩm Tốc Độ Ánh Sáng với tần suất thấp
        if (Time.time >= nextSpawnTime)
        {
            SpawnPhotonItem();
            ScheduleNextSpawn();
        }
    }

    private void FindPlayerReference()
    {
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }
    }

    private void ScheduleNextSpawn()
    {
        // Đặt khoảng thời gian ngẫu nhiên khá dài (từ 25 đến 45 giây) để item này trở nên hiếm và quý giá
        nextSpawnTime = Time.time + Random.Range(minSpawnInterval, maxSpawnInterval);
    }

    private void SpawnPhotonItem()
    {
        if (playerTransform == null || photonItemPrefab == null) return;

        // Chọn ngẫu nhiên một làn đường theo trục X (Ví dụ: -2.4, 0, hoặc 2.4)
        float[] lanes = { -2.4f, 0f, 2.4f };
        float randomX = lanes[Random.Range(0, lanes.Length)];

        // Vị trí xuất hiện ở phía trước mặt Player theo trục Z
        float spawnZ = playerTransform.position.z + spawnDistanceAhead;
        Vector3 spawnPos = new Vector3(randomX, playerTransform.position.y + itemHeight, spawnZ);

        // Sinh ra vật phẩm
        GameObject photonObj = Instantiate(photonItemPrefab, spawnPos, Quaternion.identity);

        // Đảm bảo vật phẩm có Tag là "PhotonItem" để Player nhận diện khi va chạm
        if (!photonObj.CompareTag("PhotonItem") && !photonObj.CompareTag("Photon"))
        {
            photonObj.tag = "PhotonItem";
        }

        Debug.Log("⚡ [HIẾM] Vật phẩm Tốc Độ Ánh Sáng đã xuất hiện trên đường!");
    }
}