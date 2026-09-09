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


    //=========================================================
    // GUM
    //=========================================================

    [Header("Gum Boost Settings")]

    [Tooltip("Thời gian Gum có hiệu lực.")]
    [SerializeField] private float gumDuration = 5f;

    [Tooltip("In log để kiểm tra Gum có thực sự kích hoạt.")]
    [SerializeField] private bool gumDebug = true;

    private bool isGumBoosted;

    private float gumTimer;

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
    // KNOCKBACK
    //=========================================================

    [Header("Knockback & Physics Settings")]

    [SerializeField] private float oncomingKnockbackZ = -12f;

    [SerializeField] private float exciterKnockbackZ = 12f;

    [SerializeField] private float upwardKnockbackY = 4.5f;

    [SerializeField] private float knockbackDrag = 2f;


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
    // GUM AUDIO
    //=========================================================

    [Header("Gum Audio")]

    [SerializeField] private AudioClip gumPickupSound;

    [SerializeField] private AudioClip gumBoostSound;

    [Range(0f, 1f)]
    [SerializeField] private float gumPickupVolume = 0.9f;

    [Range(0f, 1f)]
    [SerializeField] private float gumBoostVolume = 0.85f;


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
            return isGumBoosted;
        }
    }

    public float GumTimeRemaining
    {
        get
        {
            return Mathf.Max(
                0f,
                gumTimer
            );
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

        SetupPlayerAudio();
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

        if (gumDebug)
        {
            Debug.Log(
                "[PlayerController] " +
                "START | Normal Horizontal Speed = " +
                currentHorizontalSpeed +
                " | Gum Speed = " +
                gumHorizontalSpeed
            );
        }
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
        if (!isGumBoosted)
        {
            currentHorizontalSpeed =
                horizontalSpeed;

            return;
        }

        gumTimer -=
            Time.deltaTime;

        currentHorizontalSpeed =
            gumHorizontalSpeed;

        if (
            gumTimer <= 0f
        )
        {
            gumTimer = 0f;

            isGumBoosted = false;

            currentHorizontalSpeed =
                horizontalSpeed;

            if (gumDebug)
            {
                Debug.Log(
                    "[PlayerController] " +
                    "GUM ENDED | Horizontal Speed = " +
                    currentHorizontalSpeed
                );
            }
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

    private Quaternion GetRampSurfaceRotation(Vector3 normal)
{
    Vector3 rampForward = GetRampTangentWorld();

    if (rampForward.sqrMagnitude <= 0.0001f)
        rampForward = transform.forward;

    rampForward.Normalize();

    normal.Normalize();

    // Đảm bảo forward nằm trên mặt phẳng của tôn
    Vector3 surfaceForward =
        Vector3.ProjectOnPlane(
            rampForward,
            normal
        );

    if (surfaceForward.sqrMagnitude <= 0.0001f)
    {
        surfaceForward =
            Vector3.ProjectOnPlane(
                transform.forward,
                normal
            );
    }

    if (surfaceForward.sqrMagnitude <= 0.0001f)
    {
        surfaceForward = Vector3.forward;
    }

    surfaceForward.Normalize();

    // Không cho xe quay ngược đầu
    if (
        Vector3.Dot(
            surfaceForward,
            transform.forward
        ) < 0f
    )
    {
        surfaceForward = -surfaceForward;
    }

    /*
     * Đây là rotation BÁM THEO MẶT TÔN.
     *
     * surfaceForward = hướng chạy lên tôn
     * normal          = pháp tuyến thực của tôn
     *
     * LookRotation sẽ tạo pitch + roll đúng theo
     * hình học của mặt tôn.
     */
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

        flatDelta.y = 0f;

        float travelledThisFrame =
            Vector3.Dot(
                flatDelta,
                activeRampDirectionLocal
            );

        /*
         * Không dùng forwardMove fallback.
         *
         * Ba Gác có thể di chuyển riêng.
         * Progress phải dựa vào Player thực sự đi
         * qua ramp.
         */

        if (
            travelledThisFrame > 0f
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

            /*
             * Chỉ fallback nếu collider surface không raycast được.
             * Không dùng SmoothStep nữa.
             */

            newPosition.y =
                markerPosition.y +
                rampPlayerSurfaceOffset;
        }


        //=====================================================
        // SPEED BOOST WHILE CLIMBING
        //=====================================================

        float rampBoost =
            Mathf.Max(
                0f,
                rampSpeedToLaunchBonus
            );

        float targetRampSpeed =
            currentSpeed +
            rampBoost;

        currentSpeed =
            Mathf.Min(
                targetRampSpeed,
                maxSpeed +
                rampBoost
            );


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

        rampState =
            RampState.Launching;

        isGrounded =
            false;


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
        // IMPORTANT:
        // DO NOT TELEPORT TO RAMP END Z
        //=====================================================

        /*
         * Tuyệt đối không:
         *
         * newPosition.z = rampEnd.z;
         *
         * Player giữ nguyên vị trí thực tế
         * sau khi chạy hết ramp.
         */


        rampState =
            RampState.Airborne;

        rampCooldownTimer =
            Mathf.Max(
                0f,
                rampRetriggerCooldown
            );

        rampCollisionProtectionTimer =
            Mathf.Max(
                0f,
                rampLaunchCollisionProtection
            );


        activeRampCollider =
            null;


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


        //=====================================================
        // LAND
        //=====================================================

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


        rampState =
            RampState.Climbing;

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

    private void HandleRotation(float xInput)
{
    float targetY = xInput * maxTurnAngle;
    float targetZ = -xInput * maxLeanAngle;

    //=====================================================
    // RAMP - BÁM ĐÚNG HƯỚNG + ĐỘ NGHIÊNG CỦA MẶT TÔN
    //=====================================================

    if (rampState == RampState.Climbing)
    {
        Vector3 normal = activeRampLastSurfaceNormal;

        if (normal.sqrMagnitude <= 0.001f)
        {
            normal = Vector3.up;
        }

        normal.Normalize();

        // Hướng chạy dọc theo mặt tôn
        Vector3 rampForward = GetRampTangentWorld();

        // Đảm bảo hướng chạy nằm trên mặt phẳng của tôn
        rampForward = Vector3.ProjectOnPlane(
            rampForward,
            normal
        );

        if (rampForward.sqrMagnitude <= 0.001f)
        {
            rampForward = transform.forward;
            rampForward = Vector3.ProjectOnPlane(
                rampForward,
                normal
            );
        }

        rampForward.Normalize();

        // Nếu hướng bị ngược thì đảo lại
        if (Vector3.Dot(rampForward, transform.forward) < 0f)
        {
            rampForward = -rampForward;
        }

        //=================================================
        // ROTATION BÁM MẶT TÔN
        //=================================================

        Quaternion rampRotation = Quaternion.LookRotation(
            rampForward,
            normal
        );

        //=================================================
        // ĐIỀU KHIỂN LÀN
        // Chỉ thêm yaw/lean, KHÔNG phá pitch của ramp
        //=================================================

        Quaternion laneRotation = Quaternion.Euler(
            0f,
            targetY,
            targetZ
        );

        Quaternion targetRotation =
            rampRotation * laneRotation;

        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * rotationSpeed
        );

        return;
    }


    //=====================================================
    // AIRBORNE
    //=====================================================

    if (rampState == RampState.Airborne)
    {
        float targetPitch = Mathf.Clamp(
            -verticalVelocity * 0.22f,
            -18f,
            18f
        );

        Quaternion targetRotation = Quaternion.Euler(
            targetPitch,
            targetY,
            targetZ
        );

        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * rotationSpeed
        );

        return;
    }


    //=====================================================
    // NORMAL
    //=====================================================

    Quaternion normalRotation = Quaternion.Euler(
        0f,
        targetY,
        targetZ
    );

    transform.rotation = Quaternion.Lerp(
        transform.rotation,
        normalRotation,
        Time.deltaTime * rotationSpeed
    );
}


    //=========================================================
    // ACTIVATE GUM
    //=========================================================

    public void ActivateGumBoost()
    {
        if (
            gumHorizontalSpeed <=
            0f
        )
        {
            Debug.LogWarning(
                "[PlayerController] " +
                "gumHorizontalSpeed <= 0!"
            );

            return;
        }

        float oldSpeed =
            currentHorizontalSpeed;

        bool wasAlreadyBoosted =
            isGumBoosted;

        isGumBoosted =
            true;

        gumTimer =
            Mathf.Max(
                0f,
                gumDuration
            );

        currentHorizontalSpeed =
            gumHorizontalSpeed;

        PlayPlayerSFX(
            gumPickupSound,
            gumPickupVolume
        );

        if (!wasAlreadyBoosted)
        {
            PlayPlayerSFX(
                gumBoostSound,
                gumBoostVolume
            );
        }

        if (gumDebug)
        {
            Debug.Log(
                "[PlayerController] " +
                "GUM ACTIVATED! " +
                oldSpeed +
                " -> " +
                currentHorizontalSpeed +
                " | Duration = " +
                gumTimer
            );
        }
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
    // APPLY KNOCKBACK
    //=========================================================

    public void ApplyKnockback(
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

        KillPlayer(
            force
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

        if (rb != null)
        {
            rb.isKinematic =
                false;

            rb.useGravity =
                true;

            rb.linearDamping =
                knockbackDrag;

            rb.angularDamping =
                knockbackDrag;

            rb.collisionDetectionMode =
                CollisionDetectionMode.ContinuousDynamic;

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

        if (
            GameManager.Instance != null
        )
        {
            GameManager.Instance.GameOver();
        }
    }


    //=========================================================
    // OBSTACLE COLLISION
    //=========================================================

    private void ProcessObstacleCollision(
        GameObject obj
    )
    {
        if (isDead)
            return;

        if (
            IsAmbulance(obj)
        )
        {
            return;
        }

        if (
            TryConsumeShield(obj)
        )
        {
            return;
        }

        if (
            rampCollisionProtectionTimer >
            0f
        )
        {
            /*
             * Ramp protection chỉ nằm sau Shield.
             * Shield vẫn hoạt động bình thường.
             */
            return;
        }

        if (
            photonController != null &&
            photonController.IsPhotonActive
        )
        {
            return;
        }

        bool isExciter =
            obj != null &&
            obj.name
                .ToLower()
                .Contains("exciter");

        if (
            enableExplosionAnimation &&
            explosionEffectPrefab != null
        )
        {
            Vector3 spawnPos =
                obj != null
                    ?
                    (
                        transform.position +
                        obj.transform.position
                    ) * 0.5f
                    :
                    transform.position;

            Instantiate(
                explosionEffectPrefab,
                spawnPos,
                Quaternion.identity
            );
        }

        float zForce =
            isExciter
                ?
                exciterKnockbackZ
                :
                oncomingKnockbackZ;

        float randomX =
            Random.Range(
                -2.5f,
                2.5f
            );

        ApplyFatalKnockback(
            new Vector3(
                randomX,
                upwardKnockbackY,
                zForce
            )
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

        if (IsGum(obj))
        {
            CollectGum(obj);

            return;
        }

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

        if (IsShield(obj))
        {
            return;
        }


        //=====================================================
        // RAMP FIRST
        //=====================================================

        if (IsRamp(obj))
        {
            BeginRamp(other);

            return;
        }

        if (
            IsAmbulance(obj)
        )
        {
            return;
        }

        if (IsObstacle(obj))
        {
            ProcessObstacleCollision(
                obj
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

        /*
         * Không hủy ramp state ở đây.
         * Player có thể đã rời trigger nhưng vẫn
         * đang trên ramp / launch.
         */

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

        GameObject obj =
            collision.gameObject;

        if (IsGum(obj))
        {
            CollectGum(obj);

            return;
        }

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

        if (IsShield(obj))
        {
            return;
        }

        /*
         * Ramp collider non-trigger.
         */

        if (IsRamp(obj))
        {
            BeginRamp(
                collision.collider
            );

            return;
        }

        if (
            IsAmbulance(obj)
        )
        {
            return;
        }

        if (IsObstacle(obj))
        {
            ProcessObstacleCollision(
                obj
            );
        }
    }


    //=========================================================
    // COLLECT GUM
    //=========================================================

    private void CollectGum(
        GameObject obj
    )
    {
        if (obj == null)
            return;

        ActivateGumBoost();

        Gum gum =
            obj.GetComponentInParent<Gum>();

        if (gum != null)
        {
            gum.Collect();
        }
        else
        {
            Destroy(obj);
        }
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

        /*
         * Ưu tiên chính xác theo hierarchy.
         */

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

        if (IsRamp(obj))
        {
            return false;
        }

        /*
         * Khi đang chạy trên ramp:
         * không nhận Ba Gác owner làm obstacle.
         */

        if (
            rampState ==
            RampState.Climbing &&
            IsObjectFromActiveRampBaGac(obj)
        )
        {
            return false;
        }

        TrafficCarBehavior traffic =
            obj.GetComponentInParent<
                TrafficCarBehavior
            >();

        if (traffic != null)
            return true;

        TrafficVehicle vehicle =
            obj.GetComponentInParent<
                TrafficVehicle
            >();

        if (vehicle != null)
            return true;

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
}