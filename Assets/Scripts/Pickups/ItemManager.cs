using UnityEngine;

/// <summary>
/// Trung tâm điều phối toàn bộ Item Runtime của Player.
///
/// ItemManager KHÔNG chứa gameplay logic của từng item.
/// Nó chỉ:
/// - Giữ reference tới các Item Controller.
/// - Cho phép hệ thống khác Activate item.
/// - Cho phép HUD kiểm tra trạng thái item.
/// - Làm lớp trung gian giữa Gameplay và Item Controller.
///
/// Gameplay logic nằm trong:
/// - PhotonController
/// - ShieldController
/// - MagnetController
/// - GumController
/// </summary>
public class ItemManager : MonoBehaviour
{
    //=============================================================
    // SINGLETON
    //=============================================================

    public static ItemManager Instance { get; private set; }


    //=============================================================
    // ITEM REFERENCES
    //=============================================================

    [Header("Item Controllers")]

    [Tooltip("Photon Controller của Player.")]
    [SerializeField]
    private PhotonController photonController;


    [Tooltip("Shield Controller của Player.")]
    [SerializeField]
    private ShieldController shieldController;


    [Tooltip("Magnet Controller của Player.")]
    [SerializeField]
    private MagnetController magnetController;


    [Tooltip("Gum Controller của Player.")]
    [SerializeField]
    private GumController gumController;


    //=============================================================
    // AUTO FIND
    //=============================================================

    [Header("Auto Find")]

    [Tooltip(
        "Nếu bật, ItemManager sẽ tự tìm các Item Controller " +
        "trên Player khi khởi tạo."
    )]
    [SerializeField]
    private bool autoFindControllers = true;


