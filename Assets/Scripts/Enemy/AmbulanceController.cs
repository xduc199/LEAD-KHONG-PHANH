using System.Collections;
using UnityEngine;

public class AmbulanceController : MonoBehaviour
{
    //=============================================================
    // MOVEMENT
    //=============================================================

    [Header("Movement")]

    [SerializeField]
    private float vehicleHeightOffset = 0.5f;

    [SerializeField]
    private float minSpeed = 28f;

    [SerializeField]
    private float maxSpeed = 42f;

    [SerializeField]
    private float accelerationSmoothTime = 0.25f;

    [SerializeField]
    private bool useFixedSpeed = false;

    [SerializeField]
    private float fixedSpeed = 35f;


    //=============================================================
    // LANE
    //=============================================================

    [Header("Lane")]

    [SerializeField]
    private float leftLaneX = -5f;

    [SerializeField]
    private float centerLaneX = 0f;

    [SerializeField]
    private float rightLaneX = 5f;

    [SerializeField]
    private float laneSnapSpeed = 15f;


    //=============================================================
    // LIFETIME
    //=============================================================

    [Header("Despawn")]

    [SerializeField]
    private float lifetime = 18f;

    [SerializeField]
    private float despawnBehindDistance = 45f;

    [SerializeField]
    private float despawnAheadDistance = 120f;


    //=============================================================
    // PLAYER COLLISION
    //=============================================================

    [Header("Player Collision")]

    [SerializeField]
    private float playerCollisionRadius = 2.8f;

    [SerializeField]
    private float playerCollisionForwardDistance = 3.8f;

    [SerializeField]
    private float collisionStartDelay = 0.25f;

    [SerializeField]
    private float playerHitCooldown = 1.0f;


    //=============================================================
    // PLAYER KNOCKBACK
    //=============================================================

    [Header("Player Knockback")]

    [SerializeField]
    private float playerUpForce = 32f;

    [SerializeField]
    private float playerForwardForce = 1.5f;

    [SerializeField]
    private bool forcePositivePlayerZ = true;


    //=============================================================
    // TRAFFIC COLLISION
    //=============================================================

    [Header("Traffic Collision")]

    [SerializeField]
    private float trafficCollisionRadius = 3.2f;

    [SerializeField]
    private float trafficCollisionForwardDistance = 7f;

    [SerializeField]
    private float trafficForwardForce = 22f;

    [SerializeField]
    private float trafficUpForce = 38f;

    [SerializeField]
    private float trafficSideForce = 7f;

    [SerializeField]
    private float trafficHitCooldown = 0.55f;

    [SerializeField]
    private float trafficMinForwardDistance = -1.5f;

    [SerializeField]
    private bool ignorePhotonTraffic = true;


    //=============================================================
    // TRAFFIC PHYSICS
    //=============================================================

    [Header("Traffic Launch Physics")]

    [SerializeField]
    private float launchedLinearDamping = 0.15f;

    [SerializeField]
    private float launchedAngularDamping = 2.5f;

    [SerializeField]
    private bool allowTrafficRotation = false;

    [SerializeField]
    private float trafficTorque = 1.5f;

    [SerializeField]
    private float trafficPhysicsLifetime = 3.5f;


    //=============================================================
    // AMBULANCE PHOTON LAUNCH
    //=============================================================

    [Header("Ambulance Photon Launch")]

    [SerializeField]
    private float photonAmbulanceUpForce = 28f;

    [SerializeField]
    private float photonAmbulanceForwardForce = 18f;

    [SerializeField]
    private float photonAmbulanceSideForce = 5f;

    [SerializeField]
    private float photonAmbulanceLinearDamping = 0.15f;

    [SerializeField]
    private float photonAmbulanceAngularDamping = 2.5f;

    [SerializeField]
    private bool allowPhotonAmbulanceRotation = true;

    [SerializeField]
    private float photonAmbulanceTorque = 2f;

    [SerializeField]
    private float photonAmbulancePhysicsLifetime = 4f;


    //=============================================================
    // ROTATION
    //=============================================================

    [Header("Rotation")]

    [SerializeField]
    private float rotationSpeed = 10f;

    [SerializeField]
    private float modelYRotationOffset = 0f;


    //=============================================================
    // AUDIO
    //=============================================================

    [Header("Ambulance Audio")]

    [SerializeField]
    private AudioClip ambulanceEngineClip;

    [Range(0f, 1f)]
    [SerializeField]
    private float engineVolume = 0.7f;

    [SerializeField]
    private bool engineLoop = true;

    [SerializeField]
    private float engineMinDistance = 5f;

    [SerializeField]
    private float engineMaxDistance = 60f;

    [SerializeField]
    private bool forceCreateEngineAudio = true;


    //=============================================================
    // EFFECT
    //=============================================================

    [Header("Effects")]

    [SerializeField]
    private GameObject collisionEffectPrefab;


    //=============================================================
    // INTERNAL
    //=============================================================

    private Transform playerTransform;

