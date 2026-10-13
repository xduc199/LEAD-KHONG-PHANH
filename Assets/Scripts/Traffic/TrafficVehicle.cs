using System.Collections;
using UnityEngine;

public class TrafficVehicle : MonoBehaviour
{
    [Header("Speed")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float minimumSpeed = 5f;
    [Tooltip("Traffic không chạy nhanh hơn Player quá nhiều.")]
    [SerializeField] private float playerSpeedMargin = 1f;

    [Header("Movement")]
    [SerializeField] private bool moveForward = true;
    [Tooltip("Độ cao Y cố định của Traffic Vehicle khi đang chạy.")]
    [SerializeField] private float vehicleHeightY = 0f;

    private Transform playerTransform;
    private float estimatedPlayerSpeed = 15f;
    private float lastPlayerZ;
    private bool playerSpeedInitialized;

    private float baseSpeed;
    private bool temporarySpeedActive;
    private float temporarySpeed;

    private float desiredLaneX;
    private bool desiredLaneXInitialized;

    [Header("Vehicle Knockback")]
    [SerializeField] private float knockbackDuration = 2.5f;
    [SerializeField] private float destroyDelay = 0.25f;
    [SerializeField] private float spinForce = 18f;
    [Tooltip("Khi xe bị hất, tắt Collider ngay lập tức.")]
    [SerializeField] private bool disableCollidersWhenKnocked = true;

    [Header("Photon Knockback")]
    [SerializeField] private float photonUpwardVelocity = 20f;
    [SerializeField] private float photonForwardVelocity = 12f;
    [SerializeField] private float photonSideVelocity = 4f;

    private bool isKnocked;
    private Rigidbody rb;
    private Collider[] vehicleColliders;
    private Coroutine knockbackRoutine;

    public float MoveSpeed => moveSpeed;
    public float CurrentSpeed => moveSpeed;
    public float BaseSpeed => baseSpeed;
    public int TravelDirection => moveForward ? 1 : -1;
    public bool IsKnockedByPhoton => isKnocked;
    public bool IsDead => isKnocked;

    // Single source of truth for TrafficCarBehavior.
    public float PhysicsPositionX =>
        rb != null ? rb.position.x : transform.position.x;

    public float PhysicsPositionY =>
        rb != null ? rb.position.y : transform.position.y;

    public float PhysicsPositionZ =>
        rb != null ? rb.position.z : transform.position.z;

    public float VehicleHeightY => vehicleHeightY;

    private void Awake()
    {
        baseSpeed = Mathf.Max(minimumSpeed, moveSpeed);

        rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();

        vehicleColliders =
            GetComponentsInChildren<Collider>(true);

        FindPlayer();
        PrepareRigidbody();

        Vector3 initialPosition = rb.position;
        initialPosition.y = vehicleHeightY;

        if (IsFinite(initialPosition))
            rb.position = initialPosition;

        desiredLaneX = rb.position.x;
        desiredLaneXInitialized = true;

        Physics.SyncTransforms();
    }

    private void Start()
    {
        FindPlayer();

        if (playerTransform != null)
        {
            lastPlayerZ = playerTransform.position.z;
            playerSpeedInitialized = true;
        }

        if (rb != null)
        {
            desiredLaneX = rb.position.x;
            desiredLaneXInitialized = true;
        }
    }

    private void Update()
    {
        if (isKnocked)
            return;

        if (playerTransform == null)
            FindPlayer();

        UpdatePlayerSpeedEstimate();
        UpdateCurrentSpeed();
    }

    private void FixedUpdate()
    {
        if (isKnocked)
            return;

        MovePhysics();
    }

    private void PrepareRigidbody()
    {
        if (rb == null)
            return;

        rb.isKinematic = true;
        rb.useGravity = false;
        rb.detectCollisions = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode =
            CollisionDetectionMode.ContinuousSpeculative;
    }

    private void MovePhysics()
    {
        if (rb == null)
            return;

        Vector3 current = rb.position;
        float direction = moveForward ? 1f : -1f;

        Vector3 next = current;

        if (desiredLaneXInitialized)
            next.x = desiredLaneX;

        // Y is controlled only by vehicleHeightY while alive.
        next.y = vehicleHeightY;

        next.z +=
            direction *
            moveSpeed *
            Time.fixedDeltaTime;

        if (!IsFinite(next))
            return;

        rb.MovePosition(next);
    }

    private static bool IsFinite(Vector3 value)
    {
        return
            float.IsFinite(value.x) &&
            float.IsFinite(value.y) &&
            float.IsFinite(value.z);
    }

    public void SetDesiredLaneX(float x)
    {
        if (!float.IsFinite(x))
            return;

        desiredLaneX = x;
        desiredLaneXInitialized = true;
    }

    public void SetImmediateLaneX(float x)
    {
        if (!float.IsFinite(x))
            return;

        desiredLaneX = x;
        desiredLaneXInitialized = true;

        if (rb == null)
        {
            Vector3 fallback = transform.position;
            fallback.x = x;
            fallback.y = vehicleHeightY;
            transform.position = fallback;
            return;
        }

        Vector3 position = rb.position;
        position.x = x;
        position.y = vehicleHeightY;

        if (!IsFinite(position))
            return;

        rb.position = position;
        Physics.SyncTransforms();
    }

    public void SetVehicleHeightY(float heightY)
    {
        if (!float.IsFinite(heightY))
            return;

        vehicleHeightY = heightY;

        if (isKnocked || rb == null)
            return;

        Vector3 position = rb.position;
        position.y = vehicleHeightY;

        if (!IsFinite(position))
            return;

        rb.position = position;
        Physics.SyncTransforms();
    }

    private void FindPlayer()
    {
        if (playerTransform != null)
            return;

        GameObject player =
            GameObject.FindGameObjectWithTag("Player");

        if (player == null)
            return;

        playerTransform = player.transform;
        lastPlayerZ = playerTransform.position.z;
        playerSpeedInitialized = true;
    }

    private void UpdatePlayerSpeedEstimate()
    {
        if (playerTransform == null)
            return;

        float currentZ = playerTransform.position.z;

        if (!playerSpeedInitialized)
        {
            lastPlayerZ = currentZ;
            playerSpeedInitialized = true;
            return;
        }

        float delta = currentZ - lastPlayerZ;
        float measuredSpeed =
            delta / Mathf.Max(Time.deltaTime, 0.001f);

        if (measuredSpeed > 0f && measuredSpeed < 60f)
        {
            estimatedPlayerSpeed =
                Mathf.Lerp(
                    estimatedPlayerSpeed,
                    measuredSpeed,
                    Time.deltaTime * 4f
                );
        }

        lastPlayerZ = currentZ;
    }

    private void UpdateCurrentSpeed()
    {
        float targetSpeed = baseSpeed;

        if (temporarySpeedActive)
            targetSpeed = temporarySpeed;

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

    public void SetMoveSpeed(float speed)
    {
        baseSpeed = Mathf.Max(minimumSpeed, speed);

        if (!temporarySpeedActive)
            moveSpeed = baseSpeed;
    }

    public void SetTemporarySpeed(float speed)
    {
        temporarySpeed = Mathf.Max(minimumSpeed, speed);
        temporarySpeedActive = true;
    }

    public void RestoreBaseSpeed()
    {
        temporarySpeedActive = false;
        temporarySpeed = baseSpeed;
        moveSpeed = baseSpeed;
    }

    public float GetBaseSpeed() => baseSpeed;
    public float GetMoveSpeed() => moveSpeed;

    public void SetTravelDirection(bool forward)
    {
        moveForward = forward;
    }

    public void SetTravelDirection(int direction)
    {
        moveForward = direction >= 0;
    }

    public void ApplyPhotonKnockback(Vector3 force)
    {
        if (isKnocked)
            return;

        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();

        isKnocked = true;

        TrafficCarBehavior behavior =
            GetComponent<TrafficCarBehavior>();

        if (behavior != null)
            behavior.enabled = false;

        DisableVehicleColliders();

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.detectCollisions = true;
        rb.constraints = RigidbodyConstraints.None;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;

        rb.linearDamping = 0.35f;
        rb.angularDamping = 0.45f;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        float sideVelocity =
            Mathf.Clamp(
                force.x,
                -photonSideVelocity,
                photonSideVelocity
            );

        float forwardVelocity =
            Mathf.Max(
                Mathf.Abs(force.z),
                photonForwardVelocity
            );

        float upwardVelocity =
            Mathf.Max(
                Mathf.Abs(force.y),
                photonUpwardVelocity
            );

        rb.linearVelocity =
            new Vector3(
                sideVelocity,
                upwardVelocity,
                forwardVelocity
            );

        rb.AddForce(
            Vector3.forward *
            forwardVelocity *
            0.25f,
            ForceMode.Impulse
        );

        rb.AddForce(
            Vector3.up *
            upwardVelocity *
            0.15f,
            ForceMode.Impulse
        );

        Vector3 torque =
            new Vector3(
                Random.Range(-spinForce, spinForce),
                Random.Range(-spinForce, spinForce),
                Random.Range(-spinForce, spinForce)
            );

        rb.AddTorque(torque, ForceMode.Impulse);

        if (knockbackRoutine != null)
            StopCoroutine(knockbackRoutine);

        knockbackRoutine =
            StartCoroutine(KnockbackRoutine());
    }

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

        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();

        isKnocked = true;

        TrafficCarBehavior behavior =
            GetComponent<TrafficCarBehavior>();

        if (behavior != null)
            behavior.enabled = false;

        enabled = false;
        DisableVehicleColliders();

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.detectCollisions = true;
        rb.constraints = RigidbodyConstraints.None;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;

        rb.linearDamping =
            Mathf.Max(0f, linearDamping);

        rb.angularDamping =
            Mathf.Max(0f, angularDamping);

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        float horizontalX = force.x;
        float upwardY = Mathf.Max(Mathf.Abs(force.y), 32f);
        float forwardZ = Mathf.Abs(force.z);

        rb.linearVelocity =
            new Vector3(
                horizontalX,
                upwardY,
                forwardZ
            );

        rb.AddForce(
            Vector3.up *
            upwardY *
            0.35f,
            ForceMode.Impulse
        );

        rb.angularVelocity = Vector3.zero;

        if (allowRotation)
        {
            Vector3 torque =
                new Vector3(
                    Random.Range(-torqueForce, torqueForce),
                    Random.Range(-torqueForce, torqueForce),
                    Random.Range(-torqueForce, torqueForce)
                );

            rb.AddTorque(torque, ForceMode.Impulse);
        }

        LaunchCleanup cleanup =
            GetComponent<LaunchCleanup>();

        if (cleanup == null)
            cleanup =
                gameObject.AddComponent<LaunchCleanup>();

        cleanup.Initialize(rb, physicsLifetime);
    }

    private void DisableVehicleColliders()
    {
        if (!disableCollidersWhenKnocked)
            return;

        if (vehicleColliders == null)
            return;

        for (int i = 0; i < vehicleColliders.Length; i++)
        {
            Collider collider = vehicleColliders[i];

            if (collider != null)
                collider.enabled = false;
        }
    }

    private IEnumerator KnockbackRoutine()
    {
        yield return new WaitForSeconds(knockbackDuration);

        if (destroyDelay > 0f)
            yield return new WaitForSeconds(destroyDelay);

        Destroy(gameObject);
    }

    private void OnDisable()
    {
        isKnocked = false;

        if (knockbackRoutine != null)
        {
            StopCoroutine(knockbackRoutine);
            knockbackRoutine = null;
        }
    }
}
