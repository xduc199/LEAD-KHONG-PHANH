using UnityEngine;

public class MagnetController : MonoBehaviour
{
    //=============================================================
    // MAGNET SETTINGS
    //=============================================================

    [Header("Magnet Settings")]

    [SerializeField]
    private float duration = 8f;

    [SerializeField]
    private float magnetRadius = 8f;

    [SerializeField]
    private float pullSpeed = 30f;

    [SerializeField]
    private float collectDistance = 1f;

    [SerializeField]
    private bool resetDurationWhenCollected = true;


    //=============================================================
    // MAGNET EFFECT
    //=============================================================

    [Header("Magnet Effect")]

    [SerializeField]
    private GameObject magnetEffectPrefab;

    [SerializeField]
    private Vector3 effectLocalPosition = Vector3.zero;

    [SerializeField]
    private Vector3 effectLocalRotation = Vector3.zero;

    [SerializeField]
    private Vector3 effectLocalScale = Vector3.one;


    //=============================================================
    // AUDIO - PICKUP
    //=============================================================

    [Header("Audio - Pickup")]

    [SerializeField]
    private AudioClip pickupSound;

    [SerializeField]
    [Range(0f, 1f)]
    private float pickupVolume = 1f;


    //=============================================================
    // AUDIO - ACTIVE
    //=============================================================

    [Header("Audio - Active")]

    [SerializeField]
    private AudioClip activeSound;

    [SerializeField]
    [Range(0f, 1f)]
    private float activeVolume = 0.7f;


    //=============================================================
    // AUDIO SETTINGS
    //=============================================================

    [Header("Audio Settings")]

    [SerializeField]
    private bool use2DAudio = true;

    [SerializeField]
    [Range(0f, 1f)]
    private float spatialBlend = 0f;


    //=============================================================
    // DEBUG
    //=============================================================

    [Header("Debug")]

    [SerializeField]
    private bool showDebugLogs = false;


    //=============================================================
    // RUNTIME
    //=============================================================

    private GameObject activeEffect;

    private Transform playerTransform;

    private float remainingTime;

    private bool isActive;


    //=============================================================
    // RUNTIME - UPGRADE VALUES
    //=============================================================

    private float activeDuration;

    private float activeMagnetRadius;

    private float activePullSpeed;

    private float activeCollectDistance;


    //=============================================================
    // AWAKE
    //=============================================================

    private void Awake()
    {
        playerTransform = transform;

        activeDuration = duration;
        activeMagnetRadius = magnetRadius;
        activePullSpeed = pullSpeed;
        activeCollectDistance = collectDistance;
    }


    //=============================================================
    // UPDATE
    //=============================================================

    private void Update()
    {
        if (!isActive)
        {
            return;
        }

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            Deactivate();
            return;
        }

