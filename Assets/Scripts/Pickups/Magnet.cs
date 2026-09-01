using UnityEngine;

public class Magnet : MonoBehaviour
{
    [Header("Pickup Rotation")]
    [SerializeField] private float rotateSpeed = 120f;

    [Header("Pickup Height")]
    [SerializeField] private float height = 1.2f;

    [Header("Pickup Detection")]
    [SerializeField] private float pickupRadius = 1.5f;

    private Transform player;
    private bool collected;

    private void Start()
    {
        Vector3 position = transform.position;
        position.y = height;
        transform.position = position;

        FindPlayer();
    }

    private void Update()
    {
        if (collected)
            return;

        // Xoay vật phẩm
        transform.Rotate(
            0f,
            rotateSpeed * Time.deltaTime,
            0f,
            Space.World
        );

        if (player == null)
        {
            FindPlayer();
            return;
        }

        float distance = Vector3.Distance(
            transform.position,
            player.position
        );

        if (distance <= pickupRadius)
        {
            Collect();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected)
            return;

        if (!other.CompareTag("Player"))
            return;

        Collect();
    }

    private void Collect()
    {
        if (collected)
            return;

        FindPlayer();

        if (player == null)
        {
            Debug.LogWarning(
                "[Magnet] Không tìm thấy Player."
            );

            return;
        }

        MagnetController controller =
            player.GetComponent<MagnetController>();

        if (controller == null)
        {
            controller =
                player.GetComponentInChildren<MagnetController>();
        }

        if (controller == null)
        {
            controller =
                player.GetComponentInParent<MagnetController>();
        }

        if (controller == null)
        {
            Debug.LogError(
                "[Magnet] Player chưa có MagnetController!"
            );

            return;
        }

        collected = true;

        controller.Activate();

        Destroy(gameObject);
    }

    private void FindPlayer()
    {
        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    private void OnValidate()
    {
        if (rotateSpeed < 0f)
            rotateSpeed = 0f;

        if (height < 0.1f)
            height = 0.1f;

        if (pickupRadius < 0.2f)
            pickupRadius = 0.2f;
    }
}