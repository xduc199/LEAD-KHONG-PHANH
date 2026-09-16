using System.Collections;
using UnityEngine;

public class TrafficVehicle : MonoBehaviour
{
    //=========================================================
    // SPEED
    //=========================================================

    [Header("Speed")]
    [SerializeField] private float moveSpeed = 10f;

    [SerializeField] private float minimumSpeed = 5f;

    [Tooltip("Traffic không chạy nhanh hơn Player quá nhiều.")]
    [SerializeField] private float playerSpeedMargin = 1f;


    //=========================================================
    // MOVEMENT
    //=========================================================

    [Header("Movement")]
    [SerializeField] private bool moveForward = true;

    [Tooltip("Độ cao Y của Traffic Vehicle.")]
    [SerializeField] private float vehicleHeightY = 0f;


    //=========================================================
    // PLAYER SPEED
    //=========================================================

    private Transform playerTransform;

    private float estimatedPlayerSpeed = 15f;

    private float lastPlayerZ;

    private bool playerSpeedInitialized;


    //=========================================================
    // SPEED STATE
    //=========================================================

    private float baseSpeed;

    private bool temporarySpeedActive;

    private float temporarySpeed;


    //=========================================================
    // PHOTON / DEATH
    //=========================================================

    [Header("Vehicle Knockback")]

    [SerializeField]
    private float knockbackDuration = 2.5f;

    [SerializeField]
    private float destroyDelay = 0.25f;

    [SerializeField]
    private float spinForce = 18f;


    [Tooltip("Khi xe bị hất, tắt Collider ngay lập tức.")]
    [SerializeField]
    private bool disableCollidersWhenKnocked = true;


    //=========================================================
    // PHOTON KNOCKBACK SETTINGS
    //=========================================================

    [Header("Photon Knockback")]

    [Tooltip(
        "Vận tốc bay lên tối thiểu khi bị Photon hất."
    )]
    [SerializeField]
    private float photonUpwardVelocity = 20f;

    [Tooltip(
        "Vận tốc bay về phía trước tối thiểu khi bị Photon hất."
    )]
    [SerializeField]
    private float photonForwardVelocity = 12f;

    [Tooltip(
        "Giới hạn độ lệch ngang khi bị Photon hất."
    )]
    [SerializeField]
    private float photonSideVelocity = 4f;


    private bool isKnocked;

    private Rigidbody rb;

    private Collider[] vehicleColliders;

    private Coroutine knockbackRoutine;


    //=========================================================
    // PUBLIC
    //=========================================================

    public float MoveSpeed
    {
        get
        {
            return moveSpeed;
        }
    }


    public float CurrentSpeed
    {
        get
        {
            return moveSpeed;
        }
    }


    public float BaseSpeed
    {
        get
        {
            return baseSpeed;
        }
    }


    public int TravelDirection
    {
        get
        {
            return moveForward
                ? 1
                : -1;
        }
    }


    public bool IsKnockedByPhoton
    {
        get
        {
            return isKnocked;
        }
    }


    public bool IsDead
    {
        get
        {
            return isKnocked;
        }
    }


    //=========================================================
    // AWAKE
    //=========================================================

    private void Awake()
    {
        baseSpeed =
            Mathf.Max(
                minimumSpeed,
                moveSpeed
            );


        //=====================================================
        // VEHICLE HEIGHT
        //=====================================================

        Vector3 position =
            transform.position;

        position.y =
            vehicleHeightY;

        transform.position =
            position;


        //=====================================================
        // RIGIDBODY
        //=====================================================

        rb =
            GetComponent<Rigidbody>();


        //=====================================================
        // COLLIDERS
        //=====================================================

        vehicleColliders =
            GetComponentsInChildren<Collider>(
                true
            );


        //=====================================================
        // PLAYER
        //=====================================================

        FindPlayer();


        //=====================================================
        // PREPARE PHYSICS
        //=====================================================

        PrepareRigidbody();
    }


    //=========================================================
    // START
    //=========================================================

    private void Start()
    {
        FindPlayer();


        if (playerTransform != null)
        {
            lastPlayerZ =
                playerTransform.position.z;

            playerSpeedInitialized =
                true;
        }
    }


    //=========================================================
    // UPDATE
    //=========================================================

