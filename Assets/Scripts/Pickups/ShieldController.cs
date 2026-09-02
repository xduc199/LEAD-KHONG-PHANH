using System.Collections;
using UnityEngine;

public class ShieldController : MonoBehaviour
{
    //=============================================================
    // SHIELD SETTINGS
    //=============================================================

    [Header("Shield Settings")]

    [SerializeField, Min(0.5f)]
    private float duration = 8f;

    [SerializeField, Min(1)]
    private int maxHits = 1;


    //=============================================================
    // SHIELD WARNING
    //=============================================================

    [Header("Shield Warning")]

    [Tooltip("Khi Shield còn ít thời gian hơn giá trị này, Shield Effect sẽ bắt đầu nhấp nháy.")]
    [SerializeField, Min(0f)]
    private float warningTime = 2f;

    [Tooltip("Khoảng thời gian giữa mỗi lần bật/tắt Shield Effect khi đang cảnh báo.")]
    [SerializeField, Min(0.01f)]
    private float warningBlinkInterval = 0.12f;

    [Tooltip("Bật/tắt hiệu ứng nhấp nháy khi Shield sắp hết thời gian.")]
    [SerializeField]
    private bool enableWarningBlink = true;


    //=============================================================
    // INVULNERABILITY
    //=============================================================

    [Header("Block Invulnerability")]

    [Tooltip("Thời gian Player bất tử sau khi Shield block một hit.")]
    [SerializeField, Min(0f)]
    private float invulnerabilityDuration = 1.0f;

    [Tooltip("Bật hiệu ứng Player nhấp nháy trong thời gian bất tử.")]
    [SerializeField]
    private bool enableBlink = true;

    [Tooltip("Khoảng thời gian giữa mỗi lần bật/tắt Renderer.")]
    [SerializeField, Min(0.01f)]
    private float blinkInterval = 0.08f;

    [Tooltip("Nếu bật, Renderer của Player sẽ nhấp nháy trong thời gian bất tử.")]
    [SerializeField]
    private bool blinkAllChildRenderers = true;


    //=============================================================
    // VISUAL
    //=============================================================

    [Header("Shield Visual")]

    [Tooltip("Prefab hiệu ứng Shield.")]
    [SerializeField]
    private GameObject shieldEffectPrefab;

    [Tooltip("Điểm mà Shield Effect sẽ được tạo ra.")]
    [SerializeField]
    private Transform shieldEffectPoint;

    [Tooltip("Nếu không gán Shield Effect Point, có dùng chính Player làm điểm spawn hay không.")]
    [SerializeField]
    private bool fallbackToControllerTransform = true;

    [SerializeField]
    private Vector3 effectLocalPosition = Vector3.zero;

    [SerializeField]
    private Vector3 effectLocalRotation = Vector3.zero;

    [SerializeField]
    private Vector3 effectLocalScale = Vector3.one;


    //=============================================================
    // AUDIO
    //=============================================================

    [Header("Shield Audio")]

    [SerializeField]
    private AudioClip pickupSound;

    [SerializeField]
    private AudioClip breakSound;

    [SerializeField, Range(0f, 1f)]
    private float pickupVolume = 0.85f;

    [SerializeField, Range(0f, 1f)]
    private float breakVolume = 0.9f;


    //=============================================================
    // DEBUG
    //=============================================================

    [Header("Debug")]

    [SerializeField]
    private bool debugLogs = false;


    //=============================================================
    // RUNTIME - SHIELD
    //=============================================================

    private GameObject activeShieldEffect;

    private float remainingTime;

    private int remainingHits;

    private bool isActive;


    //=============================================================
    // RUNTIME - SHIELD WARNING
    //=============================================================

    private Coroutine shieldWarningCoroutine;

    private bool isShieldWarning;

    private Renderer[] shieldRenderers;

    private bool[] shieldRendererOriginalStates;


    //=============================================================
    // RUNTIME - INVULNERABILITY
    //=============================================================

    private bool isInvulnerable;

    private float invulnerabilityRemainingTime;

    private Coroutine invulnerabilityCoroutine;

    private Renderer[] playerRenderers;

    private bool[] rendererOriginalStates;


    //=============================================================
    // UPDATE
    //=============================================================