    private Rigidbody rb;

    private AudioSource engineAudioSource;

    private float spawnTime;

    private float currentSpeed;

    private float speedVelocity;

    private float laneX;

    private bool initialized;

    private float nextPlayerHitTime;

    private float nextTrafficHitTime;

    private TrafficVehicle lastHitTraffic;

    private float lastTrafficHitTime;


    //=============================================================
    // SHIELD COLLISION STATE
    //
    // KHÔNG DÙNG TIMER NỮA.
    //
    // Shield active:
    //     Ambulance <-> Player luôn IgnoreCollision.
    //
    // Shield bị vỡ:
    //     Vẫn IgnoreCollision nếu hai bên còn đang chồng nhau.
    //
    // Khi hai bên tách nhau:
    //     mới RestoreCollision.
    //=============================================================

    private Collider[] ambulanceColliders;

    private Collider[] ignoredPlayerColliders;

    private bool shieldCollisionBlocked;


    //=============================================================
    // PHOTON STATE
    //=============================================================

    private bool isPhotonLaunched;

    private float photonLaunchDestroyTime;


    //=============================================================
    // AWAKE
    //=============================================================

    private void Awake()
    {
        rb =
            GetComponent<Rigidbody>();

        if (rb == null)
        {
            rb =
                gameObject.AddComponent<Rigidbody>();
        }


        //=========================================================
        // CACHE COLLIDERS
        //=========================================================

        ambulanceColliders =
            GetComponentsInChildren<Collider>(
                true
            );


        //=========================================================
        // AMBULANCE KINEMATIC
        //=========================================================

        rb.isKinematic =
            true;

        rb.useGravity =
            false;

        rb.interpolation =
            RigidbodyInterpolation.Interpolate;

        rb.collisionDetectionMode =
            CollisionDetectionMode.ContinuousSpeculative;


        //=========================================================
        // SPEED
        //=========================================================

        currentSpeed =
            useFixedSpeed
                ? fixedSpeed
                : minSpeed;


        spawnTime =
            Time.time;


        //=========================================================
        // AUDIO
        //=========================================================

        SetupEngineAudio();


        initialized =
            true;
    }


    //=============================================================
    // INITIALIZE
    //=============================================================

    public void Initialize(
        int selectedLane,
        Transform player
    )
    {
        playerTransform =
            player;


        selectedLane =
            Mathf.Clamp(
                selectedLane,
                0,
                2
            );


        switch (selectedLane)
        {
            case 0:

                laneX =
                    leftLaneX;

                break;


            case 1:

                laneX =
                    centerLaneX;

                break;


            default:

                laneX =
                    rightLaneX;

                break;
        }


        //=========================================================
        // POSITION
        //=========================================================

        Vector3 position =
            transform.position;

        position.x =
            laneX;

        position.y =
            player != null
                ? player.position.y +
                  vehicleHeightOffset
                : position.y +
                  vehicleHeightOffset;

        transform.position =
            position;


        //=========================================================
        // RESET
        //=========================================================

        spawnTime =
            Time.time;

        currentSpeed =
            useFixedSpeed
                ? fixedSpeed
                : Random.Range(
                    minSpeed,
                    maxSpeed
                );

        speedVelocity =
            0f;

        nextPlayerHitTime =
            0f;

        nextTrafficHitTime =
            0f;

        lastHitTraffic =
            null;

        lastTrafficHitTime =
            -999f;

        isPhotonLaunched =
            false;

        photonLaunchDestroyTime =
            0f;

        shieldCollisionBlocked =
            false;

        ignoredPlayerColliders =
            null;


        //=========================================================
        // RESTORE OLD COLLISIONS
        //=========================================================

        RestorePlayerCollisions();


        //=========================================================
        // RESET RIGIDBODY
        //=========================================================

        if (rb != null)
        {
            rb.isKinematic =
                true;

            rb.useGravity =
                false;

            rb.linearVelocity =
                Vector3.zero;

            rb.angularVelocity =
                Vector3.zero;

            rb.collisionDetectionMode =
                CollisionDetectionMode.ContinuousSpeculative;
        }


        //=========================================================
        // ROTATION
        //=========================================================

        SetForwardRotation();


        //=========================================================
        // AUDIO
        //=========================================================

        RestartEngineAudio();
    }


    //=============================================================
    // SET PLAYER
    //=============================================================

    public void SetPlayer(
        Transform player
    )
    {
        playerTransform =
            player;
    }


    //=============================================================
    // UPDATE
    //=============================================================