    //=============================================================
    // AWAKE
    //=============================================================

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        FindControllers();
    }


    //=============================================================
    // FIND CONTROLLERS
    //=============================================================

    private void FindControllers()
    {
        if (!autoFindControllers)
        {
            return;
        }


        //=========================================================
        // PHOTON
        //=========================================================

        if (photonController == null)
        {
            photonController =
                GetComponent<PhotonController>();

            if (photonController == null)
            {
                photonController =
                    GetComponentInChildren<PhotonController>(true);
            }
        }


        //=========================================================
        // SHIELD
        //=========================================================

        if (shieldController == null)
        {
            shieldController =
                GetComponent<ShieldController>();

            if (shieldController == null)
            {
                shieldController =
                    GetComponentInChildren<ShieldController>(true);
            }
        }


        //=========================================================
        // MAGNET
        //=========================================================

        if (magnetController == null)
        {
            magnetController =
                GetComponent<MagnetController>();

            if (magnetController == null)
            {
                magnetController =
                    GetComponentInChildren<MagnetController>(true);
            }
        }


        //=========================================================
        // GUM
        //=========================================================

        if (gumController == null)
        {
            gumController =
                GetComponent<GumController>();

            if (gumController == null)
            {
                gumController =
                    GetComponentInChildren<GumController>(true);
            }
        }
    }


    //=============================================================
    // PHOTON
    //=============================================================

    /// <summary>
    /// Kích hoạt Photon.
    /// </summary>
    public void ActivatePhoton()
    {
        if (photonController == null)
        {
            Debug.LogWarning(
                "[ItemManager] PhotonController chưa được gán.",
                this
            );

            return;
        }

        photonController.ActivatePhoton();
    }


    /// <summary>
    /// Tắt Photon.
    /// </summary>
    public void DeactivatePhoton()
    {
        if (photonController == null)
        {
            return;
        }

        photonController.DeactivatePhoton();
    }


    /// <summary>
    /// Photon có đang active hay không.
    /// </summary>
    public bool IsPhotonActive
    {
        get
        {
            return
                photonController != null &&
                photonController.IsPhotonActive;
        }
    }


    /// <summary>
    /// Thời gian Photon còn lại.
    /// </summary>
    public float PhotonTimeRemaining
    {
        get
        {
            if (photonController == null)
            {
                return 0f;
            }

            return photonController.PhotonTimeRemaining;
        }
    }


    //=============================================================
    // SHIELD
    //=============================================================

    /// <summary>
    /// Kích hoạt Shield.
    /// </summary>
    public void ActivateShield()
    {
        if (shieldController == null)
        {
            Debug.LogWarning(
                "[ItemManager] ShieldController chưa được gán.",
                this
            );

            return;
        }

        shieldController.Activate();
    }


    /// <summary>
    /// Shield có đang active hay không.
    /// </summary>
    public bool IsShieldActive
    {
        get
        {
            return
                shieldController != null &&
                shieldController.IsActive();
        }
    }


    /// <summary>
    /// Thời gian Shield còn lại.
    /// </summary>
    public float ShieldTimeRemaining
    {
        get
        {
            if (shieldController == null)
            {
                return 0f;
            }

            return shieldController.GetRemainingTime();
        }
    }


    /// <summary>
    /// Số hit Shield còn chặn được.
    /// </summary>
    public int ShieldRemainingHits
    {
        get
        {
            if (shieldController == null)
            {
                return 0;
            }

            return shieldController.GetRemainingHits();
        }
    }


    /// <summary>
    /// Player đang bất tử sau khi Shield block hit.
    /// </summary>
    public bool IsShieldInvulnerable
    {
        get
        {
            return
                shieldController != null &&
                shieldController.IsInvulnerable();
        }
    }


    //=============================================================
    // MAGNET
    //=============================================================

    /// <summary>
    /// Kích hoạt Magnet.
    /// </summary>
    public void ActivateMagnet()
    {
        if (magnetController == null)
        {
            Debug.LogWarning(
                "[ItemManager] MagnetController chưa được gán.",
                this
            );

            return;
        }

        magnetController.Activate();
    }


    /// <summary>
    /// Kích hoạt Magnet với duration tùy chỉnh.
    /// </summary>
    public void ActivateMagnet(float duration)
    {
        if (magnetController == null)
        {
            Debug.LogWarning(
                "[ItemManager] MagnetController chưa được gán.",
                this
            );

            return;
        }

        magnetController.Activate(duration);
    }


    /// <summary>
    /// Magnet có đang active hay không.
    /// </summary>
    public bool IsMagnetActive
    {
        get
        {
            return
                magnetController != null &&
                magnetController.IsActive();
        }
    }


    /// <summary>
    /// Thời gian Magnet còn lại.
    /// </summary>
    public float MagnetTimeRemaining
    {
        get
        {
            if (magnetController == null)
            {
                return 0f;
            }

            return magnetController.GetRemainingTime();
        }
    }


    //=============================================================
    // GUM
    //=============================================================

    /// <summary>
    /// Kích hoạt Gum.
    /// </summary>
    public void ActivateGum()
    {
        if (gumController == null)
        {
            Debug.LogWarning(
                "[ItemManager] GumController chưa được gán.",
                this
            );

            return;
        }

        gumController.Activate();
    }


    /// <summary>
    /// Tắt Gum.
    /// </summary>
    public void DeactivateGum()
    {
        if (gumController == null)
        {
            return;
        }

        gumController.Deactivate();
    }


    /// <summary>
    /// Gum có đang active hay không.
    /// </summary>
    public bool IsGumActive
    {
        get
        {
            return
                gumController != null &&
                gumController.IsActive;
        }
    }


    /// <summary>
    /// Thời gian Gum còn lại.
    /// </summary>
    public float GumTimeRemaining
    {
        get
        {
            if (gumController == null)
            {
                return 0f;
            }

            return gumController.TimeRemaining;
        }
    }


    /// <summary>
    /// Tốc độ ngang khi Gum đang active.
    /// </summary>
    public float GumHorizontalSpeed
    {
        get
        {
            if (gumController == null)
            {
                return 0f;
            }

            return gumController.HorizontalSpeed;
        }
    }


    //=============================================================
    // CONTROLLER ACCESS
    //=============================================================

    public PhotonController Photon
    {
        get
        {
            return photonController;
        }
    }


    public ShieldController Shield
    {
        get
        {
            return shieldController;
        }
    }


    public MagnetController Magnet
    {
        get
        {
            return magnetController;
        }
    }


    public GumController Gum
    {
        get
        {
            return gumController;
        }
    }


    //=============================================================
    // REFRESH
    //=============================================================

    public void RefreshControllers()
    {
        FindControllers();
    }


    //=============================================================
    // VALIDATE
    //=============================================================

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (autoFindControllers)
        {
            FindControllers();
        }
    }
}