using System.Collections;
using UnityEngine;

public class TimeframeExplosion : MonoBehaviour
{
    [Tooltip("Thời gian hiển thị mỗi khung hình (giây). 0.03s = ~33 FPS")]
    [SerializeField] private float frameRate = 0.03f;

    private IEnumerator Start()
    {
        int totalFrames = transform.childCount;

        // 1. Ẩn tất cả các khung hình 3D con
        for (int i = 0; i < totalFrames; i++)
        {
            transform.GetChild(i).gameObject.SetActive(false);
        }

        // 2. Lần lượt bật từng khung hình 3D theo thời gian
        for (int i = 0; i < totalFrames; i++)
        {
            if (i > 0)
            {
                transform.GetChild(i - 1).gameObject.SetActive(false); // Tắt frame trước
            }

            transform.GetChild(i).gameObject.SetActive(true); // Bật frame hiện tại

            yield return new WaitForSeconds(frameRate);
        }

        // 3. Tự xóa Prefab nổ khỏi Scene khi chạy xong frame cuối
        Destroy(gameObject);
    }
}