    private void Update()
    {
        if (!initialized)
            return;


        //=========================================================
        // PHOTON LAUNCHED
        //=========================================================

        if (isPhotonLaunched)
        {
            if (
                Time.time >=
                photonLaunchDestroyTime
            )
            {
                Despawn();
            }

            return;
        }


        //=========================================================
        // SHIELD COLLISION PROTECTION
        //
        // ĐÂY LÀ PHẦN QUAN TRỌNG NHẤT.
        //
        // Không chờ tới lúc va chạm mới IgnoreCollision.
        //
        // Khi Shield đang active:
        //     IgnoreCollision ngay từ trước.
        //
        // Nhờ vậy OnCollisionEnter của Ambulance/Player
        // không có cơ hội xử lý cú đâm vật lý bình thường.
        //=========================================================

        UpdateShieldCollisionProtection();


        //=========================================================
        // LIFETIME
        //=========================================================

        if (
            Time.time -
            spawnTime >=
            lifetime
        )
        {
            Despawn();
            return;
        }


        //=========================================================
        // DESPAWN DISTANCE
        //=========================================================

        if (playerTransform != null)
        {
            float zDifference =
                transform.position.z -
                playerTransform.position.z;


            if (
                zDifference <
                -despawnBehindDistance
            )
            {
                Despawn();
                return;
            }


            if (
                zDifference >
                despawnAheadDistance
            )
            {
                Despawn();
                return;
            }
        }


        //=========================================================
        // SPEED
        //=========================================================

        if (useFixedSpeed)
        {
            currentSpeed =
                fixedSpeed;
        }
        else
        {
            currentSpeed =
                Mathf.SmoothDamp(
                    currentSpeed,
                    currentSpeed,
                    ref speedVelocity,
                    accelerationSmoothTime
                );

            currentSpeed =
                Mathf.Clamp(
                    currentSpeed,
                    minSpeed,
                    maxSpeed
                );
        }


        //=========================================================
        // COLLISION
        //=========================================================

        if (
            Time.time -
            spawnTime >=
            collisionStartDelay
        )
        {
            CheckPlayerCollision();

            CheckTrafficCollision();
        }


        //=========================================================
        // MOVE
        //=========================================================

        MoveAmbulance();
    }


    //=============================================================
    // SHIELD COLLISION PROTECTION
    //=============================================================

    private void UpdateShieldCollisionProtection()
    {
        if (playerTransform == null)
            return;


        PlayerController player =
            GetPlayerControllerFromTransform(
                playerTransform
            );


        if (player == null)
            return;


        ShieldController shield =
            FindPlayerShield(
                player
            );


        //=========================================================
        // SHIELD ĐANG ACTIVE
        //
        // Ignore collision ngay lập tức.
        //=========================================================

        if (
            shield != null &&
            shield.IsActive()
        )
        {
            IgnorePlayerCollisions(
                player
            );

            return;
        }


        //=========================================================
        // SHIELD ĐÃ VỠ
        //
        // Nếu hai bên vẫn còn chồng nhau thì tiếp tục Ignore.
        //
        // KHÔNG restore ngay.
        //=========================================================

        if (shieldCollisionBlocked)
        {
            if (
                !ArePlayerAndAmbulanceOverlapping(
                    player
                )
            )
            {
                RestorePlayerCollisions();

                shieldCollisionBlocked =
                    false;
            }
        }
    }


    //=============================================================
    // PLAYER COLLISION
    //=============================================================

    private void CheckPlayerCollision()
    {
        if (isPhotonLaunched)
            return;


        if (
            playerTransform == null ||
            Time.time <
            nextPlayerHitTime
        )
        {
            return;
        }


        Vector3 center =
            transform.position +
            Vector3.forward *
            (
                playerCollisionForwardDistance *
                0.35f
            );


        Collider[] hits =
            Physics.OverlapSphere(
                center,
                playerCollisionRadius,
                ~0,
                QueryTriggerInteraction.Collide
            );


        PlayerController player =
            null;


        //=========================================================
        // FIND PLAYER
        //=========================================================

        if (
            hits != null &&
            hits.Length > 0
        )
        {
            for (
                int i = 0;
                i < hits.Length;
                i++
            )
            {
                Collider hit =
                    hits[i];

                if (hit == null)
                    continue;


                player =
                    hit.GetComponentInParent<PlayerController>();


                if (player != null)
                    break;
            }
        }


        //=========================================================
        // FALLBACK
        //=========================================================

        if (player == null)
        {
            float zDistance =
                Mathf.Abs(
                    playerTransform.position.z -
                    transform.position.z
                );


            float xDistance =
                Mathf.Abs(
                    playerTransform.position.x -
                    transform.position.x
                );


            if (
                zDistance <=
                playerCollisionForwardDistance &&
                xDistance <=
                playerCollisionRadius
            )
            {
                player =
                    playerTransform.GetComponentInParent<PlayerController>();


                if (player == null)
                {
                    player =
                        playerTransform.GetComponent<PlayerController>();
                }
            }
        }


        if (player == null)
            return;


        HandlePlayerHit(
            player
        );


        //=========================================================
        // COOLDOWN
        //=========================================================

        if (!isPhotonLaunched)
        {
            nextPlayerHitTime =
                Time.time +
                playerHitCooldown;
        }
    }


    //=============================================================
    // HANDLE PLAYER HIT
    //=============================================================

