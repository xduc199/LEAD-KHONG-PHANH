using UnityEngine;

public sealed class Magnet : MonoBehaviour
{
    //=============================================================
    // PICKUP ROTATION
    //=============================================================

    [Header("Pickup Rotation")]
    [SerializeField] private float rotateSpeed = 120f;


    //=============================================================
    // PICKUP HEIGHT
    //=============================================================

    [Header("Pickup Height")]
    [SerializeField] private float height = 1.2f;


    //=============================================================
    // PICKUP DETECTION
    //=============================================================

    [Header("Pickup Detection")]
    [SerializeField] private float pickupRadius = 1.5f;


    //=============================================================
    // RUNTIME
    //=============================================================

    private Transform player;
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


        //=========================================================
        // ROTATE
        //=========================================================

        if (rotateSpeed > 0f)
        {
            transform.Rotate(
                0f,
                rotateSpeed * Time.deltaTime,
                0f,
                Space.World
            );
        }


        //=========================================================
        // FIND PLAYER
        //=========================================================

        if (player == null)
        {
            FindPlayer();

            if (player == null)
                return;
        }


        //=========================================================
        // DISTANCE CHECK
        //=========================================================

        Vector3 delta =
            transform.position -
            player.position;

        float sqrDistance =
            delta.sqrMagnitude;

        float sqrPickupRadius =
            pickupRadius *
            pickupRadius;


        if (sqrDistance <= sqrPickupRadius)
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


        if (!other.CompareTag("Player"))
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


        //=========================================================
        // FIND PLAYER
        //=========================================================

        FindPlayer();


        if (player == null)
        {
            Debug.LogWarning(
                "[Magnet] Không tìm thấy Player."
            );

            return;
        }


        //=========================================================
        // FIND MAGNET CONTROLLER
        //=========================================================

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


        //=========================================================
        // CONTROLLER NOT FOUND
        //=========================================================

        if (controller == null)
        {
            Debug.LogError(
                "[Magnet] Player chưa có MagnetController!"
            );

            return;
        }


        //=========================================================
        // MARK COLLECTED
        //=========================================================

        collected = true;


        //=========================================================
        // ACTIVATE MAGNET
        //=========================================================

        controller.Activate();


        //=========================================================
        // DESTROY PICKUP
        //=========================================================

        Destroy(gameObject);
    }


    //=============================================================
    // FIND PLAYER
    //=============================================================

    private void FindPlayer()
    {
        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");


        if (playerObject != null)
        {
            player =
                playerObject.transform;
        }
    }


    //=============================================================
    // HEIGHT
    //=============================================================

    private void SetHeight()
    {
        Vector3 position =
            transform.position;

        position.y =
            height;

        transform.position =
            position;
    }


    //=============================================================
    // VALIDATE
    //=============================================================

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