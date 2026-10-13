using System;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-90)]
public sealed class MainMenuCharacterShowcase : MonoBehaviour
{
    // =========================================================
    // CONSTANTS
    // =========================================================

    private const int DefaultVehicleId = 1;

    private const int VehicleStartId = 1;
    private const int VehicleEndId = 40;

    private const int DefaultOutfitId = 41;

    private const int OutfitStartId = 41;
    private const int OutfitEndId = 60;

    // ---------------------------------------------------------
    // RUNTIME INSTANCE PREFIX
    // ---------------------------------------------------------

    private const string VehicleInstancePrefix =
        "ShowcaseVehicle_";

    private const string OutfitInstancePrefix =
        "ShowcaseOutfit_";


    // =========================================================
    // DATABASE
    // =========================================================

    [Header("Database")]
    [SerializeField]
    private GameItemDatabase itemDatabase;

    [SerializeField]
    private GameItemVisualDatabase visualDatabase;


    // =========================================================
    // VISUAL ROOTS
    // =========================================================

    [Header("Showcase Visual Roots")]
    [SerializeField]
    private Transform vehicleVisualRoot;

    [SerializeField]
    private Transform outfitVisualRoot;


    // =========================================================
    // LEGACY
    // =========================================================

    [Header("Legacy Visual Roots")]
    [SerializeField]
    private GameObject[] legacyVisualRoots;


    // =========================================================
    // RUNTIME INSTANCES
    // =========================================================

    private GameObject currentVehicleInstance;

    private GameObject currentOutfitInstance;

    private int currentVehicleId = -1;

    private int currentOutfitId = -1;


    // =========================================================
    // OBSERVED EQUIPMENT STATE
    // =========================================================

    private int lastObservedVehicleId = -1;

    private int lastObservedOutfitId = -1;

    private bool equipmentStateInitialized;


    // =========================================================
    // LIFECYCLE
    // =========================================================

    private bool initialized;

