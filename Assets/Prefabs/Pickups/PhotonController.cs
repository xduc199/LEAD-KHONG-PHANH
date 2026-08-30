using System.Collections.Generic;
using UnityEngine;

public class PhotonController : MonoBehaviour
{
    //=========================================================
    // PHOTON BURST
    //=========================================================

    [Header("Photon Burst Settings")]
    [SerializeField] private float photonDuration = 6.0f;

    [SerializeField] private float photonSpeedMultiplier = 1.6f;


    //=========================================================
    // CAR RENDERER
    //=========================================================

    [Header("Photon Car Renderers")]
    [Tooltip("Tự động tìm MeshRenderer / SkinnedMeshRenderer trong Player.")]
    [SerializeField] private bool autoFindRenderers = true;

    [Tooltip("Nếu tắt Auto Find, có thể tự kéo Renderer vào đây.")]
    [SerializeField] private Renderer[] carBodyRenderers;

    [Tooltip("Nếu ON, tất cả Renderer của thân xe được tìm tự động sẽ bị ảnh hưởng.")]
    [SerializeField] private bool affectAllRenderers = true;

    [Tooltip("Tên object chứa các từ này sẽ bị bỏ qua.")]
    [SerializeField] private string[] ignoredRendererNames;


    //=========================================================
    // PHOTON VISUAL
    //=========================================================

    [Header("Photon Visual")]
    [SerializeField] private Color photonYellowColor = Color.yellow;

    [Tooltip("GameObject VFX bao quanh xe khi Photon hoạt động.")]
    [SerializeField] private GameObject photonVisual;


    //=========================================================
    // SPEED EFFECT
    //=========================================================

    [Header("Photon Speed Effect")]
    [Tooltip("Prefab hiệu ứng tăng tốc phía sau xe.")]
    [SerializeField] private GameObject photonSpeedEffectPrefab;

    [Tooltip("Điểm gắn Speed Effect.")]
    [SerializeField] private Transform photonEffectPoint;


    //=========================================================
    // HIT EFFECT
    //=========================================================

    [Header("Photon Hit Effect")]
    [Tooltip("Bật/tắt hiệu ứng Photon khi húc xe.")]
    [SerializeField] private bool enablePhotonHitEffect = true;

    [Tooltip("Prefab hiệu ứng khi Photon húc xe.")]
    [SerializeField] private GameObject photonHitEffectPrefab;

    [Tooltip("Thời gian tồn tại Hit Effect.")]
    [SerializeField] private float photonHitEffectLifetime = 1.2f;


    //=========================================================
    // AUDIO
    //=========================================================

    [Header("Photon Audio")]
    [SerializeField] private AudioClip photonActivateClip;

    [SerializeField] private AudioClip photonHitClip;

    [Range(0f, 1f)]
    [SerializeField] private float photonActivateVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float photonHitVolume = 1f;

    [Tooltip("AudioSource riêng cho Photon. Để trống sẽ tự tạo.")]
    [SerializeField] private AudioSource photonAudioSource;


    //=========================================================
    // STATE
    //=========================================================

    private bool isPhotonActive = false;

    private float photonTimer = 0f;

    private GameObject currentSpeedEffect;


    //=========================================================
    // MATERIAL CACHE
    //=========================================================

    private Material[][] photonMaterials;

    private Color[][] originalColors;

    private Color[][] originalEmissionColors;

    private bool[][] originalEmissionEnabled;


    //=========================================================
    // PUBLIC API
    //=========================================================

    public bool IsPhotonActive
    {
        get { return isPhotonActive; }
    }

    public float PhotonTimeRemaining
    {
        get { return photonTimer; }
    }

    public float PhotonSpeedMultiplier
    {
        get { return photonSpeedMultiplier; }
    }


    //=========================================================
    // AWAKE
    //=========================================================

    private void Awake()
    {
        SetupCarRenderers();

        SetupAudioSource();

        if (photonVisual != null)
        {
            photonVisual.SetActive(false);
        }
    }


