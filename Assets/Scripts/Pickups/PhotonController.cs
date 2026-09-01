using System.Collections.Generic;
using UnityEngine;

public class PhotonController : MonoBehaviour
{
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

    [Tooltip("Tổng lực hất xe.")]
    [SerializeField]
    private float photonHitForce = 22f;

    [Tooltip("Lực bay lên.")]
    [SerializeField]
    private float photonUpwardForce = 2.2f;

    [Tooltip("Lực đẩy theo hướng Photon.")]
    [SerializeField]
    private float photonForwardForce = 1.2f;

    [Tooltip("Lực lệch trái/phải.")]
    [SerializeField]
    private float photonSideRandomForce = 0.35f;

    [Tooltip("Lực xoay.")]
    [SerializeField]
    private float photonTorqueForce = 18f;


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
    // SPEED EFFECT
    //=========================================================

    [Header("Photon Speed Effect")]

    [SerializeField]
    private GameObject photonSpeedEffectPrefab;

    [SerializeField]
    private Transform photonEffectPoint;


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

        GameObject hitObject =
            collision.gameObject;

        ProcessPhotonHit(
            hitObject
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
            other.gameObject
        );
    }


    //=========================================================
    // PROCESS HIT
    //=========================================================

   private void ProcessPhotonHit(
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

    //=====================================================
    // CHỈ XỬ LÝ TRAFFIC
    //=====================================================

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
                else
                {
                    originalColors[i][j] =
                        material.color;
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
    // ACTIVATE
    //=========================================================

    public void ActivatePhoton()
    {
        bool wasAlreadyActive =
            isPhotonActive;

        isPhotonActive =
            true;

        photonTimer =
            photonDuration;

        ApplyPhotonVisual();

        SpawnSpeedEffect();

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
    // SPEED EFFECT
    //=========================================================

    private void SpawnSpeedEffect()
    {
        if (photonSpeedEffectPrefab == null)
        {
            return;
        }

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

    //=====================================================
    // ĐÃ BỊ PHOTON HẤT
    //=====================================================

    if (traffic.IsKnockedByPhoton)
    {
        return;
    }

    //=====================================================
    // COOLDOWN
    //=====================================================

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

    //=====================================================
    // FORCE
    //=====================================================

    Vector3 force =
        CalculatePhotonHitForce();

    //=====================================================
    // VFX
    //=====================================================

    SpawnHitEffect(
        traffic.gameObject
    );

    //=====================================================
    // AUDIO
    //=====================================================

    PlayPhotonAudio(
        photonHitClip,
        photonHitVolume
    );

    //=====================================================
    // APPLY
    //=====================================================

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


        //=====================================================
        // FIND RIGIDBODY
        //=====================================================

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


        //=====================================================
        // FORCE
        //=====================================================

        Vector3 force =
            CalculatePhotonHitForce();


        //=====================================================
        // APPLY
        //=====================================================

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
        //=====================================================
        // HƯỚNG PHOTON
        //=====================================================

        Vector3 forward =
            transform.forward;

        forward.y =
            0f;

        if (
            forward.sqrMagnitude <
            0.001f
        )
        {
            forward =
                Vector3.forward;
        }

        forward.Normalize();


        //=====================================================
        // SIDE
        //=====================================================

        Vector3 right =
            transform.right;

        right.y =
            0f;

        if (
            right.sqrMagnitude <
            0.001f
        )
        {
            right =
                Vector3.right;
        }

        right.Normalize();


        //=====================================================
        // RANDOM SIDE
        //=====================================================

        float randomSide =
            Random.Range(
                -photonSideRandomForce,
                photonSideRandomForce
            );


        //=====================================================
        // FORCE
        //=====================================================

        Vector3 direction =
            forward *
            photonForwardForce;

        direction +=
            right *
            randomSide;

        direction +=
            Vector3.up *
            photonUpwardForce;


        //=====================================================
        // NORMALIZE
        //=====================================================

        if (
            direction.sqrMagnitude <
            0.001f
        )
        {
            direction =
                Vector3.up;
        }

        direction.Normalize();


        //=====================================================
        // FINAL
        //=====================================================

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

public void HitObstacle(GameObject obj)
{
    if (!isPhotonActive)
    {
        return;
    }

    if (obj == null)
    {
        return;
    }

    ProcessPhotonHit(obj);
}
}