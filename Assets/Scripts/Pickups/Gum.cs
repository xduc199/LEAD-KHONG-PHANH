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

    //=========================================================
    // START
    //=========================================================

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
        if (rotateSpeed > 0f)
        {
            transform.Rotate(
                0f,
                rotateSpeed * Time.deltaTime,
                0f,
                Space.World
            );
        }

        // Float
        if (floatAmplitude > 0f && floatSpeed > 0f)
        {
            float offset =
                Mathf.Sin(Time.time * floatSpeed) *
                floatAmplitude;

            transform.position =
                startPosition +
                Vector3.up * offset;
        }
    }

    //=========================================================
    // COLLIDER
    //=========================================================

    private void SetupCollider()
    {
        gumCollider = GetComponent<SphereCollider>();

        if (gumCollider == null)
        {
            gumCollider = gameObject.AddComponent<SphereCollider>();
        }

        gumCollider.isTrigger = true;
        gumCollider.radius = collectRadius;
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

        // Đánh dấu ngay lập tức để tránh collect nhiều lần
        collected = true;

        // Tắt collider ngay
        if (gumCollider != null)
        {
            gumCollider.enabled = false;
        }

        //=====================================================
        // GUM EFFECT
        //=====================================================

        ActivateGum();

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
    // ACTIVATE GUM
    //=========================================================

    private void ActivateGum()
    {
        if (ItemManager.Instance == null)
        {
            Debug.LogWarning(
                "[Gum] ItemManager.Instance chưa tồn tại."
            );

            return;
        }

        ItemManager.Instance.ActivateGum();
    }

    //=========================================================
    // AUDIO
    //=========================================================

    private void PlayCollectSound()
    {
        if (collectSound == null)
            return;

        GameObject audioObject =
            new GameObject("GumCollectAudio");

        AudioSource audioSource =
            audioObject.AddComponent<AudioSource>();

        audioSource.clip = collectSound;
        audioSource.volume = collectVolume;

        // 2D audio
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
    // RESET
    //=========================================================

    public void ResetGum()
    {
        collected = false;

        if (gumCollider != null)
        {
            gumCollider.enabled = true;
        }

        gameObject.SetActive(true);
        startPosition = transform.position;
    }

    //=========================================================
    // STATUS
    //=========================================================

    public bool IsCollected()
    {
        return collected;
    }

    //=========================================================
    // VALIDATE
    //=========================================================

    private void OnValidate()
    {
        rotateSpeed = Mathf.Max(0f, rotateSpeed);

        floatAmplitude = Mathf.Max(0f, floatAmplitude);

        floatSpeed = Mathf.Max(0f, floatSpeed);

        collectRadius = Mathf.Max(0.1f, collectRadius);

        collectVolume = Mathf.Clamp01(collectVolume);
    }
}