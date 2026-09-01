using UnityEngine;

public class Coin : MonoBehaviour
{
    [Header("Coin Settings")]
    [SerializeField] private int coinValue = 1;
    [SerializeField] private float rotateSpeed = 120f;

    [Header("Collection")]
    [SerializeField] private float collectRadius = 0.8f;
    [SerializeField] private bool destroyOnCollect = true;

    [Header("Audio - 2D")]
    [SerializeField] private AudioClip collectSound;

    [SerializeField, Range(0f, 1f)]
    private float collectVolume = 1f;

    private SphereCollider coinCollider;
    private bool collected;

    //=============================================================
    // AWAKE
    //=============================================================

    private void Awake()
    {
        SetupCollider();
    }

    //=============================================================
    // UPDATE
    //=============================================================

    private void Update()
    {
        transform.Rotate(
            0f,
            rotateSpeed * Time.deltaTime,
            0f,
            Space.World
        );
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

        coinCollider.isTrigger = true;

        coinCollider.radius =
            collectRadius;
    }

    //=============================================================
    // TRIGGER
    //=============================================================

    private void OnTriggerEnter(Collider other)
    {
        if (collected)
            return;

        if (!other.CompareTag("Player"))
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

        collected = true;

        //=========================================================
        // DISABLE COLLIDER
        //=========================================================

        if (coinCollider != null)
        {
            coinCollider.enabled = false;
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
        // PLAY 2D SOUND
        //=========================================================

        PlayCollectSound();

        //=========================================================
        // DESTROY / DISABLE
        //=========================================================

        if (destroyOnCollect)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
        public void CollectFromMagnet()
{
    if (collected)
        return;

    Collect();
}

    //=============================================================
    // PLAY 2D COLLECT SOUND
    //=============================================================

    private void PlayCollectSound()
    {
        if (collectSound == null)
            return;

        // Tạo AudioSource tạm thời để phát âm thanh.
        GameObject audioObject =
            new GameObject(
                "CoinCollectAudio"
            );

        AudioSource audioSource =
            audioObject.AddComponent<AudioSource>();

        //=========================================================
        // AUDIO SETTINGS
        //=========================================================

        audioSource.clip =
            collectSound;

        audioSource.volume =
            collectVolume;

        // QUAN TRỌNG:
        // 0 = 2D hoàn toàn
        // 1 = 3D hoàn toàn
        audioSource.spatialBlend = 0f;

        audioSource.playOnAwake = false;

        audioSource.loop = false;

        audioSource.dopplerLevel = 0f;

        audioSource.pitch = 1f;

        audioSource.minDistance = 1f;

        audioSource.maxDistance = 500f;

        // Không bị ảnh hưởng bởi vị trí Coin.
        audioSource.panStereo = 0f;

        //=========================================================
        // PLAY
        //=========================================================

        audioSource.Play();

        //=========================================================
        // AUTO DESTROY
        //=========================================================

        Destroy(
            audioObject,
            collectSound.length + 0.1f
        );
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

        if (collectVolume < 0f)
            collectVolume = 0f;

        if (collectVolume > 1f)
            collectVolume = 1f;
    }
}