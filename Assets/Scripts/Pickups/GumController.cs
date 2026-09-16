using UnityEngine;

public class GumController : MonoBehaviour
{
    //=========================================================
    // GUM SETTINGS
    //=========================================================

    [Header("Gum Settings")]

    [Tooltip("Thời gian Gum Boost mặc định khi chưa có Upgrade.")]
    [SerializeField]
    private float duration = 5f;

    [Tooltip("Tốc độ ngang Gum mặc định khi chưa có Upgrade.")]
    [SerializeField]
    private float horizontalSpeed = 30f;


    //=========================================================
    // PICKUP AUDIO
    //=========================================================

    [Header("Pickup Audio")]

    [Tooltip("Âm thanh khi Player nhặt Gum.")]
    [SerializeField]
    private AudioClip pickupSound;

    [Tooltip("Âm lượng pickup.")]
    [SerializeField, Range(0f, 1f)]
    private float pickupVolume = 0.9f;


    //=========================================================
    // PICKUP SETTINGS
    //=========================================================

    [Header("Pickup Settings")]

    [Tooltip("Có tự hủy Gum sau khi Player nhặt không.")]
    [SerializeField]
    private bool destroyOnCollect = true;


    //=========================================================
    // DEBUG
    //=========================================================

    [Header("Debug")]

    [SerializeField]
    private bool debugLogs = false;


    //=========================================================
    // RUNTIME
    //=========================================================

    private bool collected;
    private bool isActive;
    private float timeRemaining;


    //=========================================================
    // RUNTIME UPGRADE VALUES
    //=========================================================

    private float activeDuration;
    private float activeHorizontalSpeed;


    //=========================================================
    // UPDATE
    //=========================================================