    private void HandlePlayerHit(
        PlayerController player
    )
    {
        if (player == null)
            return;


        //=========================================================
        // FIND SHIELD
        //=========================================================

        ShieldController shield =
            FindPlayerShield(
                player
            );


        //=========================================================
        // SHIELD ACTIVE
        //
        // KẾT QUẢ MONG MUỐN:
        //
        // Shield:
        //     1 -> 0
        //     Vỡ.
        //
        // Player:
        //     KHÔNG chết.
        //     KHÔNG ApplyKnockback.
        //
        // Ambulance:
        //     KHÔNG bị launch.
        //     KHÔNG bị destroy.
        //     KHÔNG bị knockback.
        //
        // Collision:
        //     Ignore cho tới khi hai bên tách nhau.
        //=========================================================

        if (
            shield != null &&
            shield.IsActive()
        )
        {
            //=====================================================
            // IGNORE COLLISION TRƯỚC
            //=====================================================

            IgnorePlayerCollisions(
                player
            );


            //=====================================================
            // EFFECT
            //=====================================================

            SpawnCollisionEffect(
                GetImpactPosition(
                    player.transform
                )
            );


            //=====================================================
            // CONSUME SHIELD
            //=====================================================

            bool shieldBlocked =
                shield.ConsumeShield();


            if (shieldBlocked)
            {
                //=================================================
                // GIỮ IGNORE SAU KHI SHIELD VỠ
                //
                // Không dùng timer.
                //
                // Chỉ restore khi hai object thực sự tách nhau.
                //=================================================

                shieldCollisionBlocked =
                    true;


                nextPlayerHitTime =
                    Time.time +
                    Mathf.Max(
                        playerHitCooldown,
                        0.1f
                    );


                //=================================================
                // CỰC KỲ QUAN TRỌNG:
                //
                // KHÔNG:
                //
                // player.ApplyKnockback()
                //
                // KHÔNG:
                //
                // LaunchAmbulanceByPhoton()
                //
                // KHÔNG:
                //
                // Destroy(gameObject)
                //
                // KHÔNG:
                //
                // LaunchTraffic()
                //
                // Chỉ:
                //
                // Shield vỡ
                // Player sống
                // Ambulance tiếp tục chạy
                // Hai bên xuyên nhau
                //=================================================

                return;
            }
        }


        //=========================================================
        // PLAYER PHOTON
        //=========================================================

        if (
            IsPlayerPhotonActive(
                player
            )
        )
        {
            if (isPhotonLaunched)
                return;


            SpawnCollisionEffect(
                GetImpactPosition(
                    player.transform
                )
            );


            LaunchAmbulanceByPhoton(
                player.transform
            );


            return;
        }


        //=========================================================
        // NORMAL PLAYER HIT
        //=========================================================

        SpawnCollisionEffect(
            GetImpactPosition(
                player.transform
            )
        );


        //=========================================================
        // PLAYER BAY CAO
        //=========================================================

        Vector3 playerForce =
            new Vector3(
                0f,
                Mathf.Abs(
                    playerUpForce
                ),
                forcePositivePlayerZ
                    ? Mathf.Abs(
                        playerForwardForce
                    )
                    : 0f
            );


        player.ApplyKnockback(
            playerForce
        );
    }


    //=============================================================
    // FIND PLAYER SHIELD
    //=============================================================

    private ShieldController FindPlayerShield(
        PlayerController player
    )
    {
        if (player == null)
            return null;


        ShieldController shield =
            player.GetComponent<ShieldController>();


        if (shield == null)
        {
            shield =
                player.GetComponentInChildren<ShieldController>(
                    true
                );
        }


        if (shield == null)
        {
            shield =
                player.GetComponentInParent<ShieldController>();
        }


        return shield;
    }


    //=============================================================
    // GET PLAYER CONTROLLER
    //=============================================================

    private PlayerController GetPlayerControllerFromTransform(
        Transform target
    )
    {
        if (target == null)
            return null;


        PlayerController player =
            target.GetComponentInParent<PlayerController>();


        if (player == null)
        {
            player =
                target.GetComponent<PlayerController>();
        }


        if (player == null)
        {
            player =
                target.GetComponentInChildren<PlayerController>(
                    true
                );
        }


        return player;
    }


    //=============================================================
    // IGNORE PLAYER COLLISION
    //
    // KHÔNG CÓ TIMER.
    //
    // Gọi được nhiều lần an toàn.
    //=============================================================