        AttractCoins();
    }


    //=============================================================
    // APPLY MAGNET UPGRADE
    //=============================================================

    /// <summary>
    /// Lấy toàn bộ thông số Magnet từ ItemUpgradeManager.
    ///
    /// Data Model mới:
    ///
    /// duration
    /// magnetRadius
    /// pullSpeed
    /// collectDistance
    ///
    /// Các giá trị trong ItemUpgradeData là giá trị gameplay
    /// trực tiếp, KHÔNG phải multiplier.
    /// </summary>
    private void ApplyMagnetUpgradeValues()
    {
        //=========================================================
        // FALLBACK VỀ INSPECTOR
        //=========================================================

        activeDuration = duration;
        activeMagnetRadius = magnetRadius;
        activePullSpeed = pullSpeed;
        activeCollectDistance = collectDistance;


        //=========================================================
        // KIỂM TRA UPGRADE MANAGER
        //=========================================================

        ItemUpgradeManager manager =
            ItemUpgradeManager.Instance;

        if (manager == null)
        {
            return;
        }


        //=========================================================
        // LẤY DATA LEVEL HIỆN TẠI
        //=========================================================

        ItemUpgradeLevel levelData =
            manager.GetCurrentLevelData(
                UpgradeItemType.Magnet
            );

        if (levelData == null)
        {
            return;
        }


        //=========================================================
        // APPLY DURATION
        //=========================================================

        activeDuration =
            Mathf.Max(
                0.1f,
                levelData.duration
            );


        //=========================================================
        // APPLY MAGNET RADIUS
        //=========================================================

        activeMagnetRadius =
            Mathf.Max(
                0.5f,
                levelData.magnetRadius
            );


        //=========================================================
        // APPLY PULL SPEED
        //=========================================================

        activePullSpeed =
            Mathf.Max(
                0f,
                levelData.pullSpeed
            );


        //=========================================================
        // APPLY COLLECT DISTANCE
        //=========================================================

        activeCollectDistance =
            Mathf.Max(
                0.1f,
                levelData.collectDistance
            );


        //=========================================================
        // DEBUG
        //=========================================================

        if (showDebugLogs)
        {
            Debug.Log(
                "[MagnetController] Upgrade Applied | " +
                "Level=" + levelData.level +
                " | Duration=" + activeDuration +
                " | Radius=" + activeMagnetRadius +
                " | PullSpeed=" + activePullSpeed +
                " | CollectDistance=" + activeCollectDistance,
                this
            );
        }
    }


    //=============================================================
    // ACTIVATE
    //=============================================================

    public void Activate()
    {
        ApplyMagnetUpgradeValues();

        Activate(
            activeDuration
        );
    }


    //=============================================================
    // ACTIVATE - CUSTOM DURATION
    //=============================================================

    public void Activate(float customDuration)
    {
        if (customDuration <= 0f)
        {
            customDuration =
                activeDuration > 0f
                    ? activeDuration
                    : duration;
        }


        //=========================================================
        // MAGNET ĐANG ACTIVE
        //=========================================================

        if (isActive)
        {
            if (resetDurationWhenCollected)
            {
                remainingTime = customDuration;
            }

            return;
        }


        //=========================================================
        // ACTIVATE
        //=========================================================

        isActive = true;

        remainingTime = customDuration;


        //=========================================================
        // CREATE EFFECT
        //=========================================================

        CreateEffect();


        //=========================================================
        // PICKUP SOUND
        //=========================================================

        Play2DSound(
            pickupSound,
            pickupVolume
        );


        //=========================================================
        // ACTIVE SOUND
        //=========================================================

        Play2DSound(
            activeSound,
            activeVolume
        );


        //=========================================================
        // DEBUG
        //=========================================================

        if (showDebugLogs)
        {
            Debug.Log(
                "[MagnetController] Magnet ACTIVATED | " +
                "Duration=" + customDuration +
                " | Radius=" + activeMagnetRadius +
                " | PullSpeed=" + activePullSpeed +
                " | CollectDistance=" + activeCollectDistance,
                this
            );
        }
    }


    //=============================================================
    // ATTRACT COINS
    //=============================================================

    private void AttractCoins()
    {
        Collider[] colliders =
            Physics.OverlapSphere(
                playerTransform.position,
                activeMagnetRadius,
                ~0,
                QueryTriggerInteraction.Collide
            );


        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];

            if (collider == null)
            {
                continue;
            }


            Coin coin =
                collider.GetComponent<Coin>();


            if (coin == null)
            {
                coin =
                    collider.GetComponentInParent<Coin>();
            }


            if (coin == null)
            {
                continue;
            }


            if (!coin.isActiveAndEnabled)
            {
                continue;
            }


            Transform coinTransform =
                coin.transform;


            float distance =
                Vector3.Distance(
                    coinTransform.position,
                    playerTransform.position
                );


            if (distance <= activeCollectDistance)
            {
                coin.CollectFromMagnet();
                continue;
            }


            coinTransform.position =
                Vector3.MoveTowards(
                    coinTransform.position,
                    playerTransform.position,
                    activePullSpeed * Time.deltaTime
                );
        }
    }


    //=============================================================
    // EFFECT
    //=============================================================

    private void CreateEffect()
    {
        if (magnetEffectPrefab == null)
        {
            return;
        }


        if (activeEffect != null)
        {
            Destroy(activeEffect);

            activeEffect = null;
        }


        activeEffect =
            Instantiate(
                magnetEffectPrefab
            );


        activeEffect.transform.SetParent(
            transform
        );


        activeEffect.transform.localPosition =
            effectLocalPosition;


        activeEffect.transform.localEulerAngles =
            effectLocalRotation;


        activeEffect.transform.localScale =
            effectLocalScale;
    }


    //=============================================================
    // DEACTIVATE
    //=============================================================

    private void Deactivate()
    {
        isActive = false;

        remainingTime = 0f;


        if (activeEffect != null)
        {
            Destroy(activeEffect);

            activeEffect = null;
        }


        if (showDebugLogs)
        {
            Debug.Log(
                "[MagnetController] Magnet expired.",
                this
            );
        }
    }


    //=============================================================
    // AUDIO
    //=============================================================

    private void Play2DSound(
        AudioClip clip,
        float volume
    )
    {
        if (clip == null)
        {
            return;
        }


        GameObject audioObject =
            new GameObject(
                "MagnetAudio"
            );


        AudioSource source =
            audioObject.AddComponent<AudioSource>();


        source.clip =
            clip;

        source.volume =
            volume;

        source.playOnAwake =
            false;

        source.loop =
            false;

        source.dopplerLevel =
            0f;

        source.pitch =
            1f;


        if (use2DAudio)
        {
            source.spatialBlend =
                0f;
        }
        else
        {
            source.spatialBlend =
                spatialBlend;
        }


        source.Play();


        Destroy(
            audioObject,
            clip.length + 0.1f
        );
    }


    //=============================================================
    // PUBLIC
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


    //=============================================================
    // GIZMOS
    //=============================================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(
            transform.position,
            magnetRadius
        );
    }


    //=============================================================
    // VALIDATE
    //=============================================================

    private void OnValidate()
    {
        if (duration < 0.1f)
        {
            duration = 0.1f;
        }


        if (magnetRadius < 0.5f)
        {
            magnetRadius = 0.5f;
        }


        if (pullSpeed < 1f)
        {
            pullSpeed = 1f;
        }


        if (collectDistance < 0.1f)
        {
            collectDistance = 0.1f;
        }


        pickupVolume =
            Mathf.Clamp01(
                pickupVolume
            );


        activeVolume =
            Mathf.Clamp01(
                activeVolume
            );


        spatialBlend =
            Mathf.Clamp01(
                spatialBlend
            );
    }
}