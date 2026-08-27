using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target; // Kéo PlayerBike vào đây
    [SerializeField] private Vector3 offset = new Vector3(0f, 3.5f, -6f); // Khoảng cách từ Camera đến xe
    [SerializeField] private float smoothSpeed = 10f; // Tốc độ bám theo mượt mà

    private void LateUpdate()
    {
        if (target == null) return;

        // Tọa độ mục tiêu Camera cần di chuyển tới
        Vector3 targetPosition = target.position + offset;
        
        // Khóa vị trí X của Camera ở tâm (0) hoặc cho Lerp nhẹ theo làn xe
        targetPosition.x = Mathf.Lerp(transform.position.x, target.position.x * 0.3f, Time.deltaTime * smoothSpeed);

        // Mượt mà di chuyển Camera theo xe
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);
    }
}