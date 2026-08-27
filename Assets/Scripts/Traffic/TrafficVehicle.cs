using UnityEngine;

public class TrafficVehicle : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 8f; // Tốc độ di chuyển mặc định

    private void Update()
    {
        // Di chuyển thẳng theo hướng mặt Local của xe
        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime, Space.Self);
    }
}