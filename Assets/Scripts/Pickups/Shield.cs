using UnityEngine;

public class Shield : MonoBehaviour
{
    [Header("Pickup Rotation")]
    [SerializeField] private float rotateSpeed = 120f;

    [Header("Pickup Height")]
    [SerializeField] private float height = 1.2f;

    [Header("Pickup Detection")]
    [SerializeField] private float pickupRadius = 1.5f;

    private Transform player;
    private ShieldController playerShield;

    private bool collected;

    //=============================================================
    // START
    //=============================================================

    private void Start()
    {
        SetHeight();
        FindPlayer();
    }

    //=============================================================
    // UPDATE
    //=============================================================

    private void Update()
    {
        if (collected)
            return;

        if (rotateSpeed > 0f)
        {
            transform.Rotate(
                0f,
                rotateSpeed * Time.deltaTime,
                0f,
                Space.World
            );
        }

        if (player == null)
        {
            FindPlayer();
            return;
        }

        Vector3 delta =
            transform.position - player.position;

        if (delta.sqrMagnitude <= pickupRadius * pickupRadius)
        {
            Collect();
        }
    }

    //=============================================================
    // TRIGGER
    //=============================================================

    private void OnTriggerEnter(Collider other)
    {
        if (collected)
            return;

        PlayerController playerController =
            other.GetComponentInParent<PlayerController>();

        if (playerController == null)
            return;

        Collect();
    }

    //=============================================================
    // COLLECT
    //=============================================================

    private void Collect()
    {
        if (collected)
            return;

        FindPlayer();

        if (player == null)
        {
            Debug.LogWarning(
                "[Shield] Không tìm thấy Player."
            );

            return;
        }

        FindShieldController();

        if (playerShield == null)
        {
            Debug.LogError(
                "[Shield] Player chưa có ShieldController!",
                this
            );

            return;
        }

        collected = true;

        playerShield.Activate();

        Destroy(gameObject);
    }

    //=============================================================
    // FIND PLAYER
    //=============================================================

    private void FindPlayer()
    {
        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
            return;

        player =
            playerObject.transform;

        FindShieldController();
    }

    //=============================================================
    // FIND SHIELD CONTROLLER
    //=============================================================

    private void FindShieldController()
    {
        if (player == null)
            return;

        playerShield =
            player.GetComponent<ShieldController>();

        if (playerShield == null)
        {
            playerShield =
                player.GetComponentInChildren<ShieldController>(true);
        }

        if (playerShield == null)
        {
            playerShield =
                player.GetComponentInParent<ShieldController>();
        }
    }

    //=============================================================
    // HEIGHT
    //=============================================================

    private void SetHeight()
    {
        Vector3 position =
            transform.position;

        position.y = height;

        transform.position =
            position;
    }

    //=============================================================
    // VALIDATE
    //=============================================================

    private void OnValidate()
    {
        rotateSpeed =
            Mathf.Max(0f, rotateSpeed);

        height =
            Mathf.Max(0.1f, height);

        pickupRadius =
            Mathf.Max(0.2f, pickupRadius);
    }
}