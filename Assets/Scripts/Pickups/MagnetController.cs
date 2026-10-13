using UnityEngine;

public sealed class MagnetController : MonoBehaviour
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
    // DETECTION SETTINGS
    //=============================================================

    [Header("Detection")]

    [SerializeField]
    private LayerMask coinLayers = ~0;

    [SerializeField]
    private int maxDetectedColliders = 128;


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
    // NON-ALLOC DETECTION BUFFER
    //=============================================================

    private Collider[] detectionBuffer;


    //=============================================================
    // AWAKE
    //=============================================================

    private void Awake()
    {
        playerTransform =
            transform;


        activeDuration =
            duration;

        activeMagnetRadius =
            magnetRadius;

        activePullSpeed =
            pullSpeed;

        activeCollectDistance =
            collectDistance;


        int bufferSize =
            Mathf.Max(
                16,
                maxDetectedColliders
            );


        detectionBuffer =
            new Collider[bufferSize];
    }


    //=============================================================
    // UPDATE
    //=============================================================

    private void Update()
    {
        if (!isActive)
            return;


        //=========================================================
        // TIMER
        //=========================================================

        remainingTime -=
            Time.deltaTime;


        if (remainingTime <= 0f)
        {
            Deactivate();
            return;
        }


        //=========================================================
        // ATTRACT COINS
        //=========================================================

        AttractCoins();
    }


    //=============================================================
    // APPLY MAGNET UPGRADE VALUES
    //=============================================================

    private void ApplyMagnetUpgradeValues()
    {
        //=========================================================
        // FALLBACK
        //=========================================================

        activeDuration =
            duration;

        activeMagnetRadius =
            magnetRadius;

        activePullSpeed =
            pullSpeed;

        activeCollectDistance =
            collectDistance;


        //=========================================================
        // MANAGER
        //=========================================================

        ItemUpgradeManager manager =
            ItemUpgradeManager.Instance;


        if (manager == null)
            return;


        //=========================================================
        // CURRENT LEVEL
        //=========================================================

        ItemUpgradeLevel levelData =
            manager.GetCurrentLevelData(
                UpgradeItemType.Magnet
            );


        if (levelData == null)
            return;


        //=========================================================
        // DURATION
        //=========================================================

        activeDuration =
            Mathf.Max(
                0.1f,
                levelData.duration
            );


        //=========================================================
        // RADIUS
        //=========================================================

        activeMagnetRadius =
            Mathf.Max(
                0.5f,
                levelData.magnetRadius
            );


        //=========================================================
        // PULL SPEED
        //=========================================================

        activePullSpeed =
            Mathf.Max(
                0f,
                levelData.pullSpeed
            );


        //=========================================================
        // COLLECT DISTANCE
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
    // ACTIVATE CUSTOM DURATION
    //=============================================================

    public void Activate(
        float customDuration
    )
    {
        if (customDuration <= 0f)
        {
            customDuration =
                activeDuration > 0f
                    ? activeDuration
                    : duration;
        }


        //=========================================================
        // ALREADY ACTIVE
        //=========================================================

        if (isActive)
        {
            if (resetDurationWhenCollected)
            {
                remainingTime =
                    customDuration;
            }

            return;
        }


        //=========================================================
        // ACTIVE
        //=========================================================

        isActive =
            true;

        remainingTime =
            customDuration;


        //=========================================================
        // EFFECT
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
        if (playerTransform == null)
            return;


        if (detectionBuffer == null)
            return;


        //=========================================================
        // DETECT COLLIDERS WITHOUT ALLOCATION
        //=========================================================

        int detectedCount =
            Physics.OverlapSphereNonAlloc(
                playerTransform.position,
                activeMagnetRadius,
                detectionBuffer,
                coinLayers,
                QueryTriggerInteraction.Collide
            );


        if (detectedCount <= 0)
            return;


        //=========================================================
        // PROCESS COLLIDERS
        //=========================================================

        for (int i = 0; i < detectedCount; i++)
        {
            Collider collider =
                detectionBuffer[i];


            if (collider == null)
                continue;


            //=====================================================
            // FIND COIN
            //=====================================================

            Coin coin =
                collider.GetComponent<Coin>();


            if (coin == null)
            {
                coin =
                    collider.GetComponentInParent<Coin>();
            }


            if (coin == null)
                continue;


            //=====================================================
            // COIN ACTIVE
            //=====================================================

            if (!coin.isActiveAndEnabled)
                continue;


            //=====================================================
            // COIN POSITION
            //=====================================================

            Transform coinTransform =
                coin.transform;


            if (coinTransform == null)
                continue;


            //=====================================================
            // DISTANCE
            //=====================================================

            Vector3 delta =
                coinTransform.position -
                playerTransform.position;


            float sqrDistance =
                delta.sqrMagnitude;


            float collectDistanceSqr =
                activeCollectDistance *
                activeCollectDistance;


            //=====================================================
            // COLLECT
            //=====================================================

            if (sqrDistance <= collectDistanceSqr)
            {
                coin.CollectFromMagnet();
                continue;
            }


            //=====================================================
            // PULL
            //=====================================================

            float step =
                activePullSpeed *
                Time.deltaTime;


            if (step <= 0f)
                continue;


            coinTransform.position =
                Vector3.MoveTowards(
                    coinTransform.position,
                    playerTransform.position,
                    step
                );
        }


        //=========================================================
        // CLEAR REFERENCES
        //=========================================================

        for (int i = 0; i < detectedCount; i++)
        {
            detectionBuffer[i] =
                null;
        }
    }


    //=============================================================
    // EFFECT
    //=============================================================

    private void CreateEffect()
    {
        if (magnetEffectPrefab == null)
            return;


        if (activeEffect != null)
        {
            Destroy(
                activeEffect
            );

            activeEffect =
                null;
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
        isActive =
            false;

        remainingTime =
            0f;


        if (activeEffect != null)
        {
            Destroy(
                activeEffect
            );

            activeEffect =
                null;
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
            return;


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
            duration = 0.1f;


        if (magnetRadius < 0.5f)
            magnetRadius = 0.5f;


        if (pullSpeed < 1f)
            pullSpeed = 1f;


        if (collectDistance < 0.1f)
            collectDistance = 0.1f;


        if (maxDetectedColliders < 16)
            maxDetectedColliders = 16;


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