    private void IgnorePlayerCollisions(
        PlayerController player
    )
    {
        if (player == null)
            return;


        Collider[] playerColliders =
            player.GetComponentsInChildren<Collider>(
                true
            );


        if (
            ambulanceColliders == null ||
            ambulanceColliders.Length == 0
        )
        {
            ambulanceColliders =
                GetComponentsInChildren<Collider>(
                    true
                );
        }


        if (
            playerColliders == null ||
            playerColliders.Length == 0
        )
        {
            return;
        }


        ignoredPlayerColliders =
            playerColliders;


        //=========================================================
        // IGNORE
        //=========================================================

        for (
            int i = 0;
            i < ambulanceColliders.Length;
            i++
        )
        {
            Collider ambulanceCollider =
                ambulanceColliders[i];


            if (ambulanceCollider == null)
                continue;


            for (
                int j = 0;
                j < playerColliders.Length;
                j++
            )
            {
                Collider playerCollider =
                    playerColliders[j];


                if (playerCollider == null)
                    continue;


                if (
                    ambulanceCollider ==
                    playerCollider
                )
                {
                    continue;
                }


                Physics.IgnoreCollision(
                    ambulanceCollider,
                    playerCollider,
                    true
                );
            }
        }


        shieldCollisionBlocked =
            true;


        //=========================================================
        // SYNC PHYSICS
        //
        // Đảm bảo trạng thái Ignore được Unity physics cập nhật
        // ngay sau khi thay đổi.
        //=========================================================

        Physics.SyncTransforms();
    }


    //=============================================================
    // CHECK OVERLAP
    //
    // Dùng để quyết định khi nào được RestoreCollision.
    //=============================================================

    private bool ArePlayerAndAmbulanceOverlapping(
        PlayerController player
    )
    {
        if (player == null)
            return false;


        if (
            ambulanceColliders == null ||
            ambulanceColliders.Length == 0
        )
        {
            ambulanceColliders =
                GetComponentsInChildren<Collider>(
                    true
                );
        }


        Collider[] playerColliders =
            player.GetComponentsInChildren<Collider>(
                true
            );


        if (
            ambulanceColliders == null ||
            playerColliders == null
        )
        {
            return false;
        }


        for (
            int i = 0;
            i < ambulanceColliders.Length;
            i++
        )
        {
            Collider ambulanceCollider =
                ambulanceColliders[i];


            if (ambulanceCollider == null)
                continue;


            if (!ambulanceCollider.enabled)
                continue;


            for (
                int j = 0;
                j < playerColliders.Length;
                j++
            )
            {
                Collider playerCollider =
                    playerColliders[j];


                if (playerCollider == null)
                    continue;


                if (!playerCollider.enabled)
                    continue;


                if (
                    ambulanceCollider ==
                    playerCollider
                )
                {
                    continue;
                }


                if (
                    ambulanceCollider.bounds.Intersects(
                        playerCollider.bounds
                    )
                )
                {
                    return true;
                }
            }
        }


        return false;
    }


    //=============================================================
    // RESTORE PLAYER COLLISIONS
    //=============================================================

    private void RestorePlayerCollisions()
    {
        if (
            ambulanceColliders == null ||
            ambulanceColliders.Length == 0
        )
        {
            return;
        }


        PlayerController player =
            GetPlayerControllerFromTransform(
                playerTransform
            );


        if (player == null)
            return;


        Collider[] playerColliders =
            player.GetComponentsInChildren<Collider>(
                true
            );


        if (
            playerColliders == null ||
            playerColliders.Length == 0
        )
        {
            return;
        }


        for (
            int i = 0;
            i < ambulanceColliders.Length;
            i++
        )
        {
            Collider ambulanceCollider =
                ambulanceColliders[i];


            if (ambulanceCollider == null)
                continue;


            for (
                int j = 0;
                j < playerColliders.Length;
                j++
            )
            {
                Collider playerCollider =
                    playerColliders[j];


                if (playerCollider == null)
                    continue;


                if (
                    ambulanceCollider ==
                    playerCollider
                )
                {
                    continue;
                }


                Physics.IgnoreCollision(
                    ambulanceCollider,
                    playerCollider,
                    false
                );
            }
        }


        ignoredPlayerColliders =
            null;


        shieldCollisionBlocked =
            false;


        Physics.SyncTransforms();
    }


    //=============================================================
    // PHOTON CHECK
    //=============================================================

    private bool IsPlayerPhotonActive(
        PlayerController player
    )
    {
        if (player == null)
            return false;


        PhotonStateReceiver receiver =
            player.GetComponent<PhotonStateReceiver>();


        if (
            receiver != null &&
            receiver.IsActive
        )
        {
            return true;
        }


        PhotonController photon =
            player.GetComponentInChildren<PhotonController>(
                true
            );


        if (
            photon != null &&
            photon.enabled
        )
        {
            return true;
        }


        return false;
    }


    //=============================================================
    // LAUNCH AMBULANCE BY PHOTON
    //=============================================================

