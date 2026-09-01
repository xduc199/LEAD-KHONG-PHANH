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
    // JUMP
    //=========================================================

    [Header("Jump & Ramp Settings")]

    [SerializeField] private float jumpForce = 12f;

    [SerializeField] private float gravity = 30f;

    private float verticalVelocity;

    private bool isGrounded = true;

    private float groundY;


    //=========================================================
    // KNOCKBACK
    //=========================================================

    [Header("Knockback & Physics Settings")]

    [SerializeField] private float oncomingKnockbackZ = -12f;

    [SerializeField] private float exciterKnockbackZ = 12f;

    [SerializeField] private float upwardKnockbackY = 4.5f;

    [SerializeField] private float knockbackDrag = 2f;


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

    /*
     * QUAN TRỌNG
     *
     * shieldHitCooldownTimer:
     *     Khoảng thời gian bảo vệ sau khi Shield vừa block.
     *
     * isShieldBlockingHit:
     *     Đánh dấu rằng hit hiện tại đã được Shield chặn.
     *
     * Mục đích:
     *
     * Hit đầu tiên:
     *     Shield active
     *     -> ConsumeShield()
     *     -> Player sống
     *
     * Collider/hệ thống khác cùng hit:
     *     -> KHÔNG được giết Player
     *
     * Đây là lớp bảo vệ chống trường hợp:
     *
     * OnCollisionEnter
     * +
     * Exciter TriggerPlayerHit
     * +
     * ApplyKnockback
     * +
     * ApplyFatalKnockback
     *
     * cùng xử lý một vụ va chạm.
     */

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
    // PUBLIC API
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


        //=====================================================
        // PHOTON
        //=====================================================

        if (photonController == null)
        {
            photonController =
                GetComponent<PhotonController>();
        }


        //=====================================================
        // SHIELD
        //=====================================================

        FindShieldController();


        //=====================================================
        // RIGIDBODY
        //=====================================================

        rb =
            GetComponent<Rigidbody>();


        //=====================================================
        // PLAYER RENDERERS
        //=====================================================

        CachePlayerRenderers();


        //=====================================================
        // AUDIO
        //=====================================================

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
        //=====================================================
        // ENGINE SOURCE
        //=====================================================

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


        //=====================================================
        // ENGINE CONFIG
        //=====================================================

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


        //=====================================================
        // SFX SOURCE
        //=====================================================

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


        //=====================================================
        // SFX CONFIG
        //=====================================================

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


        if (shieldFlashCoroutine != null)
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
                interval * flashCount * 2f,
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


        float horizontalMove =
            xInput *
            currentHorizontalSpeed *
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


        if (!isGrounded)
        {
            verticalVelocity -=
                gravity *
                Time.deltaTime;


            newPosition.y +=
                verticalVelocity *
                Time.deltaTime;


            if (
                newPosition.y <=
                groundY
            )
            {
                newPosition.y =
                    groundY;


                verticalVelocity =
                    0f;


                isGrounded =
                    true;
            }
        }


        transform.position =
            newPosition;


        HandleRotation(
            xInput
        );
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


        Quaternion targetRotation =
            Quaternion.Euler(
                0f,
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

private bool TryConsumeShield(
    GameObject obstacle
)
{
    //=====================================================
    // PLAYER ĐANG ĐƯỢC SHIELD BẢO VỆ
    //
    // Shield đã block hit trước đó và Player vẫn đang
    // trong thời gian bất tử.
    //
    // KHÔNG ConsumeShield() lần nữa.
    // KHÔNG cho hit đi xuống KillPlayer().
    //=====================================================

    if (
        shieldController != null &&
        shieldController.IsInvulnerable()
    )
    {
        return true;
    }


    //=====================================================
    // HIT HIỆN TẠI ĐÃ ĐƯỢC BLOCK
    //=====================================================

    if (isShieldBlockingHit)
    {
        return true;
    }


    //=====================================================
    // PLAYER ĐANG TRONG COOLDOWN BẢO VỆ
    //=====================================================

    if (
        shieldHitCooldownTimer > 0f
    )
    {
        isShieldBlockingHit = true;

        return true;
    }


    //=====================================================
    // FIND SHIELD
    //=====================================================

    if (
        shieldController == null
    )
    {
        FindShieldController();
    }


    //=====================================================
    // NO SHIELD
    //=====================================================

    if (
        shieldController == null
    )
    {
        return false;
    }


    //=====================================================
    // SHIELD ĐANG BẤT TỬ
    //
    // Kiểm tra lại lần nữa sau FindShieldController().
    //=====================================================

    if (
        shieldController.IsInvulnerable()
    )
    {
        return true;
    }


    //=====================================================
    // SHIELD INACTIVE
    //=====================================================

    if (
        !shieldController.IsActive()
    )
    {
        return false;
    }


    //=====================================================
    // CONSUME SHIELD
    //=====================================================

    bool blocked =
        shieldController.ConsumeShield();


    if (!blocked)
    {
        return false;
    }


    //=====================================================
    // MARK HIT PROTECTED
    //=====================================================

    isShieldBlockingHit =
        true;


    shieldHitCooldownTimer =
        Mathf.Max(
            0.01f,
            shieldHitCooldown
        );


    //=====================================================
    // FLASH
    //=====================================================

    PlayShieldFlash();


    //=====================================================
    // AUDIO
    //=====================================================

    PlayPlayerSFX(
        shieldBlockSound,
        shieldBlockVolume
    );


    //=====================================================
    // PUSH
    //=====================================================

    PushAwayFromObstacle(
        obstacle
    );


    //=====================================================
    // DEBUG
    //=====================================================

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


        away.y = 0f;


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
        //=====================================================
        // PLAYER ĐÃ CHẾT
        //=====================================================

        if (isDead)
            return;


        //=====================================================
        // PHOTON
        //=====================================================

        if (
            photonController != null &&
            photonController.IsPhotonActive
        )
        {
            return;
        }


        //=====================================================
        // SHIELD
        //
        // Đây là lớp bảo vệ cuối cùng.
        //
        // Exciter gọi:
        //
        //     player.ApplyKnockback()
        //
        // Shield vẫn được xử lý ở đây.
        //=====================================================

        if (
            TryConsumeShield(null)
        )
        {
            return;
        }


        //=====================================================
        // FATAL
        //=====================================================

        KillPlayer(
            force
        );
    }


    //=========================================================
    // KILL PLAYER
    //=========================================================

    /*
     * Toàn bộ logic chết của Player được gom vào một chỗ.
     *
     * Điều này tránh:
     *
     * ApplyKnockback()
     * ApplyFatalKnockback()
     *
     * mỗi hàm tự có một logic chết riêng.
     */

    private void KillPlayer(
        Vector3 force
    )
    {
        if (isDead)
            return;


        //=====================================================
        // SHIELD PROTECTION
        //
        // Kiểm tra lại lần cuối trước khi chết.
        //=====================================================

        if (
            isShieldBlockingHit
        )
        {
            return;
        }


        if (
            shieldHitCooldownTimer > 0f
        )
        {
            return;
        }


        //=====================================================
        // DEAD
        //=====================================================

        isDead =
            true;


        StopEngineAudio();


        //=====================================================
        // STOP SHIELD FLASH
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
        // PHYSICS
        //=====================================================

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


        //=====================================================
        // GAME OVER
        //=====================================================

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


        //=====================================================
        // SHIELD
        //
        // Shield luôn được xử lý trước.
        //=====================================================

        if (
            TryConsumeShield(obj)
        )
        {
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
            return;
        }


        //=====================================================
        // EXCITER
        //=====================================================

        bool isExciter =
            obj != null &&
            obj.name
                .ToLower()
                .Contains("exciter");


        //=====================================================
        // EXPLOSION
        //=====================================================

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


        //=====================================================
        // KNOCKBACK
        //=====================================================

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
    // FATAL KNOCKBACK
    //=========================================================

    private void ApplyFatalKnockback(
        Vector3 force
    )
    {
        if (isDead)
            return;


        //=====================================================
        // SHIELD
        //
        // Lớp bảo vệ cuối cùng.
        //=====================================================

        if (
            TryConsumeShield(null)
        )
        {
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
            return;
        }


        //=====================================================
        // FATAL
        //=====================================================

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
        // SHIELD ITEM
        //=====================================================

        if (IsShield(obj))
        {
            return;
        }


        //=====================================================
        // RAMP
        //=====================================================

        if (IsRamp(obj))
        {
            JumpFromRamp();

            return;
        }


        //=====================================================
        // OBSTACLE
        //=====================================================

        if (IsObstacle(obj))
        {
            ProcessObstacleCollision(
                obj
            );
        }
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
        // SHIELD ITEM
        //=====================================================

        if (IsShield(obj))
        {
            return;
        }


        //=====================================================
        // OBSTACLE
        //=====================================================

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
    // RAMP
    //=========================================================

    private void JumpFromRamp()
    {
        if (!isGrounded)
            return;


        isGrounded =
            false;


        verticalVelocity =
            jumpForce;
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


        string name =
            obj.name.ToLower();


        return
            obj.CompareTag("Ramp") ||
            name.Contains("ramp") ||
            name.Contains("docton");
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