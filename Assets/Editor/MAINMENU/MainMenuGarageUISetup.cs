#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public static class MainMenuGarageUISetup
{
    private const string GaragePanelName =
        "GaragePanel";

    private const string ShowcaseWorldName =
        "GarageShowcaseWorld";

    private const string VehiclePivotName =
        "VehiclePivot";

    private const string ShowcaseCameraName =
        "ShowcaseCamera";

    private const string ShowcaseFrameName =
        "ShowcaseFrame";

    private const string ShowcaseViewName =
        "ShowcaseView";

    private const string ShowcaseLayerName =
        "GarageShowcase";

    private const string RenderTextureFolder =
        "Assets/Generated/Garage";

    private const string RenderTexturePath =
        "Assets/Generated/Garage/GarageShowcaseRT.renderTexture";

    private const int RenderTextureWidth =
        1280;

    private const int RenderTextureHeight =
        720;


    // =========================================================
    // MENU
    // =========================================================

    [MenuItem(
        "LEAD KHÔNG PHANH/Main Menu/Repair Garage Showcase"
    )]
    public static void RepairGarage()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError(
                "[GarageSetup] " +
                "Không thể setup Garage trong Play Mode."
            );

            return;
        }

        Scene activeScene =
            SceneManager.GetActiveScene();

        if (!activeScene.IsValid())
        {
            Debug.LogError(
                "[GarageSetup] Active Scene không hợp lệ."
            );

            return;
        }


        // =====================================================
        // LAYER
        // =====================================================

        int showcaseLayer =
            GetOrCreateLayer(
                ShowcaseLayerName
            );

        if (showcaseLayer < 0)
        {
            Debug.LogError(
                "[GarageSetup] " +
                "Không tạo được layer " +
                ShowcaseLayerName
            );

            return;
        }


        // =====================================================
        // DATABASE
        // =====================================================

        GameItemDatabase database =
            FindGameItemDatabaseAsset();

        if (database == null)
        {
            Debug.LogError(
                "[GarageSetup] " +
                "Không tìm thấy GameItemDatabase."
            );

            return;
        }

        database.Initialize();


        // =====================================================
        // GARAGE PANEL
        // =====================================================

        GameObject garagePanel =
            GameObject.Find(
                GaragePanelName
            );

        if (garagePanel == null)
        {
            Debug.LogError(
                "[GarageSetup] " +
                "Không tìm thấy GaragePanel."
            );

            return;
        }


        // =====================================================
        // MAIN MENU GARAGE UI
        // =====================================================

        MainMenuGarageUI garageUI =
            garagePanel.GetComponent<MainMenuGarageUI>();

        if (garageUI == null)
        {
            garageUI =
                garagePanel.AddComponent<MainMenuGarageUI>();
        }


        // =====================================================
        // SHOWCASE WORLD
        // =====================================================

        GameObject showcaseWorld =
            GameObject.Find(
                ShowcaseWorldName
            );

        if (showcaseWorld == null)
        {
            showcaseWorld =
                new GameObject(
                    ShowcaseWorldName
                );
        }


        /*
         * BẮT BUỘC:
         *
         * GarageShowcaseWorld KHÔNG nằm trong Canvas.
         */

        showcaseWorld.transform.SetParent(
            null,
            true
        );

        showcaseWorld.transform.position =
            Vector3.zero;

        showcaseWorld.transform.rotation =
            Quaternion.identity;

        showcaseWorld.transform.localScale =
            Vector3.one;


        // =====================================================
        // SHOWCASE CONTROLLER
        // =====================================================

        GarageShowcaseController showcaseController =
            showcaseWorld.GetComponent<
                GarageShowcaseController
            >();

        if (showcaseController == null)
        {
            showcaseController =
                showcaseWorld.AddComponent<
                    GarageShowcaseController
                >();
        }


        // =====================================================
        // VEHICLE PIVOT
        // =====================================================

        Transform vehiclePivot =
            showcaseWorld.transform.Find(
                VehiclePivotName
            );

        if (vehiclePivot == null)
        {
            GameObject pivotObject =
                new GameObject(
                    VehiclePivotName
                );

            pivotObject.transform.SetParent(
                showcaseWorld.transform,
                false
            );

            pivotObject.transform.localPosition =
                Vector3.zero;

            pivotObject.transform.localRotation =
                Quaternion.identity;

            pivotObject.transform.localScale =
                Vector3.one;

            vehiclePivot =
                pivotObject.transform;
        }


        // =====================================================
        // CAMERA
        // =====================================================

        Camera showcaseCamera =
            showcaseWorld.GetComponentInChildren<Camera>(
                true
            );

        if (showcaseCamera == null)
        {
            GameObject cameraObject =
                new GameObject(
                    ShowcaseCameraName,
                    typeof(Camera)
                );

            cameraObject.transform.SetParent(
                showcaseWorld.transform,
                false
            );

            showcaseCamera =
                cameraObject.GetComponent<Camera>();
        }

        showcaseCamera.name =
            ShowcaseCameraName;

        showcaseCamera.transform.SetParent(
            showcaseWorld.transform,
            false
        );


        // =====================================================
        // RENDER TEXTURE
        // =====================================================

        RenderTexture showcaseTexture =
            GetOrCreateRenderTexture();

        if (showcaseTexture == null)
        {
            Debug.LogError(
                "[GarageSetup] " +
                "Không tạo được GarageShowcaseRT."
            );

            return;
        }


        // =====================================================
        // SHOWCASE FRAME
        // =====================================================

        Transform showcaseFrame =
            garagePanel.transform.Find(
                ShowcaseFrameName
            );

        if (showcaseFrame == null)
        {
            GameObject frameObject =
                new GameObject(
                    ShowcaseFrameName,
                    typeof(RectTransform),
                    typeof(Image)
                );

            frameObject.transform.SetParent(
                garagePanel.transform,
                false
            );

            RectTransform frameRect =
                frameObject.GetComponent<RectTransform>();

            SetRect(
                frameRect,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero
            );

            Image frameImage =
                frameObject.GetComponent<Image>();

            frameImage.color =
                new Color(
                    0.018f,
                    0.024f,
                    0.034f,
                    1f
                );

            frameImage.raycastTarget =
                false;

            showcaseFrame =
                frameObject.transform;
        }


        // =====================================================
        // SHOWCASE VIEW
        // =====================================================

        RawImage showcaseView =
            showcaseFrame.GetComponentInChildren<RawImage>(
                true
            );

        if (showcaseView == null)
        {
            GameObject viewObject =
                new GameObject(
                    ShowcaseViewName,
                    typeof(RectTransform),
                    typeof(RawImage)
                );

            viewObject.transform.SetParent(
                showcaseFrame,
                false
            );

            showcaseView =
                viewObject.GetComponent<RawImage>();
        }

        showcaseView.name =
            ShowcaseViewName;

        showcaseView.texture =
            showcaseTexture;

        showcaseView.raycastTarget =
            false;

        SetRect(
            showcaseView.rectTransform,
            new Vector2(0.23f, 0.13f),
            new Vector2(0.77f, 0.87f),
            Vector2.zero,
            Vector2.zero
        );


        // =====================================================
        // CAMERA CONFIG
        // =====================================================

        showcaseCamera.targetTexture =
            showcaseTexture;

        showcaseCamera.cullingMask =
            1 << showcaseLayer;

        showcaseCamera.clearFlags =
            CameraClearFlags.SolidColor;

        showcaseCamera.backgroundColor =
            new Color(
                0.012f,
                0.018f,
                0.028f,
                1f
            );

        showcaseCamera.fieldOfView =
            35f;

        showcaseCamera.nearClipPlane =
            0.01f;

        showcaseCamera.farClipPlane =
            100f;

        showcaseCamera.enabled =
            false;


        // =====================================================
        // LAYER GARAGE WORLD
        // =====================================================

        SetLayerRecursively(
            showcaseWorld,
            showcaseLayer
        );


        // =====================================================
        // CAMERA ISOLATION
        // =====================================================

        Camera[] cameras =
            Object.FindObjectsByType<Camera>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        int garageLayerMask =
            1 << showcaseLayer;

        for (int i = 0;
             i < cameras.Length;
             i++)
        {
            Camera camera =
                cameras[i];

            if (camera == null)
            {
                continue;
            }

            /*
             * Garage camera:
             * chỉ nhìn GarageShowcase.
             */

            if (camera == showcaseCamera)
            {
                camera.cullingMask =
                    garageLayerMask;

                continue;
            }

            /*
             * MainMenu camera / UI camera / các camera khác:
             * tuyệt đối không nhìn GarageShowcase.
             */

            camera.cullingMask &=
                ~garageLayerMask;
        }


        // =====================================================
        // EQUIPMENT MANAGER
        // =====================================================

        EquipmentManager equipmentManager =
            EquipmentManager.Instance;

        if (equipmentManager == null)
        {
            equipmentManager =
                Object.FindFirstObjectByType<
                    EquipmentManager
                >();
        }


        // =====================================================
        // SERIALIZED GARAGE UI
        // =====================================================

        SerializedObject garageSerialized =
            new SerializedObject(
                garageUI
            );

        AssignObject(
            garageSerialized,
            "panelRoot",
            garagePanel
        );

        AssignObject(
            garageSerialized,
            "itemDatabase",
            database
        );

        AssignObject(
            garageSerialized,
            "equipmentManager",
            equipmentManager
        );

        AssignObject(
            garageSerialized,
            "showcaseController",
            showcaseController
        );

        Button leftButton =
            FindButton(
                garagePanel.transform,
                "LeftButton"
            );

        Button rightButton =
            FindButton(
                garagePanel.transform,
                "RightButton"
            );

        Button equipButton =
            FindButton(
                garagePanel.transform,
                "EquipButton"
            );

        Button closeButton =
            FindButton(
                garagePanel.transform,
                "CloseButton"
            );

        TMP_Text vehicleName =
            FindTMP(
                garagePanel.transform,
                "VehicleName"
            );

        TMP_Text vehicleDescription =
            FindTMP(
                garagePanel.transform,
                "VehicleDescription"
            );

        TMP_Text vehicleIndex =
            FindTMP(
                garagePanel.transform,
                "VehicleIndex"
            );

        TMP_Text vehicleStatus =
            FindTMP(
                garagePanel.transform,
                "VehicleStatus"
            );

        TMP_Text equipButtonText =
            equipButton != null
                ? equipButton.GetComponentInChildren<
                    TMP_Text
                  >(true)
                : null;

        AssignObject(
            garageSerialized,
            "leftButton",
            leftButton
        );

        AssignObject(
            garageSerialized,
            "rightButton",
            rightButton
        );

        AssignObject(
            garageSerialized,
            "vehicleNameText",
            vehicleName
        );

        AssignObject(
            garageSerialized,
            "vehicleDescriptionText",
            vehicleDescription
        );

        AssignObject(
            garageSerialized,
            "vehicleIndexText",
            vehicleIndex
        );

        AssignObject(
            garageSerialized,
            "vehicleStatusText",
            vehicleStatus
        );

        AssignObject(
            garageSerialized,
            "equipButton",
            equipButton
        );

        AssignObject(
            garageSerialized,
            "equipButtonText",
            equipButtonText
        );

        AssignObject(
            garageSerialized,
            "closeButton",
            closeButton
        );

        garageSerialized.ApplyModifiedProperties();


        // =====================================================
        // SERIALIZED SHOWCASE CONTROLLER
        // =====================================================

        SerializedObject showcaseSerialized =
            new SerializedObject(
                showcaseController
            );

        AssignObject(
            showcaseSerialized,
            "itemDatabase",
            database
        );

        AssignObject(
            showcaseSerialized,
            "equipmentManager",
            equipmentManager
        );

        AssignObject(
            showcaseSerialized,
            "vehiclePivot",
            vehiclePivot
        );

        AssignObject(
            showcaseSerialized,
            "showcaseCamera",
            showcaseCamera
        );

        AssignObject(
            showcaseSerialized,
            "showcaseTexture",
            showcaseTexture
        );

        AssignObject(
            showcaseSerialized,
            "showcaseView",
            showcaseView
        );

        SerializedProperty layerProperty =
            showcaseSerialized.FindProperty(
                "showcaseLayer"
            );

        if (layerProperty != null)
        {
            layerProperty.intValue =
                showcaseLayer;
        }

        showcaseSerialized.ApplyModifiedProperties();


        // =====================================================
        // UI LAYER
        // =====================================================

        int uiLayer =
            LayerMask.NameToLayer(
                "UI"
            );

        if (uiLayer >= 0)
        {
            SetLayerRecursively(
                garagePanel,
                uiLayer
            );
        }


        // =====================================================
        // WORLD MUST START DISABLED
        // =====================================================

        showcaseWorld.SetActive(
            false
        );


        // =====================================================
        // PANEL START STATE
        // =====================================================

        garagePanel.SetActive(
            false
        );


        // =====================================================
        // SAVE
        // =====================================================

        EditorUtility.SetDirty(
            garageUI
        );

        EditorUtility.SetDirty(
            showcaseController
        );

        EditorSceneManager.MarkSceneDirty(
            activeScene
        );

        AssetDatabase.SaveAssets();

        AssetDatabase.Refresh();


        Selection.activeGameObject =
            garagePanel;


        Debug.Log(
            "[GarageSetup] ================================="
        );

        Debug.Log(
            "[GarageSetup] GARAGE REPAIR SUCCESS"
        );

        Debug.Log(
            "[GarageSetup] Vehicle range = 20-39"
        );

        Debug.Log(
            "[GarageSetup] Database = " +
            database.name
        );

        Debug.Log(
            "[GarageSetup] Equipped state = EquipmentManager"
        );

        Debug.Log(
            "[GarageSetup] Showcase Layer = " +
            ShowcaseLayerName +
            " (" +
            showcaseLayer +
            ")"
        );

        Debug.Log(
            "[GarageSetup] RenderTexture = " +
            showcaseTexture.name
        );

        Debug.Log(
            "[GarageSetup] GarageWorld = ROOT LEVEL"
        );

        Debug.Log(
            "[GarageSetup] Other Cameras exclude GarageShowcase"
        );

        Debug.Log(
            "[GarageSetup] ================================="
        );
    }


    // =========================================================
    // DATABASE
    // =========================================================

    private static GameItemDatabase
        FindGameItemDatabaseAsset()
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "t:GameItemDatabase"
            );

        if (guids == null ||
            guids.Length == 0)
        {
            return null;
        }

        for (int i = 0;
             i < guids.Length;
             i++)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guids[i]
                );

            GameItemDatabase database =
                AssetDatabase.LoadAssetAtPath<
                    GameItemDatabase
                >(path);

            if (database != null)
            {
                return database;
            }
        }

        return null;
    }


    // =========================================================
    // RENDER TEXTURE
    // =========================================================

    private static RenderTexture
        GetOrCreateRenderTexture()
    {
        EnsureFolder(
            "Assets/Generated"
        );

        EnsureFolder(
            RenderTextureFolder
        );

        RenderTexture texture =
            AssetDatabase.LoadAssetAtPath<
                RenderTexture
            >(
                RenderTexturePath
            );

        if (texture != null)
        {
            return texture;
        }

        texture =
            new RenderTexture(
                RenderTextureWidth,
                RenderTextureHeight,
                24,
                RenderTextureFormat.ARGB32
            );

        texture.name =
            "GarageShowcaseRT";

        texture.filterMode =
            FilterMode.Bilinear;

        texture.wrapMode =
            TextureWrapMode.Clamp;

        texture.useMipMap =
            false;

        texture.autoGenerateMips =
            false;

        texture.Create();

        AssetDatabase.CreateAsset(
            texture,
            RenderTexturePath
        );

        AssetDatabase.SaveAssets();

        return texture;
    }


    // =========================================================
    // FOLDER
    // =========================================================

    private static void EnsureFolder(
        string path)
    {
        if (AssetDatabase.IsValidFolder(
                path
            ))
        {
            return;
        }

        int slash =
            path.LastIndexOf('/');

        if (slash <= 0)
        {
            return;
        }

        string parent =
            path.Substring(
                0,
                slash
            );

        string folder =
            path.Substring(
                slash + 1
            );

        if (!AssetDatabase.IsValidFolder(
                parent
            ))
        {
            EnsureFolder(
                parent
            );
        }

        AssetDatabase.CreateFolder(
            parent,
            folder
        );
    }


    // =========================================================
    // BUTTON FIND
    // =========================================================

    private static Button FindButton(
        Transform root,
        string objectName)
    {
        Transform target =
            FindChildRecursive(
                root,
                objectName
            );

        if (target == null)
        {
            return null;
        }

        return target.GetComponent<Button>();
    }


    // =========================================================
    // TMP FIND
    // =========================================================

    private static TMP_Text FindTMP(
        Transform root,
        string objectName)
    {
        Transform target =
            FindChildRecursive(
                root,
                objectName
            );

        if (target == null)
        {
            return null;
        }

        return target.GetComponent<TMP_Text>();
    }


    // =========================================================
    // CHILD FIND
    // =========================================================

    private static Transform FindChildRecursive(
        Transform root,
        string objectName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name ==
            objectName)
        {
            return root;
        }

        int childCount =
            root.childCount;

        for (int i = 0;
             i < childCount;
             i++)
        {
            Transform result =
                FindChildRecursive(
                    root.GetChild(i),
                    objectName
                );

            if (result != null)
            {
                return result;
            }
        }

        return null;
    }


    // =========================================================
    // SERIALIZED ASSIGN
    // =========================================================

    private static void AssignObject(
        SerializedObject serializedObject,
        string propertyName,
        Object value)
    {
        SerializedProperty property =
            serializedObject.FindProperty(
                propertyName
            );

        if (property == null)
        {
            Debug.LogWarning(
                "[GarageSetup] " +
                "Không tìm thấy serialized field: " +
                propertyName
            );

            return;
        }

        property.objectReferenceValue =
            value;
    }


    // =========================================================
    // RECT
    // =========================================================

    private static void SetRect(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        rect.anchorMin =
            anchorMin;

        rect.anchorMax =
            anchorMax;

        rect.offsetMin =
            offsetMin;

        rect.offsetMax =
            offsetMax;
    }


    // =========================================================
    // LAYER
    // =========================================================

    private static int GetOrCreateLayer(
        string layerName)
    {
        int existingLayer =
            LayerMask.NameToLayer(
                layerName
            );

        if (existingLayer >= 0)
        {
            return existingLayer;
        }

        Object[] assets =
            AssetDatabase.LoadAllAssetsAtPath(
                "ProjectSettings/TagManager.asset"
            );

        if (assets == null ||
            assets.Length == 0)
        {
            return -1;
        }

        SerializedObject tagManager =
            new SerializedObject(
                assets[0]
            );

        SerializedProperty layers =
            tagManager.FindProperty(
                "layers"
            );

        if (layers == null)
        {
            return -1;
        }

        for (int i = 8;
             i < 32;
             i++)
        {
            SerializedProperty layer =
                layers.GetArrayElementAtIndex(
                    i
                );

            if (string.IsNullOrEmpty(
                    layer.stringValue
                ))
            {
                layer.stringValue =
                    layerName;

                tagManager.ApplyModifiedProperties();

                return i;
            }
        }

        return -1;
    }


    // =========================================================
    // RECURSIVE LAYER
    // =========================================================

    private static void SetLayerRecursively(
        GameObject root,
        int layer)
    {
        if (root == null)
        {
            return;
        }

        root.layer =
            layer;

        Transform rootTransform =
            root.transform;

        int childCount =
            rootTransform.childCount;

        for (int i = 0;
             i < childCount;
             i++)
        {
            SetLayerRecursively(
                rootTransform
                    .GetChild(i)
                    .gameObject,
                layer
            );
        }
    }
}

#endif