using UnityEngine;

public class Coin : MonoBehaviour
{
    [SerializeField] private float rotateSpeed = 120f;

    private void Awake()
    {
        // 1. Tự động thêm SphereCollider nếu chưa có
        SphereCollider col = GetComponent<SphereCollider>();
        if (col == null)
        {
            col = gameObject.AddComponent<SphereCollider>();
        }
        col.isTrigger = true; // Bắt buộc là Trigger
        col.radius = 1.2f;    // Mở rộng bán kính để dễ ăn

        // 2. Tự động thêm Rigidbody Kinematic để ép Unity tính va chạm
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void Update()
    {
        transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        // In ra Console để kiểm tra va chạm
        Debug.Log("Coin đã chạm vào: " + other.gameObject.name);

        // Bỏ qua nếu chạm vào mặt đường hoặc các coin khác
        if (other.name.Contains("Road") || other.name.Contains("Tile") || other.GetComponent<Coin>() != null)
        {
            return;
        }

        // Tăng coin nếu không phải xe giao thông
        if (!other.name.Contains("BaGac") && !other.name.Contains("Car"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddCoin(1);
            }
            Destroy(gameObject);
        }
    }
}