    private void Update()
    {
        //=========================================================
        // SHIELD TIMER
        //=========================================================

        if (isActive)
        {
            remainingTime -= Time.deltaTime;

            if (remainingTime <= 0f)
            {
                ExpireShield();
            }
        }


        //=========================================================
        // INVULNERABILITY TIMER
        //
        // Có thêm timer riêng để các script khác có thể kiểm tra
        // IsInvulnerable() ngay lập tức.
        //=========================================================

        if (isInvulnerable)
        {
            invulnerabilityRemainingTime -= Time.deltaTime;

            if (invulnerabilityRemainingTime <= 0f)
            {
                EndInvulnerability();
            }
        }
    }


    //=============================================================
    // ACTIVATE
    //=============================================================

    public void Activate()
    {
        //=========================================================
        // SHIELD ĐÃ ACTIVE
        //=========================================================

        if (isActive)
        {
            remainingTime = duration;

            ResetShieldWarning();

            Play2DSound(
                pickupSound,
                pickupVolume,
                "ShieldPickupAudio"
            );

            if (debugLogs)
            {
                Debug.Log(
                    "[ShieldController] Shield reset duration + pickup sound.",
                    this
                );
            }

            return;
        }


        //=========================================================
        // ACTIVATE
        //=========================================================

        isActive = true;

        remainingTime = duration;

        remainingHits =
            Mathf.Max(
                maxHits,
                1
            );


        //=========================================================
        // RESET WARNING
        //=========================================================

        ResetShieldWarning();


        //=========================================================
        // CREATE VISUAL
        //=========================================================

        CreateShieldEffect();


        //=========================================================
        // PICKUP SOUND
        //=========================================================

        Play2DSound(
            pickupSound,
            pickupVolume,
            "ShieldPickupAudio"
        );


        //=========================================================
        // DEBUG
        //=========================================================

        if (debugLogs)
        {
            Debug.Log(
                "[ShieldController] SHIELD ACTIVATED | " +
                "Duration=" + duration +
                " | Hits=" + remainingHits,
                this
            );
        }
    }


    //=============================================================
    // CREATE SHIELD EFFECT
    //=============================================================

    private void CreateShieldEffect()
    {
        DestroyShieldEffect();


        if (shieldEffectPrefab == null)
        {
            Debug.LogWarning(
                "[ShieldController] Shield Effect Prefab chưa được gán.",
                this
            );

            return;
        }


        Transform spawnPoint =
            shieldEffectPoint;


        if (spawnPoint == null)
        {
            if (!fallbackToControllerTransform)
            {
                Debug.LogWarning(
                    "[ShieldController] Shield Effect Point chưa được gán.",
                    this
                );

                return;
            }

            spawnPoint =
                transform;


            if (debugLogs)
            {
                Debug.Log(
                    "[ShieldController] Shield Effect Point chưa được gán. " +
                    "Đang fallback về ShieldController Transform.",
                    this
                );
            }
        }


        //=========================================================
        // INSTANTIATE
        //=========================================================

        activeShieldEffect =
            Instantiate(
                shieldEffectPrefab,
                spawnPoint
            );


        if (activeShieldEffect == null)
            return;


        Transform effectTransform =
            activeShieldEffect.transform;


        effectTransform.localPosition =
            effectLocalPosition;

        effectTransform.localEulerAngles =
            effectLocalRotation;

        effectTransform.localScale =
            effectLocalScale;


        activeShieldEffect.SetActive(true);


        //=========================================================
        // RENDERERS
        //=========================================================

        Renderer[] renderers =
            activeShieldEffect.GetComponentsInChildren<Renderer>(
                true
            );


        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;

            renderers[i].enabled = true;
        }


        //=========================================================
        // CACHE SHIELD RENDERERS
        //=========================================================

        CacheShieldRenderers();


        //=========================================================
        // PARTICLES
        //=========================================================

        ParticleSystem[] particles =
            activeShieldEffect.GetComponentsInChildren<ParticleSystem>(
                true
            );


        for (int i = 0; i < particles.Length; i++)
        {
            if (particles[i] == null)
                continue;

            particles[i].gameObject.SetActive(true);

            particles[i].Clear(true);

            particles[i].Play(true);
        }


