using UnityEngine;

public class MagnetController : MonoBehaviour
{
    [Header("Magnet Settings")]
    [SerializeField] private float duration = 8f;

    [SerializeField] private float magnetRadius = 8f;

    [SerializeField] private float pullSpeed = 30f;

    [SerializeField] private float collectDistance = 1f;

    [SerializeField] private bool resetDurationWhenCollected = true;

    [Header("Magnet Effect")]
    [SerializeField] private GameObject magnetEffectPrefab;

    [SerializeField] private Vector3 effectLocalPosition =
        Vector3.zero;

    [SerializeField] private Vector3 effectLocalRotation =
        Vector3.zero;

    [SerializeField] private Vector3 effectLocalScale =
        Vector3.one;

    [Header("Audio - Pickup")]
    [SerializeField] private AudioClip pickupSound;

    [SerializeField]
    [Range(0f, 1f)]
    private float pickupVolume = 1f;

    [Header("Audio - Active")]
    [SerializeField] private AudioClip activeSound;

    [SerializeField]
    [Range(0f, 1f)]
    private float activeVolume = 0.7f;

    [Header("Audio Settings")]
    [SerializeField] private bool use2DAudio = true;

    [SerializeField]
    [Range(0f, 1f)]
    private float spatialBlend = 0f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private GameObject activeEffect;

    private Transform playerTransform;

    private float remainingTime;

    private bool isActive;

    private void Awake()
    {
        playerTransform = transform;
    }

    private void Update()
    {
        if (!isActive)
            return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            Deactivate();
            return;
        }

        AttractCoins();
    }

    //=============================================================
    // ACTIVATE
    //=============================================================

    public void Activate()
    {
        Activate(duration);
    }

    public void Activate(float customDuration)
    {
        if (customDuration <= 0f)
            customDuration = duration;

        // Nếu Magnet đang hoạt động
        if (isActive)
        {
            if (resetDurationWhenCollected)
            {
                remainingTime = customDuration;
            }

            return;
        }

        isActive = true;

        remainingTime = customDuration;

        CreateEffect();

        Play2DSound(
            pickupSound,
            pickupVolume
        );

        Play2DSound(
            activeSound,
            activeVolume
        );

        if (showDebugLogs)
        {
            Debug.Log(
                "[MagnetController] Magnet ACTIVATED."
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
                magnetRadius,
                ~0,
                QueryTriggerInteraction.Collide
            );

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] == null)
                continue;

            Coin coin =
                colliders[i].GetComponent<Coin>();

            if (coin == null)
            {
                coin =
                    colliders[i].GetComponentInParent<Coin>();
            }

            if (coin == null)
                continue;

            if (!coin.isActiveAndEnabled)
                continue;

            Transform coinTransform =
                coin.transform;

            float distance =
                Vector3.Distance(
                    coinTransform.position,
                    playerTransform.position
                );

            if (distance <= collectDistance)
            {
                coin.CollectFromMagnet();
                continue;
            }

            coinTransform.position =
                Vector3.MoveTowards(
                    coinTransform.position,
                    playerTransform.position,
                    pullSpeed * Time.deltaTime
                );
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
                "[MagnetController] Magnet expired."
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

        source.clip = clip;

        source.volume = volume;

        source.playOnAwake = false;

        source.loop = false;

        source.dopplerLevel = 0f;

        source.pitch = 1f;

        if (use2DAudio)
        {
            source.spatialBlend = 0f;
        }
        else
        {
            source.spatialBlend = spatialBlend;
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

        pickupVolume = Mathf.Clamp01(
            pickupVolume
        );

        activeVolume = Mathf.Clamp01(
            activeVolume
        );

        spatialBlend = Mathf.Clamp01(
            spatialBlend
        );
    }
}