    private void Update()
    {
        if (isKnocked)
            return;


        if (playerTransform == null)
        {
            FindPlayer();
        }


        UpdatePlayerSpeedEstimate();

        UpdateCurrentSpeed();

        Move();
    }


    //=========================================================
    // RIGIDBODY
    //=========================================================

    private void PrepareRigidbody()
    {
        if (rb == null)
            return;


        rb.isKinematic =
            true;

        rb.useGravity =
            false;
    }


    //=========================================================
    // PLAYER
    //=========================================================

    private void FindPlayer()
    {
        if (playerTransform != null)
            return;


        GameObject player =
            GameObject.FindGameObjectWithTag(
                "Player"
            );


        if (player == null)
            return;


        playerTransform =
            player.transform;


        lastPlayerZ =
            playerTransform.position.z;


        playerSpeedInitialized =
            true;
    }


    //=========================================================
    // PLAYER SPEED ESTIMATION
    //=========================================================

    private void UpdatePlayerSpeedEstimate()
    {
        if (playerTransform == null)
            return;


        float currentZ =
            playerTransform.position.z;


        if (!playerSpeedInitialized)
        {
            lastPlayerZ =
                currentZ;

            playerSpeedInitialized =
                true;

            return;
        }


        float delta =
            currentZ -
            lastPlayerZ;


        float measuredSpeed =
            delta /
            Mathf.Max(
                Time.deltaTime,
                0.001f
            );


        if (
            measuredSpeed > 0f &&
            measuredSpeed < 60f
        )
        {
            estimatedPlayerSpeed =
                Mathf.Lerp(
                    estimatedPlayerSpeed,
                    measuredSpeed,
                    Time.deltaTime * 4f
                );
        }


        lastPlayerZ =
            currentZ;
    }


    //=========================================================
    // SPEED UPDATE
    //=========================================================

    private void UpdateCurrentSpeed()
    {
        float targetSpeed =
            baseSpeed;


        if (temporarySpeedActive)
        {
            targetSpeed =
                temporarySpeed;
        }


        if (playerTransform != null)
        {
            float maximumAllowed =
                Mathf.Max(
                    minimumSpeed,
                    estimatedPlayerSpeed -
                    playerSpeedMargin
                );


            targetSpeed =
                Mathf.Min(
                    targetSpeed,
                    maximumAllowed
                );
        }


        moveSpeed =
            Mathf.Max(
                minimumSpeed,
                targetSpeed
            );
    }


    //=========================================================
    // MOVE
    //=========================================================

    private void Move()
    {
        float direction =
            moveForward
                ? 1f
                : -1f;


        transform.position +=
            Vector3.forward *
            direction *
            moveSpeed *
            Time.deltaTime;
    }


    //=========================================================
    // SET BASE SPEED
    //=========================================================

    public void SetMoveSpeed(
        float speed
    )
    {
        baseSpeed =
            Mathf.Max(
                minimumSpeed,
                speed
            );


        if (!temporarySpeedActive)
        {
            moveSpeed =
                baseSpeed;
        }
    }


    //=========================================================
    // TEMPORARY SPEED
    //=========================================================

    public void SetTemporarySpeed(
        float speed
    )
    {
        temporarySpeed =
            Mathf.Max(
                minimumSpeed,
                speed
            );


        temporarySpeedActive =
            true;
    }


    //=========================================================
    // RESTORE SPEED
    //=========================================================

    public void RestoreBaseSpeed()
    {
        temporarySpeedActive =
            false;


        temporarySpeed =
            baseSpeed;


        moveSpeed =
            baseSpeed;
    }


    public float GetBaseSpeed()
    {
        return baseSpeed;
    }


    public float GetMoveSpeed()
    {
        return moveSpeed;
    }


    //=========================================================
    // DIRECTION
    //=========================================================

    public void SetTravelDirection(
        bool forward
    )
    {
        moveForward =
            forward;
    }


    public void SetTravelDirection(
        int direction
    )
    {
        moveForward =
            direction >= 0;
    }


    //=========================================================
    // PHOTON KNOCKBACK
    //=========================================================

