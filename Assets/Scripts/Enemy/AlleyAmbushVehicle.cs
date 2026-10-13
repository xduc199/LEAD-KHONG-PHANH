using UnityEngine;

[DisallowMultipleComponent]
public sealed class AlleyAmbushVehicle : MonoBehaviour
{
    // ============================================================
    // MOVEMENT
    // ============================================================

    [Header("Movement")]
    [SerializeField, Min(0f)]
    private float moveSpeed = 22f;

    [SerializeField, Min(0f)]
    private float maxTravelDistance = 45f;

    [SerializeField]
    private bool useRigidbodyMovement = true;

    // ============================================================
    // TRAFFIC KNOCKBACK
    // ============================================================

    [Header("Traffic Knockback")]
    [SerializeField, Min(0f)]
    private float trafficKnockbackForce = 18f;

    [SerializeField, Min(0f)]
    private float trafficUpwardForce = 6f;

    [SerializeField]
    private bool makeTrafficDynamicOnHit = true;

    [SerializeField]
    private bool disableTrafficControllerOnHit = true;

    [SerializeField, Range(0f, 1f)]
    private float alleyMomentumMultiplier = 0.35f;

    [SerializeField, Min(0f)]
    private float trafficPhysicsLifetime = 2.5f;

    // ============================================================
    // ALLEY FLY / RECOIL
    // ============================================================

    [Header("Alley Fly Knockback")]
    [SerializeField, Min(0f)]
    private float alleyRecoilForce = 12f;

    [SerializeField, Min(0f)]
    private float alleyRecoilUpwardForce = 18f;

    [SerializeField, Min(0.05f)]
    private float alleyRecoilDuration = 1.4f;

    [SerializeField, Min(0f)]
    private float alleyFlyTorque = 7f;

    // Kept for Inspector/API compatibility.
    // No physics pause is performed; impact remains visually continuous.
    [SerializeField, Min(0f)]
    private float trafficImpactPause = 0.055f;

    // ============================================================
    // PLAYER KNOCKBACK
    // ============================================================

    [Header("Player Knockback")]
    [SerializeField]
    private bool knockbackPlayer = true;

    [SerializeField, Min(0f)]
    private float playerKnockbackForce = 20f;

    [SerializeField, Min(0f)]
    private float playerUpwardForce = 8f;

    [SerializeField, Min(0.05f)]
    private float playerKnockbackDuration = 0.45f;

    // ============================================================
    // OBSTACLE
    // ============================================================

    [Header("Obstacle")]
    // Legacy compatibility only.
    // Gameplay obstacle knockback is now restricted to traffic vehicles.
    [SerializeField]
    private bool affectObstacles = false;

    [SerializeField, Min(0f)]
    private float obstacleKnockbackForce = 18f;

    [SerializeField, Min(0f)]
    private float obstacleUpwardForce = 5f;

    [SerializeField, Min(0f)]
    private float obstacleTorque = 6f;

    [SerializeField, Min(0f)]
    private float obstacleDestroyDelay = 2.5f;

    [SerializeField]
    private string obstacleTag = "Obstacle";

    // ============================================================
    // IMPACT AUDIO
    // ============================================================

    [Header("Impact Audio")]
    [SerializeField]
    private AudioSource impactAudioSource;

    [SerializeField]
    private AudioClip[] impactSounds;

    [SerializeField, Range(0f, 1f)]
    private float impactVolume = 1f;

    // ============================================================
    // IMPACT VFX
    // ============================================================

    [Header("Impact VFX")]
    [SerializeField]
    private GameObject impactExplosionPrefab;

    [SerializeField]
    private Vector3 impactVfxOffset;

    // ============================================================
    // DEBUG
    // ============================================================

    [Header("Debug")]
    [SerializeField]
    private bool debugLogs = false;

    // ============================================================
    // LIFECYCLE
    // ============================================================

    private enum AlleyState : byte
    {
        Active = 0,
        Impacted = 1,
        Dead = 2
    }

    private AlleyState state = AlleyState.Active;

    private bool initialized;
    private bool isDead;

    // ============================================================
    // CACHE
    // ============================================================

    private Rigidbody cachedRigidbody;

