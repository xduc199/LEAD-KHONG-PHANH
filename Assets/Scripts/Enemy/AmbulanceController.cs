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

    [Header("Player Speed Relationship")]

    [Tooltip("Hệ số tốc độ Ambulance so với tốc độ thực của Player.")]
    [SerializeField]
    private float playerSpeedMultiplier = 1.4f;

    [Tooltip("Ambulance luôn được cộng thêm tốc độ này so với Player.")]
    [SerializeField]
    private float speedBonusOverPlayer = 8f;

    [Tooltip("Tốc độ tối đa mới của Ambulance để không bị giới hạn khi Player tăng tốc.")]
    [SerializeField]
    private float maxAmbulanceSpeed = 120f;

    [Tooltip("Khoảng cách Player bỏ xa Ambulance để Ambulance tăng tốc bắt kịp.")]
    [SerializeField]
    private float catchUpDistance = 25f;

    [Tooltip("Tốc độ cộng thêm khi cần bắt kịp Player.")]
    [SerializeField]
    private float catchUpSpeedBonus = 10f;

    [Tooltip("Không cho Ambulance tụt dưới tốc độ tối thiểu đã đạt.")]
    [SerializeField]
    private bool maintainMinimumRunningSpeed = true;

    [Tooltip("Giữ lại tốc độ cao đã đạt để Ambulance không chậm dần theo Player.")]
    [SerializeField]
    private bool maintainCurrentSpeed = true;

    [SerializeField]
    private float accelerationSmoothTime = 0.12f;

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

    private float playerSpeed;

    private float highestStableSpeed;

    private Vector3 lastPlayerPosition;

    private float laneX;

    private bool initialized;

    private float nextPlayerHitTime;

    private float nextTrafficHitTime;

    private TrafficVehicle lastHitTraffic;

    private float lastTrafficHitTime;


    //=============================================================
    // SHIELD COLLISION STATE
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


        ambulanceColliders =
            GetComponentsInChildren<Collider>(
                true
            );


        rb.isKinematic =
            true;

        rb.useGravity =
            false;

        rb.interpolation =
            RigidbodyInterpolation.Interpolate;

        rb.collisionDetectionMode =
            CollisionDetectionMode.ContinuousSpeculative;


        currentSpeed =
            Mathf.Max(
                minSpeed,
                useFixedSpeed
                    ? fixedSpeed
                    : minSpeed
            );

        highestStableSpeed =
            currentSpeed;


        spawnTime =
            Time.time;


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


        spawnTime =
            Time.time;

        currentSpeed =
            Mathf.Max(
                minSpeed,
                useFixedSpeed
                    ? fixedSpeed
                    : Random.Range(
                        minSpeed,
                        maxSpeed
                    )
            );

        speedVelocity =
            0f;

        playerSpeed =
            0f;

        highestStableSpeed =
            currentSpeed;

        if (playerTransform != null)
        {
            lastPlayerPosition =
                playerTransform.position;
        }

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


        RestorePlayerCollisions();


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


        SetForwardRotation();


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


        UpdateShieldCollisionProtection();


        if (
            Time.time -
            spawnTime >=
            lifetime
        )
        {
            Despawn();
            return;
        }


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
        // TARGET SPEED - ALWAYS FASTER THAN PLAYER
        //=========================================================

        float targetSpeed =
            CalculateTargetSpeed();

        float smoothTime =
            targetSpeed > currentSpeed
                ? accelerationSmoothTime
                : accelerationSmoothTime * 1.5f;

        currentSpeed =
            Mathf.SmoothDamp(
                currentSpeed,
                targetSpeed,
                ref speedVelocity,
                Mathf.Max(0.01f, smoothTime)
            );

        //=========================================================
        // HARD SPEED FLOOR
        //=========================================================

        if (maintainMinimumRunningSpeed)
        {
            currentSpeed =
                Mathf.Max(
                    currentSpeed,
                    minSpeed
                );
        }

        //=========================================================
        // KEEP HIGHEST STABLE SPEED
        //=========================================================

        if (
            maintainCurrentSpeed &&
            currentSpeed > highestStableSpeed
        )
        {
            highestStableSpeed =
                currentSpeed;
        }

        if (
            maintainCurrentSpeed &&
            highestStableSpeed > currentSpeed
        )
        {
            float minimumMaintainedSpeed =
                Mathf.Max(
                    minSpeed,
                    highestStableSpeed * 0.92f
                );

            currentSpeed =
                Mathf.Max(
                    currentSpeed,
                    minimumMaintainedSpeed
                );
        }

        currentSpeed =
            Mathf.Min(
                currentSpeed,
                maxAmbulanceSpeed
            );


        if (
            Time.time -
            spawnTime >=
            collisionStartDelay
        )
        {
            CheckPlayerCollision();

            CheckTrafficCollision();
        }


        MoveAmbulance();
    }


    //=============================================================
    // PLAYER SPEED
    //=============================================================

    private void UpdatePlayerSpeed()
    {
        if (playerTransform == null)
            return;

        PlayerController player =
            GetPlayerControllerFromTransform(
                playerTransform
            );

        if (player != null)
        {
            playerSpeed =
                Mathf.Max(
                    0f,
                    player.CurrentForwardSpeed
                );

            lastPlayerPosition =
                playerTransform.position;

            return;
        }

        Vector3 currentPosition =
            playerTransform.position;

        Vector3 delta =
            currentPosition -
            lastPlayerPosition;

        float measuredSpeed =
            Mathf.Abs(delta.z) /
            Mathf.Max(
                Time.deltaTime,
                0.0001f
            );

        playerSpeed =
            Mathf.Lerp(
                playerSpeed,
                measuredSpeed,
                10f * Time.deltaTime
            );

        lastPlayerPosition =
            currentPosition;
    }


    //=============================================================
    // TARGET SPEED
    //=============================================================

    private float CalculateTargetSpeed()
    {
        UpdatePlayerSpeed();

        float targetSpeed =
            playerSpeed *
            Mathf.Max(0f, playerSpeedMultiplier);

        targetSpeed +=
            Mathf.Max(0f, speedBonusOverPlayer);

        // Giữ tốc độ tối thiểu cũ của Ambulance.
        targetSpeed =
            Mathf.Max(
                targetSpeed,
                minSpeed
            );

        // Nếu bật fixed speed, fixedSpeed vẫn chỉ là
        // mức sàn; Ambulance vẫn phải nhanh hơn Player.
        if (useFixedSpeed)
        {
            targetSpeed =
                Mathf.Max(
                    targetSpeed,
                    fixedSpeed
                );
        }

        if (playerTransform != null)
        {
            float zDifference =
                playerTransform.position.z -
                transform.position.z;

            if (zDifference > catchUpDistance)
            {
                targetSpeed +=
                    Mathf.Max(
                        0f,
                        catchUpSpeedBonus
                    );
            }
        }

        // Không thấp hơn tốc độ đã đạt nếu giữ tốc độ bật.
        if (maintainCurrentSpeed)
        {
            targetSpeed =
                Mathf.Max(
                    targetSpeed,
                    highestStableSpeed
                );
        }

        return Mathf.Min(
            targetSpeed,
            Mathf.Max(
                minSpeed,
                maxAmbulanceSpeed
            )
        );
    }


    //=============================================================
    // SHIELD COLLISION PROTECTION
    //=============================================================

    private void UpdateShieldCollisionProtection()
    {
        if (playerTransform == null)
            return;


        if (!shieldCollisionBlocked)
        {
            return;
        }


        PlayerController player =
            GetPlayerControllerFromTransform(
                playerTransform
            );


        if (player == null)
            return;


        if (
            ArePlayerAndAmbulanceOverlapping(
                player
            )
        )
        {
            return;
        }


        RestorePlayerCollisions();

        shieldCollisionBlocked =
            false;

        nextPlayerHitTime =
            Mathf.Max(
                nextPlayerHitTime,
                Time.time +
                0.05f
            );
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


        if (shieldCollisionBlocked)
        {
            PlayerController blockedPlayer =
                GetPlayerControllerFromTransform(
                    playerTransform
                );


            if (blockedPlayer != null)
            {
                if (
                    ArePlayerAndAmbulanceOverlapping(
                        blockedPlayer
                    )
                )
                {
                    return;
                }
            }


            RestorePlayerCollisions();

            shieldCollisionBlocked =
                false;
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
        // SHIELD
        //
        // Dùng toàn bộ Shield logic của PlayerController.
        //
        // KHÔNG ConsumeShield() trực tiếp ở Ambulance.
        //=========================================================

        if (
            player.TryConsumeShield(
                gameObject
            )
        )
        {
            //=====================================================
            // Một hit Shield hợp lệ có explosion collision effect.
            // Những hit tiếp theo trong thời gian invulnerable
            // sẽ không đi tới đây vì TryConsumeShield() chỉ
            // bảo vệ Player và CheckPlayerCollision đang cooldown.
            //=====================================================

            SpawnCollisionEffect(
                GetImpactPosition(
                    player.transform
                )
            );


            IgnorePlayerCollisions(
                player
            );


            shieldCollisionBlocked =
                true;


            nextPlayerHitTime =
                Time.time +
                Mathf.Max(
                    playerHitCooldown,
                    0.15f
                );


            return;
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
        // PLAYER BAY LÊN + BAY VỀ PHÍA TRƯỚC
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


        Physics.SyncTransforms();
    }


    //=============================================================
    // CHECK OVERLAP
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


        PhotonController photon =
            player.GetComponent<PhotonController>();


        if (photon == null)
        {
            photon =
                player.GetComponentInChildren<PhotonController>(
                    true
                );
        }


        if (photon == null)
        {
            photon =
                player.GetComponentInParent<PhotonController>();
        }


        if (
            photon != null &&
            photon.IsPhotonActive
        )
        {
            return true;
        }


        PhotonStateReceiver receiver =
            player.GetComponent<PhotonStateReceiver>();


        if (
            receiver != null &&
            receiver.IsActive
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


                //=================================================
                // QUAN TRỌNG:
                //
                // OverlapCapsule chỉ tìm ứng viên.
                // Chưa được phép hất xe ở đây.
                //=================================================

                if (
                    !AreTrafficCollidersActuallyTouching(
                        traffic
                    )
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


                //=================================================
                // FALLBACK CŨNG PHẢI CHẠM THỰC SỰ
                //=================================================

                if (
                    !AreTrafficCollidersActuallyTouching(
                        traffic
                    )
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
    // ACTUAL TRAFFIC COLLISION
    //=============================================================

    private bool AreTrafficCollidersActuallyTouching(
        TrafficVehicle traffic
    )
    {
        if (traffic == null)
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


        Collider[] trafficColliders =
            traffic.GetComponentsInChildren<Collider>(
                true
            );


        if (
            ambulanceColliders == null ||
            trafficColliders == null
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


            if (
                ambulanceCollider == null ||
                !ambulanceCollider.enabled
            )
            {
                continue;
            }


            for (
                int j = 0;
                j < trafficColliders.Length;
                j++
            )
            {
                Collider trafficCollider =
                    trafficColliders[j];


                if (
                    trafficCollider == null ||
                    !trafficCollider.enabled
                )
                {
                    continue;
                }


                if (
                    ambulanceCollider ==
                    trafficCollider
                )
                {
                    continue;
                }


                Vector3 direction;

                float distance;


                if (
                    Physics.ComputePenetration(
                        ambulanceCollider,
                        ambulanceCollider.transform.position,
                        ambulanceCollider.transform.rotation,

                        trafficCollider,
                        trafficCollider.transform.position,
                        trafficCollider.transform.rotation,

                        out direction,
                        out distance
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
    // VALIDATE
    //=============================================================

    private void OnValidate()
    {
        minSpeed = Mathf.Max(0f, minSpeed);
        maxSpeed = Mathf.Max(minSpeed, maxSpeed);
        fixedSpeed = Mathf.Max(0f, fixedSpeed);
        playerSpeedMultiplier = Mathf.Max(0f, playerSpeedMultiplier);
        speedBonusOverPlayer = Mathf.Max(0f, speedBonusOverPlayer);
        maxAmbulanceSpeed = Mathf.Max(minSpeed, maxAmbulanceSpeed);
        catchUpDistance = Mathf.Max(0f, catchUpDistance);
        catchUpSpeedBonus = Mathf.Max(0f, catchUpSpeedBonus);
        accelerationSmoothTime = Mathf.Max(0.01f, accelerationSmoothTime);
    }


    //=============================================================
    // DEBUG
    //=============================================================

    private void OnDrawGizmosSelected()
    {
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