    public void ApplyPhotonKnockback(
        Vector3 force
    )
    {
        if (isKnocked)
            return;


        //=====================================================
        // RIGIDBODY
        //=====================================================

        if (rb == null)
        {
            rb =
                GetComponent<Rigidbody>();
        }


        if (rb == null)
        {
            rb =
                gameObject.AddComponent<Rigidbody>();
        }


        //=====================================================
        // STATE
        //=====================================================

        isKnocked =
            true;


        //=====================================================
        // DỪNG TRAFFIC AI
        //=====================================================

        TrafficCarBehavior behavior =
            GetComponent<TrafficCarBehavior>();


        if (behavior != null)
        {
            behavior.enabled =
                false;
        }


        //=====================================================
        // TẮT COLLIDER
        //=====================================================

        DisableVehicleColliders();


        //=====================================================
        // PHYSICS MODE
        //=====================================================

        rb.isKinematic =
            false;

        rb.useGravity =
            true;

        rb.constraints =
            RigidbodyConstraints.None;

        rb.interpolation =
            RigidbodyInterpolation.Interpolate;

        rb.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;


        //=====================================================
        // DAMPING
        //=====================================================

        rb.linearDamping =
            0.35f;

        rb.angularDamping =
            0.45f;


        //=====================================================
        // RESET VELOCITY
        //=====================================================

        rb.linearVelocity =
            Vector3.zero;

        rb.angularVelocity =
            Vector3.zero;


        //=====================================================
        // READ PHOTON FORCE
        //=====================================================

        float sideVelocity =
            force.x;


        float forwardVelocity =
            Mathf.Abs(
                force.z
            );


        float upwardVelocity =
            Mathf.Abs(
                force.y
            );


        //=====================================================
        // FORCE MINIMUM
        //
        // Photon phải:
        // - bay cao
        // - bay về trước
        //
        // Không để giá trị từ PhotonController
        // quá nhỏ làm xe chỉ bật nhẹ.
        //=====================================================

        upwardVelocity =
            Mathf.Max(
                upwardVelocity,
                photonUpwardVelocity
            );


        forwardVelocity =
            Mathf.Max(
                forwardVelocity,
                photonForwardVelocity
            );


        sideVelocity =
            Mathf.Clamp(
                sideVelocity,
                -photonSideVelocity,
                photonSideVelocity
            );


        //=====================================================
        // DIRECT VELOCITY
        //
        // Đây là thay đổi quan trọng nhất.
        //
        // Không dùng AddForce làm lực chính nữa.
        //
        // Xe được cấp vận tốc ngay lập tức:
        //
        // X = lệch ngang
        // Y = bay lên
        // Z = bay phía trước
        //=====================================================

        rb.linearVelocity =
            new Vector3(
                sideVelocity,
                upwardVelocity,
                forwardVelocity
            );


        //=====================================================
        // EXTRA FORWARD IMPULSE
        //
        // Tạo cảm giác cú Photon tông mạnh hơn.
        //=====================================================

        rb.AddForce(
            Vector3.forward *
            forwardVelocity *
            0.25f,
            ForceMode.Impulse
        );


        //=====================================================
        // EXTRA UPWARD IMPULSE
        //
        // Giúp xe không chỉ trượt ngang
        // mà thực sự bị hất khỏi mặt đường.
        //=====================================================

        rb.AddForce(
            Vector3.up *
            upwardVelocity *
            0.15f,
            ForceMode.Impulse
        );


        //=====================================================
        // ROTATION
        //=====================================================

        Vector3 torque =
            new Vector3(
                Random.Range(
                    -spinForce,
                    spinForce
                ),
                Random.Range(
                    -spinForce,
                    spinForce
                ),
                Random.Range(
                    -spinForce,
                    spinForce
                )
            );


        rb.AddTorque(
            torque,
            ForceMode.Impulse
        );


        //=====================================================
        // ROUTINE
        //=====================================================

        if (knockbackRoutine != null)
        {
            StopCoroutine(
                knockbackRoutine
            );
        }


        knockbackRoutine =
            StartCoroutine(
                KnockbackRoutine()
            );
    }


    //=========================================================
    // AMBULANCE KNOCKBACK
    //=========================================================

