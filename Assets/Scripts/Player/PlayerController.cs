using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    //=========================================================
    // PHOTON
    //=========================================================

    [Header("Photon Reference")]
    [SerializeField] private PhotonController photonController;


    //=========================================================
    // SHIELD
    //=========================================================

    [Header("Shield Reference")]
    [SerializeField] private ShieldController shieldController;


    //=========================================================
    // FORWARD SPEED
    //=========================================================

    [Header("Forward Speed Settings")]

    [Tooltip("Tốc độ chạy ban đầu.")]
    [SerializeField] private float baseSpeed = 15f;

    [Tooltip("Tốc độ chạy tối đa.")]
    [SerializeField] private float maxSpeed = 35f;

    [Tooltip("Tốc độ tăng theo quãng đường.")]
    [SerializeField] private float speedIncreaseRate = 0.02f;


    //=========================================================
    // HORIZONTAL / LANE MOVEMENT
    //=========================================================

    [Header("Lane Movement Settings")]

    [Tooltip("Tốc độ chuyển trái/phải bình thường.")]
    [SerializeField] private float horizontalSpeed = 12f;

    [Tooltip("Tốc độ chuyển trái/phải khi ăn Gum.")]
    [SerializeField] private float gumHorizontalSpeed = 30f;

    [Tooltip("Giới hạn vị trí trái.")]
    [SerializeField] private float minX = -4.5f;

    [Tooltip("Giới hạn vị trí phải.")]
    [SerializeField] private float maxX = 4.5f;


  

    private float currentHorizontalSpeed;


    //=========================================================
    // SHIELD HIT FLASH
    //=========================================================

    [Header("Shield Hit Flash")]

    [Tooltip("Bật hiệu ứng Player nhấp nháy khi Shield chặn va chạm.")]
    [SerializeField] private bool enableShieldFlash = true;

    [Tooltip("Tổng thời gian hiệu ứng nhấp nháy.")]
    [SerializeField] private float shieldFlashDuration = 0.45f;

    [Tooltip("Khoảng thời gian giữa mỗi lần bật/tắt Renderer.")]
    [SerializeField] private float shieldFlashInterval = 0.06f;

    [Tooltip("Số lần nhấp nháy.")]
    [SerializeField] private int shieldFlashCount = 4;

    [Tooltip("Nếu bật, tìm Renderer trong toàn bộ Player hierarchy.")]
    [SerializeField] private bool includeChildRenderers = true;

    private Renderer[] playerRenderers;

    private Coroutine shieldFlashCoroutine;


    //=========================================================
    // ROTATION
    //=========================================================

    [Header("Rotation & Leaning Settings")]

    [SerializeField] private float maxTurnAngle = 20f;

    [SerializeField] private float maxLeanAngle = 15f;

    [SerializeField] private float rotationSpeed = 10f;


    //=========================================================
    // JUMP / BA GAC RAMP
    //=========================================================

    [Header("Jump & Ramp Settings")]

    [Tooltip("Bật/tắt hệ thống ramp Ba Gác.")]
    [SerializeField] private bool enableRamp = true;

    [Tooltip("Vận tốc bay thẳng đứng cơ bản sau khi rời ramp.")]
    [SerializeField] private float jumpForce = 12f;

    [Tooltip("Gravity khi Player đang bay.")]
    [SerializeField] private float gravity = 30f;


    //=========================================================
    // LEGACY RAMP SETTINGS
    //=========================================================

    [Header("Ba Gac Ramp - Legacy Settings")]

    [Tooltip(
        "Giữ lại để tương thích Inspector cũ. " +
        "Chiều dài thực tế sẽ lấy từ RampStart -> RampEnd."
    )]
    [SerializeField] private float rampLength = 4.0f;

    [Tooltip(
        "Giữ lại để tương thích Inspector cũ. " +
        "Độ cao thực tế sẽ lấy từ collider mặt tôn."
    )]
    [SerializeField] private float rampClimbHeight = 1.35f;

    [Tooltip(
        "Giữ lại để tương thích Inspector cũ. " +
        "Rotation thực tế sẽ lấy từ normal của mặt tôn."
    )]
    [SerializeField] private float rampPitchAngle = 10f;

    [Tooltip(
        "Giữ lại để tương thích Inspector cũ."
    )]
    [SerializeField] private float rampClimbSmoothness = 10f;


    //=========================================================
    // PHYSICAL RAMP SETTINGS
    //=========================================================

    [Header("Ba Gac Ramp - Physical Surface")]

    [Tooltip(
        "Khoảng cách từ mặt tôn đến pivot/center của Player."
    )]
    [SerializeField] private float rampPlayerSurfaceOffset = 0.45f;

    [Tooltip(
        "Chiều cao bắt đầu raycast tìm mặt tôn."
    )]
    [SerializeField] private float rampRaycastHeight = 2.5f;

    [Tooltip(
        "Khoảng cách raycast xuống tìm collider mặt tôn."
    )]
    [SerializeField] private float rampRaycastDistance = 5f;

    [Tooltip(
        "Khoảng cách nhỏ tránh cho Player bị chui vào collider."
    )]
    [SerializeField] private float rampSurfaceSkin = 0.02f;

    [Tooltip(
        "Nếu không tìm được collider mặt tôn thì dùng marker làm fallback."
    )]
    [SerializeField] private bool allowRampMarkerFallback = true;

    [Tooltip(
        "Log thông tin surface ramp khi bắt đầu."
    )]
    [SerializeField] private bool rampDebug = true;


    //=========================================================
    // RAMP LAUNCH
    //=========================================================

    [Header("Ba Gac Ramp - Launch")]

    [Tooltip(
        "Vận tốc bay thêm theo tốc độ hiện tại của Player."
    )]
    [SerializeField] private float rampSpeedToLaunchBonus = 0.18f;

    [Tooltip(
        "Giới hạn vận tốc bay thẳng đứng tối đa."
    )]
    [SerializeField] private float rampMaxLaunchVelocity = 18f;

    [Tooltip(
        "Tăng thêm lực phóng sau khi đi hết ramp."
    )]
    [SerializeField] private float rampLaunchBonus = 1.5f;

    [Tooltip(
        "Thời gian tối thiểu giữa hai lần kích hoạt ramp."
    )]
    [SerializeField] private float rampRetriggerCooldown = 0.35f;

    [Header("Ba Gac Ramp - Climb Speed")]

    [Tooltip(
        "Tốc độ cộng thêm khi Player đang leo ramp."
    )]
    [SerializeField] private float rampClimbSpeedBonus = 3f;
    //=========================================================
    // RAMP AIR CONTROL
    //=========================================================

    [Header("Ba Gac Ramp - Air Control")]

    [Tooltip(
        "Khả năng điều khiển trái/phải khi đang bay."
    )]
    [Range(0f, 1f)]
    [SerializeField] private float rampAirControl = 0.35f;


    //=========================================================
    // RAMP LANDING
    //=========================================================

    [Header("Ba Gac Ramp - Landing")]

    [Tooltip(
        "Khoảng cách tối đa cho phép Player rơi xuống dưới groundY trước khi tự đáp."
    )]
    [SerializeField] private float rampLandingTolerance = 0.05f;


    //=========================================================
    // RAMP COLLISION PROTECTION
    //=========================================================

    [Header("Ba Gac Ramp - Launch Protection")]

    [Tooltip(
        "Bảo vệ cực ngắn sau launch để Player không chết ngay khi vừa bay qua Traffic."
    )]
    [SerializeField] private float rampLaunchCollisionProtection = 0.40f;


    //=========================================================
    // RAMP INTERNAL STATE
    //=========================================================

    private enum RampState
    {
        None,
        Climbing,
        Launching,
        Airborne
    }

    private RampState rampState = RampState.None;

    private float rampProgress;

    private float rampStartY;

    private float rampTargetY;

    private float rampCooldownTimer;

    private Collider activeRampCollider;

    private Collider activeRampSurfaceCollider;

    private Transform activeRampRoot;

    private Transform activeRampStartPoint;

    private Transform activeRampEndPoint;

    private Vector3 activeRampStartLocal;

    private Vector3 activeRampEndLocal;

    private Vector3 activeRampDirectionLocal;

    private float activeRampLength;

    private float rampDistanceTravelled;

    private Vector3 lastRampPlayerLocal;

    private bool usingRealRampPoints;

    private Vector3 activeRampLastSurfaceNormal =
        Vector3.up;

    private Vector3 activeRampLastSurfaceTangent =
        Vector3.forward;

    private float rampCollisionProtectionTimer;

    // True khi Player dang IgnoreCollision voi Ba Gac hien tai.
    private bool rampPhysicsCollisionIgnored;

    private float verticalVelocity;

    private bool isGrounded = true;

    private float groundY;


    //=========================================================
    // RIGIDBODY RAMP STATE
    //=========================================================

    private bool rampRigidbodyStateSaved;

    private bool rampOriginalKinematic;

    private bool rampOriginalUseGravity;


    //=========================================================
// KNOCKBACK & IMPACT PHYSICS
//=========================================================

[Header("Knockback & Impact Physics")]

[Tooltip("Lực hất ngang cơ bản.")]
[SerializeField] private float knockbackForce = 22f;

[Tooltip("Lực hất lên.")]
[SerializeField] private float upwardKnockbackY = 6.5f;

[Tooltip("Lực xoay xe khi va chạm.")]
[SerializeField] private float knockbackRollTorque = 5f;

[Tooltip("Lực xoay nhẹ quanh trục Y.")]
[SerializeField] private float knockbackYawTorque = 1f;

[Tooltip("Damping của Rigidbody sau khi chết.")]
[SerializeField] private float knockbackDrag = 1.5f;

[Tooltip("Damping xoay sau khi chết.")]
[SerializeField] private float knockbackAngularDrag = 3f;

[Tooltip("Nhân lực khi va chạm mạnh.")]
[SerializeField] private float strongImpactMultiplier = 1.35f;

[Tooltip("Nhân lực khi va chạm nhẹ.")]
[SerializeField] private float lightImpactMultiplier = 0.75f;

[Tooltip("Tìm BoxCollider của Player trong toàn bộ hierarchy.")]
[SerializeField] private bool findPlayerBoxColliderInChildren = true;

[Tooltip("Bật toàn bộ debug collision/knockback.")]
[SerializeField] private bool knockbackDebug = true;

[Tooltip(
    "Hiển thị Debug.DrawRay hướng knockback trong Scene View."
)]
[SerializeField] private bool knockbackDrawDebugRay = true;

[Tooltip(
    "Độ dài ray debug hướng knockback."
)]
[SerializeField] private float knockbackDebugRayLength = 3f;

private BoxCollider playerBoxCollider;

    //=========================================================
    // DEATH WORLD SAFETY
    //=========================================================

    [Header("Death World Safety")]

    [Tooltip(
        "Khi Player chết, vẫn cho Rigidbody hất văng bình thường " +
        "nhưng không cho Player rơi vô hạn khỏi thế giới."
    )]
    [SerializeField] private bool preventDeathFall = true;

    [Tooltip(
        "Khoảng cách Player được phép rơi xuống dưới mặt đất " +
        "trước khi hệ thống giữ lại."
    )]
    [SerializeField] private float deathFallOffset = 0.15f;


    //=========================================================
    // SHIELD COLLISION PROTECTION
    //=========================================================

    [Header("Shield Collision Protection")]

    [Tooltip("Thời gian miễn nhiễm sau khi Shield block.")]
    [SerializeField] private float shieldHitCooldown = 0.30f;

    [Tooltip("Đẩy Player về phía trước khi Shield block.")]
    [SerializeField] private float shieldBlockPushForward = 1.5f;

    [Tooltip("Đẩy Player sang ngang khi Shield block.")]
    [SerializeField] private float shieldBlockPushHorizontal = 0.8f;

    private float shieldHitCooldownTimer;


    //=========================================================
    // SHIELD BLOCK STATE
    //=========================================================

    private bool isShieldBlockingHit;


    //=========================================================
    // EXPLOSION
    //=========================================================

    [Header("Explosion Settings")]

    [SerializeField] private bool enableExplosionAnimation = false;

    [SerializeField] private GameObject explosionEffectPrefab;

    public static bool EnableExplosionStatic = false;

    public static GameObject ExplosionEffectPrefabStatic;


    //=========================================================
    // PLAYER AUDIO
    //=========================================================

    [Header("Player Engine Audio")]

    [Tooltip("Âm thanh động cơ loop.")]
    [SerializeField] private AudioClip engineClip;

    [Tooltip("Âm lượng động cơ.")]
    [Range(0f, 1f)]
    [SerializeField] private float engineVolume = 0.65f;

    [Tooltip("Âm lượng tối thiểu của engine.")]
    [Range(0f, 1f)]
    [SerializeField] private float engineMinVolume = 0.35f;

    [Tooltip("Pitch thấp nhất.")]
    [SerializeField] private float engineMinPitch = 0.85f;

    [Tooltip("Pitch cao nhất.")]
    [SerializeField] private float engineMaxPitch = 1.35f;

    [Tooltip("Tốc độ thay đổi pitch.")]
    [SerializeField] private float enginePitchSmooth = 5f;

    [Tooltip("Engine là 3D hay 2D.")]
    [Range(0f, 1f)]
    [SerializeField] private float engineSpatialBlend = 0.35f;

    [SerializeField] private float engineMinDistance = 3f;

    [SerializeField] private float engineMaxDistance = 30f;



    //=========================================================
    // SHIELD AUDIO
    //=========================================================

    [Header("Shield Block Audio")]

    [SerializeField] private AudioClip shieldBlockSound;

    [Range(0f, 1f)]
    [SerializeField] private float shieldBlockVolume = 0.9f;


    //=========================================================
    // AUDIO SETTINGS
    //=========================================================

    [Header("Audio Advanced Settings")]

    [Tooltip("AudioSource engine.")]
    [SerializeField] private AudioSource engineAudioSource;

    [Tooltip("AudioSource one-shot.")]
    [SerializeField] private AudioSource sfxAudioSource;

    [SerializeField] private bool autoCreateAudioSources = true;


    //=========================================================
    // INTERNAL
    //=========================================================

    private float currentSpeed;

    private bool isDead;

    private Rigidbody rb;

    private Coroutine impactGameOverCoroutine;

    //=========================================================
    // GAMEPLAY UI API
    //=========================================================

    public float CurrentSpeed
    {
        get
        {
            return currentSpeed;
        }
    }

    public float CurrentForwardSpeed
    {
        get
        {
            return currentSpeed;
        }
    }

    public float BaseSpeed
    {
        get
        {
            return baseSpeed;
        }
    }

    public float MaxSpeed
    {
        get
        {
            return maxSpeed;
        }
    }

    public float RampClimbSpeedBonus
    {
        get
        {
            if (rampState != RampState.Climbing)
            {
                return 0f;
            }

            return Mathf.Max(
                0f,
                rampClimbSpeedBonus
            );
        }
    }

    public bool IsDead
    {
        get
        {
            return isDead;
        }
    }

    public bool IsGrounded
    {
        get
        {
            return isGrounded;
        }
    }


    //=========================================================
