using UnityEngine;

public class PhotonPowerUp : MonoBehaviour
{
    [SerializeField] private float rotateSpeed = 100f;

    private void Update()
    {
        // Xoay vật phẩm cho sinh động
        transform.Rotate(0, rotateSpeed * Time.deltaTime, 0, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Khi xe Player chạy qua nhặt
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.ActivatePhotonBurst(); // Kích hoạt trạng thái Tốc Độ Ánh Sáng
            }

            // Nhặt xong thì xóa vật phẩm đi
            Destroy(gameObject);
        }
    }
}