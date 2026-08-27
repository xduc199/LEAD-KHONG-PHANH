using System.Collections.Generic;
using UnityEngine;

public class RoadManager : MonoBehaviour
{
    [Header("Road Settings")]
    [SerializeField] private GameObject roadPrefab;
    [SerializeField] private float chunkLength = 30f;
    [SerializeField] private int numberOfChunks = 5;

    [Header("Spawn Prefabs")]
    [SerializeField] private GameObject baGacRampPrefab;      // Xe ba gác cùng chiều (+Z)
    [SerializeField] private GameObject oncomingCarPrefab;    // Xe ngược chiều (-Z)
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private float laneDistance = 2.4f;

    [Header("Traffic Density & Ratios")]
    [SerializeField] [Range(0.5f, 1f)] private float spawnChance = 0.85f;     // Tỷ lệ xuất hiện xe mỗi vị trí
    [SerializeField] [Range(0.05f, 0.3f)] private float baGacRatio = 0.15f;   // 0.15 = Chỉ 15% xe sinh ra là Xe Ba Gác
    [SerializeField] private float baGacMinDistance = 70f;                   // Phải đi xa ít nhất 70m mới có Xe Ba Gác tiếp theo

    [Header("Safety & Separation Controls")]
    [SerializeField] private float minVehicleZGap = 15f;                     // Giãn cách xe ngược chiều bình thường
    [SerializeField] private float baGacSafeGap = 45f;                       // Giãn cách an toàn khóa làn sau khi sinh Ba Gác

    [Header("Player Tracking")]
    [SerializeField] private Transform playerTransform;

    private List<GameObject> activeRoads = new List<GameObject>();
    private float spawnZ = 0f;

    // Quản lý thời gian/vị trí spawn theo làn
    private float[] lastLaneSpawnZ = new float[3] { -999f, -999f, -999f };
    private bool[] lastLaneWasBaGac = new bool[3] { false, false, false };
    private float lastGlobalBaGacZ = -999f;

    private void Start()
    {
        for (int i = 0; i < numberOfChunks; i++)
        {
            SpawnRoad(i >= 1);
        }
    }

    private void Update()
    {
        if (playerTransform != null && playerTransform.position.z > (activeRoads[0].transform.position.z + chunkLength))
        {
            SpawnRoad(true);
            DeleteRoad();
        }
    }

    private void SpawnRoad(bool allowSpawnObjects)
    {
        GameObject newRoad = Instantiate(roadPrefab, new Vector3(0, 0, spawnZ), Quaternion.identity);
        newRoad.transform.SetParent(transform);
        activeRoads.Add(newRoad);

        if (allowSpawnObjects)
        {
            // Chia mỗi đoạn 30m thành 2 điểm kiểm tra spawn (mốc +6m và +20m)
            float[] subOffsets = new float[] { 6f, 20f };

            foreach (float offsetZ in subOffsets)
            {
                float currentSubZ = spawnZ + offsetZ;

                if (Random.value <= spawnChance)
                {
                    TrySpawnTrafficRow(currentSubSubZ: currentSubZ);
                }
            }
        }

        spawnZ += chunkLength;
    }

    private void TrySpawnTrafficRow(float currentSubSubZ)
    {
        int trafficLane = Random.Range(0, 3); // 0: Trái, 1: Giữa, 2: Phải
        float trafficX = (trafficLane - 1) * laneDistance;

        // 1. Kiểm tra làn này vừa có Ba Gác hay không để tính khoảng cách an toàn
        float requiredGap = lastLaneWasBaGac[trafficLane] ? baGacSafeGap : minVehicleZGap;

        if (currentSubSubZ - lastLaneSpawnZ[trafficLane] < requiredGap)
        {
            return; // Chưa đủ khoảng cách an toàn -> Bỏ qua không spawn ở đợt này
        }

        // 2. Chọn loại xe (Xe Ba Gác hoặc Xe Ngược Chiều)
        GameObject selectedPrefab = ChooseTrafficPrefab(currentSubSubZ);

        if (selectedPrefab != null)
        {
            Vector3 spawnPos = new Vector3(trafficX, 0.8f, currentSubSubZ);
            bool isBaGac = (selectedPrefab == baGacRampPrefab);
            Quaternion spawnRot = isBaGac ? Quaternion.identity : Quaternion.Euler(0, 180f, 0);

            GameObject traffic = Instantiate(selectedPrefab, spawnPos, spawnRot);
            traffic.transform.SetParent(transform);

            // 3. Cập nhật trạng thái an toàn cho làn
            lastLaneSpawnZ[trafficLane] = currentSubSubZ;
            lastLaneWasBaGac[trafficLane] = isBaGac;

            if (isBaGac)
            {
                lastGlobalBaGacZ = currentSubSubZ;
            }

            // 4. Sinh đồng xu ở làn khác
            SpawnCoinsOnOtherLane(trafficLane, currentSubSubZ);
        }
    }

    private GameObject ChooseTrafficPrefab(float currentZ)
    {
        if (oncomingCarPrefab == null && baGacRampPrefab == null) return null;
        if (baGacRampPrefab == null) return oncomingCarPrefab;

        // Nếu vừa xuất hiện Xe Ba Gác gần đây (dưới baGacMinDistance) -> Ép buộc ra xe ngược chiều
        bool isBaGacOnCooldown = (currentZ - lastGlobalBaGacZ) < baGacMinDistance;

        if (!isBaGacOnCooldown && Random.value <= baGacRatio)
        {
            return baGacRampPrefab;
        }

        return oncomingCarPrefab;
    }

    private void SpawnCoinsOnOtherLane(int occupiedLane, float currentZ)
    {
        if (coinPrefab == null) return;

        int coinLane = Random.Range(0, 3);
        while (coinLane == occupiedLane)
        {
            coinLane = Random.Range(0, 3);
        }

        float coinX = (coinLane - 1) * laneDistance;
        for (int i = 0; i < 3; i++)
        {
            Vector3 coinPos = new Vector3(coinX, 0.8f, currentZ - 3f + (i * 2.5f));
            GameObject coin = Instantiate(coinPrefab, coinPos, Quaternion.identity);
            coin.transform.SetParent(transform);
        }
    }

    private void DeleteRoad()
    {
        Destroy(activeRoads[0]);
        activeRoads.RemoveAt(0);
    }
}