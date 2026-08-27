using UnityEngine;

public class CoffeeSpawner : MonoBehaviour
{
    [Header("Coffee Settings")]
    [SerializeField] private GameObject coffeePrefab;     // Prefab ly cà phê sữa đá
    [SerializeField] private float spawnInterval = 6f;    // Thời gian giữa các lần xuất hiện
    [SerializeField] private float spawnDistanceAhead = 40f; // Khoảng cách sinh ra phía trước Player
    
    private float timer = 0f;
    private readonly float[] lanes = new float[] { -2.0f, 0.0f, 2.0f }; // Các làn đường

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnCoffee();
        }
    }

    private void SpawnCoffee()
    {
        if (coffeePrefab == null) return;

        // Tìm vị trí của Player để sinh vật phẩm phía trước mặt
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        // Chọn ngẫu nhiên một làn đường
        float randomLaneX = lanes[Random.Range(0, lanes.Length)];
        
        // Tính toán vị trí xuất hiện (Phía trước player theo trục Z)
        Vector3 spawnPos = new Vector3(randomLaneX, 0.5f, player.transform.position.z + spawnDistanceAhead);

        Instantiate(coffeePrefab, spawnPos, Quaternion.identity);
    }
}