    private void LaunchAmbulanceByPhoton(
        Transform player
    )
    {
        if (isPhotonLaunched)
            return;


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


        isPhotonLaunched =
            true;


        StopEngineAudio();


        float sideDirection =
            0f;


        if (player != null)
        {
            float side =
                transform.position.x -
                player.position.x;


            if (
                Mathf.Abs(side) >
                0.05f
            )
            {
                sideDirection =
                    Mathf.Sign(side);
            }
        }


        if (
            Mathf.Abs(sideDirection) <=
            0.01f
        )
        {
            sideDirection =
                Random.value > 0.5f
                    ? 1f
                    : -1f;
        }


        rb.isKinematic =
            false;

        rb.useGravity =
            true;

        rb.interpolation =
            RigidbodyInterpolation.Interpolate;

        rb.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;


        rb.linearDamping =
            Mathf.Max(
                0f,
                photonAmbulanceLinearDamping
            );


        rb.angularDamping =
            Mathf.Max(
                0f,
                photonAmbulanceAngularDamping
            );


        rb.linearVelocity =
            Vector3.zero;

        rb.angularVelocity =
            Vector3.zero;


        Vector3 launchForce =
            new Vector3(
                sideDirection *
                Mathf.Abs(
                    photonAmbulanceSideForce
                ),

                Mathf.Abs(
                    photonAmbulanceUpForce
                ),

                Mathf.Abs(
                    photonAmbulanceForwardForce
                )
            );


        rb.AddForce(
            launchForce,
            ForceMode.Impulse
        );


        rb.angularVelocity =
            Vector3.zero;


        if (allowPhotonAmbulanceRotation)
        {
            Vector3 torque =
                new Vector3(
                    Random.Range(
                        -photonAmbulanceTorque,
                        photonAmbulanceTorque
                    ),

                    Random.Range(
                        -photonAmbulanceTorque,
                        photonAmbulanceTorque
                    ),

                    Random.Range(
                        -photonAmbulanceTorque,
                        photonAmbulanceTorque
                    )
                );


            rb.AddTorque(
                torque,
                ForceMode.Impulse
            );
        }


        photonLaunchDestroyTime =
            Time.time +
            Mathf.Max(
                0.1f,
                photonAmbulancePhysicsLifetime
            );
    }


    //=============================================================
    // TRAFFIC COLLISION
    //=============================================================

    private void CheckTrafficCollision()
    {
        if (isPhotonLaunched)
            return;


        if (
            Time.time <
            nextTrafficHitTime
        )
        {
            return;
        }


        Vector3 start =
            transform.position;


        Vector3 end =
            transform.position +
            Vector3.forward *
            trafficCollisionForwardDistance;


        Collider[] hits =
            Physics.OverlapCapsule(
                start,
                end,
                trafficCollisionRadius,
                ~0,
                QueryTriggerInteraction.Collide
            );


        TrafficVehicle closestTraffic =
            null;


        float closestDistance =
            float.MaxValue;


        //=========================================================
        // PHYSICS SEARCH
        //=========================================================

        if (
            hits != null &&
            hits.Length > 0
        )
        {
            for (
                int i = 0;
                i < hits.Length;
                i++
            )
            {
                Collider hit =
                    hits[i];


                if (hit == null)
                    continue;


                TrafficVehicle traffic =
                    hit.GetComponentInParent<TrafficVehicle>();


                if (traffic == null)
                {
                    traffic =
                        hit.GetComponentInChildren<TrafficVehicle>(
                            true
                        );
                }


                if (traffic == null)
                    continue;


                if (
                    traffic.gameObject ==
                    gameObject
                )
                {
                    continue;
                }


                if (
                    ignorePhotonTraffic &&
                    traffic.IsKnockedByPhoton
                )
                {
                    continue;
                }


                if (
                    traffic ==
                    lastHitTraffic &&
                    Time.time -
                    lastTrafficHitTime <
                    trafficHitCooldown
                )
                {
                    continue;
                }


                Vector3 offset =
                    traffic.transform.position -
                    transform.position;


                float forwardDistance =
                    Vector3.Dot(
                        offset,
                        Vector3.forward
                    );


                float lateralDistance =
                    Mathf.Abs(
                        Vector3.Dot(
                            offset,
                            Vector3.right
                        )
                    );


                if (
                    forwardDistance <
                    trafficMinForwardDistance
                )
                {
                    continue;
                }


                if (
                    forwardDistance >
                    trafficCollisionForwardDistance +
                    1.5f
                )
                {
                    continue;
                }


                if (
                    lateralDistance >
                    trafficCollisionRadius
                )
                {
                    continue;
                }


                float distance =
                    offset.sqrMagnitude;


                if (
                    distance <
                    closestDistance
                )
                {
                    closestDistance =
                        distance;

                    closestTraffic =
                        traffic;
                }
            }
        }


        //=========================================================
        // FALLBACK
        //=========================================================

        if (
            closestTraffic == null
        )
        {
            TrafficVehicle[] allTraffic =
                FindObjectsByType<TrafficVehicle>(
                    FindObjectsSortMode.None
                );


            for (
                int i = 0;
                i < allTraffic.Length;
                i++
            )
            {
                TrafficVehicle traffic =
                    allTraffic[i];


                if (traffic == null)
                    continue;


                if (
                    traffic.gameObject ==
                    gameObject
                )
                {
                    continue;
                }


                if (
                    ignorePhotonTraffic &&
                    traffic.IsKnockedByPhoton
                )
                {
                    continue;
                }


                if (
                    traffic ==
                    lastHitTraffic &&
                    Time.time -
                    lastTrafficHitTime <
                    trafficHitCooldown
                )
                {
                    continue;
                }


                Vector3 offset =
                    traffic.transform.position -
                    transform.position;


                float forwardDistance =
                    Vector3.Dot(
                        offset,
                        Vector3.forward
                    );


                float lateralDistance =
                    Mathf.Abs(
                        Vector3.Dot(
                            offset,
                            Vector3.right
                        )
                    );


                if (
                    forwardDistance <
                    trafficMinForwardDistance
                )
                {
                    continue;
                }


                if (
                    forwardDistance >
                    trafficCollisionForwardDistance +
                    2f
                )
                {
                    continue;
                }


                if (
                    lateralDistance >
                    trafficCollisionRadius
                )
                {
                    continue;
                }


                float distance =
                    offset.sqrMagnitude;


                if (
                    distance <
                    closestDistance
                )
                {
                    closestDistance =
                        distance;

                    closestTraffic =
                        traffic;
                }
            }
        }


        if (closestTraffic == null)
            return;


        HitTraffic(
            closestTraffic
        );


        nextTrafficHitTime =
            Time.time +
            trafficHitCooldown;
    }


