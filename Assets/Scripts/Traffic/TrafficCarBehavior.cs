using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrafficCarBehavior : MonoBehaviour
{
    private bool isPanicking = false;

    // Các mốc làn đường chuẩn trong game (Trái: -2f, Giữa: 0f, Phải: 2f)
    private readonly float[] lanes = new float[] { -2.0f, 0.0f, 2.0f };

    public void TriggerPanicLaneChange(Vector3 playerPos)
    {
        if (isPanicking) return;
        StartCoroutine(SmartPanicRoutine(playerPos));
    }

    private IEnumerator SmartPanicRoutine(Vector3 playerPos)
    {
        isPanicking = true;

        // 1. Phản ứng hoảng loạn có độ trễ ngẫu nhiên khác nhau cho từng xe
        yield return new WaitForSeconds(Random.Range(0.08f, 0.45f));

        float currentX = transform.position.x;
        float currentLane = GetNearestLane(currentX);

        // 2. Tỉ lệ ngẫu nhiên xe hoảng loạn lao về trước (Đã giảm tốc độ ở dưới)
        if (Random.value < 0.25f)
        {
            yield return StartCoroutine(RushForwardRoutine());
            isPanicking = false;
            yield break;
        }

        // Kiểm tra tình trạng trống/kẹt của các làn lân cận
        bool isLeftBlocked = IsLaneBlocked(-2.0f);
        bool isCenterBlocked = IsLaneBlocked(0.0f);
        bool isRightBlocked = IsLaneBlocked(2.0f);

        float targetX = currentX;

        // 3. Logic xử lý làn đường khi bị kẹt hoặc còi đe dọa
        if (Mathf.Abs(currentLane - 0.0f) < 0.5f) // Xe đang ở LÀN GIỮA
        {
            if (!isLeftBlocked && isRightBlocked)
            {
                targetX = -2.0f; // Né sang trái
            }
            else if (!isRightBlocked && isLeftBlocked)
            {
                targetX = 2.0f;  // Né sang phải
            }
            else if (!isLeftBlocked && !isRightBlocked)
            {
                targetX = (Random.value > 0.5f) ? -2.0f : 2.0f; // Thoáng cả 2 bên chọn ngẫu nhiên
            }
            else
            {
                // Kẹt cả 2 bên -> Hoảng loạn đâm thẳng tới trước
                yield return StartCoroutine(RushForwardRoutine());
                isPanicking = false;
                yield break;
            }
        }
        else if (currentLane < 0f) // Xe đang ở LÀN TRÁI
        {
            if (!isRightBlocked) targetX = 2.0f;
            else if (!isCenterBlocked) targetX = 0.0f;
            else
            {
                yield return StartCoroutine(RushForwardRoutine());
                isPanicking = false;
                yield break;
            }
        }
        else // Xe đang ở LÀN PHẢI
        {
            if (!isLeftBlocked) targetX = -2.0f;
            else if (!isCenterBlocked) targetX = 0.0f;
            else
            {
                yield return StartCoroutine(RushForwardRoutine());
                isPanicking = false;
                yield break;
            }
        }

        targetX = Mathf.Clamp(targetX, -4.5f, 4.5f);

        // 4. CHUYỂN LÀN CHẬM RÃI, MƯỢT MÀ HƠN (Tăng duration từ 0.35->0.7s, giảm tốc độ tiến phía trước)
        float elapsed = 0f;
        float duration = Random.Range(0.55f, 0.85f); // Kéo dài thời gian chuyển làn để dễ nhìn, dễ né
        float startX = transform.position.x;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            float newX = Mathf.Lerp(startX, targetX, smoothT);
            
            // Giảm tốc độ tiến Z lúc tạt lề xuống còn 5f - 9f (thay vì 15-22 như trước)
            transform.position = new Vector3(newX, transform.position.y, transform.position.z + (Random.Range(5f, 9f) * Time.deltaTime));

            yield return null;
        }

        isPanicking = false;
    }

    private IEnumerator RushForwardRoutine()
    {
        // GIẢM TỐC ĐỘ LAO TỚI KHI HOẢNG LOẠN (Còn 10f - 15f thay vì 28-38f để người chơi kịp phán đoán)
        float elapsed = 0f;
        float duration = Random.Range(0.5f, 0.9f);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position += transform.forward * (Random.Range(10f, 15f) * Time.deltaTime);
            yield return null;
        }
    }

    private float GetNearestLane(float posX)
    {
        float nearest = lanes[0];
        float minDiff = Mathf.Abs(posX - nearest);
        foreach (float lane in lanes)
        {
            float diff = Mathf.Abs(posX - lane);
            if (diff < minDiff)
            {
                minDiff = diff;
                nearest = lane;
            }
        }
        return nearest;
    }

    private bool IsLaneBlocked(float laneX)
    {
        Collider[] hits = Physics.OverlapSphere(new Vector3(laneX, transform.position.y, transform.position.z), 1.3f);
        foreach (var hit in hits)
        {
            if (hit.gameObject != gameObject && (hit.CompareTag("Obstacle") || hit.name.Contains("Car") || hit.name.Contains("BaGac")))
            {
                return true;
            }
        }
        return false;
    }
}