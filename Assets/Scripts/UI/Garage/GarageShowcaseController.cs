using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class GarageShowcaseController : MonoBehaviour
{
    // =========================================================
    // DATABASE CONTRACT
    // =========================================================

    private const int DefaultVehicleId = 1;
    private const int MinVehicleId = 1;
    private const int MaxVehicleId = 40;


    // =========================================================
    // DATA
    // =========================================================

    [Header("Data")]
    [SerializeField] private GameItemDatabase itemDatabase;
    [SerializeField] private GameItemVisualDatabase visualDatabase;
    [SerializeField] private EquipmentManager equipmentManager;


    // =========================================================
    // SHOWCASE WORLD
    // =========================================================

    [Header("Showcase World")]
    [SerializeField] private Transform vehiclePivot;
    [SerializeField] private Camera showcaseCamera;
    [SerializeField] private RenderTexture showcaseTexture;
    [SerializeField] private int showcaseLayer = -1;


    // =========================================================
    // UI
    // =========================================================

    [Header("UI")]
    [SerializeField] private RawImage showcaseView;


    // =========================================================
    // AUTO ROTATION
    // =========================================================

    [Header("Auto Rotation")]
    [SerializeField] private bool autoRotate = true;

    [SerializeField]
    [Min(0f)]
    private float rotationSpeed = 18f;


    // =========================================================
    // MANUAL ROTATION
    // =========================================================

    [Header("Manual Rotation")]
    [SerializeField]
    [Min(0f)]
    private float manualRotationSpeed = 0.45f;


    // =========================================================
    // ROTATION CENTER
    // =========================================================

    [Header("Rotation Center")]
    [Tooltip(
        "Tự động tạo pivot tại tâm hình học của toàn bộ Renderer trong prefab."
    )]
    [SerializeField]
    private bool centerVehicleForRotation = true;

    [Tooltip(
        "Offset bổ sung sau khi tự động căn tâm. " +
        "Dùng để chỉnh tâm xoay thủ công nếu cần."
    )]
    [SerializeField]
    private Vector3 rotationCenterOffset = Vector3.zero;

    [Tooltip(
        "Nếu bật, tâm X/Y/Z của vehicle sẽ được căn về đúng tâm Bounds."
    )]
    [SerializeField]
    private bool centerX = true;

    [SerializeField]
    private bool centerY = true;

    [SerializeField]
    private bool centerZ = true;


    // =========================================================
    // CAMERA
    // =========================================================

    [Header("Camera")]
    [SerializeField]
    [Range(10f, 80f)]
    private float cameraFieldOfView = 35f;

    [SerializeField]
    [Min(0.1f)]
    private float cameraDistanceMultiplier = 1.25f;

    [SerializeField]
    [Min(0f)]
    private float cameraHeightMultiplier = 0.12f;


    // =========================================================
    // ZOOM
    // =========================================================

    [Header("Zoom")]
    [SerializeField]
    private float zoomStep = 0.12f;

    [SerializeField]
    private float minZoom = 0.72f;

    [SerializeField]
    private float maxZoom = 1.35f;

    [SerializeField]
    [Min(0.01f)]
    private float zoomSmoothTime = 0.12f;


    // =========================================================
    // VEHICLE FIT
    // =========================================================

    [Header("Vehicle Fit")]
    [SerializeField]
    [Min(1f)]
    private float fitPadding = 1.20f;


    // =========================================================
    // RUNTIME
    // =========================================================

    private GameObject currentVehicle;
    private int currentVehicleId = -1;

    private Transform vehicleRotationPivot;

    private float baseCameraDistance = 3f;
    private float cameraLookHeight = 0.2f;

    private float targetZoom = 1f;
    private float currentZoom = 1f;
    private float zoomVelocity;

    private bool initialized;
    private bool isShowcaseActive;
    private bool isDragging;

    private float lastMouseX;

    private bool equipmentSubscribed;


    // =========================================================
    // PUBLIC
    // =========================================================

    public int CurrentVehicleId => currentVehicleId;

    public GameObject CurrentVehicle => currentVehicle;

    public bool IsShowcaseActive => isShowcaseActive;

    public bool IsAutoRotateEnabled => autoRotate;

    public bool IsDragging => isDragging;

    public Transform VehicleRotationPivot =>
        vehicleRotationPivot;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        Initialize();
    }


    private void OnEnable()
    {
        Initialize();
        SubscribeEquipmentEvents();
    }


    private void Update()
    {
        if (!isShowcaseActive)
            return;

        HandleMouseRotation();
        HandleZoom();

        if (!isDragging && autoRotate)
            AutoRotate();

        UpdateCameraSmooth();
    }


    private void OnDisable()
    {
        UnsubscribeEquipmentEvents();

        DisableCamera();
        DestroyCurrentVehicle();

        isShowcaseActive = false;
        isDragging = false;
    }


    private void OnDestroy()
    {
        UnsubscribeEquipmentEvents();
    }


    // =========================================================
    // INITIALIZATION
    // =========================================================

    private void Initialize()
    {
        if (initialized)
            return;

        ResolveReferences();

        if (itemDatabase == null)
        {
            Debug.LogError(
                "[GarageShowcaseController] " +
                "GameItemDatabase chưa được gán.",
                this
            );

            return;
        }

        if (visualDatabase == null)
        {
            Debug.LogError(
                "[GarageShowcaseController] " +
                "GameItemVisualDatabase chưa được gán.",
                this
            );

            return;
        }

        itemDatabase.Initialize();
        visualDatabase.Initialize();

        ConfigureLayer();
        ConfigureCamera();
        ConfigureRenderTexture();

        initialized = true;

        DisableCamera();

        isShowcaseActive = false;
    }


    private void ResolveReferences()
    {
        if (equipmentManager == null)
            equipmentManager = EquipmentManager.Instance;

        if (vehiclePivot == null)
        {
            Transform child =
                transform.Find("VehiclePivot");

            if (child != null)
                vehiclePivot = child;
        }

        if (showcaseCamera == null)
        {
            showcaseCamera =
                GetComponentInChildren<Camera>(true);
        }
    }


    // =========================================================
    // EQUIPMENT EVENT
    // =========================================================

    private void SubscribeEquipmentEvents()
    {
        if (equipmentSubscribed)
            return;

        if (equipmentManager == null)
            equipmentManager = EquipmentManager.Instance;

        if (equipmentManager == null)
            return;

        equipmentManager.OnEquipmentChanged +=
            HandleEquipmentChanged;

        equipmentSubscribed = true;
    }


    private void UnsubscribeEquipmentEvents()
    {
        if (!equipmentSubscribed)
            return;

        if (equipmentManager != null)
        {
            equipmentManager.OnEquipmentChanged -=
                HandleEquipmentChanged;
        }

        equipmentSubscribed = false;
    }


    private void HandleEquipmentChanged(
        GameItemCategory category)
    {
        if (category != GameItemCategory.Vehicle)
            return;

        if (!isShowcaseActive)
            return;

        RefreshEquippedVehicle();
    }


    // =========================================================
    // LAYER
    // =========================================================

    private void ConfigureLayer()
    {
        if (showcaseLayer < 0)
        {
            showcaseLayer =
                LayerMask.NameToLayer(
                    "GarageShowcase"
                );
        }

        if (showcaseLayer < 0)
        {
            Debug.LogError(
                "[GarageShowcaseController] " +
                "Không tìm thấy layer GarageShowcase.",
                this
            );

            return;
        }

        SetLayerRecursively(
            gameObject,
            showcaseLayer
        );

        if (showcaseCamera != null)
        {
            showcaseCamera.gameObject.layer =
                showcaseLayer;
        }

        if (vehiclePivot != null)
        {
            vehiclePivot.gameObject.layer =
                showcaseLayer;
        }
    }


    // =========================================================
    // CAMERA
    // =========================================================

    private void ConfigureCamera()
    {
        if (showcaseCamera == null)
            return;

        showcaseCamera.enabled = false;

        showcaseCamera.fieldOfView =
            cameraFieldOfView;

        showcaseCamera.nearClipPlane = 0.01f;

        showcaseCamera.farClipPlane = 100f;

        showcaseCamera.clearFlags =
            CameraClearFlags.SolidColor;

        showcaseCamera.backgroundColor =
            new Color(
                0.012f,
                0.018f,
                0.028f,
                1f
            );

        if (showcaseLayer >= 0)
        {
            showcaseCamera.cullingMask =
                1 << showcaseLayer;
        }

        if (showcaseTexture != null)
        {
            showcaseCamera.targetTexture =
                showcaseTexture;
        }
    }


    private void ConfigureRenderTexture()
    {
        if (showcaseView == null)
            return;

        if (showcaseTexture != null)
        {
            showcaseView.texture =
                showcaseTexture;
        }

        showcaseView.raycastTarget = false;
    }


    // =========================================================
    // OPEN
    // =========================================================

    public void OpenShowcase()
    {
        OpenShowcase(
            GetEquippedVehicleId()
        );
    }


    public void OpenShowcase(int vehicleId)
    {
        Initialize();

        if (!initialized)
            return;

        SubscribeEquipmentEvents();

        if (!IsValidVehicleId(vehicleId))
        {
            vehicleId =
                GetEquippedVehicleId();
        }

        if (!IsValidVehicleId(vehicleId))
        {
            vehicleId =
                DefaultVehicleId;
        }

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        isShowcaseActive = true;
        isDragging = false;

        if (showcaseCamera != null)
        {
            showcaseCamera.targetTexture =
                showcaseTexture;

            if (showcaseLayer >= 0)
            {
                showcaseCamera.cullingMask =
                    1 << showcaseLayer;
            }

            showcaseCamera.enabled = true;
        }

        ShowVehicle(vehicleId);

        Debug.Log(
            "[GarageShowcaseController] " +
            "OPEN | Vehicle = " +
            vehicleId,
            this
        );
    }


    // =========================================================
    // CLOSE
    // =========================================================

    public void CloseShowcase()
    {
        isShowcaseActive = false;
        isDragging = false;

        DisableCamera();
        DestroyCurrentVehicle();

        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }

        Debug.Log(
            "[GarageShowcaseController] " +
            "CLOSED + VEHICLE DESTROYED.",
            this
        );
    }


    // =========================================================
    // EQUIPMENT
    // =========================================================

    public void RefreshEquippedVehicle()
    {
        ShowVehicle(
            GetEquippedVehicleId()
        );
    }


    private int GetEquippedVehicleId()
    {
        if (equipmentManager == null)
        {
            equipmentManager =
                EquipmentManager.Instance;
        }

        if (equipmentManager == null)
            return DefaultVehicleId;

        int equippedId =
            equipmentManager.GetEquippedItemId(
                GameItemCategory.Vehicle
            );

        if (!IsValidVehicleId(equippedId))
            return DefaultVehicleId;

        return equippedId;
    }


    // =========================================================
    // SHOW VEHICLE
    // =========================================================

    public void ShowVehicle(int vehicleId)
    {
        Initialize();

        if (!initialized)
            return;

        if (!IsValidVehicleId(vehicleId))
        {
            vehicleId =
                DefaultVehicleId;
        }

        if (
            currentVehicleId == vehicleId &&
            currentVehicle != null
        )
        {
            ResetVehiclePresentation();
            return;
        }

        DestroyCurrentVehicle();

        if (!itemDatabase.TryGet(
                vehicleId,
                out GameItemData item))
        {
            Debug.LogError(
                "[GarageShowcaseController] " +
                "Không tìm thấy Vehicle ID = " +
                vehicleId,
                this
            );

            return;
        }

        if (
            item == null ||
            item.Category !=
            GameItemCategory.Vehicle
        )
        {
            Debug.LogError(
                "[GarageShowcaseController] " +
                "Item " +
                vehicleId +
                " không phải Vehicle.",
                this
            );

            return;
        }

        if (
            visualDatabase == null ||
            !visualDatabase.TryGet(
                vehicleId,
                out GameItemVisual visual
            ) ||
            visual == null
        )
        {
            Debug.LogError(
                "[GarageShowcaseController] " +
                "Vehicle " +
                vehicleId +
                " không có VisualDefinition.",
                this
            );

            return;
        }

        /*
         * Prefab == null:
         *
         * Database có item nhưng model chưa được gán.
         * Đây KHÔNG phải lỗi database.
         */

        if (visual.Prefab == null)
        {
            currentVehicleId = -1;

            Debug.Log(
                "[GarageShowcaseController] " +
                "Vehicle ID " +
                vehicleId +
                " chưa có model Prefab. " +
                "Database/VisualDefinition vẫn hợp lệ.",
                visual
            );

            return;
        }

        if (vehiclePivot == null)
        {
            Debug.LogError(
                "[GarageShowcaseController] " +
                "VehiclePivot == null.",
                this
            );

            return;
        }

        // =====================================================
        // INSTANTIATE
        // =====================================================

        currentVehicle =
            Instantiate(
                visual.Prefab,
                vehiclePivot
            );

        if (currentVehicle == null)
        {
            Debug.LogError(
                "[GarageShowcaseController] " +
                "Instantiate Vehicle thất bại.",
                this
            );

            return;
        }

        currentVehicle.name =
            "GarageVehicle_" +
            vehicleId;

        currentVehicleId =
            vehicleId;

        // =====================================================
        // LAYER
        // =====================================================

        EnsureVehicleLayer(
            currentVehicle
        );

        // =====================================================
        // SANITIZE
        // =====================================================

        SanitizeVehicle(
            currentVehicle
        );

        // =====================================================
        // PRESENTATION
        // =====================================================

        ApplyVisualTransform(
            currentVehicle,
            visual
        );

        // =====================================================
        // CREATE ROTATION PIVOT
        // =====================================================

        CreateVehicleRotationPivot();

        // =====================================================
        // CENTER VEHICLE
        // =====================================================

        CenterVehicleForRotation();

        // =====================================================
        // CAMERA / ZOOM
        // =====================================================

        RecalculateVehiclePresentation();

        Debug.Log(
            "[GarageShowcaseController] " +
            "Vehicle loaded = " +
            vehicleId +
            " | Rotation Center = " +
            centerVehicleForRotation +
            " | Layer = " +
            LayerMask.LayerToName(
                showcaseLayer
            ),
            this
        );
    }


    // =========================================================
    // VISUAL TRANSFORM
    // =========================================================

    private void ApplyVisualTransform(
        GameObject vehicle,
        GameItemVisual visual)
    {
        if (
            vehicle == null ||
            visual == null
        )
            return;

        /*
         * IMPORTANT:
         *
         * Transform trong GameItemVisual
         * vẫn được giữ nguyên.
         *
         * Chúng ta KHÔNG sửa database.
         */

        vehicle.transform.localPosition =
            visual.LocalPosition;

        vehicle.transform.localRotation =
            visual.LocalRotation;

        vehicle.transform.localScale =
            visual.LocalScale;
    }


    // =========================================================
    // ROTATION PIVOT
    // =========================================================

    private void CreateVehicleRotationPivot()
    {
        if (vehiclePivot == null)
            return;

        /*
         * Xóa pivot cũ nếu còn tồn tại.
         */
        if (vehicleRotationPivot != null)
        {
            DestroyImmediateSafe(
                vehicleRotationPivot.gameObject
            );

            vehicleRotationPivot = null;
        }

        /*
         * Tạo pivot mới.
         *
         * VehicleRotationPivot nằm trực tiếp
         * dưới VehiclePivot.
         */
        GameObject pivotObject =
            new GameObject(
                "VehicleRotationPivot"
            );

        vehicleRotationPivot =
            pivotObject.transform;

        vehicleRotationPivot.SetParent(
            vehiclePivot,
            false
        );

        vehicleRotationPivot.localPosition =
            Vector3.zero;

        vehicleRotationPivot.localRotation =
            Quaternion.identity;

        vehicleRotationPivot.localScale =
            Vector3.one;

        /*
         * Đưa vehicle vào rotation pivot.
         *
         * SetParent(worldPositionStays = true)
         * để không phá vị trí/rotation/scale hiện tại.
         */
        currentVehicle.transform.SetParent(
            vehicleRotationPivot,
            true
        );

        EnsureVehicleLayer(
            vehicleRotationPivot.gameObject
        );
    }


    // =========================================================
    // CENTER VEHICLE
    // =========================================================

    private void CenterVehicleForRotation()
    {
        if (
            !centerVehicleForRotation ||
            currentVehicle == null ||
            vehicleRotationPivot == null
        )
            return;

        if (
            !TryGetVehicleBounds(
                currentVehicle,
                out Bounds bounds
            )
        )
        {
            return;
        }

        /*
         * Bounds.center đang ở WORLD SPACE.
         *
         * Chuyển center về LOCAL SPACE
         * của VehicleRotationPivot.
         */
        Vector3 worldCenter =
            bounds.center;

        Vector3 localCenter =
            vehicleRotationPivot
                .InverseTransformPoint(
                    worldCenter
                );

        /*
         * Chỉ center những axis được bật.
         */
        if (!centerX)
            localCenter.x = 0f;

        if (!centerY)
            localCenter.y = 0f;

        if (!centerZ)
            localCenter.z = 0f;

        /*
         * Offset chỉnh tâm thủ công.
         */
        localCenter +=
            rotationCenterOffset;

        /*
         * Di chuyển MODEL ngược lại center.
         *
         * Nếu model center đang ở:
         *
         *     (0.8, 0.2, -0.4)
         *
         * thì model được đưa về:
         *
         *     (-0.8, -0.2, +0.4)
         *
         * để tâm hình học nằm đúng tại pivot.
         */
        Vector3 currentLocalPosition =
            currentVehicle.transform.localPosition;

        currentVehicle.transform.localPosition =
            currentLocalPosition -
            localCenter;

        /*
         * Debug để kiểm tra tâm.
         */
        Debug.Log(
            "[GarageShowcaseController] " +
            "Vehicle centered. " +
            "Bounds Center = " +
            worldCenter +
            " | Local Center = " +
            localCenter,
            currentVehicle
        );
    }


    // =========================================================
    // PRESENTATION RECALCULATION
    // =========================================================

    private void RecalculateVehiclePresentation()
    {
        if (currentVehicle == null)
            return;

        if (
            !TryGetVehicleBounds(
                currentVehicle,
                out Bounds bounds
            )
        )
        {
            baseCameraDistance = 3f;
            cameraLookHeight = 0.2f;
        }
        else
        {
            CalculateCameraFit(
                bounds
            );
        }

        targetZoom = 1f;
        currentZoom = 1f;
        zoomVelocity = 0f;

        UpdateCameraImmediate();
    }


    // =========================================================
    // PRESENTATION RESET
    // =========================================================

    private void ResetVehiclePresentation()
    {
        if (
            vehiclePivot == null ||
            currentVehicle == null
        )
            return;

        /*
         * Reset rotation root.
         */
        vehiclePivot.localRotation =
            Quaternion.identity;

        /*
         * Reset rotation pivot.
         */
        if (vehicleRotationPivot != null)
        {
            vehicleRotationPivot.localRotation =
                Quaternion.identity;
        }

        /*
         * Re-apply Visual Database transform.
         *
         * Vì model đã được center trước đó,
         * ta phải rebuild presentation sạch.
         */
        if (
            visualDatabase != null &&
            visualDatabase.TryGet(
                currentVehicleId,
                out GameItemVisual visual
            ) &&
            visual != null
        )
        {
            /*
             * Lưu trạng thái parent hiện tại.
             */
            if (
                vehicleRotationPivot == null
            )
            {
                currentVehicle.transform.localPosition =
                    visual.LocalPosition;

                currentVehicle.transform.localRotation =
                    visual.LocalRotation;

                currentVehicle.transform.localScale =
                    visual.LocalScale;
            }
        }

        /*
         * Rebuild lại center từ bounds hiện tại.
         */
        if (
            vehicleRotationPivot != null
        )
        {
            CenterVehicleForRotation();
        }

        RecalculateVehiclePresentation();
    }


    // =========================================================
    // BOUNDS
    // =========================================================

    private bool TryGetVehicleBounds(
        GameObject vehicle,
        out Bounds bounds)
    {
        bounds = new Bounds();

        if (vehicle == null)
            return false;

        Renderer[] renderers =
            vehicle.GetComponentsInChildren<
                Renderer
            >(true);

        if (
            renderers == null ||
            renderers.Length == 0
        )
            return false;

        bool hasBounds = false;

        for (
            int i = 0;
            i < renderers.Length;
            i++
        )
        {
            Renderer renderer =
                renderers[i];

            if (renderer == null)
                continue;

            if (!hasBounds)
            {
                bounds =
                    renderer.bounds;

                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(
                    renderer.bounds
                );
            }
        }

        return hasBounds;
    }


    private void CalculateCameraFit(
        Bounds bounds)
    {
        float radius =
            bounds.extents.magnitude;

        radius =
            Mathf.Max(
                radius,
                0.5f
            );

        float halfFovRadians =
            cameraFieldOfView *
            0.5f *
            Mathf.Deg2Rad;

        float tangent =
            Mathf.Tan(
                halfFovRadians
            );

        if (tangent <= 0.0001f)
            tangent = 0.5f;

        baseCameraDistance =
            radius /
            tangent;

        baseCameraDistance *=
            fitPadding;

        baseCameraDistance *=
            cameraDistanceMultiplier;

        cameraLookHeight =
            bounds.extents.y *
            cameraHeightMultiplier;

        cameraLookHeight =
            Mathf.Clamp(
                cameraLookHeight,
                0.05f,
                0.75f
            );
    }


    // =========================================================
    // CAMERA UPDATE
    // =========================================================

    private void UpdateCameraImmediate()
    {
        if (
            showcaseCamera == null ||
            vehiclePivot == null
        )
            return;

        float distance =
            baseCameraDistance *
            currentZoom;

        showcaseCamera.transform.localPosition =
            new Vector3(
                0f,
                cameraLookHeight,
                -distance
            );

        Vector3 target;

        if (vehicleRotationPivot != null)
        {
            target =
                vehicleRotationPivot.position;
        }
        else
        {
            target =
                vehiclePivot.position;
        }

        Vector3 direction =
            target -
            showcaseCamera.transform.position;

        if (
            direction.sqrMagnitude >
            0.000001f
        )
        {
            showcaseCamera.transform.rotation =
                Quaternion.LookRotation(
                    direction.normalized,
                    Vector3.up
                );
        }
    }


    private void UpdateCameraSmooth()
    {
        if (
            showcaseCamera == null ||
            vehiclePivot == null
        )
            return;

        float distance =
            baseCameraDistance *
            currentZoom;

        Vector3 targetLocalPosition =
            new Vector3(
                0f,
                cameraLookHeight,
                -distance
            );

        float t =
            1f -
            Mathf.Exp(
                -18f *
                Time.unscaledDeltaTime
            );

        showcaseCamera.transform.localPosition =
            Vector3.Lerp(
                showcaseCamera.transform.localPosition,
                targetLocalPosition,
                t
            );

        Vector3 target;

        if (vehicleRotationPivot != null)
        {
            target =
                vehicleRotationPivot.position;
        }
        else
        {
            target =
                vehiclePivot.position;
        }

        Vector3 direction =
            target -
            showcaseCamera.transform.position;

        if (
            direction.sqrMagnitude >
            0.000001f
        )
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(
                    direction.normalized,
                    Vector3.up
                );

            showcaseCamera.transform.rotation =
                Quaternion.Slerp(
                    showcaseCamera.transform.rotation,
                    targetRotation,
                    t
                );
        }
    }


    // =========================================================
    // AUTO ROTATION
    // =========================================================

    private void AutoRotate()
    {
        if (
            vehicleRotationPivot == null ||
            currentVehicle == null
        )
            return;

        if (rotationSpeed <= 0f)
            return;

        float delta =
            rotationSpeed *
            Time.unscaledDeltaTime;

        vehicleRotationPivot.Rotate(
            0f,
            delta,
            0f,
            Space.Self
        );
    }


    // =========================================================
    // MANUAL ROTATION
    // =========================================================

    private void HandleMouseRotation()
    {
        if (
            showcaseView == null ||
            vehicleRotationPivot == null ||
            currentVehicle == null
        )
            return;

        RectTransform rect =
            showcaseView.rectTransform;

        bool pointerInside =
            RectTransformUtility
                .RectangleContainsScreenPoint(
                    rect,
                    Input.mousePosition,
                    null
                );

        /*
         * START DRAG
         */
        if (Input.GetMouseButtonDown(0))
        {
            if (pointerInside)
            {
                isDragging = true;

                lastMouseX =
                    Input.mousePosition.x;
            }
        }

        /*
         * STOP DRAG
         */
        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        if (
            !isDragging ||
            !Input.GetMouseButton(0)
        )
            return;

        float currentMouseX =
            Input.mousePosition.x;

        float deltaX =
            currentMouseX -
            lastMouseX;

        lastMouseX =
            currentMouseX;

        if (
            Mathf.Abs(deltaX) <=
            0.001f
        )
            return;

        vehicleRotationPivot.Rotate(
            0f,
            -deltaX *
            manualRotationSpeed,
            0f,
            Space.Self
        );
    }


    // =========================================================
    // ROTATION CONTROL API
    // =========================================================

    public void SetAutoRotate(
        bool enabled)
    {
        autoRotate = enabled;
    }


    public void ToggleAutoRotate()
    {
        autoRotate =
            !autoRotate;
    }


    public void SetRotationSpeed(
        float speed)
    {
        rotationSpeed =
            Mathf.Max(
                0f,
                speed
            );
    }


    public void ResetVehicleRotation()
    {
        if (vehicleRotationPivot == null)
            return;

        vehicleRotationPivot.localRotation =
            Quaternion.identity;
    }


    // =========================================================
    // ZOOM
    // =========================================================

    private void HandleZoom()
    {
        if (showcaseView == null)
            return;

        bool pointerInside =
            RectTransformUtility
                .RectangleContainsScreenPoint(
                    showcaseView.rectTransform,
                    Input.mousePosition,
                    null
                );

        if (!pointerInside)
            return;

        float wheel =
            Input.mouseScrollDelta.y;

        if (Mathf.Abs(wheel) > 0.0001f)
        {
            targetZoom -=
                wheel *
                zoomStep;

            targetZoom =
                Mathf.Clamp(
                    targetZoom,
                    minZoom,
                    maxZoom
                );
        }

        currentZoom =
            Mathf.SmoothDamp(
                currentZoom,
                targetZoom,
                ref zoomVelocity,
                zoomSmoothTime,
                Mathf.Infinity,
                Time.unscaledDeltaTime
            );
    }


    // =========================================================
    // SANITIZE
    // =========================================================

    private static void SanitizeVehicle(
        GameObject vehicle)
    {
        if (vehicle == null)
            return;

        Rigidbody[] rigidbodies =
            vehicle.GetComponentsInChildren<
                Rigidbody
            >(true);

        for (
            int i = 0;
            i < rigidbodies.Length;
            i++
        )
        {
            Rigidbody body =
                rigidbodies[i];

            if (body == null)
                continue;

            body.isKinematic = true;
            body.useGravity = false;
            body.detectCollisions = false;
        }

        Collider[] colliders =
            vehicle.GetComponentsInChildren<
                Collider
            >(true);

        for (
            int i = 0;
            i < colliders.Length;
            i++
        )
        {
            if (colliders[i] != null)
                colliders[i].enabled = false;
        }

        CharacterController[] controllers =
            vehicle.GetComponentsInChildren<
                CharacterController
            >(true);

        for (
            int i = 0;
            i < controllers.Length;
            i++
        )
        {
            if (controllers[i] != null)
                controllers[i].enabled = false;
        }

        Animator[] animators =
            vehicle.GetComponentsInChildren<
                Animator
            >(true);

        for (
            int i = 0;
            i < animators.Length;
            i++
        )
        {
            if (animators[i] != null)
                animators[i].enabled = false;
        }

        AudioSource[] audioSources =
            vehicle.GetComponentsInChildren<
                AudioSource
            >(true);

        for (
            int i = 0;
            i < audioSources.Length;
            i++
        )
        {
            AudioSource source =
                audioSources[i];

            if (source == null)
                continue;

            source.Stop();
            source.enabled = false;
        }
    }


    // =========================================================
    // LAYER
    // =========================================================

    private void EnsureVehicleLayer(
        GameObject vehicle)
    {
        if (vehicle == null)
            return;

        if (showcaseLayer < 0)
        {
            showcaseLayer =
                LayerMask.NameToLayer(
                    "GarageShowcase"
                );
        }

        if (showcaseLayer < 0)
        {
            Debug.LogError(
                "[GarageShowcaseController] " +
                "GarageShowcase layer không tồn tại.",
                this
            );

            return;
        }

        SetLayerRecursively(
            vehicle,
            showcaseLayer
        );
    }


    private static void SetLayerRecursively(
        GameObject root,
        int layer)
    {
        if (root == null)
            return;

        root.layer = layer;

        Transform rootTransform =
            root.transform;

        int childCount =
            rootTransform.childCount;

        for (
            int i = 0;
            i < childCount;
            i++
        )
        {
            Transform child =
                rootTransform.GetChild(i);

            SetLayerRecursively(
                child.gameObject,
                layer
            );
        }
    }


    // =========================================================
    // DESTROY
    // =========================================================

    private void DestroyCurrentVehicle()
    {
        if (vehicleRotationPivot != null)
        {
            DestroyImmediateSafe(
                vehicleRotationPivot.gameObject
            );

            vehicleRotationPivot = null;
        }

        if (currentVehicle == null)
        {
            currentVehicleId = -1;
            return;
        }

        /*
         * Vehicle đã nằm dưới rotation pivot,
         * nên destroy pivot sẽ destroy vehicle.
         *
         * Không Destroy vehicle lần thứ hai.
         */
        currentVehicle = null;
        currentVehicleId = -1;
    }


    private static void DestroyImmediateSafe(
        GameObject target)
    {
        if (target == null)
            return;

        /*
         * Runtime:
         * dùng Destroy.
         *
         * Editor:
         * dùng DestroyImmediate.
         */
        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }


    private void DisableCamera()
    {
        if (showcaseCamera != null)
            showcaseCamera.enabled = false;
    }


    // =========================================================
    // VALIDATION
    // =========================================================

    private static bool IsValidVehicleId(
        int vehicleId)
    {
        return
            vehicleId >= MinVehicleId &&
            vehicleId <= MaxVehicleId;
    }


#if UNITY_EDITOR

    // =========================================================
    // EDITOR VALIDATION
    // =========================================================

    private void OnValidate()
    {
        rotationSpeed =
            Mathf.Max(
                0f,
                rotationSpeed
            );

        manualRotationSpeed =
            Mathf.Max(
                0f,
                manualRotationSpeed
            );

        fitPadding =
            Mathf.Max(
                1f,
                fitPadding
            );

        cameraDistanceMultiplier =
            Mathf.Max(
                0.1f,
                cameraDistanceMultiplier
            );

        cameraHeightMultiplier =
            Mathf.Max(
                0f,
                cameraHeightMultiplier
            );

        zoomStep =
            Mathf.Max(
                0f,
                zoomStep
            );

        minZoom =
            Mathf.Max(
                0.01f,
                minZoom
            );

        maxZoom =
            Mathf.Max(
                minZoom,
                maxZoom
            );

        zoomSmoothTime =
            Mathf.Max(
                0.01f,
                zoomSmoothTime
            );
    }

#endif
}
