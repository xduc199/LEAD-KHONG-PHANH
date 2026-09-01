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


        rb =
            GetComponent<Rigidbody>();


        vehicleColliders =
            GetComponentsInChildren<Collider>(
                true
            );


        FindPlayer();

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
    // KNOCKBACK
    //=========================================================

    public void ApplyPhotonKnockback(
        Vector3 force
    )
    {
        if (isKnocked)
            return;


        if (rb == null)
        {
            rb =
                GetComponent<Rigidbody>();
        }


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
        // TẮT COLLIDER NGAY
        //=====================================================

        DisableVehicleColliders();


        //=====================================================
        // PHYSICS
        //=====================================================

        if (rb != null)
        {
            rb.isKinematic =
                false;


            rb.useGravity =
                true;


            rb.constraints =
                RigidbodyConstraints.None;


            rb.linearDamping =
                0.8f;


            rb.angularDamping =
                0.8f;


            rb.linearVelocity =
                Vector3.zero;


            rb.angularVelocity =
                Vector3.zero;


            rb.AddForce(
                force,
                ForceMode.Impulse
            );


            rb.AddTorque(
                Random.insideUnitSphere *
                spinForce,
                ForceMode.Impulse
            );
        }


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
//
// Khác Photon:
// Ambulance cần hất Traffic BAY CAO lên trời.
//
// Không chỉ AddForce.
// Hàm này ép velocity trực tiếp để:
// - Không phụ thuộc Rigidbody.mass
// - Không bị lực ngang lấn át
// - Y chắc chắn đủ lớn
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
    //
    // Đây là phần quan trọng.
    //
    // Nếu Inspector để trafficUpForce quá thấp,
    // Traffic vẫn sẽ không bay cao.
    //
    // Minimum 32.
    // Giá trị Inspector của bạn 38 vẫn được giữ.
    //=====================================================

    upwardY =
        Mathf.Max(
            upwardY,
            32f
        );


    //=====================================================
    // DIRECT VELOCITY
    //
    // Không phụ thuộc mass.
    //
    // Y = bay thẳng lên.
    // X = lệch trái/phải.
    // Z = bay về trước.
    //=====================================================

    rb.linearVelocity =
        new Vector3(
            horizontalX,
            upwardY,
            forwardZ
        );


    //=====================================================
    // ADD EXTRA UPWARD IMPULSE
    //
    // Tạo cảm giác cú tông mạnh.
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