// GUM PUBLIC API
//=========================================================

public bool IsGumBoosted
{
    get
    {
        return
            ItemManager.Instance != null &&
            ItemManager.Instance.IsGumActive;
    }
}

public float GumTimeRemaining
{
    get
    {
        if (ItemManager.Instance == null)
            return 0f;

        return ItemManager.Instance.GumTimeRemaining;
    }
}

public float CurrentHorizontalSpeed
{
    get
    {
        return currentHorizontalSpeed;
    }
}


    //=========================================================
    // AWAKE
    //=========================================================

    private void Awake()
    {
        SyncExplosionStaticVars();

        if (photonController == null)
        {
            photonController =
                GetComponent<PhotonController>();
        }

        FindShieldController();

        rb =
            GetComponent<Rigidbody>();

        CachePlayerRenderers();

        FindPlayerBoxCollider();

        SetupPlayerAudio();

        if (knockbackDebug)
        {
            Debug.Log(
                "[PlayerController] " +
                "KNOCKBACK DEBUG READY | " +
                "Player = " +
                gameObject.name +
                " | Rigidbody = " +
                (rb != null ? "FOUND" : "NULL") +
                " | BoxCollider = " +
                (
                    playerBoxCollider != null
                        ? playerBoxCollider.name
                        : "NULL"
                )
            );
        }
    }


    //=========================================================
    // START
    //=========================================================

    private void Start()
    {
        currentSpeed =
            baseSpeed;

        groundY =
            transform.position.y;

       currentHorizontalSpeed =
    Mathf.Max(
        0f,
        horizontalSpeed
    );

StartEngineAudio();
    }


    //=========================================================
    // UPDATE
    //=========================================================

    private void Update()
    {
        SyncExplosionStaticVars();

        UpdateShieldHitCooldown();

        UpdateRampTimers();

        if (isDead)
        {
            StopEngineAudio();

            return;
        }

        UpdateGum();

        UpdateMovement();

        UpdateEngineAudio();
    }


    //=========================================================
    // RAMP TIMERS
    //=========================================================

    private void UpdateRampTimers()
    {
        if (rampCooldownTimer > 0f)
        {
            rampCooldownTimer -=
                Time.deltaTime;

            if (rampCooldownTimer < 0f)
            {
                rampCooldownTimer = 0f;
            }
        }

        if (rampCollisionProtectionTimer > 0f)
        {
            rampCollisionProtectionTimer -=
                Time.deltaTime;

            if (rampCollisionProtectionTimer < 0f)
            {
                rampCollisionProtectionTimer = 0f;
            }
        }
    }


    //=========================================================
    // FIXED UPDATE
    //=========================================================

    private void FixedUpdate()
    {
        if (!isDead)
            return;

        PreventDeathFallOutOfWorld();
    }


    //=========================================================
    // DEATH WORLD SAFETY
    //=========================================================

    private void PreventDeathFallOutOfWorld()
    {
        if (!preventDeathFall)
            return;

        if (rb == null)
            return;

        float minimumDeathY =
            groundY -
            Mathf.Max(
                0f,
                deathFallOffset
            );

        if (
            transform.position.y >=
            minimumDeathY
        )
        {
            return;
        }

        Vector3 position =
            rb.position;

        position.y =
            minimumDeathY;

        rb.position =
            position;

        Vector3 velocity =
            rb.linearVelocity;

        if (velocity.y < 0f)
        {
            velocity.y =
                0f;

            rb.linearVelocity =
                velocity;
        }
    }


    //=========================================================
    // UPDATE SHIELD COOLDOWN
    //=========================================================

    private void UpdateShieldHitCooldown()
    {
        if (shieldHitCooldownTimer > 0f)
        {
            shieldHitCooldownTimer -=
                Time.deltaTime;

            if (shieldHitCooldownTimer <= 0f)
            {
                shieldHitCooldownTimer = 0f;

                isShieldBlockingHit = false;
            }
        }
    }


    //=========================================================
    // AUDIO SETUP
    //=========================================================

    private void SetupPlayerAudio()
    {
        if (engineAudioSource == null)
        {
            engineAudioSource =
                GetComponent<AudioSource>();
        }

        if (
            engineAudioSource == null &&
            autoCreateAudioSources
        )
        {
            engineAudioSource =
                gameObject.AddComponent<AudioSource>();
        }

        if (engineAudioSource != null)
        {
            engineAudioSource.clip =
                engineClip;

            engineAudioSource.loop =
                true;

            engineAudioSource.playOnAwake =
                false;

            engineAudioSource.volume =
                engineVolume;

            engineAudioSource.pitch =
                engineMinPitch;

            engineAudioSource.spatialBlend =
                engineSpatialBlend;

            engineAudioSource.minDistance =
                engineMinDistance;

            engineAudioSource.maxDistance =
                engineMaxDistance;

            engineAudioSource.dopplerLevel =
                0f;

            engineAudioSource.rolloffMode =
                AudioRolloffMode.Logarithmic;
        }

        if (sfxAudioSource == null)
        {
            AudioSource[] sources =
                GetComponents<AudioSource>();

            if (sources.Length > 1)
            {
                sfxAudioSource =
                    sources[1];
            }
        }

        if (
            sfxAudioSource == null &&
            autoCreateAudioSources
        )
        {
            sfxAudioSource =
                gameObject.AddComponent<AudioSource>();
        }

        if (sfxAudioSource != null)
        {
            sfxAudioSource.playOnAwake =
                false;

            sfxAudioSource.loop =
                false;

            sfxAudioSource.spatialBlend =
                0f;

            sfxAudioSource.dopplerLevel =
                0f;
        }
    }


    //=========================================================
    // START ENGINE
    //=========================================================

    private void StartEngineAudio()
    {
        if (engineAudioSource == null)
            return;

        if (engineClip == null)
            return;

        engineAudioSource.clip =
            engineClip;

        engineAudioSource.volume =
            engineVolume;

        engineAudioSource.pitch =
            engineMinPitch;

        if (!engineAudioSource.isPlaying)
        {
            engineAudioSource.Play();
        }
    }


    //=========================================================
    // STOP ENGINE
    //=========================================================

    private void StopEngineAudio()
    {
        if (engineAudioSource == null)
            return;

        if (engineAudioSource.isPlaying)
        {
            engineAudioSource.Stop();
        }
    }


    //=========================================================
    // UPDATE ENGINE AUDIO
    //=========================================================

    private void UpdateEngineAudio()
    {
        if (engineAudioSource == null)
            return;

        if (engineClip == null)
            return;

        if (!engineAudioSource.isPlaying)
        {
            engineAudioSource.Play();
        }

        float normalSpeed =
            Mathf.InverseLerp(
                baseSpeed,
                maxSpeed,
                Mathf.Clamp(
                    currentSpeed,
                    baseSpeed,
                    maxSpeed
                )
            );

        float targetPitch =
            Mathf.Lerp(
                engineMinPitch,
                engineMaxPitch,
                normalSpeed
            );

        engineAudioSource.pitch =
            Mathf.Lerp(
                engineAudioSource.pitch,
                targetPitch,
                enginePitchSmooth *
                Time.deltaTime
            );

        float targetVolume =
            Mathf.Lerp(
                engineMinVolume,
                engineVolume,
                normalSpeed
            );

        engineAudioSource.volume =
            Mathf.Lerp(
                engineAudioSource.volume,
                targetVolume,
                enginePitchSmooth *
                Time.deltaTime
            );
    }


    //=========================================================
    // PLAY SFX
    //=========================================================

    private void PlayPlayerSFX(
        AudioClip clip,
        float volume
    )
    {
        if (clip == null)
            return;

        if (sfxAudioSource == null)
            return;

        sfxAudioSource.PlayOneShot(
            clip,
            Mathf.Clamp01(volume)
        );
    }


    //=========================================================
// GUM UPDATE
//=========================================================