    public void ApplyAmbulanceKnockback(
        Vector3 force,
        float linearDamping,
        float angularDamping,
        bool allowRotation,
        float torqueForce,
        float physicsLifetime
    )
    {
        if (isKnocked)
            return;


        //=====================================================
        // RIGIDBODY
        //=====================================================

        if (rb == null)
        {
            rb =
                GetComponent<Rigidbody>();
        }


        if (rb == null)
        {
            rb =
                gameObject.AddComponent<Rigidbody>();
        }


        //=====================================================
        // STATE
        //=====================================================

        isKnocked =
            true;


        //=====================================================
        // STOP TRAFFIC AI
        //=====================================================

        TrafficCarBehavior behavior =
            GetComponent<TrafficCarBehavior>();


        if (behavior != null)
        {
            behavior.enabled =
                false;
        }


        //=====================================================
        // STOP THIS SCRIPT
        //=====================================================

        enabled =
            false;


        //=====================================================
        // COLLIDER
        //=====================================================

        DisableVehicleColliders();


        //=====================================================
        // PHYSICS
        //=====================================================

        rb.isKinematic =
            false;

        rb.useGravity =
            true;

        rb.constraints =
            RigidbodyConstraints.None;

        rb.interpolation =
            RigidbodyInterpolation.Interpolate;

        rb.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;


        //=====================================================
        // DAMPING
        //=====================================================

        rb.linearDamping =
            Mathf.Max(
                0f,
                linearDamping
            );

        rb.angularDamping =
            Mathf.Max(
                0f,
                angularDamping
            );


        //=====================================================
        // RESET
        //=====================================================

        rb.linearVelocity =
            Vector3.zero;

        rb.angularVelocity =
            Vector3.zero;


        //=====================================================
        // FORCE VALUE
        //=====================================================

        float horizontalX =
            force.x;

        float upwardY =
            Mathf.Abs(
                force.y
            );

        float forwardZ =
            Mathf.Abs(
                force.z
            );


        //=====================================================
        // MINIMUM AIRBORNE FORCE
        //=====================================================

        upwardY =
            Mathf.Max(
                upwardY,
                32f
            );


        //=====================================================
        // DIRECT VELOCITY
        //=====================================================

        rb.linearVelocity =
            new Vector3(
                horizontalX,
                upwardY,
                forwardZ
            );


        //=====================================================
        // ADD EXTRA UPWARD IMPULSE
        //=====================================================

        rb.AddForce(
            Vector3.up *
            upwardY *
            0.35f,
            ForceMode.Impulse
        );


        //=====================================================
        // ROTATION
        //=====================================================

        rb.angularVelocity =
            Vector3.zero;


        if (allowRotation)
        {
            Vector3 torque =
                new Vector3(
                    Random.Range(
                        -torqueForce,
                        torqueForce
                    ),
                    Random.Range(
                        -torqueForce,
                        torqueForce
                    ),
                    Random.Range(
                        -torqueForce,
                        torqueForce
                    )
                );


            rb.AddTorque(
                torque,
                ForceMode.Impulse
            );
        }


        //=====================================================
        // CLEANUP
        //=====================================================

        LaunchCleanup cleanup =
            gameObject.GetComponent<LaunchCleanup>();


        if (cleanup == null)
        {
            cleanup =
                gameObject.AddComponent<LaunchCleanup>();
        }


        cleanup.Initialize(
            rb,
            physicsLifetime
        );
    }


    //=========================================================
    // DISABLE COLLIDERS
    //=========================================================

    private void DisableVehicleColliders()
    {
        if (!disableCollidersWhenKnocked)
            return;


        if (vehicleColliders == null)
            return;


        for (
            int i = 0;
            i < vehicleColliders.Length;
            i++
        )
        {
            if (
                vehicleColliders[i] != null
            )
            {
                vehicleColliders[i].enabled =
                    false;
            }
        }
    }


    //=========================================================
    // KNOCKBACK ROUTINE
    //=========================================================

    private IEnumerator KnockbackRoutine()
    {
        yield return new WaitForSeconds(
            knockbackDuration
        );


        if (destroyDelay > 0f)
        {
            yield return new WaitForSeconds(
                destroyDelay
            );
        }


        Destroy(
            gameObject
        );
    }


    //=========================================================
    // DISABLE
    //=========================================================

    private void OnDisable()
    {
        isKnocked =
            false;


        if (knockbackRoutine != null)
        {
            StopCoroutine(
                knockbackRoutine
            );


            knockbackRoutine =
                null;
        }
    }
}