    // PhotonController.IsPhotonActive is an INSTANCE property in this project,
    // not a static member. Cache the scene controller once and read its state.
    private PhotonController photonController;
    private Collider[] cachedColliders;

    private Vector3 movementDirection;
    private Vector3 startPosition;
    private float currentSpeed;

    // ============================================================
    // FLY STATE
    // ============================================================

    private bool isFlying;
    private float flyEndTime;

    // ============================================================
    // PLAYER KNOCKBACK STATE
    // ============================================================

    private bool playerKnockbackActive;
    private float playerKnockbackEndTime;
    private Rigidbody playerKnockbackRigidbody;

    private bool playerOriginalIsKinematic;
    private bool playerOriginalUseGravity;
    private CollisionDetectionMode playerOriginalCollisionMode;

    // ============================================================
    // TRAFFIC COLLISION DEDUP
    // ============================================================

    private int lastCollisionFrame = -1;
    private int lastCollisionObjectId = 0;

    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        cachedRigidbody = GetComponent<Rigidbody>();

        photonController =
            FindFirstObjectByType<PhotonController>();

        if (cachedRigidbody != null)
        {
            cachedRigidbody.interpolation =
                RigidbodyInterpolation.Interpolate;
        }

        CacheColliders();
    }

    private void OnEnable()
    {
        state = AlleyState.Active;
        isDead = false;
        isFlying = false;
        initialized = false;

        lastCollisionFrame = -1;
        lastCollisionObjectId = 0;

        if (cachedColliders != null)
        {
            for (int i = 0; i < cachedColliders.Length; i++)
            {
                Collider collider = cachedColliders[i];

                if (collider != null)
                    collider.enabled = true;
            }
        }

        if (cachedRigidbody != null)
            cachedRigidbody.detectCollisions = true;
    }

    private void Update()
    {
        if (state == AlleyState.Dead)
            return;

        if (state == AlleyState.Active &&
            !useRigidbodyMovement &&
            !isFlying)
        {
            MoveTransform();
        }

        CheckTravelDistance();
    }

    private void FixedUpdate()
    {
        if (state == AlleyState.Dead)
            return;

        UpdatePlayerKnockback();

        if (isFlying)
        {
            UpdateFlyState();
            return;
        }

        if (state != AlleyState.Active)
            return;

        if (useRigidbodyMovement)
            MoveRigidbody();
    }

    // ============================================================
    // INITIALIZE
    // ============================================================

    public void Initialize(Vector3 direction, float speed)
    {
        CacheColliders();

        state = AlleyState.Active;
        isDead = false;
        initialized = true;
        isFlying = false;

        movementDirection = direction;
        movementDirection.y = 0f;

        if (movementDirection.sqrMagnitude < 0.0001f)
        {
            movementDirection = transform.forward;
            movementDirection.y = 0f;
        }

        if (movementDirection.sqrMagnitude < 0.0001f)
            movementDirection = Vector3.forward;
        else
            movementDirection.Normalize();

        currentSpeed = speed > 0f ? speed : moveSpeed;
        startPosition = transform.position;

        transform.forward = movementDirection;

        EnableGameplayCollision();

        if (cachedRigidbody == null)
            return;

        // Reset dynamic state BEFORE switching to kinematic.
        // Unity does not allow velocity writes while isKinematic == true.
        if (!cachedRigidbody.isKinematic)
        {
            cachedRigidbody.linearVelocity = Vector3.zero;
            cachedRigidbody.angularVelocity = Vector3.zero;
        }

        cachedRigidbody.isKinematic = true;
        cachedRigidbody.useGravity = false;
        cachedRigidbody.detectCollisions = true;
    }

    // ============================================================
    // MOVEMENT
    // ============================================================

    private void MoveTransform()
    {
        if (!initialized || state != AlleyState.Active)
            return;

        transform.position +=
            movementDirection *
            currentSpeed *
            Time.deltaTime;
    }

    private void MoveRigidbody()
    {
        if (!initialized ||
            state != AlleyState.Active ||
            cachedRigidbody == null)
        {
            return;
        }

        // Alley is kinematic while travelling.
        if (cachedRigidbody.isKinematic)
        {
            Vector3 nextPosition =
                cachedRigidbody.position +
                movementDirection *
                currentSpeed *
                Time.fixedDeltaTime;

            cachedRigidbody.MovePosition(nextPosition);
            return;
        }

        cachedRigidbody.linearVelocity =
            movementDirection * currentSpeed;
    }

    // ============================================================
    // TRAVEL DISTANCE
    // ============================================================

    private void CheckTravelDistance()
    {
        if (!initialized ||
            state != AlleyState.Active ||
            maxTravelDistance <= 0f)
        {
            return;
        }

        Vector3 offset =
            transform.position - startPosition;

        offset.y = 0f;

        float maxDistanceSqr =
            maxTravelDistance * maxTravelDistance;

        if (offset.sqrMagnitude >= maxDistanceSqr)
            KillSelf();
    }

    // ============================================================
    // COLLISION ENTRY
    // ============================================================

    private void OnCollisionEnter(Collision collision)
    {
        if (state != AlleyState.Active || isDead)
            return;

        if (collision == null || collision.collider == null)
            return;

        Collider other = collision.collider;

        if (IsDuplicateCollision(other))
            return;

        Vector3 hitPoint = transform.position;
        Vector3 hitNormal = -movementDirection;

        if (collision.contactCount > 0)
        {
            ContactPoint contact = collision.GetContact(0);
            hitPoint = contact.point;
            hitNormal = contact.normal;
        }

        HandleCollision(other, hitNormal, hitPoint);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (state != AlleyState.Active || isDead)
            return;

        if (other == null)
            return;

        if (IsDuplicateCollision(other))
            return;

        Vector3 hitPoint =
            other.ClosestPoint(transform.position);

        Vector3 hitNormal =
            transform.position - hitPoint;

        if (hitNormal.sqrMagnitude < 0.0001f)
            hitNormal = -movementDirection;
        else
            hitNormal.Normalize();

        HandleCollision(other, hitNormal, hitPoint);
    }

    private bool IsDuplicateCollision(Collider other)
    {
        if (other == null)
            return true;

        Rigidbody rb = other.attachedRigidbody;

        int objectId;

        if (rb != null)
        {
            objectId = rb.GetInstanceID();
        }
        else
        {
            Transform root = other.transform.root;
            objectId = root != null
                ? root.gameObject.GetInstanceID()
                : other.gameObject.GetInstanceID();
        }

        int frame = Time.frameCount;

        if (frame == lastCollisionFrame &&
            objectId == lastCollisionObjectId)
        {
            return true;
        }

        lastCollisionFrame = frame;
        lastCollisionObjectId = objectId;

        return false;
    }

    // ============================================================
    // COLLISION ROUTER
    // ============================================================

    private void HandleCollision(
        Collider other,
        Vector3 hitNormal,
        Vector3 impactPoint)
    {
        if (state != AlleyState.Active ||
            isDead ||
            other == null)
        {
            return;
        }

        if (IsAmbulance(other))
        {
            Log("AMBULANCE IGNORED");
            return;
        }

        if (IsPlayer(other))
        {
            HandlePlayerCollision(
                other,
                hitNormal,
                impactPoint);

            return;
        }

        AlleyAmbushVehicle otherAlley =
            other.GetComponentInParent<AlleyAmbushVehicle>();

        if (otherAlley != null &&
            otherAlley != this)
        {
            HandleAlleyCollision(
                otherAlley,
                hitNormal,
                impactPoint);

            return;
        }

        TrafficVehicle traffic =
            other.GetComponentInParent<TrafficVehicle>();

        if (traffic != null)
        {
            HandleTrafficCollision(
                traffic,
                hitNormal,
                impactPoint);

            return;
        }

        TrafficCarBehavior trafficCar =
            other.GetComponentInParent<TrafficCarBehavior>();

        if (trafficCar != null)
        {
            HandleTrafficFallbackCollision(
                trafficCar,
                other,
                hitNormal,
                impactPoint);

            return;
        }

        // IMPORTANT:
        // In this project, gameplay "obstacles" are Traffic vehicles.
        // Environment objects such as houses/props may also carry
        // the legacy "Obstacle" tag, so they must NOT enter the
        // Alley gameplay-impact pipeline.
        //
        // TrafficVehicle and TrafficCarBehavior are handled above.
        // All other colliders are treated as environment/physical
        // collision only and cannot consume the Alley impact.
    }

    // ============================================================
    // IMPACT COMMIT
    // ============================================================

    /// <summary>
    /// The critical transition:
    /// Active -> Impacted happens BEFORE any knockback/recoil/VFX.
    /// Once this succeeds, this Alley can never process another
    /// gameplay collision.
    /// </summary>
    private bool TryCommitImpact()
    {
        if (state != AlleyState.Active || isDead)
            return false;

        state = AlleyState.Impacted;

        // Disable gameplay collision immediately.
        // The Rigidbody is NOT frozen; it may still fly/recoil.
        DisableGameplayCollision();

        return true;
    }

    private void DisableGameplayCollision()
    {
        if (cachedColliders != null)
        {
            for (int i = 0; i < cachedColliders.Length; i++)
            {
                Collider collider = cachedColliders[i];

                if (collider != null)
                    collider.enabled = false;
            }
        }

        if (cachedRigidbody != null)
            cachedRigidbody.detectCollisions = false;
    }

    private void EnableGameplayCollision()
    {
        if (state != AlleyState.Active || isDead)
            return;

        if (cachedColliders != null)
        {
            for (int i = 0; i < cachedColliders.Length; i++)
            {
                Collider collider = cachedColliders[i];

                if (collider != null)
                    collider.enabled = true;
            }
        }

        if (cachedRigidbody != null)
            cachedRigidbody.detectCollisions = true;
    }

    // ============================================================
    // PLAYER
    // ============================================================

    private void HandlePlayerCollision(
        Collider other,
        Vector3 hitNormal,
        Vector3 impactPoint)
    {
        if (!TryCommitImpact())
            return;

        if (knockbackPlayer)
        {
            Rigidbody playerRb =
                other.attachedRigidbody;

            if (playerRb == null)
                playerRb = other.GetComponentInParent<Rigidbody>();

            if (playerRb != null)
            {
                ApplyPlayerKnockback(
                    playerRb,
                    hitNormal);
            }
        }

        ApplyAlleyRecoilFromPoint(
            hitNormal,
            impactPoint);

        PlayImpactFeedback(impactPoint);

        ScheduleDeathAfterRecoil();

        Log("PLAYER HIT | COLLISION LOCKED");
    }

    // ============================================================
    // ALLEY <-> ALLEY
    // ============================================================

    private void HandleAlleyCollision(
        AlleyAmbushVehicle otherAlley,
        Vector3 hitNormal,
        Vector3 impactPoint)
    {
        if (otherAlley == null ||
            otherAlley == this)
        {
            return;
        }

        // ========================================================
        // PHOTON SAFETY
        // ========================================================
        // Alley is a temporary gameplay projectile. We do NOT want
        // two networked Alleys to become a persistent physics pair.
        //
        // Photon ON:
        //   1. Try to consume both impacts atomically.
        //   2. If either Alley is no longer Active, do NOT force
        //      another impact. Ignore this pair and let them pass.
        //
        // This makes Alley <-> Alley deterministic enough for the
        // local physics simulation without touching Traffic logic.
        bool photonActive = false;

        // PhotonController.IsPhotonActive is an INSTANCE property.
        // The controller may be created after this Alley, so resolve it
        // lazily only when the Alley-vs-Alley collision actually occurs.
        if (photonController == null)
        {
            photonController =
                FindFirstObjectByType<PhotonController>();
        }

        if (photonController != null)
        {
            photonActive =
                photonController.IsPhotonActive;
        }

        if (photonActive)
        {
            if (state != AlleyState.Active ||
                otherAlley.state != AlleyState.Active ||
                otherAlley.isDead)
            {
                IgnoreAlleyPairCollision(otherAlley);
                return;
            }
        }

        // Both state transitions happen BEFORE either Alley receives
        // its launch. This closes the recursive collision chain.
        if (!TryCommitImpact())
        {
            if (photonActive)
                IgnoreAlleyPairCollision(otherAlley);

            return;
        }

        if (!otherAlley.TryCommitImpact())
        {
            // We already committed ourselves. Do NOT leave a
            // networked Alley pair able to repeatedly collide.
            if (photonActive)
                IgnoreAlleyPairCollision(otherAlley);

            return;
        }

        Vector3 direction =
            otherAlley.transform.position -
            transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = -hitNormal;
            direction.y = 0f;
        }

        if (direction.sqrMagnitude < 0.0001f)
            direction = movementDirection;

        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector3.forward;

        direction.Normalize();

        Vector3 otherDirection = -direction;

        // Both are already collision-disabled.
        // Apply launch MUST NOT re-enable collision.
        ApplyAlleyLaunch(
            direction,
            true);

        otherAlley.ApplyAlleyLaunch(
            otherDirection,
            true);

        PlayImpactFeedback(impactPoint);

        ScheduleDeathAfterRecoil();
        otherAlley.ScheduleDeathAfterRecoil();

        if (photonActive)
        {
            Log("PHOTON | ALLEY VS ALLEY | BOTH FLYING");
        }
        else
        {
            Log("ALLEY VS ALLEY | BOTH IMPACTED + FLYING");
        }
    }

    private void IgnoreAlleyPairCollision(
        AlleyAmbushVehicle otherAlley)
    {
        if (otherAlley == null)
            return;

        Collider[] mine = cachedColliders;
        Collider[] theirs = otherAlley.cachedColliders;

        if (mine == null || theirs == null)
            return;

        for (int i = 0; i < mine.Length; i++)
        {
            Collider a = mine[i];

            if (a == null)
                continue;

            for (int j = 0; j < theirs.Length; j++)
            {
                Collider b = theirs[j];

                if (b == null)
                    continue;

                Physics.IgnoreCollision(a, b, true);
            }
        }

        Log("PHOTON | ALLEY VS ALLEY | COLLISION IGNORED");
    }

    // ============================================================
    // TRAFFIC
    // ============================================================

    private void HandleTrafficCollision(
        TrafficVehicle traffic,
        Vector3 hitNormal,
        Vector3 impactPoint)
    {
        if (traffic == null ||
            traffic.IsDead)
        {
            return;
        }

        if (!TryCommitImpact())
            return;

        Vector3 direction =
            -hitNormal;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction =
                traffic.transform.position -
                transform.position;

            direction.y = 0f;
        }

        if (direction.sqrMagnitude < 0.0001f)
            direction = movementDirection;

        direction.Normalize();

        Vector3 launchForce =
            direction * trafficKnockbackForce;

        launchForce.y = trafficUpwardForce;

        launchForce +=
            movementDirection *
            (currentSpeed *
             alleyMomentumMultiplier);

        traffic.ApplyAmbulanceKnockback(
            launchForce,
            0f,
            0f,
            true,
            0f,
            trafficPhysicsLifetime);

        ApplyAlleyRecoilFromPoint(
            hitNormal,
            impactPoint);

        PlayImpactFeedback(impactPoint);

        ScheduleDeathAfterRecoil();

        Log("TRAFFIC HIT | TRAFFIC + ALLEY IMPACTED");
    }

    // ============================================================
    // TRAFFIC FALLBACK
    // ============================================================

    private void HandleTrafficFallbackCollision(
        TrafficCarBehavior trafficCar,
        Collider other,
        Vector3 hitNormal,
        Vector3 impactPoint)
    {
        if (state != AlleyState.Active ||
            isDead ||
            trafficCar == null)
        {
            return;
        }

        if (!TryCommitImpact())
            return;

        Rigidbody rb =
            other != null
                ? other.attachedRigidbody
                : null;

        if (rb == null)
            rb = trafficCar.GetComponent<Rigidbody>();

        if (rb == null)
            rb = trafficCar.GetComponentInParent<Rigidbody>();

        if (rb == null)
            rb = trafficCar.GetComponentInChildren<Rigidbody>();

        if (rb != null)
        {
            Vector3 direction = -hitNormal;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.0001f)
                direction = movementDirection;

            direction.Normalize();

            if (makeTrafficDynamicOnHit)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.collisionDetectionMode =
                    CollisionDetectionMode.ContinuousDynamic;
            }

            Vector3 velocity =
                direction * trafficKnockbackForce;

            velocity.y = trafficUpwardForce;

            velocity +=
                movementDirection *
                (currentSpeed *
                 alleyMomentumMultiplier);

            rb.linearVelocity = velocity;

            if (obstacleTorque > 0f)
            {
                rb.AddTorque(
                    Random.onUnitSphere *
                    obstacleTorque,
                    ForceMode.VelocityChange);
            }
        }

        if (disableTrafficControllerOnHit)
            trafficCar.enabled = false;

        ApplyAlleyRecoilFromPoint(
            hitNormal,
            impactPoint);

        PlayImpactFeedback(impactPoint);

        ScheduleDeathAfterRecoil();

        if (trafficPhysicsLifetime > 0f &&
            trafficCar.gameObject != null)
        {
            Destroy(
                trafficCar.gameObject,
                trafficPhysicsLifetime);
        }

        Log("TRAFFIC FALLBACK HIT | COLLISION LOCKED");
    }

    // ============================================================
    // OBSTACLE
    // ============================================================

    private void HandleObstacleCollision(
        Collider other,
        Vector3 hitNormal,
        Vector3 impactPoint)
    {
        if (!TryCommitImpact())
            return;

        Rigidbody rb =
            other.attachedRigidbody;

        if (rb == null)
            rb = other.GetComponentInParent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;

            if (rb.collisionDetectionMode ==
                CollisionDetectionMode.Discrete)
            {
                rb.collisionDetectionMode =
                    CollisionDetectionMode.ContinuousDynamic;
            }

            Vector3 direction = -hitNormal;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.0001f)
                direction = movementDirection;

            direction.Normalize();

            Vector3 velocity =
                direction * obstacleKnockbackForce;

            velocity.y = obstacleUpwardForce;

            rb.linearVelocity = velocity;

            if (obstacleTorque > 0f)
            {
                Vector3 torqueAxis =
                    Vector3.Cross(
                        Vector3.up,
                        direction);

                if (torqueAxis.sqrMagnitude < 0.0001f)
                    torqueAxis = Vector3.right;
                else
                    torqueAxis.Normalize();

                rb.AddTorque(
                    torqueAxis * obstacleTorque,
                    ForceMode.VelocityChange);
            }
        }

        ApplyAlleyRecoilFromPoint(
            hitNormal,
            impactPoint);

        PlayImpactFeedback(impactPoint);

        ScheduleDeathAfterRecoil();

        GameObject obstacleObject =
            FindObstacleRoot(other);

        if (obstacleObject != null &&
            obstacleDestroyDelay > 0f)
        {
            Destroy(
                obstacleObject,
                obstacleDestroyDelay);
        }

        Log("OBSTACLE HIT | COLLISION LOCKED");
    }

    // ============================================================
    // PLAYER KNOCKBACK
    // ============================================================

    private void ApplyPlayerKnockback(
        Rigidbody playerRigidbody,
        Vector3 hitNormal)
    {
        if (playerRigidbody == null)
            return;

        if (playerKnockbackActive &&
            playerKnockbackRigidbody ==
            playerRigidbody)
        {
            return;
        }

        playerKnockbackRigidbody =
            playerRigidbody;

        playerOriginalIsKinematic =
            playerRigidbody.isKinematic;

        playerOriginalUseGravity =
            playerRigidbody.useGravity;

        playerOriginalCollisionMode =
            playerRigidbody.collisionDetectionMode;

        playerKnockbackActive = true;

        playerKnockbackEndTime =
            Time.time +
            playerKnockbackDuration;

        Vector3 direction = -hitNormal;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction =
                transform.position -
                playerRigidbody.position;

            direction.y = 0f;
        }

        if (direction.sqrMagnitude < 0.0001f)
            direction = -movementDirection;

        direction.Normalize();

        playerRigidbody.isKinematic = false;
        playerRigidbody.useGravity = true;
        playerRigidbody.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;

        Vector3 velocity =
            direction * playerKnockbackForce;

        velocity.y = playerUpwardForce;

        playerRigidbody.linearVelocity = velocity;
        playerRigidbody.angularVelocity = Vector3.zero;
    }

    private void UpdatePlayerKnockback()
    {
        if (!playerKnockbackActive)
            return;

        if (playerKnockbackRigidbody == null)
        {
            ClearPlayerKnockbackState();
            return;
        }

        if (Time.time < playerKnockbackEndTime)
            return;

        Rigidbody rb =
            playerKnockbackRigidbody;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.isKinematic =
            playerOriginalIsKinematic;

        rb.useGravity =
            playerOriginalUseGravity;

        rb.collisionDetectionMode =
            playerOriginalCollisionMode;

        ClearPlayerKnockbackState();
    }

    private void ClearPlayerKnockbackState()
    {
        playerKnockbackActive = false;
        playerKnockbackRigidbody = null;
    }

    // ============================================================
    // ALLEY RECOIL
    // ============================================================

    private void ApplyAlleyRecoilFromPoint(
        Vector3 hitNormal,
        Vector3 impactPoint)
    {
        Vector3 direction = hitNormal;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction =
                transform.position -
                impactPoint;

            direction.y = 0f;
        }

        if (direction.sqrMagnitude < 0.0001f)
            direction = -movementDirection;

        direction.Normalize();

        ApplyAlleyLaunch(
            direction,
            false);
    }

    // ============================================================
    // EXTERNAL ALLEY HIT
    // ============================================================

    public void ReceiveExternalFlyHit(Vector3 direction)
    {
        // An Impacted Alley can NEVER be reactivated.
        if (state != AlleyState.Active ||
            isDead)
        {
            return;
        }

        if (!TryCommitImpact())
            return;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            direction = -movementDirection;
        else
            direction.Normalize();

        ApplyAlleyLaunch(
            direction,
            true);

        ScheduleDeathAfterRecoil();

        Log("EXTERNAL ALLEY HIT | IMPACT LOCKED");
    }

    private void ApplyAlleyLaunch(
        Vector3 direction,
        bool alreadyImpactLocked)
    {
        if (state == AlleyState.Dead ||
            isDead ||
            cachedRigidbody == null)
        {
            return;
        }

        // A launch from an Impacted Alley must never restore collision.
        if (state == AlleyState.Impacted)
        {
            DisableGameplayCollision();
        }
        else if (!alreadyImpactLocked)
        {
            // Defensive fallback: launches caused by gameplay impact
            // must commit the state before physics.
            if (!TryCommitImpact())
                return;
        }

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            direction = -movementDirection;
        else
            direction.Normalize();

        isFlying = true;

        float requestedEndTime =
            Time.time +
            alleyRecoilDuration;

        if (requestedEndTime > flyEndTime)
            flyEndTime = requestedEndTime;

        cachedRigidbody.isKinematic = false;
        cachedRigidbody.useGravity = true;
        cachedRigidbody.detectCollisions = false;

        cachedRigidbody.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;

        Vector3 velocity =
            direction * alleyRecoilForce;

        velocity.y =
            alleyRecoilUpwardForce;

        cachedRigidbody.linearVelocity =
            velocity;

        if (alleyFlyTorque > 0f)
        {
            Vector3 torqueAxis =
                Vector3.Cross(
                    Vector3.up,
                    direction);

            if (torqueAxis.sqrMagnitude < 0.0001f)
                torqueAxis = Vector3.right;
            else
                torqueAxis.Normalize();

            cachedRigidbody.AddTorque(
                torqueAxis * alleyFlyTorque,
                ForceMode.VelocityChange);
        }
    }

    private void UpdateFlyState()
    {
        if (!isFlying ||
            state == AlleyState.Dead)
        {
            return;
        }

        if (Time.time < flyEndTime)
            return;

        isFlying = false;

        if (cachedRigidbody == null)
        {
            KillSelf();
            return;
        }

        cachedRigidbody.linearVelocity = Vector3.zero;
        cachedRigidbody.angularVelocity = Vector3.zero;

        // Do NOT restore gameplay collision.
        // The Alley is already Impacted and is on its way to death.
        KillSelf();
    }

    private void ScheduleDeathAfterRecoil()
    {
        if (state == AlleyState.Dead)
            return;

        state = AlleyState.Impacted;

        DisableGameplayCollision();

        float duration =
            Mathf.Max(
                alleyRecoilDuration,
                playerKnockbackDuration);

        if (duration <= 0f)
        {
            KillSelf();
            return;
        }

        float endTime =
            Time.time + duration;

        if (endTime > flyEndTime)
            flyEndTime = endTime;

        isFlying = true;
    }

    // ============================================================
    // VEHICLE DETECTION
    // ============================================================

    private bool IsAmbulance(Collider other)
    {
        if (other == null)
            return false;

        return other.GetComponentInParent<AmbulanceController>() != null;
    }

    private bool IsPlayer(Collider other)
    {
        if (other == null)
            return false;

        if (other.CompareTag("Player"))
            return true;

        Transform root = other.transform.root;

        return root != null &&
               root.CompareTag("Player");
    }

    private bool IsObstacle(Collider other)
    {
        if (other == null)
            return false;

        if (string.IsNullOrEmpty(obstacleTag))
            return false;

        if (other.CompareTag(obstacleTag))
            return true;

        Rigidbody rb = other.attachedRigidbody;

        if (rb != null &&
            rb.CompareTag(obstacleTag))
        {
            return true;
        }

        Transform parent = other.transform.parent;

        while (parent != null)
        {
            if (parent.CompareTag(obstacleTag))
                return true;

            parent = parent.parent;
        }

        Transform root = other.transform.root;

        return root != null &&
               root.CompareTag(obstacleTag);
    }

    private GameObject FindObstacleRoot(Collider other)
    {
        if (other == null)
            return null;

        Rigidbody rb = other.attachedRigidbody;

        if (rb != null)
            return rb.gameObject;

        Transform root = other.transform.root;

        return root != null
            ? root.gameObject
            : other.gameObject;
    }

    // ============================================================
    // IMPACT FEEDBACK
    // ============================================================

    private void PlayImpactFeedback(Vector3 impactPoint)
    {
        PlayImpactAudio();
        SpawnImpactExplosion(impactPoint);
    }

    private void PlayImpactAudio()
    {
        if (impactAudioSource == null ||
            impactSounds == null ||
            impactSounds.Length == 0)
        {
            return;
        }

        int index =
            Random.Range(
                0,
                impactSounds.Length);

        AudioClip clip =
            impactSounds[index];

        if (clip == null)
            return;

        impactAudioSource.PlayOneShot(
            clip,
            impactVolume);
    }

    private void SpawnImpactExplosion(Vector3 impactPoint)
    {
        if (impactExplosionPrefab == null)
            return;

        Vector3 spawnPosition =
            impactPoint +
            impactVfxOffset;

        Quaternion rotation =
            Quaternion.LookRotation(
                movementDirection,
                Vector3.up);

        GameObject effect =
            Instantiate(
                impactExplosionPrefab,
                spawnPosition,
                rotation);

        if (effect == null)
            return;

        float lifetime =
            GetParticleLifetime(effect);

        Destroy(
            effect,
            lifetime > 0f
                ? lifetime
                : 3f);
    }

    private float GetParticleLifetime(GameObject effect)
    {
        if (effect == null)
            return 0f;

        ParticleSystem[] particles =
            effect.GetComponentsInChildren<ParticleSystem>(true);

        float maxLifetime = 0f;

        for (int i = 0; i < particles.Length; i++)
        {
            ParticleSystem particle = particles[i];

            if (particle == null)
                continue;

            ParticleSystem.MainModule main =
                particle.main;

            float total =
                main.duration +
                main.startLifetime.constantMax;

            if (total > maxLifetime)
                maxLifetime = total;
        }

        return maxLifetime;
    }

    // ============================================================
    // COLLIDER CACHE
    // ============================================================

    private void CacheColliders()
    {
        if (cachedColliders != null &&
            cachedColliders.Length > 0)
        {
            return;
        }

        cachedColliders =
            GetComponentsInChildren<Collider>(true);
    }

    // ============================================================
    // DEATH
    // ============================================================

    private void KillSelf()
    {
        if (state == AlleyState.Dead ||
            isDead)
        {
            return;
        }

        state = AlleyState.Dead;
        isDead = true;
        isFlying = false;

        DisableGameplayCollision();

        if (cachedRigidbody != null && !cachedRigidbody.isKinematic)
        {
            cachedRigidbody.linearVelocity = Vector3.zero;
            cachedRigidbody.angularVelocity = Vector3.zero;
        }

        Destroy(gameObject);
    }

    // ============================================================
    // DEBUG
    // ============================================================

    private void Log(string message)
    {
        if (!debugLogs)
            return;

        Debug.Log(
            "[AlleyAmbushVehicle] " +
            message,
            this);
    }
}