private void UpdateGum()
{
    if (ItemManager.Instance == null)
    {
        currentHorizontalSpeed =
            horizontalSpeed;

        return;
    }

    if (ItemManager.Instance.IsGumActive)
    {
        currentHorizontalSpeed =
            ItemManager.Instance.GumHorizontalSpeed;
    }
    else
    {
        currentHorizontalSpeed =
            horizontalSpeed;
    }
}


    //=========================================================
    // MOVEMENT
    //=========================================================

    private void UpdateMovement()
    {
        float rawSpeed =
            Mathf.Min(
                baseSpeed +
                transform.position.z *
                speedIncreaseRate,
                maxSpeed
            );

        float speedMultiplier = 1f;

        if (
            photonController != null &&
            photonController.IsPhotonActive
        )
        {
            speedMultiplier =
                photonController.PhotonSpeedMultiplier;
        }

        currentSpeed =
            rawSpeed *
            speedMultiplier;

        //=====================================================
        // BA GAC RAMP CLIMB SPEED
        //
        // Bonus được cộng SAU speedMultiplier và SAU maxSpeed
        // của tốc độ thường.
        //
        // Ví dụ:
        // Normal = 35
        // Ramp Bonus = 3
        // => Ramp Climb = 38
        //
        // Không sửa rawSpeed/maxSpeed để tránh ảnh hưởng
        // gameplay bình thường.
        //=====================================================

        float activeRampSpeedBonus = 0f;

        if (
            rampState ==
            RampState.Climbing
        )
        {
            activeRampSpeedBonus =
                Mathf.Max(
                    0f,
                    rampClimbSpeedBonus
                );

            currentSpeed +=
                activeRampSpeedBonus;
        }

        if (
            rampDebug &&
            rampState ==
            RampState.Climbing
        )
        {
            Debug.Log(
                "[PlayerController] " +
                "RAMP CLIMB SPEED | " +
                "Normal = " +
                (currentSpeed -
                 activeRampSpeedBonus).ToString("F2") +
                " | Bonus = " +
                activeRampSpeedBonus.ToString("F2") +
                " | Final = " +
                currentSpeed.ToString("F2")
            );
        }

        float xInput =
            Input.GetAxisRaw(
                "Horizontal"
            );

        float horizontalControlMultiplier =
            1f;

        if (
            rampState ==
            RampState.Airborne
        )
        {
            horizontalControlMultiplier =
                rampAirControl;
        }

        float horizontalMove =
            xInput *
            currentHorizontalSpeed *
            horizontalControlMultiplier *
            Time.deltaTime;

        float forwardMove =
            currentSpeed *
            Time.deltaTime;

        Vector3 newPosition =
            transform.position;

        newPosition.x +=
            horizontalMove;

        newPosition.z +=
            forwardMove;

        newPosition.x =
            Mathf.Clamp(
                newPosition.x,
                minX,
                maxX
            );


        //=====================================================
        // RAMP CLIMB
        //=====================================================

        if (
            rampState ==
            RampState.Climbing
        )
        {
            UpdateRampClimb(
                ref newPosition
            );
        }


        //=====================================================
        // NORMAL AIRBORNE
        //=====================================================

        else if (
            rampState ==
            RampState.Airborne
        )
        {
            UpdateAirborne(
                ref newPosition
            );
        }


        //=====================================================
        // NORMAL GROUND
        //=====================================================

        else if (isGrounded)
        {
            newPosition.y =
                groundY;

            verticalVelocity =
                0f;
        }

        transform.position =
            newPosition;

        HandleRotation(
            xInput
        );
    }


    //=========================================================
    // RAMP SETUP
    //=========================================================

    private bool SetupRampPoints()
    {
        if (
            activeRampCollider == null
        )
        {
            return false;
        }

        Transform root =
            activeRampCollider.transform;

        Transform candidate =
            null;

        while (
            root != null
        )
        {
            Transform start =
                FindChildByName(
                    root,
                    "RampStart"
                );

            Transform end =
                FindChildByName(
                    root,
                    "RampEnd"
                );

            if (
                start != null &&
                end != null
            )
            {
                candidate =
                    root;

                activeRampStartPoint =
                    start;

                activeRampEndPoint =
                    end;

                break;
            }

            root =
                root.parent;
        }

        if (candidate == null)
        {
            Debug.LogWarning(
                "[PlayerController] " +
                "BA GAC RAMP SETUP FAILED | " +
                "Cannot find RampStart + RampEnd."
            );

            return false;
        }

        activeRampRoot =
            candidate;

        activeRampStartLocal =
            activeRampRoot.InverseTransformPoint(
                activeRampStartPoint.position
            );

        activeRampEndLocal =
            activeRampRoot.InverseTransformPoint(
                activeRampEndPoint.position
            );

        Vector3 flatDirection =
            activeRampEndLocal -
            activeRampStartLocal;

        flatDirection.y = 0f;

        activeRampLength =
            flatDirection.magnitude;

        if (
            activeRampLength <=
            0.05f
        )
        {
            Debug.LogWarning(
                "[PlayerController] " +
                "BA GAC RAMP SETUP FAILED | " +
                "Ramp length too small."
            );

            return false;
        }

        activeRampDirectionLocal =
            flatDirection.normalized;

        if (
            Vector3.Dot(
                activeRampDirectionLocal,
                activeRampRoot.forward
            ) < 0f
        )
        {
            activeRampDirectionLocal =
                -activeRampDirectionLocal;
        }

        usingRealRampPoints =
            true;

        rampDistanceTravelled =
            0f;

        rampProgress =
            0f;

        lastRampPlayerLocal =
            activeRampRoot.InverseTransformPoint(
                transform.position
            );

        activeRampSurfaceCollider =
            FindRampSurfaceCollider(
                activeRampRoot
            );

        if (rampDebug)
        {
            Debug.Log(
                "[PlayerController] " +
                "BA GAC RAMP SETUP OK | " +
                "Root = " +
                activeRampRoot.name +
                " | Length = " +
                activeRampLength.ToString("F2") +
                " | Surface = " +
                (
                    activeRampSurfaceCollider != null
                        ?
                        activeRampSurfaceCollider.name
                        :
                        "NONE"
                )
            );
        }

        return true;
    }


    //=========================================================
    // FIND CHILD BY NAME
    //=========================================================

    private Transform FindChildByName(
        Transform root,
        string targetName
    )
    {
        if (root == null)
            return null;

        Transform[] children =
            root.GetComponentsInChildren<Transform>(
                true
            );

        for (
            int i = 0;
            i < children.Length;
            i++
        )
        {
            Transform child =
                children[i];

            if (
                string.Equals(
                    child.name,
                    targetName,
                    System.StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return child;
            }
        }

        return null;
    }


    //=========================================================
    // FIND RAMP SURFACE COLLIDER
    //=========================================================

    private Collider FindRampSurfaceCollider(
        Transform rampRoot
    )
    {
        if (rampRoot == null)
            return null;

        Collider[] colliders =
            rampRoot.GetComponentsInChildren<Collider>(
                true
            );

        Collider best =
            null;

        float bestScore =
            float.MinValue;

        for (
            int i = 0;
            i < colliders.Length;
            i++
        )
        {
            Collider candidate =
                colliders[i];

            if (candidate == null)
                continue;

            if (candidate.isTrigger)
                continue;

            if (
                candidate.transform.IsChildOf(
                    transform
                ) ||
                transform.IsChildOf(
                    candidate.transform
                )
            )
            {
                continue;
            }

            string lower =
                candidate.name.ToLower();

            float score = 0f;

            if (
                lower.Contains("ramp")
            )
            {
                score += 100f;
            }

            if (
                lower.Contains("tin")
            )
            {
                score += 80f;
            }

            if (
                lower.Contains("sheet")
            )
            {
                score += 60f;
            }

            if (
                lower.Contains("surface")
            )
            {
                score += 40f;
            }

            Bounds bounds =
                candidate.bounds;

            score +=
                bounds.size.x *
                bounds.size.y *
                bounds.size.z *
                0.01f;

            if (
                candidate ==
                activeRampCollider
            )
            {
                score -= 50f;
            }

            if (
                score >
                bestScore
            )
            {
                bestScore =
                    score;

                best =
                    candidate;
            }
        }

        return best;
    }


    //=========================================================
    // GET RAMP TANGENT
    //=========================================================

    private Vector3 GetRampTangentWorld()
    {
        if (
            activeRampRoot == null
        )
        {
            return transform.forward;
        }

        Vector3 startWorld =
            activeRampRoot.TransformPoint(
                activeRampStartLocal
            );

        Vector3 endWorld =
            activeRampRoot.TransformPoint(
                activeRampEndLocal
            );

        Vector3 tangent =
            endWorld -
            startWorld;

        if (
            tangent.sqrMagnitude <=
            0.0001f
        )
        {
            return transform.forward;
        }

        tangent.Normalize();

        if (
            Vector3.Dot(
                tangent,
                transform.forward
            ) < 0f
        )
        {
            tangent =
                -tangent;
        }

        return tangent;
    }


    //=========================================================
    // RAMP SURFACE RAYCAST
    //=========================================================

    private bool TryGetRampSurfaceHit(
        Vector3 position,
        out RaycastHit hit
    )
    {
        hit =
            default;

        if (
            activeRampSurfaceCollider == null
        )
        {
            return false;
        }

        Vector3 origin =
            position +
            Vector3.up *
            rampRaycastHeight;

        Vector3 direction =
            Vector3.down;

        float distance =
            Mathf.Max(
                0.1f,
                rampRaycastHeight +
                rampRaycastDistance
            );

        if (
            activeRampSurfaceCollider.Raycast(
                new Ray(
                    origin,
                    direction
                ),
                out hit,
                distance
            )
        )
        {
            return true;
        }

        return false;
    }


    //=========================================================
    // GET SURFACE ROTATION
    //=========================================================

    private Quaternion GetRampSurfaceRotation(
        Vector3 normal
    )
    {
        Vector3 rampForward =
            GetRampTangentWorld();

        if (
            rampForward.sqrMagnitude <=
            0.0001f
        )
        {
            rampForward =
                transform.forward;
        }

        rampForward.Normalize();

        normal.Normalize();

        Vector3 surfaceForward =
            Vector3.ProjectOnPlane(
                rampForward,
                normal
            );

        if (
            surfaceForward.sqrMagnitude <=
            0.0001f
        )
        {
            surfaceForward =
                Vector3.ProjectOnPlane(
                    transform.forward,
                    normal
                );
        }

        if (
            surfaceForward.sqrMagnitude <=
            0.0001f
        )
        {
            surfaceForward =
                Vector3.forward;
        }

        surfaceForward.Normalize();

        if (
            Vector3.Dot(
                surfaceForward,
                transform.forward
            ) < 0f
        )
        {
            surfaceForward =
                -surfaceForward;
        }

        Quaternion surfaceRotation =
            Quaternion.LookRotation(
                surfaceForward,
                normal
            );

        return surfaceRotation;
    }

//=========================================================
// RAMP CLIMB
//=========================================================

private void UpdateRampClimb(
    ref Vector3 newPosition
)
{
    if (!enableRamp)
    {
        CancelRamp();

        newPosition.y =
            groundY;

        return;
    }

    if (
        activeRampRoot == null ||
        !usingRealRampPoints
    )
    {
        CancelRamp();

        newPosition.y =
            groundY;

        return;
    }


    //=====================================================
    // MEASURE REAL PLAYER TRAVEL
    //=====================================================

    Vector3 currentLocal =
        activeRampRoot.InverseTransformPoint(
            newPosition
        );

    Vector3 deltaLocal =
        currentLocal -
        lastRampPlayerLocal;

    Vector3 flatDelta =
        deltaLocal;

    flatDelta.y =
        0f;

    float travelledThisFrame =
        Vector3.Dot(
            flatDelta,
            activeRampDirectionLocal
        );

    if (
        travelledThisFrame >
        0f
    )
    {
        rampDistanceTravelled +=
            travelledThisFrame;
    }

    lastRampPlayerLocal =
        currentLocal;

    rampDistanceTravelled =
        Mathf.Clamp(
            rampDistanceTravelled,
            0f,
            activeRampLength
        );

    rampProgress =
        Mathf.Clamp01(
            rampDistanceTravelled /
            Mathf.Max(
                0.01f,
                activeRampLength
            )
        );


    //=====================================================
    // FOLLOW REAL RAMP SURFACE
    //=====================================================

    RaycastHit surfaceHit;

    bool hasSurface =
        TryGetRampSurfaceHit(
            newPosition,
            out surfaceHit
        );

    if (hasSurface)
    {
        activeRampLastSurfaceNormal =
            surfaceHit.normal;

        activeRampLastSurfaceTangent =
            Vector3.ProjectOnPlane(
                GetRampTangentWorld(),
                surfaceHit.normal
            ).normalized;

        newPosition.y =
            surfaceHit.point.y +
            rampPlayerSurfaceOffset +
            rampSurfaceSkin;
    }
    else if (
        allowRampMarkerFallback
    )
    {
        Vector3 startWorld =
            activeRampRoot.TransformPoint(
                activeRampStartLocal
            );

        Vector3 endWorld =
            activeRampRoot.TransformPoint(
                activeRampEndLocal
            );

        Vector3 markerPosition =
            Vector3.Lerp(
                startWorld,
                endWorld,
                rampProgress
            );

        newPosition.y =
            markerPosition.y +
            rampPlayerSurfaceOffset;
    }


    //=====================================================
    // RAMP COMPLETE
    //=====================================================

    if (
        rampProgress >=
        0.999f
    )
    {
        StartRampLaunch(
            ref newPosition
        );
    }
}

    //=========================================================
// START RAMP LAUNCH
//=========================================================

private void StartRampLaunch(
    ref Vector3 newPosition
)
{
    if (
        rampState !=
        RampState.Climbing
    )
    {
        return;
    }


    //=====================================================
    // FINAL SURFACE POSITION
    //=====================================================

    RaycastHit finalHit;

    if (
        TryGetRampSurfaceHit(
            newPosition,
            out finalHit
        )
    )
    {
        newPosition.y =
            finalHit.point.y +
            rampPlayerSurfaceOffset +
            rampSurfaceSkin;

        activeRampLastSurfaceNormal =
            finalHit.normal;
    }


    //=====================================================
    // LAUNCH VELOCITY
    //=====================================================

    float speedBonus =
        Mathf.Max(
            0f,
            currentSpeed -
            baseSpeed
        ) *
        rampSpeedToLaunchBonus;

    float launchVelocity =
        jumpForce +
        rampLaunchBonus +
        speedBonus;

    launchVelocity =
        Mathf.Clamp(
            launchVelocity,
            0f,
            rampMaxLaunchVelocity
        );

    verticalVelocity =
        launchVelocity;


    //=====================================================
    // CHANGE STATE
    //=====================================================

    rampState =
        RampState.Airborne;

    isGrounded =
        false;


    //=====================================================
    // RAMP COOLDOWN
    //=====================================================

    rampCooldownTimer =
        Mathf.Max(
            0f,
            rampRetriggerCooldown
        );


    //=====================================================
    // RESTORE BA GAC COLLISION
    //
    // Player chỉ Ignore Ba Gac trong lúc Climbing.
    // Khi rời ramp phải bật collision lại ngay.
    //=====================================================

    RestoreActiveRampCollisions();


    //=====================================================
    // POST-LAUNCH COLLISION PROTECTION
    //=====================================================

    rampCollisionProtectionTimer =
        Mathf.Max(
            0f,
            rampLaunchCollisionProtection
        );


    //=====================================================
    // CLEAR ACTIVE RAMP TRIGGER
    //=====================================================

    activeRampCollider =
        null;


    //=====================================================
    // DEBUG
    //=====================================================

    if (rampDebug)
    {
        Debug.Log(
            "[PlayerController] " +
            "BA GAC RAMP COMPLETE | " +
            "Distance = " +
            rampDistanceTravelled.ToString("F2") +
            "/" +
            activeRampLength.ToString("F2") +
            " | Launch = " +
            verticalVelocity.ToString("F2")
        );
    }
}

    //=========================================================
    // AIRBORNE
    //=========================================================

    private void UpdateAirborne(
        ref Vector3 newPosition
    )
    {
        verticalVelocity -=
            gravity *
            Time.deltaTime;

        newPosition.y +=
            verticalVelocity *
            Time.deltaTime;

        float landingY =
            groundY +
            rampLandingTolerance;

        if (
            newPosition.y <=
            landingY
        )
        {
            newPosition.y =
                groundY;

            verticalVelocity =
                0f;

            isGrounded =
                true;

            rampState =
                RampState.None;

            rampProgress =
                0f;

            rampDistanceTravelled =
                0f;

            activeRampCollider =
                null;

            activeRampSurfaceCollider =
                null;

            activeRampRoot =
                null;

            activeRampStartPoint =
                null;

            activeRampEndPoint =
                null;

            usingRealRampPoints =
                false;

            rampCollisionProtectionTimer =
                0f;
        }
    }

    //=========================================================
// RAMP COLLISION CONTROL
//=========================================================

private void IgnoreActiveRampCollisions()
{
    if (
        activeRampRoot == null
    )
    {
        return;
    }

    Collider[] playerColliders =
        GetComponentsInChildren<Collider>(
            true
        );

    Collider[] rampColliders =
        activeRampRoot.root.GetComponentsInChildren<Collider>(
            true
        );

    bool ignoredAnyCollision = false;

    for (int i = 0; i < playerColliders.Length; i++)
    {
        Collider playerCollider =
            playerColliders[i];

        if (playerCollider == null)
            continue;

        for (int j = 0; j < rampColliders.Length; j++)
        {
            Collider rampCollider =
                rampColliders[j];

            if (rampCollider == null)
                continue;

            // Chi tat collision vat ly. RampTrigger van giu Trigger.
            if (rampCollider.isTrigger)
                continue;

            if (playerCollider == rampCollider)
                continue;

            if (
                rampCollider.transform.IsChildOf(
                    transform
                )
            )
            {
                continue;
            }

            Physics.IgnoreCollision(
                playerCollider,
                rampCollider,
                true
            );

            ignoredAnyCollision = true;
        }
    }

    rampPhysicsCollisionIgnored =
        ignoredAnyCollision;

    if (rampDebug)
    {
        Debug.Log(
            "[PlayerController] " +
            "RAMP PHYSICS COLLISION IGNORED | " +
            "Root = " +
            activeRampRoot.root.name
        );
    }
}


//=========================================================
// RESTORE RAMP COLLISION
//=========================================================

private void RestoreActiveRampCollisions()
{
    if (
        !rampPhysicsCollisionIgnored
    )
    {
        return;
    }

    if (
        activeRampRoot == null
    )
    {
        rampPhysicsCollisionIgnored =
            false;

        return;
    }

    Collider[] playerColliders =
        GetComponentsInChildren<Collider>(
            true
        );

    Collider[] rampColliders =
        activeRampRoot.root.GetComponentsInChildren<Collider>(
            true
        );

    for (int i = 0; i < playerColliders.Length; i++)
    {
        Collider playerCollider =
            playerColliders[i];

        if (playerCollider == null)
            continue;

        for (int j = 0; j < rampColliders.Length; j++)
        {
            Collider rampCollider =
                rampColliders[j];

            if (rampCollider == null)
                continue;

            if (rampCollider.isTrigger)
                continue;

            if (playerCollider == rampCollider)
                continue;

            if (
                rampCollider.transform.IsChildOf(
                    transform
                )
            )
            {
                continue;
            }

            Physics.IgnoreCollision(
                playerCollider,
                rampCollider,
                false
            );
        }
    }

    rampPhysicsCollisionIgnored =
        false;

    if (rampDebug)
    {
        Debug.Log(
            "[PlayerController] " +
            "RAMP PHYSICS COLLISION RESTORED | " +
            "Root = " +
            activeRampRoot.root.name
        );
    }
}

    //=========================================================
    // BEGIN RAMP
    //=========================================================

    private void BeginRamp(
        Collider rampCollider
    )
    {
        if (!enableRamp)
            return;

        if (
            rampCollider == null
        )
        {
            return;
        }

        if (
            rampCooldownTimer >
            0f
        )
        {
            return;
        }

        if (!isGrounded)
        {
            return;
        }

        if (
            rampState !=
            RampState.None
        )
        {
            return;
        }

        activeRampCollider =
            rampCollider;

        activeRampRoot =
            null;

        activeRampSurfaceCollider =
            null;

        usingRealRampPoints =
            false;

        if (!SetupRampPoints())
        {
            activeRampCollider =
                null;

            return;
        }

        // Set Climbing BEFORE disabling physical collision.
        // PhotonController can receive its collision callback independently
        // in the same physics step. The state must already be Climbing.
        rampState =
            RampState.Climbing;

        // Player bam theo RampSurface bang Raycast,
        // nen trong luc Climbing khong de collision vat ly cua Ba Gac can xe.
        IgnoreActiveRampCollisions();

        rampProgress =
            0f;

        rampDistanceTravelled =
            0f;

        rampStartY =
            transform.position.y;

        rampTargetY =
            rampStartY;

        verticalVelocity =
            0f;

        isGrounded =
            false;

        lastRampPlayerLocal =
            activeRampRoot.InverseTransformPoint(
                transform.position
            );

        if (rampDebug)
        {
            Debug.Log(
                "[PlayerController] " +
                "BA GAC RAMP START | " +
                "Root = " +
                activeRampRoot.name +
                " | Progress = 0.00 | Distance = 0/" +
                activeRampLength.ToString("F2")
            );
        }
    }


    //=========================================================
    // CANCEL RAMP
    //=========================================================

   private void CancelRamp()
{
    RestoreActiveRampCollisions();

    rampState =
        RampState.None;

    rampProgress =
        0f;

    rampDistanceTravelled =
        0f;

    verticalVelocity =
        0f;

    isGrounded =
        true;

    activeRampCollider =
        null;

    activeRampSurfaceCollider =
        null;

    activeRampRoot =
        null;

    activeRampStartPoint =
        null;

    activeRampEndPoint =
        null;

    usingRealRampPoints =
        false;
}


    //=========================================================
    // ROTATION
    //=========================================================

    private void HandleRotation(
        float xInput
    )
    {
        float targetY =
            xInput *
            maxTurnAngle;

        float targetZ =
            -xInput *
            maxLeanAngle;

        if (
            rampState ==
            RampState.Climbing
        )
        {
            Vector3 normal =
                activeRampLastSurfaceNormal;

            if (
                normal.sqrMagnitude <=
                0.001f
            )
            {
                normal =
                    Vector3.up;
            }

            normal.Normalize();

            Vector3 rampForward =
                GetRampTangentWorld();

            rampForward =
                Vector3.ProjectOnPlane(
                    rampForward,
                    normal
                );

            if (
                rampForward.sqrMagnitude <=
                0.001f
            )
            {
                rampForward =
                    transform.forward;

                rampForward =
                    Vector3.ProjectOnPlane(
                        rampForward,
                        normal
                    );
            }

            rampForward.Normalize();

            if (
                Vector3.Dot(
                    rampForward,
                    transform.forward
                ) < 0f
            )
            {
                rampForward =
                    -rampForward;
            }

            Quaternion rampRotation =
                Quaternion.LookRotation(
                    rampForward,
                    normal
                );

            Quaternion laneRotation =
                Quaternion.Euler(
                    0f,
                    targetY,
                    targetZ
                );

            Quaternion targetRotation =
                rampRotation *
                laneRotation;

            transform.rotation =
                Quaternion.Lerp(
                    transform.rotation,
                    targetRotation,
                    Time.deltaTime *
                    rotationSpeed
                );

            return;
        }


        //=====================================================
        // AIRBORNE
        //=====================================================

        if (
            rampState ==
            RampState.Airborne
        )
        {
            float targetPitch =
                Mathf.Clamp(
                    -verticalVelocity *
                    0.22f,
                    -18f,
                    18f
                );

            Quaternion targetRotation =
                Quaternion.Euler(
                    targetPitch,
                    targetY,
                    targetZ
                );

            transform.rotation =
                Quaternion.Lerp(
                    transform.rotation,
                    targetRotation,
                    Time.deltaTime *
                    rotationSpeed
                );

            return;
        }


        //=====================================================
        // NORMAL
        //=====================================================

        Quaternion normalRotation =
            Quaternion.Euler(
                0f,
                targetY,
                targetZ
            );

        transform.rotation =
            Quaternion.Lerp(
                transform.rotation,
                normalRotation,
                Time.deltaTime *
                rotationSpeed
            );
    }


   
    //=========================================================
    // FIND SHIELD
    //=========================================================

    private void FindShieldController()
    {
        if (
            shieldController != null
        )
        {
            return;
        }

        shieldController =
            GetComponent<ShieldController>();

        if (
            shieldController != null
        )
        {
            return;
        }

        shieldController =
            GetComponentInChildren<
                ShieldController
            >(true);

        if (
            shieldController != null
        )
        {
            return;
        }

        shieldController =
            GetComponentInParent<
                ShieldController
            >();
    }


    //=========================================================
    // SHIELD BLOCK
    //=========================================================

    public bool TryConsumeShield(
        GameObject obstacle
    )
    {
        if (
            shieldController != null &&
            shieldController.IsInvulnerable()
        )
        {
            return true;
        }

        if (isShieldBlockingHit)
        {
            return true;
        }

        if (
            shieldHitCooldownTimer >
            0f
        )
        {
            isShieldBlockingHit =
                true;

            return true;
        }

        if (
            shieldController == null
        )
        {
            FindShieldController();
        }

        if (
            shieldController == null
        )
        {
            return false;
        }

        if (
            shieldController.IsInvulnerable()
        )
        {
            return true;
        }

        if (
            !shieldController.IsActive()
        )
        {
            return false;
        }

        bool blocked =
            shieldController.ConsumeShield();

        if (!blocked)
        {
            return false;
        }

        isShieldBlockingHit =
            true;

        shieldHitCooldownTimer =
            Mathf.Max(
                0.01f,
                shieldHitCooldown
            );

        PlayShieldFlash();

        PlayPlayerSFX(
            shieldBlockSound,
            shieldBlockVolume
        );

        PushAwayFromObstacle(
            obstacle
        );

        Debug.Log(
            "[PlayerController] " +
            "SHIELD BLOCKED DAMAGE | " +
            "Player WILL NOT DIE."
        );

        return true;
    }


    //=========================================================
    // PUSH PLAYER
    //=========================================================

    private void PushAwayFromObstacle(
        GameObject obstacle
    )
    {
        if (
            obstacle == null
        )
        {
            return;
        }

        Vector3 away =
            transform.position -
            obstacle.transform.position;

        away.y =
            0f;

        if (
            away.sqrMagnitude <
            0.01f
        )
        {
            away =
                -transform.forward;
        }

        away.Normalize();

        Vector3 push =
            away *
            shieldBlockPushHorizontal;

        push +=
            transform.forward *
            shieldBlockPushForward;

        Vector3 position =
            transform.position;

        position +=
            push;

        position.x =
            Mathf.Clamp(
                position.x,
                minX,
                maxX
            );

        transform.position =
            position;
    }


    //=========================================================
    // APPLY LEGACY KNOCKBACK
    //=========================================================

    public void ApplyKnockback(
        Vector3 force
    )
    {
        if (knockbackDebug)
        {
            Debug.Log(
                "[PlayerController] " +
                "LEGACY ApplyKnockback CALLED | " +
                "Force = " +
                force
            );
        }

        if (isDead)
            return;

        if (
            rampCollisionProtectionTimer >
            0f
        )
        {
            return;
        }

        if (
            photonController != null &&
            photonController.IsPhotonActive
        )
        {
            return;
        }

        if (
            TryConsumeShield(null)
        )
        {
            return;
        }

        Vector3 horizontalForce =
            force;

        horizontalForce.y =
            0f;

        if (
            horizontalForce.sqrMagnitude >
            0.001f
        )
        {
            horizontalForce.Normalize();
        }
        else
        {
            horizontalForce =
                -transform.forward;
        }

        float horizontalMagnitude =
            new Vector3(
                force.x,
                0f,
                force.z
            ).magnitude;

        if (
            horizontalMagnitude <=
            0.001f
        )
        {
            horizontalMagnitude =
                knockbackForce;
        }

        Vector3 finalForce =
            horizontalForce *
            horizontalMagnitude;

        finalForce.y =
            force.y;

        KillPlayerWithImpact(
            finalForce,
            horizontalForce,
            1f
        );
    }

    //=========================================================
// EXCITER KNOCKBACK
//
// Dành riêng cho trường hợp EXCITER húc PLAYER.
//
// Mục tiêu:
// - Bay mạnh về phía trước
// - Bốc đầu
// - Xoay/lộn nhiều vòng
// - Không ảnh hưởng knockback thường
//=========================================================

public void ApplyExciterKnockback(
    Vector3 force
)
{
    if (knockbackDebug)
    {
        Debug.Log(
            "[PlayerController] " +
            "EXCITER KNOCKBACK CALLED | " +
            "Force = " +
            force
        );
    }

    if (isDead)
        return;

    if (
        rampCollisionProtectionTimer >
        0f
    )
    {
        if (knockbackDebug)
        {
            Debug.Log(
                "[PlayerController] " +
                "EXCITER KNOCKBACK BLOCKED | " +
                "Ramp protection."
            );
        }

        return;
    }

    if (
        photonController != null &&
        photonController.IsPhotonActive
    )
    {
        if (knockbackDebug)
        {
            Debug.Log(
                "[PlayerController] " +
                "EXCITER KNOCKBACK BLOCKED | " +
                "Photon active."
            );
        }

        return;
    }

    if (
        TryConsumeShield(null)
    )
    {
        if (knockbackDebug)
        {
            Debug.Log(
                "[PlayerController] " +
                "EXCITER KNOCKBACK BLOCKED | " +
                "Shield."
            );
        }

        return;
    }


    //=====================================================
    // PLAYER DEAD
    //=====================================================

    isDead =
        true;

    StopEngineAudio();


    //=====================================================
    // RAMP RESET
    //=====================================================

    rampState =
        RampState.None;

    activeRampCollider =
        null;

    activeRampSurfaceCollider =
        null;

    activeRampRoot =
        null;

    rampCollisionProtectionTimer =
        0f;


    //=====================================================
    // RESTORE RENDERERS
    //=====================================================

    if (
        shieldFlashCoroutine != null
    )
    {
        StopCoroutine(
            shieldFlashCoroutine
        );

        shieldFlashCoroutine =
            null;

        RestorePlayerRenderers();
    }


    //=====================================================
    // RIGIDBODY
    //=====================================================

    if (rb != null)
    {
        rb.isKinematic =
            false;

        rb.useGravity =
            true;

        rb.detectCollisions =
            true;

        rb.constraints =
            RigidbodyConstraints.None;

        rb.interpolation =
            RigidbodyInterpolation.Interpolate;

        rb.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;

        rb.linearDamping =
            0.12f;

        rb.angularDamping =
            0.12f;


        //=================================================
        // RESET
        //=================================================

        rb.linearVelocity =
            Vector3.zero;

        rb.angularVelocity =
            Vector3.zero;


        //=================================================
        // FORWARD DIRECTION
        //
        // Exciter truyền force vào đây.
        // Ta giữ hướng ngang của force nhưng ép thành
        // hướng bay tương đối với Player.
        //=================================================

        Vector3 horizontalForce =
            force;

        horizontalForce.y =
            0f;

        if (
            horizontalForce.sqrMagnitude <
            0.001f
        )
        {
            horizontalForce =
                transform.forward;
        }
        else
        {
            horizontalForce.Normalize();
        }


        //=================================================
        // BAY XA
        //=================================================

        float forwardVelocity =
            28f;

        float upwardVelocity =
            18f;

        Vector3 launchVelocity =
            horizontalForce *
            forwardVelocity;

        launchVelocity.y =
            upwardVelocity;

        rb.linearVelocity =
            launchVelocity;


        //=================================================
        // BỐC ĐẦU
        //
        // Xoay quanh trục RIGHT của Player.
        //
        // Mục tiêu:
        // đầu xe ngửa lên mạnh khi vừa bị húc.
        //=================================================

        Vector3 wheelieTorque =
            transform.right *
            -32f;

        rb.AddTorque(
            wheelieTorque,
            ForceMode.Impulse
        );


        //=================================================
        // XOAY NHIỀU VÒNG
        //
        // Thêm rotation theo trục forward.
        // Đây là spin/roll của chiếc Lead.
        //=================================================

        Vector3 spinTorque =
            transform.forward *
            42f;

        rb.AddTorque(
            spinTorque,
            ForceMode.Impulse
        );


        //=================================================
        // YAW NHẸ
        //
        // Cho cú húc nhìn tự nhiên hơn.
        //=================================================

        Vector3 yawTorque =
            transform.up *
            12f;

        rb.AddTorque(
            yawTorque,
            ForceMode.Impulse
        );


        //=================================================
        // ĐẶT ANGULAR VELOCITY TRỰC TIẾP
        //
        // Đây là phần quan trọng nhất.
        //
        // 18 rad/s ≈ 2.86 vòng/giây.
        // Với thời gian bay ~1s có thể thấy nhiều vòng.
        //=================================================

        Vector3 angularVelocity =
            transform.right *
            -18f;

        angularVelocity +=
            transform.forward *
            22f;

        angularVelocity +=
            transform.up *
            4f;

        rb.angularVelocity =
            angularVelocity;


        //=================================================
        // DEBUG
        //=================================================

        if (knockbackDebug)
        {
            Debug.Log(
                "[PlayerController] " +
                "EXCITER LAUNCH\n" +
                "LinearVelocity = " +
                rb.linearVelocity +
                "\n" +
                "AngularVelocity = " +
                rb.angularVelocity
            );
        }
    }


    //=====================================================
    // EXPLOSION
    //=====================================================

    SpawnImpactExplosion(
        transform.position
    );


    //=====================================================
    // GAME OVER
    //
    // Cho Player có thời gian bay trước khi GameOver.
    //=====================================================

    if (
        impactGameOverCoroutine != null
    )
    {
        StopCoroutine(
            impactGameOverCoroutine
        );
    }

    impactGameOverCoroutine =
        StartCoroutine(
            DelayedExciterGameOver()
        );
}

    //=========================================================
    // KILL PLAYER
    //=========================================================

    private void KillPlayer(
        Vector3 force
    )
    {
        if (isDead)
            return;

        if (
            isShieldBlockingHit
        )
        {
            return;
        }

        if (
            shieldHitCooldownTimer >
            0f
        )
        {
            return;
        }

        Vector3 direction =
            force;

        direction.y =
            0f;

        if (
            direction.sqrMagnitude <
            0.001f
        )
        {
            direction =
                -transform.forward;
        }
        else
        {
            direction.Normalize();
        }

        KillPlayerWithImpact(
            force,
            direction,
            1f
        );
    }

//=========================================================
// OBSTACLE COLLISION - TRIGGER
//=========================================================

private void ProcessObstacleCollision(
    GameObject obj
)
{
    if (obj == null)
        return;
//=====================================================
// PHOTON
//=====================================================

if (
    photonController != null &&
    photonController.IsPhotonActive
)
{
    if (knockbackDebug)
    {
        Debug.Log(
            "[PlayerController] " +
            "TRIGGER COLLISION IGNORED | " +
            "Photon active."
        );
    }

    return;
}
    if (knockbackDebug)
    {
        Debug.Log(
            "[PlayerController] " +
            "PROCESS TRIGGER OBSTACLE | " +
            "Object = " +
            obj.name
        );
    }

    //=====================================================
    // AMBULANCE
    //=====================================================

    if (IsAmbulance(obj))
    {
        if (knockbackDebug)
        {
            Debug.Log(
                "[PlayerController] " +
                "TRIGGER IGNORED | Ambulance."
            );
        }

        return;
    }

    //=====================================================
    // SHIELD
    //=====================================================

    if (TryConsumeShield(obj))
    {
        if (knockbackDebug)
        {
            Debug.Log(
                "[PlayerController] " +
                "TRIGGER BLOCKED BY SHIELD."
            );
        }

        return;
    }

    //=====================================================
    // GET DIRECTION
    //=====================================================

    Vector3 direction =
        GetFallbackKnockbackDirection(
            obj
        );

    float impactStrength =
        1f;

    //=====================================================
    // FINAL FORCE
    //=====================================================

    Vector3 finalForce =
        direction *
        knockbackForce;

    finalForce +=
        Vector3.up *
        upwardKnockbackY;


    //=====================================================
    // EXPLOSION POSITION
    //=====================================================

    Vector3 explosionPosition =
        transform.position;

    Collider obstacleCollider =
        obj.GetComponentInChildren<Collider>();

    if (obstacleCollider != null)
    {
        explosionPosition =
            obstacleCollider.ClosestPoint(
                transform.position
            );
    }
    else
    {
        explosionPosition =
            obj.transform.position;
    }


    //=====================================================
    // EXPLOSION
    //=====================================================

    SpawnImpactExplosion(
        explosionPosition
    );


    //=====================================================
    // DEBUG
    //=====================================================

    if (knockbackDebug)
    {
        Debug.Log(
            "[PlayerController] " +
            "TRIGGER KNOCKBACK | " +
            "Direction = " +
            direction +
            " | Force = " +
            finalForce +
            " | ExplosionPosition = " +
            explosionPosition
        );
    }


    //=====================================================
    // DEATH
    //=====================================================

    KillPlayerWithImpact(
        finalForce,
        direction,
        impactStrength
    );
}


    //=========================================================
    // AMBULANCE DETECTION
    //=========================================================

    private bool IsAmbulance(
        GameObject obj
    )
    {
        if (obj == null)
            return false;

        AmbulanceController ambulance =
            obj.GetComponentInParent<
                AmbulanceController
            >();

        if (ambulance != null)
            return true;

        ambulance =
            obj.GetComponentInChildren<
                AmbulanceController
            >(true);

        if (ambulance != null)
            return true;

        Transform root =
            obj.transform.root;

        if (root != null)
        {
            ambulance =
                root.GetComponent<
                    AmbulanceController
                >();

            if (ambulance != null)
                return true;
        }

        string objectName =
            obj.name.ToLower();

        if (
            objectName.Contains("ambulance") ||
            objectName.Contains("cuu thuong") ||
            objectName.Contains("cuthuong") ||
            objectName.Contains("xe_cuu_thuong") ||
            objectName.Contains("xecutthuong")
        )
        {
            return true;
        }

        if (root != null)
        {
            string rootName =
                root.name.ToLower();

            if (
                rootName.Contains("ambulance") ||
                rootName.Contains("cuu thuong") ||
                rootName.Contains("cuthuong") ||
                rootName.Contains("xe_cuu_thuong") ||
                rootName.Contains("xecutthuong")
            )
            {
                return true;
            }
        }

        return false;
    }


    //=========================================================
    // FATAL KNOCKBACK
    //=========================================================

    private void ApplyFatalKnockback(
        Vector3 force
    )
    {
        if (isDead)
            return;

        if (
            rampCollisionProtectionTimer >
            0f
        )
        {
            return;
        }

        if (
            TryConsumeShield(null)
        )
        {
            return;
        }

        if (
            photonController != null &&
            photonController.IsPhotonActive
        )
        {
            return;
        }

        KillPlayer(
            force
        );
    }


    //=========================================================
// TRIGGER ENTER
//=========================================================

private void OnTriggerEnter(
    Collider other
)
{
    if (isDead)
        return;


    GameObject obj =
        other.gameObject;


    //=====================================================
    // BA GAC RAMP - TRIGGER HARD GUARD
    //
    // Khi đang leo ramp, mọi trigger thuộc đúng Ba Gác
    // đang leo đều không được đi vào hệ thống obstacle.
    // RampTrigger riêng vẫn được xử lý ở bên dưới.
    //=====================================================

    if (
        rampState ==
        RampState.Climbing &&
        IsObjectFromActiveRampBaGac(
            obj
        )
    )
    {
        if (knockbackDebug)
        {
            Debug.Log(
                "[PlayerController] " +
                "HARD BLOCK BA GAC RAMP TRIGGER | " +
                "Object = " +
                obj.name
            );
        }

        return;
    }


    if (knockbackDebug)
    {
        Debug.Log(
            "[PlayerController] " +
            "ON TRIGGER ENTER | " +
            "Object = " +
            obj.name
        );
    }


    //=====================================================
    // GUM
    //=====================================================

    if (IsGum(obj))
    {
        CollectGum(obj);

        return;
    }


    //=====================================================
    // PHOTON
    //=====================================================

    if (IsPhoton(obj))
    {
        if (
            photonController != null
        )
        {
            photonController.ActivatePhoton();
        }

        Destroy(obj);

        return;
    }


    //=====================================================
    // SHIELD
    //=====================================================

    if (IsShield(obj))
    {
        return;
    }


    //=====================================================
    // RAMP TRIGGER ONLY
    //
    // Chỉ RampTrigger mới được bắt đầu ramp.
    // Không dùng IsRamp() chung ở đây nữa.
    //=====================================================

    if (
        IsRampTrigger(obj)
    )
    {
        BeginRamp(other);

        return;
    }


    //=====================================================
    // AMBULANCE
    //=====================================================

    if (
        IsAmbulance(obj)
    )
    {
        return;
    }


    //=====================================================
    // OBSTACLE
    //=====================================================

    if (
        IsObstacle(obj)
    )
    {
        ProcessObstacleCollision(
            obj
        );
    }
    else if (knockbackDebug)
    {
        Debug.Log(
            "[PlayerController] " +
            "TRIGGER OBJECT IS NOT OBSTACLE | " +
            obj.name
        );
    }
}


  //=========================================================
// TRIGGER EXIT
//=========================================================

private void OnTriggerExit(
    Collider other
)
{
    if (
        activeRampCollider == null
    )
    {
        return;
    }


    if (
        other != activeRampCollider
    )
    {
        return;
    }


    //=====================================================
    // CHỈ BỎ REFERENCE TRIGGER
    //
    // KHÔNG CancelRamp() ở đây.
    //
    // Player có thể đã rời volume RampTrigger
    // nhưng vẫn đang ở trên ramp / chuẩn bị launch.
    //=====================================================

    activeRampCollider =
        null;
}


//=========================================================
// COLLISION ENTER
//=========================================================

private void OnCollisionEnter(
    Collision collision
)
{
    if (isDead)
        return;

    if (collision == null)
        return;

    GameObject obj =
        collision.gameObject;


    //=====================================================
    // BA GAC RAMP - HARD COLLISION GUARD
    //
    // Ignore chính xác cặp collider đang va chạm.
    // Không phụ thuộc vào việc IgnoreCollision() ở BeginRamp
    // đã chạy trước physics step hay chưa.
    //=====================================================

    if (
        rampState ==
        RampState.Climbing &&
        IsObjectFromActiveRampBaGac(
            obj
        )
    )
    {

        if (knockbackDebug)
        {
            Debug.Log(
                "[PlayerController] " +
                "HARD BLOCK BA GAC RAMP COLLISION | " +
                "Object = " +
                obj.name +
                " | Collider = " +
                collision.collider.name
            );
        }

        return;
    }


    //=====================================================
    // DEBUG ENTRY
    //=====================================================

    if (knockbackDebug)
    {
        ContactPoint firstContact =
            collision.contactCount > 0
                ? collision.GetContact(0)
                : default;

        Debug.Log(
            "[PlayerController] " +
            "ON COLLISION ENTER | " +
            "Object = " +
            obj.name +
            " | Collider = " +
            collision.collider.name +
            " | Contacts = " +
            collision.contactCount +
            " | RelativeVelocity = " +
            collision.relativeVelocity +
            " | ContactNormal = " +
            (
                collision.contactCount > 0
                    ? firstContact.normal.ToString()
                    : "NONE"
            )
        );
    }


    //=====================================================
    // RAMP CLIMB PROTECTION
    //
    // Khi Player đang leo ramp:
    //
    // - Không xử lý obstacle thuộc Ba Gác hiện tại
    // - Không knockback
    // - Không kill Player
    //
    // Collision vat ly voi Ba Gac da duoc IgnoreCollision
    // ngay khi BeginRamp() thanh cong.
    // RampSurface van duoc raycast de lay do cao + normal.
    //=====================================================

    if (
        rampState ==
        RampState.Climbing
    )
    {
        if (
            IsObjectFromActiveRampBaGac(
                obj
            )
        )
        {
            if (knockbackDebug)
            {
                Debug.Log(
                    "[PlayerController] " +
                    "RAMP CLIMB COLLISION IGNORED | " +
                    "Object = " +
                    obj.name
                );
            }

            return;
        }
    }


    //=====================================================
    // GUM
    //=====================================================

    if (IsGum(obj))
    {
        CollectGum(obj);

        return;
    }


    //=====================================================
    // PHOTON
    //=====================================================

    if (IsPhoton(obj))
    {
        if (
            photonController != null
        )
        {
            photonController.ActivatePhoton();
        }

        Destroy(obj);

        return;
    }


    //=====================================================
    // SHIELD
    //=====================================================

    if (IsShield(obj))
    {
        return;
    }


    //=====================================================
    // RAMP SURFACE
    //
    // KHÔNG BeginRamp ở đây.
    // Ramp chỉ bắt đầu từ RampTrigger.
    //=====================================================


    //=====================================================
    // AMBULANCE
    //=====================================================

    if (
        IsAmbulance(obj)
    )
    {
        if (knockbackDebug)
        {
            Debug.Log(
                "[PlayerController] " +
                "COLLISION IDENTIFIED AS AMBULANCE | " +
                obj.name
            );
        }

        return;
    }


    //=====================================================
    // OBSTACLE
    //=====================================================

    bool obstacle =
        IsObstacle(obj);

    if (knockbackDebug)
    {
        Debug.Log(
            "[PlayerController] " +
            "IS OBSTACLE RESULT = " +
            obstacle +
            " | Object = " +
            obj.name
        );
    }

    if (obstacle)
    {
        ProcessObstacleCollision(
            obj,
            collision
        );
    }
    else if (knockbackDebug)
    {
        Debug.Log(
            "[PlayerController] " +
            "COLLISION REJECTED AS OBSTACLE | " +
            "Object = " +
            obj.name
        );
    }
}
private void ProcessObstacleCollision(
    GameObject obj,
    Collision collision
)
{
    if (obj == null)
    {
        if (knockbackDebug)
        {
            Debug.LogWarning(
                "[PlayerController] " +
                "PROCESS PHYSICAL FAILED | obj == NULL"
            );
        }

        return;
    }

    if (collision == null)
    {
        if (knockbackDebug)
        {
            Debug.LogWarning(
                "[PlayerController] " +
                "PROCESS PHYSICAL FAILED | collision == NULL"
            );
        }

        return;
    }


    //=====================================================
    // BA GAC RAMP - FINAL PHYSICAL GUARD
    //=====================================================

    if (
        rampState ==
        RampState.Climbing &&
        IsObjectFromActiveRampBaGac(
            obj
        )
    )
    {
       

        if (knockbackDebug)
        {
            Debug.Log(
                "[PlayerController] " +
                "PHYSICAL BA GAC RAMP COLLISION REJECTED | " +
                "Object = " +
                obj.name
            );
        }

        return;
    }

    if (knockbackDebug)
    {
        Debug.Log(
            "[PlayerController] " +
            "PROCESS PHYSICAL OBSTACLE | " +
            "Object = " +
            obj.name +
            " | Contacts = " +
            collision.contactCount +
            " | RelativeVelocity = " +
            collision.relativeVelocity
        );
    }

    //=====================================================
    // AMBULANCE
    //=====================================================

    if (IsAmbulance(obj))
    {
        if (knockbackDebug)
        {
            Debug.Log(
                "[PlayerController] " +
                "PHYSICAL COLLISION IGNORED | Ambulance."
            );
        }

        return;
    }

    //=====================================================
    // SHIELD
    //=====================================================

    if (TryConsumeShield(obj))
    {
        if (knockbackDebug)
        {
            Debug.Log(
                "[PlayerController] " +
                "PHYSICAL COLLISION BLOCKED BY SHIELD."
            );
        }

        return;
    }

    //=====================================================
    // PHOTON
    //=====================================================

    if (
        photonController != null &&
        photonController.IsPhotonActive
    )
    {
        if (knockbackDebug)
        {
            Debug.Log(
                "[PlayerController] " +
                "PHYSICAL COLLISION IGNORED | Photon active."
            );
        }

        return;
    }

    //=====================================================
    // APPLY KNOCKBACK
    //=====================================================

    ApplyCollisionKnockback(
        collision,
        1f
    );
}

    //=========================================================
    // COLLECT GUM
    //=========================================================

   private void CollectGum(GameObject obj)
{
    if (obj == null)
        return;

    Gum gum =
        obj.GetComponentInParent<Gum>();

    if (gum != null)
    {
        gum.Collect();
        return;
    }

    if (ItemManager.Instance != null)
    {
        ItemManager.Instance.ActivateGum();
    }
    else
    {
        Debug.LogWarning(
            "[PlayerController] " +
            "Cannot activate Gum | " +
            "ItemManager.Instance == NULL."
        );
    }

    Destroy(obj);
}


    //=========================================================
    // LEGACY JUMP API
    //=========================================================

    private void JumpFromRamp()
    {
        BeginRamp(null);
    }


    //=========================================================
    // CHECK GUM
    //=========================================================

    private bool IsGum(
        GameObject obj
    )
    {
        if (obj == null)
            return false;

        Gum gum =
            obj.GetComponentInParent<Gum>();

        if (gum != null)
            return true;

        string name =
            obj.name.ToLower();

        bool gumTag =
            obj.CompareTag("Gum");

        return
            gumTag ||
            name.Contains("gum") ||
            name.Contains("singum") ||
            name.Contains("keo");
    }


    //=========================================================
    // CHECK PHOTON
    //=========================================================

    private bool IsPhoton(
        GameObject obj
    )
    {
        if (obj == null)
            return false;

        string name =
            obj.name.ToLower();

        bool photonTag =
            obj.CompareTag("Photon") ||
            obj.CompareTag("PhotonItem");

        return
            photonTag ||
            name.Contains("photon") ||
            name.Contains("tocdoanhang");
    }


    //=========================================================
    // CHECK SHIELD
    //=========================================================

    private bool IsShield(
        GameObject obj
    )
    {
        if (obj == null)
            return false;

        bool shieldTag =
            obj.CompareTag("Shield");

        string name =
            obj.name.ToLower();

        return
            shieldTag ||
            name.Contains("shield");
    }


    //=========================================================
    // CHECK RAMP
    //=========================================================

    private bool IsRamp(
        GameObject obj
    )
    {
        if (obj == null)
            return false;

        if (
            obj.CompareTag("Ramp")
        )
        {
            return true;
        }

        Transform current =
            obj.transform;

        while (
            current != null
        )
        {
            string lower =
                current.name.ToLower();

            if (
                lower == "ramp" ||
                lower == "ramptrigger" ||
                lower == "ramp_trigger" ||
                lower == "tinramp" ||
                lower == "tinramptrigger" ||
                lower == "tin_ramp" ||
                lower == "tin_ramp_trigger" ||
                lower == "bagacramp" ||
                lower == "bagac_ramp" ||
                lower == "bagacramptigger" ||
                lower == "bagac_ramp_trigger"
            )
            {
                return true;
            }

            current =
                current.parent;
        }

        string name =
            obj.name.ToLower();

        return
            name.Contains("docton") ||
            name.Contains("doc ton") ||
            name == "ton";
    }

    //=========================================================
// CHECK RAMP TRIGGER
//=========================================================

private bool IsRampTrigger(
    GameObject obj
)
{
    if (obj == null)
        return false;


    string name =
        obj.name.ToLower();


    //=====================================================
    // TAG
    //=====================================================

    if (
        obj.CompareTag("Ramp")
    )
    {
        if (
            name.Contains("trigger")
        )
        {
            return true;
        }
    }


    //=====================================================
    // NAME
    //=====================================================

    return
        name == "ramptrigger" ||
        name == "ramp_trigger" ||
        name == "tinramptrigger" ||
        name == "tin_ramp_trigger" ||
        name == "bagacramptrigger" ||
        name == "bagac_ramp_trigger" ||
        name.Contains("ramptrigger") ||
        name.Contains("ramp_trigger");
}

    //=========================================================
    // CHECK OBJECT FROM ACTIVE BA GAC
    //=========================================================

    private bool IsObjectFromActiveRampBaGac(
        GameObject obj
    )
    {
        if (
            obj == null ||
            activeRampRoot == null
        )
        {
            return false;
        }

        Transform objectRoot =
            obj.transform.root;

        Transform rampRoot =
            activeRampRoot.root;

        return
            objectRoot == rampRoot;
    }


    //=========================================================
    // PHOTON - BLOCK ACTIVE BA GAC RAMP HIT
    //
    // PhotonController can receive the same collider callback
    // independently from PlayerController. This public query
    // lets PhotonController respect the ramp system without
    // duplicating the ramp hierarchy logic.
    //=========================================================

    public bool IsRampTraversalActive()
    {
        return
            rampState != RampState.None &&
            activeRampRoot != null;
    }


    public bool IsPhotonRampCollisionBlocked(
        Collider hitCollider
    )
    {
        if (hitCollider == null)
        {
            return false;
        }

        //=====================================================
        // RAMP COLLIDER PROTECTION - NGAY TU CALLBACK DAU TIEN
        //=====================================================
        // Khi Photon nhan collider ramp truoc khi PlayerController
        // kip chuyen state sang Climbing, van phai bo qua collider
        // nay. Neu khong, Photon co the coi ramp la Ba Gac va goi
        // HitBaGacObject() ngay tai frame dau tien.
        if (IsRamp(hitCollider.gameObject))
        {
            return true;
        }

        //=====================================================
        // TOAN BO BA GAC TRONG SUOT LUOT RAMP
        //=====================================================
        // Khi da vao ramp, khong dung root comparison nua. Một so
        // hierarchy co the dat ramp/body thanh cac root khac nhau.
        // Dieu kien bao ve phai dua tren ramp state + active ramp.
        //
        // Tu Climbing den Airborne, Photon bo qua Ba Gac. Khi landing,
        // rampState tro ve None va activeRampRoot bi clear -> Photon
        // lai co the huc than Ba Gac trong lan tiep theo.
        if (
            rampState != RampState.None &&
            activeRampRoot != null &&
            IsBaGacLikeObject(hitCollider.gameObject)
        )
        {
            return true;
        }

        return false;
    }


    //=========================================================
    // CHECK BA GAC / RAMP FOR PHOTON
    //=========================================================
    private bool IsBaGacLikeObject(
        GameObject obj
    )
    {
        if (obj == null)
        {
            return false;
        }

        Transform current =
            obj.transform;

        while (current != null)
        {
            string name =
                current.name.ToLowerInvariant();

            if (
                name.Contains("bagac") ||
                name.Contains("ba gac") ||
                name.Contains("ba_gac")
            )
            {
                return true;
            }

            current =
                current.parent;
        }

        return false;
    }


    //=========================================================
    // CHECK OBSTACLE
    //=========================================================
private bool IsObstacle(
    GameObject obj
)
{
    if (obj == null)
        return false;


    if (
        IsAmbulance(obj)
    )
    {
        return false;
    }


    //=====================================================
    // RAMP KHÔNG PHẢI OBSTACLE
    //=====================================================

    if (IsRamp(obj))
    {
        return false;
    }


    //=====================================================
    // TRAFFIC CAR BEHAVIOR
    //=====================================================

    TrafficCarBehavior traffic =
        obj.GetComponentInParent<
            TrafficCarBehavior
        >();

    if (traffic != null)
    {
        return true;
    }


    //=====================================================
    // TRAFFIC VEHICLE
    //=====================================================

    TrafficVehicle vehicle =
        obj.GetComponentInParent<
            TrafficVehicle
        >();

    if (vehicle != null)
    {
        return true;
    }


    //=====================================================
    // OBSTACLE TAG
    //=====================================================

    if (
        obj.CompareTag("Obstacle")
    )
    {
        return true;
    }


    Transform root =
        obj.transform.root;

    if (
        root != null &&
        root.CompareTag("Obstacle")
    )
    {
        return true;
    }


    //=====================================================
    // NAME FALLBACK
    //=====================================================

    string name =
        obj.name.ToLower();


    return
        name.Contains("car") ||
        name.Contains("bus") ||
        name.Contains("motor") ||
        name.Contains("bike") ||
        name.Contains("bagac") ||
        name.Contains("exciter");
}


    //=========================================================
    // CACHE PLAYER RENDERERS
    //=========================================================

    private void CachePlayerRenderers()
    {
        if (includeChildRenderers)
        {
            playerRenderers =
                GetComponentsInChildren<Renderer>(
                    true
                );
        }
        else
        {
            Renderer ownRenderer =
                GetComponent<Renderer>();

            if (ownRenderer != null)
            {
                playerRenderers =
                    new Renderer[]
                    {
                        ownRenderer
                    };
            }
            else
            {
                playerRenderers =
                    new Renderer[0];
            }
        }

        Debug.Log(
            "[PlayerController] " +
            "Cached Player Renderers = " +
            playerRenderers.Length
        );
    }


    //=========================================================
    // SHIELD FLASH
    //=========================================================

    private void PlayShieldFlash()
    {
        if (!enableShieldFlash)
            return;

        if (isDead)
            return;

        if (
            playerRenderers == null ||
            playerRenderers.Length == 0
        )
        {
            CachePlayerRenderers();
        }

        if (
            shieldFlashCoroutine != null
        )
        {
            StopCoroutine(
                shieldFlashCoroutine
            );

            RestorePlayerRenderers();
        }

        shieldFlashCoroutine =
            StartCoroutine(
                ShieldFlashRoutine()
            );
    }


    //=========================================================
    // SHIELD FLASH ROUTINE
    //=========================================================

    private IEnumerator ShieldFlashRoutine()
    {
        float interval =
            Mathf.Max(
                0.01f,
                shieldFlashInterval
            );

        int flashCount =
            Mathf.Max(
                1,
                shieldFlashCount
            );

        float duration =
            Mathf.Max(
                interval *
                flashCount *
                2f,
                shieldFlashDuration
            );

        float elapsed = 0f;

        int flashesDone = 0;

        bool visible = true;

        while (
            elapsed < duration &&
            flashesDone < flashCount
        )
        {
            visible =
                !visible;

            SetPlayerRenderersVisible(
                visible
            );

            yield return new WaitForSeconds(
                interval
            );

            elapsed +=
                interval;

            if (!visible)
            {
                flashesDone++;
            }
        }

        RestorePlayerRenderers();

        shieldFlashCoroutine =
            null;
    }


    //=========================================================
    // SET PLAYER RENDERER VISIBILITY
    //=========================================================

    private void SetPlayerRenderersVisible(
        bool visible
    )
    {
        if (
            playerRenderers == null
        )
        {
            return;
        }

        for (
            int i = 0;
            i < playerRenderers.Length;
            i++
        )
        {
            Renderer renderer =
                playerRenderers[i];

            if (renderer == null)
                continue;

            renderer.enabled =
                visible;
        }
    }


    //=========================================================
    // RESTORE PLAYER RENDERERS
    //=========================================================

    private void RestorePlayerRenderers()
    {
        if (
            playerRenderers == null
        )
        {
            return;
        }

        for (
            int i = 0;
            i < playerRenderers.Length;
            i++
        )
        {
            Renderer renderer =
                playerRenderers[i];

            if (renderer == null)
                continue;

            renderer.enabled =
                true;
        }
    }


    //=========================================================
    // STATIC EXPLOSION
    //=========================================================

    private void SyncExplosionStaticVars()
    {
        EnableExplosionStatic =
            enableExplosionAnimation;

        ExplosionEffectPrefabStatic =
            explosionEffectPrefab;
    }


    //=========================================================
    // FIND PLAYER BOX COLLIDER
    //=========================================================

    private void FindPlayerBoxCollider()
    {
        playerBoxCollider =
            GetComponent<BoxCollider>();

        if (playerBoxCollider != null)
        {
            if (knockbackDebug)
            {
                Debug.Log(
                    "[PlayerController] " +
                    "BOX COLLIDER FOUND ON PLAYER | " +
                    playerBoxCollider.name
                );
            }

            return;
        }

        if (!findPlayerBoxColliderInChildren)
        {
            if (knockbackDebug)
            {
                Debug.LogWarning(
                    "[PlayerController] " +
                    "BOX COLLIDER NOT FOUND ON ROOT " +
                    "AND CHILD SEARCH DISABLED."
                );
            }

            return;
        }

        playerBoxCollider =
            GetComponentInChildren<BoxCollider>(
                true
            );

        if (playerBoxCollider == null)
        {
            Debug.LogWarning(
                "[PlayerController] " +
                "Không tìm thấy BoxCollider cho Player."
            );
        }
        else if (knockbackDebug)
        {
            Debug.Log(
                "[PlayerController] " +
                "BOX COLLIDER FOUND IN CHILD | " +
                playerBoxCollider.name +
                " | Transform = " +
                playerBoxCollider.transform.name
            );
        }
    }


//=========================================================
// GET IMPACT DIRECTION FROM PLAYER BOX COLLIDER
//=========================================================

private Vector3 GetImpactDirectionFromBoxCollider(
    Collision collision
)
{
    //=====================================================
    // FIND PLAYER BOX COLLIDER
    //=====================================================

    if (playerBoxCollider == null)
    {
        FindPlayerBoxCollider();
    }


    //=====================================================
    // BASIC FALLBACK
    //=====================================================

    if (
        collision == null ||
        collision.contactCount <= 0
    )
    {
        if (knockbackDebug)
        {
            Debug.LogWarning(
                "[PlayerController] " +
                "KNOCKBACK DIRECTION FALLBACK | " +
                "No valid collision contact."
            );
        }

        return -transform.forward;
    }


    //=====================================================
    // BOX COLLIDER NOT FOUND
    //=====================================================

    if (playerBoxCollider == null)
    {
        Vector3 fallback =
            GetFallbackKnockbackDirection(
                collision.gameObject
            );

        if (knockbackDebug)
        {
            Debug.LogWarning(
                "[PlayerController] " +
                "KNOCKBACK DIRECTION FALLBACK | " +
                "Player BoxCollider not found."
            );
        }

        return fallback;
    }


    //=====================================================
    // GET REAL BOX CENTER IN WORLD SPACE
    //
    // playerBoxCollider.center là LOCAL SPACE của
    // chính BoxCollider.
    //
    // Vì BoxCollider có thể nằm trong CHILD nên phải
    // chuyển đúng center của nó sang world space.
    //=====================================================

    Vector3 boxWorldCenter =
        playerBoxCollider.transform.TransformPoint(
            playerBoxCollider.center
        );


    //=====================================================
    // CONVERT BOX CENTER TO PLAYER ROOT LOCAL SPACE
    //
    // Từ đây direction sẽ dựa theo hướng của Player,
    // không phụ thuộc rotation riêng của child collider.
    //=====================================================

    Vector3 localBoxCenter =
        transform.InverseTransformPoint(
            boxWorldCenter
        );


    //=====================================================
    // ACCUMULATE IMPACT POSITION
    //
    // Có thể có nhiều ContactPoint.
    // Lấy trung bình để tránh contact đầu tiên
    // quyết định sai hướng.
    //=====================================================

    Vector3 averageLocalContact =
        Vector3.zero;

    int validContacts =
        0;

    for (
        int i = 0;
        i < collision.contactCount;
        i++
    )
    {
        ContactPoint contact =
            collision.GetContact(i);

        Vector3 localContact =
            transform.InverseTransformPoint(
                contact.point
            );

        averageLocalContact +=
            localContact;

        validContacts++;
    }


    //=====================================================
    // SAFETY
    //=====================================================

    if (validContacts <= 0)
    {
        return GetFallbackKnockbackDirection(
            collision.gameObject
        );
    }

    averageLocalContact /=
        validContacts;


    //=====================================================
    // IMPACT OFFSET
    //
    // Đây là vị trí va chạm so với tâm BoxCollider.
    //
    // +X = bên phải Player
    // -X = bên trái Player
    //
    // +Z = phía trước Player
    // -Z = phía sau Player
    //=====================================================

    Vector3 localImpact =
        averageLocalContact -
        localBoxCenter;

    localImpact.y =
        0f;


    //=====================================================
    // DEBUG RAW IMPACT
    //=====================================================

    if (knockbackDebug)
    {
        Debug.Log(
            "[PlayerController] " +
            "BOX IMPACT RAW\n" +
            "Object = " +
            collision.gameObject.name +
            "\n" +
            "BoxCenterWorld = " +
            boxWorldCenter +
            "\n" +
            "BoxCenterLocal = " +
            localBoxCenter +
            "\n" +
            "ContactLocal = " +
            averageLocalContact +
            "\n" +
            "ImpactLocal = " +
            localImpact
        );
    }


    //=====================================================
    // IF IMPACT IS TOO CLOSE TO CENTER
    //
    // Khi va đúng gần tâm BoxCollider thì vị trí không
    // cho ta hướng rõ ràng.
    //
    // Lúc này dùng vị trí của obstacle so với Player.
    //=====================================================

    if (
        localImpact.sqrMagnitude <
        0.01f
    )
    {
        Vector3 localObstacle =
            transform.InverseTransformPoint(
                collision.transform.position
            );

        localObstacle.y =
            0f;

        if (
            localObstacle.sqrMagnitude >
            0.001f
        )
        {
            // Obstacle nằm ở đâu
            // thì Player bị đánh từ phía đó.
            localImpact =
                localObstacle.normalized;
        }
        else
        {
            localImpact =
                Vector3.forward;
        }

        if (knockbackDebug)
        {
            Debug.Log(
                "[PlayerController] " +
                "IMPACT CENTER FALLBACK | " +
                "LocalImpact = " +
                localImpact
            );
        }
    }
    else
    {
        localImpact.Normalize();
    }


    //=====================================================
    // KNOCKBACK DIRECTION
    //
    // Va phía trước:
    // localImpact = +Z
    // knockback = -Z
    //
    // Va phía sau:
    // localImpact = -Z
    // knockback = +Z
    //
    // Va trái:
    // localImpact = -X
    // knockback = +X
    //
    // Va phải:
    // localImpact = +X
    // knockback = -X
    //
    // Va chéo:
    // giữ nguyên hướng chéo.
    //=====================================================

    Vector3 localKnockback =
        -localImpact;

    localKnockback.y =
        0f;


    if (
        localKnockback.sqrMagnitude <
        0.001f
    )
    {
        localKnockback =
            Vector3.back;
    }

    localKnockback.Normalize();


    //=====================================================
    // CONVERT TO WORLD SPACE
    //
    // Dùng Player ROOT transform.
    //
    // KHÔNG dùng:
    // playerBoxCollider.transform.TransformDirection()
    //
    // vì BoxCollider có thể nằm ở child và có rotation
    // riêng.
    //=====================================================

    Vector3 worldKnockback =
        transform.TransformDirection(
            localKnockback
        );

    worldKnockback.y =
        0f;


    if (
        worldKnockback.sqrMagnitude <
        0.001f
    )
    {
        worldKnockback =
            -transform.forward;
    }

    worldKnockback.Normalize();


    //=====================================================
    // DEBUG FINAL
    //=====================================================

    if (knockbackDebug)
    {
        Debug.Log(
            "[PlayerController] " +
            "FINAL KNOCKBACK DIRECTION\n" +
            "Object = " +
            collision.gameObject.name +
            "\n" +
            "LocalKnockback = " +
            localKnockback +
            "\n" +
            "WorldKnockback = " +
            worldKnockback
        );
    }


    //=====================================================
    // DEBUG RAYS
    //=====================================================

    if (knockbackDrawDebugRay)
    {
        Debug.DrawRay(
            boxWorldCenter,
            worldKnockback *
            knockbackDebugRayLength,
            Color.red,
            3f
        );

        Debug.DrawRay(
            boxWorldCenter,
            -worldKnockback *
            knockbackDebugRayLength,
            Color.blue,
            3f
        );
    }


    return worldKnockback;
}
    //=========================================================
    // GET IMPACT STRENGTH
    //=========================================================

    private float GetImpactStrength(
        Collision collision
    )
    {
        if (collision == null)
            return 1f;

        float relativeSpeed =
            collision.relativeVelocity.magnitude;

        float strength =
            Mathf.InverseLerp(
                2f,
                25f,
                relativeSpeed
            );

        float result =
            Mathf.Lerp(
                lightImpactMultiplier,
                strongImpactMultiplier,
                strength
            );

        if (knockbackDebug)
        {
            Debug.Log(
                "[PlayerController] " +
                "IMPACT STRENGTH | " +
                "RelativeSpeed = " +
                relativeSpeed.ToString("F2") +
                " | Normalized = " +
                strength.ToString("F2") +
                " | Multiplier = " +
                result.ToString("F2")
            );
        }

        return result;
    }


   //=========================================================
    // APPLY IMPACT ROTATION
    //=========================================================
private void ApplyImpactRotation(
    Vector3 knockbackDirection,
    float impactStrength
)
{
    if (rb == null)
        return;


    //=====================================================
    // CONVERT KNOCKBACK TO PLAYER LOCAL SPACE
    //=====================================================

    Vector3 localDirection =
        transform.InverseTransformDirection(
            knockbackDirection
        );

    localDirection.y = 0f;

    if (
        localDirection.sqrMagnitude <
        0.001f
    )
    {
        localDirection =
            Vector3.back;
    }

    localDirection.Normalize();


    //=====================================================
    // DIRECTION COMPONENTS
    //=====================================================

    float lateral =
        localDirection.x;

    float longitudinal =
        localDirection.z;


    //=====================================================
    // ROTATION SETTINGS
    //
    // Roll = xe ngã trái / phải
    // Pitch = xe chúi / ngửa nhẹ
    // Yaw = xoay đầu xe
    //=====================================================

    float rollTorque =
        -lateral *
        knockbackRollTorque *
        impactStrength;

    float pitchTorque =
        longitudinal *
        knockbackRollTorque *
        0.20f *
        impactStrength;

    float yawTorque =
        lateral *
        knockbackYawTorque *
        0.25f *
        impactStrength;


    //=====================================================
    // BUILD TORQUE
    //=====================================================

    Vector3 torque =
        transform.forward *
        rollTorque;

    torque +=
        transform.right *
        pitchTorque;

    torque +=
        transform.up *
        yawTorque;


    //=====================================================
    // LIMIT TORQUE
    //
    // Tránh va chạm chéo làm xe xoay quá mạnh.
    //=====================================================

    float maxTorque =
        6f *
        impactStrength;

    if (
        torque.magnitude >
        maxTorque
    )
    {
        torque =
            torque.normalized *
            maxTorque;
    }


    //=====================================================
    // DEBUG
    //=====================================================

    if (knockbackDebug)
    {
        Debug.Log(
            "[PlayerController] " +
            "IMPACT ROTATION\n" +
            "LocalDirection = " +
            localDirection +
            "\n" +
            "Lateral = " +
            lateral.ToString("F2") +
            "\n" +
            "Longitudinal = " +
            longitudinal.ToString("F2") +
            "\n" +
            "RollTorque = " +
            rollTorque.ToString("F2") +
            "\n" +
            "PitchTorque = " +
            pitchTorque.ToString("F2") +
            "\n" +
            "YawTorque = " +
            yawTorque.ToString("F2") +
            "\n" +
            "FinalTorque = " +
            torque
        );
    }


    //=====================================================
    // APPLY PHYSICS TORQUE
    //=====================================================

    rb.AddTorque(
        torque,
        ForceMode.Impulse
    );


    //=====================================================
    // CONTROLLED INITIAL ROTATION
    //
    // Không cộng torque * 0.35 nữa.
    // Chỉ cho xe bắt đầu nghiêng một chút.
    //=====================================================

    Vector3 currentAngularVelocity =
        rb.angularVelocity;

    Vector3 controlledAngularVelocity =
        torque *
        0.035f;

    currentAngularVelocity +=
        controlledAngularVelocity;


    //=====================================================
    // LIMIT ANGULAR VELOCITY
    //=====================================================

    float maxAngularVelocity =
        4.5f;

    if (
        currentAngularVelocity.magnitude >
        maxAngularVelocity
    )
    {
        currentAngularVelocity =
            currentAngularVelocity.normalized *
            maxAngularVelocity;
    }

    rb.angularVelocity =
        currentAngularVelocity;


    //=====================================================
    // DEBUG FINAL ANGULAR VELOCITY
    //=====================================================

    if (knockbackDebug)
    {
        Debug.Log(
            "[PlayerController] " +
            "ANGULAR VELOCITY AFTER IMPACT = " +
            rb.angularVelocity
        );
    }
}




    //=========================================================
    // APPLY COLLISION KNOCKBACK
    //=========================================================

public void ApplyCollisionKnockback(
    Collision collision,
    float forceMultiplier = 1f
)
{
    if (knockbackDebug)
    {
        Debug.Log(
            "[PlayerController] " +
            "APPLY COLLISION KNOCKBACK ENTER | " +
            "Collision = " +
            (
                collision != null
                    ? collision.gameObject.name
                    : "NULL"
            ) +
            " | Multiplier = " +
            forceMultiplier
        );
    }

    if (isDead)
    {
        if (knockbackDebug)
        {
            Debug.Log(
                "[PlayerController] " +
                "KNOCKBACK STOP | Player already dead."
            );
        }

        return;
    }

    if (
        rampCollisionProtectionTimer >
        0f
    )
    {
        if (knockbackDebug)
        {
            Debug.Log(
                "[PlayerController] " +
                "KNOCKBACK STOP | Ramp protection = " +
                rampCollisionProtectionTimer.ToString("F3")
            );
        }

        return;
    }

    if (
    photonController != null &&
    photonController.IsPhotonActive
)
{
    if (knockbackDebug)
    {
        Debug.Log(
            "[PlayerController] " +
            "PHOTON ACTIVE | " +
            "Skip local knockback + death."
        );
    }

    return;
}
    if (collision == null)
    {
        if (knockbackDebug)
        {
            Debug.LogWarning(
                "[PlayerController] " +
                "KNOCKBACK STOP | Collision NULL."
            );
        }

        return;
    }


    //=====================================================
    // OBSTACLE
    //=====================================================

    GameObject obstacle =
        collision.gameObject;


    //=====================================================
    // SHIELD
    //=====================================================

    if (
        TryConsumeShield(obstacle)
    )
    {
        if (knockbackDebug)
        {
            Debug.Log(
                "[PlayerController] " +
                "KNOCKBACK STOP | Shield consumed collision."
            );
        }

        return;
    }


    //=====================================================
    // GET DIRECTION
    //=====================================================

    Vector3 direction =
        GetImpactDirectionFromBoxCollider(
            collision
        );


    //=====================================================
    // IMPACT STRENGTH
    //=====================================================

    float impactStrength =
        GetImpactStrength(
            collision
        );

    impactStrength *=
        Mathf.Max(
            0f,
            forceMultiplier
        );


    //=====================================================
    // FINAL FORCE
    //=====================================================

    Vector3 force =
        direction *
        knockbackForce *
        impactStrength;

    force +=
        Vector3.up *
        upwardKnockbackY;


    //=====================================================
    // EXPLOSION POSITION
    //=====================================================

    Vector3 explosionPosition =
        transform.position;

    if (
        collision.contactCount >
        0
    )
    {
        explosionPosition =
            collision.GetContact(0).point;
    }


    //=====================================================
    // EXPLOSION
    //=====================================================

    SpawnImpactExplosion(
        explosionPosition
    );


    //=====================================================
    // FINAL DEBUG
    //=====================================================

    if (knockbackDebug)
    {
        Debug.Log(
            "[PlayerController] " +
            "IMPACT FINAL | " +
            "Direction = " +
            direction +
            " | Strength = " +
            impactStrength.ToString("F2") +
            " | HorizontalForce = " +
            (
                direction *
                knockbackForce *
                impactStrength
            ) +
            " | FinalForce = " +
            force +
            " | ExplosionPosition = " +
            explosionPosition
        );
    }


    //=====================================================
    // DEATH
    //=====================================================

    KillPlayerWithImpact(
        force,
        direction,
        impactStrength
    );
}


    //=========================================================
    // KILL PLAYER WITH IMPACT
    //=========================================================

    private void KillPlayerWithImpact(
        Vector3 force,
        Vector3 knockbackDirection,
        float impactStrength
    )
    {
        if (isDead)
            return;

        if (
            isShieldBlockingHit
        )
        {
            return;
        }

        if (
            shieldHitCooldownTimer >
            0f
        )
        {
            return;
        }

        isDead =
            true;

        StopEngineAudio();

        rampState =
            RampState.None;

        activeRampCollider =
            null;

        activeRampSurfaceCollider =
            null;

        activeRampRoot =
            null;

        rampCollisionProtectionTimer =
            0f;


        //=====================================================
        // RESTORE RENDERERS
        //=====================================================

        if (
            shieldFlashCoroutine != null
        )
        {
            StopCoroutine(
                shieldFlashCoroutine
            );

            shieldFlashCoroutine =
                null;

            RestorePlayerRenderers();
        }


        //=====================================================
        // RIGIDBODY
        //=====================================================

        if (rb != null)
        {
            rb.isKinematic =
                false;

            rb.useGravity =
                true;

            // Cho phép Rigidbody tự do xoay khi Player chết
            rb.constraints &= ~RigidbodyConstraints.FreezeRotationX;
            rb.constraints &= ~RigidbodyConstraints.FreezeRotationY;
            rb.constraints &= ~RigidbodyConstraints.FreezeRotationZ;

            rb.linearDamping =
                knockbackDrag;

            rb.angularDamping =
                knockbackAngularDrag;

            rb.collisionDetectionMode =
                CollisionDetectionMode.ContinuousDynamic;


            //=================================================
            // RESET VELOCITY
            //=================================================

            rb.linearVelocity =
                Vector3.zero;

            rb.angularVelocity =
                Vector3.zero;


            //=================================================
            // KNOCKBACK
            //=================================================

            rb.AddForce(
                force,
                ForceMode.Impulse
            );


            //=================================================
            // ROTATION
            //=================================================

            ApplyImpactRotation(
                knockbackDirection,
                impactStrength
            );
        }


        //=====================================================
        // DEBUG DEATH
        //=====================================================

        if (knockbackDebug)
        {
            Debug.Log(
                "[PlayerController] " +
                "PLAYER KILLED BY IMPACT | " +
                "Force = " +
                force +
                " | Direction = " +
                knockbackDirection +
                " | ImpactStrength = " +
                impactStrength.ToString("F2") +
                " | Rigidbody = " +
                (rb != null ? "FOUND" : "NULL")
            );
        }

        
        //=====================================================
        // GAME OVER
        //=====================================================

        if (
    impactGameOverCoroutine != null
)
{
    StopCoroutine(
        impactGameOverCoroutine
    );
}

impactGameOverCoroutine =
    StartCoroutine(
        DelayedImpactGameOver()
    );
    }


    //=========================================================
    // FALLBACK DIRECTION
    //=========================================================

    private Vector3 GetFallbackKnockbackDirection(
        GameObject obstacle
    )
    {
        if (obstacle == null)
        {
            return -transform.forward;
        }

        Vector3 direction =
            transform.position -
            obstacle.transform.position;

        direction.y =
            0f;

        if (
            direction.sqrMagnitude <
            0.001f
        )
        {
            direction =
                -transform.forward;
        }

        return direction.normalized;
    }

    //=========================================================
    // DELAY GAME OVER AFTER IMPACT
    //=========================================================

private IEnumerator DelayedImpactGameOver()
{
    yield return new WaitForSeconds(0.75f);

    Debug.Log(
        "[PlayerController] DELAYED GAME OVER | " +
        "GameManager.Instance = " +
        (GameManager.Instance != null ? "FOUND" : "NULL")
    );

    if (GameManager.Instance != null)
    {
        GameManager.Instance.GameOver();
    }

    impactGameOverCoroutine = null;
}

//=========================================================
// DELAY GAME OVER AFTER EXCITER IMPACT
//=========================================================

private IEnumerator DelayedExciterGameOver()
{
    // Cho Player bay + xoay trên không
    // trước khi chuyển sang Game Over.

    yield return new WaitForSeconds(
        1.25f
    );

    if (GameManager.Instance != null)
    {
        GameManager.Instance.GameOver();
    }

    impactGameOverCoroutine =
        null;
}
private void SpawnImpactExplosion(Vector3 position)
{
    if (!enableExplosionAnimation)
        return;

    if (explosionEffectPrefab == null)
    {
        Debug.LogWarning(
            "EXPLOSION: Chưa gán Explosion Effect Prefab trong PlayerController."
        );
        return;
    }

    GameObject explosion =
        Instantiate(
            explosionEffectPrefab,
            position,
            Quaternion.identity
        );

    ParticleSystem[] particles =
        explosion.GetComponentsInChildren<ParticleSystem>();

    float maxLifetime = 0f;

    foreach (ParticleSystem particle in particles)
    {
        var main = particle.main;

        float lifetime =
            main.startLifetime.constantMax;

        if (lifetime > maxLifetime)
            maxLifetime = lifetime;
    }

    if (maxLifetime <= 0f)
        maxLifetime = 2f;

    Destroy(explosion, maxLifetime + 0.5f);
}

private void EnableDeathPhysics()
{
    Collider[] colliders =
        GetComponentsInChildren<Collider>();

    for (int i = 0; i < colliders.Length; i++)
    {
        Collider col = colliders[i];

        if (col == null)
            continue;

        // Không xử lý collider của Particle System
        if (col.GetComponent<ParticleSystem>() != null)
            continue;

        col.isTrigger = false;
    }

    if (rb != null)
    {
        rb.isKinematic = false;
        rb.useGravity = true;

        rb.detectCollisions = true;

        rb.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;

        rb.interpolation =
            RigidbodyInterpolation.Interpolate;

        rb.linearDamping =
            knockbackDrag;

        rb.angularDamping =
            knockbackAngularDrag;
    }

    if (knockbackDebug)
    {
        Debug.Log(
            "[PlayerController] " +
            "DEATH PHYSICS ENABLED | " +
            "Colliders = NON-TRIGGER | " +
            "Collision Detection = ContinuousDynamic"
        );
    }
}

}
