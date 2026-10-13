using UnityEngine;

public sealed class Coin : MonoBehaviour
{
    //=============================================================
    // COIN SETTINGS
    //=============================================================

    [Header("Coin Settings")]
    [SerializeField] private int coinValue = 1;
    [SerializeField] private float rotateSpeed = 120f;


    //=============================================================
    // COLLECTION
    //=============================================================

    [Header("Collection")]
    [SerializeField] private float collectRadius = 0.8f;
    [SerializeField] private bool destroyOnCollect = true;


    //=============================================================
    // AUDIO
    //=============================================================

    [Header("Audio - 2D")]
    [SerializeField] private AudioClip collectSound;

    [SerializeField, Range(0f, 1f)]
    private float collectVolume = 1f;


    //=============================================================
    // RUNTIME
    //=============================================================

    private SphereCollider coinCollider;

    private bool collected;


    //=============================================================
    // AWAKE
    //=============================================================

    private void Awake()
    {
        SetupCollider();

        ResetRuntimeState();
    }


    //=============================================================
    // ON ENABLE
    //=============================================================

    private void OnEnable()
    {
        ResetRuntimeState();
    }


    //=============================================================
    // UPDATE
    //=============================================================

    private void Update()
    {
        if (rotateSpeed <= 0f)
            return;


        transform.Rotate(
            0f,
            rotateSpeed * Time.deltaTime,
            0f,
            Space.World
        );
    }


    //=============================================================
    // RESET RUNTIME STATE
    //=============================================================

    private void ResetRuntimeState()
    {
        collected =
            false;


        if (coinCollider == null)
        {
            coinCollider =
                GetComponent<SphereCollider>();
        }


        if (coinCollider == null)
        {
            return;
        }


        coinCollider.isTrigger =
            true;


        coinCollider.radius =
            collectRadius;


        coinCollider.enabled =
            true;
    }


    //=============================================================
    // COLLIDER
    //=============================================================

    private void SetupCollider()
    {
        coinCollider =
            GetComponent<SphereCollider>();


        if (coinCollider == null)
        {
            coinCollider =
                gameObject.AddComponent<SphereCollider>();
        }


        coinCollider.isTrigger =
            true;


        coinCollider.radius =
            collectRadius;


        coinCollider.enabled =
            true;
    }


    //=============================================================
    // TRIGGER
    //=============================================================

    private void OnTriggerEnter(
        Collider other
    )
    {
        if (collected)
            return;


        if (!other.CompareTag("Player"))
            return;


        Collect();
    }


    //=============================================================
    // COLLECT FROM MAGNET
    //=============================================================

    public void CollectFromMagnet()
    {
        if (collected)
            return;


        Collect();
    }


    //=============================================================
    // COLLECT
    //=============================================================

    private void Collect()
    {
        if (collected)
            return;


        //=========================================================
        // MARK COLLECTED
        //=========================================================

        collected =
            true;


        //=========================================================
        // DISABLE COLLIDER
        //=========================================================

        if (coinCollider != null)
        {
            coinCollider.enabled =
                false;
        }


        //=========================================================
        // ADD COIN
        //=========================================================

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddCoin(
                coinValue
            );
        }
        else
        {
            Debug.LogWarning(
                "[Coin] Không tìm thấy GameManager.Instance."
            );
        }


        //=========================================================
        // SOUND
        //=========================================================

        PlayCollectSound();


        //=========================================================
        // DESTROY / DISABLE
        //=========================================================

        if (destroyOnCollect)
        {
            Destroy(
                gameObject
            );
        }
        else
        {
            gameObject.SetActive(
                false
            );
        }
    }


    //=============================================================
    // AUDIO
    //=============================================================

    private void PlayCollectSound()
    {
        if (collectSound == null)
            return;


        GameObject audioObject =
            new GameObject(
                "CoinCollectAudio"
            );


        AudioSource audioSource =
            audioObject.AddComponent<AudioSource>();


        audioSource.clip =
            collectSound;


        audioSource.volume =
            collectVolume;


        audioSource.spatialBlend =
            0f;


        audioSource.playOnAwake =
            false;


        audioSource.loop =
            false;


        audioSource.dopplerLevel =
            0f;


        audioSource.pitch =
            1f;


        audioSource.minDistance =
            1f;


        audioSource.maxDistance =
            500f;


        audioSource.panStereo =
            0f;


        audioSource.Play();


        Destroy(
            audioObject,
            collectSound.length + 0.1f
        );
    }


    //=============================================================
    // PUBLIC
    //=============================================================

    public int GetCoinValue()
    {
        return coinValue;
    }


    public bool IsCollected()
    {
        return collected;
    }


    //=============================================================
    // VALIDATE
    //=============================================================

    private void OnValidate()
    {
        if (coinValue < 1)
            coinValue = 1;


        if (rotateSpeed < 0f)
            rotateSpeed = 0f;


        if (collectRadius < 0.1f)
            collectRadius = 0.1f;


        collectVolume =
            Mathf.Clamp01(
                collectVolume
            );
    }
}