    //=============================================================
    // HIT TRAFFIC
    //=============================================================

    private void HitTraffic(
        TrafficVehicle traffic
    )
    {
        if (traffic == null)
            return;


        if (
            ignorePhotonTraffic &&
            traffic.IsKnockedByPhoton
        )
        {
            return;
        }


        SpawnCollisionEffect(
            GetImpactPosition(
                traffic.transform
            )
        );


        float side =
            traffic.transform.position.x -
            transform.position.x;


        float sideDirection;


        if (
            Mathf.Abs(side) >
            0.05f
        )
        {
            sideDirection =
                Mathf.Sign(side);
        }
        else
        {
            sideDirection =
                Random.value > 0.5f
                    ? 1f
                    : -1f;
        }


        LaunchTraffic(
            traffic,
            sideDirection
        );


        lastHitTraffic =
            traffic;

        lastTrafficHitTime =
            Time.time;
    }


    //=============================================================
    // LAUNCH TRAFFIC
    //=============================================================

    private void LaunchTraffic(
        TrafficVehicle traffic,
        float sideDirection
    )
    {
        if (traffic == null)
            return;


        if (
            ignorePhotonTraffic &&
            traffic.IsKnockedByPhoton
        )
        {
            return;
        }


        SpawnCollisionEffect(
            GetImpactPosition(
                traffic.transform
            )
        );


        float safeSide =
            Mathf.Abs(sideDirection) > 0.01f
                ? Mathf.Sign(sideDirection)
                : Random.value > 0.5f
                    ? 1f
                    : -1f;


        traffic.ApplyAmbulanceKnockback(
            new Vector3(
                safeSide *
                Mathf.Abs(
                    trafficSideForce
                ),

                Mathf.Abs(
                    trafficUpForce
                ),

                Mathf.Abs(
                    trafficForwardForce
                )
            ),
            launchedLinearDamping,
            launchedAngularDamping,
            allowTrafficRotation,
            trafficTorque,
            trafficPhysicsLifetime
        );


        lastHitTraffic =
            traffic;

        lastTrafficHitTime =
            Time.time;
    }


    //=============================================================
    // IMPACT POSITION
    //=============================================================

    private Vector3 GetImpactPosition(
        Transform other
    )
    {
        if (other == null)
            return transform.position;


        return (
            transform.position +
            other.position
        ) * 0.5f;
    }


    //=============================================================
    // EFFECT
    //=============================================================

    private void SpawnCollisionEffect(
        Vector3 position
    )
    {
        if (
            collisionEffectPrefab == null
        )
        {
            return;
        }


        Instantiate(
            collisionEffectPrefab,
            position,
            Quaternion.identity
        );
    }


    //=============================================================
    // ROTATION
    //=============================================================

    private void SetForwardRotation()
    {
        Quaternion rotation =
            Quaternion.LookRotation(
                Vector3.forward,
                Vector3.up
            );


        rotation *=
            Quaternion.Euler(
                0f,
                modelYRotationOffset,
                0f
            );


        transform.rotation =
            rotation;
    }


