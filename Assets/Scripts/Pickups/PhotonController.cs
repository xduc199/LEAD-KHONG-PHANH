using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class PhotonController : MonoBehaviour
{
    //=========================================================
    // PHOTON SPEED LINES - CANVAS VIDEO
    //=========================================================

    [Header("Photon Speed Lines - Canvas")]

    [Tooltip(
        "Canvas chứa hiệu ứng vệt sáng Photon. " +
        "Canvas này chỉ bật khi Photon đang hoạt động."
    )]
    [SerializeField]
    private GameObject photonSpeedLinesCanvas;

    [Tooltip(
        "VideoPlayer nằm trong PhotonSpeedCanvas. " +
        "Video này phát speed lines."
    )]
    [SerializeField]
    private VideoPlayer photonSpeedLinesVideo;


    //=========================================================
    // LEGACY / CAR SMOKE EFFECT
    //=========================================================

    [Header("Photon Car Smoke Effect")]

    [Tooltip(
        "Prefab hiệu ứng khói xe Photon hiện tại. " +
        "Giữ nguyên hệ thống cũ."
    )]
    [SerializeField]
    private GameObject photonSpeedEffectPrefab;

    [Tooltip(
        "Điểm spawn hiệu ứng khói xe Photon."
    )]
    [SerializeField]
    private Transform photonEffectPoint;


    //=========================================================
    // PHOTON BURST
    //=========================================================

    [Header("Photon Burst Settings")]

    [SerializeField]
    private float photonDuration = 6f;

    [SerializeField]
    private float photonSpeedMultiplier = 1.6f;


    //=========================================================
    // PHOTON HIT FORCE
    //=========================================================

    [Header("Photon Hit Force")]

    [Tooltip(
        "Tổng lực hất Traffic."
    )]
    [SerializeField]
    private float photonHitForce = 30f;

    [Tooltip(
        "Độ ưu tiên lực bay lên. " +
        "Tăng giá trị này để xe bay cao hơn."
    )]
    [SerializeField]
    private float photonUpwardForce = 10f;

    [Tooltip(
        "Độ ưu tiên lực bay về phía trước."
    )]
    [SerializeField]
    private float photonForwardForce = 6f;

    [Tooltip(
        "Độ lệch ngẫu nhiên trái/phải."
    )]
    [SerializeField]
    private float photonSideRandomForce = 0.8f;

    [Tooltip(
        "Lực xoay khi Traffic bị hất."
    )]
    [SerializeField]
    private float photonTorqueForce = 25f;


    //=========================================================
    // HIT DETECTION
    //=========================================================

    [Header("Photon Collision Detection")]

    [Tooltip(
        "Photon tự bắt OnCollisionEnter. " +
        "Nên bật để không phụ thuộc script collision khác."
    )]
    [SerializeField]
    private bool useCollisionDetection = true;

    [Tooltip(
        "Photon tự bắt OnTriggerEnter."
    )]
    [SerializeField]
    private bool useTriggerDetection = true;

    [Tooltip(
        "Khoảng thời gian chống cùng một traffic bị hit liên tục."
    )]
    [SerializeField]
    private float hitCooldown = 0.15f;

    private readonly Dictionary<int, float>
        hitCooldowns =
            new Dictionary<int, float>();


    //=========================================================
    // CAR RENDERER
    //=========================================================

    [Header("Photon Car Renderers")]

    [SerializeField]
    private bool autoFindRenderers = true;

    [SerializeField]
    private Renderer[] carBodyRenderers;

    [SerializeField]
    private bool affectAllRenderers = true;

    [SerializeField]
    private string[] ignoredRendererNames;


    //=========================================================
    // PHOTON VISUAL
    //=========================================================

    [Header("Photon Visual")]

    [SerializeField]
    private Color photonYellowColor =
        Color.yellow;

    [SerializeField]
    private GameObject photonVisual;


    //=========================================================
    // HIT EFFECT
    //=========================================================

    [Header("Photon Hit Effect")]

    [SerializeField]
    private bool enablePhotonHitEffect = true;

    [SerializeField]
    private GameObject photonHitEffectPrefab;

    [SerializeField]
    private float photonHitEffectLifetime = 1.2f;


    //=========================================================
    // AUDIO
    //=========================================================

    [Header("Photon Audio")]

    [SerializeField]
    private AudioClip photonActivateClip;

    [SerializeField]
    private AudioClip photonHitClip;

    [Range(0f, 1f)]
    [SerializeField]
    private float photonActivateVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField]
    private float photonHitVolume = 1f;

    [SerializeField]
    private AudioSource photonAudioSource;


    //=========================================================
    // STATE
    //=========================================================

    private bool isPhotonActive;

    private float photonTimer;

    private GameObject currentSpeedEffect;


    //=========================================================
    // PLAYER CONTROLLER
    //=========================================================

    private PlayerController playerController;


    //=========================================================
    // MATERIAL CACHE
    //=========================================================

    private Material[][] photonMaterials;

    private Color[][] originalColors;

    private Color[][] originalEmissionColors;

    private bool[][] originalEmissionEnabled;


    //=========================================================
    // PUBLIC
    //=========================================================

    public bool IsPhotonActive
    {
        get
        {
            return isPhotonActive;
        }
    }

    public float PhotonTimeRemaining
    {
        get
        {
            return photonTimer;
        }
    }

    public float PhotonSpeedMultiplier
    {
        get
        {
            return photonSpeedMultiplier;
        }
    }


    //=========================================================
    // AWAKE
    //=========================================================

    private void Awake()
    {
        playerController =
            GetComponent<PlayerController>();

        if (playerController == null)
        {
            playerController =
                GetComponentInParent<PlayerController>();
        }

        SetupCarRenderers();

        SetupAudioSource();

        if (photonVisual != null)
        {
            photonVisual.SetActive(false);
        }

        SetupPhotonSpeedLines();
    }


    //=========================================================
    // SETUP PHOTON SPEED LINES
    //=========================================================

    private void SetupPhotonSpeedLines()
    {
        if (photonSpeedLinesVideo != null)
        {
            photonSpeedLinesVideo.Stop();

            photonSpeedLinesVideo.time = 0f;
        }

        if (photonSpeedLinesCanvas != null)
        {
            photonSpeedLinesCanvas.SetActive(false);
        }
    }


    //=========================================================
    // UPDATE
    //=========================================================

    private void Update()
    {
        CleanupHitCooldowns();

        if (!isPhotonActive)
        {
            return;
        }

        photonTimer -=
            Time.deltaTime;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdatePhotonTimerUI(
                photonTimer
            );
        }

        if (photonTimer <= 2f)
        {
            UpdateFinalFlash();
        }

        if (photonTimer <= 0f)
        {
            DeactivatePhoton();
        }
    }


    //=========================================================
    // COLLISION ENTER
    //=========================================================

    private void OnCollisionEnter(
        Collision collision
    )
    {
        if (!useCollisionDetection)
        {
            return;
        }

        if (!isPhotonActive)
        {
            return;
        }

        if (collision == null)
        {
            return;
        }

        ProcessPhotonHit(
            collision.collider
        );
    }


    //=========================================================
    // TRIGGER ENTER
    //=========================================================

    private void OnTriggerEnter(
        Collider other
    )
    {
        if (!useTriggerDetection)
        {
            return;
        }

        if (!isPhotonActive)
        {
            return;
        }

        if (other == null)
        {
            return;
        }

        ProcessPhotonHit(
            other
        );
    }


    //=========================================================
    // PROCESS HIT
    //=========================================================

    private void ProcessPhotonHit(
        Collider hitCollider
    )
    {
        if (!isPhotonActive)
        {
            return;
        }

        if (hitCollider == null)
        {
            return;
        }

        GameObject obj =
            hitCollider.gameObject;

        if (obj == null)
        {
            return;
        }

        //=====================================================
        // KHÔNG TỰ HIT
        //=====================================================

        if (
            obj.transform.root ==
            transform.root
        )
        {
            return;
        }

        //=====================================================
        // BA GAC / RAMP PROTECTION
        //=====================================================

        if (
            playerController != null &&
            playerController.IsPhotonRampCollisionBlocked(
                hitCollider
            )
        )
        {
            return;
        }

        if (IsBaGacRampCollider(obj))
        {
            return;
        }

        //=====================================================
        // BA GAC BODY - RAMP TRAVERSAL HARD BLOCK
        //=====================================================

        if (
            playerController != null &&
            playerController.IsRampTraversalActive() &&
            IsBaGacObject(obj)
        )
        {
            return;
        }

        //=====================================================
        // BA GAC BODY - DIRECT PHOTON HIT
        //=====================================================

        if (IsBaGacObject(obj))
        {
            HitBaGacObject(obj);
            return;
        }

        //=====================================================
        // TÌM TRAFFIC
        //=====================================================

        TrafficVehicle traffic =
            obj.GetComponent<TrafficVehicle>();

        if (traffic == null)
        {
            traffic =
                obj.GetComponentInParent<TrafficVehicle>();
        }

        if (traffic == null)
        {
            traffic =
                obj.GetComponentInChildren<TrafficVehicle>();
        }

        if (traffic == null)
        {
            return;
        }

        HitTrafficVehicle(
            traffic
        );
    }


    //=========================================================
    // SETUP RENDERERS
    //=========================================================

    private void SetupCarRenderers()
    {
        if (autoFindRenderers)
        {
            Renderer[] allRenderers =
                GetComponentsInChildren<Renderer>(
                    true
                );

            List<Renderer> validRenderers =
                new List<Renderer>();

            foreach (
                Renderer renderer
                in allRenderers
            )
            {
                if (renderer == null)
                {
                    continue;
                }

                if (
                    !(renderer is MeshRenderer) &&
                    !(renderer is SkinnedMeshRenderer)
                )
                {
                    continue;
                }

                if (
                    !affectAllRenderers &&
                    IsIgnoredRenderer(renderer)
                )
                {
                    continue;
                }

                validRenderers.Add(
                    renderer
                );
            }

            carBodyRenderers =
                validRenderers.ToArray();
        }

        if (
            carBodyRenderers == null ||
            carBodyRenderers.Length == 0
        )
        {
            Debug.LogWarning(
                "[PhotonController] " +
                "Không tìm thấy Renderer của Player."
            );

            return;
        }

        photonMaterials =
            new Material[
                carBodyRenderers.Length
            ][];

        originalColors =
            new Color[
                carBodyRenderers.Length
            ][];

        originalEmissionColors =
            new Color[
                carBodyRenderers.Length
            ][];

        originalEmissionEnabled =
            new bool[
                carBodyRenderers.Length
            ][];

        for (
            int i = 0;
            i < carBodyRenderers.Length;
            i++
        )
        {
            Renderer renderer =
                carBodyRenderers[i];

            if (renderer == null)
            {
                continue;
            }

            Material[] materials =
                renderer.materials;

            photonMaterials[i] =
                materials;

            originalColors[i] =
                new Color[
                    materials.Length
                ];

            originalEmissionColors[i] =
                new Color[
                    materials.Length
                ];

            originalEmissionEnabled[i] =
                new bool[
                    materials.Length
                ];

            for (
                int j = 0;
                j < materials.Length;
                j++
            )
            {
                Material material =
                    materials[j];

                if (material == null)
                {
                    continue;
                }

                if (
                    material.HasProperty(
                        "_BaseColor"
                    )
                )
                {
                    originalColors[i][j] =
                        material.GetColor(
                            "_BaseColor"
                        );
                }
                else if (
                    material.HasProperty(
                        "_Color"
                    )
                )
                {
                    originalColors[i][j] =
                        material.GetColor(
                            "_Color"
                        );
                }
                else
                {
                    originalColors[i][j] =
                        Color.white;
                }

                if (
                    material.HasProperty(
                        "_EmissionColor"
                    )
                )
                {
                    originalEmissionColors[i][j] =
                        material.GetColor(
                            "_EmissionColor"
                        );

                    originalEmissionEnabled[i][j] =
                        material.IsKeywordEnabled(
                            "_EMISSION"
                        );
                }
                else
                {
                    originalEmissionColors[i][j] =
                        Color.black;

                    originalEmissionEnabled[i][j] =
                        false;
                }
            }
        }
    }


    //=========================================================
    // IGNORE RENDERER
    //=========================================================

    private bool IsIgnoredRenderer(
        Renderer renderer
    )
    {
        if (renderer == null)
        {
            return true;
        }

        if (
            ignoredRendererNames == null ||
            ignoredRendererNames.Length == 0
        )
        {
            return false;
        }

        string objectName =
            renderer.gameObject.name.ToLower();

        foreach (
            string ignoredName
            in ignoredRendererNames
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    ignoredName
                )
            )
            {
                continue;
            }

            if (
                objectName.Contains(
                    ignoredName.ToLower()
                )
            )
            {
                return true;
            }
        }

        return false;
    }


    //=========================================================
    // UPGRADE VALUES
    //=========================================================

    /// <summary>
    /// Lấy thông số Photon hiện tại từ ItemUpgradeManager.
    ///
    /// ItemUpgradeData:
    ///     duration -> thời gian Photon
    ///     value1   -> speed multiplier
    ///     value2   -> hit force
    ///
    /// Nếu Upgrade chưa sẵn sàng thì giữ nguyên
    /// giá trị Inspector hiện tại.
    /// </summary>
    private void ApplyPhotonUpgradeValues()
{
    if (ItemUpgradeManager.Instance == null)
    {
        return;
    }

    ItemUpgradeLevel levelData =
        ItemUpgradeManager.Instance.GetCurrentLevelData(
            UpgradeItemType.Photon
        );

    if (levelData == null)
    {
        return;
    }

    //=====================================================
    // PHOTON UPGRADE DATA
    //
    // duration        -> Photon duration
    // speedMultiplier -> Photon speed multiplier
    //
    // Hit Force       -> FIXED Inspector value
    // Upward Force    -> FIXED Inspector value
    // Forward Force  -> FIXED Inspector value
    // Side Random     -> FIXED Inspector value
    // Torque Force    -> FIXED Inspector value
    // Hit Cooldown    -> FIXED Inspector value
    //=====================================================

    photonDuration =
        Mathf.Max(
            0f,
            levelData.duration
        );

    photonSpeedMultiplier =
        Mathf.Max(
            0f,
            levelData.speedMultiplier
        );

    Debug.Log(
        "[PhotonController] Upgrade Applied | " +
        "Level=" +
        levelData.level +
        " | Duration=" +
        photonDuration +
        " | SpeedMultiplier=" +
        photonSpeedMultiplier +
        " | HitForce=" +
        photonHitForce,
        this
    );
}


    //=========================================================
    // ACTIVATE
    //=========================================================

    public void ActivatePhoton()
    {
        //=====================================================
        // APPLY CURRENT UPGRADE
        //=====================================================

        ApplyPhotonUpgradeValues();

        //=====================================================
        // EXISTING ACTIVATION
        //=====================================================

        bool wasAlreadyActive =
            isPhotonActive;

        isPhotonActive =
            true;

        photonTimer =
            photonDuration;

        ApplyPhotonVisual();

        SpawnSpeedEffect();

        PlayPhotonSpeedLines();

        if (!wasAlreadyActive)
        {
            PlayPhotonAudio(
                photonActivateClip,
                photonActivateVolume
            );
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdatePhotonTimerUI(
                photonTimer
            );
        }
    }


    //=========================================================
    // DEACTIVATE
    //=========================================================

    public void DeactivatePhoton()
    {
        if (!isPhotonActive)
        {
            return;
        }

        isPhotonActive =
            false;

        photonTimer =
            0f;

        RestorePhotonVisual();

        RemoveSpeedEffect();

        StopPhotonSpeedLines();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.HidePhotonStatusUI();
        }
    }


    //=========================================================
    // APPLY VISUAL
    //=========================================================

    private void ApplyPhotonVisual()
    {
        if (photonMaterials != null)
        {
            for (
                int i = 0;
                i < photonMaterials.Length;
                i++
            )
            {
                Material[] materials =
                    photonMaterials[i];

                if (materials == null)
                {
                    continue;
                }

                for (
                    int j = 0;
                    j < materials.Length;
                    j++
                )
                {
                    Material material =
                        materials[j];

                    if (material == null)
                    {
                        continue;
                    }

                    if (
                        material.HasProperty(
                            "_BaseColor"
                        )
                    )
                    {
                        material.SetColor(
                            "_BaseColor",
                            photonYellowColor
                        );
                    }

                    if (
                        material.HasProperty(
                            "_Color"
                        )
                    )
                    {
                        material.SetColor(
                            "_Color",
                            photonYellowColor
                        );
                    }

                    if (
                        material.HasProperty(
                            "_EmissionColor"
                        )
                    )
                    {
                        material.EnableKeyword(
                            "_EMISSION"
                        );

                        material.SetColor(
                            "_EmissionColor",
                            photonYellowColor * 3f
                        );
                    }
                }
            }
        }

        if (photonVisual != null)
        {
            photonVisual.SetActive(true);
        }
    }


    //=========================================================
    // FINAL FLASH
    //=========================================================

    private void UpdateFinalFlash()
    {
        bool showHighlight =
            Mathf.FloorToInt(
                photonTimer / 0.15f
            ) % 2 == 0;

        if (photonMaterials != null)
        {
            for (
                int i = 0;
                i < photonMaterials.Length;
                i++
            )
            {
                Material[] materials =
                    photonMaterials[i];

                if (materials == null)
                {
                    continue;
                }

                for (
                    int j = 0;
                    j < materials.Length;
                    j++
                )
                {
                    Material material =
                        materials[j];

                    if (material == null)
                    {
                        continue;
                    }

                    Color baseColor =
                        showHighlight
                            ? photonYellowColor
                            : originalColors[i][j];

                    if (
                        material.HasProperty(
                            "_BaseColor"
                        )
                    )
                    {
                        material.SetColor(
                            "_BaseColor",
                            baseColor
                        );
                    }

                    if (
                        material.HasProperty(
                            "_Color"
                        )
                    )
                    {
                        material.SetColor(
                            "_Color",
                            baseColor
                        );
                    }

                    if (
                        material.HasProperty(
                            "_EmissionColor"
                        )
                    )
                    {
                        material.SetColor(
                            "_EmissionColor",
                            showHighlight
                                ? photonYellowColor * 3f
                                : originalEmissionColors[i][j]
                        );
                    }
                }
            }
        }

        if (photonVisual != null)
        {
            photonVisual.SetActive(
                showHighlight
            );
        }
    }


    //=========================================================
    // RESTORE VISUAL
    //=========================================================

    private void RestorePhotonVisual()
    {
        if (photonMaterials != null)
        {
            for (
                int i = 0;
                i < photonMaterials.Length;
                i++
            )
            {
                Material[] materials =
                    photonMaterials[i];

                if (materials == null)
                {
                    continue;
                }

                for (
                    int j = 0;
                    j < materials.Length;
                    j++
                )
                {
                    Material material =
                        materials[j];

                    if (material == null)
                    {
                        continue;
                    }

                    if (
                        material.HasProperty(
                            "_BaseColor"
                        )
                    )
                    {
                        material.SetColor(
                            "_BaseColor",
                            originalColors[i][j]
                        );
                    }

                    if (
                        material.HasProperty(
                            "_Color"
                        )
                    )
                    {
                        material.SetColor(
                            "_Color",
                            originalColors[i][j]
                        );
                    }

                    if (
                        material.HasProperty(
                            "_EmissionColor"
                        )
                    )
                    {
                        material.SetColor(
                            "_EmissionColor",
                            originalEmissionColors[i][j]
                        );

                        if (
                            originalEmissionEnabled[i][j]
                        )
                        {
                            material.EnableKeyword(
                                "_EMISSION"
                            );
                        }
                        else
                        {
                            material.DisableKeyword(
                                "_EMISSION"
                            );
                        }
                    }
                }
            }
        }

        if (photonVisual != null)
        {
            photonVisual.SetActive(false);
        }
    }


    //=========================================================
    // CANVAS SPEED LINES
    //=========================================================

    private void PlayPhotonSpeedLines()
    {
        if (photonSpeedLinesCanvas == null)
        {
            Debug.LogWarning(
                "[PhotonController] " +
                "Photon Speed Lines Canvas chưa được gán.",
                this
            );

            return;
        }

        photonSpeedLinesCanvas.SetActive(true);

        if (photonSpeedLinesVideo == null)
        {
            Debug.LogWarning(
                "[PhotonController] " +
                "Photon Speed Lines Video chưa được gán.",
                this
            );

            return;
        }

        photonSpeedLinesVideo.Stop();

        photonSpeedLinesVideo.time = 0f;

        photonSpeedLinesVideo.Play();

        Debug.Log(
            "[PhotonController] " +
            "Photon Speed Lines ON",
            this
        );
    }


    //=========================================================
    // STOP CANVAS SPEED LINES
    //=========================================================

    private void StopPhotonSpeedLines()
    {
        if (photonSpeedLinesVideo != null)
        {
            photonSpeedLinesVideo.Stop();

            photonSpeedLinesVideo.time = 0f;
        }

        if (photonSpeedLinesCanvas != null)
        {
            photonSpeedLinesCanvas.SetActive(false);
        }

        Debug.Log(
            "[PhotonController] " +
            "Photon Speed Lines OFF",
            this
        );
    }


    //=========================================================
    // SPEED EFFECT - CAR SMOKE
    //=========================================================

    private void SpawnSpeedEffect()
    {
        if (photonSpeedEffectPrefab == null)
        {
            return;
        }

        Transform spawnPoint =
            photonEffectPoint != null
                ? photonEffectPoint
                : transform;

        if (currentSpeedEffect != null)
        {
            currentSpeedEffect.SetActive(true);

            PlayVideoEffects(
                currentSpeedEffect
            );

            PlayParticleEffects(
                currentSpeedEffect
            );

            return;
        }

        currentSpeedEffect =
            Instantiate(
                photonSpeedEffectPrefab,
                spawnPoint
            );

        if (currentSpeedEffect == null)
        {
            return;
        }

        currentSpeedEffect.transform.localPosition =
            Vector3.zero;

        currentSpeedEffect.transform.localRotation =
            Quaternion.identity;

        currentSpeedEffect.transform.localScale =
            Vector3.one;

        currentSpeedEffect.SetActive(true);

        PlayVideoEffects(
            currentSpeedEffect
        );

        PlayParticleEffects(
            currentSpeedEffect
        );

        Debug.Log(
            "[PhotonController] " +
            "Photon Car Smoke Effect ON | " +
            "Point=" +
            spawnPoint.name,
            this
        );
    }


    //=========================================================
    // PLAY VIDEO EFFECTS
    //=========================================================

    private void PlayVideoEffects(
        GameObject effect
    )
    {
        if (effect == null)
        {
            return;
        }

        VideoPlayer[] videoPlayers =
            effect.GetComponentsInChildren<VideoPlayer>(
                true
            );

        for (
            int i = 0;
            i < videoPlayers.Length;
            i++
        )
        {
            VideoPlayer videoPlayer =
                videoPlayers[i];

            if (videoPlayer == null)
            {
                continue;
            }

            videoPlayer.Stop();

            videoPlayer.time =
                0f;

            videoPlayer.Play();
        }
    }


    //=========================================================
    // PLAY PARTICLE EFFECTS
    //=========================================================

    private void PlayParticleEffects(
        GameObject effect
    )
    {
        if (effect == null)
        {
            return;
        }

        ParticleSystem[] particles =
            effect.GetComponentsInChildren<ParticleSystem>(
                true
            );

        for (
            int i = 0;
            i < particles.Length;
            i++
        )
        {
            if (particles[i] == null)
            {
                continue;
            }

            particles[i].Clear(true);

            particles[i].Play(true);
        }
    }


    //=========================================================
    // REMOVE SPEED EFFECT - CAR SMOKE
    //=========================================================

    private void RemoveSpeedEffect()
    {
        if (currentSpeedEffect == null)
        {
            return;
        }

        Destroy(
            currentSpeedEffect
        );

        currentSpeedEffect =
            null;
    }


    //=========================================================
    // BA GAC DETECTION
    //=========================================================

    private bool IsBaGacObject(
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
            string objectName =
                current.name.ToLowerInvariant();

            if (
                objectName.Contains("bagac") ||
                objectName.Contains("ba gac") ||
                objectName.Contains("ba_gac")
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
    // BA GAC RAMP DETECTION
    //=========================================================

    private bool IsBaGacRampCollider(
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
            string lower =
                current.name.ToLowerInvariant();

            if (
                lower == "bagacramp" ||
                lower == "bagac_ramp" ||
                lower == "bagacramptigger" ||
                lower == "bagacramptrigger" ||
                lower == "bagac_ramp_trigger" ||
                lower == "ramp" ||
                lower == "ramptrigger" ||
                lower == "ramp_trigger" ||
                lower == "tinramp" ||
                lower == "tinramptrigger" ||
                lower == "tin_ramp" ||
                lower == "tin_ramp_trigger" ||
                lower.Contains("ramp")
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
    // HIT BA GAC
    //=========================================================

    private void HitBaGacObject(
        GameObject hitObject
    )
    {
        if (hitObject == null)
        {
            return;
        }

        Transform baGacRoot =
            hitObject.transform;

        while (
            baGacRoot.parent != null &&
            IsBaGacObject(baGacRoot.parent.gameObject)
        )
        {
            baGacRoot =
                baGacRoot.parent;
        }

        GameObject baGacObject =
            baGacRoot.gameObject;

        if (baGacObject == null)
        {
            return;
        }

        int id =
            baGacObject.GetInstanceID();

        if (
            hitCooldowns.TryGetValue(
                id,
                out float lastHit
            )
        )
        {
            if (
                Time.time -
                lastHit <
                hitCooldown
            )
            {
                return;
            }
        }

        hitCooldowns[id] =
            Time.time;

        Rigidbody targetRb =
            hitObject.GetComponent<Rigidbody>();

        if (targetRb == null)
        {
            targetRb =
                hitObject.GetComponentInParent<Rigidbody>();
        }

        if (targetRb == null)
        {
            targetRb =
                hitObject.GetComponentInChildren<Rigidbody>();
        }

        if (targetRb == null)
        {
            Debug.LogWarning(
                "[PhotonController] Photon cham Ba Gac nhung khong tim thay Rigidbody: " +
                baGacObject.name,
                baGacObject
            );

            return;
        }

        Vector3 force =
            CalculatePhotonHitForce();

        targetRb.isKinematic =
            false;

        targetRb.useGravity =
            true;

        targetRb.constraints =
            RigidbodyConstraints.None;

        targetRb.linearVelocity =
            Vector3.zero;

        targetRb.angularVelocity =
            Vector3.zero;

        targetRb.AddForce(
            force,
            ForceMode.Impulse
        );

        targetRb.AddTorque(
            Random.insideUnitSphere *
            photonTorqueForce,
            ForceMode.Impulse
        );

        SpawnHitEffect(
            baGacObject
        );

        PlayPhotonAudio(
            photonHitClip,
            photonHitVolume
        );

        Debug.Log(
            "[PhotonController] Photon HIT BA GAC: " +
            baGacObject.name +
            " | Rigidbody=" +
            targetRb.name +
            " | Force=" +
            force
        );
    }


    //=========================================================
    // HIT TRAFFIC
    //=========================================================

    private void HitTrafficVehicle(
        TrafficVehicle traffic
    )
    {
        if (traffic == null)
        {
            return;
        }

        if (traffic.IsKnockedByPhoton)
        {
            return;
        }

        int id =
            traffic.gameObject.GetInstanceID();

        if (
            hitCooldowns.TryGetValue(
                id,
                out float lastHit
            )
        )
        {
            if (
                Time.time -
                lastHit <
                hitCooldown
            )
            {
                return;
            }
        }

        hitCooldowns[id] =
            Time.time;

        Vector3 force =
            CalculatePhotonHitForce();

        SpawnHitEffect(
            traffic.gameObject
        );

        PlayPhotonAudio(
            photonHitClip,
            photonHitVolume
        );

        traffic.ApplyPhotonKnockback(
            force
        );

        Debug.Log(
            "[PhotonController] Photon HIT: " +
            traffic.gameObject.name +
            " | Force = " +
            force
        );
    }


    //=========================================================
    // GENERIC OBJECT
    //=========================================================

    private void HitGenericObject(
        GameObject obj
    )
    {
        if (obj == null)
        {
            return;
        }

        SpawnHitEffect(
            obj
        );

        PlayPhotonAudio(
            photonHitClip,
            photonHitVolume
        );

        Rigidbody targetRb =
            obj.GetComponent<Rigidbody>();

        if (targetRb == null)
        {
            targetRb =
                obj.GetComponentInParent<Rigidbody>();
        }

        if (targetRb == null)
        {
            targetRb =
                obj.GetComponentInChildren<Rigidbody>();
        }

        Vector3 force =
            CalculatePhotonHitForce();

        if (targetRb != null)
        {
            targetRb.isKinematic =
                false;

            targetRb.useGravity =
                true;

            targetRb.constraints =
                RigidbodyConstraints.None;

            targetRb.linearVelocity =
                Vector3.zero;

            targetRb.angularVelocity =
                Vector3.zero;

            targetRb.AddForce(
                force,
                ForceMode.Impulse
            );

            targetRb.AddTorque(
                Random.insideUnitSphere *
                photonTorqueForce,
                ForceMode.Impulse
            );
        }
        else
        {
            Rigidbody dynamicRb =
                obj.AddComponent<Rigidbody>();

            dynamicRb.mass =
                1.5f;

            dynamicRb.useGravity =
                true;

            dynamicRb.linearVelocity =
                Vector3.zero;

            dynamicRb.angularVelocity =
                Vector3.zero;

            dynamicRb.AddForce(
                force,
                ForceMode.Impulse
            );

            dynamicRb.AddTorque(
                Random.insideUnitSphere *
                photonTorqueForce,
                ForceMode.Impulse
            );
        }

        Destroy(
            obj,
            3f
        );
    }


    //=========================================================
    // CALCULATE FORCE
    //=========================================================

    private Vector3 CalculatePhotonHitForce()
    {
        Vector3 forward =
            transform.forward;

        forward.y = 0f;

        if (
            forward.sqrMagnitude <
            0.001f
        )
        {
            forward =
                Vector3.forward;
        }

        forward.Normalize();

        Vector3 right =
            transform.right;

        right.y = 0f;

        if (
            right.sqrMagnitude <
            0.001f
        )
        {
            right =
                Vector3.right;
        }

        right.Normalize();

        float randomSide =
            Random.Range(
                -photonSideRandomForce,
                photonSideRandomForce
            );

        Vector3 direction =
            forward *
            photonForwardForce;

        direction +=
            right *
            randomSide;

        direction +=
            Vector3.up *
            photonUpwardForce;

        if (
            direction.sqrMagnitude <
            0.001f
        )
        {
            direction =
                Vector3.up;
        }

        direction.Normalize();

        return
            direction *
            photonHitForce;
    }


    //=========================================================
    // HIT EFFECT
    //=========================================================

    private void SpawnHitEffect(
        GameObject obj
    )
    {
        if (
            !enablePhotonHitEffect ||
            photonHitEffectPrefab == null ||
            obj == null
        )
        {
            return;
        }

        Vector3 spawnPos =
            (
                transform.position +
                obj.transform.position
            ) * 0.5f;

        GameObject effect =
            Instantiate(
                photonHitEffectPrefab,
                spawnPos,
                Quaternion.identity
            );

        if (
            photonHitEffectLifetime >
            0f
        )
        {
            Destroy(
                effect,
                photonHitEffectLifetime
            );
        }
    }


    //=========================================================
    // COOLDOWN CLEANUP
    //=========================================================

    private void CleanupHitCooldowns()
    {
        if (hitCooldowns.Count == 0)
        {
            return;
        }

        List<int> removeList =
            new List<int>();

        foreach (
            KeyValuePair<int, float> pair
            in hitCooldowns
        )
        {
            if (
                Time.time -
                pair.Value >
                hitCooldown * 4f
            )
            {
                removeList.Add(
                    pair.Key
                );
            }
        }

        foreach (int id in removeList)
        {
            hitCooldowns.Remove(id);
        }
    }


    //=========================================================
    // AUDIO SOURCE
    //=========================================================

    private void SetupAudioSource()
    {
        if (photonAudioSource != null)
        {
            ConfigurePhotonAudio(
                photonAudioSource
            );

            return;
        }

        GameObject audioObject =
            new GameObject(
                "PhotonAudioSource"
            );

        audioObject.transform.SetParent(
            transform
        );

        audioObject.transform.localPosition =
            Vector3.zero;

        photonAudioSource =
            audioObject.AddComponent<AudioSource>();

        ConfigurePhotonAudio(
            photonAudioSource
        );
    }


    //=========================================================
    // CONFIG AUDIO
    //=========================================================

    private void ConfigurePhotonAudio(
        AudioSource source
    )
    {
        if (source == null)
        {
            return;
        }

        source.spatialBlend =
            0f;

        source.dopplerLevel =
            0f;

        source.pitch =
            1f;

        source.playOnAwake =
            false;

        source.loop =
            false;
    }


    //=========================================================
    // PLAY AUDIO
    //=========================================================

    private void PlayPhotonAudio(
        AudioClip clip,
        float volume
    )
    {
        if (clip == null)
        {
            return;
        }

        if (photonAudioSource == null)
        {
            SetupAudioSource();
        }

        photonAudioSource.PlayOneShot(
            clip,
            volume
        );
    }


    //=========================================================
    // PUBLIC HIT OBSTACLE
    //=========================================================

    public void HitObstacle(
        GameObject obj
    )
    {
        if (!isPhotonActive)
        {
            return;
        }

        if (obj == null)
        {
            return;
        }

        Collider hitCollider =
            obj.GetComponent<Collider>();

        if (hitCollider == null)
        {
            hitCollider =
                obj.GetComponentInChildren<Collider>();
        }

        if (hitCollider == null)
        {
            return;
        }

        ProcessPhotonHit(
            hitCollider
        );
    }
}