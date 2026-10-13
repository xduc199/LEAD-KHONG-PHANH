using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MainMenuGarageUI : MonoBehaviour
{
    // =========================================================
    // DATABASE CONTRACT
    // =========================================================

    private const int DefaultVehicleId = 1;

    private const int VehicleStartId = 1;
    private const int VehicleEndId = 40;

    private const int VehicleCount =
        VehicleEndId - VehicleStartId + 1;


    // =========================================================
    // PANEL
    // =========================================================

    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;


    // =========================================================
    // DATABASE
    // =========================================================

    [Header("Database")]
    [SerializeField] private GameItemDatabase itemDatabase;

    [SerializeField] private GameItemVisualDatabase visualDatabase;


    // =========================================================
    // EQUIPMENT
    // =========================================================

    [Header("Equipment")]
    [SerializeField] private EquipmentManager equipmentManager;


    // =========================================================
    // 3D SHOWCASE
    // =========================================================

    [Header("3D Showcase")]
    [SerializeField]
    private GarageShowcaseController showcaseController;


    // =========================================================
    // NAVIGATION
    // =========================================================

    [Header("Navigation")]
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;


    // =========================================================
    // VEHICLE INFO
    // =========================================================

    [Header("Vehicle Info")]
    [SerializeField] private TMP_Text vehicleNameText;

    [SerializeField]
    private TMP_Text vehicleDescriptionText;

    [SerializeField]
    private TMP_Text vehicleIndexText;

    [SerializeField]
    private TMP_Text vehicleStatusText;


    // =========================================================
    // EQUIP
    // =========================================================

    [Header("Equip")]
    [SerializeField] private Button equipButton;

    [SerializeField]
    private TMP_Text equipButtonText;


    // =========================================================
    // CLOSE
    // =========================================================

    [Header("Close")]
    [SerializeField] private Button closeButton;


    // =========================================================
    // RUNTIME
    // =========================================================

    private int currentVehicleId = DefaultVehicleId;

    private bool initialized;


    // =========================================================
    // PUBLIC
    // =========================================================

    public int CurrentVehicleId =>
        currentVehicleId;


    public bool IsOpen
    {
        get
        {
            return panelRoot != null &&
                   panelRoot.activeSelf;
        }
    }


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        InitializeReferences();
        BindButtons();

        currentVehicleId =
            ResolveInitialOwnedVehicleId();

        initialized = true;

        RefreshVehicle();

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }


    private void OnDestroy()
    {
        UnbindButtons();
    }


    // =========================================================
    // INITIALIZATION
    // =========================================================

    private void InitializeReferences()
    {
        if (equipmentManager == null)
            equipmentManager = EquipmentManager.Instance;


        if (showcaseController == null)
        {
            showcaseController =
                FindFirstObjectByType<GarageShowcaseController>(
                    FindObjectsInactive.Include
                );
        }


        // -----------------------------------------------------
        // ITEM DATABASE
        // -----------------------------------------------------

        if (itemDatabase == null)
        {
            Debug.LogError(
                "[MainMenuGarageUI] " +
                "GameItemDatabase chưa được gán.",
                this
            );
        }
        else
        {
            itemDatabase.Initialize();
        }


        // -----------------------------------------------------
        // VISUAL DATABASE
        // -----------------------------------------------------

        if (visualDatabase == null)
        {
            Debug.LogError(
                "[MainMenuGarageUI] " +
                "GameItemVisualDatabase chưa được gán.",
                this
            );
        }
        else
        {
            visualDatabase.Initialize();
        }


        // -----------------------------------------------------
        // EQUIPMENT
        // -----------------------------------------------------

        if (equipmentManager == null)
        {
            Debug.LogWarning(
                "[MainMenuGarageUI] " +
                "EquipmentManager chưa tồn tại.",
                this
            );
        }


        // -----------------------------------------------------
        // SHOWCASE
        // -----------------------------------------------------

        if (showcaseController == null)
        {
            Debug.LogWarning(
                "[MainMenuGarageUI] " +
                "GarageShowcaseController " +
                "chưa được gán/tìm thấy.",
                this
            );
        }
    }


    // =========================================================
    // BUTTONS
    // =========================================================

    private void BindButtons()
    {
        if (leftButton != null)
        {
            leftButton.onClick.RemoveListener(
                ShowPreviousVehicle
            );

            leftButton.onClick.AddListener(
                ShowPreviousVehicle
            );
        }


        if (rightButton != null)
        {
            rightButton.onClick.RemoveListener(
                ShowNextVehicle
            );

            rightButton.onClick.AddListener(
                ShowNextVehicle
            );
        }


        if (equipButton != null)
        {
            equipButton.onClick.RemoveListener(
                EquipCurrentVehicle
            );

            equipButton.onClick.AddListener(
                EquipCurrentVehicle
            );
        }


        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(
                Close
            );

            closeButton.onClick.AddListener(
                Close
            );
        }
    }


    private void UnbindButtons()
    {
        if (leftButton != null)
            leftButton.onClick.RemoveListener(
                ShowPreviousVehicle
            );


        if (rightButton != null)
            rightButton.onClick.RemoveListener(
                ShowNextVehicle
            );


        if (equipButton != null)
            equipButton.onClick.RemoveListener(
                EquipCurrentVehicle
            );


        if (closeButton != null)
            closeButton.onClick.RemoveListener(
                Close
            );
    }


    // =========================================================
    // OPEN / CLOSE
    // =========================================================

    public void Open()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);

        currentVehicleId =
            ResolveInitialOwnedVehicleId();

        RefreshVehicle();
        RefreshShowcase();
    }


    public void OpenVehicle(int vehicleId)
    {
        if (!IsVehicleId(vehicleId) ||
            !InventoryOwnership.IsOwned(vehicleId))
        {
            vehicleId =
                ResolveInitialOwnedVehicleId();
        }

        currentVehicleId = vehicleId;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        RefreshVehicle();
        RefreshShowcase();
    }


    public void Close()
    {
        if (showcaseController != null)
            showcaseController.CloseShowcase();

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }


    // =========================================================
    // OWNERSHIP
    // =========================================================

    private int ResolveInitialOwnedVehicleId()
    {
        int equippedId =
            ResolveEquippedVehicleId();


        if (IsVehicleId(equippedId) &&
            InventoryOwnership.IsOwned(equippedId))
        {
            return equippedId;
        }


        return FindFirstOwnedVehicle();
    }


    private int FindFirstOwnedVehicle()
    {
        for (
            int id = VehicleStartId;
            id <= VehicleEndId;
            id++)
        {
            if (InventoryOwnership.IsOwned(id))
                return id;
        }


        return -1;
    }


    private int FindNextOwnedVehicle(
        int startId,
        int direction)
    {
        if (direction != -1 &&
            direction != 1)
        {
            return startId;
        }


        int candidate = startId;


        for (
            int i = 0;
            i < VehicleCount;
            i++)
        {
            candidate += direction;


            if (candidate > VehicleEndId)
            {
                candidate = VehicleStartId;
            }
            else if (candidate < VehicleStartId)
            {
                candidate = VehicleEndId;
            }


            if (InventoryOwnership.IsOwned(candidate))
                return candidate;
        }


        return -1;
    }


    private bool HasAnyOwnedVehicle()
    {
        return FindFirstOwnedVehicle() >=
               VehicleStartId;
    }


    // =========================================================
    // NAVIGATION
    // =========================================================

    public void ShowPreviousVehicle()
    {
        if (!HasAnyOwnedVehicle())
        {
            RefreshNoOwnedVehicleUI();
            return;
        }


        if (!IsVehicleId(currentVehicleId) ||
            !InventoryOwnership.IsOwned(currentVehicleId))
        {
            currentVehicleId =
                ResolveInitialOwnedVehicleId();

            RefreshVehicle();
            RefreshShowcase();
            return;
        }


        int previousId =
            FindNextOwnedVehicle(
                currentVehicleId,
                -1
            );


        if (!IsVehicleId(previousId))
            return;


        currentVehicleId = previousId;

        RefreshVehicle();
        RefreshShowcase();
    }


    public void ShowNextVehicle()
    {
        if (!HasAnyOwnedVehicle())
        {
            RefreshNoOwnedVehicleUI();
            return;
        }


        if (!IsVehicleId(currentVehicleId) ||
            !InventoryOwnership.IsOwned(currentVehicleId))
        {
            currentVehicleId =
                ResolveInitialOwnedVehicleId();

            RefreshVehicle();
            RefreshShowcase();
            return;
        }


        int nextId =
            FindNextOwnedVehicle(
                currentVehicleId,
                1
            );


        if (!IsVehicleId(nextId))
            return;


        currentVehicleId = nextId;

        RefreshVehicle();
        RefreshShowcase();
    }


    // =========================================================
    // REFRESH
    // =========================================================

    public void RefreshFromEquipment()
    {
        currentVehicleId =
            ResolveInitialOwnedVehicleId();

        RefreshVehicle();
        RefreshShowcase();
    }


    private void RefreshVehicle()
    {
        if (!initialized &&
            itemDatabase == null)
        {
            return;
        }


        if (!HasAnyOwnedVehicle())
        {
            currentVehicleId = -1;

            RefreshNoOwnedVehicleUI();
            CloseShowcaseOnly();

            return;
        }


        if (!IsVehicleId(currentVehicleId) ||
            !InventoryOwnership.IsOwned(currentVehicleId))
        {
            currentVehicleId =
                ResolveInitialOwnedVehicleId();


            if (!IsVehicleId(currentVehicleId))
            {
                RefreshNoOwnedVehicleUI();
                CloseShowcaseOnly();
                return;
            }
        }


        if (!TryGetVehicle(
                currentVehicleId,
                out GameItemData item))
        {
            UpdateInvalidVehicleUI();
            CloseShowcaseOnly();
            return;
        }


        UpdateVehicleInfo(item);
        UpdateEquipmentUI();
    }


    private void RefreshShowcase()
    {
        if (showcaseController == null)
            return;


        if (!IsOpen)
            return;


        if (!IsVehicleId(currentVehicleId) ||
            !InventoryOwnership.IsOwned(currentVehicleId))
        {
            showcaseController.CloseShowcase();
            return;
        }


        showcaseController.OpenShowcase(
            currentVehicleId
        );
    }


    private void CloseShowcaseOnly()
    {
        if (showcaseController != null)
            showcaseController.CloseShowcase();
    }


    // =========================================================
    // NO VEHICLE
    // =========================================================

    private void RefreshNoOwnedVehicleUI()
    {
        if (vehicleNameText != null)
            vehicleNameText.text = "CHƯA CÓ XE";


        if (vehicleDescriptionText != null)
        {
            vehicleDescriptionText.text =
                "Bạn chưa sở hữu phương tiện nào.";
        }


        if (vehicleIndexText != null)
            vehicleIndexText.text = "-- / --";


        if (vehicleStatusText != null)
            vehicleStatusText.text = "CHƯA SỞ HỮU";


        if (equipButton != null)
            equipButton.interactable = false;


        if (equipButtonText != null)
            equipButtonText.text = "CHƯA CÓ XE";


        if (leftButton != null)
            leftButton.interactable = false;


        if (rightButton != null)
            rightButton.interactable = false;
    }


    // =========================================================
    // DATABASE
    // =========================================================

    private bool TryGetVehicle(
        int vehicleId,
        out GameItemData item)
    {
        item = null;


        if (itemDatabase == null)
            return false;


        if (!IsVehicleId(vehicleId))
            return false;


        if (!itemDatabase.TryGet(
                vehicleId,
                out item))
        {
            Debug.LogWarning(
                "[MainMenuGarageUI] " +
                "Không tìm thấy Vehicle Item ID = " +
                vehicleId,
                this
            );

            item = null;
            return false;
        }


        if (item == null)
            return false;


        if (item.Category !=
            GameItemCategory.Vehicle)
        {
            Debug.LogError(
                "[MainMenuGarageUI] " +
                "Item ID " +
                vehicleId +
                " không thuộc Vehicle category.",
                this
            );

            item = null;
            return false;
        }


        return true;
    }


    // =========================================================
    // INFO
    // =========================================================

    private void UpdateVehicleInfo(
        GameItemData item)
    {
        if (vehicleNameText != null)
            vehicleNameText.text = item.ItemName;


        if (vehicleDescriptionText != null)
        {
            vehicleDescriptionText.text =
                item.Description;
        }


        if (vehicleIndexText != null)
        {
            int displayIndex =
                currentVehicleId -
                VehicleStartId +
                1;


            vehicleIndexText.text =
                displayIndex.ToString("00") +
                " / " +
                VehicleCount.ToString("00");
        }
    }


    private void UpdateInvalidVehicleUI()
    {
        if (vehicleNameText != null)
            vehicleNameText.text = "UNKNOWN VEHICLE";


        if (vehicleDescriptionText != null)
        {
            vehicleDescriptionText.text =
                "Không tìm thấy dữ liệu phương tiện.";
        }


        if (vehicleIndexText != null)
        {
            vehicleIndexText.text =
                "-- / " +
                VehicleCount.ToString("00");
        }


        if (vehicleStatusText != null)
            vehicleStatusText.text = "DATABASE ERROR";


        if (equipButton != null)
            equipButton.interactable = false;


        if (leftButton != null)
            leftButton.interactable = false;


        if (rightButton != null)
            rightButton.interactable = false;
    }


    // =========================================================
    // EQUIPMENT UI
    // =========================================================

    private void UpdateEquipmentUI()
    {
        bool owned =
            InventoryOwnership.IsOwned(
                currentVehicleId
            );


        bool equipped =
            IsCurrentVehicleEquipped();


        bool hasOwnedVehicle =
            HasAnyOwnedVehicle();


        if (leftButton != null)
            leftButton.interactable =
                hasOwnedVehicle;


        if (rightButton != null)
            rightButton.interactable =
                hasOwnedVehicle;


        if (vehicleStatusText != null)
        {
            if (!owned)
                vehicleStatusText.text =
                    "CHƯA SỞ HỮU";
            else if (equipped)
                vehicleStatusText.text =
                    "ĐANG TRANG BỊ";
            else
                vehicleStatusText.text =
                    "ĐÃ SỞ HỮU";
        }


        if (equipButton == null)
            return;


        if (!owned)
        {
            equipButton.interactable = false;


            if (equipButtonText != null)
                equipButtonText.text =
                    "CHƯA SỞ HỮU";


            return;
        }


        if (equipped)
        {
            equipButton.interactable = false;


            if (equipButtonText != null)
                equipButtonText.text =
                    "ĐANG DÙNG";


            return;
        }


        equipButton.interactable = true;


        if (equipButtonText != null)
            equipButtonText.text =
                "TRANG BỊ";
    }


    // =========================================================
    // EQUIP
    // =========================================================

    public void EquipCurrentVehicle()
    {
        if (!IsVehicleId(currentVehicleId))
            return;


        if (!InventoryOwnership.IsOwned(
                currentVehicleId))
        {
            Debug.LogWarning(
                "[MainMenuGarageUI] " +
                "Không thể equip Vehicle chưa sở hữu: " +
                currentVehicleId,
                this
            );


            currentVehicleId =
                ResolveInitialOwnedVehicleId();


            RefreshVehicle();
            RefreshShowcase();

            return;
        }


        if (equipmentManager == null)
            equipmentManager =
                EquipmentManager.Instance;


        if (equipmentManager == null)
        {
            Debug.LogError(
                "[MainMenuGarageUI] " +
                "Không tìm thấy EquipmentManager.",
                this
            );

            return;
        }


        bool success =
            equipmentManager.TryEquip(
                currentVehicleId
            );


        if (!success)
        {
            Debug.LogWarning(
                "[MainMenuGarageUI] " +
                "Equip thất bại: " +
                currentVehicleId,
                this
            );

            return;
        }


        Debug.Log(
            "[MainMenuGarageUI] " +
            "Equipped Vehicle = " +
            currentVehicleId,
            this
        );


        RefreshVehicle();
        RefreshShowcase();
    }


    // =========================================================
    // EQUIPPED
    // =========================================================

    private int ResolveEquippedVehicleId()
    {
        if (equipmentManager == null)
            equipmentManager =
                EquipmentManager.Instance;


        if (equipmentManager == null)
            return DefaultVehicleId;


        int equippedId =
            equipmentManager.GetEquippedItemId(
                GameItemCategory.Vehicle
            );


        if (!IsVehicleId(equippedId))
            return DefaultVehicleId;


        return equippedId;
    }


    public bool IsCurrentVehicleEquipped()
    {
        if (equipmentManager == null)
            equipmentManager =
                EquipmentManager.Instance;


        if (equipmentManager == null)
            return false;


        return
            equipmentManager.GetEquippedItemId(
                GameItemCategory.Vehicle
            ) ==
            currentVehicleId;
    }


    // =========================================================
    // PUBLIC API
    // =========================================================

    public int GetCurrentVehicleId()
    {
        return currentVehicleId;
    }


    public string GetCurrentVehicleName()
    {
        if (TryGetVehicle(
                currentVehicleId,
                out GameItemData item))
        {
            return item.ItemName;
        }


        return string.Empty;
    }


    public bool HasCurrentVehiclePrefab()
    {
        return
            TryGetVehicle(
                currentVehicleId,
                out GameItemData item) &&

            visualDatabase != null &&

            visualDatabase.TryGet(
                currentVehicleId,
                out GameItemVisual visual) &&

            visual != null &&

            visual.Prefab != null;
    }


    // =========================================================
    // VALIDATION
    // =========================================================

    private static bool IsVehicleId(
        int itemId)
    {
        return
            itemId >= VehicleStartId &&
            itemId <= VehicleEndId;
    }
}