        //=========================================================
        // TRAILS
        //=========================================================

        TrailRenderer[] trails =
            activeShieldEffect.GetComponentsInChildren<TrailRenderer>(
                true
            );


        for (int i = 0; i < trails.Length; i++)
        {
            if (trails[i] == null)
                continue;

            trails[i].enabled = true;
        }


        //=========================================================
        // LINES
        //=========================================================

        LineRenderer[] lines =
            activeShieldEffect.GetComponentsInChildren<LineRenderer>(
                true
            );


        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i] == null)
                continue;

            lines[i].enabled = true;
        }


        //=========================================================
        // DEBUG
        //=========================================================

        if (debugLogs)
        {
            Debug.Log(
                "[ShieldController] EFFECT CREATED | " +
                "Point=" + spawnPoint.name +
                " | Renderers=" + renderers.Length +
                " | Particles=" + particles.Length +
                " | Trails=" + trails.Length +
                " | Lines=" + lines.Length,
                this
            );
        }
    }


    //=============================================================
    // SHIELD WARNING
    //=============================================================

    private void CheckShieldWarning()
    {
        if (!isActive)
            return;

        if (!enableWarningBlink)
            return;

        if (activeShieldEffect == null)
            return;

        if (isShieldWarning)
            return;

        if (warningTime <= 0f)
            return;

        if (remainingTime > warningTime)
            return;


        //=========================================================
        // START WARNING BLINK
        //=========================================================

        isShieldWarning = true;

        CacheShieldRenderers();

        if (shieldWarningCoroutine != null)
        {
            StopCoroutine(
                shieldWarningCoroutine
            );

            shieldWarningCoroutine = null;
        }

        shieldWarningCoroutine =
            StartCoroutine(
                ShieldWarningBlinkRoutine()
            );


        if (debugLogs)
        {
            Debug.Log(
                "[ShieldController] SHIELD WARNING | " +
                "Remaining=" + remainingTime.ToString("F2") +
                "s",
                this
            );
        }
    }


    //=============================================================
    // SHIELD WARNING UPDATE
    //=============================================================

    private void LateUpdate()
    {
        if (!isActive)
            return;

        if (!isShieldWarning)
        {
            CheckShieldWarning();
        }
    }


    //=============================================================
    // SHIELD WARNING BLINK
    //=============================================================

    private IEnumerator ShieldWarningBlinkRoutine()
    {
        float interval =
            Mathf.Max(
                0.01f,
                warningBlinkInterval
            );

        bool visible = true;


        while (
            isActive &&
            activeShieldEffect != null &&
            remainingTime > 0f
        )
        {
            visible = !visible;

            SetShieldRenderersVisible(
                visible
            );


            yield return new WaitForSeconds(
                interval
            );
        }


        //=========================================================
        // RESTORE SHIELD VISUAL
        //=========================================================

        RestoreShieldRenderers();

        shieldWarningCoroutine = null;
    }


    //=============================================================
    // RESET SHIELD WARNING
    //=============================================================

    private void ResetShieldWarning()
    {
        isShieldWarning = false;


        if (shieldWarningCoroutine != null)
        {
            StopCoroutine(
                shieldWarningCoroutine
            );

            shieldWarningCoroutine = null;
        }


        RestoreShieldRenderers();
    }


    //=============================================================
    // CACHE SHIELD RENDERERS
    //=============================================================

    private void CacheShieldRenderers()
    {
        if (activeShieldEffect == null)
            return;


        shieldRenderers =
            activeShieldEffect.GetComponentsInChildren<Renderer>(
                true
            );


        if (
            shieldRenderers == null ||
            shieldRenderers.Length == 0
        )
        {
            shieldRendererOriginalStates = null;
            return;
        }


        shieldRendererOriginalStates =
            new bool[shieldRenderers.Length];


        for (
            int i = 0;
            i < shieldRenderers.Length;
            i++
        )
        {
            if (shieldRenderers[i] == null)
            {
                shieldRendererOriginalStates[i] = false;
                continue;
            }


            shieldRendererOriginalStates[i] =
                shieldRenderers[i].enabled;
        }
    }


    //=============================================================
    // SET SHIELD RENDERERS
    //=============================================================

    private void SetShieldRenderersVisible(
        bool visible
    )
    {
        if (
            shieldRenderers == null ||
            shieldRenderers.Length == 0
        )
        {
            return;
        }


        for (
            int i = 0;
            i < shieldRenderers.Length;
            i++
        )
        {
            Renderer renderer =
                shieldRenderers[i];


            if (renderer == null)
                continue;


            renderer.enabled =
                visible &&
                (
                    shieldRendererOriginalStates == null ||
                    i >= shieldRendererOriginalStates.Length ||
                    shieldRendererOriginalStates[i]
                );
        }
    }


    //=============================================================
    // RESTORE SHIELD RENDERERS
    //=============================================================

    private void RestoreShieldRenderers()
    {
        if (
            shieldRenderers == null ||
            shieldRenderers.Length == 0
        )
        {
            return;
        }


        for (
            int i = 0;
            i < shieldRenderers.Length;
            i++
        )
        {
            Renderer renderer =
                shieldRenderers[i];


            if (renderer == null)
                continue;


            if (
                shieldRendererOriginalStates != null &&
                i < shieldRendererOriginalStates.Length
            )
            {
                renderer.enabled =
                    shieldRendererOriginalStates[i];
            }
            else
            {
                renderer.enabled = true;
            }
        }
    }


    //=============================================================
    // CONSUME SHIELD
    //=============================================================

    /// <summary>
    /// Shield chặn một hit.
    ///
    /// TRUE:
    ///     Hit đã được Shield chặn.
    ///
    /// FALSE:
    ///     Shield không active / không còn hit.
    ///
    /// Khi block thành công:
    ///     - Shield mất 1 hit
    ///     - Shield có thể vỡ
    ///     - Player bắt đầu bất tử
    ///     - Player nhấp nháy
    ///     - TRUE được trả về
    /// </summary>
    public bool ConsumeShield()
    {
        if (!isActive)
            return false;


        if (remainingHits <= 0)
        {
            BreakShield();

            return false;
        }


        //=========================================================
        // CONSUME 1 HIT
        //=========================================================

        remainingHits--;


        if (debugLogs)
        {
            Debug.Log(
                "[ShieldController] SHIELD BLOCKED HIT | " +
                "Remaining=" + remainingHits,
                this
            );
        }


        //=========================================================
        // START INVULNERABILITY
        //
        // Gọi ngay tại thời điểm Shield block.
        //
        // Không phụ thuộc vào việc Shield còn active hay đã Break.
        //=========================================================

        StartInvulnerability();


        //=========================================================
        // BREAK NẾU HẾT HIT
        //=========================================================

        if (remainingHits <= 0)
        {
            BreakShield();
        }


        //=========================================================
        // LUÔN TRUE CHO CÚ HIT VỪA BLOCK
        //=========================================================

        return true;
    }


    //=============================================================
    // START INVULNERABILITY
    //=============================================================

    private void StartInvulnerability()
    {
        //=========================================================
        // HỦY COROUTINE CŨ
        //=========================================================

        if (invulnerabilityCoroutine != null)
        {
            StopCoroutine(
                invulnerabilityCoroutine
            );

            invulnerabilityCoroutine = null;
        }


        //=========================================================
        // SET STATE
        //=========================================================

        isInvulnerable = true;

        invulnerabilityRemainingTime =
            Mathf.Max(
                0f,
                invulnerabilityDuration
            );


        //=========================================================
        // CACHE RENDERERS
        //=========================================================

        CachePlayerRenderers();


        //=========================================================
        // KHÔNG CẦN BLINK
        //=========================================================

        if (
            !enableBlink ||
            invulnerabilityDuration <= 0f
        )
        {
            return;
        }


        //=========================================================
        // START BLINK
        //=========================================================

        invulnerabilityCoroutine =
            StartCoroutine(
                InvulnerabilityBlinkRoutine()
            );


        if (debugLogs)
        {
            Debug.Log(
                "[ShieldController] PLAYER INVULNERABLE | " +
                "Duration=" +
                invulnerabilityDuration,
                this
            );
        }
    }


    //=============================================================
    // INVULNERABILITY BLINK
    //=============================================================

    private IEnumerator InvulnerabilityBlinkRoutine()
    {
        float elapsed = 0f;

        bool visible = true;


        float interval =
            Mathf.Max(
                0.01f,
                blinkInterval
            );


        while (
            isInvulnerable &&
            elapsed < invulnerabilityDuration
        )
        {
            visible = !visible;

            SetPlayerRenderersVisible(
                visible
            );


            yield return new WaitForSeconds(
                interval
            );


            elapsed += interval;
        }


        //=========================================================
        // RESTORE RENDERER
        //=========================================================

        RestorePlayerRenderers();

        invulnerabilityCoroutine = null;
    }


    //=============================================================
    // END INVULNERABILITY
    //=============================================================

    private void EndInvulnerability()
    {
        if (!isInvulnerable)
            return;


        isInvulnerable = false;

        invulnerabilityRemainingTime = 0f;


        //=========================================================
        // STOP BLINK
        //=========================================================

        if (invulnerabilityCoroutine != null)
        {
            StopCoroutine(
                invulnerabilityCoroutine
            );

            invulnerabilityCoroutine = null;
        }


        //=========================================================
        // RESTORE PLAYER VISUAL
        //=========================================================

        RestorePlayerRenderers();


        if (debugLogs)
        {
            Debug.Log(
                "[ShieldController] PLAYER INVULNERABILITY ENDED.",
                this
            );
        }
    }


    //=============================================================
    // CACHE PLAYER RENDERERS
    //=============================================================

    private void CachePlayerRenderers()
    {
        if (
            playerRenderers != null &&
            playerRenderers.Length > 0
        )
        {
            return;
        }


        if (blinkAllChildRenderers)
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


        //=========================================================
        // SAVE ORIGINAL STATES
        //=========================================================

        if (
            playerRenderers != null &&
            playerRenderers.Length > 0
        )
        {
            rendererOriginalStates =
                new bool[playerRenderers.Length];


            for (
                int i = 0;
                i < playerRenderers.Length;
                i++
            )
            {
                if (playerRenderers[i] == null)
                {
                    rendererOriginalStates[i] = false;
                    continue;
                }


                rendererOriginalStates[i] =
                    playerRenderers[i].enabled;
            }
        }
    }


    //=============================================================
    // SET PLAYER RENDERERS
    //=============================================================

    private void SetPlayerRenderersVisible(
        bool visible
    )
    {
        if (
            playerRenderers == null ||
            playerRenderers.Length == 0
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
                visible &&
                (
                    rendererOriginalStates == null ||
                    i >= rendererOriginalStates.Length ||
                    rendererOriginalStates[i]
                );
        }
    }


    //=============================================================
    // RESTORE PLAYER RENDERERS
    //=============================================================

    private void RestorePlayerRenderers()
    {
        if (
            playerRenderers == null ||
            playerRenderers.Length == 0
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


            if (
                rendererOriginalStates != null &&
                i < rendererOriginalStates.Length
            )
            {
                renderer.enabled =
                    rendererOriginalStates[i];
            }
            else
            {
                renderer.enabled = true;
            }
        }
    }


    //=============================================================
    // BREAK
    //=============================================================

    private void BreakShield()
    {
        if (!isActive)
            return;


        isActive = false;

        remainingTime = 0f;

        remainingHits = 0;


        //=========================================================
        // STOP WARNING BLINK
        //=========================================================

        ResetShieldWarning();


        //=========================================================
        // DESTROY EFFECT
        //=========================================================

        DestroyShieldEffect();


        //=========================================================
        // BREAK AUDIO
        //=========================================================

        Play2DSound(
            breakSound,
            breakVolume,
            "ShieldBreakAudio"
        );


        if (debugLogs)
        {
            Debug.Log(
                "[ShieldController] SHIELD BROKEN",
                this
            );
        }
    }


    //=============================================================
    // EXPIRE
    //=============================================================

    private void ExpireShield()
    {
        if (!isActive)
            return;


        isActive = false;

        remainingTime = 0f;

        remainingHits = 0;


        //=========================================================
        // STOP WARNING BLINK
        //=========================================================

        ResetShieldWarning();


        //=========================================================
        // DESTROY EFFECT
        //=========================================================

        DestroyShieldEffect();


        if (debugLogs)
        {
            Debug.Log(
                "[ShieldController] SHIELD EXPIRED",
                this
            );
        }
    }


    //=============================================================
    // DESTROY EFFECT
    //=============================================================

    private void DestroyShieldEffect()
    {
        //=========================================================
        // STOP WARNING BLINK
        //=========================================================

        if (shieldWarningCoroutine != null)
        {
            StopCoroutine(
                shieldWarningCoroutine
            );

            shieldWarningCoroutine = null;
        }


        isShieldWarning = false;


        //=========================================================
        // DESTROY
        //=========================================================

        if (activeShieldEffect == null)
            return;


        Destroy(
            activeShieldEffect
        );

        activeShieldEffect = null;


        shieldRenderers = null;

        shieldRendererOriginalStates = null;
    }


    //=============================================================
    // AUDIO 2D
    //=============================================================

    private void Play2DSound(
        AudioClip clip,
        float volume,
        string objectName
    )
    {
        if (clip == null)
            return;


        GameObject audioObject =
            new GameObject(
                objectName
            );


        AudioSource source =
            audioObject.AddComponent<AudioSource>();


        source.clip =
            clip;

        source.volume =
            volume;

        source.spatialBlend =
            0f;

        source.panStereo =
            0f;

        source.dopplerLevel =
            0f;

        source.playOnAwake =
            false;

        source.loop =
            false;


        source.Play();


        Destroy(
            audioObject,
            clip.length + 0.1f
        );
    }


    //=============================================================
    // PUBLIC API
    //=============================================================

    public bool IsActive()
    {
        return isActive;
    }


    public float GetRemainingTime()
    {
        return Mathf.Max(
            remainingTime,
            0f
        );
    }


    public int GetRemainingHits()
    {
        return remainingHits;
    }


    //=============================================================
    // INVULNERABILITY API
    //=============================================================

    /// <summary>
    /// TRUE khi Player đang trong thời gian bất tử
    /// sau khi Shield block.
    /// </summary>
    public bool IsInvulnerable()
    {
        return isInvulnerable;
    }


    /// <summary>
    /// Thời gian bất tử còn lại.
    /// </summary>
    public float GetInvulnerabilityRemainingTime()
    {
        return Mathf.Max(
            invulnerabilityRemainingTime,
            0f
        );
    }


    /// <summary>
    /// Cho phép script khác chủ động kết thúc bất tử nếu cần.
    /// </summary>
    public void EndInvulnerabilityManually()
    {
        EndInvulnerability();
    }


    //=============================================================
    // ON DESTROY
    //=============================================================

    private void OnDestroy()
    {
        if (invulnerabilityCoroutine != null)
        {
            StopCoroutine(
                invulnerabilityCoroutine
            );

            invulnerabilityCoroutine = null;
        }


        if (shieldWarningCoroutine != null)
        {
            StopCoroutine(
                shieldWarningCoroutine
            );

            shieldWarningCoroutine = null;
        }


        RestorePlayerRenderers();

        RestoreShieldRenderers();
    }


    //=============================================================
    // VALIDATE
    //=============================================================

    private void OnValidate()
    {
        duration =
            Mathf.Max(
                0.5f,
                duration
            );


        maxHits =
            Mathf.Max(
                1,
                maxHits
            );


        warningTime =
            Mathf.Max(
                0f,
                warningTime
            );


        warningBlinkInterval =
            Mathf.Max(
                0.01f,
                warningBlinkInterval
            );


        invulnerabilityDuration =
            Mathf.Max(
                0f,
                invulnerabilityDuration
            );


        blinkInterval =
            Mathf.Max(
                0.01f,
                blinkInterval
            );


        pickupVolume =
            Mathf.Clamp01(
                pickupVolume
            );


        breakVolume =
            Mathf.Clamp01(
                breakVolume
            );


        effectLocalScale.x =
            Mathf.Max(
                0.01f,
                effectLocalScale.x
            );


        effectLocalScale.y =
            Mathf.Max(
                0.01f,
                effectLocalScale.y
            );


        effectLocalScale.z =
            Mathf.Max(
                0.01f,
                effectLocalScale.z
            );
    }
}