    private void Update()
    {
        if (!isActive)
        {
            return;
        }

        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0f)
        {
            Deactivate();
        }
    }


    //=========================================================
    // ACTIVATE
    //=========================================================

    /// <summary>
    /// Kích hoạt Gum Boost.
    ///
    /// Khi ItemUpgradeManager tồn tại:
    ///     duration        -> ItemUpgradeLevel.duration
    ///     horizontalSpeed -> ItemUpgradeLevel.horizontalSpeed
    ///
    /// Nếu Upgrade chưa sẵn sàng:
    ///     sử dụng giá trị Inspector mặc định.
    /// </summary>
    public void Activate()
    {
        float upgradeDuration = duration;
        float upgradeHorizontalSpeed = horizontalSpeed;

        TryApplyUpgradeValues(
            ref upgradeDuration,
            ref upgradeHorizontalSpeed
        );

        Activate(
            upgradeDuration,
            upgradeHorizontalSpeed
        );
    }


    /// <summary>
    /// Kích hoạt Gum Boost với thời gian tùy chỉnh.
    ///
    /// Hàm này được giữ lại để không phá API cũ.
    /// Horizontal Speed vẫn lấy từ Upgrade hiện tại.
    /// </summary>
    public void Activate(float customDuration)
    {
        float upgradeHorizontalSpeed = horizontalSpeed;

        TryApplyUpgradeValues(
            ref customDuration,
            ref upgradeHorizontalSpeed
        );

        Activate(
            customDuration,
            upgradeHorizontalSpeed
        );
    }


    /// <summary>
    /// Kích hoạt Gum với Duration và Horizontal Speed
    /// đã được xác định.
    /// </summary>
    private void Activate(
        float customDuration,
        float customHorizontalSpeed
    )
    {
        activeDuration = Mathf.Max(
            0f,
            customDuration
        );

        activeHorizontalSpeed = Mathf.Max(
            0f,
            customHorizontalSpeed
        );

        timeRemaining = activeDuration;

        isActive = timeRemaining > 0f;

        if (debugLogs)
        {
            Debug.Log(
                "[GumController] GUM ACTIVE | " +
                "Duration: " +
                activeDuration +
                " | Horizontal Speed: " +
                activeHorizontalSpeed
            );
        }
    }


    //=========================================================
    // APPLY UPGRADE
    //=========================================================

    /// <summary>
    /// Lấy giá trị Gum từ ItemUpgradeManager.
    ///
    /// ItemUpgradeLevel:
    ///     duration        -> thời gian Gum
    ///     horizontalSpeed -> tốc độ ngang Gum
    ///
    /// Không sửa PlayerData trực tiếp.
    /// Không tạo hệ thống upgrade riêng.
    /// </summary>
    private void TryApplyUpgradeValues(
        ref float upgradeDuration,
        ref float upgradeHorizontalSpeed
    )
    {
        if (ItemUpgradeManager.Instance == null)
        {
            return;
        }

        ItemUpgradeLevel levelData =
            ItemUpgradeManager.Instance.GetCurrentLevelData(
                UpgradeItemType.Gum
            );

        if (levelData == null)
        {
            return;
        }

        upgradeDuration = Mathf.Max(
            0f,
            levelData.duration
        );

        upgradeHorizontalSpeed = Mathf.Max(
            0f,
            levelData.horizontalSpeed
        );

        if (debugLogs)
        {
            Debug.Log(
                "[GumController] Upgrade Applied | " +
                "Level=" +
                levelData.level +
                " | Duration=" +
                levelData.duration +
                " | HorizontalSpeed=" +
                levelData.horizontalSpeed
            );
        }
    }


    //=========================================================
    // DEACTIVATE
    //=========================================================

    /// <summary>
    /// Tắt Gum Boost.
    /// </summary>
    public void Deactivate()
    {
        if (!isActive && timeRemaining <= 0f)
        {
            return;
        }

        isActive = false;
        timeRemaining = 0f;

        if (debugLogs)
        {
            Debug.Log(
                "[GumController] GUM DEACTIVATED."
            );
        }
    }


    //=========================================================
    // COLLECT
    //=========================================================

    /// <summary>
    /// Gọi khi Player nhặt Gum.
    ///
    /// GumController này có thể được đặt trực tiếp
    /// trên Gum pickup.
    /// </summary>
    public void Collect()
    {
        if (collected)
        {
            return;
        }

        collected = true;


        //=====================================================
        // PLAY PICKUP AUDIO
        //=====================================================

        PlayPickupSound();


        //=====================================================
        // ACTIVATE THROUGH ITEM MANAGER
        //=====================================================

        if (ItemManager.Instance != null)
        {
            ItemManager.Instance.ActivateGum();
        }
        else
        {
            Debug.LogWarning(
                "[GumController] ItemManager.Instance " +
                "chưa tồn tại."
            );
        }


        //=====================================================
        // DESTROY PICKUP
        //=====================================================

        if (destroyOnCollect)
        {
            Destroy(gameObject);
        }


        if (debugLogs)
        {
            Debug.Log(
                "[GumController] GUM COLLECTED."
            );
        }
    }


    //=========================================================
    // AUDIO
    //=========================================================

    private void PlayPickupSound()
    {
        if (pickupSound == null)
        {
            return;
        }

        GameObject audioObject =
            new GameObject("GumPickupAudio");

        AudioSource source =
            audioObject.AddComponent<AudioSource>();

        source.clip = pickupSound;
        source.volume = pickupVolume;
        source.spatialBlend = 0f;
        source.panStereo = 0f;
        source.dopplerLevel = 0f;
        source.playOnAwake = false;
        source.loop = false;

        source.Play();

        Destroy(
            audioObject,
            pickupSound.length + 0.1f
        );
    }


    //=========================================================
    // RESET PICKUP
    //=========================================================

    /// <summary>
    /// Reset trạng thái pickup.
    /// </summary>
    public void ResetGum()
    {
        collected = false;
    }


    //=========================================================
    // PUBLIC RUNTIME API
    //=========================================================

    /// <summary>
    /// Gum Boost hiện đang active.
    /// </summary>
    public bool IsActive
    {
        get
        {
            return isActive;
        }
    }


    /// <summary>
    /// Thời gian Gum Boost còn lại.
    /// </summary>
    public float TimeRemaining
    {
        get
        {
            return Mathf.Max(
                0f,
                timeRemaining
            );
        }
    }


    /// <summary>
    /// Tốc độ ngang khi Gum active.
    ///
    /// Nếu Gum chưa active:
    ///     lấy Upgrade hiện tại.
    ///
    /// Nếu Upgrade chưa tồn tại:
    ///     dùng giá trị Inspector mặc định.
    /// </summary>
    public float HorizontalSpeed
    {
        get
        {
            if (isActive)
            {
                return activeHorizontalSpeed;
            }

            if (ItemUpgradeManager.Instance != null)
            {
                ItemUpgradeLevel levelData =
                    ItemUpgradeManager.Instance.GetCurrentLevelData(
                        UpgradeItemType.Gum
                    );

                if (levelData != null)
                {
                    return Mathf.Max(
                        0f,
                        levelData.horizontalSpeed
                    );
                }
            }

            return horizontalSpeed;
        }
    }


    /// <summary>
    /// Pickup đã được nhặt chưa.
    /// </summary>
    public bool IsCollected()
    {
        return collected;
    }


    //=========================================================
    // VALIDATE
    //=========================================================

    private void OnValidate()
    {
        duration = Mathf.Max(
            0f,
            duration
        );

        horizontalSpeed = Mathf.Max(
            0f,
            horizontalSpeed
        );

        pickupVolume = Mathf.Clamp01(
            pickupVolume
        );
    }
}