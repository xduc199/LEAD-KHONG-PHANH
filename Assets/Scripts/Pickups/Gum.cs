using UnityEngine;

public class Gum : MonoBehaviour
{
    //=========================================================
    // GUM SETTINGS
    //=========================================================

    [Header("Gum Settings")]
    [SerializeField] private float rotateSpeed = 120f;

    [SerializeField] private float floatAmplitude = 0.15f;
    [SerializeField] private float floatSpeed = 2f;

    //=========================================================
    // COLLECTION
    //=========================================================

    [Header("Collection")]
    [SerializeField] private float collectRadius = 0.8f;
    [SerializeField] private bool destroyOnCollect = true;

    //=========================================================
    // AUDIO
    //=========================================================

    [Header("Audio")]
    [SerializeField] private AudioClip collectSound;

    [SerializeField, Range(0f, 1f)]
    private float collectVolume = 1f;

    //=========================================================
    // INTERNAL
    //=========================================================

    private SphereCollider gumCollider;
    private bool collected;

    private Vector3 startPosition;

    //=========================================================
    // AWAKE
    //=========================================================

    private void Awake()
    {
        SetupCollider();
    }

    private void Start()
    {
        startPosition = transform.position;
    }

    //=========================================================
    // UPDATE
    //=========================================================

    private void Update()
    {
        // Rotate
        transform.Rotate(
            0f,
            rotateSpeed * Time.deltaTime,
            0f,
            Space.World
        );

        // Float
        float offset =
            Mathf.Sin(
                Time.time * floatSpeed
            ) * floatAmplitude;

        transform.position =
            startPosition +
            Vector3.up * offset;
    }

    //=========================================================
    // COLLIDER
    //=========================================================

    private void SetupCollider()
    {
        gumCollider =
            GetComponent<SphereCollider>();

        if (gumCollider == null)
        {
            gumCollider =
                gameObject.AddComponent<SphereCollider>();
        }

        gumCollider.isTrigger = true;

        gumCollider.radius =
            collectRadius;
    }

    //=========================================================
    // TRIGGER
    //=========================================================

    private void OnTriggerEnter(Collider other)
    {
        if (collected)
            return;

        if (!other.CompareTag("Player"))
            return;

        Collect();
    }

    //=========================================================
    // COLLECT
    //=========================================================

    public void Collect()
    {
        if (collected)
            return;

        collected = true;

        // Disable collider immediately
        if (gumCollider != null)
        {
            gumCollider.enabled = false;
        }

        //=====================================================
        // GUM EFFECT
        //=====================================================

        ApplyGumEffect();

        //=====================================================
        // SOUND
        //=====================================================

        PlayCollectSound();

        //=====================================================
        // DESTROY
        //=====================================================

        if (destroyOnCollect)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    //=========================================================
    // GUM EFFECT
    //=========================================================

    private void ApplyGumEffect()
    {
        PlayerController player =
            FindPlayerController();

        if (player == null)
        {
            Debug.LogWarning(
                "[Gum] Không tìm thấy PlayerController."
            );

            return;
        }

        /*
         * Nếu PlayerController hiện tại của bạn
         * có hệ thống boost Coffee cũ thì phần này
         * sẽ gọi qua hàm tương ứng.
         *
         * Nếu chưa có hàm Gum riêng, tạm thời
         * Gum chỉ thực hiện pickup + sound.
         */

        // Có thể mở rộng tại đây:
        // player.ActivateGumBoost();
    }

    //=========================================================
    // FIND PLAYER
    //=========================================================

    private PlayerController FindPlayerController()
    {
        GameObject player =
            GameObject.FindGameObjectWithTag("Player");

        if (player == null)
            return null;

        return player.GetComponent<PlayerController>();
    }

    //=========================================================
    // AUDIO
    //=========================================================

    private void PlayCollectSound()
    {
        if (collectSound == null)
            return;

        GameObject audioObject =
            new GameObject(
                "GumCollectAudio"
            );

        AudioSource audioSource =
            audioObject.AddComponent<AudioSource>();

        audioSource.clip =
            collectSound;

        audioSource.volume =
            collectVolume;

        // 2D
        audioSource.spatialBlend = 0f;

        audioSource.playOnAwake = false;
        audioSource.loop = false;

        audioSource.dopplerLevel = 0f;
        audioSource.pitch = 1f;

        audioSource.panStereo = 0f;

        audioSource.Play();

        Destroy(
            audioObject,
            collectSound.length + 0.1f
        );
    }

    //=========================================================
    // MAGNET SUPPORT
    //=========================================================

    public void CollectFromMagnet()
    {
        if (collected)
            return;

        Collect();
    }

    //=========================================================
    // VALIDATE
    //=========================================================

    private void OnValidate()
    {
        if (rotateSpeed < 0f)
            rotateSpeed = 0f;

        if (floatAmplitude < 0f)
            floatAmplitude = 0f;

        if (floatSpeed < 0f)
            floatSpeed = 0f;

        if (collectRadius < 0.1f)
            collectRadius = 0.1f;

        if (collectVolume < 0f)
            collectVolume = 0f;

        if (collectVolume > 1f)
            collectVolume = 1f;
    }
}