    private bool subscribed;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        Initialize();
    }


    private void Start()
    {
        if (!initialized)
        {
            return;
        }

        TryBindEquipmentManager();

        TrySyncFromEquipmentManager();
    }


    private void OnEnable()
    {
        if (!initialized)
        {
            return;
        }

        TryBindEquipmentManager();

        TrySyncFromEquipmentManager();
    }


    private void OnDisable()
    {
        UnsubscribeEquipmentEvents();
    }


    private void OnDestroy()
    {
        UnsubscribeEquipmentEvents();

        ClearVehicleVisual();
        ClearOutfitVisual();
    }


    // =========================================================
    // INITIALIZATION
    // =========================================================

    private void Initialize()
    {
        if (initialized)
        {
            return;
        }


        // -----------------------------------------------------
        // ITEM DATABASE
        // -----------------------------------------------------

        if (itemDatabase == null)
        {
            Debug.LogError(
                "[MainMenuCharacterShowcase] " +
                "ItemDatabase chưa được gán.",
                this
            );

            return;
        }

        itemDatabase.Initialize();


        // -----------------------------------------------------
        // VISUAL DATABASE
        // -----------------------------------------------------

        if (visualDatabase == null)
        {
            Debug.LogError(
                "[MainMenuCharacterShowcase] " +
                "GameItemVisualDatabase chưa được gán.",
                this
            );

            return;
        }

        visualDatabase.Initialize();


        // -----------------------------------------------------
        // VEHICLE ROOT
        // -----------------------------------------------------

        if (vehicleVisualRoot == null)
        {
            Debug.LogError(
                "[MainMenuCharacterShowcase] " +
                "VehicleVisualRoot chưa được gán.",
                this
            );

            return;
        }


        // -----------------------------------------------------
        // OUTFIT ROOT
        // -----------------------------------------------------

        if (outfitVisualRoot == null)
        {
            Debug.LogError(
                "[MainMenuCharacterShowcase] " +
                "OutfitVisualRoot chưa được gán.",
                this
            );

            return;
        }


        // -----------------------------------------------------
        // ROOT SAFETY
        // -----------------------------------------------------

        if (vehicleVisualRoot == transform)
        {
            Debug.LogError(
                "[MainMenuCharacterShowcase] " +
                "VehicleVisualRoot không được là Showcase root.",
                this
            );

            return;
        }


        if (outfitVisualRoot == transform)
        {
            Debug.LogError(
                "[MainMenuCharacterShowcase] " +
                "OutfitVisualRoot không được là Showcase root.",
                this
            );

            return;
        }


        // -----------------------------------------------------
        // GARAGE ROOT GUARD
        // -----------------------------------------------------

        if (IsGarageRoot(vehicleVisualRoot))
        {
            Debug.LogError(
                "[MainMenuCharacterShowcase] " +
                "VehicleVisualRoot đang trỏ vào GarageShowcaseWorld " +
                "hoặc GaragePanel. " +
                "MainMenu Showcase phải có root riêng.",
                this
            );

            return;
        }


        if (IsGarageRoot(outfitVisualRoot))
        {
            Debug.LogError(
                "[MainMenuCharacterShowcase] " +
                "OutfitVisualRoot đang trỏ vào GarageShowcaseWorld " +
                "hoặc GaragePanel. " +
                "MainMenu Showcase phải có root riêng.",
                this
            );

            return;
        }


        // -----------------------------------------------------
        // LEGACY
        // -----------------------------------------------------

        DisableLegacyVisualRoots();


        // -----------------------------------------------------
        // CLEAR RUNTIME ROOTS
        // -----------------------------------------------------

        ClearRuntimeRoot(vehicleVisualRoot);
        ClearRuntimeRoot(outfitVisualRoot);


        // -----------------------------------------------------
        // RESET STATE
        // -----------------------------------------------------

        currentVehicleInstance = null;
        currentOutfitInstance = null;

        currentVehicleId = -1;
        currentOutfitId = -1;

        lastObservedVehicleId = -1;
        lastObservedOutfitId = -1;

        equipmentStateInitialized = false;


        // -----------------------------------------------------
        // READY
        // -----------------------------------------------------

        initialized = true;


        Debug.Log(
            "[MainMenuCharacterShowcase] Initialized.",
            this
        );
    }


    // =========================================================
    // EQUIPMENT MANAGER BINDING
    // =========================================================

    private void TryBindEquipmentManager()
    {
        EquipmentManager equipmentManager =
            EquipmentManager.Instance;


        if (equipmentManager == null)
        {
            return;
        }


        if (subscribed)
        {
            return;
        }


        equipmentManager.OnEquipmentChanged -=
            HandleEquipmentChanged;

        equipmentManager.OnEquipmentChanged +=
            HandleEquipmentChanged;


        subscribed = true;


        Debug.Log(
            "[MainMenuCharacterShowcase] " +
            "Bound EquipmentManager.",
            this
        );
    }


    private void UnsubscribeEquipmentEvents()
    {
        if (!subscribed)
        {
            return;
        }


        EquipmentManager equipmentManager =
            EquipmentManager.Instance;


        if (equipmentManager != null)
        {
            equipmentManager.OnEquipmentChanged -=
                HandleEquipmentChanged;
        }


        subscribed = false;
    }


    // =========================================================
    // EQUIPMENT CHANGED
    // =========================================================

    private void HandleEquipmentChanged(
        GameItemCategory category
    )
    {
        if (!initialized)
        {
            return;
        }


        EquipmentManager equipmentManager =
            EquipmentManager.Instance;


        if (equipmentManager == null)
        {
            return;
        }


        if (!equipmentManager.IsInitialized)
        {
            return;
        }


        if (category == GameItemCategory.Vehicle)
        {
            SyncVehicleState(
                equipmentManager
            );

            return;
        }


        if (category == GameItemCategory.Outfit)
        {
            SyncOutfitState(
                equipmentManager
            );

            return;
        }
    }


    // =========================================================
    // INITIAL STATE SYNC
    // =========================================================

    private void TrySyncFromEquipmentManager()
    {
        EquipmentManager equipmentManager =
            EquipmentManager.Instance;


        if (equipmentManager == null)
        {
            return;
        }


        if (!equipmentManager.IsInitialized)
        {
            return;
        }


        SyncEquipmentState(
            equipmentManager
        );
    }


    // =========================================================
    // FULL EQUIPMENT SYNC
    // =========================================================

    private void SyncEquipmentState(
        EquipmentManager equipmentManager
    )
    {
        if (equipmentManager == null)
        {
            return;
        }


        if (!equipmentManager.IsInitialized)
        {
            return;
        }


        SyncVehicleState(
            equipmentManager
        );


        SyncOutfitState(
            equipmentManager
        );


        equipmentStateInitialized = true;


        Debug.Log(
            "[MainMenuCharacterShowcase] " +
            "Equipment State Synced → " +
            $"Vehicle={equipmentManager.GetEquippedItemId(GameItemCategory.Vehicle)} | " +
            $"Outfit={equipmentManager.GetEquippedItemId(GameItemCategory.Outfit)} | " +
            $"Accessory={equipmentManager.GetEquippedItemId(GameItemCategory.Accessory)} | " +
            $"Cosmetic={equipmentManager.GetEquippedItemId(GameItemCategory.Cosmetic)}",
            this
        );
    }


    // =========================================================
    // VEHICLE STATE
    // =========================================================

    private void SyncVehicleState(
        EquipmentManager equipmentManager
    )
    {
        if (equipmentManager == null)
        {
            return;
        }


        if (!equipmentManager.IsInitialized)
        {
            return;
        }


        int vehicleId =
            equipmentManager.GetEquippedItemId(
                GameItemCategory.Vehicle
            );


        if (!IsValidVehicleId(vehicleId))
        {
            vehicleId = DefaultVehicleId;
        }


        if (
            lastObservedVehicleId == vehicleId &&
            currentVehicleInstance != null &&
            currentVehicleId == vehicleId
        )
        {
            currentVehicleInstance.SetActive(true);

            return;
        }


        lastObservedVehicleId = vehicleId;


        Debug.Log(
            "[MainMenuCharacterShowcase] " +
            $"Sync Vehicle → ID={vehicleId}",
            this
        );


        ApplyVehicleById(
            vehicleId
        );
    }


    // =========================================================
    // OUTFIT STATE
    // =========================================================

    private void SyncOutfitState(
        EquipmentManager equipmentManager
    )
    {
        if (equipmentManager == null)
        {
            return;
        }


        if (!equipmentManager.IsInitialized)
        {
            return;
        }


        int outfitId =
            equipmentManager.GetEquippedItemId(
                GameItemCategory.Outfit
            );


        if (!IsValidOutfitId(outfitId))
        {
            outfitId = DefaultOutfitId;
        }


        if (
            lastObservedOutfitId == outfitId &&
            currentOutfitInstance != null &&
            currentOutfitId == outfitId
        )
        {
            currentOutfitInstance.SetActive(true);

            return;
        }


        lastObservedOutfitId = outfitId;


        Debug.Log(
            "[MainMenuCharacterShowcase] " +
            $"Sync Outfit → ID={outfitId}",
            this
        );


        ApplyOutfitById(
            outfitId
        );
    }


    // =========================================================
    // PUBLIC REFRESH
    // =========================================================

    public void RefreshShowcase()
    {
        if (!initialized)
        {
            Initialize();
        }


        if (!initialized)
        {
            return;
        }


        TryBindEquipmentManager();


        EquipmentManager equipmentManager =
            EquipmentManager.Instance;


        if (equipmentManager == null)
        {
            Debug.LogWarning(
                "[MainMenuCharacterShowcase] " +
                "EquipmentManager chưa tồn tại.",
                this
            );

            return;
        }


        if (!equipmentManager.IsInitialized)
        {
            Debug.LogWarning(
                "[MainMenuCharacterShowcase] " +
                "EquipmentManager chưa initialized.",
                this
            );

            return;
        }


        equipmentStateInitialized = false;


        SyncEquipmentState(
            equipmentManager
        );
    }


    // =========================================================
    // PUBLIC VEHICLE API
    // =========================================================

    public void ApplyEquippedVehicle()
    {
        if (!initialized)
        {
            Initialize();
        }


        if (!initialized)
        {
            return;
        }


        EquipmentManager equipmentManager =
            EquipmentManager.Instance;


        if (equipmentManager == null)
        {
            return;
        }


        if (!equipmentManager.IsInitialized)
        {
            return;
        }


        int vehicleId =
            equipmentManager.GetEquippedItemId(
                GameItemCategory.Vehicle
            );


        if (!IsValidVehicleId(vehicleId))
        {
            vehicleId = DefaultVehicleId;
        }


        ApplyVehicleById(
            vehicleId
        );
    }


    // =========================================================
    // APPLY VEHICLE
    // =========================================================

    private void ApplyVehicleById(
        int vehicleId
    )
    {
        if (!initialized)
        {
            return;
        }


        if (!IsValidVehicleId(vehicleId))
        {
            vehicleId = DefaultVehicleId;
        }


        if (
            currentVehicleInstance != null &&
            currentVehicleId == vehicleId
        )
        {
            currentVehicleInstance.SetActive(true);

            lastObservedVehicleId = vehicleId;

            return;
        }


        if (!TryGetVehicle(
                vehicleId,
                out GameItemData item
            ))
        {
            Debug.LogWarning(
                "[MainMenuCharacterShowcase] " +
                $"Không tìm thấy Vehicle ID={vehicleId} " +
                "trong ItemDatabase/VisualDatabase.",
                this
            );

            return;
        }


        ReplaceVehicle(
            vehicleId,
            item
        );
    }


    // =========================================================
    // GET VEHICLE
    // =========================================================

    private bool TryGetVehicle(
        int vehicleId,
        out GameItemData item
    )
    {
        item = null;


        if (itemDatabase == null)
        {
            return false;
        }


        if (!IsValidVehicleId(vehicleId))
        {
            return false;
        }


        if (!itemDatabase.TryGet(
                vehicleId,
                out item
            ))
        {
            return false;
        }


        if (item == null)
        {
            return false;
        }


        if (
            item.Category !=
            GameItemCategory.Vehicle
        )
        {
            item = null;

            return false;
        }


        if (visualDatabase == null)
        {
            item = null;

            return false;
        }


        if (!visualDatabase.TryGet(
                vehicleId,
                out GameItemVisual visual
            ))
        {
            item = null;

            return false;
        }


        if (
            visual == null ||
            visual.Prefab == null
        )
        {
            item = null;

            return false;
        }


        return true;
    }


    // =========================================================
    // REPLACE VEHICLE
    // =========================================================

    private void ReplaceVehicle(
        int vehicleId,
        GameItemData item
    )
    {
        if (item == null)
        {
            return;
        }


        if (vehicleVisualRoot == null)
        {
            return;
        }


        if (
            visualDatabase == null ||
            !visualDatabase.TryGet(
                vehicleId,
                out GameItemVisual visual
            ) ||
            visual == null ||
            visual.Prefab == null
        )
        {
            Debug.LogWarning(
                "[MainMenuCharacterShowcase] " +
                $"Không có visual prefab cho Vehicle ID={vehicleId}.",
                this
            );

            return;
        }


        // -----------------------------------------------------
        // REMOVE ALL PREVIOUS SHOWCASE VEHICLES
        // -----------------------------------------------------

        ClearGeneratedChildren(
            vehicleVisualRoot,
            VehicleInstancePrefix
        );


        currentVehicleInstance = null;
        currentVehicleId = -1;


        // -----------------------------------------------------
        // CREATE NEW
        // -----------------------------------------------------

        GameObject newInstance =
            Instantiate(
                visual.Prefab,
                vehicleVisualRoot
            );


        if (newInstance == null)
        {
            return;
        }


        newInstance.name =
            VehicleInstancePrefix +
            vehicleId;


        ApplyVisualTransform(
            visual,
            newInstance.transform
        );


        SanitizeVisualOnly(
            newInstance
        );


        newInstance.SetActive(true);


        // -----------------------------------------------------
        // SET CURRENT
        // -----------------------------------------------------

        currentVehicleInstance =
            newInstance;

        currentVehicleId =
            vehicleId;

        lastObservedVehicleId =
            vehicleId;


        Debug.Log(
            "[MainMenuCharacterShowcase] " +
            $"Vehicle Showcase → ID={vehicleId}",
            this
        );
    }


    // =========================================================
    // PUBLIC OUTFIT API
    // =========================================================

    public void ApplyEquippedOutfit()
    {
        if (!initialized)
        {
            Initialize();
        }


        if (!initialized)
        {
            return;
        }


        EquipmentManager equipmentManager =
            EquipmentManager.Instance;


        if (equipmentManager == null)
        {
            return;
        }


        if (!equipmentManager.IsInitialized)
        {
            return;
        }


        int outfitId =
            equipmentManager.GetEquippedItemId(
                GameItemCategory.Outfit
            );


        if (!IsValidOutfitId(outfitId))
        {
            outfitId = DefaultOutfitId;
        }


        ApplyOutfitById(
            outfitId
        );
    }


    // =========================================================
    // APPLY OUTFIT
    // =========================================================

    private void ApplyOutfitById(
        int outfitId
    )
    {
        if (!initialized)
        {
            return;
        }


        if (!IsValidOutfitId(outfitId))
        {
            outfitId = DefaultOutfitId;
        }


        if (
            currentOutfitInstance != null &&
            currentOutfitId == outfitId
        )
        {
            currentOutfitInstance.SetActive(true);

            lastObservedOutfitId = outfitId;

            return;
        }


        Debug.Log(
            "[MainMenuCharacterShowcase] " +
            $"Apply Outfit → ID={outfitId}",
            this
        );


        if (!TryGetOutfit(
                outfitId,
                out GameItemData item
            ))
        {
            /*
             * Quan trọng:
             *
             * Nếu visual mới không tồn tại,
             * KHÔNG được giữ prefab outfit cũ.
             *
             * Character phải trở về trạng thái
             * OutfitRoot sạch.
             */
            ClearOutfitVisual();

            Debug.LogWarning(
                "[MainMenuCharacterShowcase] " +
                $"Không tìm thấy Outfit ID={outfitId} " +
                "trong ItemDatabase/VisualDatabase. " +
                "Outfit cũ đã được clear.",
                this
            );

            return;
        }


        ReplaceOutfit(
            outfitId,
            item
        );
    }


    // =========================================================
    // GET OUTFIT
    // =========================================================

    private bool TryGetOutfit(
        int outfitId,
        out GameItemData item
    )
    {
        item = null;


        if (itemDatabase == null)
        {
            return false;
        }


        if (!IsValidOutfitId(outfitId))
        {
            return false;
        }


        if (!itemDatabase.TryGet(
                outfitId,
                out item
            ))
        {
            return false;
        }


        if (item == null)
        {
            return false;
        }


        if (
            item.Category !=
            GameItemCategory.Outfit
        )
        {
            item = null;

            return false;
        }


        if (visualDatabase == null)
        {
            item = null;

            return false;
        }


        if (!visualDatabase.TryGet(
                outfitId,
                out GameItemVisual visual
            ))
        {
            item = null;

            return false;
        }


        if (
            visual == null ||
            visual.Prefab == null
        )
        {
            item = null;

            return false;
        }


        return true;
    }


    // =========================================================
    // REPLACE OUTFIT
    // =========================================================

    private void ReplaceOutfit(
        int outfitId,
        GameItemData item
    )
    {
        if (item == null)
        {
            return;
        }


        if (outfitVisualRoot == null)
        {
            return;
        }


        if (
            visualDatabase == null ||
            !visualDatabase.TryGet(
                outfitId,
                out GameItemVisual visual
            ) ||
            visual == null ||
            visual.Prefab == null
        )
        {
            /*
             * Không giữ outfit cũ khi visual mới invalid.
             */
            ClearOutfitVisual();

            Debug.LogWarning(
                "[MainMenuCharacterShowcase] " +
                $"Không có visual prefab cho Outfit ID={outfitId}. " +
                "Outfit cũ đã được clear.",
                this
            );

            return;
        }


        // -----------------------------------------------------
        // HARD CLEANUP
        // -----------------------------------------------------

        /*
         * Không chỉ Destroy currentOutfitInstance.
         *
         * Xóa mọi object có prefix ShowcaseOutfit_
         * bên dưới OutfitVisualRoot.
         *
         * Điều này bảo vệ khỏi stale instance/reference.
         */
        ClearGeneratedChildren(
            outfitVisualRoot,
            OutfitInstancePrefix
        );


        currentOutfitInstance = null;
        currentOutfitId = -1;


        // -----------------------------------------------------
        // CREATE NEW
        // -----------------------------------------------------

        GameObject newInstance =
            Instantiate(
                visual.Prefab,
                outfitVisualRoot
            );


        if (newInstance == null)
        {
            return;
        }


        newInstance.name =
            OutfitInstancePrefix +
            outfitId;


        ApplyVisualTransform(
            visual,
            newInstance.transform
        );


        SanitizeVisualOnly(
            newInstance
        );


        newInstance.SetActive(true);


        // -----------------------------------------------------
        // SET CURRENT
        // -----------------------------------------------------

        currentOutfitInstance =
            newInstance;

        currentOutfitId =
            outfitId;

        lastObservedOutfitId =
            outfitId;


        Debug.Log(
            "[MainMenuCharacterShowcase] " +
            $"Outfit Showcase → ID={outfitId}",
            this
        );
    }


    // =========================================================
    // VISUAL TRANSFORM
    // =========================================================

    private static void ApplyVisualTransform(
        GameItemVisual visual,
        Transform target
    )
    {
        if (
            visual == null ||
            target == null
        )
        {
            return;
        }


        target.localPosition =
            visual.LocalPosition;


        target.localRotation =
            visual.LocalRotation;


        target.localScale =
            visual.LocalScale;
    }


    // =========================================================
    // VISUAL SANITIZER
    // =========================================================

    private static void SanitizeVisualOnly(
        GameObject visual
    )
    {
        if (visual == null)
        {
            return;
        }


        Rigidbody[] rigidbodies =
            visual.GetComponentsInChildren<Rigidbody>(
                true
            );


        for (
            int i = 0;
            i < rigidbodies.Length;
            i++
        )
        {
            Rigidbody rb =
                rigidbodies[i];


            if (rb == null)
            {
                continue;
            }


            rb.isKinematic = true;
            rb.detectCollisions = false;
            rb.useGravity = false;
        }


        Collider[] colliders =
            visual.GetComponentsInChildren<Collider>(
                true
            );


        for (
            int i = 0;
            i < colliders.Length;
            i++
        )
        {
            Collider collider =
                colliders[i];


            if (collider != null)
            {
                collider.enabled = false;
            }
        }


        CharacterController[] controllers =
            visual.GetComponentsInChildren<
                CharacterController
            >(true);


        for (
            int i = 0;
            i < controllers.Length;
            i++
        )
        {
            CharacterController controller =
                controllers[i];


            if (controller != null)
            {
                controller.enabled = false;
            }
        }
    }


    // =========================================================
    // CLEAR GENERATED CHILDREN
    // =========================================================

    private static void ClearGeneratedChildren(
        Transform root,
        string prefix
    )
    {
        if (root == null)
        {
            return;
        }


        int childCount =
            root.childCount;


        for (
            int i = childCount - 1;
            i >= 0;
            i--
        )
        {
            Transform child =
                root.GetChild(i);


            if (child == null)
            {
                continue;
            }


            if (!child.name.StartsWith(
                    prefix,
                    StringComparison.Ordinal
                ))
            {
                continue;
            }


            Destroy(
                child.gameObject
            );
        }
    }


    // =========================================================
    // LEGACY ROOTS
    // =========================================================

    private void DisableLegacyVisualRoots()
    {
        if (legacyVisualRoots == null)
        {
            return;
        }


        int count =
            legacyVisualRoots.Length;


        for (
            int i = 0;
            i < count;
            i++
        )
        {
            GameObject root =
                legacyVisualRoots[i];


            if (root == null)
            {
                continue;
            }


            if (
                vehicleVisualRoot != null &&
                root == vehicleVisualRoot.gameObject
            )
            {
                continue;
            }


            if (
                outfitVisualRoot != null &&
                root == outfitVisualRoot.gameObject
            )
            {
                continue;
            }


            root.SetActive(false);
        }
    }


    // =========================================================
    // CLEAR RUNTIME ROOT
    // =========================================================

    private static void ClearRuntimeRoot(
        Transform root
    )
    {
        if (root == null)
        {
            return;
        }


        int childCount =
            root.childCount;


        for (
            int i = childCount - 1;
            i >= 0;
            i--
        )
        {
            Transform child =
                root.GetChild(i);


            if (child == null)
            {
                continue;
            }


            Destroy(
                child.gameObject
            );
        }
    }


    // =========================================================
    // GARAGE ROOT GUARD
    // =========================================================

    private static bool IsGarageRoot(
        Transform root
    )
    {
        if (root == null)
        {
            return false;
        }


        Transform current =
            root;


        while (current != null)
        {
            if (
                current.name ==
                "GarageShowcaseWorld" ||
                current.name ==
                "GaragePanel"
            )
            {
                return true;
            }


            if (
                current.GetComponent<
                    GarageShowcaseController
                >() != null
            )
            {
                return true;
            }


            current =
                current.parent;
        }


        return false;
    }


    // =========================================================
    // CLEAR VEHICLE
    // =========================================================

    public void ClearVehicleVisual()
    {
        if (vehicleVisualRoot != null)
        {
            ClearGeneratedChildren(
                vehicleVisualRoot,
                VehicleInstancePrefix
            );
        }


        if (currentVehicleInstance != null)
        {
            Destroy(
                currentVehicleInstance
            );
        }


        currentVehicleInstance = null;

        currentVehicleId = -1;

        lastObservedVehicleId = -1;
    }


    // =========================================================
    // CLEAR OUTFIT
    // =========================================================

    public void ClearOutfitVisual()
    {
        if (outfitVisualRoot != null)
        {
            ClearGeneratedChildren(
                outfitVisualRoot,
                OutfitInstancePrefix
            );
        }


        if (currentOutfitInstance != null)
        {
            Destroy(
                currentOutfitInstance
            );
        }


        currentOutfitInstance = null;

        currentOutfitId = -1;

        lastObservedOutfitId = -1;
    }


    // =========================================================
    // VALIDATION
    // =========================================================

    private static bool IsValidVehicleId(
        int vehicleId
    )
    {
        return
            vehicleId >= VehicleStartId &&
            vehicleId <= VehicleEndId;
    }


    private static bool IsValidOutfitId(
        int outfitId
    )
    {
        return
            outfitId >= OutfitStartId &&
            outfitId <= OutfitEndId;
    }


    // =========================================================
    // PUBLIC STATE
    // =========================================================

    public int CurrentVehicleId =>
        currentVehicleId;


    public int CurrentOutfitId =>
        currentOutfitId;


    public GameObject CurrentVehicleInstance =>
        currentVehicleInstance;


    public GameObject CurrentOutfitInstance =>
        currentOutfitInstance;


    public bool IsInitialized =>
        initialized;


    public bool IsEquipmentStateInitialized =>
        equipmentStateInitialized;
}