    //=========================================================
    // UPDATE
    //=========================================================

    private void Update()
    {
        if (!isPhotonActive)
            return;

        photonTimer -= Time.deltaTime;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdatePhotonTimerUI(
                photonTimer
            );
        }

        //=====================================================
        // 2 GIÂY CUỐI
        //=====================================================

        if (photonTimer <= 2f)
        {
            UpdateFinalFlash();
        }

        //=====================================================
        // HẾT PHOTON
        //=====================================================

        if (photonTimer <= 0f)
        {
            DeactivatePhoton();
        }
    }


    //=========================================================
    // SETUP RENDERERS
    //=========================================================

    private void SetupCarRenderers()
    {
        if (autoFindRenderers)
        {
            Renderer[] allRenderers =
                GetComponentsInChildren<Renderer>(true);

            List<Renderer> validRenderers =
                new List<Renderer>();

            foreach (Renderer renderer in allRenderers)
            {
                if (renderer == null)
                    continue;

                //=================================================
                // CHỈ NHẬN MESH RENDERER
                // KHÔNG NHẬN PARTICLE SYSTEM RENDERER
                //=================================================

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

                validRenderers.Add(renderer);
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
                "[PhotonController] Không tìm thấy Renderer của Player."
            );

            return;
        }

        photonMaterials =
            new Material[carBodyRenderers.Length][];

        originalColors =
            new Color[carBodyRenderers.Length][];

        originalEmissionColors =
            new Color[carBodyRenderers.Length][];

        originalEmissionEnabled =
            new bool[carBodyRenderers.Length][];

        //=====================================================
        // CACHE TẤT CẢ MATERIAL SLOT
        //=====================================================

        for (
            int i = 0;
            i < carBodyRenderers.Length;
            i++
        )
        {
            Renderer renderer =
                carBodyRenderers[i];

            if (renderer == null)
                continue;

            Material[] materials =
                renderer.materials;

            photonMaterials[i] =
                materials;

            originalColors[i] =
                new Color[materials.Length];

            originalEmissionColors[i] =
                new Color[materials.Length];

            originalEmissionEnabled[i] =
                new bool[materials.Length];

            for (
                int j = 0;
                j < materials.Length;
                j++
            )
            {
                Material material =
                    materials[j];

                if (material == null)
                    continue;

                //==============================
                // COLOR
                //==============================

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
                else
                {
                    originalColors[i][j] =
                        material.color;
                }

                //==============================
                // EMISSION
                //==============================

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
            return true;

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
    // ACTIVATE PHOTON
    //=========================================================

    public void ActivatePhoton()
    {
        bool wasAlreadyActive =
            isPhotonActive;

        isPhotonActive = true;

        // Refresh thời gian
        photonTimer =
            photonDuration;

        ApplyPhotonVisual();

        // Không spawn effect trùng
        SpawnSpeedEffect();

        // Chỉ phát sound khi vừa kích hoạt
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
            return;

        isPhotonActive = false;

        photonTimer = 0f;

        RestorePhotonVisual();

        RemoveSpeedEffect();

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
                    continue;

                for (
                    int j = 0;
                    j < materials.Length;
                    j++
                )
                {
                    Material material =
                        materials[j];

                    if (material == null)
                        continue;

                    //==============================
                    // BASE COLOR
                    //==============================

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

                    //==============================
                    // EMISSION
                    //==============================

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
                    continue;

                for (
                    int j = 0;
                    j < materials.Length;
                    j++
                )
                {
                    Material material =
                        materials[j];

                    if (material == null)
                        continue;

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
                    continue;

                for (
                    int j = 0;
                    j < materials.Length;
                    j++
                )
                {
                    Material material =
                        materials[j];

                    if (material == null)
                        continue;

                    //==============================
                    // RESTORE COLOR
                    //==============================

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

                    //==============================
                    // RESTORE EMISSION
                    //==============================

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
    // SPEED EFFECT
    //=========================================================

    private void SpawnSpeedEffect()
    {
        if (
            photonSpeedEffectPrefab == null
        )
        {
            return;
        }

        // Không tạo duplicate
        if (currentSpeedEffect != null)
        {
            currentSpeedEffect.SetActive(true);
            return;
        }

        Transform spawnPoint =
            photonEffectPoint != null
                ? photonEffectPoint
                : transform;

        currentSpeedEffect =
            Instantiate(
                photonSpeedEffectPrefab,
                spawnPoint.position,
                spawnPoint.rotation
            );

        currentSpeedEffect.transform.SetParent(
            spawnPoint,
            true
        );

        currentSpeedEffect.SetActive(true);
    }


    //=========================================================
    // REMOVE SPEED EFFECT
    //=========================================================

    private void RemoveSpeedEffect()
    {
        if (
            currentSpeedEffect == null
        )
        {
            return;
        }

        Destroy(
            currentSpeedEffect
        );

        currentSpeedEffect = null;
    }


    //=========================================================
    // HIT OBSTACLE
    //=========================================================

    public void HitObstacle(
        GameObject obj
    )
    {
        if (!isPhotonActive)
            return;

        if (obj == null)
            return;

        //=====================================================
        // HIT VFX
        //=====================================================

        if (
            enablePhotonHitEffect &&
            photonHitEffectPrefab != null
        )
        {
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
                photonHitEffectLifetime > 0f
            )
            {
                Destroy(
                    effect,
                    photonHitEffectLifetime
                );
            }
        }

        //=====================================================
        // HIT AUDIO
        //=====================================================

        PlayPhotonAudio(
            photonHitClip,
            photonHitVolume
        );

        //=====================================================
        // FIND RIGIDBODY
        //=====================================================

        Rigidbody carRb =
            obj.GetComponent<Rigidbody>();

        if (carRb == null)
        {
            carRb =
                obj.GetComponentInParent<Rigidbody>();
        }

        //=====================================================
        // PUSH
        //=====================================================

        if (carRb != null)
        {
            carRb.isKinematic = false;
            carRb.useGravity = true;

            Vector3 pushDir =
                (
                    obj.transform.position -
                    transform.position
                ).normalized;

            pushDir +=
                new Vector3(
                    Random.Range(-1f, 1f),
                    1.8f,
                    1.5f
                );

            carRb.AddForce(
                pushDir * 18f,
                ForceMode.Impulse
            );

            carRb.AddTorque(
                Random.insideUnitSphere * 15f,
                ForceMode.Impulse
            );
        }
        else
        {
            Rigidbody dynamicRb =
                obj.AddComponent<Rigidbody>();

            dynamicRb.mass = 1.5f;

            Vector3 pushDir =
                (
                    obj.transform.position -
                    transform.position
                ).normalized;

            pushDir +=
                new Vector3(
                    0f,
                    1.8f,
                    1.5f
                );

            dynamicRb.AddForce(
                pushDir * 18f,
                ForceMode.Impulse
            );

            dynamicRb.AddTorque(
                new Vector3(
                    10f,
                    10f,
                    10f
                ),
                ForceMode.Impulse
            );
        }

        Destroy(
            obj,
            3f
        );
    }


    //=========================================================
    // AUDIO SOURCE
    //=========================================================

    private void SetupAudioSource()
    {
        // Nếu người dùng đã gán AudioSource riêng
        if (photonAudioSource != null)
        {
            ConfigurePhotonAudio(
                photonAudioSource
            );

            return;
        }

        //=====================================================
        // LUÔN TẠO AUDIO SOURCE RIÊNG
        // Không lấy AudioSource của Player/Honk
        //=====================================================

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
            return;

        // 2D
        source.spatialBlend = 0f;

        // Không Doppler
        source.dopplerLevel = 0f;

        // Pitch ổn định
        source.pitch = 1f;

        source.playOnAwake = false;
        source.loop = false;
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
            return;

        if (
            photonAudioSource == null
        )
        {
            SetupAudioSource();
        }

        photonAudioSource.PlayOneShot(
            clip,
            volume
        );
    }
}