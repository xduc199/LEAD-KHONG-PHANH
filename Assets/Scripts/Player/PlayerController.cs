using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Honk & Pressure Settings")]
    [SerializeField] private KeyCode honkKey = KeyCode.H;
    [SerializeField] private float honkRange = 18f;
    [SerializeField] private float honkRadius = 3.5f;

    [Header("Anti-Spam Horn Settings")]
    [SerializeField] private int maxSpamCount = 5;
    [SerializeField] private float timeWindow = 2.0f;
    [SerializeField] private float brokenDuration = 4.0f;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip honkClip;
    [SerializeField] private AudioClip brokenHornClip;

    [Header("Iced Coffee Boost Settings")]
    [SerializeField] private float boostMultiplier = 2f;
    [SerializeField] private float boostDuration = 5.0f;

    private bool isCoffeeBoosted = false;
    private float coffeeTimer = 0f;
    private float originalHorizontalSpeed;

    [Header("Photon Reference")]
    [SerializeField] private PhotonController photonController;

    [Header("Speed Settings")]
    [SerializeField] private float baseSpeed = 15f;
    [SerializeField] private float maxSpeed = 35f;
    [SerializeField] private float speedIncreaseRate = 0.02f;

    [Header("Movement Settings")]
    [SerializeField] private float horizontalSpeed = 12f;
    [SerializeField] private float minX = -4.5f;
    [SerializeField] private float maxX = 4.5f;

    [Header("Rotation & Leaning Settings")]
    [SerializeField] private float maxTurnAngle = 20f;
    [SerializeField] private float maxLeanAngle = 15f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Jump & Ramp Settings")]
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float gravity = 30f;

    private float verticalVelocity = 0f;
    private bool isGrounded = true;
    private float groundY = 0f;

    [Header("Knockback & Physics Settings")]
    [Tooltip("Lực văng lùi khi tông xe ngược chiều / chướng ngại vật trước mắt")]
    [SerializeField] private float oncomingKnockbackZ = -12f;

    [Tooltip("Lực văng tới khi bị Exciter đâm từ phía sau")]
    [SerializeField] private float exciterKnockbackZ = 12f;

    [Tooltip("Lực hất nảy lên cao")]
    [SerializeField] private float upwardKnockbackY = 4.5f;

    [Tooltip("Ma sát sau va chạm để xe dừng lại gọn gàng")]
    [SerializeField] private float knockbackDrag = 2.0f;

    [Header("Explosion Settings")]
    [Tooltip("Bỏ tích (false) để TẮT NỔ khi Player vs Xe ngược chiều hoặc Exciter vs Player")]
    [SerializeField] private bool enableExplosionAnimation = false;

    [Tooltip("Prefab nổ 3D Timeframe")]
    [SerializeField] private GameObject explosionEffectPrefab;

    public static bool EnableExplosionStatic = false;
    public static GameObject ExplosionEffectPrefabStatic;

    private float currentSpeed;
    private bool isDead = false;
    private Rigidbody rb;

    private List<float> honkTimestamps =
        new List<float>();

    private bool isHornBrokenState = false;

    private void Awake()
    {
        SyncExplosionStaticVars();

        if (photonController == null)
        {
            photonController =
                GetComponent<PhotonController>();
        }
    }

    private void Start()
    {
        currentSpeed = baseSpeed;

        groundY =
            transform.position.y;

        rb =
            GetComponent<Rigidbody>();

        if (audioSource == null)
        {
            audioSource =
                GetComponent<AudioSource>();
        }

        originalHorizontalSpeed =
            horizontalSpeed;
    }

    private void Update()
    {
        SyncExplosionStaticVars();

        if (isDead)
            return;

        //=======================================================
        // COFFEE BOOST
        //=======================================================

        if (isCoffeeBoosted)
        {
            coffeeTimer -=
                Time.deltaTime;

            if (coffeeTimer <= 0f)
            {
                isCoffeeBoosted = false;

                horizontalSpeed =
                    originalHorizontalSpeed;
            }
        }

        //=======================================================
        // HONK
        //=======================================================

        if (
            !isHornBrokenState &&
            Input.GetKeyDown(honkKey)
        )
        {
            TryHonk();
        }

        //=======================================================
        // NORMAL SPEED
        //=======================================================

        float rawSpeed =
            Mathf.Min(
                baseSpeed +
                (
                    transform.position.z *
                    speedIncreaseRate
                ),
                maxSpeed
            );

        //=======================================================
        // PHOTON SPEED
        //=======================================================

        float speedMultiplier = 1f;

        if (
            photonController != null &&
            photonController.IsPhotonActive
        )
        {
            speedMultiplier =
                photonController
                    .PhotonSpeedMultiplier;
        }

        currentSpeed =
            rawSpeed *
            speedMultiplier;

        float forwardMove =
            currentSpeed *
            Time.deltaTime;

        //=======================================================
        // HORIZONTAL
        //=======================================================

        float xInput =
            Input.GetAxis("Horizontal");

        float horizontalMove =
            xInput *
            horizontalSpeed *
            Time.deltaTime;

        Vector3 newPosition =
            transform.position +
            new Vector3(
                horizontalMove,
                0f,
                forwardMove
            );

        newPosition.x =
            Mathf.Clamp(
                newPosition.x,
                minX,
                maxX
            );

        //=======================================================
        // AIR
        //=======================================================

        if (!isGrounded)
        {
            verticalVelocity -=
                gravity *
                Time.deltaTime;

            newPosition.y +=
                verticalVelocity *
                Time.deltaTime;

            if (newPosition.y <= groundY)
            {
                newPosition.y =
                    groundY;

                isGrounded = true;

                verticalVelocity = 0f;
            }
        }

        transform.position =
            newPosition;

        HandleRotation(xInput);
    }

    private void SyncExplosionStaticVars()
    {
        EnableExplosionStatic =
            enableExplosionAnimation;

        ExplosionEffectPrefabStatic =
            explosionEffectPrefab;
    }

    private void HandleRotation(
        float xInput
    )
    {
        float targetYRotation =
            xInput *
            maxTurnAngle;

        float targetZRotation =
            -xInput *
            maxLeanAngle;

        Quaternion targetRotation =
            Quaternion.Euler(
                0f,
                targetYRotation,
                targetZRotation
            );

        transform.rotation =
            Quaternion.Lerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime *
                rotationSpeed
            );
    }

    //=========================================================
    // KNOCKBACK
    //=========================================================

    public void ApplyKnockback(
        Vector3 force
    )
    {
        // Photon đang bật -> Player không bị hất
        if (
            photonController != null &&
            photonController.IsPhotonActive
        )
        {
            return;
        }

        if (isDead)
            return;

        isDead = true;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;

            rb.linearDamping =
                knockbackDrag;

            rb.angularDamping =
                knockbackDrag;

            rb.AddForce(
                force,
                ForceMode.Impulse
            );

            rb.AddTorque(
                new Vector3(
                    -5f,
                    Random.Range(
                        -3f,
                        3f
                    ),
                    4f
                ),
                ForceMode.Impulse
            );
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
    }

    //=========================================================
    // COLLISION PROCESS
    //=========================================================

    private void ProcessObstacleCollision(
        GameObject obj
    )
    {
        // Photon xử lý riêng
        if (
            photonController != null &&
            photonController.IsPhotonActive
        )
        {
            photonController.HitObstacle(obj);
            return;
        }

        // Explosion bình thường
        if (
            enableExplosionAnimation &&
            explosionEffectPrefab != null
        )
        {
            Vector3 spawnPos =
                (
                    transform.position +
                    obj.transform.position
                ) * 0.5f;

            Instantiate(
                explosionEffectPrefab,
                spawnPos,
                Quaternion.identity
            );
        }

        bool isExciter =
            obj.name.Contains("Exciter");

        float zForce =
            isExciter
                ? exciterKnockbackZ
                : oncomingKnockbackZ;

        float randomX =
            Random.Range(
                -2.5f,
                2.5f
            );

        ApplyKnockback(
            new Vector3(
                randomX,
                upwardKnockbackY,
                zForce
            )
        );
    }

    //=========================================================
    // TRIGGER
    //=========================================================

    private void OnTriggerEnter(
        Collider other
    )
    {
        if (isDead)
            return;

        if (
            other.CompareTag("Coffee") ||
            other.name.Contains("Coffee") ||
            other.name.Contains("CaPhe")
        )
        {
            ActivateCoffeeBoost();

            Destroy(
                other.gameObject
            );

            return;
        }

        // Photon pickup
        if (
            other.CompareTag("Photon") ||
            other.CompareTag("PhotonItem") ||
            other.name.Contains("Photon") ||
            other.name.Contains("TocDoAnhSang")
        )
        {
            if (photonController != null)
            {
                photonController.ActivatePhoton();
            }

            Destroy(
                other.gameObject
            );

            return;
        }

        if (
            other.CompareTag("Ramp") ||
            other.name.Contains("Ramp") ||
            other.name.Contains("DocTon")
        )
        {
            isGrounded = false;

            verticalVelocity =
                jumpForce;

            return;
        }

        if (
            IsObstacle(
                other.gameObject
            )
        )
        {
            ProcessObstacleCollision(
                other.gameObject
            );
        }
    }

    //=========================================================
    // COLLISION
    //=========================================================

    private void OnCollisionEnter(
        Collision collision
    )
    {
        if (isDead)
            return;

        if (
            collision.gameObject.CompareTag(
                "Coffee"
            ) ||
            collision.gameObject.name.Contains(
                "Coffee"
            ) ||
            collision.gameObject.name.Contains(
                "CaPhe"
            )
        )
        {
            ActivateCoffeeBoost();

            Destroy(
                collision.gameObject
            );

            return;
        }

        if (
            collision.gameObject.CompareTag(
                "PhotonItem"
            ) ||
            collision.gameObject.CompareTag(
                "Photon"
            ) ||
            collision.gameObject.name.Contains(
                "Photon"
            ) ||
            collision.gameObject.name.Contains(
                "TocDoAnhSang"
            )
        )
        {
            if (photonController != null)
            {
                photonController.ActivatePhoton();
            }

            Destroy(
                collision.gameObject
            );

            return;
        }

        if (
            IsObstacle(
                collision.gameObject
            )
        )
        {
            ProcessObstacleCollision(
                collision.gameObject
            );
        }
    }

    //=========================================================
    // OBSTACLE CHECK
    //=========================================================

    private bool IsObstacle(
        GameObject obj
    )
    {
        string objName =
            obj.name;

        return
            obj.CompareTag("Obstacle") ||
            objName.Contains("Car") ||
            objName.Contains("BaGac_Body") ||
            objName.Contains("Oncoming") ||
            objName.Contains("Exciter");
    }

    //=========================================================
    // COFFEE
    //=========================================================

    public void ActivateCoffeeBoost()
    {
        isCoffeeBoosted = true;

        coffeeTimer =
            boostDuration;

        horizontalSpeed =
            originalHorizontalSpeed *
            boostMultiplier;
    }

    //=========================================================
    // HONK
    //=========================================================

    public void TryHonk()
    {
        if (isHornBrokenState)
            return;

        float currentTime =
            Time.time;

        honkTimestamps.Add(
            currentTime
        );

        honkTimestamps.RemoveAll(
            t =>
                currentTime - t >
                timeWindow
        );

        if (
            honkTimestamps.Count >=
            maxSpamCount
        )
        {
            StartCoroutine(
                BreakHornRoutine()
            );

            return;
        }

        ExecuteHonkPressure();
    }

    private void ExecuteHonkPressure()
    {
        if (
            audioSource != null &&
            honkClip != null
        )
        {
            audioSource.PlayOneShot(
                honkClip
            );
        }

        Vector3 origin =
            transform.position;

        RaycastHit[] hits =
            Physics.SphereCastAll(
                origin,
                honkRadius,
                transform.forward,
                honkRange
            );

        foreach (
            RaycastHit hit
            in hits
        )
        {
            GameObject obj =
                hit.collider.gameObject;

            string objName =
                obj.name.ToLower();

            if (
                objName.Contains("player") ||
                objName.Contains("road") ||
                objName.Contains("ground") ||
                objName.Contains("coin") ||
                objName.Contains("ramp")
            )
            {
                continue;
            }

            TrafficCarBehavior carScript =
                obj.GetComponentInParent<
                    TrafficCarBehavior
                >();

            if (carScript == null)
            {
                carScript =
                    obj.GetComponent<
                        TrafficCarBehavior
                    >();
            }

            if (carScript != null)
            {
                carScript.TriggerPanicLaneChange(
                    transform.position
                );
            }
        }
    }

    //=========================================================
    // BROKEN HORN
    //=========================================================

    private IEnumerator BreakHornRoutine()
    {
        isHornBrokenState = true;

        if (
            audioSource != null &&
            brokenHornClip != null
        )
        {
            audioSource.PlayOneShot(
                brokenHornClip
            );
        }

        yield return new WaitForSeconds(
            brokenDuration
        );

        isHornBrokenState = false;

        honkTimestamps.Clear();
    }
}