    private void SetForwardRotationSmooth()
    {
        if (isPhotonLaunched)
            return;


        Quaternion targetRotation =
            Quaternion.LookRotation(
                Vector3.forward,
                Vector3.up
            );


        targetRotation *=
            Quaternion.Euler(
                0f,
                modelYRotationOffset,
                0f
            );


        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed *
                Time.deltaTime
            );
    }


    //=============================================================
    // MOVE
    //=============================================================

    private void MoveAmbulance()
    {
        if (isPhotonLaunched)
            return;


        Vector3 position =
            transform.position;


        position.x =
            Mathf.MoveTowards(
                position.x,
                laneX,
                laneSnapSpeed *
                Time.deltaTime
            );


        position.z +=
            currentSpeed *
            Time.deltaTime;


        transform.position =
            position;


        SetForwardRotationSmooth();
    }


    //=============================================================
    // AUDIO
    //=============================================================

    private void SetupEngineAudio()
    {
        if (
            ambulanceEngineClip == null
        )
        {
            Debug.LogWarning(
                "[AmbulanceController] " +
                "Chưa gán Ambulance Engine Clip."
            );

            return;
        }


        if (
            forceCreateEngineAudio
        )
        {
            engineAudioSource =
                gameObject.AddComponent<AudioSource>();
        }
        else
        {
            engineAudioSource =
                GetComponent<AudioSource>();


            if (engineAudioSource == null)
            {
                engineAudioSource =
                    GetComponentInChildren<AudioSource>(
                        true
                    );
            }


            if (engineAudioSource == null)
            {
                engineAudioSource =
                    gameObject.AddComponent<AudioSource>();
            }
        }


        engineAudioSource.clip =
            ambulanceEngineClip;

        engineAudioSource.volume =
            Mathf.Clamp01(
                engineVolume
            );

        engineAudioSource.loop =
            engineLoop;

        engineAudioSource.playOnAwake =
            false;

        engineAudioSource.spatialBlend =
            0f;

        engineAudioSource.dopplerLevel =
            0f;

        engineAudioSource.minDistance =
            engineMinDistance;

        engineAudioSource.maxDistance =
            engineMaxDistance;

        engineAudioSource.ignoreListenerPause =
            true;


        engineAudioSource.Stop();


        if (engineLoop)
        {
            engineAudioSource.Play();
        }
    }


    //=============================================================
    // RESTART AUDIO
    //=============================================================

    private void RestartEngineAudio()
    {
        if (
            ambulanceEngineClip == null
        )
        {
            return;
        }


        if (engineAudioSource == null)
        {
            SetupEngineAudio();
            return;
        }


        engineAudioSource.enabled =
            true;

        engineAudioSource.clip =
            ambulanceEngineClip;

        engineAudioSource.volume =
            Mathf.Clamp01(
                engineVolume
            );

        engineAudioSource.loop =
            engineLoop;


        if (
            !engineAudioSource.isPlaying
        )
        {
            engineAudioSource.Play();
        }
    }


    //=============================================================
    // DESPAWN
    //=============================================================

    public void Despawn()
    {
        RestorePlayerCollisions();


        StopEngineAudio();


        Destroy(
            gameObject
        );
    }


    //=============================================================
    // STOP AUDIO
    //=============================================================

    private void StopEngineAudio()
    {
        if (
            engineAudioSource != null
        )
        {
            engineAudioSource.Stop();
        }
    }


    //=============================================================
    // DESTROY
    //=============================================================

    private void OnDestroy()
    {
        RestorePlayerCollisions();


        StopEngineAudio();
    }


    //=============================================================
    // DEBUG
    //=============================================================

    private void OnDrawGizmosSelected()
    {
        //=========================================================
        // PLAYER
        //=========================================================

        Gizmos.color =
            Color.yellow;


        Gizmos.DrawWireSphere(
            transform.position +
            Vector3.forward *
            (
                playerCollisionForwardDistance *
                0.35f
            ),
            playerCollisionRadius
        );


        //=========================================================
        // TRAFFIC
        //=========================================================

        Gizmos.color =
            Color.red;


        Vector3 start =
            transform.position;


        Vector3 end =
            transform.position +
            Vector3.forward *
            trafficCollisionForwardDistance;


        Gizmos.DrawWireSphere(
            start,
            trafficCollisionRadius
        );


        Gizmos.DrawWireSphere(
            end,
            trafficCollisionRadius
        );


        Gizmos.DrawLine(
            start,
            end
        );
    }
}


//=================================================================
// TRAFFIC LAUNCH CLEANUP
//=================================================================

public class LaunchCleanup : MonoBehaviour
{
    private Rigidbody targetRigidbody;

    private float destroyTime;

    private bool initialized;


    public void Initialize(
        Rigidbody rb,
        float lifetime
    )
    {
        targetRigidbody =
            rb;


        destroyTime =
            Time.time +
            Mathf.Max(
                0.1f,
                lifetime
            );


        initialized =
            true;
    }


    private void Update()
    {
        if (!initialized)
            return;


        if (
            Time.time >=
            destroyTime
        )
        {
            Destroy(
                gameObject
            );
        }
    }


    private void OnDestroy()
    {
        if (
            targetRigidbody != null
        )
        {
            targetRigidbody.linearVelocity =
                Vector3.zero;

            targetRigidbody.angularVelocity =
                Vector3.zero;
        }
    }
}


//=================================================================
// PHOTON STATE BRIDGE
//=================================================================

public class PhotonStateReceiver : MonoBehaviour
{
    public bool IsActive;
}
